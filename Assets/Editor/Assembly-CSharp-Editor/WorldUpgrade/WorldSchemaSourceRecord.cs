////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Unturned.WorldUpgrade;
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	internal sealed class WorldSchemaEntitySeed
	{
		public string Kind;
		public string SourceKey;
		public RawMapVector3Data Position = new RawMapVector3Data();
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
	}

	internal sealed class WorldSchemaAssetAliasSeed
	{
		public string Kind;
		public int LegacyId;
		public string AssetGuid;
		public string SourceKey;
	}

	internal sealed class RawMapDetailedDecodeResult
	{
		public RawMapDecodedSummaryData Summary;
		public List<WorldSchemaEntitySeed> EntitySeeds;
		public List<WorldSchemaAssetAliasSeed> AssetAliases;
	}
}
