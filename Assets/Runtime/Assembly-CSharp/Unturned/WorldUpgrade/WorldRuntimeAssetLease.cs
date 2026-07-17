////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade
{
	internal sealed class WorldRuntimeAssetLease : MonoBehaviour
	{
		private WorldAssetRegistry registry;
		private string assetGuid;

		public void Initialize(WorldAssetRegistry registry, string assetGuid)
		{
			this.registry = registry;
			this.assetGuid = assetGuid;
		}

		private void OnDestroy()
		{
			registry?.ReleaseObjectAsset(assetGuid);
			registry = null;
		}
	}
}
