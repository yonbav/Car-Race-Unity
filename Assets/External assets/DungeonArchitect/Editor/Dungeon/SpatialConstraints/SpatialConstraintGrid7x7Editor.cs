//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Constraints;
using DungeonArchitect.Constraints.Grid;

namespace DungeonArchitect.Editors
{
    [CustomEditor(typeof(SpatialConstraintGrid7x7))]
    [ConstraintEditor(typeof(SpatialConstraintGrid7x7))]
    public class SpatialConstraintGrid7x7Editor : SpatialConstraintGridEditor
    {
        public override void DrawConstraintEditor(SpatialConstraint constraint)
        {
            var constraint7x7 = constraint as SpatialConstraintGrid7x7;
            if (constraint7x7.cells == null || constraint7x7.cells[0] == null)
            {
                // gets invalidated during code change / hot-reloading
                throw new System.ApplicationException("invalid state");
            }

            var cells = constraint7x7.cells;
            for (int row = 0; row < 7; row++)
            {
                EditorGUILayout.BeginHorizontal();
                DrawGridCell(cells[row * 7 + 0]);
                DrawGridCell(cells[row * 7 + 1]);
                DrawGridCell(cells[row * 7 + 2]);
                DrawGridCell(cells[row * 7 + 3]);
                DrawGridCell(cells[row * 7 + 4]);
                DrawGridCell(cells[row * 7 + 5]);
                DrawGridCell(cells[row * 7 + 6]);
                EditorGUILayout.EndHorizontal();
            }
        }

    }
}
