//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Constraints;
using DungeonArchitect.Constraints.Grid;

namespace DungeonArchitect.Editors
{
    [CustomEditor(typeof(SpatialConstraintGrid5x5))]
    [ConstraintEditor(typeof(SpatialConstraintGrid5x5))]
    public class SpatialConstraintGrid5x5Editor : SpatialConstraintGridEditor
    {
        public override void DrawConstraintEditor(SpatialConstraint constraint)
        {
            var constraint5x5 = constraint as SpatialConstraintGrid5x5;
            if (constraint5x5.cells == null || constraint5x5.cells[0] == null)
            {
                // gets invalidated during code change / hot-reloading
                throw new System.ApplicationException("invalid state");
            }

            var cells = constraint5x5.cells;
            for (int row = 0; row < 5; row++)
            {
                EditorGUILayout.BeginHorizontal();
                DrawGridCell(cells[row * 5 + 0]);
                DrawGridCell(cells[row * 5 + 1]);
                DrawGridCell(cells[row * 5 + 2]);
                DrawGridCell(cells[row * 5 + 3]);
                DrawGridCell(cells[row * 5 + 4]);
                EditorGUILayout.EndHorizontal();
            }
        }

    }
}
