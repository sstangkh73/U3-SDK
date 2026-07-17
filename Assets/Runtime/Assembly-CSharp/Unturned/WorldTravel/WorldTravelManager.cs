////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using UnityEngine;

namespace SDG.Unturned
{
	/// <summary>
	/// Singleplayer proof-of-concept for moving between maps while preserving player state.
	/// The first version intentionally uses the existing full level-loading pipeline behind a
	/// tunnel/ferry interaction rather than attempting to keep two static Level instances alive.
	/// </summary>
	public sealed class WorldTravelManager : MonoBehaviour
	{
		private static WorldTravelManager instance;

		private WorldTravelManifest manifest;
		private float nextConnectionCheckTime;
		private float nextPromptTime;
		private float suppressTriggersUntil;
		private bool isTraveling;
		private WorldTravelConnection nearbyConnection;
		private WorldTravelTransitionPresenter transitionPresenter;

		public static void Install(GameObject host)
		{
			if (instance == null && host != null)
			{
				instance = host.AddComponent<WorldTravelManager>();
			}
		}

		private void Awake()
		{
			if (instance != null && instance != this)
			{
				Destroy(this);
				return;
			}

			instance = this;
			transitionPresenter = GetComponent<WorldTravelTransitionPresenter>();
			if (transitionPresenter == null)
			{
				transitionPresenter = gameObject.AddComponent<WorldTravelTransitionPresenter>();
			}
			manifest = WorldTravelSavedata.LoadOrCreateManifest();
			Provider.onLoginSpawning += OnLoginSpawning;
			Player.onPlayerCreated += OnPlayerCreated;
		}

		private void OnDestroy()
		{
			if (instance != this)
			{
				return;
			}

			Provider.onLoginSpawning -= OnLoginSpawning;
			Player.onPlayerCreated -= OnPlayerCreated;
			transitionPresenter?.CancelTransition();
			instance = null;
		}

		private void Update()
		{
			if (isTraveling || Time.realtimeSinceStartup < suppressTriggersUntil)
			{
				return;
			}

			if (manifest == null || !manifest.Enabled || manifest.Connections == null
				|| !IsSingleplayerSurvivalReady())
			{
				return;
			}

#if UNITY_EDITOR
			// Lets the complete map hand-off be exercised without walking to an approximate seam.
			// Editor-only so exported builds always require the configured in-world trigger.
			if (InputEx.GetKeyDown(KeyCode.F8))
			{
				WorldTravelConnection testConnection = FindFirstConnectionForCurrentMap();
				if (testConnection != null)
				{
					UnturnedLog.info($"Editor forcing world travel through '{testConnection.Id}'");
					BeginTravel(testConnection);
				}
				else
				{
					ShowPrompt($"No enabled world travel connection starts from {Level.info.name}.");
				}
				return;
			}
#endif

			if (Time.realtimeSinceStartup >= nextConnectionCheckTime)
			{
				nextConnectionCheckTime = Time.realtimeSinceStartup + 0.2f;
				nearbyConnection = FindNearbyConnection(Player.LocalPlayer.transform.position);
			}

			if (nearbyConnection == null)
			{
				return;
			}

			if (Player.LocalPlayer.movement.getVehicle() != null)
			{
				ShowPrompt("Exit the vehicle before travelling between maps.");
				return;
			}

			ShowPrompt($"Press interact to travel to {nearbyConnection.TargetMap}");
			if (InputEx.GetKeyDown(ControlsSettings.interact))
			{
				BeginTravel(nearbyConnection);
			}
		}

		private static bool IsSingleplayerSurvivalReady()
		{
			return !Dedicator.IsDedicatedServer
				&& Provider.isConnected
				&& Provider.isServer
				&& Provider.isClient
				&& Provider.maxPlayers == 1
				&& Level.isLoaded
				&& Level.info != null
				&& Level.info.type == ELevelType.SURVIVAL
				&& Player.LocalPlayer != null
				&& Player.LocalPlayer.channel != null
				&& Player.LocalPlayer.channel.IsLocalPlayer;
		}

		private WorldTravelConnection FindNearbyConnection(Vector3 playerPosition)
		{
			foreach (WorldTravelConnection connection in manifest.Connections)
			{
				if (connection == null || !connection.Enabled || string.IsNullOrWhiteSpace(connection.TargetMap)
					|| !MapNamesMatch(connection.SourceMap, Level.info.name))
				{
					continue;
				}

				Vector3 triggerPosition = DeprojectMapPosition(connection.SourceMapPosition);
				float deltaX = playerPosition.x - triggerPosition.x;
				float deltaZ = playerPosition.z - triggerPosition.z;
				float radius = Mathf.Max(2.0f, connection.TriggerRadius);
				if ((deltaX * deltaX) + (deltaZ * deltaZ) <= radius * radius)
				{
					return connection;
				}
			}

			return null;
		}

