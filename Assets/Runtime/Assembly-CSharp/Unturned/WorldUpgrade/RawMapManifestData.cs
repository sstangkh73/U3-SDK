////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade
{
	[JsonConverter(typeof(StringEnumConverter))]
	public enum ERawMapSourceType
	{
		Official,
		Workshop,
		Local,
	}

	[JsonConverter(typeof(StringEnumConverter))]
	public enum ERawMapFileCategory
	{
		Metadata,
		Landscape,
		Terrain,
		WorldObjects,
		Environment,
		Navigation,
		Spawns,
		MapImage,
		AssetBundle,
		Other,
	}

	[Serializable]
	public class RawMapLevelDescriptorData
	{
		public byte FormatVersion;
		public byte SizeValue;
		public string SizeName;
		public int SizeInWorldUnits;
		public byte TypeValue;
		public string TypeName;
	}

	[Serializable]
	public class RawMapConfigReferenceData
	{
		public string RelativePath;
		public string Version;
		public string AssetGuid;
		public bool LegacyGameplayRulesAreReferenceOnly = true;
		public string ConfigParseError;
		public List<string> RequiredWorkshopFileIds = new List<string>();
		public List<string> CommonGameplayOverrideKeys = new List<string>();
		public List<string> EasyGameplayOverrideKeys = new List<string>();
		public List<string> NormalGameplayOverrideKeys = new List<string>();
		public List<string> HardGameplayOverrideKeys = new List<string>();
		public int TrainAssociationCount;
		public int SpawnLoadoutCount;
	}

	[Serializable]
	public class RawMapFileRecordData
	{
		public string RelativePath;
		public long SizeInBytes;
		public ERawMapFileCategory Category;
	}

	[Serializable]
	public class RawMapCategorySummaryData
	{
		public ERawMapFileCategory Category;
		public int FileCount;
		public long SizeInBytes;
	}

	[Serializable]
	public class RawMapDependencySummaryData
	{
		public string SourceRoot;
		public int FileCount;
		public long SizeInBytes;
		public string InventoryFingerprintSha256;
	}

	[Serializable]
	public class RawMapManifestData
	{
		public int SchemaVersion = 1;
		public string ZoneId;
		public string DisplayName;
		public ERawMapSourceType SourceType;
		public string PublishedFileId;
		public string SourceRoot;
		public string GeneratedUtc;
		public string InventoryFingerprintSha256;
		public bool CopiesSourceContent = false;
		public bool ImportsLegacyGameplayRules = false;
		public RawMapLevelDescriptorData Level = new RawMapLevelDescriptorData();
		public RawMapConfigReferenceData Config = new RawMapConfigReferenceData();
		public List<RawMapFileRecordData> Files = new List<RawMapFileRecordData>();
		public List<RawMapCategorySummaryData> Categories = new List<RawMapCategorySummaryData>();
		public List<RawMapDependencySummaryData> Dependencies = new List<RawMapDependencySummaryData>();
	}
}
