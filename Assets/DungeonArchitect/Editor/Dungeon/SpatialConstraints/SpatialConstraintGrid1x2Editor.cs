//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Constraints;
using DungeonArchitect.Constraints.Grid;

namespace DungeonArchitect.Editors
{
    [CustomEditor(typeof(SpatialConstraintGrid1x2))]
    [ConstraintEditor(typeof(SpatialConstraintGrid1x2))]
    public class SpatialConstraintGrid1x2Editor : SpatialConstraintGridEditor
    {
        public override void DrawConstraintEditor(SpatialConstraint constraint)
        {
            var constraint1x2 = constraint as SpatialConstraintGrid1x2;

            EditorGUILayout.BeginHorizontal();
            DrawGridCell(constraint1x2.left);
            DrawGridCell(constraint1x2.right);
            EditorGUILayout.EndHorizontal();
            
        }

    }
}
