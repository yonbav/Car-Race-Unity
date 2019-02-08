//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect;
using DungeonArchitect.Utils;
using DungeonArchitect.Constraints;
using DungeonArchitect.Constraints.Grid;

namespace DungeonArchitect.Builders.Grid
{
    public class SpatialConstraintProcessorGrid3D : SpatialConstraintProcessor
    {
        /// <summary>
        /// Location to cell id mapping of group positions
        /// </summary>
        Dictionary<IntVector, int> groundPositions = new Dictionary<IntVector, int>();

        HashSet<IntVector> doorPositions = new HashSet<IntVector>();
        public bool doorsOccupySpace = true;

        public bool mergeRoomCorridor = false;

        public override void Initialize(DungeonModel model, List<PropSocket> levelSockets)
        {
            groundPositions.Clear();
            foreach (var marker in levelSockets)
            {
                if (marker.SocketType == DungeonConstants.ST_GROUND)
                {
                    groundPositions.Add(marker.gridPosition, marker.cellId);
                }

                if (marker.SocketType == DungeonConstants.ST_DOOR)
                {
                    doorPositions.Add(marker.gridPosition);
                }
            }
        }

        public override void Cleanup()
        {
            groundPositions.Clear();
        }

        public override bool ProcessSpatialConstraint(SpatialConstraint constraint, PropSocket socket, DungeonModel model, List<PropSocket> levelSockets, out Matrix4x4 outOffset)
        {
            outOffset = Matrix4x4.identity;
            if (constraint is SpatialConstraintGrid7x7)
            {
                return Process7x7(constraint as SpatialConstraintGrid7x7, socket, model, ref outOffset);
            }
            if (constraint is SpatialConstraintGrid5x5)
            {
                return Process5x5(constraint as SpatialConstraintGrid5x5, socket, model, ref outOffset);
            }
            if (constraint is SpatialConstraintGrid3x3)
            {
                return Process3x3(constraint as SpatialConstraintGrid3x3, socket, model, ref outOffset);
            }
            if (constraint is SpatialConstraintGrid2x2)
            {
                return Process2x2(constraint as SpatialConstraintGrid2x2, socket, model, ref outOffset);
            }
            if (constraint is SpatialConstraintGrid1x2)
            {
                return Process1x2(constraint as SpatialConstraintGrid1x2, socket, model, ref outOffset);
            }
            return false;
        }

        CellType GetCellTypeFromId(int cellId, GridDungeonModel model)
        {
            if (!model.CellLookup.ContainsKey(cellId))
            {
                return CellType.Unknown;
            }
            var cell = model.CellLookup[cellId];
            if (cell == null) return CellType.Unknown;
            if (cell.CellType == CellType.CorridorPadding)
            {
                return CellType.Corridor;
            }
            return cell.CellType;
        }

        bool Process1x2(SpatialConstraintGrid1x2 constraint, PropSocket socket, DungeonModel model, ref Matrix4x4 outOffset)
        {
            var gridModel = model as GridDungeonModel;
            if (gridModel == null) return false;
            var gridSize = gridModel.Config.GridCellSize;

            var markerRotation = Matrix.GetRotation(ref socket.Transform);
            var markerPosition = Matrix.GetTranslation(ref socket.Transform);
            Vector3 leftOffset, rightOffset;
            float offset = gridSize.x;
            leftOffset = markerRotation * new Vector3(-offset, 0, 0);
            rightOffset = markerRotation * new Vector3(0, 0, 0);

            var left = MathUtils.RoundToIntVector(MathUtils.Divide(markerPosition + leftOffset, gridSize));
            var right = MathUtils.RoundToIntVector(MathUtils.Divide(markerPosition + rightOffset, gridSize));

            var leftOccupied = groundPositions.ContainsKey(left);
            var rightOccupied = groundPositions.ContainsKey(right);

            var occupied = new bool[] { leftOccupied, rightOccupied };
            var cells = new SpatialConstraintGridCell[] { constraint.left, constraint.right };
            bool valid = false;
            if (CheckValidity(cells, occupied))
            {
                valid = true;
            }

            cells = new SpatialConstraintGridCell[] { constraint.right, constraint.left };
            if (CheckValidity(cells, occupied))
            {
                valid = true;
            }

            if (!valid)
            {
                return false;
            }

            var rotation = Quaternion.identity; // Quaternion.Euler(0, offsetRotY, 0);
            outOffset = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
            return true;
        }

