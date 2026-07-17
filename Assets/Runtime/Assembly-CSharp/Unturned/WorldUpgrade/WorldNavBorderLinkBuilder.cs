////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDG.Unturned.WorldUpgrade
{
	[Serializable]
	public sealed class WorldNavBorderLinkData
	{
		public string LinkId;
		public string SourceCellId;
		public string DestinationCellId;
		public string SourceZoneKey;
		public string DestinationZoneKey;
		public bool IsCrossZone;
		public bool IsRuntimeBaked;
	}

	public static class WorldNavBorderLinkBuilder
	{
		public static List<WorldNavBorderLinkData> Build(IEnumerable<WorldZoneDefinitionData> zones,
			WorldTransitionCorridorData corridor)
		{
			List<WorldZoneDefinitionData> zoneList = zones.ToList();
			List<WorldNavBorderLinkData> links = new List<WorldNavBorderLinkData>();
			foreach (WorldZoneDefinitionData zone in zoneList)
			{
				Dictionary<string, WorldCellIndexData> cells = zone.Cells.ToDictionary(CellKey, StringComparer.Ordinal);
				foreach (WorldCellIndexData cell in zone.Cells.OrderBy(value => value.CellId, StringComparer.Ordinal))
				{
					AddNeighbor(cells, links, zone, cell, cell.GridX + 1, cell.GridZ);
					AddNeighbor(cells, links, zone, cell, cell.GridX, cell.GridZ + 1);
				}
			}
			if (corridor != null)
			{
				WorldZoneDefinitionData source = zoneList.Single(zone => zone.ZoneKey == corridor.SourceZoneKey);
				WorldZoneDefinitionData destination = zoneList.Single(zone => zone.ZoneKey == corridor.DestinationZoneKey);
				WorldCellIndexData sourceCell = source.Cells.OrderBy(cell => DistanceToPoint(cell.WorldBounds, corridor.SourceBoundaryX, corridor.CenterZ))
					.ThenBy(cell => cell.CellId, StringComparer.Ordinal).First();
				WorldCellIndexData destinationCell = destination.Cells.OrderBy(cell => DistanceToPoint(cell.WorldBounds, corridor.DestinationBoundaryX, corridor.CenterZ))
					.ThenBy(cell => cell.CellId, StringComparer.Ordinal).First();
				links.Add(CreateLink(source.ZoneKey, sourceCell.CellId, destination.ZoneKey, destinationCell.CellId, true));
			}
			return links.OrderBy(link => link.LinkId, StringComparer.Ordinal).ToList();
		}

		private static void AddNeighbor(Dictionary<string, WorldCellIndexData> cells, List<WorldNavBorderLinkData> links,
			WorldZoneDefinitionData zone, WorldCellIndexData source, int x, int z)
		{
			if (cells.TryGetValue(x + "," + z, out WorldCellIndexData destination))
				links.Add(CreateLink(zone.ZoneKey, source.CellId, zone.ZoneKey, destination.CellId, false));
		}

		private static WorldNavBorderLinkData CreateLink(string sourceZone, string sourceCell, string destinationZone,
			string destinationCell, bool crossZone)
		{
			string key = sourceZone + "|" + sourceCell + "|" + destinationZone + "|" + destinationCell;
			return new WorldNavBorderLinkData
			{
				LinkId = WorldRuntimeIdentityUtility.Create("nav", key),
				SourceZoneKey = sourceZone,
				SourceCellId = sourceCell,
				DestinationZoneKey = destinationZone,
				DestinationCellId = destinationCell,
				IsCrossZone = crossZone,
				IsRuntimeBaked = false,
			};
		}

		private static string CellKey(WorldCellIndexData cell)
		{
			return cell.GridX + "," + cell.GridZ;
		}

		private static float DistanceToPoint(RawMapBoundsData bounds, float x, float z)
		{
			float dx = x < bounds.Min.X ? bounds.Min.X - x : x > bounds.Max.X ? x - bounds.Max.X : 0f;
			float dz = z < bounds.Min.Z ? bounds.Min.Z - z : z > bounds.Max.Z ? z - bounds.Max.Z : 0f;
			return dx * dx + dz * dz;
		}
	}
}
