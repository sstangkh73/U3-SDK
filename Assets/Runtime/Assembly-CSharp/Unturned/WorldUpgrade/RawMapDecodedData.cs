////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public class RawMapVector3Data
	{
		public float X;
		public float Y;
		public float Z;

		public RawMapVector3Data()
		{
		}

		public RawMapVector3Data(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}
	}

	[Serializable]
	public class RawMapBoundsData
	{
		public bool HasValue;
		public RawMapVector3Data Min = new RawMapVector3Data();
		public RawMapVector3Data Max = new RawMapVector3Data();
	}

	[Serializable]
	public class RawMapDecodeIssueData
	{
		public string Severity;
		public string Code;
		public string RelativePath;
		public long Offset;
		public string Message;
	}

	[Serializable]
	public class RawMapTableSummaryData
	{
		public int Index;
		public int UniqueId;
		public string Name;
		public int LegacyTableId;
		public int TierCount;
		public int EntryCount;
		public bool IsMega;
	}

	[Serializable]
	public class RawMapSpawnFileSummaryData
	{
		public string RelativePath;
		public bool Present;
		public string ContentSha256;
		public int Version;
		public int FileSizeInBytes;
		public int BytesConsumed;
		public int TrailingBytes;
		public int TableCount;
		public int SpawnPointCount;
		public int RegionsWithSpawnPoints;
		public int InvalidTableReferenceCount;
		public RawMapBoundsData SpawnBounds = new RawMapBoundsData();
		public List<RawMapTableSummaryData> Tables = new List<RawMapTableSummaryData>();
	}

	[Serializable]
	public class RawMapLandscapeCoordData
	{
		public int X;
		public int Y;
	}

	[Serializable]
	public class RawMapLandscapeTileSummaryData
	{
		public int X;
		public int Y;
		public bool IsActiveInHierarchy;
		public bool HasHeightmap;
		public bool HasSplatmap;
		public bool HasHoles;
		public int HeightmapBytes;
		public string HeightmapSha256;
		public int SplatmapBytes;
		public string SplatmapSha256;
		public int HolesBytes;
		public string HolesSha256;
		public int MinRawHeight;
		public int MaxRawHeight;
		public int HoleCount;
		public int BlackSplatPixelCount;
	}

	[Serializable]
	public class RawMapLandscapeSummaryData
	{
		public int LegacyTerrainFileCount;
		public long LegacyTerrainBytes;
		public int HeightmapTileCount;
		public int SplatmapTileCount;
		public int HoleTileCount;
		public int InvalidFileCount;
		public int MissingHeightmapCount;
		public int MissingSplatmapCount;
		public bool HasHierarchyTileManifest;
		public int HierarchyTileCount;
		public int SourceOnlyTileCount;
		public int TotalHoleCount;
		public RawMapBoundsData WorldBounds = new RawMapBoundsData();
		public List<RawMapLandscapeCoordData> HierarchyTiles = new List<RawMapLandscapeCoordData>();
		public List<RawMapLandscapeTileSummaryData> Tiles = new List<RawMapLandscapeTileSummaryData>();
	}

	[Serializable]
	public class RawMapObjectSummaryData
	{
		public bool Present;
		public string ContentSha256;
		public int Version;
		public int FileSizeInBytes;
		public int BytesConsumed;
		public int TrailingBytes;
		public long AvailableInstanceId;
		public int ObjectCount;
		public int RegionsWithObjects;
		public int DuplicateInstanceIdCount;
		public int EmptyAssetReferenceCount;
		public RawMapBoundsData Bounds = new RawMapBoundsData();
	}

	[Serializable]
	public class RawMapRoadSummaryData
	{
		public bool MaterialsFilePresent;
		public string MaterialsContentSha256;
		public int MaterialsVersion;
		public int MaterialCount;
		public bool PathsFilePresent;
		public string PathsContentSha256;
		public int PathsVersion;
		public int PathCount;
		public int JointCount;
		public int InvalidMaterialReferenceCount;
		public int PathsBytesConsumed;
		public int PathsTrailingBytes;
		public RawMapBoundsData Bounds = new RawMapBoundsData();
	}

	[Serializable]
	public class RawMapHierarchyTypeCountData
	{
		public string TypeName;
		public int Count;
	}

	[Serializable]
	public class RawMapHierarchySummaryData
	{
		public bool Present;
		public string ContentSha256;
		public long FileSizeInBytes;
		public long AvailableInstanceId;
		public int ItemCount;
		public int MissingTypeCount;
		public List<RawMapHierarchyTypeCountData> Types = new List<RawMapHierarchyTypeCountData>();
	}

	[Serializable]
	public class RawMapDecodedSummaryData
	{
		public int SchemaVersion = 1;
		public string ZoneId;
		public string DisplayName;
		public string SourceRoot;
		public string SourceInventoryFingerprintSha256;
		public string GeneratedUtc;
		public bool IsValid;
		public List<RawMapSpawnFileSummaryData> SpawnFiles = new List<RawMapSpawnFileSummaryData>();
		public RawMapLandscapeSummaryData Landscape = new RawMapLandscapeSummaryData();
		public RawMapObjectSummaryData Objects = new RawMapObjectSummaryData();
		public RawMapRoadSummaryData Roads = new RawMapRoadSummaryData();
		public RawMapHierarchySummaryData Hierarchy = new RawMapHierarchySummaryData();
		public List<RawMapDecodeIssueData> Issues = new List<RawMapDecodeIssueData>();
	}
}
