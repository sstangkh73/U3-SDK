////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Unturned.WorldUpgrade;
using System;
using System.Collections.Generic;
using System.IO;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	internal static class RawMapSpawnDecoder
	{
		private const int REGION_AXIS_COUNT = 64;

		internal static void DecodeAll(RawMapDecodeContext context)
		{
			RawMapSpawnFileSummaryData items = null;
			RawMapSpawnFileSummaryData zombies = null;
			context.Capture("Spawns/Items.dat", () => items = DecodeItems(context));
			context.Capture("Spawns/Jars.dat", () => DecodeRegionPoints(context, "Spawns/Jars.dat", items?.TableCount ?? 0));
			context.Capture("Spawns/Zombies.dat", () => zombies = DecodeZombies(context));
			context.Capture("Spawns/Animals.dat", () => DecodeRegionPoints(context, "Spawns/Animals.dat", zombies?.TableCount ?? 0));
			context.Capture("Spawns/Fauna.dat", () => DecodeFauna(context));
			context.Capture("Spawns/Vehicles.dat", () => DecodeVehicles(context));
			context.Capture("Spawns/Players.dat", () => DecodePlayers(context));
		}

		internal static RawMapSpawnFileSummaryData DecodeItems(RawMapDecodeContext context)
		{
			const string path = "Spawns/Items.dat";
			RawMapSpawnFileSummaryData summary = CreateSummary(context, path);
			if (!summary.Present)
				return summary;

			StrictBinaryReader reader = StrictBinaryReader.FromFile(context.GetFullPath(path), path);
			summary.Version = reader.ReadByte();
			if (summary.Version > 1 && summary.Version < 3)
				reader.ReadUInt64();

			int tableCount = reader.ReadByte();
			summary.TableCount = tableCount;
			for (int tableIndex = 0; tableIndex < tableCount; ++tableIndex)
			{
				reader.Skip(3, "item table color");
				RawMapTableSummaryData table = new RawMapTableSummaryData
				{
					Index = tableIndex,
					Name = reader.ReadByteLengthUtf8String(),
					LegacyTableId = summary.Version > 3 ? reader.ReadUInt16() : 0,
				};

				int tierCount = reader.ReadByte();
				table.TierCount = tierCount;
				for (int tierIndex = 0; tierIndex < tierCount; ++tierIndex)
				{
					reader.ReadByteLengthUtf8String();
					reader.ReadSingle();
					int entryCount = reader.ReadByte();
					table.EntryCount += entryCount;
					for (int entryIndex = 0; entryIndex < entryCount; ++entryIndex)
						reader.ReadUInt16();
				}
				summary.Tables.Add(table);
			}

			Finish(context, summary, reader);
			return summary;
		}

		internal static RawMapSpawnFileSummaryData DecodeZombies(RawMapDecodeContext context)
		{
			const string path = "Spawns/Zombies.dat";
			RawMapSpawnFileSummaryData summary = CreateSummary(context, path);
			if (!summary.Present)
				return summary;

			StrictBinaryReader reader = StrictBinaryReader.FromFile(context.GetFullPath(path), path);
			summary.Version = reader.ReadByte();
			if (summary.Version > 3 && summary.Version < 5)
				reader.ReadUInt64();

			if (summary.Version >= 10)
				reader.ReadInt32(); // nextTableUniqueId

			int tableCount = reader.ReadByte();
			summary.TableCount = tableCount;
			HashSet<int> uniqueIds = new HashSet<int>();
			for (int tableIndex = 0; tableIndex < tableCount; ++tableIndex)
			{
				RawMapTableSummaryData table = new RawMapTableSummaryData { Index = tableIndex };
				if (summary.Version >= 10)
				{
					table.UniqueId = reader.ReadInt32();
					if (table.UniqueId <= 0 || !uniqueIds.Add(table.UniqueId))
						context.AddError("ZombieTableUniqueId", path, reader.Position - 4, "Zombie table unique ID must be positive and unique: " + table.UniqueId);
				}

				reader.Skip(3, "zombie table color");
				table.Name = reader.ReadByteLengthUtf8String();
				if (summary.Version > 2)
				{
					table.IsMega = reader.ReadBoolean();
					reader.ReadUInt16(); // health
					reader.ReadByte(); // damage
					reader.ReadByte(); // loot table index
					table.LegacyTableId = summary.Version > 6 ? reader.ReadUInt16() : 0;
					if (summary.Version > 7)
						reader.ReadUInt32(); // XP
					if (summary.Version > 5)
						reader.ReadSingle(); // regen
					if (summary.Version > 8)
						reader.ReadByteLengthUtf8String(); // difficulty GUID/string
				}
				else
				{
					reader.ReadByte(); // legacy loot table index
				}

				int slotCount = reader.ReadByte();
				if (slotCount > 4)
					throw reader.Error("zombie slot count exceeds four: " + slotCount);
				table.TierCount = slotCount;
				for (int slotIndex = 0; slotIndex < slotCount; ++slotIndex)
				{
					reader.ReadSingle();
					int clothCount = reader.ReadByte();
					table.EntryCount += clothCount;
					for (int clothIndex = 0; clothIndex < clothCount; ++clothIndex)
						reader.ReadUInt16();
				}
				summary.Tables.Add(table);
			}

			Finish(context, summary, reader);
			return summary;
		}

		internal static RawMapSpawnFileSummaryData DecodeFauna(RawMapDecodeContext context)
		{
			const string path = "Spawns/Fauna.dat";
			RawMapSpawnFileSummaryData summary = CreateSummary(context, path);
			if (!summary.Present)
				return summary;

			StrictBinaryReader reader = StrictBinaryReader.FromFile(context.GetFullPath(path), path);
			summary.Version = reader.ReadByte();
			int tableCount = reader.ReadByte();
			summary.TableCount = tableCount;
			for (int tableIndex = 0; tableIndex < tableCount; ++tableIndex)
			{
				reader.Skip(3, "animal table color");
				RawMapTableSummaryData table = new RawMapTableSummaryData
				{
					Index = tableIndex,
					Name = reader.ReadByteLengthUtf8String(),
					LegacyTableId = summary.Version > 2 ? reader.ReadUInt16() : 0,
				};
				int tierCount = reader.ReadByte();
				table.TierCount = tierCount;
				for (int tierIndex = 0; tierIndex < tierCount; ++tierIndex)
				{
					reader.ReadByteLengthUtf8String();
					reader.ReadSingle();
					int entryCount = reader.ReadByte();
					table.EntryCount += entryCount;
					for (int entryIndex = 0; entryIndex < entryCount; ++entryIndex)
						reader.ReadUInt16();
				}
				summary.Tables.Add(table);
			}

			int pointCount = reader.ReadUInt16();
			for (int index = 0; index < pointCount; ++index)
			{
				int type = reader.ReadByte();
				RawMapVector3Data point = reader.ReadVector3();
				RawMapDecodeContext.Include(summary.SpawnBounds, point);
				if (type >= tableCount)
					summary.InvalidTableReferenceCount++;
			}
			summary.SpawnPointCount = pointCount;
			Finish(context, summary, reader);
			return summary;
		}

		internal static RawMapSpawnFileSummaryData DecodeVehicles(RawMapDecodeContext context)
		{
			const string path = "Spawns/Vehicles.dat";
			RawMapSpawnFileSummaryData summary = CreateSummary(context, path);
			if (!summary.Present)
				return summary;

			StrictBinaryReader reader = StrictBinaryReader.FromFile(context.GetFullPath(path), path);
			summary.Version = reader.ReadByte();
			if (summary.Version > 1 && summary.Version < 3)
				reader.ReadUInt64();

			int tableCount = reader.ReadByte();
			summary.TableCount = tableCount;
			for (int tableIndex = 0; tableIndex < tableCount; ++tableIndex)
			{
				reader.Skip(3, "vehicle table color");
				RawMapTableSummaryData table = new RawMapTableSummaryData
				{
					Index = tableIndex,
					Name = reader.ReadByteLengthUtf8String(),
					LegacyTableId = summary.Version > 3 ? reader.ReadUInt16() : 0,
				};
				int tierCount = reader.ReadByte();
				table.TierCount = tierCount;
				for (int tierIndex = 0; tierIndex < tierCount; ++tierIndex)
				{
					reader.ReadByteLengthUtf8String();
					reader.ReadSingle();
					int entryCount = reader.ReadByte();
					table.EntryCount += entryCount;
					for (int entryIndex = 0; entryIndex < entryCount; ++entryIndex)
						reader.ReadUInt16();
				}
				summary.Tables.Add(table);
			}

			int pointCount = reader.ReadUInt16();
			for (int index = 0; index < pointCount; ++index)
			{
				int type = reader.ReadByte();
				RawMapVector3Data point = reader.ReadVector3();
				reader.ReadByte(); // angle / 2
				RawMapDecodeContext.Include(summary.SpawnBounds, point);
				if (type >= tableCount)
					summary.InvalidTableReferenceCount++;
			}
			summary.SpawnPointCount = pointCount;
			Finish(context, summary, reader);
			return summary;
		}

		internal static RawMapSpawnFileSummaryData DecodePlayers(RawMapDecodeContext context)
		{
			const string path = "Spawns/Players.dat";
			RawMapSpawnFileSummaryData summary = CreateSummary(context, path);
			if (!summary.Present)
				return summary;

			StrictBinaryReader reader = StrictBinaryReader.FromFile(context.GetFullPath(path), path);
			summary.Version = reader.ReadByte();
			if (summary.Version > 1 && summary.Version < 3)
				reader.ReadUInt64();

			int pointCount = reader.ReadByte();
			for (int index = 0; index < pointCount; ++index)
			{
				RawMapVector3Data point = reader.ReadVector3();
				reader.ReadByte(); // angle / 2
				if (summary.Version > 3)
					reader.ReadBoolean(); // alternate spawn
				RawMapDecodeContext.Include(summary.SpawnBounds, point);
			}
			summary.SpawnPointCount = pointCount;
			Finish(context, summary, reader);
			return summary;
		}

		internal static RawMapSpawnFileSummaryData DecodeRegionPoints(RawMapDecodeContext context, string path, int tableCount)
		{
			RawMapSpawnFileSummaryData summary = CreateSummary(context, path);
			if (!summary.Present)
				return summary;

			StrictBinaryReader reader = StrictBinaryReader.FromFile(context.GetFullPath(path), path);
			summary.Version = reader.ReadByte();
			if (summary.Version > 0)
			{
				for (int x = 0; x < REGION_AXIS_COUNT; ++x)
				{
					for (int y = 0; y < REGION_AXIS_COUNT; ++y)
					{
						int count = reader.ReadUInt16();
						if (count > 0)
							summary.RegionsWithSpawnPoints++;
						for (int index = 0; index < count; ++index)
						{
							int type = reader.ReadByte();
							RawMapVector3Data point = reader.ReadVector3();
							RawMapDecodeContext.Include(summary.SpawnBounds, point);
							if (type >= tableCount)
								summary.InvalidTableReferenceCount++;
						}
						summary.SpawnPointCount += count;
					}
				}
			}

			Finish(context, summary, reader);
			return summary;
		}

		private static RawMapSpawnFileSummaryData CreateSummary(RawMapDecodeContext context, string path)
		{
			string fullPath = context.GetFullPath(path);
			RawMapSpawnFileSummaryData summary = new RawMapSpawnFileSummaryData
			{
				RelativePath = path,
				Present = File.Exists(fullPath),
				FileSizeInBytes = File.Exists(fullPath) ? checked((int) new FileInfo(fullPath).Length) : 0,
				ContentSha256 = File.Exists(fullPath) ? RawMapDecodeContext.CalculateFileSha256(fullPath) : null,
			};
			context.Summary.SpawnFiles.Add(summary);
			if (!summary.Present)
				context.AddWarning("MissingSpawnFile", path, -1, "Spawn file is not present; decoded count is zero.");
			return summary;
		}

		private static void Finish(RawMapDecodeContext context, RawMapSpawnFileSummaryData summary, StrictBinaryReader reader)
		{
			summary.BytesConsumed = reader.Position;
			summary.TrailingBytes = reader.Remaining;
			if (summary.InvalidTableReferenceCount > 0)
				context.AddError("InvalidSpawnTableReference", summary.RelativePath, -1, summary.InvalidTableReferenceCount + " spawn point(s) reference a table index outside the decoded table list.");
			if (summary.TrailingBytes > 0)
				context.AddWarning("TrailingBytes", summary.RelativePath, reader.Position, summary.TrailingBytes + " trailing byte(s) were not consumed by the known format.");
		}
	}
}
