using UnityEngine;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Utils;

namespace DungeonArchitect.Builders.Grid
{
    /// <summary>
    /// Helper functions to draw debug information of the dungeon layout in the scene view 
    /// </summary>
    public class GridDebugDrawUtils
    {

        public static void DrawCell(Cell cell, Color color, Vector3 gridScale, bool mode2D)
        {
            DrawBounds(cell.Bounds, color, gridScale, mode2D);
        }
        public static void DrawBounds(Rectangle bounds, Color color, Vector3 gridScale, bool mode2D)
        {
            var x0 = bounds.Left * gridScale.x;
            var x1 = bounds.Right * gridScale.x;
            var z0 = bounds.Front * gridScale.z;
            var z1 = bounds.Back * gridScale.z;
            var y = bounds.Location.y * gridScale.y;
            DrawLine(new Vector3(x0, y, z0), new Vector3(x1, y, z0), color, 0, false, mode2D);
            DrawLine(new Vector3(x1, y, z0), new Vector3(x1, y, z1), color, 0, false, mode2D);
            DrawLine(new Vector3(x1, y, z1), new Vector3(x0, y, z1), color, 0, false, mode2D);
            DrawLine(new Vector3(x0, y, z1), new Vector3(x0, y, z0), color, 0, false, mode2D);
        }

        public static void DrawCellId(Cell cell, Vector3 gridScale, bool mode2D)
        {
            var center = Vector3.Scale(cell.Bounds.CenterF(), gridScale); // + new Vector3(0, .2f, 0);
            var screenCoord = Camera.main.WorldToScreenPoint(center);
            if (screenCoord.z > 0)
            {
                GUI.Label(new Rect(screenCoord.x, Screen.height - screenCoord.y, 100, 50), "" + cell.Id);
            }
        }

        public static void DrawMarker(PropSocket marker, Color color, bool mode2D)
        {
            var start = Matrix.GetTranslation(ref marker.Transform);
            var end = start + new Vector3(0, 0.2f, 0);
            DrawLine(start, end, color, 0, false, mode2D);
        }

        public static void DrawAdjacentCells(Cell cell, GridDungeonModel model, Color color, bool mode2D)
        {
            if (model == null) return;
            var gridConfig = model.Config as GridDungeonConfig;
            if (gridConfig == null) return;

            foreach (var adjacentId in cell.AdjacentCells)
            {
                var adjacentCell = model.GetCell(adjacentId);
                if (adjacentCell == null) return;
                var centerA = Vector3.Scale(cell.Bounds.CenterF(), gridConfig.GridCellSize);
                var centerB = Vector3.Scale(adjacentCell.Bounds.CenterF(), gridConfig.GridCellSize);
                DrawLine(centerA, centerB, color, 0, false, mode2D);
            }

            foreach (var adjacentId in cell.FixedRoomConnections)
            {
                var adjacentCell = model.GetCell(adjacentId);
                if (adjacentCell == null) return;
                var centerA = Vector3.Scale(cell.Bounds.CenterF(), gridConfig.GridCellSize) + new Vector3(0, 0.2f, 0);
                var centerB = Vector3.Scale(adjacentCell.Bounds.CenterF(), gridConfig.GridCellSize) + new Vector3(0, 0.2f, 0);
                DrawLine(centerA, centerB, Color.red, 0, false, mode2D);
            }

        }

        static Vector3 FlipFor2D(Vector3 v)
        {
            return new Vector3(v.x, v.z, v.y);
        }

        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest, bool mode2D)
        {
            if (mode2D)
            {
                start = FlipFor2D(start);
                end = FlipFor2D(end);
            }

            Debug.DrawLine(start, end, color, duration, depthTest);
        }

    }
}
