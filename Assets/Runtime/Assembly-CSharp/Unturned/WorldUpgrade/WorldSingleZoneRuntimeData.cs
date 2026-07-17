////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public class WorldRuntimeIssueData
	{
		public string Severity;
		public string Code;
		public string ScopeId;
		public string Message;
	}

	[Serializable]
	public class WorldCellCycleSampleData
	{
		public int Cycle;
		public int LoadedCellCount;
		public int LoadedEntityCount;
		public long EstimatedResidentBytes;
		public int AfterUnloadCellCount;
		public int AfterUnloadEntityCount;
		public long AfterUnloadEstimatedResidentBytes;
	}

	[Serializable]
	public class WorldSingleZoneRuntimeEvidenceData
	{
		public int SchemaVersion = 1;
		public string WorldId;
		public string ZoneKey;
		public string ZoneId;
		public string ActiveMapName;
		public string ManifestContentFingerprintSha256;
		public bool IsValid;
		public int ErrorCount;
		public int WarningCount;
		public int CellCount;
		public int EntityCount;
		public int CellCycleCount;
		public int PeakLoadedCellCount;
		public int PeakLoadedEntityCount;
		public long PeakEstimatedResidentBytes;
		public int FinalLoadedCellCount;
		public int FinalLoadedEntityCount;
		public long FinalEstimatedResidentBytes;
		public int LandscapeRecordCount;
		public int HeightmapSourceCount;
		public int DefaultHeightmapFallbackCount;
		public int BlackSplatFallbackPixelCount;
		public int LandscapeBaselineMissingCount;
		public long HeightSamplesCompared;
		public long HeightSampleMismatchCount;
		public long SplatSamplesCompared;
		public long SplatSampleMismatchCount;
		public long HoleSamplesCompared;
		public long HoleSampleMismatchCount;
		public int TerrainColliderMissingCount;
		public int StaticObjectRecordCount;
		public int BaselineObjectMissingCount;
		public int ObjectGuidMismatchCount;
		public int ObjectTransformUnavailableCount;
		public int ObjectTransformsCompared;
		public int ObjectPositionMismatchCount;
		public int ObjectRotationMismatchCount;
		public int ObjectScaleMismatchCount;
		public int BaselineObjectsWithCollider;
		public int BaselineObjectsWithRenderer;
		public int ObjectComponentSampleCount;
		public int AssetRegistryPeakObjectAssetCount;
		public int AssetRegistryPeakReferenceCount;
		public int AssetRegistryFinalObjectAssetCount;
		public int AssetRegistryFinalReferenceCount;
		public string SmokeCellId;
		public bool SmokeTerrainCreated;
		public bool SmokeTerrainColliderCreated;
		public bool SmokeObjectCreated;
		public int SmokeObjectColliderCount;
		public int SmokeObjectRendererCount;
		public bool SmokeObjectsDestroyed;
		public bool SmokeTerrainDestroyed;
		public List<WorldCellCycleSampleData> CellCycles = new List<WorldCellCycleSampleData>();
		public List<WorldRuntimeIssueData> Issues = new List<WorldRuntimeIssueData>();
	}
}
