////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Unturned.WorldUpgrade;
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	[Serializable]
	public class RawMapSourceEntryData
	{
		public bool Enabled = true;
		public string ZoneId;
		public string DisplayName;
		public ERawMapSourceType SourceType;
		public string PublishedFileId;
		public string SourceRoot;
		public List<string> DependencyRoots = new List<string>();
	}

	[Serializable]
	public class RawMapSourceCatalogData
	{
		public int SchemaVersion = 1;
		public string OutputDirectory = "Builds/WorldUpgrade/RawMapManifests";
		public List<RawMapSourceEntryData> Maps = new List<RawMapSourceEntryData>();
	}
}
