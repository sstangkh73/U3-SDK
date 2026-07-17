////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public sealed class WorldPackageArtifactData
	{
		public string RelativePath;
		public long SizeInBytes;
		public string ContentSha256;
	}

	[Serializable]
	public sealed class WorldProductionPackageManifestData
	{
		public int SchemaVersion = 1;
		public string WorldId;
		public int ServerProtocolVersion = 1;
		public string ManifestFingerprintSha256;
		public bool AllArtifactsPresent;
		public List<WorldPackageArtifactData> Artifacts = new List<WorldPackageArtifactData>();
	}

	[Serializable]
	public sealed class WorldPhase7EvidenceData
	{
		public int SchemaVersion = 1;
		public string WorldId;
		public bool IsValid;
		public int ErrorCount;
		public int WarningCount;
		public bool DeterministicAuthoritySimulationExecuted;
		public bool DedicatedServerProcessExecuted;
		public int SimulatedClientCount;
		public int SimulatedZoneCount;
		public int SimulatedDistinctCellCount;
		public int LoadSimulationCycleCount;
		public int ReplicationSnapshotCount;
		public int ReplicationScopeLeakCount;
		public int StateConflictCount;
		public int InitialLiveItemCount;
		public int FinalLiveItemCount;
		public int FinalInventoryItemCount;
		public int PickupCommitCount;
		public int DropCommitCount;
		public int ReplayAttemptCount;
		public int ReplayMutationCount;
		public int UnauthorizedTravelRejectionCount;
		public int UnauthorizedSaveRejectionCount;
		public int WrongReconnectTokenRejectionCount;
		public int SuccessfulReconnectCount;
		public int ServerVerifiedMigrationCount;
		public int StaleRevisionRejectionCount;
		public bool ItemConservationPassed;
		public bool ReconnectServerSnapshotPassed;
		public bool ClientTravelAuthorityBlocked;
		public bool ClientSaveAuthorityBlocked;
		public string FinalServerSnapshotFingerprintSha256;
		public bool CompatibilityContractPassed;
		public bool PackageManifestGenerated;
		public bool PackagingBuildExecuted;
		public bool LicenseFilePresent;
		public bool LicenseReviewApproved;
		public bool UpdateMigrationTestExecuted;
		public List<WorldRuntimeIssueData> Issues = new List<WorldRuntimeIssueData>();
	}
}
