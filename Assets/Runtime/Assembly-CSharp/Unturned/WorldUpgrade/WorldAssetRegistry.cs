////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade
{
	/// <summary>
	/// Tracks logical ownership of object assets used by streamed world cells. The underlying
	/// Unturned asset system owns the actual asset cache, so reaching zero removes the registry
	/// entry but does not attempt to unload a globally shared bundle.
	/// </summary>
	public sealed class WorldAssetRegistry : IDisposable
	{
		private sealed class Entry
		{
			public ObjectAsset Asset;
			public int ReferenceCount;
		}

		private readonly Func<Guid, ObjectAsset> objectAssetResolver;
		private readonly Dictionary<Guid, Entry> objectAssets = new Dictionary<Guid, Entry>();

		public int ActiveObjectAssetCount => objectAssets.Count;
		public int TotalObjectAssetReferenceCount { get; private set; }
		public int PeakObjectAssetCount { get; private set; }
		public int PeakObjectAssetReferenceCount { get; private set; }

		public WorldAssetRegistry()
			: this(guid => Assets.find(new AssetReference<ObjectAsset>(guid)))
		{
		}

		public WorldAssetRegistry(Func<Guid, ObjectAsset> objectAssetResolver)
		{
			this.objectAssetResolver = objectAssetResolver ?? throw new ArgumentNullException(nameof(objectAssetResolver));
		}

		public ObjectAsset AcquireObjectAsset(string assetGuid)
		{
			if (!Guid.TryParse(assetGuid, out Guid guid))
				return null;
			if (!objectAssets.TryGetValue(guid, out Entry entry))
			{
				ObjectAsset asset = objectAssetResolver(guid);
				if (asset == null)
					return null;
				entry = new Entry { Asset = asset };
				objectAssets.Add(guid, entry);
			}
			entry.ReferenceCount++;
			TotalObjectAssetReferenceCount++;
			PeakObjectAssetCount = Math.Max(PeakObjectAssetCount, ActiveObjectAssetCount);
			PeakObjectAssetReferenceCount = Math.Max(PeakObjectAssetReferenceCount, TotalObjectAssetReferenceCount);
			return entry.Asset;
		}

		public bool ReleaseObjectAsset(string assetGuid)
		{
			if (!Guid.TryParse(assetGuid, out Guid guid) || !objectAssets.TryGetValue(guid, out Entry entry))
				return false;
			entry.ReferenceCount--;
			TotalObjectAssetReferenceCount--;
			if (entry.ReferenceCount <= 0)
				objectAssets.Remove(guid);
			return true;
		}

		public int GetObjectAssetReferenceCount(string assetGuid)
		{
			return Guid.TryParse(assetGuid, out Guid guid) && objectAssets.TryGetValue(guid, out Entry entry)
				? entry.ReferenceCount
				: 0;
		}

		public void Dispose()
		{
			objectAssets.Clear();
			TotalObjectAssetReferenceCount = 0;
		}
	}
}
