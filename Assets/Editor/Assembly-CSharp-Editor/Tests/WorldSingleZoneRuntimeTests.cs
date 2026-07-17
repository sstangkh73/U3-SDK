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
	public class WorldSingleZoneRuntimeTests
	{
		private string tempRoot;

		[SetUp]
		public void SetUp()
		{
			tempRoot = Path.Combine(Path.GetTempPath(), "u3-world-runtime-tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempRoot);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(tempRoot))
				Directory.Delete(tempRoot, true);
		}

		[Test]
		public void TerrainDecoderPreservesSamplesAndAppliesExplicitFallbacks()
		{
			string heightDirectory = Directory.CreateDirectory(Path.Combine(tempRoot, "Landscape", "Heightmaps")).FullName;
			string splatDirectory = Directory.CreateDirectory(Path.Combine(tempRoot, "Landscape", "Splatmaps")).FullName;
			string holesDirectory = Directory.CreateDirectory(Path.Combine(tempRoot, "Landscape", "Holes")).FullName;
			byte[] height = new byte[WorldRuntimeTerrainDecoder.HeightmapBytes];
			height[0] = 0x12;
			height[1] = 0x34;
			File.WriteAllBytes(Path.Combine(heightDirectory, "Tile_-1_2_Source.heightmap"), height);
			byte[] splat = new byte[WorldRuntimeTerrainDecoder.SplatmapBytes];
			for (int offset = 0; offset < splat.Length; offset += WorldRuntimeTerrainDecoder.SplatLayers)
				splat[offset] = 255;
			splat[0] = 0;
			File.WriteAllBytes(Path.Combine(splatDirectory, "Tile_-1_2_Source.splatmap"), splat);
			byte[] holes = new byte[WorldRuntimeTerrainDecoder.HolesBytes];
			holes[0] = 1;
			holes[1] = 0b_0000_0101;
			File.WriteAllBytes(Path.Combine(holesDirectory, "Tile_-1_2.bin"), holes);

			WorldRuntimeTerrainPayload payload = WorldRuntimeTerrainDecoder.Decode(tempRoot, -1, 2);

			Assert.AreEqual(0x1234 / (float) ushort.MaxValue, payload.Heights[0, 0]);
			Assert.AreEqual(1, payload.BlackSplatFallbackPixelCount);
			Assert.AreEqual(1f, payload.SplatWeights[0, 0, 0]);
			Assert.AreEqual(1f, payload.SplatWeights[0, 1, 0]);
			Assert.IsTrue(payload.Holes[0, 0]);
			Assert.IsFalse(payload.Holes[0, 1]);
			Assert.IsTrue(payload.Holes[0, 2]);
		}

		[Test]
		public void TerrainDecoderUsesFlatSolidDefaultsForOrphanCell()
		{
			WorldRuntimeTerrainPayload payload = WorldRuntimeTerrainDecoder.Decode(tempRoot, 4, -5);

			Assert.IsTrue(payload.UsedDefaultHeightmap);
			Assert.IsTrue(payload.UsedDefaultSplatmap);
			Assert.AreEqual(0.5f, payload.Heights[0, 0]);
			Assert.AreEqual(1f, payload.SplatWeights[0, 0, 0]);
			Assert.IsTrue(payload.Holes[0, 0]);
		}

		[Test]
		public void CellRepositoryLoadsIdempotentlyAndReturnsToZeroResidency()
		{
			WriteRepositoryFixture("zones/test.json", "cells/test/0_0.json");
			using (WorldCellBundleRepository repository = new WorldCellBundleRepository(tempRoot, "test"))
			{
				WorldCellData first = repository.LoadCell("cell-test");
				WorldCellData repeat = repository.LoadCell("cell-test");
				Assert.AreSame(first, repeat);
				Assert.AreEqual(1, repository.LoadedCellCount);
				Assert.AreEqual(1, repository.LoadedEntityCount);
				Assert.Greater(repository.EstimatedResidentBytes, 0);
				repository.UnloadAll();
				Assert.AreEqual(0, repository.LoadedCellCount);
				Assert.AreEqual(0, repository.LoadedEntityCount);
				Assert.AreEqual(0, repository.EstimatedResidentBytes);
			}
		}

		[Test]
		public void CellRepositoryRejectsPathEscapingSchemaRoot()
		{
			WriteRepositoryFixture("../outside-zone.json", "cells/test/0_0.json");
			Assert.Throws<InvalidDataException>(() => new WorldCellBundleRepository(tempRoot, "test"));
		}

		[Test]
		public void AssetRegistryReferenceCountsAndReturnsToZero()
		{
			string guid = "00112233445566778899aabbccddeeff";
			ObjectAsset asset = new ObjectAsset();
			using (WorldAssetRegistry registry = new WorldAssetRegistry(_ => asset))
			{
				Assert.AreSame(asset, registry.AcquireObjectAsset(guid));
				Assert.AreSame(asset, registry.AcquireObjectAsset(guid));
				Assert.AreEqual(1, registry.ActiveObjectAssetCount);
				Assert.AreEqual(2, registry.TotalObjectAssetReferenceCount);
				Assert.AreEqual(2, registry.GetObjectAssetReferenceCount(guid));
				Assert.AreEqual(1, registry.PeakObjectAssetCount);
				Assert.AreEqual(2, registry.PeakObjectAssetReferenceCount);
				Assert.IsTrue(registry.ReleaseObjectAsset(guid));
				Assert.IsTrue(registry.ReleaseObjectAsset(guid));
				Assert.AreEqual(0, registry.ActiveObjectAssetCount);
				Assert.AreEqual(0, registry.TotalObjectAssetReferenceCount);
			}
		}

		[Test]
		public void TerrainFactoryCreatesColliderAndCanBeDestroyed()
		{
			WorldRuntimeTerrainPayload payload = WorldRuntimeTerrainDecoder.Decode(tempRoot, 0, 0);
			GameObject terrainObject = WorldRuntimePreviewFactory.CreateTerrain(payload, null, new Vector3(10000f, 0f, 0f));
			Terrain terrain = terrainObject.GetComponent<Terrain>();
			TerrainCollider collider = terrainObject.GetComponent<TerrainCollider>();
			Assert.IsNotNull(terrain);
			Assert.IsNotNull(collider);
			Assert.AreSame(terrain.terrainData, collider.terrainData);
			TerrainData data = terrain.terrainData;
			UnityEngine.Object.DestroyImmediate(terrainObject);
			UnityEngine.Object.DestroyImmediate(data);
		}

		private void WriteRepositoryFixture(string zonePath, string cellPath)
		{
			WorldManifestData manifest = new WorldManifestData { WorldId = "world-test" };
			manifest.Zones.Add(new WorldZoneIndexData { ZoneKey = "test", ZoneId = "zone-test", RelativePath = zonePath });
			WriteJson(Path.Combine(tempRoot, "world-manifest.json"), manifest);
			if (zonePath.StartsWith("..", StringComparison.Ordinal))
				return;
			WorldZoneDefinitionData zone = new WorldZoneDefinitionData { WorldId = "world-test", ZoneKey = "test", ZoneId = "zone-test" };
			zone.Cells.Add(new WorldCellIndexData { CellId = "cell-test", GridX = 0, GridZ = 0, RelativePath = cellPath });
			WriteJson(Path.Combine(tempRoot, zonePath), zone);
			WorldCellData cell = new WorldCellData { WorldId = "world-test", ZoneId = "zone-test", CellId = "cell-test" };
			cell.Entities.Add(new WorldEntityRecordData { EntityId = "entity-test", OwnerCellId = "cell-test", Kind = "Test" });
			WriteJson(Path.Combine(tempRoot, cellPath), cell);
		}

		private static void WriteJson(string path, object value)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, JsonConvert.SerializeObject(value));
		}
	}
}
