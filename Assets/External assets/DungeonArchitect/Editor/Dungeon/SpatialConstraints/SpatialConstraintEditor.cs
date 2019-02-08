//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Constraints;
using DungeonArchitect.Constraints.Grid;

namespace DungeonArchitect.Editors
{
    public abstract class SpatialConstraintEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawConstraintEditor(target as SpatialConstraint);
        }

        public virtual void DrawConstraintEditor(SpatialConstraint constraint)
        {
            GUILayout.Label("Editor not implemented");
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ConstraintEditorAttribute : System.Attribute
    {
        public Type constraintType;

        public ConstraintEditorAttribute(Type constraintType)
        {
            this.constraintType = constraintType;
        }
    }
}