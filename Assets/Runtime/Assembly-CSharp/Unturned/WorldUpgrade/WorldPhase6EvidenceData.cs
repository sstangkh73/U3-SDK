////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public sealed class WorldCellPerformanceBudgetData
	{
		public int MaximumEntityRecords = 8000;
		public int MaximumStaticObjectRecords = 5000;
		public long MaximumEstimatedResidentBytes = 8L * 1024L * 1024L;
	}

	[Serializable]
	public sealed class WorldCellPerformanceAuditData
	{
		public string ZoneId;
		public int CellCount;
		public int CellsOverEntityBudget;
		public int CellsOverStaticObjectBudget;
		public int MaximumEntityRecords;
		public int MaximumStaticObjectRecords;
		public long MaximumEstimatedResidentBytes;
		public bool RequiresHlod;
	}

	[Serializable]
	public sealed class WorldExpansionReadinessData
	{
		public string ZoneId;
		public string DisplayName;
		public bool InventoryGenerated;
		public bool DecodeValid;
		public bool EnvironmentDecoded;
		public int ActiveLandscapeTileCount;
		public int ObjectCount;
		public int SpawnPointCount;
		public int HierarchyItemCount;
		public int RoadJointCount;
		public bool WorldSchemaGenerated;
		public bool RuntimeParityPassed;
		public bool RuntimePerformancePassed;
		public bool RouteApproved;
		public bool RuntimeIncluded;
		public string GateStatus;
		public List<string> Blockers = new List<string>();
	}

	[Serializable]
	public sealed class WorldPhase6EvidenceData
	{
		public int SchemaVersion = 1;
		public bool IsValid;
		public int ErrorCount;
		public int WarningCount;
		public int EnvironmentProfileCount;
		public int ExactLightingDecodeCount;
		public int BlendPairCount;
		public int BlendSampleCount;
		public float MaximumAdjacentNormalizedDelta;
		public bool BlendEndpointsExact;
		public bool AbruptEnvironmentChangeDetected;
		public bool OxygenProfilesSourceDerived;
		public bool AmbienceAssetsSourceDerived;
		public bool HlodGenerated;
		public bool OcclusionGenerated;
		public bool RuntimeProfilerExecuted;
		public int ExpansionCandidateCount;
		public int ExpansionCandidatesReadyForRuntime;
		public WorldCellPerformanceBudgetData Budget = new WorldCellPerformanceBudgetData();
		public List<WorldCellPerformanceAuditData> ActiveZonePerformance = new List<WorldCellPerformanceAuditData>();
		public List<WorldExpansionReadinessData> ExpansionCandidates = new List<WorldExpansionReadinessData>();
		public List<WorldRuntimeIssueData> Issues = new List<WorldRuntimeIssueData>();
	}

	public static class WorldCellPerformanceAuditor
	{
		public static WorldCellPerformanceAuditData Audit(WorldZoneDefinitionData zone, WorldCellPerformanceBudgetData budget)
		{
			if (zone == null || budget == null)
				throw new ArgumentNullException(zone == null ? nameof(zone) : nameof(budget));
			WorldCellPerformanceAuditData result = new WorldCellPerformanceAuditData { ZoneId = zone.ZoneKey, CellCount = zone.Cells.Count };
			foreach (WorldCellIndexData cell in zone.Cells)
			{
				int staticObjects = 0;
				foreach (WorldRecordKindCountData count in cell.RecordCounts)
					if (string.Equals(count.Kind, "StaticObject", StringComparison.Ordinal))
						staticObjects = count.Count;
				long estimatedBytes = cell.EntityCount * 192L;
				result.MaximumEntityRecords = Math.Max(result.MaximumEntityRecords, cell.EntityCount);
				result.MaximumStaticObjectRecords = Math.Max(result.MaximumStaticObjectRecords, staticObjects);
				result.MaximumEstimatedResidentBytes = Math.Max(result.MaximumEstimatedResidentBytes, estimatedBytes);
				if (cell.EntityCount > budget.MaximumEntityRecords || estimatedBytes > budget.MaximumEstimatedResidentBytes)
					result.CellsOverEntityBudget++;
				if (staticObjects > budget.MaximumStaticObjectRecords)
					result.CellsOverStaticObjectBudget++;
			}
			result.RequiresHlod = result.CellsOverEntityBudget > 0 || result.CellsOverStaticObjectBudget > 0;
			return result;
		}
	}
}
