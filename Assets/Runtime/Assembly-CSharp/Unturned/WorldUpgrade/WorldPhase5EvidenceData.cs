////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public sealed class WorldPopulationCycleSampleData
	{
		public int Cycle;
		public int ActivatedEntityCount;
		public int PersistentEntityCount;
	}

	[Serializable]
	public sealed class WorldPhase5EvidenceData
	{
		public int SchemaVersion = 1;
		public string WorldId;
		public string PolicyId;
		public bool IsValid;
		public int ErrorCount;
		public int WarningCount;
		public int PopulationCycleCount;
		public int PopulationCreatedCount;
		public int PersistentPopulationCount;
		public int DuplicateEntityIdCount;
		public int PopulationGrowthAfterFirstCycle;
		public int DroppedItemCount;
		public int ZombieCount;
		public int AnimalCount;
		public int VehicleCount;
		public int BuildableCount;
		public bool CrossZoneOwnerMigrationPassed;
		public bool SaveReloadCountPassed;
		public bool SaveReloadOwnerPassed;
		public bool JournalRecoveryApplied;
		public bool JournalRecoveryCountPassed;
		public string FinalSnapshotFingerprintSha256;
		public int NavBorderLinkCount;
		public int CrossZoneNavBorderLinkCount;
		public int RuntimeBakedNavLinkCount;
		public List<WorldPopulationCycleSampleData> Cycles = new List<WorldPopulationCycleSampleData>();
		public List<WorldRuntimeIssueData> Issues = new List<WorldRuntimeIssueData>();
	}
}
