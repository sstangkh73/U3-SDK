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
	public static class WorldUpgradePhase5Validator
	{
		private const string WorldKey = "u3-connected-world";
		private const int PopulationCycles = 12;

		public static void GenerateEvidence()
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string schemaRoot = Path.Combine(projectRoot, "Builds", "WorldUpgrade", "WorldSchemas", WorldKey);
			WorldSimulationPolicyData policy = ReadJson<WorldSimulationPolicyData>(Path.Combine(projectRoot,
				"Builds", "WorldUpgrade", "WorldSimulationPolicy.json"));
			policy.Validate();
			using (WorldCellBundleRepository california = new WorldCellBundleRepository(schemaRoot, "california-2"))
			using (WorldCellBundleRepository limestone = new WorldCellBundleRepository(schemaRoot, "limestone"))
			{
				WorldPhase5EvidenceData evidence = Validate(projectRoot, policy, california, limestone);
				File.WriteAllText(Path.Combine(schemaRoot, "phase-5-population-persistence-evidence.json"),
					JsonConvert.SerializeObject(evidence, Formatting.Indented));
				Debug.LogFormat("World Upgrade Phase 5 validation complete: valid={0}, errors={1}, warnings={2}, population={3}",
					evidence.IsValid, evidence.ErrorCount, evidence.WarningCount, evidence.PersistentPopulationCount);
				if (!evidence.IsValid)
					throw new InvalidDataException("Phase 5 validation failed with " + evidence.ErrorCount + " error(s).");
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

		private static WorldPhase5EvidenceData Validate(string projectRoot, WorldSimulationPolicyData policy,
			WorldCellBundleRepository california, WorldCellBundleRepository limestone)
		{
			WorldManifestData manifest = california.Manifest;
			WorldPersistenceStore store = new WorldPersistenceStore();
			WorldPopulationService population = new WorldPopulationService(policy, store);
			WorldPhase5EvidenceData evidence = new WorldPhase5EvidenceData
			{
				WorldId = manifest.WorldId,
				PolicyId = policy.PolicyId,
				PopulationCycleCount = PopulationCycles,
			};
			WorldCellBundleRepository[] repositories = { california, limestone };
			int firstPersistentCount = 0;
			for (int cycle = 1; cycle <= PopulationCycles; ++cycle)
			{
				WorldPopulationCycleSampleData sample = new WorldPopulationCycleSampleData { Cycle = cycle };
				foreach (WorldCellBundleRepository repository in repositories)
				{
					foreach (WorldCellIndexData index in repository.EnumerateCellIndices())
					{
						WorldCellData cell = repository.LoadCell(index.CellId);
						sample.ActivatedEntityCount += population.ActivateCell(cell);
						population.DeactivateCell(cell.CellId);
						repository.UnloadCell(cell.CellId);
					}
				}
				sample.PersistentEntityCount = store.Count;
				evidence.Cycles.Add(sample);
				if (cycle == 1)
					firstPersistentCount = store.Count;
			}
			evidence.PopulationCreatedCount = population.CreatedEntityCount;
			evidence.PersistentPopulationCount = firstPersistentCount;
			evidence.PopulationGrowthAfterFirstCycle = store.Count - firstPersistentCount;
			evidence.DuplicateEntityIdCount = store.Entities.Count() - store.Entities.Select(entity => entity.EntityId).Distinct(StringComparer.Ordinal).Count();

			WorldEntityOwnershipResolver ownership = new WorldEntityOwnershipResolver(manifest,
				new[] { california.Zone, limestone.Zone });
			WorldDynamicEntityStateData droppedItem = CreateManualEntity("phase5-dropped-item", "DroppedItem");
			ownership.Move(droppedItem, new Vector3(3584f, 20f, 512f));
			store.Add(droppedItem);
			WorldDynamicEntityStateData buildable = CreateManualEntity("phase5-buildable", "Buildable");
			ownership.Move(buildable, new Vector3(3584f, 20f, 512f));
			store.Add(buildable);
			string californiaOwner = buildable.OwnerCellId;
			ownership.Move(buildable, new Vector3(5632f, 20f, 512f));
			store.Upsert(buildable);
			evidence.CrossZoneOwnerMigrationPassed = !string.Equals(californiaOwner, buildable.OwnerCellId, StringComparison.Ordinal) &&
				string.Equals(buildable.ZoneId, limestone.Zone.ZoneId, StringComparison.Ordinal);

			string validationDirectory = Path.Combine(projectRoot, "Library", "WorldUpgrade", "Phase5Validation");
			string savePath = Path.Combine(validationDirectory, "world-save.json");
			DeleteValidationFiles(savePath);
			WorldPersistenceTransaction transaction = new WorldPersistenceTransaction(savePath);
			WorldPersistenceSnapshotData firstSnapshot = store.CreateSnapshot(manifest.WorldId, 1);
			transaction.Save(firstSnapshot);
			WorldPersistenceStore reloaded = new WorldPersistenceStore();
			reloaded.Restore(transaction.Load());
			evidence.SaveReloadCountPassed = reloaded.Count == store.Count;
			evidence.SaveReloadOwnerPassed = reloaded.TryGet(buildable.EntityId, out WorldDynamicEntityStateData reloadedBuildable) &&
				string.Equals(reloadedBuildable.OwnerCellId, buildable.OwnerCellId, StringComparison.Ordinal) &&
				string.Equals(reloadedBuildable.ZoneId, buildable.ZoneId, StringComparison.Ordinal);

			WorldDynamicEntityStateData recoveryEntity = CreateManualEntity("phase5-recovery-item", "DroppedItem");
			ownership.Move(recoveryEntity, new Vector3(5500f, 20f, 512f));
			store.Add(recoveryEntity);
			WorldPersistenceSnapshotData preparedSnapshot = store.CreateSnapshot(manifest.WorldId, 2);
			transaction.Prepare(preparedSnapshot);
			WorldPersistenceTransaction recovery = new WorldPersistenceTransaction(savePath);
			evidence.JournalRecoveryApplied = recovery.Recover();
			WorldPersistenceStore recovered = new WorldPersistenceStore();
			WorldPersistenceSnapshotData finalSnapshot = recovery.Load();
			recovered.Restore(finalSnapshot);
			evidence.JournalRecoveryCountPassed = recovered.Count == store.Count;
			evidence.FinalSnapshotFingerprintSha256 = finalSnapshot.ContentSha256;

			evidence.DroppedItemCount = recovered.Entities.Count(entity => entity.Kind == "DroppedItem");
			evidence.ZombieCount = recovered.Entities.Count(entity => entity.Kind == "Zombie");
			evidence.AnimalCount = recovered.Entities.Count(entity => entity.Kind == "Animal");
			evidence.VehicleCount = recovered.Entities.Count(entity => entity.Kind == "Vehicle");
			evidence.BuildableCount = recovered.Entities.Count(entity => entity.Kind == "Buildable");

			WorldZoneResolver zoneResolver = new WorldZoneResolver(manifest);
			WorldTransitionSafetyService safety = new WorldTransitionSafetyService(zoneResolver.FindZone("california-2"),
				zoneResolver.FindZone("limestone"), 256f);
			List<WorldNavBorderLinkData> navLinks = WorldNavBorderLinkBuilder.Build(new[] { california.Zone, limestone.Zone }, safety.Corridor);
			evidence.NavBorderLinkCount = navLinks.Count;
			evidence.CrossZoneNavBorderLinkCount = navLinks.Count(link => link.IsCrossZone);
			evidence.RuntimeBakedNavLinkCount = navLinks.Count(link => link.IsRuntimeBaked);
			AddWarning(evidence, "LogicalNavLinksOnly", manifest.WorldId,
				"Nav border links are deterministic ownership contracts only; no runtime NavMesh links are baked yet.");
			AddWarning(evidence, "PopulationIsDataOnly", manifest.WorldId,
				"Population validation creates persistent entity states, not live zombie/animal/vehicle GameObjects.");

			if (evidence.PopulationCreatedCount != firstPersistentCount || evidence.PopulationGrowthAfterFirstCycle != 0 || evidence.DuplicateEntityIdCount != 0)
				AddError(evidence, "PopulationDuplication", manifest.WorldId, "Population IDs were not stable across cell activation cycles.");
			if (!evidence.CrossZoneOwnerMigrationPassed)
				AddError(evidence, "OwnerMigrationFailed", buildable.EntityId, "Dynamic entity owner cell did not migrate across zones.");
			if (!evidence.SaveReloadCountPassed || !evidence.SaveReloadOwnerPassed)
				AddError(evidence, "SaveReloadMismatch", manifest.WorldId, "Save/reload changed entity count or migrated ownership.");
			if (!evidence.JournalRecoveryApplied || !evidence.JournalRecoveryCountPassed)
				AddError(evidence, "JournalRecoveryFailed", manifest.WorldId, "Prepared persistence transaction was not recovered losslessly.");
			if (evidence.DroppedItemCount == 0 || evidence.ZombieCount == 0 || evidence.AnimalCount == 0 || evidence.VehicleCount == 0 || evidence.BuildableCount == 0)
				AddError(evidence, "PopulationKindMissing", manifest.WorldId, "One or more required dynamic entity kinds were not represented.");
			if (evidence.CrossZoneNavBorderLinkCount != 1)
				AddError(evidence, "CrossZoneNavContractMissing", manifest.WorldId, "Expected exactly one deterministic cross-zone nav border contract.");
			evidence.ErrorCount = evidence.Issues.Count(issue => issue.Severity == "Error");
			evidence.WarningCount = evidence.Issues.Count(issue => issue.Severity == "Warning");
			evidence.IsValid = evidence.ErrorCount == 0;
			return evidence;
		}

		private static WorldDynamicEntityStateData CreateManualEntity(string key, string kind)
		{
			return new WorldDynamicEntityStateData
			{
				EntityId = WorldRuntimeIdentityUtility.Create("dyn", key),
				Kind = kind,
				SourceEntityId = "manual|" + key,
			};
		}

		private static void DeleteValidationFiles(string targetPath)
		{
			foreach (string path in new[] { targetPath, targetPath + ".pending", targetPath + ".journal", targetPath + ".backup" })
				if (File.Exists(path))
					File.Delete(path);
		}

		private static T ReadJson<T>(string path)
		{
			return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
		}

		private static void AddError(WorldPhase5EvidenceData evidence, string code, string scopeId, string message)
		{
			evidence.Issues.Add(new WorldRuntimeIssueData { Severity = "Error", Code = code, ScopeId = scopeId, Message = message });
		}

		private static void AddWarning(WorldPhase5EvidenceData evidence, string code, string scopeId, string message)
		{
			evidence.Issues.Add(new WorldRuntimeIssueData { Severity = "Warning", Code = code, ScopeId = scopeId, Message = message });
		}
	}
}
