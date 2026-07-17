////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SDG.Unturned.WorldUpgrade
{
	public sealed class WorldCellBundleRepository : IDisposable
	{
		private readonly string schemaRoot;
		private readonly Dictionary<string, WorldCellIndexData> cellIndices;
		private readonly Dictionary<string, WorldCellData> loadedCells = new Dictionary<string, WorldCellData>(StringComparer.Ordinal);

		public WorldManifestData Manifest { get; }
		public WorldZoneDefinitionData Zone { get; }
		public int LoadedCellCount => loadedCells.Count;
		public int LoadedEntityCount => loadedCells.Values.Sum(cell => cell.Entities.Count);
		public long EstimatedResidentBytes => loadedCells.Values.Sum(EstimateCellBytes);

		public WorldCellBundleRepository(string schemaRoot, string zoneKey)
		{
			if (string.IsNullOrWhiteSpace(schemaRoot))
				throw new ArgumentException("Schema root is required.", nameof(schemaRoot));
			if (string.IsNullOrWhiteSpace(zoneKey))
				throw new ArgumentException("Zone key is required.", nameof(zoneKey));
			this.schemaRoot = Path.GetFullPath(schemaRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			Manifest = ReadJson<WorldManifestData>(Resolve("world-manifest.json"));
			WorldZoneIndexData zoneIndex = Manifest.Zones.SingleOrDefault(zone => string.Equals(zone.ZoneKey, zoneKey, StringComparison.Ordinal));
			if (zoneIndex == null)
				throw new InvalidDataException("World manifest does not contain zone: " + zoneKey);
			Zone = ReadJson<WorldZoneDefinitionData>(Resolve(zoneIndex.RelativePath));
			if (!string.Equals(Zone.ZoneId, zoneIndex.ZoneId, StringComparison.Ordinal))
				throw new InvalidDataException("Zone definition ID does not match world manifest index.");
			cellIndices = Zone.Cells.ToDictionary(cell => cell.CellId, StringComparer.Ordinal);
		}

		public IEnumerable<WorldCellIndexData> EnumerateCellIndices()
		{
			return Zone.Cells;
		}

		public WorldCellData LoadCell(string cellId)
		{
			if (loadedCells.TryGetValue(cellId, out WorldCellData loaded))
				return loaded;
			if (!cellIndices.TryGetValue(cellId, out WorldCellIndexData index))
				throw new KeyNotFoundException("Zone does not contain cell ID: " + cellId);
			WorldCellData cell = ReadJson<WorldCellData>(Resolve(index.RelativePath));
			if (!string.Equals(cell.CellId, cellId, StringComparison.Ordinal) || !string.Equals(cell.ZoneId, Zone.ZoneId, StringComparison.Ordinal))
				throw new InvalidDataException("World cell identity does not match its zone index: " + index.RelativePath);
			if (cell.Entities.Any(entity => !string.Equals(entity.OwnerCellId, cellId, StringComparison.Ordinal)))
				throw new InvalidDataException("World cell contains entity assigned to a different owner: " + cellId);
			loadedCells.Add(cellId, cell);
			return cell;
		}

		public bool UnloadCell(string cellId)
		{
			return loadedCells.Remove(cellId);
		}

		public void UnloadAll()
		{
			loadedCells.Clear();
		}

		public void Dispose()
		{
			UnloadAll();
		}

		private string Resolve(string relativePath)
		{
			string fullPath = Path.GetFullPath(Path.Combine(schemaRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
			string prefix = schemaRoot + Path.DirectorySeparatorChar;
			if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				throw new InvalidDataException("Schema path escapes configured root: " + relativePath);
			return fullPath;
		}

		private static T ReadJson<T>(string path)
		{
			if (!File.Exists(path))
				throw new FileNotFoundException("World schema file is missing.", path);
			T value = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
			if (value == null)
				throw new InvalidDataException("World schema JSON deserialized to null: " + path);
			return value;
		}

		private static long EstimateCellBytes(WorldCellData cell)
		{
			long bytes = 256;
			foreach (WorldEntityRecordData entity in cell.Entities)
			{
				bytes += 256;
				bytes += EstimateString(entity.EntityId) + EstimateString(entity.OwnerCellId) + EstimateString(entity.Kind) +
					EstimateString(entity.SourceKey) + EstimateString(entity.AssetGuid) + EstimateString(entity.ContentFingerprintSha256);
			}
			return bytes;
		}

		private static long EstimateString(string value)
		{
			return value == null ? 0 : 24 + (value.Length * 2L);
		}
	}
}
