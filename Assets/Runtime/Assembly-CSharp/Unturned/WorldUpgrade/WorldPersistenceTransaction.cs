////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using Newtonsoft.Json;
using System;
using System.IO;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public sealed class WorldPersistenceJournalData
	{
		public int SchemaVersion = 1;
		public string TransactionId;
		public string PayloadSha256;
		public long Sequence;
		public string Stage;
	}

	public sealed class WorldPersistenceTransaction
	{
		private readonly string targetPath;
		private readonly string pendingPath;
		private readonly string journalPath;
		private readonly string backupPath;

		public bool RecoveryApplied { get; private set; }

		public WorldPersistenceTransaction(string targetPath)
		{
			if (string.IsNullOrWhiteSpace(targetPath))
				throw new ArgumentException("Persistence target path is required.", nameof(targetPath));
			this.targetPath = Path.GetFullPath(targetPath);
			pendingPath = this.targetPath + ".pending";
			journalPath = this.targetPath + ".journal";
			backupPath = this.targetPath + ".backup";
		}

		public string Prepare(WorldPersistenceSnapshotData snapshot)
		{
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
			string payload = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
			string payloadSha256 = WorldRuntimeIdentityUtility.Sha256(payload);
			WriteDurable(pendingPath, payload);
			WorldPersistenceJournalData journal = new WorldPersistenceJournalData
			{
				TransactionId = WorldRuntimeIdentityUtility.Create("txn", snapshot.WorldId + "|" + snapshot.Sequence + "|" + payloadSha256),
				PayloadSha256 = payloadSha256,
				Sequence = snapshot.Sequence,
				Stage = "Prepared",
			};
			WriteDurable(journalPath, JsonConvert.SerializeObject(journal, Formatting.Indented));
			return journal.TransactionId;
		}

		public void CommitPrepared()
		{
			WorldPersistenceJournalData journal = ReadJournal();
			if (!File.Exists(pendingPath))
				throw new InvalidDataException("Prepared persistence payload is missing.");
			string payload = File.ReadAllText(pendingPath);
			if (!string.Equals(WorldRuntimeIdentityUtility.Sha256(payload), journal.PayloadSha256, StringComparison.Ordinal))
				throw new InvalidDataException("Prepared persistence payload hash mismatch.");
			if (File.Exists(targetPath))
			{
				if (File.Exists(backupPath))
					File.Delete(backupPath);
				File.Replace(pendingPath, targetPath, backupPath, true);
			}
			else
			{
				File.Move(pendingPath, targetPath);
			}
			if (File.Exists(backupPath))
				File.Delete(backupPath);
			File.Delete(journalPath);
		}

		public void Save(WorldPersistenceSnapshotData snapshot)
		{
			Prepare(snapshot);
			CommitPrepared();
		}

		public bool Recover()
		{
			RecoveryApplied = false;
			if (!File.Exists(journalPath))
				return false;
			WorldPersistenceJournalData journal = ReadJournal();
			if (File.Exists(pendingPath))
			{
				string payload = File.ReadAllText(pendingPath);
				if (!string.Equals(WorldRuntimeIdentityUtility.Sha256(payload), journal.PayloadSha256, StringComparison.Ordinal))
					throw new InvalidDataException("Recovery payload hash mismatch.");
				CommitPrepared();
				RecoveryApplied = true;
				return true;
			}
			if (File.Exists(targetPath) && string.Equals(WorldRuntimeIdentityUtility.Sha256(File.ReadAllText(targetPath)),
				journal.PayloadSha256, StringComparison.Ordinal))
			{
				File.Delete(journalPath);
				RecoveryApplied = true;
				return true;
			}
			throw new InvalidDataException("Persistence journal cannot be recovered because neither pending nor committed payload matches.");
		}

		public WorldPersistenceSnapshotData Load()
		{
			if (!File.Exists(targetPath))
				throw new FileNotFoundException("Persistence snapshot is missing.", targetPath);
			WorldPersistenceSnapshotData snapshot = JsonConvert.DeserializeObject<WorldPersistenceSnapshotData>(File.ReadAllText(targetPath));
			if (snapshot == null)
				throw new InvalidDataException("Persistence snapshot deserialized to null.");
			return snapshot;
		}

		private WorldPersistenceJournalData ReadJournal()
		{
			if (!File.Exists(journalPath))
				throw new FileNotFoundException("Persistence journal is missing.", journalPath);
			WorldPersistenceJournalData journal = JsonConvert.DeserializeObject<WorldPersistenceJournalData>(File.ReadAllText(journalPath));
			if (journal == null || journal.SchemaVersion != 1 || !string.Equals(journal.Stage, "Prepared", StringComparison.Ordinal))
				throw new InvalidDataException("Persistence journal is invalid.");
			return journal;
		}

		private static void WriteDurable(string path, string content)
		{
			using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
			using (StreamWriter writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
			{
				writer.Write(content);
				writer.Flush();
				stream.Flush(true);
			}
		}
	}
}
