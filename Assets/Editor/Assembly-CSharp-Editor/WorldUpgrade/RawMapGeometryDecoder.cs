////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Framework.IO.FormattedFiles;
using SDG.Framework.IO.FormattedFiles.KeyValueTables;
using SDG.Unturned.WorldUpgrade;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	internal static class RawMapGeometryDecoder
	{
		private const int REGION_AXIS_COUNT = 64;
		private const int LANDSCAPE_TILE_SIZE = 1024;
		private const int LANDSCAPE_TILE_HEIGHT = 2048;
		private const int HEIGHTMAP_BYTES = 257 * 257 * 2;
		private const int SPLATMAP_BYTES = 256 * 256 * 8;
		private const int HOLES_BYTES = 1 + (256 * 256 / 8);

		private static readonly Regex HeightmapName = new Regex(@"^Tile_(-?\d+)_(-?\d+)_Source\.heightmap$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex SplatmapName = new Regex(@"^Tile_(-?\d+)_(-?\d+)_Source\.splatmap$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex HolesName = new Regex(@"^Tile_(-?\d+)_(-?\d+)\.bin$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		internal static void DecodeAll(RawMapDecodeContext context)
		{
			context.Capture("Landscape", () => DecodeLandscape(context));
			context.Capture("Level/Objects.dat", () => DecodeObjects(context));
			context.Capture("Environment/Roads.dat", () => DecodeRoads(context));
			context.Capture("Level.hierarchy", () => DecodeHierarchy(context));
		}

		internal static void DecodeLandscape(RawMapDecodeContext context)
		{
			RawMapLandscapeSummaryData summary = context.Summary.Landscape;
			string terrainRoot = context.GetFullPath("Terrain");
			if (Directory.Exists(terrainRoot))
			{
				FileInfo[] legacyFiles = new DirectoryInfo(terrainRoot).EnumerateFiles("*", SearchOption.AllDirectories).ToArray();
				summary.LegacyTerrainFileCount = legacyFiles.Length;
				summary.LegacyTerrainBytes = legacyFiles.Sum(file => file.Length);
			}

			Dictionary<string, RawMapLandscapeTileSummaryData> tiles = new Dictionary<string, RawMapLandscapeTileSummaryData>(StringComparer.Ordinal);
			DecodeHeightmaps(context, summary, tiles);
			DecodeSplatmaps(context, summary, tiles);
			DecodeHoles(context, summary, tiles);

			foreach (RawMapLandscapeTileSummaryData tile in tiles.Values.OrderBy(value => value.X).ThenBy(value => value.Y))
			{
				if (!tile.HasHeightmap && (tile.HasSplatmap || tile.HasHoles))
				{
					summary.MissingHeightmapCount++;
					context.AddWarning("MissingLandscapeHeightmap", "Landscape/Heightmaps", -1, "Tile " + tile.X + "," + tile.Y + " has splatmap or holes data but no matching source heightmap.");
				}
				if (tile.HasHeightmap && !tile.HasSplatmap)
				{
					summary.MissingSplatmapCount++;
					context.AddWarning("MissingLandscapeSplatmap", "Landscape/Splatmaps", -1, "Heightmap tile " + tile.X + "," + tile.Y + " has no matching source splatmap.");
				}
				summary.Tiles.Add(tile);
			}
		}

		private static void DecodeHeightmaps(RawMapDecodeContext context, RawMapLandscapeSummaryData summary,
			Dictionary<string, RawMapLandscapeTileSummaryData> tiles)
		{
			string directory = context.GetFullPath("Landscape/Heightmaps");
			if (!Directory.Exists(directory))
			{
				context.AddWarning("MissingLandscapeDirectory", "Landscape/Heightmaps", -1, "Landscape heightmap directory is not present.");
				return;
			}

			foreach (string filePath in Directory.EnumerateFiles(directory, "*.heightmap", SearchOption.TopDirectoryOnly))
			{
				Match match = HeightmapName.Match(Path.GetFileName(filePath));
				if (!match.Success)
				{
					context.AddWarning("UnknownLandscapeHeightmap", GetRelative(context, filePath), -1, "Heightmap filename does not match the supported tile convention.");
					continue;
				}

				int x = ParseCoordinate(match.Groups[1].Value);
				int y = ParseCoordinate(match.Groups[2].Value);
				RawMapLandscapeTileSummaryData tile = GetTile(tiles, x, y);
				byte[] bytes = File.ReadAllBytes(filePath);
				tile.HasHeightmap = true;
				tile.HeightmapBytes = bytes.Length;
				tile.HeightmapSha256 = RawMapDecodeContext.CalculateSha256(bytes);
				summary.HeightmapTileCount++;
				if (bytes.Length != HEIGHTMAP_BYTES)
				{
					summary.InvalidFileCount++;
					context.AddError("LandscapeHeightmapSize", GetRelative(context, filePath), -1, "Expected " + HEIGHTMAP_BYTES + " bytes, found " + bytes.Length + ".");
					continue;
				}

				int minRaw = ushort.MaxValue;
				int maxRaw = ushort.MinValue;
				for (int offset = 0; offset < bytes.Length; offset += 2)
				{
					int raw = (bytes[offset] << 8) | bytes[offset + 1];
					minRaw = Math.Min(minRaw, raw);
					maxRaw = Math.Max(maxRaw, raw);
				}
				tile.MinRawHeight = minRaw;
				tile.MaxRawHeight = maxRaw;

				float minHeight = (minRaw / (float) ushort.MaxValue * LANDSCAPE_TILE_HEIGHT) - (LANDSCAPE_TILE_HEIGHT / 2f);
				float maxHeight = (maxRaw / (float) ushort.MaxValue * LANDSCAPE_TILE_HEIGHT) - (LANDSCAPE_TILE_HEIGHT / 2f);
				RawMapDecodeContext.Include(summary.WorldBounds, new RawMapVector3Data(x * LANDSCAPE_TILE_SIZE, minHeight, y * LANDSCAPE_TILE_SIZE));
				RawMapDecodeContext.Include(summary.WorldBounds, new RawMapVector3Data((x + 1) * LANDSCAPE_TILE_SIZE, maxHeight, (y + 1) * LANDSCAPE_TILE_SIZE));
			}
		}

		private static void DecodeSplatmaps(RawMapDecodeContext context, RawMapLandscapeSummaryData summary,
			Dictionary<string, RawMapLandscapeTileSummaryData> tiles)
		{
			string directory = context.GetFullPath("Landscape/Splatmaps");
			if (!Directory.Exists(directory))
				return;

			foreach (string filePath in Directory.EnumerateFiles(directory, "*.splatmap", SearchOption.TopDirectoryOnly))
			{
				Match match = SplatmapName.Match(Path.GetFileName(filePath));
				if (!match.Success)
				{
					context.AddWarning("UnknownLandscapeSplatmap", GetRelative(context, filePath), -1, "Splatmap filename does not match the supported tile convention.");
					continue;
				}

				RawMapLandscapeTileSummaryData tile = GetTile(tiles, ParseCoordinate(match.Groups[1].Value), ParseCoordinate(match.Groups[2].Value));
				byte[] bytes = File.ReadAllBytes(filePath);
				tile.HasSplatmap = true;
				tile.SplatmapBytes = bytes.Length;
				tile.SplatmapSha256 = RawMapDecodeContext.CalculateSha256(bytes);
				summary.SplatmapTileCount++;
				if (bytes.Length != SPLATMAP_BYTES)
				{
					summary.InvalidFileCount++;
					context.AddError("LandscapeSplatmapSize", GetRelative(context, filePath), -1, "Expected " + SPLATMAP_BYTES + " bytes, found " + bytes.Length + ".");
					continue;
				}

				for (int pixelOffset = 0; pixelOffset < bytes.Length; pixelOffset += 8)
				{
					int sum = 0;
					for (int layer = 0; layer < 8; ++layer)
						sum += bytes[pixelOffset + layer];
					if (sum == 0)
						tile.BlackSplatPixelCount++;
				}
				if (tile.BlackSplatPixelCount > 0)
					context.AddWarning("BlackLandscapeSplatPixels", GetRelative(context, filePath), -1, tile.BlackSplatPixelCount + " pixel(s) have zero total material weight.");
			}
		}

		private static void DecodeHoles(RawMapDecodeContext context, RawMapLandscapeSummaryData summary,
			Dictionary<string, RawMapLandscapeTileSummaryData> tiles)
		{
			string directory = context.GetFullPath("Landscape/Holes");
			if (!Directory.Exists(directory))
				return;

			foreach (string filePath in Directory.EnumerateFiles(directory, "*.bin", SearchOption.TopDirectoryOnly))
			{
				Match match = HolesName.Match(Path.GetFileName(filePath));
				if (!match.Success)
				{
					context.AddWarning("UnknownLandscapeHoles", GetRelative(context, filePath), -1, "Holes filename does not match the supported tile convention.");
					continue;
				}

				RawMapLandscapeTileSummaryData tile = GetTile(tiles, ParseCoordinate(match.Groups[1].Value), ParseCoordinate(match.Groups[2].Value));
				byte[] bytes = File.ReadAllBytes(filePath);
				tile.HasHoles = true;
				tile.HolesBytes = bytes.Length;
				tile.HolesSha256 = RawMapDecodeContext.CalculateSha256(bytes);
				summary.HoleTileCount++;
				if (bytes.Length != HOLES_BYTES)
				{
					summary.InvalidFileCount++;
					context.AddError("LandscapeHolesSize", GetRelative(context, filePath), -1, "Expected " + HOLES_BYTES + " bytes, found " + bytes.Length + ".");
					continue;
				}

				for (int offset = 1; offset < bytes.Length; ++offset)
				{
					byte value = bytes[offset];
					for (int bit = 0; bit < 8; ++bit)
					{
						if ((value & (1 << bit)) != 0)
							tile.HoleCount++;
					}
				}
				summary.TotalHoleCount += tile.HoleCount;
			}
		}

		internal static void DecodeObjects(RawMapDecodeContext context)
		{
			const string path = "Level/Objects.dat";
			string fullPath = context.GetFullPath(path);
			RawMapObjectSummaryData summary = context.Summary.Objects;
			summary.Present = File.Exists(fullPath);
			if (!summary.Present)
			{
				context.AddWarning("MissingObjectsFile", path, -1, "Aggregated Objects.dat is not present; legacy per-region object files are not decoded in this phase.");
				return;
			}

			summary.FileSizeInBytes = checked((int) new FileInfo(fullPath).Length);
			summary.ContentSha256 = RawMapDecodeContext.CalculateFileSha256(fullPath);
			StrictBinaryReader reader = StrictBinaryReader.FromFile(fullPath, path);
			summary.Version = reader.ReadByte();
			if (summary.Version <= 0)
			{
				FinishObjectSummary(context, summary, reader, path);
				return;
			}
			if (summary.Version > 1 && summary.Version < 3)
				reader.ReadUInt64();
			if (summary.Version > 8)
				summary.AvailableInstanceId = reader.ReadUInt32();

			HashSet<uint> instanceIds = new HashSet<uint>();
			for (int x = 0; x < REGION_AXIS_COUNT; ++x)
			{
				for (int y = 0; y < REGION_AXIS_COUNT; ++y)
				{
					int count = reader.ReadUInt16();
					if (count > 0)
						summary.RegionsWithObjects++;
					for (int index = 0; index < count; ++index)
					{
						RawMapVector3Data point = reader.ReadVector3();
						reader.ReadVector3(); // Euler rotation
						if (summary.Version > 3)
							reader.ReadVector3(); // scale
						int legacyId = reader.ReadUInt16();
						if (summary.Version > 5 && summary.Version < 10)
							reader.ReadByteLengthUtf8String();
						Guid guid = summary.Version > 7 ? reader.ReadUInt16LengthGuid() : Guid.Empty;
						if (summary.Version > 6)
							reader.ReadByte(); // placement origin
						if (summary.Version > 8)
						{
							uint instanceId = reader.ReadUInt32();
							if (!instanceIds.Add(instanceId))
								summary.DuplicateInstanceIdCount++;
						}
						if (summary.Version >= 11)
						{
							reader.ReadUInt16LengthGuid(); // material palette override, Guid.Empty is still encoded as 16 bytes
							reader.ReadInt32();
						}
						if (summary.Version >= 12)
							reader.ReadBoolean();
						if (legacyId == 0 && guid == Guid.Empty)
							summary.EmptyAssetReferenceCount++;
						RawMapDecodeContext.Include(summary.Bounds, point);
						summary.ObjectCount++;
					}
				}
			}

			FinishObjectSummary(context, summary, reader, path);
			if (summary.DuplicateInstanceIdCount > 0)
				context.AddError("DuplicateObjectInstanceId", path, -1, summary.DuplicateInstanceIdCount + " duplicate object instance ID(s) were found.");
		}

		private static void FinishObjectSummary(RawMapDecodeContext context, RawMapObjectSummaryData summary,
			StrictBinaryReader reader, string path)
		{
			summary.BytesConsumed = reader.Position;
			summary.TrailingBytes = reader.Remaining;
			if (summary.TrailingBytes > 0)
				context.AddWarning("TrailingBytes", path, reader.Position, summary.TrailingBytes + " trailing byte(s) were not consumed by the known object format.");
		}

		internal static void DecodeRoads(RawMapDecodeContext context)
		{
			RawMapRoadSummaryData summary = context.Summary.Roads;
			DecodeRoadMaterials(context, summary);
			DecodeRoadPaths(context, summary);
		}

		private static void DecodeRoadMaterials(RawMapDecodeContext context, RawMapRoadSummaryData summary)
		{
			const string path = "Environment/Roads.dat";
			string fullPath = context.GetFullPath(path);
			summary.MaterialsFilePresent = File.Exists(fullPath);
			if (!summary.MaterialsFilePresent)
				return;

			StrictBinaryReader reader = StrictBinaryReader.FromFile(fullPath, path);
			summary.MaterialsContentSha256 = RawMapDecodeContext.CalculateFileSha256(fullPath);
			summary.MaterialsVersion = reader.ReadByte();
			if (summary.MaterialsVersion > 0)
			{
				summary.MaterialCount = reader.ReadByte();
				for (int index = 0; index < summary.MaterialCount; ++index)
				{
					reader.ReadSingle(); // width
					reader.ReadSingle(); // height
					reader.ReadSingle(); // depth
					if (summary.MaterialsVersion > 1)
						reader.ReadSingle(); // offset
					reader.ReadBoolean(); // concrete
				}
			}
			if (reader.Remaining > 0)
				context.AddWarning("TrailingBytes", path, reader.Position, reader.Remaining + " trailing road-material byte(s) were not consumed.");
		}

		private static void DecodeRoadPaths(RawMapDecodeContext context, RawMapRoadSummaryData summary)
		{
			const string path = "Environment/Paths.dat";
			string fullPath = context.GetFullPath(path);
			summary.PathsFilePresent = File.Exists(fullPath);
			if (!summary.PathsFilePresent)
				return;

			StrictBinaryReader reader = StrictBinaryReader.FromFile(fullPath, path);
			summary.PathsContentSha256 = RawMapDecodeContext.CalculateFileSha256(fullPath);
			summary.PathsVersion = reader.ReadByte();
			if (summary.PathsVersion > 1)
			{
				summary.PathCount = reader.ReadUInt16();
				for (int pathIndex = 0; pathIndex < summary.PathCount; ++pathIndex)
				{
					int jointCount = reader.ReadUInt16();
					int material = reader.ReadByte();
					if (summary.MaterialCount > 0 && material >= summary.MaterialCount)
						summary.InvalidMaterialReferenceCount++;
					if (summary.PathsVersion > 2)
						reader.ReadBoolean(); // loop
					if (summary.PathsVersion >= 6)
						reader.ReadUInt16LengthGuid(); // road asset
					for (int jointIndex = 0; jointIndex < jointCount; ++jointIndex)
					{
						RawMapVector3Data point = reader.ReadVector3();
						if (summary.PathsVersion > 2)
						{
							reader.ReadVector3();
							reader.ReadVector3();
							reader.ReadByte(); // road mode
						}
						if (summary.PathsVersion > 4)
							reader.ReadSingle(); // offset
						if (summary.PathsVersion > 3)
							reader.ReadBoolean(); // ignore terrain
						RawMapDecodeContext.Include(summary.Bounds, point);
					}
					summary.JointCount += jointCount;
				}
			}
			else if (summary.PathsVersion > 0)
			{
				summary.PathCount = reader.ReadByte();
				for (int pathIndex = 0; pathIndex < summary.PathCount; ++pathIndex)
				{
					int jointCount = reader.ReadByte();
					int material = reader.ReadByte();
					if (summary.MaterialCount > 0 && material >= summary.MaterialCount)
						summary.InvalidMaterialReferenceCount++;
					for (int jointIndex = 0; jointIndex < jointCount; ++jointIndex)
						RawMapDecodeContext.Include(summary.Bounds, reader.ReadVector3());
					summary.JointCount += jointCount;
				}
			}

			summary.PathsBytesConsumed = reader.Position;
			summary.PathsTrailingBytes = reader.Remaining;
			if (summary.PathsTrailingBytes > 0)
				context.AddWarning("TrailingBytes", path, reader.Position, summary.PathsTrailingBytes + " trailing path byte(s) were not consumed.");
			if (summary.InvalidMaterialReferenceCount > 0)
				context.AddWarning("RoadMaterialReference", path, -1, summary.InvalidMaterialReferenceCount + " path(s) reference a material outside Roads.dat. The external road asset may still resolve it.");
		}

		internal static void DecodeHierarchy(RawMapDecodeContext context)
		{
			const string path = "Level.hierarchy";
			string fullPath = context.GetFullPath(path);
			RawMapHierarchySummaryData summary = context.Summary.Hierarchy;
			summary.Present = File.Exists(fullPath);
			if (!summary.Present)
				return;

			summary.FileSizeInBytes = new FileInfo(fullPath).Length;
			summary.ContentSha256 = RawMapDecodeContext.CalculateFileSha256(fullPath);
			Dictionary<string, int> typeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
			using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (StreamReader text = new StreamReader(stream))
			{
				IFormattedFileReader reader = new KeyValueTableReader(text);
				if (reader.containsKey("Available_Instance_ID"))
				{
					string value = reader.readValue("Available_Instance_ID");
					if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long availableId))
						context.AddError("HierarchyAvailableInstanceId", path, -1, "Available_Instance_ID is not an integer: " + value);
					else
						summary.AvailableInstanceId = availableId;
				}

				summary.ItemCount = reader.readArrayLength("Items");
				for (int index = 0; index < summary.ItemCount; ++index)
				{
					IFormattedFileReader item = reader.readObject(index);
					string typeName = item?.readValue("Type");
					if (string.IsNullOrWhiteSpace(typeName))
					{
						summary.MissingTypeCount++;
						continue;
					}
					typeCounts.TryGetValue(typeName, out int count);
					typeCounts[typeName] = count + 1;
				}
			}

			foreach (KeyValuePair<string, int> pair in typeCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal))
				summary.Types.Add(new RawMapHierarchyTypeCountData { TypeName = pair.Key, Count = pair.Value });
			if (summary.MissingTypeCount > 0)
				context.AddError("HierarchyMissingType", path, -1, summary.MissingTypeCount + " hierarchy item(s) are missing a Type value.");
		}

		private static RawMapLandscapeTileSummaryData GetTile(Dictionary<string, RawMapLandscapeTileSummaryData> tiles, int x, int y)
		{
			string key = x.ToString(CultureInfo.InvariantCulture) + "," + y.ToString(CultureInfo.InvariantCulture);
			if (!tiles.TryGetValue(key, out RawMapLandscapeTileSummaryData tile))
			{
				tile = new RawMapLandscapeTileSummaryData { X = x, Y = y };
				tiles.Add(key, tile);
			}
			return tile;
		}

		private static int ParseCoordinate(string value)
		{
			return int.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
		}

		private static string GetRelative(RawMapDecodeContext context, string fullPath)
		{
			string prefix = context.SourceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			return RawMapDecodeContext.NormalizePath(fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
				? fullPath.Substring(prefix.Length)
				: fullPath);
		}
	}
}
