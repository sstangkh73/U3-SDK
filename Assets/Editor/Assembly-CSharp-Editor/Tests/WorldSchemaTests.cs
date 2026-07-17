////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using NUnit.Framework;
using SDG.Unturned.WorldUpgrade;
using SDG.Unturned.WorldUpgrade.Editor;
using System.Collections.Generic;
using System.Linq;

namespace SDG.Unturned.Tests
{
	public class WorldSchemaTests
	{
		[Test]
		public void StableIdsRepeatAndRespectNamespace()
		{
			string first = WorldStableIdUtility.Create("ent", "zone-a", "object|42");
			string repeat = WorldStableIdUtility.Create("ent", "zone-a", "object|42");
			string otherZone = WorldStableIdUtility.Create("ent", "zone-b", "object|42");

			Assert.AreEqual(first, repeat);
			Assert.AreNotEqual(first, otherZone);
			StringAssert.StartsWith("ent_", first);
			Assert.AreEqual(36, first.Length);
		}

		[TestCase(0f, 0)]
		[TestCase(1023.99f, 0)]
		[TestCase(1024f, 1)]
		[TestCase(-0.01f, -1)]
		[TestCase(-1024f, -1)]
		[TestCase(-1024.01f, -2)]
		public void CellOwnershipUsesFloorForNegativeCoordinates(float coordinate, int expected)
		{
			Assert.AreEqual(expected, WorldSchemaBuilder.FloorToCell(coordinate, 1024));
		}

		[Test]
		public void RepeatedBuildKeepsWorldZoneCellAndEntityIdentity()
		{
			WorldLayoutSourceData layout = CreateLayout("alpha");
			List<WorldSchemaZoneSource> sources = new List<WorldSchemaZoneSource> { CreateSource("alpha") };
			WorldSchemaBuildResult first = WorldSchemaBuilder.Build(layout, sources);
			WorldSchemaBuildResult repeat = WorldSchemaBuilder.Build(layout, sources);

			Assert.IsTrue(first.Manifest.Validation.IsValid);
			Assert.AreEqual(first.Manifest.WorldId, repeat.Manifest.WorldId);
			Assert.AreEqual(first.Manifest.Zones[0].ZoneId, repeat.Manifest.Zones[0].ZoneId);
			Assert.AreEqual(first.Manifest.ContentFingerprintSha256, repeat.Manifest.ContentFingerprintSha256);
			Assert.AreEqual(first.Manifest.EntityIdentityFingerprintSha256, repeat.Manifest.EntityIdentityFingerprintSha256);
			CollectionAssert.AreEqual(first.Entities.Select(value => value.EntityId).ToArray(),
				repeat.Entities.Select(value => value.EntityId).ToArray());
		}

		[Test]
		public void MovingZoneLayoutPreservesIdentityAndChangesContent()
		{
			List<WorldSchemaZoneSource> sources = new List<WorldSchemaZoneSource> { CreateSource("alpha") };
			WorldLayoutSourceData beforeLayout = CreateLayout("alpha");
			WorldLayoutSourceData afterLayout = CreateLayout("alpha");
			afterLayout.Zones[0].LayoutOffset.X = 2048f;

			WorldSchemaBuildResult before = WorldSchemaBuilder.Build(beforeLayout, sources);
			WorldSchemaBuildResult after = WorldSchemaBuilder.Build(afterLayout, sources);

			CollectionAssert.AreEqual(before.Entities.Select(value => value.EntityId).ToArray(),
				after.Entities.Select(value => value.EntityId).ToArray());
			Assert.AreNotEqual(before.Manifest.ContentFingerprintSha256, after.Manifest.ContentFingerprintSha256);
			Assert.IsTrue(before.Entities.Zip(after.Entities, (left, right) =>
				left.ContentFingerprintSha256 != right.ContentFingerprintSha256).All(changed => changed));
		}