        bool CheckValidity(SpatialConstraintGridCell[] constraintCells, bool[] occupied)
        {
            for (int i = 0; i < constraintCells.Length; i++)
            {
                var cell = constraintCells[i];
                if (cell.CellType == SpatialConstraintGridCellType.DontCare)
                {
                    continue;
                }
                if (occupied[i] && cell.CellType == SpatialConstraintGridCellType.Empty)
                {
                    return false;
                }
                if (!occupied[i] && cell.CellType == SpatialConstraintGridCellType.Occupied)
                {
                    return false;
                }
            }

            return true;
        }

        bool Process2x2(SpatialConstraintGrid2x2 constraint, PropSocket socket, DungeonModel model, ref Matrix4x4 outOffset)
        {
            var gridModel = model as GridDungeonModel;
            if (gridModel == null) return false;

            int rotationsRequired = constraint.rotateToFit ? 4 : 1;

            SpatialConstraintGridCell[] constraintCells = constraint.cells;
            for (int rotIndex = 0; rotIndex < rotationsRequired; rotIndex++)
            {

                int baseCellId = -1;
                if (groundPositions.ContainsKey(socket.gridPosition))
                {
                    baseCellId = groundPositions[socket.gridPosition];
                }
                CellType baseCellType = GetCellTypeFromId(baseCellId, gridModel);

                bool isValid = true;
                for (int i = 0; i < constraintCells.Length; i++)
                {
                    var constraintType = constraintCells[i];
                    if (constraintType.CellType == SpatialConstraintGridCellType.DontCare)
                    {
                        continue;
                    }
                    int dx = i % 2;
                    int dz = i / 2;
                    dx += -1;
                    dz += -1;   // bring to -1..0 range

                    var adjacentPos = socket.gridPosition + new IntVector(dx, 0, dz);
                    int adjacentCellId;
                    var occupied = IsOccupied(adjacentPos, out adjacentCellId);

                    CellType adjacentCellType = GetCellTypeFromId(adjacentCellId, gridModel);

                    if (occupied && adjacentCellType != baseCellType && !mergeRoomCorridor)
                    {
                        //occupied = false;
                    }

                    if (adjacentCellType == CellType.Unknown)
                    {
                        occupied = false;
                    }

                    if (occupied && constraintType.CellType == SpatialConstraintGridCellType.Empty)
                    {
                        // Expected an empty cell and got an occupied cell
                        isValid = false;
                        break;
                    }
                    if (!occupied && constraintType.CellType == SpatialConstraintGridCellType.Occupied)
                    {
                        // Expected an occupied cell and got an empty cell
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                {
                    int rotationAngle = 90 * rotIndex;
                    Quaternion rotation = Quaternion.Euler(0, rotationAngle, 0);
                    outOffset = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
                    return true;
                }
                constraintCells = Rotate2x2(constraintCells);
            }


            // All tests failed
            return false;
        }

        bool Process3x3(SpatialConstraintGrid3x3 constraint, PropSocket socket, DungeonModel model, ref Matrix4x4 outOffset)
        {
            var gridModel = model as GridDungeonModel;
            if (gridModel == null) return false;

            int rotationsRequired = constraint.rotateToFit ? 4 : 1;

            SpatialConstraintGridCell[] constraintCells = constraint.cells;
            for (int rotIndex = 0; rotIndex < rotationsRequired; rotIndex++)
            {
                int baseCellId = -1;
                if (groundPositions.ContainsKey(socket.gridPosition))
                {
                    baseCellId = groundPositions[socket.gridPosition];
                }
                CellType baseCellType = GetCellTypeFromId(baseCellId, gridModel);

                bool isValid = true;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        var cx = dx + 1;
                        var cz = 2 - (dz + 1);
                        var index = cz * 3 + cx;
                        var constraintType = constraintCells[index];
                        var adjacentPos = socket.gridPosition + new IntVector(dx, 0, dz);
                        int adjacentCellId;
                        var occupied = IsOccupied(adjacentPos, out adjacentCellId);

                        CellType adjacentCellType = GetCellTypeFromId(adjacentCellId, gridModel);

                        if (occupied && adjacentCellType != baseCellType && !mergeRoomCorridor)
                        {
                            occupied = false;
                        }

                        if (occupied && constraintType.CellType == SpatialConstraintGridCellType.Empty)
                        {
                            // Expected an empty cell and got an occupied cell
                            isValid = false;
                            break;
                        }
                        if (!occupied && constraintType.CellType == SpatialConstraintGridCellType.Occupied)
                        {
                            // Expected an occupied cell and got an empty cell
                            isValid = false;
                            break;
                        }
                    }
                    if (!isValid)
                    {
                        break;
                    }
                }
                if (isValid)
                {
                    int rotationAngle = -90 * rotIndex;
                    Quaternion rotation = Quaternion.Euler(0, rotationAngle, 0);
                    outOffset = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
                    return true;
                }
                constraintCells = Rotate3x3(constraintCells);
            }
                

            // All tests failed
            return false;
        }

