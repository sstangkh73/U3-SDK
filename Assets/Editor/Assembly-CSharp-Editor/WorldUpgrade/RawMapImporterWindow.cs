////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using UnityEditor;
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public class RawMapImporterWindow : EditorWindow
	{
		[MenuItem("Window/Unturned/World Upgrade/Raw Map Importer")]
		public static void ShowWindow()
		{
			GetWindow<RawMapImporterWindow>();
		}

		private void OnEnable()
		{
			titleContent = new GUIContent("Raw Map Importer");
		}

		private void OnGUI()
		{
			EditorGUILayout.HelpBox(
				"Reads map-authored files from the external Steam installation and writes derived inventory manifests. " +
				"It does not copy source content and never imports legacy gameplay overrides as runtime rules.",
				MessageType.Info);

			EditorGUILayout.LabelField("Source catalog", RawMapManifestGenerator.DefaultCatalogPath);
			EditorGUILayout.Space();

			if (GUILayout.Button("Generate Configured Raw Map Manifests", GUILayout.Height(32.0f)))
			{
				try
				{
					RawMapManifestGenerator.GenerateConfiguredManifests();
					ShowNotification(new GUIContent("Raw map manifests generated"));
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					EditorUtility.DisplayDialog("Raw Map Import Failed", exception.Message, "OK");
				}
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Import boundary", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Included: terrain, objects, environment, navigation, spawns, dependencies", EditorStyles.wordWrappedLabel);
			EditorGUILayout.LabelField("Reference only: legacy item, zombie, difficulty, survival, vehicle, event rules", EditorStyles.wordWrappedLabel);
		}
	}
}
