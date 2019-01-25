//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect;
using DungeonArchitect.Utils;
using DungeonArchitect.Constraints;
using DungeonArchitect.Constraints.Grid;

namespace DungeonArchitect.Builders.Grid
{
    public class SpatialConstraintProcessorGrid2D : SpatialConstraintProcessor
    {
        HashSet<IntVector> groundPositions = new HashSet<IntVector>();
        HashSet<IntVector> doorPositions = new HashSet<IntVector>();
        public bool doorsOccupySpace = true;

        public override void Initialize(DungeonModel model, List<PropSocket> levelSockets)
        {
            groundPositions.Clear();
            foreach (var marker in levelSockets)
            {
                if (marker.SocketType == DungeonConstants.ST_GROUND2D)
                {
                    groundPositions.Add(marker.gridPosition);
                }

                if (marker.SocketType == DungeonConstants.ST_DOOR2D || marker.SocketType == DungeonConstants.ST_DOOR2D_90)
                {
                    doorPositions.Add(marker.gridPosition);
                }
            }
        }

        public override void Cleanup() {
            groundPositions.Clear();
        }

        public override bool ProcessSpatialConstraint(SpatialConstraint constraint, PropSocket socket, DungeonModel model, List<PropSocket> levelSockets, out Matrix4x4 outOffset)
        {
            outOffset = Matrix4x4.identity;
            if (constraint is SpatialConstraintGrid3x3)
            {
                return Process3x3(constraint as SpatialConstraintGrid3x3, socket, model);
            }
            return false;
        }

        bool Process3x3(SpatialConstraintGrid3x3 constraint, PropSocket socket, DungeonModel model)
        {
            var isaacModel = model as GridDungeonModel;
            if (isaacModel == null) return false;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    var cx = dx + 1;
                    var cz = 2 - (dz + 1);
                    var index = cz * 3 + cx;
                    var constraintType = constraint.cells[index];
                    var adjacentPos = socket.gridPosition + new IntVector(dx, 0, dz);
                    var occupied = IsOccupied(adjacentPos);
                    if (occupied && constraintType.CellType == SpatialConstraintGridCellType.Empty)
                    {
                        // Expected an empty cell and got an occupied cell
                        return false;
                    }
                    if (!occupied && constraintType.CellType == SpatialConstraintGridCellType.Occupied)
                    {
                        // Expected an occupied cell and got an empty cell
                        return false;
                    }
                }
            }

            // All tests passed
            return true;
        }

        bool IsOccupied(IntVector position)
        {
            var occupied = groundPositions.Contains(position);
            if (!doorsOccupySpace && doorPositions.Contains(position))
            {
                occupied = false;
            }
            return occupied;
        }
    }
}