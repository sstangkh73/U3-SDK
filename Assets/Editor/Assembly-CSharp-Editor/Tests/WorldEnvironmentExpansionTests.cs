////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using NUnit.Framework;
using SDG.Unturned.WorldUpgrade;
using SDG.Unturned.WorldUpgrade.Editor;
using System;

namespace SDG.Unturned.Tests
{
	public class WorldEnvironmentExpansionTests
	{
		[Test]
		public void LightingDecoderConsumesExactVersion12Payload()
		{
			byte[] bytes = CreateLightingBytes();
			WorldEnvironmentProfileData profile = WorldLightingProfileDecoder.Decode("test", "Test", bytes, "Lighting.dat");
			Assert.AreEqual(12, profile.SourceVersion);
			Assert.AreEqual(268, profile.SourceBytesConsumed);
			Assert.AreEqual(4, profile.Times.Count);
			Assert.AreEqual(12, profile.Times[0].Colors.Count);
			Assert.AreEqual(5, profile.Times[0].Singles.Count);
		}

		[Test]
		public void LightingDecoderRejectsTrailingBytes()
		{
			byte[] bytes = new byte[269];
			Array.Copy(CreateLightingBytes(), bytes, 268);
			Assert.Throws<RawMapBinaryFormatException>(() => WorldLightingProfileDecoder.Decode("test", "Test", bytes, "Lighting.dat"));
		}

		[Test]
		public void EnvironmentBlendHasExactEndpointsAndSmoothMidpoint()
		{
			WorldEnvironmentProfileData source = WorldLightingProfileDecoder.Decode("a", "A", CreateLightingBytes(0), "Lighting.dat");
			WorldEnvironmentProfileData destination = WorldLightingProfileDecoder.Decode("b", "B", CreateLightingBytes(120), "Lighting.dat");
			WorldBlendedEnvironmentStateData start = WorldEnvironmentBlender.Sample(source, destination, 0, 0f);
			WorldBlendedEnvironmentStateData middle = WorldEnvironmentBlender.Sample(source, destination, 0, 0.5f);
			WorldBlendedEnvironmentStateData end = WorldEnvironmentBlender.Sample(source, destination, 0, 1f);
			Assert.AreEqual(0f, start.Blend);
			Assert.AreEqual(0.5f, middle.Blend);
			Assert.AreEqual(1f, end.Blend);
			Assert.AreEqual(source.Times[0].Colors[0].R / 255f, start.Colors[0]);
			Assert.AreEqual(destination.Times[0].Colors[0].R / 255f, end.Colors[0]);
		}

		[Test]
		public void CellPerformanceAuditFlagsDenseCells()
		{
			WorldZoneDefinitionData zone = new WorldZoneDefinitionData { ZoneKey = "dense" };
			WorldCellIndexData cell = new WorldCellIndexData { EntityCount = 9000 };
			cell.RecordCounts.Add(new WorldRecordKindCountData { Kind = "StaticObject", Count = 6000 });
			zone.Cells.Add(cell);
			WorldCellPerformanceAuditData audit = WorldCellPerformanceAuditor.Audit(zone, new WorldCellPerformanceBudgetData());
			Assert.IsTrue(audit.RequiresHlod);
			Assert.AreEqual(1, audit.CellsOverEntityBudget);
			Assert.AreEqual(1, audit.CellsOverStaticObjectBudget);
		}

		private static byte[] CreateLightingBytes(byte colorOffset = 0)
		{
			byte[] bytes = new byte[268];
			int offset = 0;
			bytes[offset++] = 12;
			for (int index = 0; index < 4; ++index) WriteSingle(bytes, ref offset, index + 0.25f);
			bytes[offset++] = 2;
			WriteSingle(bytes, ref offset, 0.2f);
			WriteSingle(bytes, ref offset, 0.8f);
			bytes[offset++] = 1;
			bytes[offset++] = 0;
			for (int index = 0; index < 4; ++index) WriteSingle(bytes, ref offset, index + 1f);
			for (int time = 0; time < 4; ++time)
			{
				for (int color = 0; color < 12; ++color)
				{
					bytes[offset++] = (byte) (colorOffset + color);
					bytes[offset++] = (byte) (colorOffset + color + 1);
					bytes[offset++] = (byte) (colorOffset + color + 2);
				}
				for (int single = 0; single < 5; ++single) WriteSingle(bytes, ref offset, single / 5f);
			}
			Assert.AreEqual(bytes.Length, offset);
			return bytes;
		}

		private static void WriteSingle(byte[] bytes, ref int offset, float value)
		{
			byte[] encoded = BitConverter.GetBytes(value);
			Array.Copy(encoded, 0, bytes, offset, encoded.Length);
			offset += encoded.Length;
		}
	}
}
