////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade
{
	public enum EWorldClientCommand
	{
		PickupEntity,
		DropInventoryItem,
		ClientTravelCommit,
		ClientSaveImport,
	}

	[Serializable]
	public sealed class WorldClientCommandData
	{
		public string RequestId;
		public string ClientId;
		public EWorldClientCommand Command;
		public string EntityId;
		public string InventoryItemId;
		public int ExpectedEntityRevision;
	}

	[Serializable]
	public sealed class WorldAuthorityCommandResultData
	{
		public string RequestId;
		public bool Accepted;
		public bool WasReplay;
		public string Code;
		public string EntityId;
		public int EntityRevision;
		public int SessionRevision;
	}

	[Serializable]
	public sealed class WorldPlayerSessionData
	{
		public string ClientId;
		public string ReconnectToken;
		public bool IsConnected;
		public string ZoneId;
		public string OwnerCellId;
		public RawMapVector3Data WorldPosition = new RawMapVector3Data();
		public int Revision;
		public List<string> InventoryItemIds = new List<string>();
	}

	[Serializable]
	public sealed class WorldConnectResultData
	{
		public bool Accepted;
		public bool WasReconnect;
		public string Code;
		public WorldPlayerSessionData Session;
	}

	[Serializable]
	public sealed class WorldReplicationSnapshotData
	{
		public int ProtocolVersion = 1;
		public string ClientId;
		public int SessionRevision;
		public string ZoneId;
		public string OwnerCellId;
		public List<string> ScopedCellIds = new List<string>();
		public List<WorldDynamicEntityStateData> Entities = new List<WorldDynamicEntityStateData>();
		public List<string> InventoryItemIds = new List<string>();
		public string ContentSha256;
	}

	[Serializable]
	public sealed class WorldCompatibilityPolicyData
	{
		public int ServerProtocolVersion = 1;
		public int MinimumClientProtocolVersion = 1;
		public int MaximumClientProtocolVersion = 1;
		public int SupportedWorldSchemaVersion = 1;
		public int SupportedPersistenceSchemaVersion = 1;

		public void Validate(int clientProtocolVersion, WorldManifestData manifest, WorldPersistenceSnapshotData snapshot)
		{
			if (clientProtocolVersion < MinimumClientProtocolVersion || clientProtocolVersion > MaximumClientProtocolVersion)
				throw new InvalidOperationException("Client protocol version is not supported.");
			if (manifest == null || manifest.SchemaVersion != SupportedWorldSchemaVersion)
				throw new InvalidOperationException("World schema version is not supported.");
			if (snapshot == null || snapshot.SchemaVersion != SupportedPersistenceSchemaVersion)
				throw new InvalidOperationException("Persistence schema version is not supported.");
		}
	}

	public sealed class WorldReplicationScopeResolver
	{
		private readonly List<WorldCellIndexData> cells;
		private readonly float radiusSquared;

		public WorldReplicationScopeResolver(IEnumerable<WorldZoneDefinitionData> zones, float radius)
		{
			if (zones == null)
				throw new ArgumentNullException(nameof(zones));
			if (radius < 0f || float.IsNaN(radius) || float.IsInfinity(radius))
				throw new ArgumentOutOfRangeException(nameof(radius));
			cells = zones.SelectMany(zone => zone.Cells).OrderBy(cell => cell.CellId, StringComparer.Ordinal).ToList();
			radiusSquared = radius * radius;
		}

		public List<string> Resolve(Vector3 position)
		{
			return cells.Where(cell => WorldZoneResolver.DistanceSquaredXZ(cell.WorldBounds, position) <= radiusSquared)
				.Select(cell => cell.CellId).ToList();
		}
	}

	/// <summary>
	/// Deterministic server-side authority model. Client commands can mutate only server-owned
	/// world/inventory state through revision-checked, replay-safe transactions.
	/// </summary>
	public sealed class WorldServerAuthority
	{
		private readonly string serverSecret;
		private readonly WorldPersistenceStore store;
		private readonly WorldEntityOwnershipResolver ownership;
		private readonly WorldReplicationScopeResolver scopeResolver;
		private readonly Dictionary<string, WorldPlayerSessionData> sessions = new Dictionary<string, WorldPlayerSessionData>(StringComparer.Ordinal);
		private readonly Dictionary<string, WorldAuthorityCommandResultData> processedRequests = new Dictionary<string, WorldAuthorityCommandResultData>(StringComparer.Ordinal);

		public int ProcessedRequestCount => processedRequests.Count;
		public IEnumerable<WorldPlayerSessionData> Sessions => sessions.Values.Select(CloneSession);

		public WorldServerAuthority(string serverSecret, WorldPersistenceStore store, WorldManifestData manifest,
			IEnumerable<WorldZoneDefinitionData> zones, float replicationRadius)
		{
			if (string.IsNullOrWhiteSpace(serverSecret))
				throw new ArgumentException("Server secret is required.", nameof(serverSecret));
			this.serverSecret = serverSecret;
			this.store = store ?? throw new ArgumentNullException(nameof(store));
			List<WorldZoneDefinitionData> zoneList = zones?.ToList() ?? throw new ArgumentNullException(nameof(zones));
			ownership = new WorldEntityOwnershipResolver(manifest, zoneList);
			scopeResolver = new WorldReplicationScopeResolver(zoneList, replicationRadius);
		}

		public WorldConnectResultData Connect(string clientId, string reconnectToken, Vector3 serverApprovedSpawn)
		{
			if (string.IsNullOrWhiteSpace(clientId))
				return new WorldConnectResultData { Code = "ClientIdRequired" };
			if (sessions.TryGetValue(clientId, out WorldPlayerSessionData existing))
			{
				if (existing.IsConnected)
					return new WorldConnectResultData { Code = "SessionAlreadyConnected" };
				if (!ConstantTimeEquals(existing.ReconnectToken, reconnectToken))
					return new WorldConnectResultData { Code = "ReconnectTokenRejected" };
				existing.IsConnected = true;
				existing.Revision++;
				return new WorldConnectResultData { Accepted = true, WasReconnect = true, Code = "Reconnected", Session = CloneSession(existing) };
			}

			WorldDynamicEntityStateData ownershipProbe = new WorldDynamicEntityStateData { EntityId = "session-probe", Kind = "Player" };
			ownership.Move(ownershipProbe, serverApprovedSpawn);
			WorldPlayerSessionData session = new WorldPlayerSessionData
			{
				ClientId = clientId,
				ReconnectToken = WorldRuntimeIdentityUtility.Sha256(serverSecret + "|session|" + clientId),
				IsConnected = true,
				ZoneId = ownershipProbe.ZoneId,
				OwnerCellId = ownershipProbe.OwnerCellId,
				WorldPosition = new RawMapVector3Data(serverApprovedSpawn.x, serverApprovedSpawn.y, serverApprovedSpawn.z),
				Revision = 1,
			};
			sessions.Add(clientId, session);
			return new WorldConnectResultData { Accepted = true, Code = "Connected", Session = CloneSession(session) };
		}

		public bool Disconnect(string clientId)
		{
			if (!sessions.TryGetValue(clientId, out WorldPlayerSessionData session) || !session.IsConnected)
				return false;
			session.IsConnected = false;
			session.Revision++;
			return true;
		}

		public bool ServerUpdatePlayerPosition(string clientId, Vector3 serverVerifiedPosition)
		{
			if (!sessions.TryGetValue(clientId, out WorldPlayerSessionData session) || !session.IsConnected)
				return false;
			WorldDynamicEntityStateData ownershipProbe = new WorldDynamicEntityStateData { EntityId = "session-probe", Kind = "Player" };
			ownership.Move(ownershipProbe, serverVerifiedPosition);
			session.ZoneId = ownershipProbe.ZoneId;
			session.OwnerCellId = ownershipProbe.OwnerCellId;
			session.WorldPosition = new RawMapVector3Data(serverVerifiedPosition.x, serverVerifiedPosition.y, serverVerifiedPosition.z);
			session.Revision++;
			return true;
		}

		public bool SeedServerEntity(WorldDynamicEntityStateData entity)
		{
			return store.Add(entity);
		}

		public WorldAuthorityCommandResultData ProcessClientCommand(WorldClientCommandData command)
		{
			if (command == null || string.IsNullOrWhiteSpace(command.RequestId))
				return new WorldAuthorityCommandResultData { Code = "RequestIdRequired" };
			string requestKey = RequestKey(command.ClientId, command.RequestId);
			if (processedRequests.TryGetValue(requestKey, out WorldAuthorityCommandResultData previous))
			{
				WorldAuthorityCommandResultData replay = CloneResult(previous);
				replay.WasReplay = true;
				return replay;
			}
			if (string.IsNullOrWhiteSpace(command.ClientId) || !sessions.TryGetValue(command.ClientId, out WorldPlayerSessionData session) || !session.IsConnected)
				return Remember(requestKey, command.RequestId, false, "ConnectedSessionRequired", null, 0, 0);

			switch (command.Command)
			{
				case EWorldClientCommand.PickupEntity:
					return Pickup(command, session);
				case EWorldClientCommand.DropInventoryItem:
					return Drop(command, session);
				case EWorldClientCommand.ClientTravelCommit:
					return Remember(requestKey, command.RequestId, false, "ClientTravelAuthorityRejected", null, 0, session.Revision);
				case EWorldClientCommand.ClientSaveImport:
					return Remember(requestKey, command.RequestId, false, "ClientSaveAuthorityRejected", null, 0, session.Revision);
				default:
					return Remember(requestKey, command.RequestId, false, "UnsupportedCommand", null, 0, session.Revision);
			}
		}

		public WorldReplicationSnapshotData GetReplicationSnapshot(string clientId)
		{
			if (!sessions.TryGetValue(clientId, out WorldPlayerSessionData session) || !session.IsConnected)
				throw new InvalidOperationException("Connected server session is required.");
			Vector3 position = new Vector3(session.WorldPosition.X, session.WorldPosition.Y, session.WorldPosition.Z);
			List<string> scope = scopeResolver.Resolve(position);
			HashSet<string> scopeSet = new HashSet<string>(scope, StringComparer.Ordinal);
			WorldReplicationSnapshotData snapshot = new WorldReplicationSnapshotData
			{
				ClientId = clientId,
				SessionRevision = session.Revision,
				ZoneId = session.ZoneId,
				OwnerCellId = session.OwnerCellId,
				ScopedCellIds = scope,
				Entities = store.Entities.Where(entity => !entity.IsRemoved && scopeSet.Contains(entity.OwnerCellId))
					.OrderBy(entity => entity.EntityId, StringComparer.Ordinal).Select(CloneEntity).ToList(),
				InventoryItemIds = session.InventoryItemIds.OrderBy(id => id, StringComparer.Ordinal).ToList(),
			};
			snapshot.ContentSha256 = Fingerprint(snapshot);
			return snapshot;
		}

		private WorldAuthorityCommandResultData Pickup(WorldClientCommandData command, WorldPlayerSessionData session)
		{
			string requestKey = RequestKey(command.ClientId, command.RequestId);
			if (string.IsNullOrWhiteSpace(command.EntityId) || !store.TryGet(command.EntityId, out WorldDynamicEntityStateData entity))
				return Remember(requestKey, command.RequestId, false, "EntityNotFound", command.EntityId, 0, session.Revision);
			if (entity.IsRemoved)
				return Remember(requestKey, command.RequestId, false, "EntityAlreadyRemoved", entity.EntityId, entity.Revision, session.Revision);
			if (entity.Revision != command.ExpectedEntityRevision)
				return Remember(requestKey, command.RequestId, false, "StaleEntityRevision", entity.EntityId, entity.Revision, session.Revision);
			WorldReplicationSnapshotData visible = GetReplicationSnapshot(session.ClientId);
			if (!visible.ScopedCellIds.Contains(entity.OwnerCellId))
				return Remember(requestKey, command.RequestId, false, "EntityOutsideReplicationScope", entity.EntityId, entity.Revision, session.Revision);
			entity.IsRemoved = true;
			entity.Revision++;
			store.Upsert(entity);
			if (!session.InventoryItemIds.Contains(entity.EntityId))
				session.InventoryItemIds.Add(entity.EntityId);
			session.Revision++;
			return Remember(requestKey, command.RequestId, true, "PickupCommitted", entity.EntityId, entity.Revision, session.Revision);
		}

		private WorldAuthorityCommandResultData Drop(WorldClientCommandData command, WorldPlayerSessionData session)
		{
			string requestKey = RequestKey(command.ClientId, command.RequestId);
			if (string.IsNullOrWhiteSpace(command.InventoryItemId) || !session.InventoryItemIds.Remove(command.InventoryItemId))
				return Remember(requestKey, command.RequestId, false, "InventoryItemNotOwned", null, 0, session.Revision);
			string entityId = WorldRuntimeIdentityUtility.Create("srv", serverSecret + "|drop|" + command.RequestId + "|" + session.ClientId);
			WorldDynamicEntityStateData entity = new WorldDynamicEntityStateData
			{
				EntityId = entityId,
				Kind = "DroppedItem",
				SourceEntityId = command.InventoryItemId,
			};
			ownership.Move(entity, new Vector3(session.WorldPosition.X, session.WorldPosition.Y, session.WorldPosition.Z));
			if (!store.Add(entity))
			{
				session.InventoryItemIds.Add(command.InventoryItemId);
				return Remember(requestKey, command.RequestId, false, "ServerEntityIdCollision", entity.EntityId, entity.Revision, session.Revision);
			}
			session.Revision++;
			return Remember(requestKey, command.RequestId, true, "DropCommitted", entity.EntityId, entity.Revision, session.Revision);
		}

		private WorldAuthorityCommandResultData Remember(string requestKey, string requestId, bool accepted, string code, string entityId, int entityRevision, int sessionRevision)
		{
			WorldAuthorityCommandResultData result = new WorldAuthorityCommandResultData
			{
				RequestId = requestId,
				Accepted = accepted,
				Code = code,
				EntityId = entityId,
				EntityRevision = entityRevision,
				SessionRevision = sessionRevision,
			};
			processedRequests.Add(requestKey, result);
			return CloneResult(result);
		}

		private static string RequestKey(string clientId, string requestId) => (clientId ?? string.Empty) + "\n" + requestId;

		private static string Fingerprint(WorldReplicationSnapshotData snapshot)
		{
			List<string> lines = new List<string>
			{
				snapshot.ProtocolVersion.ToString(CultureInfo.InvariantCulture), snapshot.ClientId,
				snapshot.SessionRevision.ToString(CultureInfo.InvariantCulture), snapshot.ZoneId, snapshot.OwnerCellId,
				string.Join(",", snapshot.ScopedCellIds), string.Join(",", snapshot.InventoryItemIds),
			};
			lines.AddRange(snapshot.Entities.Select(entity => string.Join("|", entity.EntityId, entity.ZoneId, entity.OwnerCellId,
				entity.Revision.ToString(CultureInfo.InvariantCulture), entity.WorldPosition.X.ToString("R", CultureInfo.InvariantCulture),
				entity.WorldPosition.Y.ToString("R", CultureInfo.InvariantCulture), entity.WorldPosition.Z.ToString("R", CultureInfo.InvariantCulture))));
			return WorldRuntimeIdentityUtility.Sha256(string.Join("\n", lines));
		}

		private static bool ConstantTimeEquals(string a, string b)
		{
			if (a == null || b == null)
				return false;
			int difference = a.Length ^ b.Length;
			int length = Math.Max(a.Length, b.Length);
			for (int index = 0; index < length; ++index)
				difference |= (index < a.Length ? a[index] : 0) ^ (index < b.Length ? b[index] : 0);
			return difference == 0;
		}

		private static WorldAuthorityCommandResultData CloneResult(WorldAuthorityCommandResultData value) => new WorldAuthorityCommandResultData
		{
			RequestId = value.RequestId, Accepted = value.Accepted, WasReplay = value.WasReplay, Code = value.Code,
			EntityId = value.EntityId, EntityRevision = value.EntityRevision, SessionRevision = value.SessionRevision,
		};

		private static WorldPlayerSessionData CloneSession(WorldPlayerSessionData value) => new WorldPlayerSessionData
		{
			ClientId = value.ClientId, ReconnectToken = value.ReconnectToken, IsConnected = value.IsConnected,
			ZoneId = value.ZoneId, OwnerCellId = value.OwnerCellId,
			WorldPosition = new RawMapVector3Data(value.WorldPosition.X, value.WorldPosition.Y, value.WorldPosition.Z),
			Revision = value.Revision, InventoryItemIds = value.InventoryItemIds.OrderBy(id => id, StringComparer.Ordinal).ToList(),
		};

		private static WorldDynamicEntityStateData CloneEntity(WorldDynamicEntityStateData value) => new WorldDynamicEntityStateData
		{
			EntityId = value.EntityId, Kind = value.Kind, SourceEntityId = value.SourceEntityId, ZoneId = value.ZoneId,
			OwnerCellId = value.OwnerCellId, WorldPosition = new RawMapVector3Data(value.WorldPosition.X, value.WorldPosition.Y, value.WorldPosition.Z),
			AssetGuid = value.AssetGuid, LegacyAssetId = value.LegacyAssetId, Revision = value.Revision, IsRemoved = value.IsRemoved,
		};
	}
}
