////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using NUnit.Framework;
using SDG.Unturned.WorldUpgrade;
using System;
using System.Linq;
using UnityEngine;

namespace SDG.Unturned.Tests
{
	public class WorldServerAuthorityTests
	{
		[Test]
		public void PickupAndDropReplaysDoNotDuplicateItems()
		{
			WorldPersistenceStore store = new WorldPersistenceStore();
			WorldServerAuthority authority = Authority(store);
			WorldConnectResultData connect = authority.Connect("client", null, new Vector3(500f, 0f, 0f));
			Assert.IsTrue(connect.Accepted);
			Assert.IsTrue(authority.SeedServerEntity(Entity("item", "cell-west", "zone-west", 500f)));
			WorldClientCommandData pickup = new WorldClientCommandData { RequestId = "pickup", ClientId = "client", Command = EWorldClientCommand.PickupEntity, EntityId = "item", ExpectedEntityRevision = 1 };
			Assert.IsTrue(authority.ProcessClientCommand(pickup).Accepted);
			Assert.IsTrue(authority.ProcessClientCommand(pickup).WasReplay);
			WorldClientCommandData drop = new WorldClientCommandData { RequestId = "drop", ClientId = "client", Command = EWorldClientCommand.DropInventoryItem, InventoryItemId = "item" };
			WorldAuthorityCommandResultData dropped = authority.ProcessClientCommand(drop);
			Assert.IsTrue(dropped.Accepted);
			Assert.AreEqual(dropped.EntityId, authority.ProcessClientCommand(drop).EntityId);
			Assert.AreEqual(1, store.Entities.Count(entity => !entity.IsRemoved));
			Assert.AreEqual(0, authority.GetReplicationSnapshot("client").InventoryItemIds.Count);
		}

		[Test]
		public void ReconnectRequiresServerTokenAndPreservesSnapshot()
		{
			WorldServerAuthority authority = Authority(new WorldPersistenceStore());
			WorldConnectResultData connected = authority.Connect("client", null, new Vector3(500f, 0f, 0f));
			WorldReplicationSnapshotData before = authority.GetReplicationSnapshot("client");
			Assert.IsTrue(authority.Disconnect("client"));
			Assert.IsFalse(authority.Connect("client", "wrong", Vector3.zero).Accepted);
			Assert.IsTrue(authority.Connect("client", connected.Session.ReconnectToken, Vector3.zero).Accepted);
			WorldReplicationSnapshotData after = authority.GetReplicationSnapshot("client");
			Assert.AreEqual(before.ZoneId, after.ZoneId);
			Assert.AreEqual(before.OwnerCellId, after.OwnerCellId);
		}

		[Test]
		public void ClientTravelAndSaveAuthorityAreRejected()
		{
			WorldServerAuthority authority = Authority(new WorldPersistenceStore());
			authority.Connect("client", null, new Vector3(500f, 0f, 0f));
			Assert.AreEqual("ClientTravelAuthorityRejected", authority.ProcessClientCommand(new WorldClientCommandData
				{ RequestId = "travel", ClientId = "client", Command = EWorldClientCommand.ClientTravelCommit }).Code);
			Assert.AreEqual("ClientSaveAuthorityRejected", authority.ProcessClientCommand(new WorldClientCommandData
				{ RequestId = "save", ClientId = "client", Command = EWorldClientCommand.ClientSaveImport }).Code);
		}

		[Test]
		public void ReplicationSnapshotExcludesOtherCell()
		{
			WorldPersistenceStore store = new WorldPersistenceStore();
			WorldServerAuthority authority = Authority(store);
			authority.Connect("client", null, new Vector3(500f, 0f, 0f));
			authority.SeedServerEntity(Entity("near", "cell-west", "zone-west", 500f));
			authority.SeedServerEntity(Entity("far", "cell-east", "zone-east", 2500f));
			WorldReplicationSnapshotData snapshot = authority.GetReplicationSnapshot("client");
			Assert.IsTrue(snapshot.Entities.Any(entity => entity.EntityId == "near"));
			Assert.IsFalse(snapshot.Entities.Any(entity => entity.EntityId == "far"));
		}

		[Test]
		public void CompatibilityPolicyRejectsFutureProtocol()
		{
			WorldManifestData manifest = Manifest();
			WorldPersistenceSnapshotData snapshot = new WorldPersistenceStore().CreateSnapshot("world", 1);
			WorldCompatibilityPolicyData policy = new WorldCompatibilityPolicyData();
			Assert.Throws<InvalidOperationException>(() => policy.Validate(2, manifest, snapshot));
			Assert.DoesNotThrow(() => policy.Validate(1, manifest, snapshot));
		}

		[Test]
		public void RequestIdsAreScopedPerClient()
		{
			WorldServerAuthority authority = Authority(new WorldPersistenceStore());
			authority.Connect("client-a", null, new Vector3(500f, 0f, 0f));
			authority.Connect("client-b", null, new Vector3(500f, 0f, 0f));
			WorldAuthorityCommandResultData first = authority.ProcessClientCommand(new WorldClientCommandData
				{ RequestId = "same", ClientId = "client-a", Command = EWorldClientCommand.ClientSaveImport });
			WorldAuthorityCommandResultData second = authority.ProcessClientCommand(new WorldClientCommandData
				{ RequestId = "same", ClientId = "client-b", Command = EWorldClientCommand.ClientTravelCommit });
			Assert.AreEqual("ClientSaveAuthorityRejected", first.Code);
			Assert.AreEqual("ClientTravelAuthorityRejected", second.Code);
			Assert.IsFalse(second.WasReplay);
		}

		private static WorldServerAuthority Authority(WorldPersistenceStore store)
		{
			WorldZoneDefinitionData west = Zone("west", "zone-west", "cell-west", 0f, 1000f);
			WorldZoneDefinitionData east = Zone("east", "zone-east", "cell-east", 2000f, 3000f);
			return new WorldServerAuthority("secret", store, Manifest(), new[] { west, east }, 100f);
		}

		private static WorldDynamicEntityStateData Entity(string id, string cell, string zone, float x) => new WorldDynamicEntityStateData
			{ EntityId = id, Kind = "DroppedItem", ZoneId = zone, OwnerCellId = cell, WorldPosition = new RawMapVector3Data(x, 0f, 0f), Revision = 1 };
		private static WorldManifestData Manifest()
		{
			WorldManifestData manifest = new WorldManifestData { WorldId = "world" };
			manifest.Zones.Add(Index("west", "zone-west", 0f, 1000f));
			manifest.Zones.Add(Index("east", "zone-east", 2000f, 3000f));
			return manifest;
		}
		private static WorldZoneIndexData Index(string key, string id, float minX, float maxX) => new WorldZoneIndexData { ZoneKey = key, ZoneId = id, WorldBounds = Bounds(minX, maxX) };
		private static WorldZoneDefinitionData Zone(string key, string id, string cellId, float minX, float maxX)
		{
			WorldZoneDefinitionData zone = new WorldZoneDefinitionData { ZoneKey = key, ZoneId = id };
			zone.Cells.Add(new WorldCellIndexData { CellId = cellId, WorldBounds = Bounds(minX, maxX) });
			return zone;
		}
		private static RawMapBoundsData Bounds(float minX, float maxX) => new RawMapBoundsData
			{ HasValue = true, Min = new RawMapVector3Data(minX, -100f, -500f), Max = new RawMapVector3Data(maxX, 100f, 500f) };
	}
}
