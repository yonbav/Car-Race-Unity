//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEditor;
using UnityEngine;
using System.Collections;
using DungeonArchitect;
using DMathUtils = DungeonArchitect.Utils.MathUtils;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Custom property editor for the paint mode object
    /// </summary>
    public class DungeonPaintModeEditor : Editor
    {
        protected virtual void OnEnable()
        {
            SceneView.onSceneGUIDelegate += SceneGUI;
            var paintMode = target as DungeonPaintMode;
            if (paintMode != null)
            {
                var model = paintMode.GetDungeonModel();
                if (model.ToolData == null)
                {
                    Undo.RecordObjects(new Object[] {model}, "Enter Tool Mode");
                    model.ToolData = model.CreateToolDataInstance();
                    Undo.RecordObjects(new Object[] { model.ToolData }, "Create Tool Data");
                }
            }

        }

        protected virtual void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= SceneGUI;
        }

        protected virtual void SceneGUI(SceneView sceneview) {
        }
    }
}
