////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned
{
	/// <summary>
	/// JSON-safe normalized map position. UnityEngine.Vector2 exposes calculated properties such as
	/// normalized which Json.NET follows recursively, so persisted world-travel data uses plain floats.
	/// </summary>
	[Serializable]
	public struct WorldTravelMapPosition
	{
		public float X;
		public float Y;

		public WorldTravelMapPosition(float x, float y)
		{
			X = x;
			Y = y;
		}

		public Vector2 ToVector2()
		{
			return new Vector2(X, Y);
		}
	}

	/// <summary>
	/// Describes a one-way connection between two survival maps. Positions are normalized
	/// coordinates on each map's cartography image where (0, 0) is top-left.
	/// </summary>
	[Serializable]
	public class WorldTravelConnection
	{
		public string Id;
		public bool Enabled = true;
		public string SourceMap;
		public WorldTravelMapPosition SourceMapPosition;
		public string TargetMap;
		public WorldTravelMapPosition TargetMapPosition;
		public float TargetYaw;
		public float TriggerRadius = 24.0f;
	}

	/// <summary>
	/// User-editable configuration stored in WorldTravel.json in the Unturned root directory.
	/// </summary>
	[Serializable]
	public class WorldTravelManifest
	{
		public bool Enabled = true;
		public List<WorldTravelConnection> Connections = new List<WorldTravelConnection>();

		public static WorldTravelManifest CreateDefault()
		{
			WorldTravelManifest manifest = new WorldTravelManifest();

			// Initial proof-of-concept seam. These positions are intentionally a little inside the
			// map boundary so the interaction can be tested before a bespoke tunnel is authored.
			manifest.Connections.Add(new WorldTravelConnection
			{
				Id = "california2_to_limestone",
				SourceMap = "California 2",
				SourceMapPosition = new WorldTravelMapPosition(0.965f, 0.405f),
				TargetMap = "Limestone",
				TargetMapPosition = new WorldTravelMapPosition(0.045f, 0.405f),
				TargetYaw = 90.0f,
				TriggerRadius = 35.0f
			});

			manifest.Connections.Add(new WorldTravelConnection
			{
				Id = "limestone_to_california2",
				SourceMap = "Limestone",
				SourceMapPosition = new WorldTravelMapPosition(0.045f, 0.405f),
				TargetMap = "California 2",
				TargetMapPosition = new WorldTravelMapPosition(0.965f, 0.405f),
				TargetYaw = 270.0f,
				TriggerRadius = 35.0f
			});

			return manifest;
		}
	}

	/// <summary>
	/// Small durable hand-off record used while unloading the source map and starting the target map.
	/// Player inventory/life/skills are copied using their existing savedata files.
	/// </summary>
	[Serializable]
	public class WorldTravelPendingData
	{
		public string ConnectionId;
		public string SourceMap;
		public string TargetMap;
		public WorldTravelMapPosition TargetMapPosition;
		public float TargetYaw;
		public ulong SteamId;
		public byte CharacterId;
		public int GameMode;
		public bool Cheats;
		public int ResumeAttempts;
	}
}
