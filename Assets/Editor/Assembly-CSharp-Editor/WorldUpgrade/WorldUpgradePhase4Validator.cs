////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using Newtonsoft.Json;
using SDG.Framework.Landscapes;
using SDG.Unturned.WorldUpgrade;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public static class WorldUpgradePhase4Validator
	{
		private const string WorldKey = "u3-connected-world";
		private const string SourceZoneKey = "california-2";
		private const string DestinationZoneKey = "limestone";
		private const float PreloadRadius = 1536f;
		private const float UnloadRadius = 2048f;
		private const float SimulationStep = 128f;
		private const int RoundTrips = 20;

		public static void GenerateEvidence()
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string schemaRoot = Path.Combine(projectRoot, "Builds", "WorldUpgrade", "WorldSchemas", WorldKey);
			string catalogPath = Path.Combine(projectRoot, "Builds", "WorldUpgrade", "WorldSourceCatalog.json");
			RawMapSourceCatalogData catalog = ReadJson<RawMapSourceCatalogData>(catalogPath);
			Dictionary<string, string> sourceRoots = catalog.Maps.Where(map => map.Enabled)
				.ToDictionary(map => map.ZoneId, map => map.SourceRoot, StringComparer.Ordinal);

			using (WorldCellBundleRepository sourceRepository = new WorldCellBundleRepository(schemaRoot, SourceZoneKey))
			using (WorldCellBundleRepository destinationRepository = new WorldCellBundleRepository(schemaRoot, DestinationZoneKey))
			using (WorldCellStreamer streamer = new WorldCellStreamer(new[] { sourceRepository, destinationRepository }, PreloadRadius, UnloadRadius))
			{
				WorldTwoZoneStreamingEvidenceData evidence = Validate(sourceRepository, destinationRepository, streamer, sourceRoots);
				string outputPath = Path.Combine(schemaRoot, "phase-4-two-zone-streaming-evidence.json");
				File.WriteAllText(outputPath, JsonConvert.SerializeObject(evidence, Formatting.Indented));
				Debug.LogFormat("World Upgrade Phase 4 validation complete: valid={0}, errors={1}, warnings={2}, samples={3}",
					evidence.IsValid, evidence.ErrorCount, evidence.WarningCount, evidence.PositionSampleCount);
				if (!evidence.IsValid)
					throw new InvalidDataException("Phase 4 validation failed with " + evidence.ErrorCount + " error(s).");
			}
		}

		public static void GenerateFromCommandLine()
		{
			try
			{
				GenerateEvidence();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				EditorApplication.Exit(1);
				return;
			}
			EditorApplication.Exit(0);
		}

		private static WorldTwoZoneStreamingEvidenceData Validate(WorldCellBundleRepository sourceRepository,
			WorldCellBundleRepository destinationRepository, WorldCellStreamer streamer, Dictionary<string, string> sourceRoots)
		{
			WorldManifestData manifest = sourceRepository.Manifest;
			WorldZoneResolver resolver = new WorldZoneResolver(manifest);
			WorldZoneIndexData sourceZone = resolver.FindZone(SourceZoneKey);
			WorldZoneIndexData destinationZone = resolver.FindZone(DestinationZoneKey);
			WorldTransitionSafetyService safety = new WorldTransitionSafetyService(sourceZone, destinationZone, 256f);
			WorldTwoZoneStreamingEvidenceData evidence = new WorldTwoZoneStreamingEvidenceData
			{
				WorldId = manifest.WorldId,
				ManifestContentFingerprintSha256 = manifest.ContentFingerprintSha256,
				ZoneCount = manifest.Zones.Count,
				SourceZoneKey = SourceZoneKey,
				DestinationZoneKey = DestinationZoneKey,
				Corridor = safety.Corridor,
				PreloadRadius = PreloadRadius,
				UnloadRadius = UnloadRadius,
				SimulationStepMeters = SimulationStep,
				RoundTripCount = RoundTrips,
			};

			GameObject sourceTerrain = null;
			GameObject destinationTerrain = null;
			GameObject bridge = null;
			try
			{
				sourceTerrain = CreateBoundaryTerrain(sourceRepository, sourceRoots[SourceZoneKey], safety.Corridor, true);
				destinationTerrain = CreateBoundaryTerrain(destinationRepository, sourceRoots[DestinationZoneKey], safety.Corridor, false);
				evidence.SourceTerrainCreated = HasTerrainCollider(sourceTerrain);
				evidence.DestinationTerrainCreated = HasTerrainCollider(destinationTerrain);
				float sourceY = SampleTerrainEdge(sourceTerrain, safety.Corridor.StartX, safety.Corridor.CenterZ);
				float destinationY = SampleTerrainEdge(destinationTerrain, safety.Corridor.EndX, safety.Corridor.CenterZ);
				bridge = safety.CreateCollisionBridge(sourceY, destinationY);
				evidence.TransitionBridgeCreated = bridge.GetComponent<BoxCollider>() != null;
				Physics.SyncTransforms();
				ValidatePhysicalCollision(sourceTerrain, destinationTerrain, bridge, safety.Corridor, evidence);
				RunStreamingSimulation(resolver, safety, streamer, sourceZone, destinationZone, evidence);
			}
			finally
			{
				DestroyTerrain(sourceTerrain);
				DestroyTerrain(destinationTerrain);
				if (bridge != null)
					UnityEngine.Object.DestroyImmediate(bridge);
				streamer.UnloadAll();
				evidence.FinalActiveCellCount = streamer.ActiveCellCount;
				evidence.FinalActiveEntityCount = streamer.ActiveEntityCount;
				evidence.FinalEstimatedResidentBytes = streamer.EstimatedResidentBytes;
			}

			evidence.PeakActiveCellCount = streamer.PeakActiveCellCount;
			evidence.PeakActiveEntityCount = streamer.PeakActiveEntityCount;
			evidence.PeakEstimatedResidentBytes = streamer.PeakEstimatedResidentBytes;
			evidence.ActivationCount = streamer.ActivationCount;
			evidence.DeactivationCount = streamer.DeactivationCount;
			evidence.LogicalMemoryPlateau = HasStablePlateau(evidence.Loops);
			evidence.NavigationDataAvailable = false;
			AddWarning(evidence, "NavigationStreamingDeferred", manifest.WorldId,
				"Navigation records are not yet present in the world schema; Phase 4 validates terrain collision and cell-owned object records only.");
			AddWarning(evidence, "PlayerTraversalNotExecuted", manifest.WorldId,
				"Boundary traversal is a deterministic probe simulation in one scene, not a manual player/vehicle gameplay session.");

			if (!evidence.SourceTerrainCreated || !evidence.DestinationTerrainCreated || !evidence.TransitionBridgeCreated)
				AddError(evidence, "TransitionCollisionCreationFailed", manifest.WorldId, "One or more boundary collision objects were not created.");
			if (evidence.PreloadMissCount > 0)
				AddError(evidence, "BoundaryPreloadMiss", manifest.WorldId, "A corridor sample did not have both boundary zones preloaded.");
			if (evidence.LogicalGroundCoverageMissCount > 0 || evidence.PhysicalCollisionMissCount > 0)
				AddError(evidence, "TransitionCollisionGap", manifest.WorldId, "One or more transition samples had no logical or physical ground collision coverage.");
			if (evidence.SceneHandleChangeCount > 0 || evidence.TeleportCount > 0)
				AddError(evidence, "NonContinuousTransition", manifest.WorldId, "The probe changed scene or exceeded the configured continuous movement step.");
			if (!evidence.LogicalMemoryPlateau)
				AddError(evidence, "StreamingResidencyGrowth", manifest.WorldId, "Per-loop logical residency did not settle to a stable plateau.");
			if (evidence.PeakCaliforniaStaticObjectRecords == 0 || evidence.PeakLimestoneStaticObjectRecords == 0)
				AddError(evidence, "StaticObjectPreloadMissing", manifest.WorldId, "Both zones were not observed with active static-object records.");
			if (evidence.FinalActiveCellCount != 0 || evidence.FinalActiveEntityCount != 0 || evidence.FinalEstimatedResidentBytes != 0)
				AddError(evidence, "FinalStreamingResidencyLeak", manifest.WorldId, "Cell repositories retained logical residency after final unload.");
			evidence.ErrorCount = evidence.Issues.Count(issue => issue.Severity == "Error");
			evidence.WarningCount = evidence.Issues.Count(issue => issue.Severity == "Warning");
			evidence.IsValid = evidence.ErrorCount == 0;
			return evidence;
		}

		private static void RunStreamingSimulation(WorldZoneResolver resolver, WorldTransitionSafetyService safety,
			WorldCellStreamer streamer, WorldZoneIndexData sourceZone, WorldZoneIndexData destinationZone,
			WorldTwoZoneStreamingEvidenceData evidence)
		{
			Vector3 source = new Vector3(safety.Corridor.StartX - 512f, 0f, safety.Corridor.CenterZ);
			Vector3 destination = new Vector3(safety.Corridor.EndX + 512f, 0f, safety.Corridor.CenterZ);
			int initialSceneHandle = SceneManager.GetActiveScene().handle;
			Vector3? previousPosition = null;
			string previousZone = null;
			bool wasInsideCorridor = false;
			for (int loop = 1; loop <= RoundTrips; ++loop)
			{
				WorldStreamingLoopSampleData loopSample = new WorldStreamingLoopSampleData { Loop = loop };
				foreach (Vector3 position in EnumeratePath(source, destination, SimulationStep).Concat(EnumeratePath(destination, source, SimulationStep).Skip(1)))
				{
					streamer.Update(position);
					evidence.PositionSampleCount++;
					WorldZoneResolution resolution = resolver.Resolve(position);
					if (previousZone != null && !string.Equals(previousZone, resolution.Zone.ZoneKey, StringComparison.Ordinal))
						evidence.ZoneResolutionChangeCount++;
					previousZone = resolution.Zone.ZoneKey;
					bool insideCorridor = safety.IsInsideCorridor(position);
					if (insideCorridor && !wasInsideCorridor)
						evidence.CorridorEntryCount++;
					wasInsideCorridor = insideCorridor;
					if (insideCorridor && !safety.AreBoundaryZonesPreloaded(streamer, position))
						evidence.PreloadMissCount++;
					if (!safety.HasGroundCoverage(streamer, position))
						evidence.LogicalGroundCoverageMissCount++;
					if (SceneManager.GetActiveScene().handle != initialSceneHandle)
						evidence.SceneHandleChangeCount++;
					if (previousPosition.HasValue && Vector3.Distance(previousPosition.Value, position) > SimulationStep + 0.01f)
						evidence.TeleportCount++;
					previousPosition = position;
					Vector3 reconstructed = WorldCoordinateStrategy.ToWorld(resolution.Zone, resolution.LocalPosition);
					evidence.MaximumCoordinateRoundTripError = Math.Max(evidence.MaximumCoordinateRoundTripError,
						Vector3.Distance(position, reconstructed));
					evidence.PeakCaliforniaStaticObjectRecords = Math.Max(evidence.PeakCaliforniaStaticObjectRecords,
						streamer.GetActiveRecordCount(SourceZoneKey, "StaticObject"));
					evidence.PeakLimestoneStaticObjectRecords = Math.Max(evidence.PeakLimestoneStaticObjectRecords,
						streamer.GetActiveRecordCount(DestinationZoneKey, "StaticObject"));
					loopSample.PeakActiveCellCount = Math.Max(loopSample.PeakActiveCellCount, streamer.ActiveCellCount);
					loopSample.PeakActiveEntityCount = Math.Max(loopSample.PeakActiveEntityCount, streamer.ActiveEntityCount);
					loopSample.PeakEstimatedResidentBytes = Math.Max(loopSample.PeakEstimatedResidentBytes, streamer.EstimatedResidentBytes);
				}
				evidence.Loops.Add(loopSample);
			}
		}

		private static IEnumerable<Vector3> EnumeratePath(Vector3 start, Vector3 end, float step)
		{
			float distance = Vector3.Distance(start, end);
			int segments = Math.Max(1, Mathf.CeilToInt(distance / step));
			for (int index = 0; index <= segments; ++index)
				yield return Vector3.Lerp(start, end, index / (float) segments);
		}

		private static GameObject CreateBoundaryTerrain(WorldCellBundleRepository repository, string sourceRoot,
			WorldTransitionCorridorData corridor, bool sourceSide)
		{
			WorldCellIndexData index = repository.EnumerateCellIndices()
				.Where(cell => cell.HasLandscapeRecord && corridor.CenterZ >= cell.WorldBounds.Min.Z && corridor.CenterZ <= cell.WorldBounds.Max.Z)
				.OrderBy(cell => sourceSide ? Math.Abs(cell.WorldBounds.Max.X - corridor.StartX) : Math.Abs(cell.WorldBounds.Min.X - corridor.EndX))
				.ThenBy(cell => cell.CellId, StringComparer.Ordinal)
				.First();
			WorldRuntimeTerrainPayload payload = WorldRuntimeTerrainDecoder.Decode(sourceRoot, index.GridX, index.GridZ);
			return WorldRuntimePreviewFactory.CreateTerrain(payload, null, WorldRuntimePreviewFactory.ToVector3(repository.Zone.LayoutOffset));
		}

		private static float SampleTerrainEdge(GameObject terrainObject, float worldX, float worldZ)
		{
			Terrain terrain = terrainObject.GetComponent<Terrain>();
			float normalizedX = Mathf.Clamp01((worldX - terrain.transform.position.x) / Landscape.TILE_SIZE);
			float normalizedZ = Mathf.Clamp01((worldZ - terrain.transform.position.z) / Landscape.TILE_SIZE);
			return terrain.transform.position.y + terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
		}

		private static void ValidatePhysicalCollision(GameObject sourceTerrain, GameObject destinationTerrain, GameObject bridge,
			WorldTransitionCorridorData corridor, WorldTwoZoneStreamingEvidenceData evidence)
		{
			HashSet<Collider> expected = new HashSet<Collider>
			{
				sourceTerrain.GetComponent<TerrainCollider>(),
				destinationTerrain.GetComponent<TerrainCollider>(),
				bridge.GetComponent<BoxCollider>(),
			};
			for (float x = corridor.StartX - 512f; x <= corridor.EndX + 512f; x += 64f)
			{
				evidence.PhysicalCollisionSampleCount++;
				if (!Physics.Raycast(new Vector3(x, 2048f, corridor.CenterZ), Vector3.down, out RaycastHit hit, 4096f) || !expected.Contains(hit.collider))
				{
					evidence.PhysicalCollisionMissCount++;
					Debug.LogWarningFormat("Phase 4 physical collision miss at x={0}, z={1}, hit={2}", x, corridor.CenterZ,
						hit.collider != null ? hit.collider.name : "none");
				}
			}
		}

		private static bool HasStablePlateau(IList<WorldStreamingLoopSampleData> loops)
		{
			if (loops == null || loops.Count < 2)
				return false;
			WorldStreamingLoopSampleData expected = loops[loops.Count - 1];
			return loops.Skip(1).All(loop => loop.PeakActiveCellCount == expected.PeakActiveCellCount &&
				loop.PeakActiveEntityCount == expected.PeakActiveEntityCount &&
				loop.PeakEstimatedResidentBytes == expected.PeakEstimatedResidentBytes);
		}

		private static bool HasTerrainCollider(GameObject value)
		{
			return value != null && value.GetComponent<Terrain>() != null && value.GetComponent<TerrainCollider>()?.terrainData != null;
		}

		private static void DestroyTerrain(GameObject value)
		{
			if (value == null)
				return;
			TerrainData data = value.GetComponent<Terrain>()?.terrainData;
			UnityEngine.Object.DestroyImmediate(value);
			if (data != null)
				UnityEngine.Object.DestroyImmediate(data);
		}

		private static T ReadJson<T>(string path)
		{
			return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
		}

		private static void AddError(WorldTwoZoneStreamingEvidenceData evidence, string code, string scopeId, string message)
		{
			evidence.Issues.Add(new WorldRuntimeIssueData { Severity = "Error", Code = code, ScopeId = scopeId, Message = message });
		}

		private static void AddWarning(WorldTwoZoneStreamingEvidenceData evidence, string code, string scopeId, string message)
		{
			evidence.Issues.Add(new WorldRuntimeIssueData { Severity = "Warning", Code = code, ScopeId = scopeId, Message = message });
		}
	}
}
