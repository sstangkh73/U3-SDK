////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Unturned.WorldUpgrade;
using System;
using System.IO;
using System.Text;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public sealed class RawMapBinaryFormatException : IOException
	{
		public string RelativePath { get; }
		public int Offset { get; }

		public RawMapBinaryFormatException(string relativePath, int offset, string message)
			: base(relativePath + " at byte " + offset + ": " + message)
		{
			RelativePath = relativePath;
			Offset = offset;
		}
	}

	/// <summary>
	/// Read-only little-endian reader for legacy Unturned map data. Unlike Block and River it throws
	/// on truncated data rather than silently returning zero, which makes it suitable for import validation.
	/// </summary>
	public sealed class StrictBinaryReader
	{
		private readonly byte[] bytes;
		private readonly string relativePath;
		private int offset;

		public int Position => offset;
		public int Length => bytes.Length;
		public int Remaining => bytes.Length - offset;

		public StrictBinaryReader(byte[] bytes, string relativePath)
		{
			this.bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
			this.relativePath = string.IsNullOrEmpty(relativePath) ? "<memory>" : relativePath;
		}

		public static StrictBinaryReader FromFile(string fullPath, string relativePath)
		{
			return new StrictBinaryReader(File.ReadAllBytes(fullPath), relativePath);
		}

		public byte ReadByte()
		{
			Require(1, "byte");
			return bytes[offset++];
		}

		public bool ReadBoolean()
		{
			byte value = ReadByte();
			if (value > 1)
				throw Error("boolean value must be 0 or 1, found " + value);
			return value != 0;
		}

		public ushort ReadUInt16()
		{
			Require(2, "UInt16");
			ushort value = (ushort) (bytes[offset] | (bytes[offset + 1] << 8));
			offset += 2;
			return value;
		}

		public int ReadInt32()
		{
			Require(4, "Int32");
			int value = bytes[offset]
				| (bytes[offset + 1] << 8)
				| (bytes[offset + 2] << 16)
				| (bytes[offset + 3] << 24);
			offset += 4;
			return value;
		}

		public uint ReadUInt32()
		{
			return unchecked((uint) ReadInt32());
		}

		public ulong ReadUInt64()
		{
			Require(8, "UInt64");
			ulong value = bytes[offset]
				| ((ulong) bytes[offset + 1] << 8)
				| ((ulong) bytes[offset + 2] << 16)
				| ((ulong) bytes[offset + 3] << 24)
				| ((ulong) bytes[offset + 4] << 32)
				| ((ulong) bytes[offset + 5] << 40)
				| ((ulong) bytes[offset + 6] << 48)
				| ((ulong) bytes[offset + 7] << 56);
			offset += 8;
			return value;
		}

		public float ReadSingle()
		{
			Require(4, "Single");
			float value = BitConverter.ToSingle(bytes, offset);
			offset += 4;
			if (float.IsNaN(value) || float.IsInfinity(value))
				throw Error("floating-point value is not finite");
			return value;
		}

		public RawMapVector3Data ReadVector3()
		{
			return new RawMapVector3Data(ReadSingle(), ReadSingle(), ReadSingle());
		}

		public string ReadByteLengthUtf8String()
		{
			int length = ReadByte();
			Require(length, "UTF-8 string payload");
			string value;
			try
			{
				value = new UTF8Encoding(false, true).GetString(bytes, offset, length);
			}
			catch (DecoderFallbackException exception)
			{
				throw Error("invalid UTF-8 string: " + exception.Message);
			}
			offset += length;
			return value;
		}

		public Guid ReadUInt16LengthGuid()
		{
			int length = ReadUInt16();
			if (length != 16)
				throw Error("GUID payload length must be 16, found " + length);
			Require(length, "GUID payload");
			byte[] guidBytes = new byte[16];
			Buffer.BlockCopy(bytes, offset, guidBytes, 0, 16);
			offset += 16;
			return new Guid(guidBytes);
		}

		public void Skip(int count, string description)
		{
			Require(count, description);
			offset += count;
		}

		public RawMapBinaryFormatException Error(string message)
		{
			return new RawMapBinaryFormatException(relativePath, offset, message);
		}

		private void Require(int count, string description)
		{
			if (count < 0 || offset > bytes.Length - count)
				throw Error("truncated while reading " + description + " (need " + count + ", remaining " + Remaining + ")");
		}
	}
}
