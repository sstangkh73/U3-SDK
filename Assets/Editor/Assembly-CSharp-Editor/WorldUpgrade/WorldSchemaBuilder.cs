////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Unturned.WorldUpgrade;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	internal sealed class WorldSchemaZoneSource
	{
		public string ZoneKey;
		public string DisplayName;
		public RawMapManifestData Inventory;
		public RawMapDetailedDecodeResult Decode;
	}

	internal sealed class WorldSchemaZoneBuildResult
	{
		public WorldZoneDefinitionData Definition;
		public List<WorldCellData> Cells = new List<WorldCellData>();
	}

	internal sealed class WorldSchemaBuildResult
	{
		public WorldManifestData Manifest;
		public List<WorldSchemaZoneBuildResult> Zones = new List<WorldSchemaZoneBuildResult>();
		public List<WorldEntityRecordData> Entities = new List<WorldEntityRecordData>();
	}

	internal static class WorldSchemaBuilder
	{
		public const int CurrentSchemaVersion = 1;
		public const int CurrentGeneratorVersion = 1;

		internal static WorldSchemaBuildResult Build(WorldLayoutSourceData layout, IList<WorldSchemaZoneSource> sources)
		{
			ValidateLayoutInput(layout, sources);
			WorldSchemaBuildResult result = new WorldSchemaBuildResult();
			WorldManifestData manifest = new WorldManifestData
			{
				SchemaVersion = CurrentSchemaVersion,
				GeneratorVersion = CurrentGeneratorVersion,
				WorldKey = layout.WorldKey,
				WorldId = WorldStableIdUtility.Create("wld", string.Empty, "world|" + layout.WorldKey),
				DisplayName = layout.DisplayName,
				CellSize = layout.CellSize,
			};
			result.Manifest = manifest;

			Dictionary<string, WorldSchemaZoneSource> sourcesByKey = sources.ToDictionary(source => source.ZoneKey,
				StringComparer.Ordinal);
			List<string> sourceFingerprintLines = new List<string>();
			foreach (WorldZoneLayoutSourceData zoneLayout in layout.Zones)
			{
				WorldSchemaZoneSource source = sourcesByKey[zoneLayout.ZoneKey];
				WorldSchemaZoneBuildResult zoneResult = BuildZone(manifest, zoneLayout, source, result.Entities);
				result.Zones.Add(zoneResult);
				manifest.Zones.Add(CreateZoneIndex(zoneResult.Definition));
				AppendMigrations(manifest, zoneResult.Definition.ZoneId, source.Decode.AssetAliases);
				sourceFingerprintLines.Add(source.ZoneKey + "|" + source.Inventory.InventoryFingerprintSha256);
			}

			manifest.Zones = manifest.Zones.OrderBy(zone => zone.ZoneKey, StringComparer.Ordinal).ToList();
			manifest.AssetMigrations = manifest.AssetMigrations
				.OrderBy(value => value.ZoneId, StringComparer.Ordinal)
				.ThenBy(value => value.Kind, StringComparer.Ordinal)
				.ThenBy(value => value.LegacyId)
				.ThenBy(value => value.AssetGuid, StringComparer.Ordinal)
				.ToList();
			AnnotateMigrationAliases(manifest.AssetMigrations);
			manifest.SourceFingerprintSha256 = WorldStableIdUtility.Sha256(string.Join("\n", sourceFingerprintLines.OrderBy(value => value, StringComparer.Ordinal)));
			manifest.EntityIdentityFingerprintSha256 = CalculateIdentityFingerprint(result.Entities);
			manifest.Validation = WorldSchemaValidator.Validate(manifest, result.Zones, result.Entities);
			manifest.ContentFingerprintSha256 = CalculateManifestFingerprint(manifest);
			return result;
		}

		private static WorldSchemaZoneBuildResult BuildZone(WorldManifestData manifest, WorldZoneLayoutSourceData layout,
			WorldSchemaZoneSource source, List<WorldEntityRecordData> allEntities)
		{
			string zoneId = WorldStableIdUtility.Create("zon", manifest.WorldId, "zone|" + source.ZoneKey);
			WorldSchemaZoneBuildResult result = new WorldSchemaZoneBuildResult();
			WorldZoneDefinitionData definition = new WorldZoneDefinitionData
			{
				SchemaVersion = CurrentSchemaVersion,
				WorldId = manifest.WorldId,
				ZoneKey = source.ZoneKey,
				ZoneId = zoneId,
				DisplayName = source.DisplayName,
				LayoutOffset = Copy(layout.LayoutOffset),
				CellSize = manifest.CellSize,
				SourceInventoryFingerprintSha256 = source.Inventory.InventoryFingerprintSha256,
				SourceDecodedSchemaVersion = source.Decode.Summary.SchemaVersion,
			};
			result.Definition = definition;

			Dictionary<string, WorldCellData> cells = new Dictionary<string, WorldCellData>(StringComparer.Ordinal);
			foreach (WorldSchemaEntitySeed seed in source.Decode.EntitySeeds.OrderBy(value => value.Kind, StringComparer.Ordinal)
				.ThenBy(value => value.SourceKey, StringComparer.Ordinal))
			{
				int gridX = FloorToCell(seed.Position.X, manifest.CellSize);
				int gridZ = FloorToCell(seed.Position.Z, manifest.CellSize);
				string cellKey = gridX.ToString(CultureInfo.InvariantCulture) + "," + gridZ.ToString(CultureInfo.InvariantCulture);
				if (!cells.TryGetValue(cellKey, out WorldCellData cell))
				{
					cell = CreateCell(manifest, definition, gridX, gridZ);
					cells.Add(cellKey, cell);
				}

				WorldEntityRecordData entity = CreateEntity(zoneId, cell.CellId, definition.LayoutOffset, seed);
				cell.Entities.Add(entity);
				allEntities.Add(entity);
			}

			foreach (WorldCellData cell in cells.Values.OrderBy(value => value.GridX).ThenBy(value => value.GridZ))
			{
				cell.Entities = cell.Entities.OrderBy(value => value.EntityId, StringComparer.Ordinal).ToList();
				cell.EntityFingerprintSha256 = CalculateCellFingerprint(cell.Entities);
				result.Cells.Add(cell);
				definition.Cells.Add(CreateCellIndex(definition.ZoneKey, cell));
			}

			List<WorldEntityRecordData> zoneEntities = result.Cells.SelectMany(cell => cell.Entities).ToList();
			definition.EntityCount = zoneEntities.Count;
			definition.RecordCounts = CountKinds(zoneEntities);
			definition.EntityIdentityFingerprintSha256 = CalculateIdentityFingerprint(zoneEntities);
			CalculateZoneBounds(definition, result.Cells);
			definition.ContentFingerprintSha256 = CalculateZoneFingerprint(definition);
			return result;
		}

		private static WorldCellData CreateCell(WorldManifestData manifest, WorldZoneDefinitionData zone, int gridX, int gridZ)
		{
			float minX = gridX * manifest.CellSize;
			float minZ = gridZ * manifest.CellSize;
			WorldCellData cell = new WorldCellData
			{
				SchemaVersion = CurrentSchemaVersion,
				WorldId = manifest.WorldId,
				ZoneId = zone.ZoneId,
				CellId = WorldStableIdUtility.Create("cel", zone.ZoneId, "cell|" + gridX + "|" + gridZ),
				GridX = gridX,
				GridZ = gridZ,
			};
			cell.LocalBounds.HasValue = true;
			cell.LocalBounds.Min = new RawMapVector3Data(minX, -1024f, minZ);
			cell.LocalBounds.Max = new RawMapVector3Data(minX + manifest.CellSize, 1024f, minZ + manifest.CellSize);
			cell.WorldBounds = OffsetBounds(cell.LocalBounds, zone.LayoutOffset);
			return cell;
		}

		private static WorldEntityRecordData CreateEntity(string zoneId, string cellId, RawMapVector3Data layoutOffset,
			WorldSchemaEntitySeed seed)
		{
			WorldEntityRecordData entity = new WorldEntityRecordData
			{
				EntityId = WorldStableIdUtility.Create("ent", zoneId, seed.Kind + "|" + seed.SourceKey),
				OwnerCellId = cellId,
				Kind = seed.Kind,
				SourceKey = seed.SourceKey,
				LocalPosition = Copy(seed.Position),
				WorldPosition = Add(seed.Position, layoutOffset),
				Rotation = Copy(seed.Rotation),
				Scale = Copy(seed.Scale),
				TableIndex = seed.TableIndex,
				LegacyAssetId = seed.LegacyAssetId,
				AssetGuid = seed.AssetGuid,
				SourceInstanceId = seed.SourceInstanceId,
				PlacementOrigin = seed.PlacementOrigin,
				Angle = seed.Angle,
				Alternate = seed.Alternate,
				HasHeightmap = seed.HasHeightmap,
				HasSplatmap = seed.HasSplatmap,
				HasHoles = seed.HasHoles,
			};
			entity.ContentFingerprintSha256 = WorldStableIdUtility.Sha256(EntityContentLine(entity));
			return entity;
		}

		private static WorldCellIndexData CreateCellIndex(string zoneKey, WorldCellData cell)
		{
			WorldEntityRecordData landscape = cell.Entities.FirstOrDefault(entity => entity.Kind == "LandscapeTile");
			return new WorldCellIndexData
			{
				CellId = cell.CellId,
				GridX = cell.GridX,
				GridZ = cell.GridZ,
				RelativePath = "cells/" + zoneKey + "/" + cell.GridX.ToString(CultureInfo.InvariantCulture) + "_" + cell.GridZ.ToString(CultureInfo.InvariantCulture) + ".world-cell.json",
				LocalBounds = Copy(cell.LocalBounds),
				WorldBounds = Copy(cell.WorldBounds),
				EntityCount = cell.Entities.Count,
				EntityFingerprintSha256 = cell.EntityFingerprintSha256,
				HasLandscapeRecord = landscape != null,
				LandscapeHasHeightmap = landscape?.HasHeightmap ?? false,
				LandscapeHasSplatmap = landscape?.HasSplatmap ?? false,
				LandscapeHasHoles = landscape?.HasHoles ?? false,
				RecordCounts = CountKinds(cell.Entities),
			};
		}

		private static WorldZoneIndexData CreateZoneIndex(WorldZoneDefinitionData definition)
		{
			return new WorldZoneIndexData
			{
				ZoneKey = definition.ZoneKey,
				ZoneId = definition.ZoneId,
				DisplayName = definition.DisplayName,
				RelativePath = "zones/" + definition.ZoneKey + ".zone-definition.json",
				LayoutOffset = Copy(definition.LayoutOffset),
				LocalBounds = Copy(definition.LocalBounds),
				WorldBounds = Copy(definition.WorldBounds),
				SourceInventoryFingerprintSha256 = definition.SourceInventoryFingerprintSha256,
				ContentFingerprintSha256 = definition.ContentFingerprintSha256,
				EntityIdentityFingerprintSha256 = definition.EntityIdentityFingerprintSha256,
				CellCount = definition.Cells.Count,
				EntityCount = definition.EntityCount,
				RecordCounts = definition.RecordCounts.Select(value => new WorldRecordKindCountData { Kind = value.Kind, Count = value.Count }).ToList(),
			};
		}

		private static void AppendMigrations(WorldManifestData manifest, string zoneId, IEnumerable<WorldSchemaAssetAliasSeed> aliases)
		{
			foreach (IGrouping<string, WorldSchemaAssetAliasSeed> group in aliases.GroupBy(alias =>
				alias.Kind + "|" + alias.LegacyId.ToString(CultureInfo.InvariantCulture) + "|" + (alias.AssetGuid ?? string.Empty), StringComparer.Ordinal))
			{
				WorldSchemaAssetAliasSeed first = group.First();
				WorldAssetMigrationData migration = new WorldAssetMigrationData
				{
					MigrationId = WorldStableIdUtility.Create("mig", zoneId, group.Key),
					ZoneId = zoneId,
					Kind = first.Kind,
					LegacyId = first.LegacyId,
					AssetGuid = first.AssetGuid,
					SourceReferenceCount = group.Select(value => value.SourceKey).Distinct(StringComparer.Ordinal).Count(),
				};
				migration.SourceKeys = group.Select(value => value.SourceKey).Distinct(StringComparer.Ordinal)
					.OrderBy(value => value, StringComparer.Ordinal).Take(8).ToList();
				manifest.AssetMigrations.Add(migration);
			}
		}

		private static void AnnotateMigrationAliases(IList<WorldAssetMigrationData> migrations)
		{
			foreach (WorldAssetMigrationData migration in migrations)
			{
				if (migration.LegacyId <= 0)
				{
					migration.LegacyAliasStatus = "GuidOnly";
					migration.CanResolveLegacyId = false;
					migration.CollisionTargetCount = 0;
				}
			}

			foreach (IGrouping<string, WorldAssetMigrationData> group in migrations.Where(value => value.LegacyId > 0)
				.GroupBy(value => value.ZoneId + "|" + value.Kind + "|" + value.LegacyId, StringComparer.Ordinal))
			{
				int targetCount = group.Select(value => value.AssetGuid ?? string.Empty).Distinct(StringComparer.Ordinal).Count();
				bool ambiguous = targetCount > 1;
				foreach (WorldAssetMigrationData migration in group)
				{
					migration.LegacyAliasStatus = ambiguous ? "AmbiguousGuidPrimary" : "Unambiguous";
					migration.CanResolveLegacyId = !ambiguous;
					migration.CollisionTargetCount = targetCount;
				}
			}
		}

		private static List<WorldRecordKindCountData> CountKinds(IEnumerable<WorldEntityRecordData> entities)
		{
			return entities.GroupBy(entity => entity.Kind, StringComparer.Ordinal)
				.OrderBy(group => group.Key, StringComparer.Ordinal)
				.Select(group => new WorldRecordKindCountData { Kind = group.Key, Count = group.Count() }).ToList();
		}

		private static void CalculateZoneBounds(WorldZoneDefinitionData definition, IList<WorldCellData> cells)
		{
			foreach (WorldCellData cell in cells)
			{
				Include(definition.LocalBounds, cell.LocalBounds.Min);
				Include(definition.LocalBounds, cell.LocalBounds.Max);
				Include(definition.WorldBounds, cell.WorldBounds.Min);
				Include(definition.WorldBounds, cell.WorldBounds.Max);
			}
		}

		private static string CalculateCellFingerprint(IEnumerable<WorldEntityRecordData> entities)
		{
			return WorldStableIdUtility.Sha256(string.Join("\n", entities.OrderBy(value => value.EntityId, StringComparer.Ordinal)
				.Select(value => value.EntityId + "|" + value.OwnerCellId + "|" + value.ContentFingerprintSha256)));
		}

		private static string CalculateIdentityFingerprint(IEnumerable<WorldEntityRecordData> entities)
		{
			return WorldStableIdUtility.Sha256(string.Join("\n", entities.Select(value => value.EntityId)
				.OrderBy(value => value, StringComparer.Ordinal)));
		}

		private static string CalculateZoneFingerprint(WorldZoneDefinitionData zone)
		{
			IEnumerable<string> lines = zone.Cells.OrderBy(value => value.CellId, StringComparer.Ordinal)
				.Select(value => value.CellId + "|" + value.EntityFingerprintSha256);
			return WorldStableIdUtility.Sha256(zone.ZoneId + "\n" + zone.SourceInventoryFingerprintSha256 + "\n" +
				VectorLine(zone.LayoutOffset) + "\n" + string.Join("\n", lines));
		}

		private static string CalculateManifestFingerprint(WorldManifestData manifest)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine(manifest.WorldId);
			builder.AppendLine(manifest.SourceFingerprintSha256);
			builder.AppendLine(manifest.EntityIdentityFingerprintSha256);
			foreach (WorldZoneIndexData zone in manifest.Zones)
				builder.AppendLine(zone.ZoneId + "|" + zone.ContentFingerprintSha256);
			foreach (WorldAssetMigrationData migration in manifest.AssetMigrations)
				builder.AppendLine(migration.MigrationId + "|" + migration.LegacyId + "|" + migration.AssetGuid + "|" +
					migration.LegacyAliasStatus + "|" + (migration.CanResolveLegacyId ? "1" : "0") + "|" + migration.CollisionTargetCount);
			return WorldStableIdUtility.Sha256(builder.ToString());
		}

		internal static string EntityContentLine(WorldEntityRecordData entity)
		{
			return string.Join("|", new[]
			{
				entity.Kind,
				entity.SourceKey,
				VectorLine(entity.LocalPosition),
				VectorLine(entity.WorldPosition),
				VectorLine(entity.Rotation),
				VectorLine(entity.Scale),
				entity.TableIndex.ToString(CultureInfo.InvariantCulture),
				entity.LegacyAssetId.ToString(CultureInfo.InvariantCulture),
				entity.AssetGuid ?? string.Empty,
				entity.SourceInstanceId.ToString(CultureInfo.InvariantCulture),
				entity.PlacementOrigin.ToString(CultureInfo.InvariantCulture),
				entity.Angle.ToString(CultureInfo.InvariantCulture),
				entity.Alternate ? "1" : "0",
				entity.HasHeightmap ? "1" : "0",
				entity.HasSplatmap ? "1" : "0",
				entity.HasHoles ? "1" : "0",
			});
		}

		private static string VectorLine(RawMapVector3Data value)
		{
			return value.X.ToString("R", CultureInfo.InvariantCulture) + "," +
				value.Y.ToString("R", CultureInfo.InvariantCulture) + "," +
				value.Z.ToString("R", CultureInfo.InvariantCulture);
		}

		internal static int FloorToCell(float coordinate, int cellSize)
		{
			return (int) Math.Floor(coordinate / cellSize);
		}

		private static void ValidateLayoutInput(WorldLayoutSourceData layout, IList<WorldSchemaZoneSource> sources)
		{
			if (layout == null)
				throw new ArgumentNullException(nameof(layout));
			if (layout.SchemaVersion != CurrentSchemaVersion)
				throw new InvalidOperationException("Unsupported world layout schema version: " + layout.SchemaVersion);
			if (string.IsNullOrWhiteSpace(layout.WorldKey))
				throw new InvalidOperationException("WorldKey is required.");
			if (layout.CellSize <= 0)
				throw new InvalidOperationException("CellSize must be positive.");
			if (layout.Zones == null || layout.Zones.Count == 0)
				throw new InvalidOperationException("At least one zone layout is required.");
			if (layout.Zones.Any(zone => zone == null || string.IsNullOrWhiteSpace(zone.ZoneKey)))
				throw new InvalidOperationException("Every zone layout requires a ZoneKey.");
			if (layout.Zones.Select(zone => zone.ZoneKey).Distinct(StringComparer.Ordinal).Count() != layout.Zones.Count)
				throw new InvalidOperationException("ZoneKey values must be unique in world layout.");
			Dictionary<string, WorldSchemaZoneSource> sourceMap = sources.ToDictionary(source => source.ZoneKey, StringComparer.Ordinal);
			foreach (WorldZoneLayoutSourceData zone in layout.Zones)
			{
				if (!sourceMap.TryGetValue(zone.ZoneKey, out WorldSchemaZoneSource source))
					throw new InvalidOperationException("World layout source is missing: " + zone.ZoneKey);
				if (source.Inventory == null || source.Decode?.Summary == null)
					throw new InvalidOperationException("World source inventory/decode is missing: " + zone.ZoneKey);
				if (!source.Decode.Summary.IsValid)
					throw new InvalidOperationException("Decoded source contains validation errors: " + zone.ZoneKey);
			}
		}

		private static RawMapVector3Data Copy(RawMapVector3Data value)
		{
			return value == null ? new RawMapVector3Data() : new RawMapVector3Data(value.X, value.Y, value.Z);
		}

		private static RawMapBoundsData Copy(RawMapBoundsData value)
		{
			return new RawMapBoundsData { HasValue = value.HasValue, Min = Copy(value.Min), Max = Copy(value.Max) };
		}

		private static RawMapVector3Data Add(RawMapVector3Data left, RawMapVector3Data right)
		{
			return new RawMapVector3Data(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		private static RawMapBoundsData OffsetBounds(RawMapBoundsData bounds, RawMapVector3Data offset)
		{
			return new RawMapBoundsData { HasValue = bounds.HasValue, Min = Add(bounds.Min, offset), Max = Add(bounds.Max, offset) };
		}

		private static void Include(RawMapBoundsData bounds, RawMapVector3Data point)
		{
			RawMapDecodeContext.Include(bounds, point);
		}
	}
}
