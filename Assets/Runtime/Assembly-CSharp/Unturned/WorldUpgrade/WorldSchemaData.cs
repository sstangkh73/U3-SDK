////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public class WorldLayoutSourceData
	{
		public int SchemaVersion = 1;
		public string WorldKey;
		public string DisplayName;
		public int CellSize = 1024;
		public List<WorldZoneLayoutSourceData> Zones = new List<WorldZoneLayoutSourceData>();
	}

	[Serializable]
	public class WorldZoneLayoutSourceData
	{
		public string ZoneKey;
		public RawMapVector3Data LayoutOffset = new RawMapVector3Data();
	}

	[Serializable]
	public class WorldRecordKindCountData
	{
		public string Kind;
		public int Count;
	}

	[Serializable]
	public class WorldCellIndexData
	{
		public string CellId;
		public int GridX;
		public int GridZ;
		public string RelativePath;
		public RawMapBoundsData LocalBounds = new RawMapBoundsData();
		public RawMapBoundsData WorldBounds = new RawMapBoundsData();
		public int EntityCount;
		public string EntityFingerprintSha256;
		public bool HasLandscapeRecord;
		public bool LandscapeHasHeightmap;
		public bool LandscapeHasSplatmap;
		public bool LandscapeHasHoles;
		public List<WorldRecordKindCountData> RecordCounts = new List<WorldRecordKindCountData>();
	}

	[Serializable]
	public class WorldEntityRecordData
	{
		public string EntityId;
		public string OwnerCellId;
		public string Kind;
		public string SourceKey;
		public RawMapVector3Data LocalPosition = new RawMapVector3Data();
		public RawMapVector3Data WorldPosition = new RawMapVector3Data();
		public RawMapVector3Data Rotation = new RawMapVector3Data();
		public RawMapVector3Data Scale = new RawMapVector3Data(1f, 1f, 1f);
		public int TableIndex = -1;
		public int LegacyAssetId;
		public string AssetGuid;
		public long SourceInstanceId;
		public int PlacementOrigin;
		public int Angle;
		public bool Alternate;
		public bool HasHeightmap;
		public bool HasSplatmap;
		public bool HasHoles;
		public string ContentFingerprintSha256;
	}

	[Serializable]
	public class WorldCellData
	{
		public int SchemaVersion = 1;
		public string WorldId;
		public string ZoneId;
		public string CellId;
		public int GridX;
		public int GridZ;
		public RawMapBoundsData LocalBounds = new RawMapBoundsData();
		public RawMapBoundsData WorldBounds = new RawMapBoundsData();
		public string EntityFingerprintSha256;
		public List<WorldEntityRecordData> Entities = new List<WorldEntityRecordData>();
	}

	[Serializable]
	public class WorldZoneDefinitionData
	{
		public int SchemaVersion = 1;
		public string WorldId;
		public string ZoneKey;
		public string ZoneId;
		public string DisplayName;
		public RawMapVector3Data LayoutOffset = new RawMapVector3Data();
		public int CellSize;
		public RawMapBoundsData LocalBounds = new RawMapBoundsData();
		public RawMapBoundsData WorldBounds = new RawMapBoundsData();
		public string SourceInventoryFingerprintSha256;
		public int SourceDecodedSchemaVersion;
		public string ContentFingerprintSha256;
		public string EntityIdentityFingerprintSha256;
		public int EntityCount;
		public List<WorldRecordKindCountData> RecordCounts = new List<WorldRecordKindCountData>();
		public List<WorldCellIndexData> Cells = new List<WorldCellIndexData>();
	}

	[Serializable]
	public class WorldZoneIndexData
	{
		public string ZoneKey;
		public string ZoneId;
		public string DisplayName;
		public string RelativePath;
		public RawMapVector3Data LayoutOffset = new RawMapVector3Data();
		public RawMapBoundsData LocalBounds = new RawMapBoundsData();
		public RawMapBoundsData WorldBounds = new RawMapBoundsData();
		public string SourceInventoryFingerprintSha256;
		public string ContentFingerprintSha256;
		public string EntityIdentityFingerprintSha256;
		public int CellCount;
		public int EntityCount;
		public List<WorldRecordKindCountData> RecordCounts = new List<WorldRecordKindCountData>();
	}

	[Serializable]
	public class WorldAssetMigrationData
	{
		public string MigrationId;
		public string ZoneId;
		public string Kind;
		public int LegacyId;
		public string AssetGuid;
		public string LegacyAliasStatus;
		public bool CanResolveLegacyId;
		public int CollisionTargetCount;
		public int SourceReferenceCount;
		public List<string> SourceKeys = new List<string>();
	}

	[Serializable]
	public class WorldSchemaIssueData
	{
		public string Severity;
		public string Code;
		public string ScopeId;
		public string Message;
	}

	[Serializable]
	public class WorldSchemaValidationSummaryData
	{
		public bool IsValid;
		public int ErrorCount;
		public int WarningCount;
		public int ZoneCount;
		public int CellCount;
		public int EntityCount;
		public int MigrationCount;
		public int DuplicateIdCount;
		public int OwnerCellMismatchCount;
		public int ZoneOverlapCount;
		public List<WorldSchemaIssueData> Issues = new List<WorldSchemaIssueData>();
	}

	[Serializable]
	public class WorldManifestData
	{
		public int SchemaVersion = 1;
		public int GeneratorVersion = 1;
		public string WorldKey;
		public string WorldId;
		public string DisplayName;
		public int CellSize;
		public string SourceFingerprintSha256;
		public string ContentFingerprintSha256;
		public string EntityIdentityFingerprintSha256;
		public List<WorldZoneIndexData> Zones = new List<WorldZoneIndexData>();
		public List<WorldAssetMigrationData> AssetMigrations = new List<WorldAssetMigrationData>();
		public WorldSchemaValidationSummaryData Validation = new WorldSchemaValidationSummaryData();
	}

	[Serializable]
	public class WorldSchemaDiffEntryData
	{
		public string EntityId;
		public string Kind;
		public string BeforeOwnerCellId;
		public string AfterOwnerCellId;
		public string BeforeContentFingerprintSha256;
		public string AfterContentFingerprintSha256;
	}

	[Serializable]
	public class WorldSchemaDiffData
	{
		public int AddedCount;
		public int RemovedCount;
		public int MovedCount;
		public int ModifiedCount;
		public int UnchangedCount;
		public List<WorldSchemaDiffEntryData> Added = new List<WorldSchemaDiffEntryData>();
		public List<WorldSchemaDiffEntryData> Removed = new List<WorldSchemaDiffEntryData>();
		public List<WorldSchemaDiffEntryData> Moved = new List<WorldSchemaDiffEntryData>();
		public List<WorldSchemaDiffEntryData> Modified = new List<WorldSchemaDiffEntryData>();
	}

	[Serializable]
	public class WorldSchemaGenerationEvidenceData
	{
		public int SchemaVersion = 1;
		public string WorldId;
		public bool IsValid;
		public bool DeterministicRepeatBuild;
		public string FirstContentFingerprintSha256;
		public string RepeatContentFingerprintSha256;
		public string FirstEntityIdentityFingerprintSha256;
		public string RepeatEntityIdentityFingerprintSha256;
		public int ZoneFileCount;
		public int CellFileCount;
		public WorldSchemaValidationSummaryData Validation = new WorldSchemaValidationSummaryData();
		public WorldSchemaDiffData SourceUpdateDiff = new WorldSchemaDiffData();
	}
}