        bool Process5x5(SpatialConstraintGrid5x5 constraint, PropSocket socket, DungeonModel model, ref Matrix4x4 outOffset)
        {
            var gridModel = model as GridDungeonModel;
            if (gridModel == null) return false;

            int rotationsRequired = constraint.rotateToFit ? 4 : 1;

            SpatialConstraintGridCell[] constraintCells = constraint.cells;
            for (int rotIndex = 0; rotIndex < rotationsRequired; rotIndex++)
            {
                int baseCellId = -1;
                if (groundPositions.ContainsKey(socket.gridPosition))
                {
                    baseCellId = groundPositions[socket.gridPosition];
                }
                CellType baseCellType = GetCellTypeFromId(baseCellId, gridModel);

                bool isValid = true;
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        var cx = dx + 2;
                        var cz = 4 - (dz + 2);
                        var index = cz * 5 + cx;
                        var constraintType = constraintCells[index];
                        var adjacentPos = socket.gridPosition + new IntVector(dx, 0, dz);
                        int adjacentCellId;
                        var occupied = IsOccupied(adjacentPos, out adjacentCellId);

                        CellType adjacentCellType = GetCellTypeFromId(adjacentCellId, gridModel);

                        if (occupied && adjacentCellType != baseCellType && !mergeRoomCorridor)
                        {
                            occupied = false;
                        }

                        if (occupied && constraintType.CellType == SpatialConstraintGridCellType.Empty)
                        {
                            // Expected an empty cell and got an occupied cell
                            isValid = false;
                            break;
                        }
                        if (!occupied && constraintType.CellType == SpatialConstraintGridCellType.Occupied)
                        {
                            // Expected an occupied cell and got an empty cell
                            isValid = false;
                            break;
                        }
                    }
                    if (!isValid)
                    {
                        break;
                    }
                }
                if (isValid)
                {
                    int rotationAngle = -90 * rotIndex;
                    Quaternion rotation = Quaternion.Euler(0, rotationAngle, 0);
                    outOffset = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
                    return true;
                }
                constraintCells = Rotate5x5(constraintCells);
            }


