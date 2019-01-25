//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Constraints;
using DungeonArchitect.Constraints.Grid;

namespace DungeonArchitect.Editors
{
    [CustomEditor(typeof(SpatialConstraintGrid3x3))]
    [ConstraintEditor(typeof(SpatialConstraintGrid3x3))]
    public class SpatialConstraintGrid3x3Editor : SpatialConstraintGridEditor
    {
        public override void DrawConstraintEditor(SpatialConstraint constraint)
        {
            var constraint3x3 = constraint as SpatialConstraintGrid3x3;
            if (constraint3x3.cells == null || constraint3x3.cells[0] == null)
            {
                // gets invalidated during code change / hot-reloading
                throw new System.ApplicationException("invalid state");
            }

            var cells = constraint3x3.cells;
            for (int row = 0; row < 3; row++)
            {
                EditorGUILayout.BeginHorizontal();
                DrawGridCell(cells[row * 3 + 0]);
                DrawGridCell(cells[row * 3 + 1]);
                DrawGridCell(cells[row * 3 + 2]);
                EditorGUILayout.EndHorizontal();
            }
        }

    }
}
