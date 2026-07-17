////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public sealed class WorldEnvironmentColorData
	{
		public byte R;
		public byte G;
		public byte B;
	}

	[Serializable]
	public sealed class WorldEnvironmentTimeProfileData
	{
		public List<WorldEnvironmentColorData> Colors = new List<WorldEnvironmentColorData>();
		public List<float> Singles = new List<float>();
	}

	[Serializable]
	public sealed class WorldEnvironmentProfileData
	{
		public int SchemaVersion = 1;
		public string ZoneId;
		public string DisplayName;
		public string SourceRelativePath;
		public string SourceContentSha256;
		public int SourceVersion;
		public int SourceBytesConsumed;
		public int SourceTrailingBytes;
		public float Azimuth;
		public float Bias;
		public float Fade;
		public float Time;
		public byte Moon;
		public float SeaLevel;
		public float SnowLevel;
		public bool CanRain;
		public bool CanSnow;
		public float RainFrequency;
		public float RainDuration;
		public float SnowFrequency;
		public float SnowDuration;
		public bool LightingSourceDerived = true;
		public bool WaterSurfaceSourceDerived = true;
		public bool OxygenSourceDerived;
		public bool AmbienceAssetSourceDerived;
		public List<WorldEnvironmentTimeProfileData> Times = new List<WorldEnvironmentTimeProfileData>();

		public void Validate()
		{
			if (string.IsNullOrWhiteSpace(ZoneId))
				throw new InvalidOperationException("Environment profile zone ID is required.");
			if (SourceVersion != 12 || SourceBytesConsumed != 268 || SourceTrailingBytes != 0)
				throw new InvalidOperationException("Environment profile must be an exact Lighting.dat v12 decode.");
			if (Times == null || Times.Count != 4)
				throw new InvalidOperationException("Environment profile must contain four time samples.");
			foreach (WorldEnvironmentTimeProfileData sample in Times)
			{
				if (sample == null || sample.Colors == null || sample.Colors.Count != 12)
					throw new InvalidOperationException("Each environment time sample must contain twelve colors.");
				if (sample.Singles == null || sample.Singles.Count != 5)
					throw new InvalidOperationException("Each environment time sample must contain five scalar values.");
				foreach (float value in sample.Singles)
					if (float.IsNaN(value) || float.IsInfinity(value))
						throw new InvalidOperationException("Environment scalar values must be finite.");
			}
		}
	}

	[Serializable]
	public sealed class WorldEnvironmentProfileSetData
	{
		public int SchemaVersion = 1;
		public List<WorldEnvironmentProfileData> Profiles = new List<WorldEnvironmentProfileData>();
	}

	[Serializable]
	public sealed class WorldBlendedEnvironmentStateData
	{
		public float Blend;
		public float Azimuth;
		public float Bias;
		public float Fade;
		public float Time;
		public float Moon;
		public float SeaLevel;
		public float SnowLevel;
		public float RainCapability;
		public float SnowCapability;
		public float RainFrequency;
		public float RainDuration;
		public float SnowFrequency;
		public float SnowDuration;
		public List<float> Colors = new List<float>();
		public List<float> Singles = new List<float>();
	}

	public static class WorldEnvironmentBlender
	{
		public static WorldBlendedEnvironmentStateData Sample(WorldEnvironmentProfileData source,
			WorldEnvironmentProfileData destination, int timeIndex, float blend)
		{
			if (source == null || destination == null)
				throw new ArgumentNullException(source == null ? nameof(source) : nameof(destination));
			source.Validate();
			destination.Validate();
			if (timeIndex < 0 || timeIndex >= source.Times.Count)
				throw new ArgumentOutOfRangeException(nameof(timeIndex));
			float t = SmoothStep(Clamp01(blend));
			WorldBlendedEnvironmentStateData state = new WorldBlendedEnvironmentStateData
			{
				Blend = t,
				Azimuth = Lerp(source.Azimuth, destination.Azimuth, t),
				Bias = Lerp(source.Bias, destination.Bias, t),
				Fade = Lerp(source.Fade, destination.Fade, t),
				Time = Lerp(source.Time, destination.Time, t),
				Moon = Lerp(source.Moon, destination.Moon, t),
				SeaLevel = Lerp(source.SeaLevel, destination.SeaLevel, t),
				SnowLevel = Lerp(source.SnowLevel, destination.SnowLevel, t),
				RainCapability = Lerp(source.CanRain ? 1f : 0f, destination.CanRain ? 1f : 0f, t),
				SnowCapability = Lerp(source.CanSnow ? 1f : 0f, destination.CanSnow ? 1f : 0f, t),
				RainFrequency = Lerp(source.RainFrequency, destination.RainFrequency, t),
				RainDuration = Lerp(source.RainDuration, destination.RainDuration, t),
				SnowFrequency = Lerp(source.SnowFrequency, destination.SnowFrequency, t),
				SnowDuration = Lerp(source.SnowDuration, destination.SnowDuration, t),
			};
			WorldEnvironmentTimeProfileData sourceTime = source.Times[timeIndex];
			WorldEnvironmentTimeProfileData destinationTime = destination.Times[timeIndex];
			for (int colorIndex = 0; colorIndex < 12; ++colorIndex)
			{
				WorldEnvironmentColorData a = sourceTime.Colors[colorIndex];
				WorldEnvironmentColorData b = destinationTime.Colors[colorIndex];
				state.Colors.Add(Lerp(a.R / 255f, b.R / 255f, t));
				state.Colors.Add(Lerp(a.G / 255f, b.G / 255f, t));
				state.Colors.Add(Lerp(a.B / 255f, b.B / 255f, t));
			}
			for (int singleIndex = 0; singleIndex < 5; ++singleIndex)
				state.Singles.Add(Lerp(sourceTime.Singles[singleIndex], destinationTime.Singles[singleIndex], t));
			return state;
		}

		private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
		private static float SmoothStep(float value) => value * value * (3f - 2f * value);
		private static float Lerp(float a, float b, float value)
		{
			if (value <= 0f)
				return a;
			if (value >= 1f)
				return b;
			return a + (b - a) * value;
		}
	}
}
