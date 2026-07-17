////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDG.Unturned.WorldUpgrade
{
	public sealed class WorldPopulationService
	{
		private readonly WorldSimulationPolicyData policy;
		private readonly WorldPersistenceStore store;
		private readonly Dictionary<string, HashSet<string>> activeEntityIdsByCell =
			new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

		public int ActiveEntityCount => activeEntityIdsByCell.Values.Sum(ids => ids.Count);
		public int ActiveCellCount => activeEntityIdsByCell.Count;
		public int CreatedEntityCount { get; private set; }

		public WorldPopulationService(WorldSimulationPolicyData policy, WorldPersistenceStore store)
		{
			this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
			this.store = store ?? throw new ArgumentNullException(nameof(store));
			policy.Validate();
		}

		public int ActivateCell(WorldCellData cell)
		{
			if (cell == null)
				throw new ArgumentNullException(nameof(cell));
			if (activeEntityIdsByCell.TryGetValue(cell.CellId, out HashSet<string> active))
				return active.Count;
			active = new HashSet<string>(StringComparer.Ordinal);
			foreach (WorldEntityRecordData source in SelectPopulationSeeds(cell))
			{
				string entityId = WorldRuntimeIdentityUtility.Create("dyn", "population|" + source.EntityId);
				if (!store.TryGet(entityId, out WorldDynamicEntityStateData entity))
				{
					if (store.Count >= policy.Population.MaximumPersistentPopulation)
						break;
					entity = new WorldDynamicEntityStateData
					{
						EntityId = entityId,
						Kind = PopulationKind(source.Kind),
						SourceEntityId = source.EntityId,
						ZoneId = cell.ZoneId,
						OwnerCellId = cell.CellId,
						WorldPosition = new RawMapVector3Data(source.WorldPosition.X, source.WorldPosition.Y, source.WorldPosition.Z),
						AssetGuid = source.AssetGuid,
						LegacyAssetId = source.LegacyAssetId,
					};
					store.Add(entity);
					CreatedEntityCount++;
				}
				if (!entity.IsRemoved)
					active.Add(entity.EntityId);
			}
			activeEntityIdsByCell.Add(cell.CellId, active);
			return active.Count;
		}

		public bool DeactivateCell(string cellId)
		{
			return activeEntityIdsByCell.Remove(cellId);
		}

		public void DeactivateAll()
		{
			activeEntityIdsByCell.Clear();
		}

		private IEnumerable<WorldEntityRecordData> SelectPopulationSeeds(WorldCellData cell)
		{
			return Select(cell, "ItemSpawn", policy.Population.ItemEntitiesPerCell)
				.Concat(Select(cell, "ZombieSpawn", policy.Population.ZombieEntitiesPerCell))
				.Concat(Select(cell, "AnimalSpawn", policy.Population.AnimalEntitiesPerCell))
				.Concat(Select(cell, "VehicleSpawn", policy.Population.VehicleEntitiesPerCell));
		}

		private static IEnumerable<WorldEntityRecordData> Select(WorldCellData cell, string kind, int limit)
		{
			return cell.Entities.Where(entity => string.Equals(entity.Kind, kind, StringComparison.Ordinal))
				.OrderBy(entity => entity.EntityId, StringComparer.Ordinal).Take(limit);
		}

		private static string PopulationKind(string sourceKind)
		{
			switch (sourceKind)
			{
				case "ItemSpawn": return "DroppedItem";
				case "ZombieSpawn": return "Zombie";
				case "AnimalSpawn": return "Animal";
				case "VehicleSpawn": return "Vehicle";
				default: throw new ArgumentOutOfRangeException(nameof(sourceKind));
			}
		}
	}
}
