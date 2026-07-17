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
	public static class RawMapDecodedSummaryGenerator
	{
		public const string DefaultOutputDirectory = "Builds/WorldUpgrade/DecodedMapSummaries";

		[MenuItem("Window/Unturned/World Upgrade/Generate Decoded Map Summaries")]
		public static void GenerateConfiguredDecodedSummaries()
		{
			List<RawMapDecodedSummaryData> summaries = GenerateConfiguredDecodedSummariesInternal();
			int invalidCount = summaries.Count(summary => !summary.IsValid);
			Debug.LogFormat("Generated {0} decoded map summary file(s); {1} invalid.", summaries.Count, invalidCount);
			if (invalidCount > 0)
				throw new InvalidDataException(invalidCount + " decoded map summary file(s) contain validation errors.");
		}

		public static List<RawMapDecodedSummaryData> GenerateConfiguredDecodedSummariesInternal()
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string catalogPath = Path.Combine(projectRoot, RawMapManifestGenerator.DefaultCatalogPath);
			if (!File.Exists(catalogPath))
				throw new FileNotFoundException("World source catalog was not found.", catalogPath);

			RawMapSourceCatalogData catalog = JsonConvert.DeserializeObject<RawMapSourceCatalogData>(File.ReadAllText(catalogPath));
			if (catalog == null)
				throw new InvalidDataException("World source catalog deserialized to null: " + catalogPath);

			string outputDirectory = Path.GetFullPath(Path.Combine(projectRoot, DefaultOutputDirectory));
			string projectPrefix = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			if (!outputDirectory.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
				throw new InvalidDataException("Decoded summary output must stay inside the Unity project.");
			Directory.CreateDirectory(outputDirectory);

			List<RawMapDecodedSummaryData> results = new List<RawMapDecodedSummaryData>();
			foreach (RawMapSourceEntryData source in catalog.Maps)
			{
				if (source == null || !source.Enabled)
					continue;

				string inventoryPath = Path.Combine(projectRoot, catalog.OutputDirectory,
					source.ZoneId + ".raw-map-manifest.json");
				if (!File.Exists(inventoryPath))
					throw new FileNotFoundException("Raw inventory manifest must be generated before decoding.", inventoryPath);
				RawMapManifestData inventory = JsonConvert.DeserializeObject<RawMapManifestData>(File.ReadAllText(inventoryPath));
				if (inventory == null)
					throw new InvalidDataException("Raw inventory manifest deserialized to null: " + inventoryPath);

				RawMapDecodedSummaryData summary = RawMapDataDecoder.Decode(source.ZoneId, source.DisplayName,
					source.SourceRoot, inventory.InventoryFingerprintSha256);
				string outputPath = Path.Combine(outputDirectory, source.ZoneId + ".decoded-map-summary.json");
				File.WriteAllText(outputPath, JsonConvert.SerializeObject(summary, Formatting.Indented));
				Debug.LogFormat("Generated decoded map summary: {0} (valid={1}, issues={2})",
					outputPath, summary.IsValid, summary.Issues.Count);
				results.Add(summary);
			}

			return results;
		}

		public static void GenerateFromCommandLine()
		{
			try
			{
				GenerateConfiguredDecodedSummaries();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				EditorApplication.Exit(1);
				return;
			}

			EditorApplication.Exit(0);
		}
	}
}
