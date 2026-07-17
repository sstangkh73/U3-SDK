////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using Newtonsoft.Json;
using SDG.Framework.Landscapes;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SDG.Unturned.WorldUpgrade
{
	internal sealed class WorldUpgradePhase3ValidationBootstrap : MonoBehaviour
	{
		private const string ValidationFlag = "-WorldUpgradePhase3Validation";
		private const string ZoneKey = "california-2";
		private const int CellCycleCount = 5;
		private bool hasStarted;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Install()
		{
			if (!Environment.GetCommandLineArgs().Any(argument => string.Equals(argument, ValidationFlag, StringComparison.OrdinalIgnoreCase)))
				return;
			GameObject host = new GameObject("WorldUpgrade_Phase3_Validation");
			DontDestroyOnLoad(host);
			host.AddComponent<WorldUpgradePhase3ValidationBootstrap>();
		}

		private void OnEnable()
		{
			Level.onPostLevelLoaded += HandlePostLevelLoaded;
		}

		private void OnDisable()
		{
			Level.onPostLevelLoaded -= HandlePostLevelLoaded;
		}

		private void HandlePostLevelLoaded(int level)
		{
			if (hasStarted || Level.info == null || !MapNamesMatch(Level.info.name, "California 2"))
				return;
			hasStarted = true;
			StartCoroutine(RunValidation());
		}

		private IEnumerator RunValidation()
		{
			for (int frame = 0; frame < 5; ++frame)
				yield return null;

			WorldSingleZoneRuntimeEvidenceData evidence = null;
			try
			{
				string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
				string schemaRoot = Path.Combine(projectRoot, "Builds", "WorldUpgrade", "WorldSchemas", "u3-connected-world");
				using (WorldCellBundleRepository repository = new WorldCellBundleRepository(schemaRoot, ZoneKey))
				{
					evidence = WorldSingleZoneParityValidator.Validate(repository, Level.info.path, CellCycleCount);
					RunSmokeInstantiation(repository, Level.info.path, evidence);
				}
				FinalizeEvidence(evidence);
				WriteEvidence(projectRoot, evidence);
			}
			catch (Exception exception)
			{
				evidence ??= new WorldSingleZoneRuntimeEvidenceData { ZoneKey = ZoneKey, ActiveMapName = Level.info?.name };
				evidence.Issues.Add(new WorldRuntimeIssueData
				{
					Severity = "Error",
					Code = "ValidationException",
					ScopeId = ZoneKey,
					Message = exception.GetType().Name + ": " + exception.Message,
				});
				FinalizeEvidence(evidence);
				try
				{
					string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
					WriteEvidence(projectRoot, evidence);
				}
				catch (Exception writeException)
				{
					Debug.LogException(writeException);
				}
				Debug.LogException(exception);
			}

			Debug.LogFormat("World Upgrade Phase 3 validation complete: valid={0}, errors={1}, warnings={2}",
				evidence?.IsValid, evidence?.ErrorCount, evidence?.WarningCount);
			ExitEditor(evidence?.IsValid == true ? 0 : 1);
		}

		private static void RunSmokeInstantiation(WorldCellBundleRepository repository, string sourceRoot,
			WorldSingleZoneRuntimeEvidenceData evidence)
		{
			WorldCellIndexData smokeIndex = repository.EnumerateCellIndices()
				.Where(index => index.LandscapeHasHeightmap && index.RecordCounts.Any(count => count.Kind == "StaticObject" && count.Count > 0))
				.OrderBy(index => index.RecordCounts.First(count => count.Kind == "StaticObject").Count)
				.ThenBy(index => index.CellId, StringComparer.Ordinal)
				.FirstOrDefault();
			if (smokeIndex == null)
			{
				AddError(evidence, "SmokeCellMissing", repository.Zone.ZoneId, "No heightmap cell with a static object was available for smoke instantiation.");
				return;
			}

			evidence.SmokeCellId = smokeIndex.CellId;
			WorldCellData cell = repository.LoadCell(smokeIndex.CellId);
			WorldRuntimeTerrainPayload payload = WorldRuntimeTerrainDecoder.Decode(sourceRoot, smokeIndex.GridX, smokeIndex.GridZ);
			LandscapeTile baseline = Landscape.getTile(new LandscapeCoord(smokeIndex.GridX, smokeIndex.GridZ));
			TerrainLayer[] layers = baseline?.data?.terrainLayers;
			Vector3 offset = new Vector3(16384f, 0f, 0f);
			GameObject terrain = null;
			GameObject objectInstance = null;
			WorldAssetRegistry assetRegistry = new WorldAssetRegistry();
			try
			{
				terrain = WorldRuntimePreviewFactory.CreateTerrain(payload, layers, offset);
				evidence.SmokeTerrainCreated = terrain != null && terrain.GetComponent<Terrain>() != null;
				evidence.SmokeTerrainColliderCreated = terrain != null && terrain.GetComponent<TerrainCollider>()?.terrainData != null;
				WorldEntityRecordData objectRecord = cell.Entities.First(entity => entity.Kind == "StaticObject");
				objectInstance = WorldRuntimePreviewFactory.CreateObject(objectRecord, offset, assetRegistry);
				evidence.SmokeObjectCreated = objectInstance != null;
				if (objectInstance != null)
				{
					evidence.SmokeObjectColliderCount = objectInstance.GetComponentsInChildren<Collider>(true).Length;
					evidence.SmokeObjectRendererCount = objectInstance.GetComponentsInChildren<Renderer>(true).Length;
				}
				if (!evidence.SmokeTerrainCreated || !evidence.SmokeTerrainColliderCreated)
					AddError(evidence, "SmokeTerrainFailed", smokeIndex.CellId, "Cell terrain/collider smoke instantiation failed.");
				if (!evidence.SmokeObjectCreated)
					AddError(evidence, "SmokeObjectFailed", smokeIndex.CellId, "Cell object prefab smoke instantiation failed.");
			}
			finally
			{
				TerrainData terrainData = terrain != null ? terrain.GetComponent<Terrain>()?.terrainData : null;
#if UNITY_EDITOR
				if (objectInstance != null)
					DestroyImmediate(objectInstance);
				if (terrain != null)
					DestroyImmediate(terrain);
				if (terrainData != null)
					DestroyImmediate(terrainData);
#else
				if (objectInstance != null)
					Destroy(objectInstance);
				WorldRuntimePreviewFactory.DestroyTerrain(terrain);
#endif
				repository.UnloadCell(smokeIndex.CellId);
				evidence.AssetRegistryPeakObjectAssetCount = assetRegistry.PeakObjectAssetCount;
				evidence.AssetRegistryPeakReferenceCount = assetRegistry.PeakObjectAssetReferenceCount;
				evidence.AssetRegistryFinalObjectAssetCount = assetRegistry.ActiveObjectAssetCount;
				evidence.AssetRegistryFinalReferenceCount = assetRegistry.TotalObjectAssetReferenceCount;
				assetRegistry.Dispose();
			}

			evidence.SmokeObjectsDestroyed = objectInstance == null;
			evidence.SmokeTerrainDestroyed = terrain == null;
			if (!evidence.SmokeObjectsDestroyed || !evidence.SmokeTerrainDestroyed)
				AddError(evidence, "SmokeUnloadFailed", smokeIndex.CellId, "Smoke GameObjects remained alive after unload frame.");
			if (evidence.AssetRegistryFinalObjectAssetCount != 0 || evidence.AssetRegistryFinalReferenceCount != 0)
				AddError(evidence, "AssetRegistryReferenceLeak", smokeIndex.CellId, "World asset registry retained references after smoke object destruction.");
		}

		private static void FinalizeEvidence(WorldSingleZoneRuntimeEvidenceData evidence)
		{
			if (evidence == null)
				return;
			if (evidence.BaselineObjectMissingCount > 0)
				AddErrorOnce(evidence, "BaselineObjectMissing", evidence.ZoneId, "One or more stable source instance IDs are missing from active LevelObjects.");
			if (evidence.ObjectGuidMismatchCount > 0)
				AddErrorOnce(evidence, "ObjectGuidMismatch", evidence.ZoneId, "One or more object GUIDs differ from the active baseline.");
			if (evidence.ObjectPositionMismatchCount > 0 || evidence.ObjectRotationMismatchCount > 0 || evidence.ObjectScaleMismatchCount > 0)
				AddErrorOnce(evidence, "ObjectTransformMismatch", evidence.ZoneId, "One or more instantiated object transforms differ from schema records.");
			evidence.ErrorCount = evidence.Issues.Count(issue => issue.Severity == "Error");
			evidence.WarningCount = evidence.Issues.Count(issue => issue.Severity == "Warning");
			evidence.IsValid = evidence.ErrorCount == 0;
		}

		private static void WriteEvidence(string projectRoot, WorldSingleZoneRuntimeEvidenceData evidence)
		{
			string outputPath = Path.Combine(projectRoot, "Builds", "WorldUpgrade", "WorldSchemas", "u3-connected-world", "phase-3-single-zone-runtime-evidence.json");
			File.WriteAllText(outputPath, JsonConvert.SerializeObject(evidence, Formatting.Indented));
		}

		private static void AddErrorOnce(WorldSingleZoneRuntimeEvidenceData evidence, string code, string scopeId, string message)
		{
			if (!evidence.Issues.Any(issue => issue.Code == code))
				AddError(evidence, code, scopeId, message);
		}

		private static void AddError(WorldSingleZoneRuntimeEvidenceData evidence, string code, string scopeId, string message)
		{
			evidence.Issues.Add(new WorldRuntimeIssueData { Severity = "Error", Code = code, ScopeId = scopeId, Message = message });
		}

		private static bool MapNamesMatch(string left, string right)
		{
			return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
		}

		private static string Normalize(string value)
		{
			return new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
		}

		private static void ExitEditor(int exitCode)
		{
#if UNITY_EDITOR
			RestoreEditorAutoLoadPreferences();
			SessionState.SetBool("WorldUpgrade.Phase3.Active", false);
			EditorApplication.Exit(exitCode);
#else
			Application.Quit(exitCode);
#endif
		}

#if UNITY_EDITOR
		private static void RestoreEditorAutoLoadPreferences()
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
#endif
	}
}
