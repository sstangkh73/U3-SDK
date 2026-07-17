////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using Newtonsoft.Json;
using SDG.Unturned.WorldUpgrade;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public static class RawMapManifestGenerator
	{
		public const string DefaultCatalogPath = "Builds/WorldUpgrade/WorldSourceCatalog.json";

		[MenuItem("Window/Unturned/World Upgrade/Generate Raw Map Manifests")]
		public static void GenerateConfiguredManifests()
		{
			string projectRoot = GetProjectRoot();
			string catalogPath = Path.Combine(projectRoot, DefaultCatalogPath);
			if (!File.Exists(catalogPath))
				throw new FileNotFoundException("World source catalog was not found.", catalogPath);

			RawMapSourceCatalogData catalog = JsonConvert.DeserializeObject<RawMapSourceCatalogData>(File.ReadAllText(catalogPath));
			if (catalog == null)
				throw new InvalidDataException("World source catalog deserialized to null: " + catalogPath);

			string outputDirectory = ResolveProjectOutputDirectory(projectRoot, catalog.OutputDirectory);
			Directory.CreateDirectory(outputDirectory);

			int generatedCount = 0;
			foreach (RawMapSourceEntryData source in catalog.Maps)
			{
				if (source == null || !source.Enabled)
					continue;

				RawMapManifestData manifest = RawMapInventoryBuilder.Build(source);
				string outputPath = Path.Combine(outputDirectory, source.ZoneId + ".raw-map-manifest.json");
				File.WriteAllText(outputPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
				Debug.Log("Generated raw map manifest: " + outputPath);
				generatedCount++;
			}

			Debug.LogFormat("Generated {0} raw map manifest(s). Legacy gameplay overrides were recorded as reference-only metadata.", generatedCount);
		}

		public static void GenerateFromCommandLine()
		{
			try
			{
				GenerateConfiguredManifests();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				EditorApplication.Exit(1);
				return;
			}

			EditorApplication.Exit(0);
		}

		private static string ResolveProjectOutputDirectory(string projectRoot, string configuredPath)
		{
			if (string.IsNullOrWhiteSpace(configuredPath))
				throw new InvalidDataException("OutputDirectory is required in the world source catalog.");

			string outputDirectory = Path.GetFullPath(Path.Combine(projectRoot, configuredPath));
			string projectPrefix = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			if (!outputDirectory.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
				throw new InvalidDataException("OutputDirectory must stay inside the Unity project: " + outputDirectory);

			return outputDirectory;
		}

		private static string GetProjectRoot()
		{
			return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
		}
	}
}