		private WorldTravelConnection FindFirstConnectionForCurrentMap()
		{
			foreach (WorldTravelConnection connection in manifest.Connections)
			{
				if (connection != null && connection.Enabled && !string.IsNullOrWhiteSpace(connection.TargetMap)
					&& MapNamesMatch(connection.SourceMap, Level.info.name))
				{
					return connection;
				}
			}

			return null;
		}

		private void ShowPrompt(string text)
		{
			if (Time.realtimeSinceStartup < nextPromptTime)
			{
				return;
			}
			nextPromptTime = Time.realtimeSinceStartup + 0.25f;
			PlayerUI.message(EPlayerMessage.NPC_CUSTOM, text, 0.4f);
		}

		private void BeginTravel(WorldTravelConnection connection)
		{
			if (isTraveling)
			{
				return;
			}

			LevelInfo targetLevel = ResolveLevel(connection.TargetMap);
			if (targetLevel == null)
			{
				ShowPrompt($"Unable to travel: map '{connection.TargetMap}' is not installed or enabled.");
				return;
			}

			isTraveling = true;
			StartCoroutine(BeginTravelAfterTransition(connection, targetLevel));
		}

		private IEnumerator BeginTravelAfterTransition(WorldTravelConnection connection, LevelInfo targetLevel)
		{
			transitionPresenter.PrepareSourceCapture(targetLevel.name);
			yield return new WaitForEndOfFrame();
			transitionPresenter.CaptureSourceFrame();
			transitionPresenter.ShowTransition();

			// Give the source frame time to fade into the tunnel before beginning the synchronous
			// parts of Unturned's ordinary disconnect and level-loading pipeline.
			yield return new WaitForSecondsRealtime(0.65f);

			Player player = Player.LocalPlayer;
			try
			{
				if (player == null || player.channel == null || player.channel.owner == null)
				{
					throw new InvalidOperationException("Local player disappeared before world travel could begin.");
				}

				SteamPlayerID playerId = player.channel.owner.playerID;
				SaveManager.save();
				WorldTravelSavedata.CopyPlayerState(playerId, Level.info.name, targetLevel.name);

				WorldTravelPendingData pending = new WorldTravelPendingData
				{
					ConnectionId = connection.Id,
					SourceMap = Level.info.name,
					TargetMap = targetLevel.name,
					TargetMapPosition = connection.TargetMapPosition,
					TargetYaw = connection.TargetYaw,
					SteamId = playerId.steamID.m_SteamID,
					CharacterId = playerId.characterID,
					GameMode = (int) Provider.mode,
					Cheats = Provider.hasCheats,
					ResumeAttempts = 0
				};
				WorldTravelSavedata.WritePending(pending);
				Provider.RequestDisconnect($"world travel from {pending.SourceMap} to {pending.TargetMap}");
			}
			catch (Exception exception)
			{
				isTraveling = false;
				transitionPresenter.CancelTransition();
				WorldTravelSavedata.ClearPending();
				UnturnedLog.exception(exception, "Caught exception beginning world travel:");
				ShowPrompt("Unable to travel. See the log for details.");
			}
		}

		/// <summary>
		/// Called after MenuStartup initialized Characters and UI. Returns true if a pending trip
		/// consumed the menu scene by immediately starting the target singleplayer map.
		/// </summary>
		public static bool TryResumePendingTravel()
		{
			WorldTravelPendingData pending = WorldTravelSavedata.ReadPending();
			if (pending == null)
			{
				return false;
			}

			if (instance != null)
			{
				instance.transitionPresenter.EnsureTransitionVisible(pending.TargetMap);
			}

			if (pending.ResumeAttempts > 0)
			{
				UnturnedLog.error($"World travel to '{pending.TargetMap}' returned to the menu before completing; cancelling to avoid a loop.");
				WorldTravelSavedata.ClearPending();
				if (instance != null)
				{
					instance.isTraveling = false;
					instance.transitionPresenter.CancelTransition();
				}
				return false;
			}

			LevelInfo targetLevel = ResolveLevel(pending.TargetMap);
			if (targetLevel == null)
			{
				UnturnedLog.error($"Unable to resume world travel because map '{pending.TargetMap}' is not installed or enabled.");
				WorldTravelSavedata.ClearPending();
				if (instance != null)
				{
					instance.isTraveling = false;
					instance.transitionPresenter.CancelTransition();
				}
				return false;
			}

			pending.TargetMap = targetLevel.name;
			pending.ResumeAttempts++;
			WorldTravelSavedata.WritePending(pending);
			Provider.map = targetLevel.name;
			Provider.singleplayer((EGameMode) pending.GameMode, pending.Cheats);
			return true;
		}

