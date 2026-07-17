////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Security.Cryptography;
using System.Text;

namespace SDG.Unturned.WorldUpgrade.Editor
{
	public static class WorldStableIdUtility
	{
		public static string Create(string prefix, string namespaceId, string canonicalKey)
		{
			if (string.IsNullOrWhiteSpace(prefix))
				throw new ArgumentException("Stable ID prefix is required.", nameof(prefix));
			if (string.IsNullOrWhiteSpace(canonicalKey))
				throw new ArgumentException("Stable ID canonical key is required.", nameof(canonicalKey));

			string input = (namespaceId ?? string.Empty) + "\n" + canonicalKey;
			byte[] bytes = Encoding.UTF8.GetBytes(input);
			using (SHA256 sha256 = SHA256.Create())
			{
				byte[] hash = sha256.ComputeHash(bytes);
				StringBuilder builder = new StringBuilder(prefix.Length + 33);
				builder.Append(prefix);
				builder.Append('_');
				for (int index = 0; index < 16; ++index)
					builder.Append(hash[index].ToString("x2"));
				return builder.ToString();
			}
		}

		public static string Sha256(string canonicalText)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(canonicalText ?? string.Empty);
			using (SHA256 sha256 = SHA256.Create())
			{
				byte[] hash = sha256.ComputeHash(bytes);
				return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
			}
		}
	}
}