		[Test]
		public void ValidatorRejectsDuplicateIdentityAndQuarantinesMigrationAliasCollision()
		{
			WorldLayoutSourceData layout = CreateLayout("alpha");
			WorldSchemaZoneSource source = CreateSource("alpha");
			source.Decode.EntitySeeds.Add(new WorldSchemaEntitySeed
			{
				Kind = "StaticObject",
				SourceKey = "Level/Objects.dat#instance:10",
				Position = new RawMapVector3Data(4f, 5f, 6f),
			});
			source.Decode.AssetAliases.Add(new WorldSchemaAssetAliasSeed
			{
				Kind = "ObjectAsset",
				LegacyId = 7,
				AssetGuid = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
				SourceKey = "second",
			});

			WorldSchemaBuildResult result = WorldSchemaBuilder.Build(layout, new List<WorldSchemaZoneSource> { source });

			Assert.IsFalse(result.Manifest.Validation.IsValid);
			Assert.Greater(result.Manifest.Validation.DuplicateIdCount, 0);
			Assert.IsTrue(result.Manifest.Validation.Issues.Any(issue => issue.Code == "QuarantinedMigrationAliasCollision"));
			Assert.IsFalse(result.Manifest.AssetMigrations.Where(value => value.LegacyId == 7).Any(value => value.CanResolveLegacyId));
		}

		[Test]
		public void DiffClassifiesAddedRemovedMovedModifiedAndUnchanged()
		{
			WorldEntityRecordData removed = Entity("removed", "cell-a", "hash-a");
			WorldEntityRecordData movedBefore = Entity("moved", "cell-a", "hash-b");
			WorldEntityRecordData modifiedBefore = Entity("modified", "cell-a", "hash-c");
			WorldEntityRecordData unchanged = Entity("unchanged", "cell-a", "hash-d");
			List<WorldEntityRecordData> before = new List<WorldEntityRecordData> { removed, movedBefore, modifiedBefore, unchanged };
			List<WorldEntityRecordData> after = new List<WorldEntityRecordData>
			{
				Entity("added", "cell-b", "hash-e"),
				Entity("moved", "cell-b", "hash-b2"),
				Entity("modified", "cell-a", "hash-c2"),
				Entity("unchanged", "cell-a", "hash-d"),
			};

			WorldSchemaDiffData diff = WorldSchemaDiffUtility.Compare(before, after);

			Assert.AreEqual(1, diff.AddedCount);
			Assert.AreEqual(1, diff.RemovedCount);
			Assert.AreEqual(1, diff.MovedCount);
			Assert.AreEqual(1, diff.ModifiedCount);
			Assert.AreEqual(1, diff.UnchangedCount);
		}

		private static WorldLayoutSourceData CreateLayout(string zoneKey)
		{
			WorldLayoutSourceData layout = new WorldLayoutSourceData
			{
				WorldKey = "test-world",
				DisplayName = "Test World",
				CellSize = 1024,
			};
			layout.Zones.Add(new WorldZoneLayoutSourceData { ZoneKey = zoneKey });
			return layout;
		}

		private static WorldSchemaZoneSource CreateSource(string zoneKey)
		{
			RawMapDetailedDecodeResult decode = new RawMapDetailedDecodeResult
			{
				Summary = new RawMapDecodedSummaryData { IsValid = true, SchemaVersion = 1 },
				EntitySeeds = new List<WorldSchemaEntitySeed>(),
				AssetAliases = new List<WorldSchemaAssetAliasSeed>(),
			};
			decode.EntitySeeds.Add(new WorldSchemaEntitySeed
			{
				Kind = "LandscapeTile",
				SourceKey = "Landscape#tile:0:0",
				Position = new RawMapVector3Data(0f, 0f, 0f),
				HasHeightmap = true,
			});
			decode.EntitySeeds.Add(new WorldSchemaEntitySeed
			{
				Kind = "StaticObject",
				SourceKey = "Level/Objects.dat#instance:10",
				Position = new RawMapVector3Data(1f, 2f, 3f),
				LegacyAssetId = 7,
				AssetGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
				SourceInstanceId = 10,
			});
			decode.AssetAliases.Add(new WorldSchemaAssetAliasSeed
			{
				Kind = "ObjectAsset",
				LegacyId = 7,
				AssetGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
				SourceKey = "first",
			});
			return new WorldSchemaZoneSource
			{
				ZoneKey = zoneKey,
				DisplayName = zoneKey,
				Inventory = new RawMapManifestData { InventoryFingerprintSha256 = "inventory-" + zoneKey },
				Decode = decode,
			};
		}

		private static WorldEntityRecordData Entity(string id, string cell, string hash)
		{
			return new WorldEntityRecordData
			{
				EntityId = id,
				OwnerCellId = cell,
				Kind = "Test",
				ContentFingerprintSha256 = hash,
			};
		}
	}
}
