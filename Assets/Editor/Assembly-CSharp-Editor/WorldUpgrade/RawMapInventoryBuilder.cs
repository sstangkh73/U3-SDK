////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using Newtonsoft.Json.Linq;
using SDG.Unturned.WorldUpgrade;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public static class RawMapInventoryBuilder
	{
		public static RawMapManifestData Build(RawMapSourceEntryData source)
		{
			ValidateSource(source);

			string sourceRoot = Path.GetFullPath(source.SourceRoot);
			RawMapManifestData manifest = new RawMapManifestData
			{
				ZoneId = source.ZoneId,
				DisplayName = string.IsNullOrEmpty(source.DisplayName) ? source.ZoneId : source.DisplayName,
				SourceType = source.SourceType,
				PublishedFileId = source.PublishedFileId,
				SourceRoot = NormalizePath(sourceRoot),
				GeneratedUtc = DateTime.UtcNow.ToString("o"),
			};

			manifest.Level = ReadLevelDescriptor(Path.Combine(sourceRoot, "Level.dat"));
			manifest.Config = ReadConfigReference(Path.Combine(sourceRoot, "Config.json"));

			List<FileInfo> files = Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
				.Select(path => new FileInfo(path))
				.OrderBy(file => GetRelativePath(sourceRoot, file.FullName), StringComparer.OrdinalIgnoreCase)
				.ToList();

			Dictionary<ERawMapFileCategory, RawMapCategorySummaryData> categorySummaries = new Dictionary<ERawMapFileCategory, RawMapCategorySummaryData>();
			foreach (FileInfo file in files)
			{
				string relativePath = GetRelativePath(sourceRoot, file.FullName);
				ERawMapFileCategory category = Classify(relativePath);
				manifest.Files.Add(new RawMapFileRecordData
				{
					RelativePath = relativePath,
					SizeInBytes = file.Length,
					Category = category,
				});

				if (!categorySummaries.TryGetValue(category, out RawMapCategorySummaryData summary))
				{
					summary = new RawMapCategorySummaryData { Category = category };
					categorySummaries.Add(category, summary);
				}

				summary.FileCount++;
				summary.SizeInBytes += file.Length;
			}

			manifest.Categories = categorySummaries.Values.OrderBy(summary => summary.Category).ToList();
			manifest.InventoryFingerprintSha256 = CalculateInventoryFingerprint(sourceRoot, files);

			foreach (string dependencyRoot in source.DependencyRoots ?? Enumerable.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(dependencyRoot))
					continue;

				manifest.Dependencies.Add(BuildDependencySummary(dependencyRoot));
			}

			return manifest;
		}

		public static RawMapLevelDescriptorData ReadLevelDescriptor(string levelFilePath)
		{
			if (!File.Exists(levelFilePath))
				throw new FileNotFoundException("Raw map is missing Level.dat", levelFilePath);

			byte[] data = File.ReadAllBytes(levelFilePath);
			if (data.Length < 10)
				throw new InvalidDataException("Level.dat is too short to contain version, owner, and size fields.");

			byte formatVersion = data[0];
			byte sizeValue = data[9];
			byte typeValue = formatVersion > 1
				? data.Length > 10 ? data[10] : throw new InvalidDataException("Level.dat version requires a type field, but the file is truncated.")
				: (byte) 0;

			return new RawMapLevelDescriptorData
			{
				FormatVersion = formatVersion,
				SizeValue = sizeValue,
				SizeName = GetLevelSizeName(sizeValue),
				SizeInWorldUnits = GetLevelSizeInWorldUnits(sizeValue),
				TypeValue = typeValue,
				TypeName = GetLevelTypeName(typeValue),
			};
		}

		private static RawMapConfigReferenceData ReadConfigReference(string configFilePath)
		{
			RawMapConfigReferenceData result = new RawMapConfigReferenceData
			{
				RelativePath = "Config.json",
			};

			if (!File.Exists(configFilePath))
			{
				result.ConfigParseError = "Config.json was not present.";
				return result;
			}

			try
			{
				JObject config = JObject.Parse(File.ReadAllText(configFilePath));
				result.Version = config.Value<string>("Version");
				result.AssetGuid = config["Asset"]?.Value<string>("GUID");
				CopyArrayValues(config["RequiredWorkshopFileIds"] as JArray, result.RequiredWorkshopFileIds);
				CopyPropertyNames(config["Mode_Config_Overrides"] as JObject, result.CommonGameplayOverrideKeys);
				CopyPropertyNames(config["EasyDifficulty_Config_Overrides"] as JObject, result.EasyGameplayOverrideKeys);
				CopyPropertyNames(config["NormalDifficulty_Config_Overrides"] as JObject, result.NormalGameplayOverrideKeys);
				CopyPropertyNames(config["HardDifficulty_Config_Overrides"] as JObject, result.HardGameplayOverrideKeys);
				result.TrainAssociationCount = (config["Trains"] as JArray)?.Count ?? 0;
				result.SpawnLoadoutCount = (config["Spawn_Loadouts"] as JArray)?.Count ?? 0;
			}
			catch (Exception exception)
			{
				result.ConfigParseError = exception.Message;
			}

			return result;
		}

		private static void CopyPropertyNames(JObject source, List<string> destination)
		{
			if (source == null)
				return;

			foreach (JProperty property in source.Properties().OrderBy(property => property.Name, StringComparer.Ordinal))
				destination.Add(property.Name);
		}

		private static void CopyArrayValues(JArray source, List<string> destination)
		{
			if (source == null)
				return;

			foreach (JToken value in source)
				destination.Add(value.ToString());
		}

		private static RawMapDependencySummaryData BuildDependencySummary(string dependencyRoot)
		{
			string fullPath = Path.GetFullPath(dependencyRoot);
			if (!Directory.Exists(fullPath))
				throw new DirectoryNotFoundException("Raw map dependency root does not exist: " + fullPath);

			List<FileInfo> files = Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories)
				.Select(path => new FileInfo(path))
				.OrderBy(file => GetRelativePath(fullPath, file.FullName), StringComparer.OrdinalIgnoreCase)
				.ToList();

			return new RawMapDependencySummaryData
			{
				SourceRoot = NormalizePath(fullPath),
				FileCount = files.Count,
				SizeInBytes = files.Sum(file => file.Length),
				InventoryFingerprintSha256 = CalculateInventoryFingerprint(fullPath, files),
			};
		}

		private static string CalculateInventoryFingerprint(string root, List<FileInfo> files)
		{
			StringBuilder inventory = new StringBuilder();
			foreach (FileInfo file in files)
			{
				inventory.Append(GetRelativePath(root, file.FullName).ToLowerInvariant());
				inventory.Append('|');
				inventory.Append(file.Length);
				inventory.Append('\n');
			}

			using (SHA256 sha256 = SHA256.Create())
			{
				byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(inventory.ToString()));
				return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
			}
		}

		private static ERawMapFileCategory Classify(string relativePath)
		{
			string path = relativePath.ToLowerInvariant();
			if (path == "level.dat" || path == "config.json" || path.EndsWith(".localization"))
				return ERawMapFileCategory.Metadata;
			if (path.StartsWith("landscape/"))
				return ERawMapFileCategory.Landscape;
			if (path.StartsWith("terrain/"))
				return ERawMapFileCategory.Terrain;
			if (path == "level.hierarchy" || path == "foliage.blob" || path.StartsWith("level/"))
				return ERawMapFileCategory.WorldObjects;
			if (path.StartsWith("environment/navigation_"))
				return ERawMapFileCategory.Navigation;
			if (path.StartsWith("environment/"))
				return ERawMapFileCategory.Environment;
			if (path.StartsWith("spawns/"))
				return ERawMapFileCategory.Spawns;
			if (path == "map.png" || path == "chart.png" || path.EndsWith("/map.png") || path.EndsWith("/chart.png"))
				return ERawMapFileCategory.MapImage;
			if (path.EndsWith(".unity3d") || path.EndsWith(".masterbundle"))
				return ERawMapFileCategory.AssetBundle;

			return ERawMapFileCategory.Other;
		}

		private static void ValidateSource(RawMapSourceEntryData source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (string.IsNullOrWhiteSpace(source.ZoneId))
				throw new ArgumentException("ZoneId is required.", nameof(source));
			if (source.ZoneId.Any(character => !((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '-')))
				throw new ArgumentException("ZoneId may only contain lowercase letters, digits, and hyphens: " + source.ZoneId, nameof(source));
			if (string.IsNullOrWhiteSpace(source.SourceRoot) || !Directory.Exists(source.SourceRoot))
				throw new DirectoryNotFoundException("Raw map source root does not exist: " + source.SourceRoot);
		}

		private static string GetRelativePath(string root, string fullPath)
		{
			string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			string normalizedFullPath = Path.GetFullPath(fullPath);
			if (!normalizedFullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("File is outside inventory root: " + normalizedFullPath);

			return NormalizePath(normalizedFullPath.Substring(normalizedRoot.Length));
		}

		private static string NormalizePath(string path)
		{
			return path.Replace('\\', '/');
		}

		private static string GetLevelSizeName(byte value)
		{
			switch (value)
			{
				case 0: return "Tiny";
				case 1: return "Small";
				case 2: return "Medium";
				case 3: return "Large";
				case 4: return "Insane";
				default: return "Unknown";
			}
		}

		private static int GetLevelSizeInWorldUnits(byte value)
		{
			switch (value)
			{
				case 0: return 512;
				case 1: return 1024;
				case 2: return 2048;
				case 3: return 4096;
				case 4: return 8192;
				default: return 0;
			}
		}

		private static string GetLevelTypeName(byte value)
		{
			switch (value)
			{
				case 0: return "Survival";
				case 1: return "Horde";
				case 2: return "Arena";
				default: return "Unknown";
			}
		}
	}
}
