////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using Newtonsoft.Json;
using NUnit.Framework;
using SDG.Unturned.WorldUpgrade;
using System;
using System.IO;
using UnityEngine;

namespace SDG.Unturned.Tests
{
	public class WorldTwoZoneStreamingTests
	{
		private string tempRoot;

		[SetUp]
		public void SetUp()
		{
			tempRoot = Path.Combine(Path.GetTempPath(), "u3-world-streaming-tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempRoot);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(tempRoot))
				Directory.Delete(tempRoot, true);
		}

		[Test]
		public void ZoneResolverUsesBoundsAndRoundTripsLocalCoordinates()
		{
			WorldManifestData manifest = CreateManifest();
			WorldZoneResolver resolver = new WorldZoneResolver(manifest);

			WorldZoneResolution west = resolver.Resolve(new Vector3(250f, 0f, 10f));
			WorldZoneResolution east = resolver.Resolve(new Vector3(2250f, 0f, 10f));

			Assert.AreEqual("west", west.Zone.ZoneKey);
			Assert.IsTrue(west.IsInsideZoneBounds);
			Assert.AreEqual("east", east.Zone.ZoneKey);
			Assert.AreEqual(new Vector3(250f, 0f, 10f), WorldCoordinateStrategy.ToWorld(west.Zone, west.LocalPosition));
			Assert.AreEqual(new Vector3(250f, 0f, 10f), east.LocalPosition);
		}

		[Test]
		public void CellStreamerPreloadsBothZonesAndReturnsToZero()
		{
			WriteRepositoryFixture();
			using (WorldCellBundleRepository west = new WorldCellBundleRepository(tempRoot, "west"))
			using (WorldCellBundleRepository east = new WorldCellBundleRepository(tempRoot, "east"))
			using (WorldCellStreamer streamer = new WorldCellStreamer(new[] { west, east }, 1100f, 1400f))
			{
				streamer.Update(new Vector3(1500f, 0f, 0f));
				Assert.AreEqual(1, streamer.GetActiveCellCount("west"));
				Assert.AreEqual(1, streamer.GetActiveCellCount("east"));
				Assert.AreEqual(2, streamer.ActiveEntityCount);
				streamer.UnloadAll();
				Assert.AreEqual(0, streamer.ActiveCellCount);
				Assert.AreEqual(0, streamer.ActiveEntityCount);
			}
		}

		[Test]
		public void TransitionSafetyCreatesContinuousColliderBridge()
		{
			WorldManifestData manifest = CreateManifest();
			WorldTransitionSafetyService safety = new WorldTransitionSafetyService(manifest.Zones[0], manifest.Zones[1], 100f);
			GameObject bridge = safety.CreateCollisionBridge(20f, 40f);
			try
			{
				Assert.AreEqual(1512f, safety.Corridor.Length);
				Assert.IsTrue(safety.IsInsideCorridor(new Vector3(1500f, 0f, 0f)));
				Assert.IsNotNull(bridge.GetComponent<BoxCollider>());
				Assert.Greater(bridge.GetComponent<BoxCollider>().size.x, 1000f);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(bridge);
			}
		}

		private WorldManifestData CreateManifest()
		{
			WorldManifestData manifest = new WorldManifestData { WorldId = "world-test" };
			manifest.Zones.Add(CreateZoneIndex("west", 0f, 1000f, 0f));
			manifest.Zones.Add(CreateZoneIndex("east", 2000f, 3000f, 2000f));
			return manifest;
		}

		private static WorldZoneIndexData CreateZoneIndex(string key, float minX, float maxX, float offsetX)
		{
			return new WorldZoneIndexData
			{
				ZoneKey = key,
				ZoneId = "zone-" + key,
				RelativePath = "zones/" + key + ".json",
				LayoutOffset = new RawMapVector3Data(offsetX, 0f, 0f),
				WorldBounds = Bounds(minX, maxX),
			};
		}

		private void WriteRepositoryFixture()
		{
			WorldManifestData manifest = CreateManifest();
			WriteJson(Path.Combine(tempRoot, "world-manifest.json"), manifest);
			foreach (WorldZoneIndexData zoneIndex in manifest.Zones)
			{
				WorldZoneDefinitionData zone = new WorldZoneDefinitionData
				{
					WorldId = manifest.WorldId,
					ZoneKey = zoneIndex.ZoneKey,
					ZoneId = zoneIndex.ZoneId,
					WorldBounds = zoneIndex.WorldBounds,
				};
				string cellId = "cell-" + zone.ZoneKey;
				string cellPath = "cells/" + zone.ZoneKey + ".json";
				WorldCellIndexData index = new WorldCellIndexData
				{
					CellId = cellId,
					RelativePath = cellPath,
					WorldBounds = zone.WorldBounds,
					HasLandscapeRecord = true,
				};
				index.RecordCounts.Add(new WorldRecordKindCountData { Kind = "StaticObject", Count = 1 });
				zone.Cells.Add(index);
				WriteJson(Path.Combine(tempRoot, zoneIndex.RelativePath), zone);
				WorldCellData cell = new WorldCellData { WorldId = manifest.WorldId, ZoneId = zone.ZoneId, CellId = cellId };
				cell.Entities.Add(new WorldEntityRecordData { EntityId = "entity-" + zone.ZoneKey, OwnerCellId = cellId, Kind = "StaticObject" });
				WriteJson(Path.Combine(tempRoot, cellPath), cell);
			}
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

		private static void WriteJson(string path, object value)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, JsonConvert.SerializeObject(value));
		}
	}
}
