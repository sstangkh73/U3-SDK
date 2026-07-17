////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public sealed class WorldDynamicEntityStateData
	{
		public string EntityId;
		public string Kind;
		public string SourceEntityId;
		public string ZoneId;
		public string OwnerCellId;
		public RawMapVector3Data WorldPosition = new RawMapVector3Data();
		public string AssetGuid;
		public int LegacyAssetId;
		public int Revision;
		public bool IsRemoved;
	}

	[Serializable]
	public sealed class WorldPersistenceSnapshotData
	{
		public int SchemaVersion = 1;
		public string WorldId;
		public long Sequence;
		public string ContentSha256;
		public List<WorldDynamicEntityStateData> Entities = new List<WorldDynamicEntityStateData>();
	}

	public sealed class WorldPersistenceStore
	{
		private readonly Dictionary<string, WorldDynamicEntityStateData> entities =
			new Dictionary<string, WorldDynamicEntityStateData>(StringComparer.Ordinal);

		public int Count => entities.Count;
		public IEnumerable<WorldDynamicEntityStateData> Entities => entities.Values;

		public bool TryGet(string entityId, out WorldDynamicEntityStateData entity)
		{
			return entities.TryGetValue(entityId, out entity);
		}

		public bool Add(WorldDynamicEntityStateData entity)
		{
			ValidateEntity(entity);
			if (entities.ContainsKey(entity.EntityId))
				return false;
			entities.Add(entity.EntityId, Clone(entity));
			return true;
		}

		public void Upsert(WorldDynamicEntityStateData entity)
		{
			ValidateEntity(entity);
			entities[entity.EntityId] = Clone(entity);
		}

		public WorldPersistenceSnapshotData CreateSnapshot(string worldId, long sequence)
		{
			WorldPersistenceSnapshotData snapshot = new WorldPersistenceSnapshotData
			{
				WorldId = worldId,
				Sequence = sequence,
				Entities = entities.Values.OrderBy(entity => entity.EntityId, StringComparer.Ordinal).Select(Clone).ToList(),
			};
			snapshot.ContentSha256 = WorldRuntimeIdentityUtility.Sha256(string.Join("\n", snapshot.Entities.Select(CanonicalLine)));
			return snapshot;
		}

		public void Restore(WorldPersistenceSnapshotData snapshot)
		{
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			string expected = WorldRuntimeIdentityUtility.Sha256(string.Join("\n", snapshot.Entities
				.OrderBy(entity => entity.EntityId, StringComparer.Ordinal).Select(CanonicalLine)));
			if (!string.Equals(expected, snapshot.ContentSha256, StringComparison.Ordinal))
				throw new InvalidOperationException("Persistence snapshot content fingerprint mismatch.");
			entities.Clear();
			foreach (WorldDynamicEntityStateData entity in snapshot.Entities)
			{
				ValidateEntity(entity);
				if (!entities.TryAdd(entity.EntityId, Clone(entity)))
					throw new InvalidOperationException("Persistence snapshot contains duplicate entity ID: " + entity.EntityId);
			}
		}

		private static void ValidateEntity(WorldDynamicEntityStateData entity)
		{
			if (entity == null || string.IsNullOrWhiteSpace(entity.EntityId) || string.IsNullOrWhiteSpace(entity.Kind) ||
				string.IsNullOrWhiteSpace(entity.ZoneId) || string.IsNullOrWhiteSpace(entity.OwnerCellId))
				throw new ArgumentException("Dynamic entity identity, kind, zone, and owner cell are required.", nameof(entity));
		}

		private static string CanonicalLine(WorldDynamicEntityStateData entity)
		{
			return string.Join("|", entity.EntityId, entity.Kind, entity.SourceEntityId, entity.ZoneId, entity.OwnerCellId,
				(entity.WorldPosition?.X ?? 0f).ToString("R", CultureInfo.InvariantCulture),
				(entity.WorldPosition?.Y ?? 0f).ToString("R", CultureInfo.InvariantCulture),
				(entity.WorldPosition?.Z ?? 0f).ToString("R", CultureInfo.InvariantCulture), entity.AssetGuid,
				entity.LegacyAssetId.ToString(CultureInfo.InvariantCulture), entity.Revision.ToString(CultureInfo.InvariantCulture),
				entity.IsRemoved ? "1" : "0");
		}

		private static WorldDynamicEntityStateData Clone(WorldDynamicEntityStateData value)
		{
			return new WorldDynamicEntityStateData
			{
				EntityId = value.EntityId,
				Kind = value.Kind,
				SourceEntityId = value.SourceEntityId,
				ZoneId = value.ZoneId,
				OwnerCellId = value.OwnerCellId,
				WorldPosition = new RawMapVector3Data(value.WorldPosition?.X ?? 0f, value.WorldPosition?.Y ?? 0f, value.WorldPosition?.Z ?? 0f),
				AssetGuid = value.AssetGuid,
				LegacyAssetId = value.LegacyAssetId,
				Revision = value.Revision,
				IsRemoved = value.IsRemoved,
			};
		}
	}

	public static class WorldRuntimeIdentityUtility
	{
		public static string Create(string prefix, string canonicalKey)
		{
			return prefix + "_" + Sha256(canonicalKey).Substring(0, 32);
		}

		public static string Sha256(string value)
		{
			using (System.Security.Cryptography.SHA256 hash = System.Security.Cryptography.SHA256.Create())
			{
				byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty);
				return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
			}
		}
	}

	public sealed class WorldEntityOwnershipResolver
	{
		private readonly WorldZoneResolver zoneResolver;
		private readonly Dictionary<string, WorldZoneDefinitionData> zonesByKey;

		public WorldEntityOwnershipResolver(WorldManifestData manifest, IEnumerable<WorldZoneDefinitionData> zones)
		{
			zoneResolver = new WorldZoneResolver(manifest);
			zonesByKey = zones.ToDictionary(zone => zone.ZoneKey, StringComparer.Ordinal);
		}

		public WorldCellIndexData ResolveCell(Vector3 worldPosition, out WorldZoneDefinitionData zone)
		{
			WorldZoneResolution resolution = zoneResolver.Resolve(worldPosition);
			zone = zonesByKey[resolution.Zone.ZoneKey];
			return zone.Cells.OrderBy(cell => WorldZoneResolver.DistanceSquaredXZ(cell.WorldBounds, worldPosition))
				.ThenBy(cell => cell.CellId, StringComparer.Ordinal).First();
		}

		public void Move(WorldDynamicEntityStateData entity, Vector3 worldPosition)
		{
			WorldCellIndexData cell = ResolveCell(worldPosition, out WorldZoneDefinitionData zone);
			entity.WorldPosition = new RawMapVector3Data(worldPosition.x, worldPosition.y, worldPosition.z);
			entity.ZoneId = zone.ZoneId;
			entity.OwnerCellId = cell.CellId;
			entity.Revision++;
		}
	}
}