		private static void OnLoginSpawning(SteamPlayerID playerId, ref Vector3 point, ref float yaw,
			ref EPlayerStance initialStance, ref bool needsNewSpawnpoint)
		{
			WorldTravelPendingData pending = WorldTravelSavedata.ReadPending();
			if (!DoesPendingMatchPlayerAndMap(pending, playerId))
			{
				return;
			}

			point = DeprojectMapPosition(pending.TargetMapPosition);
			yaw = pending.TargetYaw;
			initialStance = EPlayerStance.STAND;
			needsNewSpawnpoint = false;
			UnturnedLog.info($"World travel spawning player at {point} on '{pending.TargetMap}'");
		}

		private static void OnPlayerCreated(Player player)
		{
			if (player == null || player.channel == null || !player.channel.IsLocalPlayer || player.channel.owner == null)
			{
				return;
			}

			WorldTravelPendingData pending = WorldTravelSavedata.ReadPending();
			if (!DoesPendingMatchPlayerAndMap(pending, player.channel.owner.playerID))
			{
				return;
			}

			WorldTravelSavedata.ClearPending();
			if (instance != null)
			{
				instance.isTraveling = false;
				instance.suppressTriggersUntil = Time.realtimeSinceStartup + 8.0f;
				instance.transitionPresenter.BeginReveal();
			}
			UnturnedLog.info($"Completed world travel from '{pending.SourceMap}' to '{pending.TargetMap}'");
		}

		private static bool DoesPendingMatchPlayerAndMap(WorldTravelPendingData pending, SteamPlayerID playerId)
		{
			// SteamPlayerID overloads == without guarding null, so use a reference check here.
			if (pending == null || ReferenceEquals(playerId, null))
			{
				return false;
			}

			// Level.info can change during the scene hand-off. Cache it so a second property
			// lookup cannot observe a transient null between the guard and the name comparison.
			LevelInfo currentLevel = Level.info;
			if (currentLevel == null)
			{
				return false;
			}

			return pending.SteamId == playerId.steamID.m_SteamID
				&& pending.CharacterId == playerId.characterID
				&& MapNamesMatch(pending.TargetMap, currentLevel.name);
		}

		private static Vector3 DeprojectMapPosition(WorldTravelMapPosition serializedPosition)
		{
			Vector2 mapPosition = serializedPosition.ToVector2();
			mapPosition.x = Mathf.Clamp01(mapPosition.x);
			mapPosition.y = Mathf.Clamp01(mapPosition.y);

			Vector3 worldPosition;
			CartographyVolumeManager cartographyVolumeManager = CartographyVolumeManager.Get();
			CartographyVolume cartographyVolume = cartographyVolumeManager != null
				? cartographyVolumeManager.GetMainVolume()
				: null;
			if (cartographyVolume != null)
			{
				Vector3 localPosition = new Vector3(mapPosition.x - 0.5f, 0.0f, 0.5f - mapPosition.y);
				worldPosition = cartographyVolume.transform.TransformPoint(localPosition);
			}
			else
			{
				float levelSize = Level.size - (Level.border * 2.0f);
				worldPosition = new Vector3((mapPosition.x - 0.5f) * levelSize, 0.0f,
					(0.5f - mapPosition.y) * levelSize);
			}

			worldPosition.y = LevelGround.getHeight(worldPosition) + 0.5f;
			return worldPosition;
		}

		private static LevelInfo ResolveLevel(string configuredName)
		{
			LevelInfo exact = Level.getLevel(configuredName);
			if (exact != null)
			{
				return exact;
			}

			foreach (LevelInfo candidate in Level.getLevels(ESingleplayerMapCategory.ALL))
			{
				if (candidate != null && MapNamesMatch(configuredName, candidate.name))
				{
					return candidate;
				}
			}
			return null;
		}

		private static bool MapNamesMatch(string lhs, string rhs)
		{
			return string.Equals(NormalizeMapName(lhs), NormalizeMapName(rhs), StringComparison.OrdinalIgnoreCase);
		}

		private static string NormalizeMapName(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
			foreach (char character in value)
			{
				if (char.IsLetterOrDigit(character))
				{
					builder.Append(char.ToLowerInvariant(character));
				}
			}
			return builder.ToString();
		}
	}
}
