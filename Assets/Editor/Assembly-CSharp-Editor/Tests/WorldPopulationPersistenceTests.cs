////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using NUnit.Framework;
using SDG.Unturned.WorldUpgrade;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SDG.Unturned.Tests
{
	public class WorldPopulationPersistenceTests
	{
		private string tempRoot;

		[SetUp]
		public void SetUp()
		{
			tempRoot = Path.Combine(Path.GetTempPath(), "u3-world-persistence-tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempRoot);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(tempRoot))
				Directory.Delete(tempRoot, true);
		}

		[Test]
		public void SimulationPolicyRejectsInvalidValues()
		{
			WorldSimulationPolicyData policy = new WorldSimulationPolicyData();
			policy.Population.MaximumPersistentPopulation = 0;
			Assert.Throws<InvalidOperationException>(() => policy.Validate());
		}

		[Test]
		public void PopulationActivationIsDeterministicAndIdempotent()
		{
			WorldSimulationPolicyData policy = new WorldSimulationPolicyData();
			WorldPersistenceStore store = new WorldPersistenceStore();
			WorldPopulationService population = new WorldPopulationService(policy, store);
			WorldCellData cell = new WorldCellData { WorldId = "world", ZoneId = "zone", CellId = "cell" };
			cell.Entities.Add(Seed("item", "ItemSpawn"));
			cell.Entities.Add(Seed("zombie", "ZombieSpawn"));
			cell.Entities.Add(Seed("animal", "AnimalSpawn"));
			cell.Entities.Add(Seed("vehicle", "VehicleSpawn"));

			Assert.AreEqual(4, population.ActivateCell(cell));
			Assert.AreEqual(4, population.ActivateCell(cell));
			population.DeactivateCell(cell.CellId);
			Assert.AreEqual(4, population.ActivateCell(cell));
			Assert.AreEqual(4, store.Count);
			Assert.AreEqual(4, population.CreatedEntityCount);
		}

		[Test]
		public void PreparedTransactionRecoversWithoutLoss()
		{
			WorldPersistenceStore store = new WorldPersistenceStore();
			store.Add(Entity("one", "cell-west", "zone-west"));
			string path = Path.Combine(tempRoot, "save.json");
			WorldPersistenceTransaction transaction = new WorldPersistenceTransaction(path);
			transaction.Prepare(store.CreateSnapshot("world", 1));

			WorldPersistenceTransaction recovery = new WorldPersistenceTransaction(path);
			Assert.IsTrue(recovery.Recover());
			WorldPersistenceStore restored = new WorldPersistenceStore();
			restored.Restore(recovery.Load());
			Assert.AreEqual(1, restored.Count);
			Assert.IsTrue(restored.TryGet("one", out _));
		}

		[Test]
		public void OwnershipResolverMigratesEntityAcrossZones()
		{
			WorldManifestData manifest = Manifest();
			WorldZoneDefinitionData west = Zone("west", "zone-west", "cell-west", 0f, 1000f);
			WorldZoneDefinitionData east = Zone("east", "zone-east", "cell-east", 2000f, 3000f);
			WorldEntityOwnershipResolver resolver = new WorldEntityOwnershipResolver(manifest, new[] { west, east });
			WorldDynamicEntityStateData entity = new WorldDynamicEntityStateData { EntityId = "entity", Kind = "Buildable" };

			resolver.Move(entity, new Vector3(2500f, 0f, 0f));

			Assert.AreEqual("zone-east", entity.ZoneId);
			Assert.AreEqual("cell-east", entity.OwnerCellId);
			Assert.AreEqual(1, entity.Revision);
		}

		[Test]
		public void NavBorderBuilderCreatesStableCrossZoneContract()
		{
			WorldManifestData manifest = Manifest();
			WorldZoneDefinitionData west = Zone("west", "zone-west", "cell-west", 0f, 1000f);
			WorldZoneDefinitionData east = Zone("east", "zone-east", "cell-east", 2000f, 3000f);
			WorldTransitionSafetyService safety = new WorldTransitionSafetyService(manifest.Zones[0], manifest.Zones[1], 100f);

			var first = WorldNavBorderLinkBuilder.Build(new[] { west, east }, safety.Corridor);
			var repeat = WorldNavBorderLinkBuilder.Build(new[] { west, east }, safety.Corridor);

			Assert.AreEqual(1, first.Count(link => link.IsCrossZone));
			Assert.AreEqual(first.Single(link => link.IsCrossZone).LinkId, repeat.Single(link => link.IsCrossZone).LinkId);
			Assert.IsFalse(first.Single(link => link.IsCrossZone).IsRuntimeBaked);
		}

		private static WorldEntityRecordData Seed(string id, string kind)
		{
			return new WorldEntityRecordData
			{
				EntityId = id,
				Kind = kind,
				WorldPosition = new RawMapVector3Data(1f, 2f, 3f),
			};
		}

		private static WorldDynamicEntityStateData Entity(string id, string cell, string zone)
		{
			return new WorldDynamicEntityStateData { EntityId = id, Kind = "DroppedItem", OwnerCellId = cell, ZoneId = zone };
		}

		private static WorldManifestData Manifest()
		{
			WorldManifestData manifest = new WorldManifestData { WorldId = "world" };
			manifest.Zones.Add(ZoneIndex("west", "zone-west", 0f, 1000f));
			manifest.Zones.Add(ZoneIndex("east", "zone-east", 2000f, 3000f));
			return manifest;
		}

		private static WorldZoneIndexData ZoneIndex(string key, string id, float minX, float maxX)
		{
			return new WorldZoneIndexData { ZoneKey = key, ZoneId = id, WorldBounds = Bounds(minX, maxX) };
		}

		private static WorldZoneDefinitionData Zone(string key, string id, string cellId, float minX, float maxX)
		{
			WorldZoneDefinitionData zone = new WorldZoneDefinitionData { ZoneKey = key, ZoneId = id };
			zone.Cells.Add(new WorldCellIndexData { CellId = cellId, WorldBounds = Bounds(minX, maxX) });
			return zone;
		}

		private static RawMapBoundsData Bounds(float minX, float maxX)
		{
			return new RawMapBoundsData
			{
				HasValue = true,
				Min = new RawMapVector3Data(minX, -100f, -500f),
				Max = new RawMapVector3Data(maxX, 100f, 500f),
			};
		}
	}
}
