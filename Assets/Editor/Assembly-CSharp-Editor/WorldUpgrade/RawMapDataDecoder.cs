////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Unturned.WorldUpgrade;
using System;
using System.IO;
using System.Linq;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public static class RawMapDataDecoder
	{
		public static RawMapDecodedSummaryData Decode(string zoneId, string displayName, string sourceRoot,
			string sourceInventoryFingerprintSha256)
		{
			if (string.IsNullOrWhiteSpace(zoneId))
				throw new ArgumentException("Zone ID is required.", nameof(zoneId));
			if (string.IsNullOrWhiteSpace(sourceRoot))
				throw new ArgumentException("Source root is required.", nameof(sourceRoot));

			string fullRoot = Path.GetFullPath(sourceRoot);
			if (!Directory.Exists(fullRoot))
				throw new DirectoryNotFoundException("Raw map source root does not exist: " + fullRoot);

			RawMapDecodedSummaryData summary = new RawMapDecodedSummaryData
			{
				ZoneId = zoneId,
				DisplayName = displayName,
				SourceRoot = RawMapDecodeContext.NormalizePath(fullRoot),
				SourceInventoryFingerprintSha256 = sourceInventoryFingerprintSha256,
				GeneratedUtc = DateTime.UtcNow.ToString("O"),
			};
			RawMapDecodeContext context = new RawMapDecodeContext(fullRoot, summary);

			RawMapSpawnDecoder.DecodeAll(context);
			RawMapGeometryDecoder.DecodeAll(context);

			summary.IsValid = !summary.Issues.Any(issue => string.Equals(issue.Severity, "Error", StringComparison.Ordinal));
			return summary;
		}
	}
}