            // All tests failed
            return false;
        }


        bool Process7x7(SpatialConstraintGrid7x7 constraint, PropSocket socket, DungeonModel model, ref Matrix4x4 outOffset)
        {
            var gridModel = model as GridDungeonModel;
            if (gridModel == null) return false;

            int rotationsRequired = constraint.rotateToFit ? 4 : 1;

            SpatialConstraintGridCell[] constraintCells = constraint.cells;
            for (int rotIndex = 0; rotIndex < rotationsRequired; rotIndex++)
            {
                int baseCellId = -1;
                if (groundPositions.ContainsKey(socket.gridPosition))
                {
                    baseCellId = groundPositions[socket.gridPosition];
                }
                CellType baseCellType = GetCellTypeFromId(baseCellId, gridModel);

                bool isValid = true;
                for (int dx = -3; dx <= 3; dx++)
                {
                    for (int dz = -3; dz <= 3; dz++)
                    {
                        var cx = dx + 3;
                        var cz = 6 - (dz + 3);
                        var index = cz * 7 + cx;
                        var constraintType = constraintCells[index];
                        var adjacentPos = socket.gridPosition + new IntVector(dx, 0, dz);
                        int adjacentCellId;
                        var occupied = IsOccupied(adjacentPos, out adjacentCellId);

                        CellType adjacentCellType = GetCellTypeFromId(adjacentCellId, gridModel);

                        if (occupied && adjacentCellType != baseCellType && !mergeRoomCorridor)
                        {
                            occupied = false;
                        }

                        if (occupied && constraintType.CellType == SpatialConstraintGridCellType.Empty)
                        {
                            // Expected an empty cell and got an occupied cell
                            isValid = false;
                            break;
                        }
                        if (!occupied && constraintType.CellType == SpatialConstraintGridCellType.Occupied)
                        {
                            // Expected an occupied cell and got an empty cell
                            isValid = false;
                            break;
                        }
                    }
                    if (!isValid)
                    {
                        break;
                    }
                }
                if (isValid)
                {
                    int rotationAngle = -90 * rotIndex;
                    Quaternion rotation = Quaternion.Euler(0, rotationAngle, 0);
                    outOffset = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
                    return true;
                }
                constraintCells = Rotate7x7(constraintCells);
            }


            // All tests failed
            return false;
        }

        SpatialConstraintGridCell[] Rotate7x7(SpatialConstraintGridCell[] cells)
        {
            var SrcIndex = new int[] {
                0,  1,  2,  3,  4,  5,   6,
                7,  8,  9,  10, 11, 12, 13,
                14, 15, 16, 17, 18, 19, 20,
                21, 22, 23, 24, 25, 26, 27,
                28, 29, 30, 31, 32, 33, 34,
                35, 36, 37, 38, 39, 40, 41,
                42, 43, 44, 45, 46, 47, 48
            };

            var DstIndex = new int[] {
               42, 35, 28, 21, 14,  7,   0,
               43, 36, 29, 22, 15,  8,   1,
               44, 37, 30, 23, 16,  9,   2,
               45, 38, 31, 24, 17, 10,   3,
               46, 39, 32, 25, 18, 11,   4,
               47, 40, 33, 26, 19, 12,   5,
               48, 41, 34, 27, 20, 13,   6,
            };

            var result = new SpatialConstraintGridCell[cells.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[DstIndex[i]] = cells[SrcIndex[i]];
            }
            return result;
        }

        SpatialConstraintGridCell[] Rotate5x5(SpatialConstraintGridCell[] cells)
        {
            var SrcIndex = new int[] {
                 0,  1,  2,  3,  4,
                 5,  6,  7,  8,  9,
                10, 11, 12, 13, 14,
                15, 16, 17, 18, 19,
                20, 21, 22, 23, 24
            };
            var DstIndex = new int[] {
                20,  15,  10,  5,  0,
                21,  16,  11,  6,  1,
                22,  17,  12,  7,  2,
                23,  18,  13,  8,  3,
                24,  19,  14,  9,  4,
            };

            var result = new SpatialConstraintGridCell[cells.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[DstIndex[i]] = cells[SrcIndex[i]];
            }
            return result;
        }

        SpatialConstraintGridCell[] Rotate3x3(SpatialConstraintGridCell[] cells)
        {
            var SrcIndex = new int[] {
		        0, 1, 2,
		        3, 4, 5,
		        6, 7, 8
	        };
	        var DstIndex = new int[] {
		        6, 3, 0,
		        7, 4, 1,
		        8, 5, 2
	        };

            var result = new SpatialConstraintGridCell[9];
            for (int i = 0; i < result.Length; i++)
            {
                result[DstIndex[i]] = cells[SrcIndex[i]];
	        }
	        return result;
        }

        SpatialConstraintGridCell[] Rotate2x2(SpatialConstraintGridCell[] cells)
        {
            var SrcIndex = new int[] {
		        0, 1,
		        2, 3
	        };
	        var DstIndex = new int[] {
		        2, 0,
		        3, 1
	        };

            var result = new SpatialConstraintGridCell[4];
            for (int i = 0; i < 4; i++)
            {
		        result[DstIndex[i]] = cells[SrcIndex[i]];
	        }
	        return result;
        }

        bool IsOccupied(IntVector position, out int cellId)
        {
            
            var occupied = groundPositions.ContainsKey(position);
            if (!doorsOccupySpace && doorPositions.Contains(position))
            {
                occupied = false;
            }
            cellId = occupied ? groundPositions[position] : -1;
            return occupied;
        }
    }
}
