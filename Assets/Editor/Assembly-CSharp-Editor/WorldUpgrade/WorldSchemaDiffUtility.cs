////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Unturned.WorldUpgrade;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public static class WorldSchemaDiffUtility
	{
		public static WorldSchemaDiffData Compare(IEnumerable<WorldEntityRecordData> before,
			IEnumerable<WorldEntityRecordData> after)
		{
			Dictionary<string, WorldEntityRecordData> beforeById = ToUniqueMap(before, nameof(before));
			Dictionary<string, WorldEntityRecordData> afterById = ToUniqueMap(after, nameof(after));
			WorldSchemaDiffData result = new WorldSchemaDiffData();

			foreach (string entityId in beforeById.Keys.Union(afterById.Keys).OrderBy(value => value, StringComparer.Ordinal))
			{
				bool existedBefore = beforeById.TryGetValue(entityId, out WorldEntityRecordData beforeEntity);
				bool existsAfter = afterById.TryGetValue(entityId, out WorldEntityRecordData afterEntity);
				if (!existedBefore)
				{
					result.Added.Add(CreateEntry(null, afterEntity));
					continue;
				}
				if (!existsAfter)
				{
					result.Removed.Add(CreateEntry(beforeEntity, null));
					continue;
				}

				bool moved = !string.Equals(beforeEntity.OwnerCellId, afterEntity.OwnerCellId, StringComparison.Ordinal);
				bool modified = !string.Equals(beforeEntity.ContentFingerprintSha256, afterEntity.ContentFingerprintSha256, StringComparison.Ordinal);
				if (moved)
					result.Moved.Add(CreateEntry(beforeEntity, afterEntity));
				else if (modified)
					result.Modified.Add(CreateEntry(beforeEntity, afterEntity));
				else
					result.UnchangedCount++;
			}

			result.AddedCount = result.Added.Count;
			result.RemovedCount = result.Removed.Count;
			result.MovedCount = result.Moved.Count;
			result.ModifiedCount = result.Modified.Count;
			return result;
		}

		private static Dictionary<string, WorldEntityRecordData> ToUniqueMap(IEnumerable<WorldEntityRecordData> entities, string parameterName)
		{
			if (entities == null)
				throw new ArgumentNullException(parameterName);
			Dictionary<string, WorldEntityRecordData> result = new Dictionary<string, WorldEntityRecordData>(StringComparer.Ordinal);
			foreach (WorldEntityRecordData entity in entities)
			{
				if (entity == null || string.IsNullOrWhiteSpace(entity.EntityId))
					throw new ArgumentException("Schema diff inputs require non-empty entity IDs.", parameterName);
				if (!result.TryAdd(entity.EntityId, entity))
					throw new ArgumentException("Schema diff input contains duplicate entity ID: " + entity.EntityId, parameterName);
			}
			return result;
		}

		private static WorldSchemaDiffEntryData CreateEntry(WorldEntityRecordData before, WorldEntityRecordData after)
		{
			return new WorldSchemaDiffEntryData
			{
				EntityId = before?.EntityId ?? after.EntityId,
				Kind = before?.Kind ?? after.Kind,
				BeforeOwnerCellId = before?.OwnerCellId,
				AfterOwnerCellId = after?.OwnerCellId,
				BeforeContentFingerprintSha256 = before?.ContentFingerprintSha256,
				AfterContentFingerprintSha256 = after?.ContentFingerprintSha256,
			};
		}
	}
}
