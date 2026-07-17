////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public sealed class WorldPopulationPolicyData
	{
		public int ItemEntitiesPerCell = 2;
		public int ZombieEntitiesPerCell = 2;
		public int AnimalEntitiesPerCell = 1;
		public int VehicleEntitiesPerCell = 1;
		public int MaximumPersistentPopulation = 10000;
	}

	[Serializable]
	public sealed class WorldSurvivalPolicyData
	{
		public float FoodDrainMultiplier = 1f;
		public float WaterDrainMultiplier = 1f;
		public float VirusDrainMultiplier = 1f;
	}

	[Serializable]
	public sealed class WorldEconomyPolicyData
	{
		public int ItemRespawnTicks = 600;
		public int VehicleRespawnTicks = 3600;
		public float LootAbundanceMultiplier = 1f;
	}

	[Serializable]
	public sealed class WorldSimulationPolicyData
	{
		public int SchemaVersion = 1;
		public string PolicyId = "u3-connected-world-default";
		public WorldPopulationPolicyData Population = new WorldPopulationPolicyData();
		public WorldSurvivalPolicyData Survival = new WorldSurvivalPolicyData();
		public WorldEconomyPolicyData Economy = new WorldEconomyPolicyData();

		public void Validate()
		{
			if (SchemaVersion != 1)
				throw new InvalidOperationException("Unsupported world simulation policy schema version: " + SchemaVersion);
			if (string.IsNullOrWhiteSpace(PolicyId))
				throw new InvalidOperationException("World simulation policy ID is required.");
			if (Population == null || Survival == null || Economy == null)
				throw new InvalidOperationException("World simulation policy sections are required.");
			if (Population.ItemEntitiesPerCell < 0 || Population.ZombieEntitiesPerCell < 0 ||
				Population.AnimalEntitiesPerCell < 0 || Population.VehicleEntitiesPerCell < 0 ||
				Population.MaximumPersistentPopulation < 1)
				throw new InvalidOperationException("Population limits are invalid.");
			if (Survival.FoodDrainMultiplier <= 0f || Survival.WaterDrainMultiplier <= 0f || Survival.VirusDrainMultiplier <= 0f)
				throw new InvalidOperationException("Survival multipliers must be positive.");
			if (Economy.ItemRespawnTicks < 1 || Economy.VehicleRespawnTicks < 1 || Economy.LootAbundanceMultiplier <= 0f)
				throw new InvalidOperationException("Economy policy values are invalid.");
		}
	}
}
