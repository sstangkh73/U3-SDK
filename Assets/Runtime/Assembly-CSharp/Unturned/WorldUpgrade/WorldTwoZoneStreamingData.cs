////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public sealed class WorldStreamingLoopSampleData
	{
		public int Loop;
		public int PeakActiveCellCount;
		public int PeakActiveEntityCount;
		public long PeakEstimatedResidentBytes;
	}

	[Serializable]
	public sealed class WorldTwoZoneStreamingEvidenceData
	{
		public int SchemaVersion = 1;
		public string WorldId;
		public string ManifestContentFingerprintSha256;
		public bool IsValid;
		public int ErrorCount;
		public int WarningCount;
		public int ZoneCount;
		public string SourceZoneKey;
		public string DestinationZoneKey;
		public WorldTransitionCorridorData Corridor;
		public float PreloadRadius;
		public float UnloadRadius;
		public float SimulationStepMeters;
		public int RoundTripCount;
		public int PositionSampleCount;
		public int ZoneResolutionChangeCount;
		public int CorridorEntryCount;
		public int SceneHandleChangeCount;
		public int TeleportCount;
		public int PreloadMissCount;
		public int LogicalGroundCoverageMissCount;
		public int PhysicalCollisionSampleCount;
		public int PhysicalCollisionMissCount;
		public bool SourceTerrainCreated;
		public bool DestinationTerrainCreated;
		public bool TransitionBridgeCreated;
		public int PeakActiveCellCount;
		public int PeakActiveEntityCount;
		public long PeakEstimatedResidentBytes;
		public int ActivationCount;
		public int DeactivationCount;
		public int PeakCaliforniaStaticObjectRecords;
		public int PeakLimestoneStaticObjectRecords;
		public float MaximumCoordinateRoundTripError;
		public int FinalActiveCellCount;
		public int FinalActiveEntityCount;
		public long FinalEstimatedResidentBytes;
		public bool LogicalMemoryPlateau;
		public bool NavigationDataAvailable;
		public List<WorldStreamingLoopSampleData> Loops = new List<WorldStreamingLoopSampleData>();
		public List<WorldRuntimeIssueData> Issues = new List<WorldRuntimeIssueData>();
	}
}
