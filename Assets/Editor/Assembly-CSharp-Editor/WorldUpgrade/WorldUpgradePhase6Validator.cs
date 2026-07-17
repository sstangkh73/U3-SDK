////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using Newtonsoft.Json;
using SDG.Unturned.WorldUpgrade;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public static class WorldUpgradePhase6Validator
	{
		private const string WorldKey = "u3-connected-world";
		private const int BlendSteps = 256;
		private static readonly string[] ExpansionZoneIds = { "pei", "washington", "yukon", "russia", "germany" };

		public static void GenerateEvidence()
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string schemaRoot = Path.Combine(projectRoot, "Builds", "WorldUpgrade", "WorldSchemas", WorldKey);
			RawMapSourceCatalogData catalog = ReadJson<RawMapSourceCatalogData>(Path.Combine(projectRoot, "Builds", "WorldUpgrade", "WorldSourceCatalog.json"));
			WorldEnvironmentProfileSetData profileSet = DecodeProfiles(catalog);
			WorldPhase6EvidenceData evidence = Validate(projectRoot, schemaRoot, catalog, profileSet);
			File.WriteAllText(Path.Combine(projectRoot, "Builds", "WorldUpgrade", "EnvironmentProfiles.json"),
				JsonConvert.SerializeObject(profileSet, Formatting.Indented));
			File.WriteAllText(Path.Combine(schemaRoot, "phase-6-environment-expansion-evidence.json"),
				JsonConvert.SerializeObject(evidence, Formatting.Indented));
			Debug.LogFormat("World Upgrade Phase 6 validation complete: valid={0}, errors={1}, warnings={2}, profiles={3}, expansion-ready={4}",
				evidence.IsValid, evidence.ErrorCount, evidence.WarningCount, evidence.EnvironmentProfileCount, evidence.ExpansionCandidatesReadyForRuntime);
			if (!evidence.IsValid)
				throw new InvalidDataException("Phase 6 foundation validation failed with " + evidence.ErrorCount + " error(s).");
		}

		public static void GenerateFromCommandLine()
		{
			try { GenerateEvidence(); }
			catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); return; }
			EditorApplication.Exit(0);
		}

		private static WorldEnvironmentProfileSetData DecodeProfiles(RawMapSourceCatalogData catalog)
		{
			WorldEnvironmentProfileSetData set = new WorldEnvironmentProfileSetData();
			foreach (RawMapSourceEntryData map in catalog.Maps.Where(map => map.Enabled).OrderBy(map => map.ZoneId, StringComparer.Ordinal))
				set.Profiles.Add(WorldLightingProfileDecoder.Decode(map.ZoneId, map.DisplayName,
					Path.Combine(map.SourceRoot, "Environment", "Lighting.dat")));
			return set;
		}

		private static WorldPhase6EvidenceData Validate(string projectRoot, string schemaRoot, RawMapSourceCatalogData catalog,
			WorldEnvironmentProfileSetData profiles)
		{
			WorldPhase6EvidenceData evidence = new WorldPhase6EvidenceData
			{
				EnvironmentProfileCount = profiles.Profiles.Count,
				ExactLightingDecodeCount = profiles.Profiles.Count(profile => profile.SourceVersion == 12 && profile.SourceBytesConsumed == 268 && profile.SourceTrailingBytes == 0),
				OxygenProfilesSourceDerived = profiles.Profiles.All(profile => profile.OxygenSourceDerived),
				AmbienceAssetsSourceDerived = profiles.Profiles.All(profile => profile.AmbienceAssetSourceDerived),
			};

			ValidateBlending(profiles.Profiles, evidence);
			foreach (string zoneId in new[] { "california-2", "limestone" })
			{
				string zonePath = Path.Combine(schemaRoot, "zones", zoneId + ".zone-definition.json");
				evidence.ActiveZonePerformance.Add(WorldCellPerformanceAuditor.Audit(ReadJson<WorldZoneDefinitionData>(zonePath), evidence.Budget));
			}

			foreach (string zoneId in ExpansionZoneIds)
			{
				RawMapSourceEntryData map = catalog.Maps.Single(entry => entry.ZoneId == zoneId);
				string inventoryPath = Path.Combine(projectRoot, catalog.OutputDirectory, zoneId + ".raw-map-manifest.json");
				string summaryPath = Path.Combine(projectRoot, "Builds", "WorldUpgrade", "DecodedMapSummaries", zoneId + ".decoded-map-summary.json");
				RawMapDecodedSummaryData summary = ReadJson<RawMapDecodedSummaryData>(summaryPath);
				WorldExpansionReadinessData readiness = new WorldExpansionReadinessData
				{
					ZoneId = zoneId,
					DisplayName = map.DisplayName,
					InventoryGenerated = File.Exists(inventoryPath),
					DecodeValid = summary.IsValid,
					EnvironmentDecoded = profiles.Profiles.Any(profile => profile.ZoneId == zoneId),
					ActiveLandscapeTileCount = summary.Landscape.HierarchyTileCount,
					ObjectCount = summary.Objects.ObjectCount,
					SpawnPointCount = summary.SpawnFiles.Sum(spawn => spawn.SpawnPointCount),
					HierarchyItemCount = summary.Hierarchy.ItemCount,
					RoadJointCount = summary.Roads.JointCount,
					WorldSchemaGenerated = File.Exists(Path.Combine(schemaRoot, "zones", zoneId + ".zone-definition.json")),
					RuntimeParityPassed = false,
					RuntimePerformancePassed = false,
					RouteApproved = false,
					RuntimeIncluded = false,
					GateStatus = "BlockedPendingAuthoredRouteAndRuntimeValidation",
				};
				readiness.Blockers.Add("No authored world-layout offset or geographic transition route has been approved.");
				readiness.Blockers.Add("Single-zone runtime parity has not been executed for this map.");
				readiness.Blockers.Add("Target-hardware runtime profiling has not been executed for this map.");
				evidence.ExpansionCandidates.Add(readiness);
			}
			evidence.ExpansionCandidateCount = evidence.ExpansionCandidates.Count;
			evidence.ExpansionCandidatesReadyForRuntime = evidence.ExpansionCandidates.Count(candidate => candidate.RuntimeIncluded);

			if (evidence.EnvironmentProfileCount != catalog.Maps.Count(map => map.Enabled) || evidence.ExactLightingDecodeCount != evidence.EnvironmentProfileCount)
				AddError(evidence, "LightingDecodeIncomplete", WorldKey, "Every enabled source map must have an exact Lighting.dat v12 profile.");
			if (!evidence.BlendEndpointsExact || evidence.AbruptEnvironmentChangeDetected)
				AddError(evidence, "EnvironmentBlendDiscontinuity", WorldKey, "Environment blend endpoints or adjacent continuity validation failed.");
			if (evidence.ActiveZonePerformance.Any(audit => audit.RequiresHlod))
				AddWarning(evidence, "HlodRequired", WorldKey, "One or more active cells exceed record budgets; HLOD generation remains required before production.");
			AddWarning(evidence, "OxygenAndAmbienceNotDecoded", WorldKey,
				"Lighting/weather/water-level values are source-derived; oxygen rules and ambience asset graphs are not decoded yet.");
			AddWarning(evidence, "HlodOcclusionNotGenerated", WorldKey,
				"Per-cell budgets are audited, but HLOD and occlusion assets are not generated.");
			AddWarning(evidence, "RuntimeProfilerNotExecuted", WorldKey,
				"Budget checks use deterministic record estimates, not target-hardware CPU/GPU/memory captures.");
			AddWarning(evidence, "ExpansionGatesClosed", WorldKey,
				"Five official maps pass inventory/decode/environment readiness only; none is included without route, parity, and performance approval.");
			evidence.ErrorCount = evidence.Issues.Count(issue => issue.Severity == "Error");
			evidence.WarningCount = evidence.Issues.Count(issue => issue.Severity == "Warning");
			evidence.IsValid = evidence.ErrorCount == 0;
			return evidence;
		}

		private static void ValidateBlending(IList<WorldEnvironmentProfileData> profiles, WorldPhase6EvidenceData evidence)
		{
			evidence.BlendEndpointsExact = true;
			for (int sourceIndex = 0; sourceIndex < profiles.Count; ++sourceIndex)
			{
				for (int destinationIndex = sourceIndex + 1; destinationIndex < profiles.Count; ++destinationIndex)
				{
					evidence.BlendPairCount++;
					for (int timeIndex = 0; timeIndex < 4; ++timeIndex)
					{
						WorldBlendedEnvironmentStateData previous = null;
						for (int step = 0; step <= BlendSteps; ++step)
						{
							WorldBlendedEnvironmentStateData current = WorldEnvironmentBlender.Sample(profiles[sourceIndex], profiles[destinationIndex], timeIndex, step / (float) BlendSteps);
							evidence.BlendSampleCount++;
							if (step == 0 && !MatchesProfile(current, profiles[sourceIndex], timeIndex) ||
								step == BlendSteps && !MatchesProfile(current, profiles[destinationIndex], timeIndex))
								evidence.BlendEndpointsExact = false;
							if (previous != null)
								evidence.MaximumAdjacentNormalizedDelta = Math.Max(evidence.MaximumAdjacentNormalizedDelta, MaximumDelta(previous, current));
							previous = current;
						}
					}
				}
			}
			evidence.AbruptEnvironmentChangeDetected = evidence.MaximumAdjacentNormalizedDelta > 0.02f;
		}

		private static float MaximumDelta(WorldBlendedEnvironmentStateData a, WorldBlendedEnvironmentStateData b)
		{
			float maximum = Math.Abs(a.RainCapability - b.RainCapability);
			maximum = Math.Max(maximum, Math.Abs(a.SnowCapability - b.SnowCapability));
			for (int index = 0; index < a.Colors.Count; ++index)
				maximum = Math.Max(maximum, Math.Abs(a.Colors[index] - b.Colors[index]));
			for (int index = 0; index < a.Singles.Count; ++index)
				maximum = Math.Max(maximum, Math.Abs(a.Singles[index] - b.Singles[index]));
			return maximum;
		}

		private static bool MatchesProfile(WorldBlendedEnvironmentStateData state, WorldEnvironmentProfileData profile, int timeIndex)
		{
			if (state.Azimuth != profile.Azimuth || state.Bias != profile.Bias || state.Fade != profile.Fade ||
				state.Time != profile.Time || state.Moon != profile.Moon || state.SeaLevel != profile.SeaLevel ||
				state.SnowLevel != profile.SnowLevel || state.RainCapability != (profile.CanRain ? 1f : 0f) ||
				state.SnowCapability != (profile.CanSnow ? 1f : 0f) || state.RainFrequency != profile.RainFrequency ||
				state.RainDuration != profile.RainDuration || state.SnowFrequency != profile.SnowFrequency ||
				state.SnowDuration != profile.SnowDuration)
			{
				return false;
			}
			WorldEnvironmentTimeProfileData sample = profile.Times[timeIndex];
			for (int colorIndex = 0; colorIndex < sample.Colors.Count; ++colorIndex)
			{
				WorldEnvironmentColorData color = sample.Colors[colorIndex];
				int offset = colorIndex * 3;
				float expectedR = color.R / 255f;
				float expectedG = color.G / 255f;
				float expectedB = color.B / 255f;
				if (state.Colors[offset] != expectedR || state.Colors[offset + 1] != expectedG || state.Colors[offset + 2] != expectedB)
				{
					return false;
				}
			}
			for (int index = 0; index < sample.Singles.Count; ++index)
				if (state.Singles[index] != sample.Singles[index])
				{
					return false;
				}
			return true;
		}

		private static T ReadJson<T>(string path) => JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
		private static void AddError(WorldPhase6EvidenceData evidence, string code, string scopeId, string message) =>
			evidence.Issues.Add(new WorldRuntimeIssueData { Severity = "Error", Code = code, ScopeId = scopeId, Message = message });
		private static void AddWarning(WorldPhase6EvidenceData evidence, string code, string scopeId, string message) =>
			evidence.Issues.Add(new WorldRuntimeIssueData { Severity = "Warning", Code = code, ScopeId = scopeId, Message = message });
	}
}
