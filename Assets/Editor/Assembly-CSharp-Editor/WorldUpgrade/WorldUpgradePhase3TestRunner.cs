////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public static class WorldUpgradePhase3TestRunner
	{
		private const string ActiveKey = "WorldUpgrade.Phase3.Active";
		private const string StartedTicksKey = "WorldUpgrade.Phase3.StartedTicks";

		[InitializeOnLoadMethod]
		private static void InstallTimeoutWatcher()
		{
			EditorApplication.update -= WatchTimeout;
			EditorApplication.update += WatchTimeout;
		}

		public static void RunFromCommandLine()
		{
			if (!Environment.GetCommandLineArgs().Any(value => string.Equals(value, "-WorldUpgradePhase3Validation", StringComparison.OrdinalIgnoreCase)))
				throw new InvalidOperationException("Phase 3 runner requires -WorldUpgradePhase3Validation.");
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string evidencePath = Path.Combine(projectRoot, "Builds", "WorldUpgrade", "WorldSchemas", "u3-connected-world", "phase-3-single-zone-runtime-evidence.json");
			if (File.Exists(evidencePath))
				File.Delete(evidencePath);

			SessionState.SetBool("WorldUpgrade.Phase3.HadAutoLoadLevel", EditorPrefs.HasKey("AutoLoadLevel"));
			SessionState.SetString("WorldUpgrade.Phase3.OldAutoLoadLevel", EditorPrefs.GetString("AutoLoadLevel", string.Empty));
			SessionState.SetBool("WorldUpgrade.Phase3.HadAutoLoadMode", EditorPrefs.HasKey("AutoLoadMode"));
			SessionState.SetInt("WorldUpgrade.Phase3.OldAutoLoadMode", EditorPrefs.GetInt("AutoLoadMode", 0));
			SessionState.SetBool(ActiveKey, true);
			SessionState.SetString(StartedTicksKey, DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
			// The discovered workshop level is named "California2" (without a space).
			// Editor mode loads Landscape and LevelObjects without requiring a Steam player session.
			EditorPrefs.SetString("AutoLoadLevel", "California2");
			EditorPrefs.SetInt("AutoLoadMode", 1);
			EditorSceneManager.OpenScene("Assets/GameStartup.unity", OpenSceneMode.Single);
			EditorApplication.isPlaying = true;
		}

		private static void WatchTimeout()
		{
			if (!SessionState.GetBool(ActiveKey, false))
				return;
			if (!long.TryParse(SessionState.GetString(StartedTicksKey, string.Empty), out long ticks))
				return;
			if (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) < TimeSpan.FromMinutes(5))
				return;
			Debug.LogError("World Upgrade Phase 3 runtime validation timed out after five minutes.");
			RestorePreferences();
			SessionState.SetBool(ActiveKey, false);
			EditorApplication.Exit(1);
		}

		private static void RestorePreferences()
		{
			if (SessionState.GetBool("WorldUpgrade.Phase3.HadAutoLoadLevel", false))
				EditorPrefs.SetString("AutoLoadLevel", SessionState.GetString("WorldUpgrade.Phase3.OldAutoLoadLevel", string.Empty));
			else
				EditorPrefs.DeleteKey("AutoLoadLevel");
			if (SessionState.GetBool("WorldUpgrade.Phase3.HadAutoLoadMode", false))
				EditorPrefs.SetInt("AutoLoadMode", SessionState.GetInt("WorldUpgrade.Phase3.OldAutoLoadMode", 0));
			else
				EditorPrefs.DeleteKey("AutoLoadMode");
		}
	}
}
