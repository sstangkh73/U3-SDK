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
	internal sealed class RawMapDecodeContext
	{
		public string SourceRoot { get; }
		public RawMapDecodedSummaryData Summary { get; }

		public RawMapDecodeContext(string sourceRoot, RawMapDecodedSummaryData summary)
		{
			SourceRoot = sourceRoot;
			Summary = summary;
		}

		public string GetFullPath(string relativePath)
		{
			return Path.Combine(SourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
		}

		public void AddError(string code, string relativePath, long offset, string message)
		{
			Summary.Issues.Add(new RawMapDecodeIssueData
			{
				Severity = "Error",
				Code = code,
				RelativePath = NormalizePath(relativePath),
				Offset = offset,
				Message = message,
			});
		}

		public void AddWarning(string code, string relativePath, long offset, string message)
		{
			Summary.Issues.Add(new RawMapDecodeIssueData
			{
				Severity = "Warning",
				Code = code,
				RelativePath = NormalizePath(relativePath),
				Offset = offset,
				Message = message,
			});
		}

		public void Capture(string relativePath, System.Action action)
		{
			try
			{
				action();
			}
			catch (RawMapBinaryFormatException exception)
			{
				AddError("BinaryFormat", relativePath, exception.Offset, exception.Message);
			}
			catch (Exception exception)
			{
				AddError("DecodeException", relativePath, -1, exception.GetType().Name + ": " + exception.Message);
			}
		}

		public static void Include(RawMapBoundsData bounds, RawMapVector3Data point)
		{
			if (!bounds.HasValue)
			{
				bounds.HasValue = true;
				bounds.Min = new RawMapVector3Data(point.X, point.Y, point.Z);
				bounds.Max = new RawMapVector3Data(point.X, point.Y, point.Z);
				return;
			}

			bounds.Min.X = Math.Min(bounds.Min.X, point.X);
			bounds.Min.Y = Math.Min(bounds.Min.Y, point.Y);
			bounds.Min.Z = Math.Min(bounds.Min.Z, point.Z);
			bounds.Max.X = Math.Max(bounds.Max.X, point.X);
			bounds.Max.Y = Math.Max(bounds.Max.Y, point.Y);
			bounds.Max.Z = Math.Max(bounds.Max.Z, point.Z);
		}

		public static string NormalizePath(string path)
		{
			return string.IsNullOrEmpty(path) ? path : path.Replace('\\', '/');
		}

		public static string CalculateSha256(byte[] bytes)
		{
			using (SHA256 sha256 = SHA256.Create())
			{
				byte[] hash = sha256.ComputeHash(bytes);
				return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
			}
		}

		public static string CalculateFileSha256(string path)
		{
			using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (SHA256 sha256 = SHA256.Create())
			{
				byte[] hash = sha256.ComputeHash(stream);
				return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
			}
		}
	}
}
