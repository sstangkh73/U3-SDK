////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.IO;

namespace SDG.Unturned.WorldUpgrade
{
	public sealed class WorldRuntimeTerrainPayload
	{
		public int GridX;
		public int GridZ;
		public bool HasHeightmapSource;
		public bool HasSplatmapSource;
		public bool HasHolesSource;
		public bool UsedDefaultHeightmap;
		public bool UsedDefaultSplatmap;
		public int BlackSplatFallbackPixelCount;
		public float[,] Heights;
		public float[,,] SplatWeights;
		public bool[,] Holes;
	}

	public static class WorldRuntimeTerrainDecoder
	{
		public const int HeightResolution = 257;
		public const int SplatResolution = 256;
		public const int SplatLayers = 8;
		public const int HeightmapBytes = HeightResolution * HeightResolution * 2;
		public const int SplatmapBytes = SplatResolution * SplatResolution * SplatLayers;
		public const int HolesBytes = 1 + (SplatResolution * SplatResolution / 8);

		public static WorldRuntimeTerrainPayload Decode(string sourceRoot, int gridX, int gridZ)
		{
			if (string.IsNullOrWhiteSpace(sourceRoot))
				throw new ArgumentException("Map source root is required.", nameof(sourceRoot));
			WorldRuntimeTerrainPayload payload = new WorldRuntimeTerrainPayload
			{
				GridX = gridX,
				GridZ = gridZ,
				Heights = new float[HeightResolution, HeightResolution],
				SplatWeights = new float[SplatResolution, SplatResolution, SplatLayers],
				Holes = new bool[SplatResolution, SplatResolution],
			};

			string tile = "Tile_" + gridX.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_" + gridZ.ToString(System.Globalization.CultureInfo.InvariantCulture);
			DecodeHeightmap(Path.Combine(sourceRoot, "Landscape", "Heightmaps", tile + "_Source.heightmap"), payload);
			DecodeSplatmap(Path.Combine(sourceRoot, "Landscape", "Splatmaps", tile + "_Source.splatmap"), payload);
			DecodeHoles(Path.Combine(sourceRoot, "Landscape", "Holes", tile + ".bin"), payload);
			return payload;
		}

		private static void DecodeHeightmap(string path, WorldRuntimeTerrainPayload payload)
		{
			payload.HasHeightmapSource = File.Exists(path);
			if (!payload.HasHeightmapSource)
			{
				payload.UsedDefaultHeightmap = true;
				for (int x = 0; x < HeightResolution; ++x)
					for (int y = 0; y < HeightResolution; ++y)
						payload.Heights[x, y] = 0.5f;
				return;
			}
			byte[] bytes = File.ReadAllBytes(path);
			if (bytes.Length != HeightmapBytes)
				throw new InvalidDataException("Runtime heightmap size mismatch: " + path);
			int offset = 0;
			for (int x = 0; x < HeightResolution; ++x)
			{
				for (int y = 0; y < HeightResolution; ++y)
				{
					int raw = (bytes[offset++] << 8) | bytes[offset++];
					payload.Heights[x, y] = raw / (float) ushort.MaxValue;
				}
			}
		}

		private static void DecodeSplatmap(string path, WorldRuntimeTerrainPayload payload)
		{
			payload.HasSplatmapSource = File.Exists(path);
			if (!payload.HasSplatmapSource)
			{
				payload.UsedDefaultSplatmap = true;
				for (int x = 0; x < SplatResolution; ++x)
					for (int y = 0; y < SplatResolution; ++y)
						payload.SplatWeights[x, y, 0] = 1f;
				return;
			}
			byte[] bytes = File.ReadAllBytes(path);
			if (bytes.Length != SplatmapBytes)
				throw new InvalidDataException("Runtime splatmap size mismatch: " + path);
			int offset = 0;
			for (int x = 0; x < SplatResolution; ++x)
			{
				for (int y = 0; y < SplatResolution; ++y)
				{
					int sum = 0;
					for (int layer = 0; layer < SplatLayers; ++layer)
					{
						byte raw = bytes[offset++];
						payload.SplatWeights[x, y, layer] = raw / (float) byte.MaxValue;
						sum += raw;
					}
					if (sum == 0)
					{
						payload.SplatWeights[x, y, 0] = 1f;
						payload.BlackSplatFallbackPixelCount++;
					}
				}
			}
		}

		private static void DecodeHoles(string path, WorldRuntimeTerrainPayload payload)
		{
			for (int x = 0; x < SplatResolution; ++x)
				for (int y = 0; y < SplatResolution; ++y)
					payload.Holes[x, y] = true;
			payload.HasHolesSource = File.Exists(path);
			if (!payload.HasHolesSource)
				return;
			byte[] bytes = File.ReadAllBytes(path);
			if (bytes.Length != HolesBytes)
				throw new InvalidDataException("Runtime holes size mismatch: " + path);
			int offset = 1;
			for (int x = 0; x < SplatResolution; ++x)
			{
				for (int y = 0; y < SplatResolution; y += 8)
				{
					byte value = bytes[offset++];
					for (int bit = 0; bit < 8; ++bit)
						payload.Holes[x, y + bit] = (value & (1 << bit)) != 0;
				}
			}
		}
	}
}
