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
	internal static class WorldSchemaValidator
	{
		internal static WorldSchemaValidationSummaryData Validate(WorldManifestData manifest,
			IList<WorldSchemaZoneBuildResult> zones, IList<WorldEntityRecordData> entities)
		{
			WorldSchemaValidationSummaryData validation = new WorldSchemaValidationSummaryData
			{
				ZoneCount = zones.Count,
				CellCount = zones.Sum(zone => zone.Cells.Count),
				EntityCount = entities.Count,
				MigrationCount = manifest.AssetMigrations.Count,
			};

			HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
			AddId(validation, ids, manifest.WorldId, "WorldId");
			foreach (WorldSchemaZoneBuildResult zone in zones)
			{
				AddId(validation, ids, zone.Definition.ZoneId, "ZoneId");
				foreach (WorldCellData cell in zone.Cells)
					AddId(validation, ids, cell.CellId, "CellId");
			}
			foreach (WorldEntityRecordData entity in entities)
				AddId(validation, ids, entity.EntityId, "EntityId");
			foreach (WorldAssetMigrationData migration in manifest.AssetMigrations)
				AddId(validation, ids, migration.MigrationId, "MigrationId");

			Dictionary<string, WorldCellData> cellsById = zones.SelectMany(zone => zone.Cells)
				.ToDictionary(cell => cell.CellId, StringComparer.Ordinal);
			foreach (WorldEntityRecordData entity in entities)
			{
				if (!cellsById.TryGetValue(entity.OwnerCellId, out WorldCellData owner))
				{
					validation.OwnerCellMismatchCount++;
					AddError(validation, "MissingOwnerCell", entity.EntityId, "Entity owner cell does not exist: " + entity.OwnerCellId);
					continue;
				}
				int expectedX = WorldSchemaBuilder.FloorToCell(entity.LocalPosition.X, manifest.CellSize);
				int expectedZ = WorldSchemaBuilder.FloorToCell(entity.LocalPosition.Z, manifest.CellSize);
				if (owner.GridX != expectedX || owner.GridZ != expectedZ)
				{
					validation.OwnerCellMismatchCount++;
					AddError(validation, "OwnerCellMismatch", entity.EntityId,
						"Entity is assigned to cell " + owner.GridX + "," + owner.GridZ + " but position resolves to " + expectedX + "," + expectedZ + ".");
				}
			}

			for (int leftIndex = 0; leftIndex < zones.Count; ++leftIndex)
			{
				for (int rightIndex = leftIndex + 1; rightIndex < zones.Count; ++rightIndex)
				{
					WorldZoneDefinitionData left = zones[leftIndex].Definition;
					WorldZoneDefinitionData right = zones[rightIndex].Definition;
					if (Overlaps(left.WorldBounds, right.WorldBounds))
					{
						validation.ZoneOverlapCount++;
						AddError(validation, "ZoneLayoutOverlap", left.ZoneId + "/" + right.ZoneId,
							"World-space zone bounds overlap. Adjust the explicit layout offsets before runtime streaming.");
					}
				}
			}

			foreach (WorldSchemaZoneBuildResult zone in zones)
			{
				foreach (WorldCellIndexData cell in zone.Definition.Cells.Where(cell => cell.HasLandscapeRecord && !cell.LandscapeHasHeightmap))
				{
					AddWarning(validation, "LandscapeWithoutHeightmap", cell.CellId,
						"Cell has splatmap or holes source data without a matching heightmap. Runtime fallback policy is required.");
				}
			}

			foreach (IGrouping<string, WorldAssetMigrationData> group in manifest.AssetMigrations
				.Where(migration => migration.LegacyId > 0)
				.GroupBy(migration => migration.ZoneId + "|" + migration.Kind + "|" + migration.LegacyId, StringComparer.Ordinal))
			{
				int targetCount = group.Select(value => value.AssetGuid ?? string.Empty).Distinct(StringComparer.Ordinal).Count();
				if (targetCount > 1)
				{
					if (group.Any(value => value.CanResolveLegacyId))
						AddError(validation, "UnsafeMigrationAliasCollision", group.Key, "Ambiguous legacy ID was incorrectly marked safe for resolution.");
					else
						AddWarning(validation, "QuarantinedMigrationAliasCollision", group.Key,
							"Legacy ID has multiple GUID targets. GUID remains primary and legacy-only resolution is disabled for this alias group.");
				}
			}

			validation.ErrorCount = validation.Issues.Count(issue => issue.Severity == "Error");
			validation.WarningCount = validation.Issues.Count(issue => issue.Severity == "Warning");
			validation.IsValid = validation.ErrorCount == 0;
			return validation;
		}

		private static void AddId(WorldSchemaValidationSummaryData validation, HashSet<string> ids, string id, string kind)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				validation.DuplicateIdCount++;
				AddError(validation, "MissingStableId", kind, kind + " is empty.");
			}
			else if (!ids.Add(id))
			{
				validation.DuplicateIdCount++;
				AddError(validation, "DuplicateStableId", id, kind + " collides with an existing stable ID.");
			}
		}

		private static bool Overlaps(RawMapBoundsData left, RawMapBoundsData right)
		{
			if (!left.HasValue || !right.HasValue)
				return false;
			return left.Min.X < right.Max.X && left.Max.X > right.Min.X &&
				left.Min.Z < right.Max.Z && left.Max.Z > right.Min.Z;
		}

		private static void AddError(WorldSchemaValidationSummaryData validation, string code, string scopeId, string message)
		{
			validation.Issues.Add(new WorldSchemaIssueData { Severity = "Error", Code = code, ScopeId = scopeId, Message = message });
		}

		private static void AddWarning(WorldSchemaValidationSummaryData validation, string code, string scopeId, string message)
		{
			validation.Issues.Add(new WorldSchemaIssueData { Severity = "Warning", Code = code, ScopeId = scopeId, Message = message });
		}
	}
}
