using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect.Utils;

namespace DungeonArchitect.Builders.Grid
{
    public class GridBuilderUtils
    {
        public static bool IsCorridor(CellType type)
        {
            return type == CellType.Corridor || type == CellType.CorridorPadding;
        }

        public static bool IsRoomCorridor(CellType typeA, CellType typeB)
        {
            return (typeA == CellType.Room && IsCorridor(typeB))
                || (typeB == CellType.Room && IsCorridor(typeA));
        }


        public static bool AreAdjacentCellsReachable(GridDungeonModel gridModel, int cellIdA, int cellIdB)
        {
            var cellA = gridModel.GetCell(cellIdA);
            var cellB = gridModel.GetCell(cellIdB);

            if (cellA == null || cellB == null)
            {
                return false;
            }


            // If any one is a room, make sure we have a door between them
            if (cellA.CellType == CellType.Room || cellB.CellType == CellType.Room)
            {
                if (!gridModel.DoorManager.ContainsDoorBetweenCells(cellIdA, cellIdB))
                {
                    // We don't have a door between them and is blocked by a room wall
                    return false;
                }
            }

            // if their height is different, make sure we have a stair between them
            if (cellA.Bounds.Location.y != cellB.Bounds.Location.y)
            {
                if (!gridModel.ContainsStair(cellIdA, cellIdB))
                {
                    // Height difference with no stairs. not reachable
                    return false;
                }
            }

            // reachable
            return true;
        }

        public static void GetAdjacentCorridors(GridDungeonModel gridModel, int startCellId, ref List<int> OutConnectedCorridors, ref List<int> OutConnectedRooms)
        {
            OutConnectedCorridors.Clear();
            OutConnectedRooms.Clear();

            // search all nearby cells till we reach a dead end (or a room)
            var visited = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(startCellId);

            while (stack.Count > 0)
            {
                var topId = stack.Pop();
                if (visited.Contains(topId)) continue;
                visited.Add(topId);

                var top = gridModel.GetCell(topId);
                if (top == null) continue;
                if (top.CellType == CellType.Unknown) continue;

                if (top.CellType == CellType.Room && top.Id != startCellId)
                {
                    OutConnectedRooms.Add(topId);
                    continue;
                }

                if (IsCorridor(top.CellType))
                {
                    OutConnectedCorridors.Add(topId);
                }

                // search adjacent cells
                foreach (var adjacentId in top.AdjacentCells)
                {
                    // make sure the adjacent cell is reachable
                    if (AreAdjacentCellsReachable(gridModel, topId, adjacentId))
                    {
                        stack.Push(adjacentId);
                    }
                }
            }
        }
    }

}
