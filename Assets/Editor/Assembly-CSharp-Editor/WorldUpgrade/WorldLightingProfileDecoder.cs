////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Unturned.WorldUpgrade;
using System;
using System.IO;
using System.Security.Cryptography;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public static class WorldLightingProfileDecoder
	{
		public const int ExpectedVersion = 12;
		public const int ExpectedLength = 268;

		public static WorldEnvironmentProfileData Decode(string zoneId, string displayName, string lightingPath)
		{
			if (!File.Exists(lightingPath))
				throw new FileNotFoundException("Lighting.dat was not found.", lightingPath);
			byte[] bytes = File.ReadAllBytes(lightingPath);
			return Decode(zoneId, displayName, bytes, "Environment/Lighting.dat");
		}

		public static WorldEnvironmentProfileData Decode(string zoneId, string displayName, byte[] bytes, string relativePath)
		{
			StrictBinaryReader reader = new StrictBinaryReader(bytes, relativePath);
			WorldEnvironmentProfileData profile = new WorldEnvironmentProfileData
			{
				ZoneId = zoneId,
				DisplayName = displayName,
				SourceRelativePath = relativePath,
				SourceContentSha256 = Sha256(bytes),
				SourceVersion = reader.ReadByte(),
			};
			if (profile.SourceVersion != ExpectedVersion)
				throw reader.Error("unsupported Lighting.dat version " + profile.SourceVersion + "; expected " + ExpectedVersion);
			profile.Azimuth = reader.ReadSingle();
			profile.Bias = reader.ReadSingle();
			profile.Fade = reader.ReadSingle();
			profile.Time = reader.ReadSingle();
			profile.Moon = reader.ReadByte();
			profile.SeaLevel = reader.ReadSingle();
			profile.SnowLevel = reader.ReadSingle();
			profile.CanRain = reader.ReadBoolean();
			profile.CanSnow = reader.ReadBoolean();
			profile.RainFrequency = reader.ReadSingle();
			profile.RainDuration = reader.ReadSingle();
			profile.SnowFrequency = reader.ReadSingle();
			profile.SnowDuration = reader.ReadSingle();
			for (int timeIndex = 0; timeIndex < 4; ++timeIndex)
			{
				WorldEnvironmentTimeProfileData sample = new WorldEnvironmentTimeProfileData();
				for (int colorIndex = 0; colorIndex < 12; ++colorIndex)
					sample.Colors.Add(new WorldEnvironmentColorData { R = reader.ReadByte(), G = reader.ReadByte(), B = reader.ReadByte() });
				for (int singleIndex = 0; singleIndex < 5; ++singleIndex)
					sample.Singles.Add(reader.ReadSingle());
				profile.Times.Add(sample);
			}
			profile.SourceBytesConsumed = reader.Position;
			profile.SourceTrailingBytes = reader.Remaining;
			if (bytes.Length != ExpectedLength || reader.Remaining != 0)
				throw reader.Error("Lighting.dat must be exactly " + ExpectedLength + " bytes; found " + bytes.Length);
			profile.Validate();
			return profile;
		}

		private static string Sha256(byte[] bytes)
		{
			using (SHA256 hash = SHA256.Create())
				return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
		}
	}
}
