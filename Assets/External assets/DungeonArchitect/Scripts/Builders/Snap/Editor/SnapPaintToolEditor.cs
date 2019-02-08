using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DMathUtils = DungeonArchitect.Utils.MathUtils;
using DungeonArchitect.Builders.Snap;

namespace DungeonArchitect.Editors
{
    [CustomEditor(typeof(SnapPaintTool))]
    public class SnapPaintToolEditor : DungeonPaintModeEditor
    {
        GameObject cursor;
        int cursorModuleIndex = 0;

        protected override void SceneGUI(SceneView sceneview)
        {
            base.SceneGUI(sceneview);

            var e = Event.current;

            if (e.type == EventType.ScrollWheel)
            {
                var delta = (int)Mathf.Sign(e.delta.y);
                var moduleTemplate = GetNextModule(delta);
                CreateCursor(moduleTemplate);
                e.Use();
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {

                e.Use();
            }
            else if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
            }

            DrawCursor();
        }

        void DrawCursor()
        {

        }

        void CreateCursor(GameObject template)
        {
            if (cursor != null)
            {
                DestroyImmediate(cursor);
                cursor = null;
            }

            cursor = Instantiate(template);
            cursor.name = "SnapEditorCursor";

            var tool = target as SnapPaintTool;
            var cursorMaterial = tool.cursorMaterial;
            var doorMaterial = tool.cursorDoorMaterial;
            if (tool != null && cursorMaterial != null)
            {
                if (doorMaterial == null)
                {
                    doorMaterial = cursorMaterial;
                }

                var moduleRenderers = cursor.GetComponentsInChildren<MeshRenderer>();

                var connections = cursor.GetComponentsInChildren<SnapConnection>();
                var connectionRenderers = new List<MeshRenderer>();
                foreach (var connection in connections)
                {
                    connectionRenderers.AddRange(connection.GetComponentsInChildren<MeshRenderer>());
                }

                foreach (var renderer in moduleRenderers)
                {
                    renderer.material = connectionRenderers.Contains(renderer) ? doorMaterial : cursorMaterial;
                }
            }
        }

        GameObject GetNextModule(int indexOffset)
        {
            var paintTool = target as SnapPaintTool;
            if (paintTool == null) return null;
            var config = paintTool.GetDungeonConfig() as SnapConfig;
            if (config == null) return null;

            var numModules = config.Modules.Length;
            if (numModules == 0) return null;

            cursorModuleIndex += indexOffset;
            if (cursorModuleIndex < 0) cursorModuleIndex += numModules;
            cursorModuleIndex %= numModules;
            cursorModuleIndex = Mathf.Clamp(cursorModuleIndex, 0, numModules - 1);
            
            return config.Modules[cursorModuleIndex];
        }


        protected override void OnEnable()
        {
            base.OnEnable();
            var moduleTemplate = GetNextModule(1);
            CreateCursor(moduleTemplate);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (cursor != null)
            {
                DestroyImmediate(cursor);
            }
        }
    }
}