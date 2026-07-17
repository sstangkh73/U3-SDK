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
	public static class WorldSchemaGenerator
	{
		public const string DefaultLayoutPath = "Builds/WorldUpgrade/WorldLayout.json";
		public const string DefaultOutputDirectory = "Builds/WorldUpgrade/WorldSchemas";

		[MenuItem("Window/Unturned/World Upgrade/Generate Deterministic World Schema")]
		public static void GenerateConfiguredWorldSchema()
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string layoutPath = Path.Combine(projectRoot, DefaultLayoutPath);
			string catalogPath = Path.Combine(projectRoot, RawMapManifestGenerator.DefaultCatalogPath);
			WorldLayoutSourceData layout = ReadJson<WorldLayoutSourceData>(layoutPath, "world layout");
			RawMapSourceCatalogData catalog = ReadJson<RawMapSourceCatalogData>(catalogPath, "world source catalog");
			List<WorldSchemaZoneSource> sources = LoadSources(projectRoot, layout, catalog);

			WorldSchemaBuildResult first = WorldSchemaBuilder.Build(layout, sources);
			WorldSchemaBuildResult repeat = WorldSchemaBuilder.Build(layout, sources);
			bool deterministic = string.Equals(first.Manifest.ContentFingerprintSha256, repeat.Manifest.ContentFingerprintSha256, StringComparison.Ordinal) &&
				string.Equals(first.Manifest.EntityIdentityFingerprintSha256, repeat.Manifest.EntityIdentityFingerprintSha256, StringComparison.Ordinal);

			string outputRoot = ResolveInsideProject(projectRoot, Path.Combine(DefaultOutputDirectory, layout.WorldKey));
			List<WorldEntityRecordData> previousEntities = LoadExistingEntities(outputRoot);
			WorldSchemaDiffData sourceDiff = WorldSchemaDiffUtility.Compare(previousEntities, first.Entities);
			WriteBuild(outputRoot, first, sourceDiff, deterministic, repeat);

			Debug.LogFormat("Generated world schema {0}: zones={1}, cells={2}, entities={3}, migrations={4}, deterministic={5}, valid={6}",
				first.Manifest.WorldId, first.Manifest.Validation.ZoneCount, first.Manifest.Validation.CellCount,
				first.Manifest.Validation.EntityCount, first.Manifest.Validation.MigrationCount, deterministic,
				first.Manifest.Validation.IsValid);
			if (!deterministic)
				throw new InvalidDataException("Repeated world schema generation changed deterministic fingerprints.");
			if (!first.Manifest.Validation.IsValid)
				throw new InvalidDataException("World schema validation failed with " + first.Manifest.Validation.ErrorCount + " error(s).");
		}

		public static void GenerateFromCommandLine()
		{
			try
			{
				GenerateConfiguredWorldSchema();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				EditorApplication.Exit(1);
				return;
			}
			EditorApplication.Exit(0);
		}

		private static List<WorldSchemaZoneSource> LoadSources(string projectRoot, WorldLayoutSourceData layout,
			RawMapSourceCatalogData catalog)
		{
			Dictionary<string, RawMapSourceEntryData> catalogByKey = catalog.Maps.Where(map => map != null && map.Enabled)
				.ToDictionary(map => map.ZoneId, StringComparer.Ordinal);
			List<WorldSchemaZoneSource> sources = new List<WorldSchemaZoneSource>();
			foreach (WorldZoneLayoutSourceData zoneLayout in layout.Zones)
			{
				if (!catalogByKey.TryGetValue(zoneLayout.ZoneKey, out RawMapSourceEntryData source))
					throw new InvalidDataException("World layout zone is missing from enabled source catalog: " + zoneLayout.ZoneKey);
				string inventoryPath = Path.Combine(projectRoot, catalog.OutputDirectory, source.ZoneId + ".raw-map-manifest.json");
				RawMapManifestData inventory = ReadJson<RawMapManifestData>(inventoryPath, "raw map inventory");
				RawMapDetailedDecodeResult decode = RawMapDataDecoder.DecodeDetailed(source.ZoneId, source.DisplayName,
					source.SourceRoot, inventory.InventoryFingerprintSha256);
				sources.Add(new WorldSchemaZoneSource
				{
					ZoneKey = source.ZoneId,
					DisplayName = source.DisplayName,
					Inventory = inventory,
					Decode = decode,
				});
			}
			return sources;
		}

		private static void WriteBuild(string outputRoot, WorldSchemaBuildResult build, WorldSchemaDiffData sourceDiff,
			bool deterministic, WorldSchemaBuildResult repeat)
		{
			Directory.CreateDirectory(outputRoot);
			foreach (WorldSchemaZoneBuildResult zone in build.Zones)
			{
				string zonePath = ResolveInsideRoot(outputRoot, "zones/" + zone.Definition.ZoneKey + ".zone-definition.json");
				Directory.CreateDirectory(Path.GetDirectoryName(zonePath));
				WriteJson(zonePath, zone.Definition);
				Dictionary<string, WorldCellData> cellsById = zone.Cells.ToDictionary(cell => cell.CellId, StringComparer.Ordinal);
				foreach (WorldCellIndexData cellIndex in zone.Definition.Cells)
				{
					string cellPath = ResolveInsideRoot(outputRoot, cellIndex.RelativePath);
					Directory.CreateDirectory(Path.GetDirectoryName(cellPath));
					WriteJson(cellPath, cellsById[cellIndex.CellId]);
				}
			}

			WriteJson(Path.Combine(outputRoot, "world-manifest.json"), build.Manifest);
			WriteJson(Path.Combine(outputRoot, "source-update-diff.json"), sourceDiff);
			WorldSchemaGenerationEvidenceData evidence = new WorldSchemaGenerationEvidenceData
			{
				WorldId = build.Manifest.WorldId,
				IsValid = build.Manifest.Validation.IsValid && deterministic,
				DeterministicRepeatBuild = deterministic,
				FirstContentFingerprintSha256 = build.Manifest.ContentFingerprintSha256,
				RepeatContentFingerprintSha256 = repeat.Manifest.ContentFingerprintSha256,
				FirstEntityIdentityFingerprintSha256 = build.Manifest.EntityIdentityFingerprintSha256,
				RepeatEntityIdentityFingerprintSha256 = repeat.Manifest.EntityIdentityFingerprintSha256,
				ZoneFileCount = build.Zones.Count,
				CellFileCount = build.Zones.Sum(zone => zone.Cells.Count),
				Validation = build.Manifest.Validation,
				SourceUpdateDiff = sourceDiff,
			};
			WriteJson(Path.Combine(outputRoot, "generation-evidence.json"), evidence);
		}

		private static List<WorldEntityRecordData> LoadExistingEntities(string outputRoot)
		{
			string manifestPath = Path.Combine(outputRoot, "world-manifest.json");
			if (!File.Exists(manifestPath))
				return new List<WorldEntityRecordData>();

			WorldManifestData manifest = ReadJson<WorldManifestData>(manifestPath, "existing world manifest");
			List<WorldEntityRecordData> result = new List<WorldEntityRecordData>();
			foreach (WorldZoneIndexData zoneIndex in manifest.Zones)
			{
				WorldZoneDefinitionData zone = ReadJson<WorldZoneDefinitionData>(ResolveInsideRoot(outputRoot, zoneIndex.RelativePath),
					"existing zone definition");
				foreach (WorldCellIndexData cellIndex in zone.Cells)
				{
					string cellPath = ResolveInsideRoot(outputRoot, cellIndex.RelativePath);
					if (!File.Exists(cellPath))
						throw new FileNotFoundException("Existing schema cell referenced by zone definition is missing.", cellPath);
					WorldCellData cell = ReadJson<WorldCellData>(cellPath, "existing world cell");
					result.AddRange(cell.Entities);
				}
			}
			return result;
		}

		private static T ReadJson<T>(string path, string description)
		{
			if (!File.Exists(path))
				throw new FileNotFoundException("Required " + description + " was not found.", path);
			T value = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
			if (value == null)
				throw new InvalidDataException(description + " deserialized to null: " + path);
			return value;
		}

		private static void WriteJson(string path, object value)
		{
			File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented));
		}

		private static string ResolveInsideProject(string projectRoot, string relativePath)
		{
			return ResolveInsideRoot(projectRoot, relativePath);
		}

		private static string ResolveInsideRoot(string root, string relativePath)
		{
			string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
			string prefix = fullRoot + Path.DirectorySeparatorChar;
			if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				throw new InvalidDataException("World schema path escapes its configured root: " + relativePath);
			return fullPath;
		}
	}
}
