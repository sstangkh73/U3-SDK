////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using NUnit.Framework;
using SDG.Unturned.WorldUpgrade;
using SDG.Unturned.WorldUpgrade.Editor;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SDG.Unturned.Tests
{
	public class WorldUpgradeBinaryDecoderTests
	{
		private string tempRoot;

		[SetUp]
		public void SetUp()
		{
			tempRoot = Path.Combine(Path.GetTempPath(), "u3-world-upgrade-tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempRoot);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(tempRoot))
				Directory.Delete(tempRoot, true);
		}

		[Test]
		public void StrictReaderThrowsOnTruncatedInteger()
		{
			StrictBinaryReader reader = new StrictBinaryReader(new byte[] { 1 }, "test.dat");
			RawMapBinaryFormatException exception = Assert.Throws<RawMapBinaryFormatException>(() => reader.ReadUInt16());
			StringAssert.Contains("truncated", exception.Message);
			Assert.AreEqual(0, exception.Offset);
		}

		[Test]
		public void DecodesItemTablesAndRegionSpawnPoints()
		{
			string spawns = Directory.CreateDirectory(Path.Combine(tempRoot, "Spawns")).FullName;
			using (BinaryWriter writer = CreateWriter(Path.Combine(spawns, "Items.dat")))
			{
				writer.Write((byte) 4);
				writer.Write((byte) 1);
				writer.Write(new byte[] { 10, 20, 30 });
				WriteByteString(writer, "Food");
				writer.Write((ushort) 100);
				writer.Write((byte) 1);
				WriteByteString(writer, "Common");
				writer.Write(1.0f);
				writer.Write((byte) 2);
				writer.Write((ushort) 1);
				writer.Write((ushort) 2);
			}

			using (BinaryWriter writer = CreateWriter(Path.Combine(spawns, "Jars.dat")))
			{
				writer.Write((byte) 1);
				writer.Write((ushort) 1);
				writer.Write((byte) 0);
				WriteVector3(writer, 1, 2, 3);
				for (int index = 1; index < 64 * 64; ++index)
					writer.Write((ushort) 0);
			}

			RawMapDecodedSummaryData decoded = NewSummary();
			RawMapDecodeContext context = new RawMapDecodeContext(tempRoot, decoded);
			RawMapSpawnFileSummaryData items = RawMapSpawnDecoder.DecodeItems(context);
			RawMapSpawnFileSummaryData jars = RawMapSpawnDecoder.DecodeRegionPoints(context, "Spawns/Jars.dat", items.TableCount);

			Assert.AreEqual(1, items.TableCount);
			Assert.AreEqual("Food", items.Tables[0].Name);
			Assert.AreEqual(2, items.Tables[0].EntryCount);
			Assert.AreEqual(1, jars.SpawnPointCount);
			Assert.AreEqual(1, jars.RegionsWithSpawnPoints);
			Assert.AreEqual(1, jars.SpawnBounds.Min.X);
			Assert.AreEqual(3, jars.SpawnBounds.Max.Z);
			Assert.IsFalse(decoded.Issues.Any(issue => issue.Severity == "Error"));
		}

		[Test]
		public void ReportsTruncatedRegionSpawnFile()
		{
			string spawns = Directory.CreateDirectory(Path.Combine(tempRoot, "Spawns")).FullName;
			File.WriteAllBytes(Path.Combine(spawns, "Jars.dat"), new byte[] { 1, 1, 0, 0 });
			RawMapDecodedSummaryData decoded = NewSummary();
			RawMapDecodeContext context = new RawMapDecodeContext(tempRoot, decoded);

			context.Capture("Spawns/Jars.dat", () => RawMapSpawnDecoder.DecodeRegionPoints(context, "Spawns/Jars.dat", 1));

			Assert.IsTrue(decoded.Issues.Any(issue => issue.Severity == "Error" && issue.Code == "BinaryFormat"));
		}

		[Test]
		public void DecodesLandscapeDimensionsBoundsAndHoles()
		{
			string heightmaps = Directory.CreateDirectory(Path.Combine(tempRoot, "Landscape", "Heightmaps")).FullName;
			string splatmaps = Directory.CreateDirectory(Path.Combine(tempRoot, "Landscape", "Splatmaps")).FullName;
			string holes = Directory.CreateDirectory(Path.Combine(tempRoot, "Landscape", "Holes")).FullName;

			byte[] heightBytes = new byte[257 * 257 * 2];
			heightBytes[heightBytes.Length - 2] = 0xff;
			heightBytes[heightBytes.Length - 1] = 0xff;
			File.WriteAllBytes(Path.Combine(heightmaps, "Tile_-1_2_Source.heightmap"), heightBytes);

			byte[] splatBytes = new byte[256 * 256 * 8];
			for (int offset = 0; offset < splatBytes.Length; offset += 8)
				splatBytes[offset] = 0xff;
			File.WriteAllBytes(Path.Combine(splatmaps, "Tile_-1_2_Source.splatmap"), splatBytes);

			byte[] holesBytes = new byte[1 + (256 * 256 / 8)];
			holesBytes[0] = 1;
			holesBytes[1] = 1;
			File.WriteAllBytes(Path.Combine(holes, "Tile_-1_2.bin"), holesBytes);

			RawMapDecodedSummaryData decoded = NewSummary();
			RawMapDecodeContext context = new RawMapDecodeContext(tempRoot, decoded);
			RawMapGeometryDecoder.DecodeLandscape(context);

			Assert.AreEqual(1, decoded.Landscape.HeightmapTileCount);
			Assert.AreEqual(1, decoded.Landscape.SplatmapTileCount);
			Assert.AreEqual(1, decoded.Landscape.HoleTileCount);
			Assert.AreEqual(1, decoded.Landscape.TotalHoleCount);
			Assert.AreEqual(-1024, decoded.Landscape.WorldBounds.Min.X);
			Assert.AreEqual(3072, decoded.Landscape.WorldBounds.Max.Z);
			Assert.AreEqual(0, decoded.Landscape.Tiles[0].BlackSplatPixelCount);
		}

		[Test]
		public void RejectsInvalidLandscapeFileSize()
		{
			string heightmaps = Directory.CreateDirectory(Path.Combine(tempRoot, "Landscape", "Heightmaps")).FullName;
			File.WriteAllBytes(Path.Combine(heightmaps, "Tile_0_0_Source.heightmap"), new byte[3]);
			RawMapDecodedSummaryData decoded = NewSummary();
			RawMapDecodeContext context = new RawMapDecodeContext(tempRoot, decoded);

			RawMapGeometryDecoder.DecodeLandscape(context);

			Assert.AreEqual(1, decoded.Landscape.InvalidFileCount);
			Assert.IsTrue(decoded.Issues.Any(issue => issue.Code == "LandscapeHeightmapSize" && issue.Severity == "Error"));
		}

		[Test]
		public void ReportsSplatmapWithoutMatchingHeightmap()
		{
			string splatmaps = Directory.CreateDirectory(Path.Combine(tempRoot, "Landscape", "Splatmaps")).FullName;
			byte[] splatBytes = new byte[256 * 256 * 8];
			for (int offset = 0; offset < splatBytes.Length; offset += 8)
				splatBytes[offset] = 0xff;
			File.WriteAllBytes(Path.Combine(splatmaps, "Tile_3_4_Source.splatmap"), splatBytes);
			RawMapDecodedSummaryData decoded = NewSummary();
			RawMapDecodeContext context = new RawMapDecodeContext(tempRoot, decoded);

			RawMapGeometryDecoder.DecodeLandscape(context);

			Assert.AreEqual(1, decoded.Landscape.MissingHeightmapCount);
			Assert.IsTrue(decoded.Issues.Any(issue => issue.Code == "MissingLandscapeHeightmap" && issue.Severity == "Warning"));
		}

		[Test]
		public void DecodesVersionTwelveObjects()
		{
			string level = Directory.CreateDirectory(Path.Combine(tempRoot, "Level")).FullName;
			using (BinaryWriter writer = CreateWriter(Path.Combine(level, "Objects.dat")))
			{
				writer.Write((byte) 12);
				writer.Write((uint) 2);
				writer.Write((ushort) 1);
				WriteVector3(writer, 10, 20, 30);
				WriteVector3(writer, 0, 90, 0);
				WriteVector3(writer, 1, 1, 1);
				writer.Write((ushort) 100);
				WriteGuid(writer, Guid.NewGuid());
				writer.Write((byte) 0);
				writer.Write((uint) 1);
				WriteGuid(writer, Guid.Empty);
				writer.Write(-1);
				writer.Write(true);
				for (int index = 1; index < 64 * 64; ++index)
					writer.Write((ushort) 0);
			}

			RawMapDecodedSummaryData decoded = NewSummary();
			RawMapDecodeContext context = new RawMapDecodeContext(tempRoot, decoded);
			RawMapGeometryDecoder.DecodeObjects(context);

			Assert.AreEqual(12, decoded.Objects.Version);
			Assert.AreEqual(1, decoded.Objects.ObjectCount);
			Assert.AreEqual(0, decoded.Objects.DuplicateInstanceIdCount);
			Assert.AreEqual(0, decoded.Objects.TrailingBytes);
			Assert.AreEqual(30, decoded.Objects.Bounds.Max.Z);
		}

		[Test]
		public void DecodesRoadMaterialsAndVersionSixPaths()
		{
			string environment = Directory.CreateDirectory(Path.Combine(tempRoot, "Environment")).FullName;
			using (BinaryWriter writer = CreateWriter(Path.Combine(environment, "Roads.dat")))
			{
				writer.Write((byte) 2);
				writer.Write((byte) 1);
				writer.Write(8f);
				writer.Write(1f);
				writer.Write(0.5f);
				writer.Write(0f);
				writer.Write(true);
			}
			using (BinaryWriter writer = CreateWriter(Path.Combine(environment, "Paths.dat")))
			{
				writer.Write((byte) 6);
				writer.Write((ushort) 1);
				writer.Write((ushort) 2);
				writer.Write((byte) 0);
				writer.Write(false);
				WriteGuid(writer, Guid.NewGuid());
				WriteRoadJoint(writer, 1, 2, 3);
				WriteRoadJoint(writer, 4, 5, 6);
			}

			RawMapDecodedSummaryData decoded = NewSummary();
			RawMapDecodeContext context = new RawMapDecodeContext(tempRoot, decoded);
			RawMapGeometryDecoder.DecodeRoads(context);

			Assert.AreEqual(1, decoded.Roads.MaterialCount);
			Assert.AreEqual(1, decoded.Roads.PathCount);
			Assert.AreEqual(2, decoded.Roads.JointCount);
			Assert.AreEqual(0, decoded.Roads.PathsTrailingBytes);
			Assert.AreEqual(6, decoded.Roads.Bounds.Max.Z);
		}

		[Test]
		public void ReadsHierarchyTypeInventory()
		{
			string hierarchy = "\"Available_Instance_ID\" \"2\"\n\"Items\"\n[\n{\n\"Type\" \"Example.Type, Assembly-CSharp\"\n}\n]\n";
			File.WriteAllText(Path.Combine(tempRoot, "Level.hierarchy"), hierarchy, Encoding.UTF8);
			RawMapDecodedSummaryData decoded = NewSummary();
			RawMapDecodeContext context = new RawMapDecodeContext(tempRoot, decoded);

			RawMapGeometryDecoder.DecodeHierarchy(context);

			Assert.AreEqual(1, decoded.Hierarchy.ItemCount);
			Assert.AreEqual(2, decoded.Hierarchy.AvailableInstanceId);
			Assert.AreEqual("Example.Type, Assembly-CSharp", decoded.Hierarchy.Types.Single().TypeName);
		}

		private RawMapDecodedSummaryData NewSummary()
		{
			return new RawMapDecodedSummaryData { ZoneId = "test", DisplayName = "Test", SourceRoot = tempRoot };
		}

		private static BinaryWriter CreateWriter(string path)
		{
			return new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8);
		}

		private static void WriteByteString(BinaryWriter writer, string value)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(value);
			writer.Write(checked((byte) bytes.Length));
			writer.Write(bytes);
		}

		private static void WriteVector3(BinaryWriter writer, float x, float y, float z)
		{
			writer.Write(x);
			writer.Write(y);
			writer.Write(z);
		}

		private static void WriteGuid(BinaryWriter writer, Guid guid)
		{
			writer.Write((ushort) 16);
			writer.Write(guid.ToByteArray());
		}

		private static void WriteRoadJoint(BinaryWriter writer, float x, float y, float z)
		{
			WriteVector3(writer, x, y, z);
			WriteVector3(writer, -1, 0, 0);
			WriteVector3(writer, 1, 0, 0);
			writer.Write((byte) 0);
			writer.Write(0f);
			writer.Write(false);
		}
	}
}
