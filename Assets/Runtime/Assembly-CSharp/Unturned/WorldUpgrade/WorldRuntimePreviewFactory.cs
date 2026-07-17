////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Framework.Landscapes;
using System;
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade
{
	public static class WorldRuntimePreviewFactory
	{
		public static GameObject CreateTerrain(WorldRuntimeTerrainPayload payload, TerrainLayer[] terrainLayers, Vector3 previewOffset)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));
			TerrainData data = new TerrainData
			{
				heightmapResolution = WorldRuntimeTerrainDecoder.HeightResolution,
				alphamapResolution = WorldRuntimeTerrainDecoder.SplatResolution,
				baseMapResolution = WorldRuntimeTerrainDecoder.SplatResolution,
				size = new Vector3(Landscape.TILE_SIZE, Landscape.TILE_HEIGHT, Landscape.TILE_SIZE),
			};
			data.SetHeights(0, 0, payload.Heights);
			data.SetHoles(0, 0, payload.Holes);
			if (terrainLayers != null && terrainLayers.Length == WorldRuntimeTerrainDecoder.SplatLayers)
			{
				bool allLayersPresent = true;
				for (int index = 0; index < terrainLayers.Length; ++index)
					allLayersPresent &= terrainLayers[index] != null;
				if (allLayersPresent)
				{
					data.terrainLayers = terrainLayers;
					data.SetAlphamaps(0, 0, payload.SplatWeights);
				}
			}

			GameObject gameObject = Terrain.CreateTerrainGameObject(data);
			gameObject.name = "WorldUpgrade_Terrain_" + payload.GridX + "_" + payload.GridZ;
			gameObject.transform.position = new Vector3(payload.GridX * Landscape.TILE_SIZE, -Landscape.TILE_HEIGHT / 2f,
				payload.GridZ * Landscape.TILE_SIZE) + previewOffset;
			return gameObject;
		}

		public static GameObject CreateObject(WorldEntityRecordData entity, Vector3 previewOffset)
		{
			return CreateObject(entity, previewOffset, null);
		}

		public static GameObject CreateObject(WorldEntityRecordData entity, Vector3 previewOffset, WorldAssetRegistry registry)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));
			if (!Guid.TryParse(entity.AssetGuid, out Guid guid))
				return null;
			ObjectAsset asset = registry != null
				? registry.AcquireObjectAsset(entity.AssetGuid)
				: Assets.find(new AssetReference<ObjectAsset>(guid));
			GameObject prefab = asset?.GetOrLoadModel(false);
			if (prefab == null)
			{
				registry?.ReleaseObjectAsset(entity.AssetGuid);
				return null;
			}
			Vector3 position = ToVector3(entity.WorldPosition) + previewOffset;
			Quaternion rotation = Quaternion.Euler(ToVector3(entity.Rotation));
			GameObject instance;
			try
			{
				instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
			}
			catch
			{
				registry?.ReleaseObjectAsset(entity.AssetGuid);
				throw;
			}
			instance.name = "WorldUpgrade_" + entity.EntityId;
			if (asset.useScale)
				instance.transform.localScale = ToVector3(entity.Scale);
			if (registry != null)
				instance.AddComponent<WorldRuntimeAssetLease>().Initialize(registry, entity.AssetGuid);
			return instance;
		}

		public static void DestroyTerrain(GameObject terrainObject)
		{
			if (terrainObject == null)
				return;
			Terrain terrain = terrainObject.GetComponent<Terrain>();
			TerrainData data = terrain?.terrainData;
			UnityEngine.Object.Destroy(terrainObject);
			if (data != null)
				UnityEngine.Object.Destroy(data);
		}

		public static Vector3 ToVector3(RawMapVector3Data value)
		{
			return value == null ? Vector3.zero : new Vector3(value.X, value.Y, value.Z);
		}
	}
}
