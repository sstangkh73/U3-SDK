////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SDG.Unturned.WorldUpgrade
{
	public sealed class WorldCellStreamer : IDisposable
	{
		private sealed class ActiveCell
		{
			public WorldCellBundleRepository Repository;
			public WorldCellIndexData Index;
		}

		private readonly List<WorldCellBundleRepository> repositories;
		private readonly Dictionary<string, ActiveCell> activeCells = new Dictionary<string, ActiveCell>(StringComparer.Ordinal);

		public float PreloadRadius { get; }
		public float UnloadRadius { get; }
		public int ActiveCellCount => activeCells.Count;
		public int ActiveEntityCount => repositories.Sum(repository => repository.LoadedEntityCount);
		public long EstimatedResidentBytes => repositories.Sum(repository => repository.EstimatedResidentBytes);
		public int PeakActiveCellCount { get; private set; }
		public int PeakActiveEntityCount { get; private set; }
		public long PeakEstimatedResidentBytes { get; private set; }
		public int ActivationCount { get; private set; }
		public int DeactivationCount { get; private set; }

		public WorldCellStreamer(IEnumerable<WorldCellBundleRepository> repositories, float preloadRadius, float unloadRadius)
		{
			this.repositories = repositories?.ToList() ?? throw new ArgumentNullException(nameof(repositories));
			if (this.repositories.Count == 0)
				throw new ArgumentException("At least one zone repository is required.", nameof(repositories));
			if (preloadRadius < 0f)
				throw new ArgumentOutOfRangeException(nameof(preloadRadius));
			if (unloadRadius < preloadRadius)
				throw new ArgumentOutOfRangeException(nameof(unloadRadius), "Unload radius must be greater than or equal to preload radius.");
			PreloadRadius = preloadRadius;
			UnloadRadius = unloadRadius;
		}

		public void Update(Vector3 worldPosition)
		{
			float preloadSquared = PreloadRadius * PreloadRadius;
			foreach (WorldCellBundleRepository repository in repositories)
			{
				foreach (WorldCellIndexData index in repository.EnumerateCellIndices())
				{
					if (WorldZoneResolver.DistanceSquaredXZ(index.WorldBounds, worldPosition) > preloadSquared || activeCells.ContainsKey(index.CellId))
						continue;
					repository.LoadCell(index.CellId);
					activeCells.Add(index.CellId, new ActiveCell { Repository = repository, Index = index });
					ActivationCount++;
				}
			}

			float unloadSquared = UnloadRadius * UnloadRadius;
			foreach (KeyValuePair<string, ActiveCell> pair in activeCells.ToArray())
			{
				if (WorldZoneResolver.DistanceSquaredXZ(pair.Value.Index.WorldBounds, worldPosition) <= unloadSquared)
					continue;
				pair.Value.Repository.UnloadCell(pair.Key);
				activeCells.Remove(pair.Key);
				DeactivationCount++;
			}

			PeakActiveCellCount = Math.Max(PeakActiveCellCount, ActiveCellCount);
			PeakActiveEntityCount = Math.Max(PeakActiveEntityCount, ActiveEntityCount);
			PeakEstimatedResidentBytes = Math.Max(PeakEstimatedResidentBytes, EstimatedResidentBytes);
		}

		public bool HasLoadedLandscapeCoverage(Vector3 worldPosition)
		{
			return activeCells.Values.Any(active => active.Index.HasLandscapeRecord &&
				WorldZoneResolver.ContainsXZ(active.Index.WorldBounds, worldPosition));
		}

		public bool HasLoadedLandscapeForZone(string zoneKey, Vector3 worldPosition, float maximumDistance)
		{
			float maximumDistanceSquared = maximumDistance * maximumDistance;
			return activeCells.Values.Any(active => string.Equals(active.Repository.Zone.ZoneKey, zoneKey, StringComparison.Ordinal) &&
				active.Index.HasLandscapeRecord && WorldZoneResolver.DistanceSquaredXZ(active.Index.WorldBounds, worldPosition) <= maximumDistanceSquared);
		}

		public int GetActiveCellCount(string zoneKey)
		{
			return activeCells.Values.Count(active => string.Equals(active.Repository.Zone.ZoneKey, zoneKey, StringComparison.Ordinal));
		}

		public int GetActiveRecordCount(string zoneKey, string kind)
		{
			return activeCells.Values.Where(active => string.Equals(active.Repository.Zone.ZoneKey, zoneKey, StringComparison.Ordinal))
				.Sum(active => active.Index.RecordCounts.FirstOrDefault(count => string.Equals(count.Kind, kind, StringComparison.Ordinal))?.Count ?? 0);
		}

		public void UnloadAll()
		{
			foreach (WorldCellBundleRepository repository in repositories)
				repository.UnloadAll();
			DeactivationCount += activeCells.Count;
			activeCells.Clear();
		}

		public void Dispose()
		{
			UnloadAll();
		}
	}
}
