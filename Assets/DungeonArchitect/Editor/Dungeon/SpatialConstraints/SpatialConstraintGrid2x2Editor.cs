//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Constraints;
using DungeonArchitect.Constraints.Grid;

namespace DungeonArchitect.Editors
{
    [CustomEditor(typeof(SpatialConstraintGrid2x2))]
    [ConstraintEditor(typeof(SpatialConstraintGrid2x2))]
    public class SpatialConstraintGrid2x2Editor : SpatialConstraintGridEditor
    {
        public override void DrawConstraintEditor(SpatialConstraint constraint)
        {
            var constraint2x2 = constraint as SpatialConstraintGrid2x2;
            if (constraint2x2.cells == null || constraint2x2.cells[0] == null)
            {
                // gets invalidated during code change / hot-reloading
                throw new System.ApplicationException("invalid state");
            }

            var cells = constraint2x2.cells;
            for (int row = 0; row < 2; row++)
            {
                EditorGUILayout.BeginHorizontal();
                DrawGridCell(cells[row * 2 + 0]);
                DrawGridCell(cells[row * 2 + 1]);
                EditorGUILayout.EndHorizontal();
            }
        }

    }
}
