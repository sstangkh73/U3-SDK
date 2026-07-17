////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.IO;

namespace SDG.Unturned
{
	/// <summary>
	/// Persistence helpers for the world-travel prototype. This deliberately reuses the existing
	/// per-map player file formats rather than duplicating inventory, life, clothing, or quest logic.
	/// </summary>
	public static class WorldTravelSavedata
	{
		private const string PENDING_PATH = "/WorldTravel/PendingTravel.json";

		private static readonly string[] PLAYER_STATE_FILES =
		{
			"/Player/Player.dat",
			"/Player/Anim.dat",
			"/Player/Clothing.dat",
			"/Player/Inventory.dat",
			"/Player/Life.dat",
			"/Player/Quests.dat",
			"/Player/Skills.dat"
		};

		public static string ManifestPath => Path.Combine(UnturnedPaths.RootDirectory.FullName, "WorldTravel.json");

		public static WorldTravelManifest LoadOrCreateManifest()
		{
			try
			{
				if (ReadWrite.fileExists(ManifestPath, false, false))
				{
					WorldTravelManifest manifest = ReadWrite.deserializeJSON<WorldTravelManifest>(ManifestPath, false, false);
					if (manifest != null)
					{
						manifest.Connections ??= new System.Collections.Generic.List<WorldTravelConnection>();
						return manifest;
					}
				}
			}
			catch (Exception exception)
			{
				UnturnedLog.exception(exception, $"Caught exception reading world travel manifest at \"{ManifestPath}\":");
			}

			WorldTravelManifest defaultManifest = WorldTravelManifest.CreateDefault();
			try
			{
				ReadWrite.serializeJSON(ManifestPath, false, false, defaultManifest);
				UnturnedLog.info($"Created default world travel manifest at \"{ManifestPath}\"");
			}
			catch (Exception exception)
			{
				UnturnedLog.exception(exception, $"Caught exception writing world travel manifest at \"{ManifestPath}\":");
			}
			return defaultManifest;
		}

		public static WorldTravelPendingData ReadPending()
		{
			try
			{
				if (!ServerSavedata.fileExists(PENDING_PATH))
				{
					return null;
				}
				return ServerSavedata.deserializeJSON<WorldTravelPendingData>(PENDING_PATH);
			}
			catch (Exception exception)
			{
				UnturnedLog.exception(exception, "Caught exception reading pending world travel:");
				return null;
			}
		}

		public static void WritePending(WorldTravelPendingData pending)
		{
			ServerSavedata.serializeJSON(PENDING_PATH, pending);
		}

		public static void ClearPending()
		{
			if (ServerSavedata.fileExists(PENDING_PATH))
			{
				ServerSavedata.deleteFile(PENDING_PATH);
			}
		}

		/// <summary>
		/// Mirror the player's source-map state into the target-map namespace. The pending spawn
		/// hook replaces position and rotation after the target terrain is available.
		/// </summary>
		public static void CopyPlayerState(SteamPlayerID playerId, string sourceMap, string targetMap)
		{
			string sourceRoot = GetPlayerMapRoot(playerId, sourceMap);
			string targetRoot = GetPlayerMapRoot(playerId, targetMap);

			foreach (string relativePath in PLAYER_STATE_FILES)
			{
				string sourcePath = sourceRoot + relativePath;
				string targetPath = targetRoot + relativePath;

				if (ReadWrite.fileExists(sourcePath, false, false))
				{
					byte[] bytes = ReadWrite.readBytes(sourcePath, false, false);
					if (bytes == null)
					{
						throw new IOException($"Unable to read player state file \"{sourcePath}\"");
					}

					ReadWrite.DeleteIfExistsAbsolute(ServerSavedata.GetBackupFilePathV1(targetPath));
					ReadWrite.MoveIfExistsAbsolute(targetPath, ServerSavedata.GetBackupFilePath(targetPath));
					ReadWrite.writeBytes(targetPath, false, false, bytes);
				}
				else
				{
					// An absent source file is meaningful (e.g., no quests yet), so do not retain an
					// unrelated older state from a previous visit to the target map.
					ReadWrite.DeleteIfExistsAbsolute(targetPath);
				}
			}
		}

		private static string GetPlayerMapRoot(SteamPlayerID playerId, string mapName)
		{
			string characterFolder = playerId.steamID + "_" + playerId.characterID;
			if (PlayerSavedata.hasSync)
			{
				// Match PlayerSavedata's legacy absolute Sync path behavior.
				return ReadWrite.PATH + "/Sync/" + characterFolder + "/" + mapName;
			}

			return ReadWrite.PATH + ServerSavedata.directory + "/" + Provider.serverID
				+ "/Players/" + characterFolder + "/" + mapName;
		}
	}
}
