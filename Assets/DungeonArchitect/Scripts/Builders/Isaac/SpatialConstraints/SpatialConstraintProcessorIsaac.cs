using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using DungeonArchitect.Constraints.Grid;
using DungeonArchitect.Builders.Isaac;
using DungeonArchitect.Utils;

namespace DungeonArchitect.Constraints.Isaac
{
    public class SpatialConstraintProcessorIsaac : SpatialConstraintProcessor
    {
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
            var roomId = socket.cellId;
            var isaacModel = model as IsaacDungeonModel;
            if (isaacModel == null) return false;

            var room = IsaacBuilderUtils.GetRoom(isaacModel, roomId);
            if (room == null) return false;

            int x, z;
            GetLayoutPosition(room, isaacModel, socket.Transform, out x, out z);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    var cx = dx + 1;
                    var cz = 2 - (dz + 1);
                    var index = cz * 3 + cx;
                    var constraintType = constraint.cells[index];
                    var mapTileType = IsaacBuilderUtils.GetTileAt(x + dx, z + dz, room.layout);
                    bool empty = (mapTileType.tileType != IsaacRoomTileType.Floor);
                    if (empty && constraintType.CellType == SpatialConstraintGridCellType.Occupied)
                    {
                        // Expected an occupied cell and got an empty cell
                        return false;
                    }
                    if (!empty && constraintType.CellType == SpatialConstraintGridCellType.Empty)
                    {
                        // Expected an empty cell and got an occupied cell
                        return false;
                    }
                }
            }

            // All tests passed
            return true;
        }

        void GetLayoutPosition(IsaacRoom room, IsaacDungeonModel model, Matrix4x4 trans, out int outX, out int outZ)
        {
            var isaacConfig = model.config;
            var tileSize = new Vector3(isaacConfig.tileSize.x, 0, isaacConfig.tileSize.y);
            var roomSizeWorld = new IntVector(isaacConfig.roomWidth, 0, isaacConfig.roomHeight) * tileSize;
            var roomPadding = new Vector3(isaacConfig.roomPadding.x, 0, isaacConfig.roomPadding.y);

            var roomBasePosition = room.position * (roomSizeWorld + roomPadding);
            var markerPositionF = Matrix.GetTranslation(ref trans);

            // Translate to room relative coords
            markerPositionF -= roomBasePosition;

            // Get logical coords relative to room location
            outX = Mathf.FloorToInt(markerPositionF.x / tileSize.x);
            outZ = Mathf.FloorToInt(markerPositionF.z / tileSize.z);
        }
    }
}
