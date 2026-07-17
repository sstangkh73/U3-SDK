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
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public static class WorldUpgradePhase7Validator
	{
		private const string WorldKey = "u3-connected-world";
		private const int ClientCount = 64;
		private const int LoadCycles = 50;
		private const int ReplayAttemptsPerCommand = 5;

		public static void GenerateEvidence()
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string schemaRoot = Path.Combine(projectRoot, "Builds", "WorldUpgrade", "WorldSchemas", WorldKey);
			WorldManifestData manifest = ReadJson<WorldManifestData>(Path.Combine(schemaRoot, "world-manifest.json"));
			List<WorldZoneDefinitionData> zones = manifest.Zones.Select(zone =>
				ReadJson<WorldZoneDefinitionData>(Path.Combine(schemaRoot, zone.RelativePath.Replace('/', Path.DirectorySeparatorChar)))).ToList();
			WorldPhase7EvidenceData evidence = Validate(projectRoot, manifest, zones);
			WorldProductionPackageManifestData package = BuildPackageManifest(projectRoot, manifest);
			evidence.PackageManifestGenerated = package.AllArtifactsPresent;
			File.WriteAllText(Path.Combine(projectRoot, "Builds", "WorldUpgrade", "ProductionPackageManifest.json"),
				JsonConvert.SerializeObject(package, Formatting.Indented));
			FinalizeEvidence(evidence);
			File.WriteAllText(Path.Combine(schemaRoot, "phase-7-multiplayer-production-evidence.json"),
				JsonConvert.SerializeObject(evidence, Formatting.Indented));
			Debug.LogFormat("World Upgrade Phase 7 validation complete: valid={0}, errors={1}, warnings={2}, clients={3}, snapshots={4}",
				evidence.IsValid, evidence.ErrorCount, evidence.WarningCount, evidence.SimulatedClientCount, evidence.ReplicationSnapshotCount);
			if (!evidence.IsValid)
				throw new InvalidDataException("Phase 7 authority foundation validation failed with " + evidence.ErrorCount + " error(s).");
		}

		public static void GenerateFromCommandLine()
		{
			try { GenerateEvidence(); }
			catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); return; }
			EditorApplication.Exit(0);
		}

		private static WorldPhase7EvidenceData Validate(string projectRoot, WorldManifestData manifest, List<WorldZoneDefinitionData> zones)
		{
			WorldPersistenceStore store = new WorldPersistenceStore();
			WorldServerAuthority authority = new WorldServerAuthority("phase7-validation-server-secret", store, manifest, zones, 128f);
			WorldPhase7EvidenceData evidence = new WorldPhase7EvidenceData
			{
				WorldId = manifest.WorldId,
				DeterministicAuthoritySimulationExecuted = true,
				SimulatedClientCount = ClientCount,
				LoadSimulationCycleCount = LoadCycles,
			};
			List<WorldCellIndexData> cells = zones.SelectMany(zone => zone.Cells)
				.OrderBy(cell => cell.CellId, StringComparer.Ordinal).Take(ClientCount).ToList();
			Dictionary<string, string> tokens = new Dictionary<string, string>(StringComparer.Ordinal);
			Dictionary<string, Vector3> originalPositions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
			Dictionary<string, string> seededEntities = new Dictionary<string, string>(StringComparer.Ordinal);

			for (int index = 0; index < ClientCount; ++index)
			{
				string clientId = "client-" + index.ToString("D3");
				Vector3 position = Center(cells[index]);
				WorldConnectResultData connected = authority.Connect(clientId, null, position);
				if (!connected.Accepted)
					evidence.StateConflictCount++;
				tokens[clientId] = connected.Session?.ReconnectToken;
				originalPositions[clientId] = position;
				WorldDynamicEntityStateData entity = new WorldDynamicEntityStateData
				{
					EntityId = WorldRuntimeIdentityUtility.Create("seed", "phase7|" + clientId),
					Kind = "DroppedItem",
					SourceEntityId = "phase7-seed|" + clientId,
					ZoneId = connected.Session.ZoneId,
					OwnerCellId = connected.Session.OwnerCellId,
					WorldPosition = new RawMapVector3Data(position.x, position.y, position.z),
					Revision = 1,
				};
				if (!authority.SeedServerEntity(entity))
					evidence.StateConflictCount++;
				seededEntities[clientId] = entity.EntityId;
			}
			evidence.InitialLiveItemCount = store.Entities.Count(entity => !entity.IsRemoved);

			foreach (string clientId in tokens.Keys.OrderBy(id => id, StringComparer.Ordinal))
			{
				WorldReplicationSnapshotData initial = authority.GetReplicationSnapshot(clientId);
				evidence.ReplicationSnapshotCount++;
				ValidateScope(initial, evidence);
				if (!initial.Entities.Any(entity => entity.EntityId == seededEntities[clientId]))
					evidence.StateConflictCount++;

				string pickupRequestId = "pickup|" + clientId;
				WorldAuthorityCommandResultData pickup = authority.ProcessClientCommand(new WorldClientCommandData
				{
					RequestId = pickupRequestId, ClientId = clientId, Command = EWorldClientCommand.PickupEntity,
					EntityId = seededEntities[clientId], ExpectedEntityRevision = 1,
				});
				if (pickup.Accepted) evidence.PickupCommitCount++; else evidence.StateConflictCount++;
				for (int replay = 0; replay < ReplayAttemptsPerCommand; ++replay)
				{
					evidence.ReplayAttemptCount++;
					WorldAuthorityCommandResultData result = authority.ProcessClientCommand(new WorldClientCommandData
					{
						RequestId = pickupRequestId, ClientId = clientId, Command = EWorldClientCommand.PickupEntity,
						EntityId = seededEntities[clientId], ExpectedEntityRevision = 1,
					});
					if (!result.WasReplay || !result.Accepted || result.EntityRevision != pickup.EntityRevision)
						evidence.ReplayMutationCount++;
				}

				WorldAuthorityCommandResultData stale = authority.ProcessClientCommand(new WorldClientCommandData
				{
					RequestId = "stale|" + clientId, ClientId = clientId, Command = EWorldClientCommand.PickupEntity,
					EntityId = seededEntities[clientId], ExpectedEntityRevision = 1,
				});
				if (!stale.Accepted && stale.Code == "EntityAlreadyRemoved") evidence.StaleRevisionRejectionCount++;

				string dropRequestId = "drop|" + clientId;
				WorldAuthorityCommandResultData drop = authority.ProcessClientCommand(new WorldClientCommandData
				{
					RequestId = dropRequestId, ClientId = clientId, Command = EWorldClientCommand.DropInventoryItem,
					InventoryItemId = seededEntities[clientId],
				});
				if (drop.Accepted) evidence.DropCommitCount++; else evidence.StateConflictCount++;
				for (int replay = 0; replay < ReplayAttemptsPerCommand; ++replay)
				{
					evidence.ReplayAttemptCount++;
					WorldAuthorityCommandResultData result = authority.ProcessClientCommand(new WorldClientCommandData
					{
						RequestId = dropRequestId, ClientId = clientId, Command = EWorldClientCommand.DropInventoryItem,
						InventoryItemId = seededEntities[clientId],
					});
					if (!result.WasReplay || !result.Accepted || result.EntityId != drop.EntityId)
						evidence.ReplayMutationCount++;
				}

				WorldAuthorityCommandResultData travel = authority.ProcessClientCommand(new WorldClientCommandData
				{
					RequestId = "travel|" + clientId, ClientId = clientId, Command = EWorldClientCommand.ClientTravelCommit,
				});
				if (!travel.Accepted && travel.Code == "ClientTravelAuthorityRejected") evidence.UnauthorizedTravelRejectionCount++;
				WorldAuthorityCommandResultData save = authority.ProcessClientCommand(new WorldClientCommandData
				{
					RequestId = "save|" + clientId, ClientId = clientId, Command = EWorldClientCommand.ClientSaveImport,
				});
				if (!save.Accepted && save.Code == "ClientSaveAuthorityRejected") evidence.UnauthorizedSaveRejectionCount++;
			}

			WorldCellIndexData californiaTarget = zones.Single(zone => zone.ZoneKey == "california-2").Cells.OrderBy(cell => cell.CellId, StringComparer.Ordinal).First();
			WorldCellIndexData limestoneTarget = zones.Single(zone => zone.ZoneKey == "limestone").Cells.OrderBy(cell => cell.CellId, StringComparer.Ordinal).First();
			for (int index = 0; index < 16; ++index)
			{
				string clientId = "client-" + index.ToString("D3");
				WorldReplicationSnapshotData before = authority.GetReplicationSnapshot(clientId);
				WorldCellIndexData destination = before.ZoneId == zones.Single(zone => zone.ZoneKey == "california-2").ZoneId ? limestoneTarget : californiaTarget;
				if (authority.ServerUpdatePlayerPosition(clientId, Center(destination)))
				{
					WorldReplicationSnapshotData after = authority.GetReplicationSnapshot(clientId);
					if (after.ZoneId != before.ZoneId && after.OwnerCellId != before.OwnerCellId)
						evidence.ServerVerifiedMigrationCount++;
				}
			}

			for (int cycle = 0; cycle < LoadCycles; ++cycle)
			{
				foreach (string clientId in tokens.Keys.OrderBy(id => id, StringComparer.Ordinal))
				{
					WorldReplicationSnapshotData snapshot = authority.GetReplicationSnapshot(clientId);
					evidence.ReplicationSnapshotCount++;
					ValidateScope(snapshot, evidence);
				}
			}

			evidence.ReconnectServerSnapshotPassed = true;
			foreach (string clientId in tokens.Keys.OrderBy(id => id, StringComparer.Ordinal))
			{
				WorldReplicationSnapshotData before = authority.GetReplicationSnapshot(clientId);
				authority.Disconnect(clientId);
				WorldConnectResultData rejected = authority.Connect(clientId, "wrong-token", originalPositions[clientId]);
				if (!rejected.Accepted && rejected.Code == "ReconnectTokenRejected") evidence.WrongReconnectTokenRejectionCount++;
				WorldConnectResultData reconnected = authority.Connect(clientId, tokens[clientId], originalPositions[clientId]);
				if (reconnected.Accepted && reconnected.WasReconnect) evidence.SuccessfulReconnectCount++; else evidence.ReconnectServerSnapshotPassed = false;
				WorldReplicationSnapshotData after = authority.GetReplicationSnapshot(clientId);
				evidence.ReplicationSnapshotCount++;
				ValidateScope(after, evidence);
				if (after.ZoneId != before.ZoneId || after.OwnerCellId != before.OwnerCellId ||
					!after.InventoryItemIds.SequenceEqual(before.InventoryItemIds) ||
					!after.Entities.Select(entity => entity.EntityId).SequenceEqual(before.Entities.Select(entity => entity.EntityId)))
					evidence.ReconnectServerSnapshotPassed = false;
			}

			evidence.SimulatedZoneCount = authority.Sessions.Select(session => session.ZoneId).Distinct(StringComparer.Ordinal).Count();
			evidence.SimulatedDistinctCellCount = authority.Sessions.Select(session => session.OwnerCellId).Distinct(StringComparer.Ordinal).Count();
			evidence.FinalLiveItemCount = store.Entities.Count(entity => !entity.IsRemoved);
			evidence.FinalInventoryItemCount = authority.Sessions.Sum(session => session.InventoryItemIds.Count);
			evidence.ItemConservationPassed = evidence.FinalLiveItemCount + evidence.FinalInventoryItemCount == evidence.InitialLiveItemCount;
			evidence.ClientTravelAuthorityBlocked = evidence.UnauthorizedTravelRejectionCount == ClientCount;
			evidence.ClientSaveAuthorityBlocked = evidence.UnauthorizedSaveRejectionCount == ClientCount;
			WorldPersistenceSnapshotData finalSnapshot = store.CreateSnapshot(manifest.WorldId, 7);
			evidence.FinalServerSnapshotFingerprintSha256 = finalSnapshot.ContentSha256;
			try
			{
				new WorldCompatibilityPolicyData().Validate(1, manifest, finalSnapshot);
				evidence.CompatibilityContractPassed = true;
			}
			catch { evidence.CompatibilityContractPassed = false; }
			evidence.LicenseFilePresent = File.Exists(Path.Combine(projectRoot, "LICENSE.txt"));
			return evidence;
		}

		private static void ValidateScope(WorldReplicationSnapshotData snapshot, WorldPhase7EvidenceData evidence)
		{
			HashSet<string> scope = new HashSet<string>(snapshot.ScopedCellIds, StringComparer.Ordinal);
			evidence.ReplicationScopeLeakCount += snapshot.Entities.Count(entity => !scope.Contains(entity.OwnerCellId));
		}

		private static WorldProductionPackageManifestData BuildPackageManifest(string projectRoot, WorldManifestData manifest)
		{
			string[] paths =
			{
				"Builds/WorldUpgrade/WorldSchemas/u3-connected-world/world-manifest.json",
				"Builds/WorldUpgrade/WorldSchemas/u3-connected-world/generation-evidence.json",
				"Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-3-single-zone-runtime-evidence.json",
				"Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-4-two-zone-streaming-evidence.json",
				"Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-5-population-persistence-evidence.json",
				"Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-6-environment-expansion-evidence.json",
				"Builds/WorldUpgrade/WorldSimulationPolicy.json",
				"Builds/WorldUpgrade/EnvironmentProfiles.json",
			};
			WorldProductionPackageManifestData package = new WorldProductionPackageManifestData { WorldId = manifest.WorldId, AllArtifactsPresent = true };
			foreach (string relativePath in paths)
			{
				string fullPath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
				if (!File.Exists(fullPath)) { package.AllArtifactsPresent = false; continue; }
				byte[] bytes = File.ReadAllBytes(fullPath);
				package.Artifacts.Add(new WorldPackageArtifactData { RelativePath = relativePath, SizeInBytes = bytes.LongLength, ContentSha256 = Sha256(bytes) });
			}
			package.ManifestFingerprintSha256 = WorldRuntimeIdentityUtility.Sha256(string.Join("\n", package.Artifacts
				.OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal).Select(artifact => artifact.RelativePath + "|" + artifact.SizeInBytes + "|" + artifact.ContentSha256)));
			return package;
		}

		private static void FinalizeEvidence(WorldPhase7EvidenceData evidence)
		{
			if (evidence.SimulatedClientCount != ClientCount || evidence.SimulatedZoneCount < 2 || evidence.SimulatedDistinctCellCount < 2)
				AddError(evidence, "InsufficientAuthorityCoverage", evidence.WorldId, "Authority simulation did not keep clients across multiple cells and zones.");
			if (evidence.StateConflictCount != 0 || evidence.ReplicationScopeLeakCount != 0)
				AddError(evidence, "AuthorityStateConflict", evidence.WorldId, "Server state conflicts or replication scope leaks were detected.");
			if (!evidence.ItemConservationPassed || evidence.ReplayMutationCount != 0 || evidence.PickupCommitCount != ClientCount || evidence.DropCommitCount != ClientCount)
				AddError(evidence, "AntiDuplicationFailed", evidence.WorldId, "Replay-safe item conservation failed.");
			if (!evidence.ReconnectServerSnapshotPassed || evidence.SuccessfulReconnectCount != ClientCount || evidence.WrongReconnectTokenRejectionCount != ClientCount)
				AddError(evidence, "ReconnectAuthorityFailed", evidence.WorldId, "Reconnect did not preserve server-authoritative state or reject invalid tokens.");
			if (!evidence.ClientTravelAuthorityBlocked || !evidence.ClientSaveAuthorityBlocked)
				AddError(evidence, "ClientAuthorityBypass", evidence.WorldId, "Client travel/save authority bypass was not rejected.");
			if (!evidence.CompatibilityContractPassed || !evidence.PackageManifestGenerated)
				AddError(evidence, "ProductionContractMissing", evidence.WorldId, "Compatibility or package-input manifest contract failed.");
			AddWarning(evidence, "DedicatedServerNotExecuted", evidence.WorldId,
				"The 64-client load is an in-process deterministic authority simulation, not a dedicated server process or network transport test.");
			AddWarning(evidence, "PackagingBuildNotExecuted", evidence.WorldId,
				"A hashed input manifest is generated, but no distributable client/server package was built or installed.");
			AddWarning(evidence, "LicenseReviewPending", evidence.WorldId,
				"The repository license file is present, but map/workshop redistribution and release licensing require human approval.");
			AddWarning(evidence, "UpdateMigrationNotExecuted", evidence.WorldId,
				"Current schema compatibility is checked; migration from an older shipped build has not been exercised.");
			evidence.ErrorCount = evidence.Issues.Count(issue => issue.Severity == "Error");
			evidence.WarningCount = evidence.Issues.Count(issue => issue.Severity == "Warning");
			evidence.IsValid = evidence.ErrorCount == 0;
		}

		private static Vector3 Center(WorldCellIndexData cell) => new Vector3((cell.WorldBounds.Min.X + cell.WorldBounds.Max.X) * 0.5f,
			20f, (cell.WorldBounds.Min.Z + cell.WorldBounds.Max.Z) * 0.5f);
		private static T ReadJson<T>(string path) => JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
		private static string Sha256(byte[] bytes) { using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant(); }
		private static void AddError(WorldPhase7EvidenceData evidence, string code, string scopeId, string message) => evidence.Issues.Add(new WorldRuntimeIssueData { Severity = "Error", Code = code, ScopeId = scopeId, Message = message });
		private static void AddWarning(WorldPhase7EvidenceData evidence, string code, string scopeId, string message) => evidence.Issues.Add(new WorldRuntimeIssueData { Severity = "Warning", Code = code, ScopeId = scopeId, Message = message });
	}
}
