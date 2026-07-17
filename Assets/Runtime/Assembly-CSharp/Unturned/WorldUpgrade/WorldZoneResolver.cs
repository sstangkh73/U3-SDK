////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Linq;
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade
{
	public sealed class WorldZoneResolution
	{
		public WorldZoneIndexData Zone;
		public bool IsInsideZoneBounds;
		public float DistanceToZoneBounds;
		public Vector3 LocalPosition;
	}

	public sealed class WorldZoneResolver
	{
		private readonly WorldManifestData manifest;

		public WorldZoneResolver(WorldManifestData manifest)
		{
			this.manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
			if (manifest.Zones == null || manifest.Zones.Count == 0)
				throw new ArgumentException("World manifest must contain at least one zone.", nameof(manifest));
		}

		public WorldZoneResolution Resolve(Vector3 worldPosition)
		{
			WorldZoneIndexData zone = manifest.Zones
				.OrderBy(candidate => DistanceSquaredXZ(candidate.WorldBounds, worldPosition))
				.ThenBy(candidate => candidate.ZoneKey, StringComparer.Ordinal)
				.First();
			float distanceSquared = DistanceSquaredXZ(zone.WorldBounds, worldPosition);
			return new WorldZoneResolution
			{
				Zone = zone,
				IsInsideZoneBounds = distanceSquared <= 0.000001f,
				DistanceToZoneBounds = Mathf.Sqrt(distanceSquared),
				LocalPosition = WorldCoordinateStrategy.ToZoneLocal(zone, worldPosition),
			};
		}

		public WorldZoneIndexData FindZone(string zoneKey)
		{
			return manifest.Zones.SingleOrDefault(zone => string.Equals(zone.ZoneKey, zoneKey, StringComparison.Ordinal));
		}

		internal static float DistanceSquaredXZ(RawMapBoundsData bounds, Vector3 point)
		{
			if (bounds == null || !bounds.HasValue)
				return float.PositiveInfinity;
			float dx = DistanceToInterval(point.x, bounds.Min.X, bounds.Max.X);
			float dz = DistanceToInterval(point.z, bounds.Min.Z, bounds.Max.Z);
			return dx * dx + dz * dz;
		}

		internal static bool ContainsXZ(RawMapBoundsData bounds, Vector3 point)
		{
			return bounds != null && bounds.HasValue && point.x >= bounds.Min.X && point.x <= bounds.Max.X &&
				point.z >= bounds.Min.Z && point.z <= bounds.Max.Z;
		}

		private static float DistanceToInterval(float value, float min, float max)
		{
			if (value < min)
				return min - value;
			if (value > max)
				return value - max;
			return 0f;
		}
	}

	public static class WorldCoordinateStrategy
	{
		public static Vector3 ToZoneLocal(WorldZoneIndexData zone, Vector3 worldPosition)
		{
			if (zone == null)
				throw new ArgumentNullException(nameof(zone));
			return worldPosition - WorldRuntimePreviewFactory.ToVector3(zone.LayoutOffset);
		}

		public static Vector3 ToWorld(WorldZoneIndexData zone, Vector3 localPosition)
		{
			if (zone == null)
				throw new ArgumentNullException(nameof(zone));
			return localPosition + WorldRuntimePreviewFactory.ToVector3(zone.LayoutOffset);
		}
	}
}
