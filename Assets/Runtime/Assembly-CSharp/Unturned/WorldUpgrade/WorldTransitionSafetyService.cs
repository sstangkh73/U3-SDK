////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Framework.Landscapes;
using System;
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public sealed class WorldTransitionCorridorData
	{
		public string SourceZoneKey;
		public string DestinationZoneKey;
		public float SourceBoundaryX;
		public float DestinationBoundaryX;
		public float LandingOverlap;
		public float StartX;
		public float EndX;
		public float CenterZ;
		public float Width;
		public float Length => EndX - StartX;
	}

	public sealed class WorldTransitionSafetyService
	{
		public WorldTransitionCorridorData Corridor { get; }

		public WorldTransitionSafetyService(WorldZoneIndexData source, WorldZoneIndexData destination, float requestedWidth,
			float landingOverlap = 256f)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));
			if (requestedWidth <= 0f)
				throw new ArgumentOutOfRangeException(nameof(requestedWidth));
			if (landingOverlap < 0f)
				throw new ArgumentOutOfRangeException(nameof(landingOverlap));
			if (source.WorldBounds.Max.X > destination.WorldBounds.Min.X)
				throw new ArgumentException("Prototype corridor expects source zone to be west of destination zone.");
			float overlapMinZ = Math.Max(source.WorldBounds.Min.Z, destination.WorldBounds.Min.Z);
			float overlapMaxZ = Math.Min(source.WorldBounds.Max.Z, destination.WorldBounds.Max.Z);
			if (overlapMaxZ <= overlapMinZ)
				throw new ArgumentException("Zones do not share a Z interval for a transition corridor.");
			float overlapMidpoint = (overlapMinZ + overlapMaxZ) * 0.5f;
			float tileCenteredZ = Mathf.Floor(overlapMidpoint / Landscape.TILE_SIZE) * Landscape.TILE_SIZE + Landscape.TILE_SIZE * 0.5f;
			float halfWidth = Math.Min(requestedWidth, overlapMaxZ - overlapMinZ) * 0.5f;
			float centerZ = tileCenteredZ - halfWidth >= overlapMinZ && tileCenteredZ + halfWidth <= overlapMaxZ
				? tileCenteredZ
				: overlapMidpoint;
			Corridor = new WorldTransitionCorridorData
			{
				SourceZoneKey = source.ZoneKey,
				DestinationZoneKey = destination.ZoneKey,
				SourceBoundaryX = source.WorldBounds.Max.X,
				DestinationBoundaryX = destination.WorldBounds.Min.X,
				LandingOverlap = landingOverlap,
				StartX = source.WorldBounds.Max.X - landingOverlap,
				EndX = destination.WorldBounds.Min.X + landingOverlap,
				CenterZ = centerZ,
				Width = Math.Min(requestedWidth, overlapMaxZ - overlapMinZ),
			};
		}

		public bool IsInsideCorridor(Vector3 worldPosition)
		{
			return worldPosition.x >= Corridor.StartX && worldPosition.x <= Corridor.EndX &&
				Math.Abs(worldPosition.z - Corridor.CenterZ) <= Corridor.Width * 0.5f;
		}

		public bool AreBoundaryZonesPreloaded(WorldCellStreamer streamer, Vector3 worldPosition)
		{
			if (streamer == null)
				throw new ArgumentNullException(nameof(streamer));
			return streamer.HasLoadedLandscapeForZone(Corridor.SourceZoneKey, worldPosition, streamer.PreloadRadius) &&
				streamer.HasLoadedLandscapeForZone(Corridor.DestinationZoneKey, worldPosition, streamer.PreloadRadius);
		}

		public bool HasGroundCoverage(WorldCellStreamer streamer, Vector3 worldPosition)
		{
			return IsInsideCorridor(worldPosition) || streamer.HasLoadedLandscapeCoverage(worldPosition);
		}

		public GameObject CreateCollisionBridge(float sourceSurfaceY, float destinationSurfaceY)
		{
			Vector3 start = new Vector3(Corridor.StartX, sourceSurfaceY, Corridor.CenterZ);
			Vector3 end = new Vector3(Corridor.EndX, destinationSurfaceY, Corridor.CenterZ);
			Vector3 direction = end - start;
			GameObject bridge = new GameObject("WorldUpgrade_TransitionBridge_" + Corridor.SourceZoneKey + "_" + Corridor.DestinationZoneKey);
			bridge.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction.normalized);
			bridge.transform.position = (start + end) * 0.5f - bridge.transform.up;
			BoxCollider collider = bridge.AddComponent<BoxCollider>();
			collider.size = new Vector3(direction.magnitude + 4f, 2f, Corridor.Width);
			return bridge;
		}
	}
}
