////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Framework.Landscapes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade
{
	public static class WorldSingleZoneParityValidator
	{
		private const float PositionTolerance = 0.005f;
		private const float RotationToleranceDegrees = 0.1f;
		private const int ComponentSampleLimit = 4096;

		public static WorldSingleZoneRuntimeEvidenceData Validate(WorldCellBundleRepository repository, string sourceRoot,
			int cycleCount)
		{
			if (repository == null)
				throw new ArgumentNullException(nameof(repository));
			if (cycleCount < 1)
				throw new ArgumentOutOfRangeException(nameof(cycleCount));
			WorldSingleZoneRuntimeEvidenceData evidence = new WorldSingleZoneRuntimeEvidenceData
			{
				WorldId = repository.Manifest.WorldId,
				ZoneKey = repository.Zone.ZoneKey,
				ZoneId = repository.Zone.ZoneId,
				ActiveMapName = Level.info?.name,
				ManifestContentFingerprintSha256 = repository.Manifest.ContentFingerprintSha256,
				CellCount = repository.Zone.Cells.Count,
				EntityCount = repository.Zone.EntityCount,
				CellCycleCount = cycleCount,
			};

			RunCellCycles(repository, cycleCount, evidence);
			ValidateRecords(repository, sourceRoot, evidence);
			if (evidence.DefaultHeightmapFallbackCount > 0)
				AddWarning(evidence, "DefaultHeightmapFallback", evidence.ZoneId, evidence.DefaultHeightmapFallbackCount + " orphan landscape cell(s) use flat height 0 as an explicit fallback.");
			if (evidence.BlackSplatFallbackPixelCount > 0)
				AddWarning(evidence, "BlackSplatFallback", evidence.ZoneId, evidence.BlackSplatFallbackPixelCount + " black splat pixel(s) use layer 0 as an explicit fallback.");
			if (evidence.ObjectTransformUnavailableCount > 0)
				AddWarning(evidence, "BaselineTransformUnavailable", evidence.ZoneId,
					evidence.ObjectTransformUnavailableCount + " baseline object transform(s) are unavailable because assets are missing or runtime batching/clutter policy skipped instances.");

			evidence.FinalLoadedCellCount = repository.LoadedCellCount;
			evidence.FinalLoadedEntityCount = repository.LoadedEntityCount;
			evidence.FinalEstimatedResidentBytes = repository.EstimatedResidentBytes;
			if (evidence.FinalLoadedCellCount != 0 || evidence.FinalLoadedEntityCount != 0 || evidence.FinalEstimatedResidentBytes != 0)
				AddError(evidence, "RepositoryResidencyLeak", evidence.ZoneId, "Repository retained cell/entity records after unload.");
			evidence.ErrorCount = evidence.Issues.Count(issue => issue.Severity == "Error");
			evidence.WarningCount = evidence.Issues.Count(issue => issue.Severity == "Warning");
			evidence.IsValid = evidence.ErrorCount == 0;
			return evidence;
		}

		private static void RunCellCycles(WorldCellBundleRepository repository, int cycleCount,
			WorldSingleZoneRuntimeEvidenceData evidence)
		{
			WorldCellIndexData[] indices = repository.EnumerateCellIndices().ToArray();
			for (int cycle = 1; cycle <= cycleCount; ++cycle)
			{
				foreach (WorldCellIndexData index in indices)
					repository.LoadCell(index.CellId);
				WorldCellCycleSampleData sample = new WorldCellCycleSampleData
				{
					Cycle = cycle,
					LoadedCellCount = repository.LoadedCellCount,
					LoadedEntityCount = repository.LoadedEntityCount,
					EstimatedResidentBytes = repository.EstimatedResidentBytes,
				};
				evidence.PeakLoadedCellCount = Math.Max(evidence.PeakLoadedCellCount, sample.LoadedCellCount);
				evidence.PeakLoadedEntityCount = Math.Max(evidence.PeakLoadedEntityCount, sample.LoadedEntityCount);
				evidence.PeakEstimatedResidentBytes = Math.Max(evidence.PeakEstimatedResidentBytes, sample.EstimatedResidentBytes);
				repository.UnloadAll();
				sample.AfterUnloadCellCount = repository.LoadedCellCount;
				sample.AfterUnloadEntityCount = repository.LoadedEntityCount;
				sample.AfterUnloadEstimatedResidentBytes = repository.EstimatedResidentBytes;
				evidence.CellCycles.Add(sample);
				if (sample.AfterUnloadCellCount != 0 || sample.AfterUnloadEntityCount != 0 || sample.AfterUnloadEstimatedResidentBytes != 0)
					AddError(evidence, "CellCycleResidencyLeak", repository.Zone.ZoneId, "Cell repository did not return to zero residency after cycle " + cycle + ".");
			}
		}

		private static void ValidateRecords(WorldCellBundleRepository repository, string sourceRoot,
			WorldSingleZoneRuntimeEvidenceData evidence)
		{
			foreach (WorldCellIndexData index in repository.EnumerateCellIndices())
			{
				WorldCellData cell = repository.LoadCell(index.CellId);
				WorldEntityRecordData landscape = cell.Entities.FirstOrDefault(entity => entity.Kind == "LandscapeTile");
				if (landscape != null)
					ValidateLandscape(index, sourceRoot, landscape, evidence);
				foreach (WorldEntityRecordData entity in cell.Entities.Where(entity => entity.Kind == "StaticObject"))
					ValidateObject(entity, evidence);
				repository.UnloadCell(index.CellId);
			}
		}

		private static void ValidateLandscape(WorldCellIndexData index, string sourceRoot, WorldEntityRecordData entity,
			WorldSingleZoneRuntimeEvidenceData evidence)
		{
			evidence.LandscapeRecordCount++;
			WorldRuntimeTerrainPayload payload = WorldRuntimeTerrainDecoder.Decode(sourceRoot, index.GridX, index.GridZ);
			if (payload.HasHeightmapSource)
				evidence.HeightmapSourceCount++;
			if (payload.UsedDefaultHeightmap)
				evidence.DefaultHeightmapFallbackCount++;
			evidence.BlackSplatFallbackPixelCount += payload.BlackSplatFallbackPixelCount;
			LandscapeTile baseline = Landscape.getTile(new LandscapeCoord(index.GridX, index.GridZ));
			if (baseline == null)
			{
				if (payload.HasHeightmapSource)
				{
					evidence.LandscapeBaselineMissingCount++;
					AddError(evidence, "LandscapeBaselineMissing", entity.EntityId, "Source heightmap exists but active baseline landscape tile is missing.");
				}
				return;
			}
			if (baseline.collider == null || baseline.collider.terrainData == null)
			{
				evidence.TerrainColliderMissingCount++;
				AddError(evidence, "TerrainColliderMissing", entity.EntityId, "Active baseline landscape tile has no TerrainCollider data.");
			}

			for (int x = 0; x < WorldRuntimeTerrainDecoder.HeightResolution; ++x)
			{
				for (int y = 0; y < WorldRuntimeTerrainDecoder.HeightResolution; ++y)
				{
					evidence.HeightSamplesCompared++;
					if (Math.Abs(payload.Heights[x, y] - baseline.heightmap[x, y]) > 0.000001f)
						evidence.HeightSampleMismatchCount++;
				}
			}
			for (int x = 0; x < WorldRuntimeTerrainDecoder.SplatResolution; ++x)
			{
				for (int y = 0; y < WorldRuntimeTerrainDecoder.SplatResolution; ++y)
				{
					float baselineSum = 0f;
					for (int layer = 0; layer < WorldRuntimeTerrainDecoder.SplatLayers; ++layer)
						baselineSum += baseline.splatmap[x, y, layer];
					for (int layer = 0; layer < WorldRuntimeTerrainDecoder.SplatLayers; ++layer)
					{
						evidence.SplatSamplesCompared++;
						if (baselineSum > 0.000001f && Math.Abs(payload.SplatWeights[x, y, layer] - baseline.splatmap[x, y, layer]) > 0.000001f)
							evidence.SplatSampleMismatchCount++;
					}
					evidence.HoleSamplesCompared++;
					if (payload.Holes[x, y] != baseline.holes[x, y])
						evidence.HoleSampleMismatchCount++;
				}
			}

			if (evidence.HeightSampleMismatchCount > 0)
				AddErrorOnce(evidence, "HeightParityMismatch", evidence.ZoneId, "Runtime-decoded height samples differ from active baseline landscape.");
			if (evidence.SplatSampleMismatchCount > 0)
				AddErrorOnce(evidence, "SplatParityMismatch", evidence.ZoneId, "Runtime-decoded non-black splat samples differ from active baseline landscape.");
			if (evidence.HoleSampleMismatchCount > 0)
				AddErrorOnce(evidence, "HoleParityMismatch", evidence.ZoneId, "Runtime-decoded hole samples differ from active baseline landscape.");
		}

		private static void ValidateObject(WorldEntityRecordData entity, WorldSingleZoneRuntimeEvidenceData evidence)
		{
			evidence.StaticObjectRecordCount++;
			if (entity.SourceInstanceId <= 0 || entity.SourceInstanceId > uint.MaxValue)
			{
				evidence.BaselineObjectMissingCount++;
				return;
			}
			LevelObject baseline = LevelObjects.FindLevelObjectByInstanceId((uint) entity.SourceInstanceId);
			if (baseline == null)
			{
				evidence.BaselineObjectMissingCount++;
				return;
			}
			if (Guid.TryParse(entity.AssetGuid, out Guid expectedGuid) && baseline.GUID != expectedGuid)
				evidence.ObjectGuidMismatchCount++;
			Transform transform = baseline.transform != null ? baseline.transform : baseline.placeholderTransform;
			if (transform == null)
			{
				evidence.ObjectTransformUnavailableCount++;
				return;
			}
			evidence.ObjectTransformsCompared++;
			Vector3 expectedPosition = WorldRuntimePreviewFactory.ToVector3(entity.WorldPosition);
			if (Vector3.Distance(transform.position, expectedPosition) > PositionTolerance)
				evidence.ObjectPositionMismatchCount++;
			Quaternion expectedRotation = Quaternion.Euler(WorldRuntimePreviewFactory.ToVector3(entity.Rotation));
			if (Quaternion.Angle(transform.rotation, expectedRotation) > RotationToleranceDegrees)
				evidence.ObjectRotationMismatchCount++;
			Vector3 expectedScale = baseline.asset != null && baseline.asset.useScale
				? WorldRuntimePreviewFactory.ToVector3(entity.Scale)
				: Vector3.one;
			if (Vector3.Distance(transform.localScale, expectedScale) > PositionTolerance)
				evidence.ObjectScaleMismatchCount++;

			if (evidence.ObjectComponentSampleCount < ComponentSampleLimit)
			{
				evidence.ObjectComponentSampleCount++;
				if (transform.GetComponentInChildren<Collider>(true) != null)
					evidence.BaselineObjectsWithCollider++;
				if (transform.GetComponentInChildren<Renderer>(true) != null)
					evidence.BaselineObjectsWithRenderer++;
			}

			if (evidence.BaselineObjectMissingCount > 0)
				AddErrorOnce(evidence, "BaselineObjectMissing", evidence.ZoneId, "One or more stable source instance IDs are missing from active LevelObjects.");
			if (evidence.ObjectGuidMismatchCount > 0)
				AddErrorOnce(evidence, "ObjectGuidMismatch", evidence.ZoneId, "One or more object GUIDs differ from the active baseline.");
			if (evidence.ObjectPositionMismatchCount > 0 || evidence.ObjectRotationMismatchCount > 0 || evidence.ObjectScaleMismatchCount > 0)
				AddErrorOnce(evidence, "ObjectTransformMismatch", evidence.ZoneId, "One or more instantiated object transforms differ from schema records.");
		}

		private static void AddErrorOnce(WorldSingleZoneRuntimeEvidenceData evidence, string code, string scopeId, string message)
		{
			if (!evidence.Issues.Any(issue => issue.Code == code))
				AddError(evidence, code, scopeId, message);
		}

		private static void AddError(WorldSingleZoneRuntimeEvidenceData evidence, string code, string scopeId, string message)
		{
			evidence.Issues.Add(new WorldRuntimeIssueData { Severity = "Error", Code = code, ScopeId = scopeId, Message = message });
		}

		private static void AddWarning(WorldSingleZoneRuntimeEvidenceData evidence, string code, string scopeId, string message)
		{
			evidence.Issues.Add(new WorldRuntimeIssueData { Severity = "Warning", Code = code, ScopeId = scopeId, Message = message });
		}
	}
}
