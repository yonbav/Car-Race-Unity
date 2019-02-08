//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Graphs;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Custom property editors for MarkerEmitterNode
    /// </summary>
    [CustomEditor(typeof(MarkerEmitterNode))]
    public class MarkerEmitterNodeEditor : PlaceableNodeEditor
    {

        public override void OnEnable()
        {
            base.OnEnable();
            drawOffset = true;
            drawAttachments = false;
        }

        protected override void DrawPreInspectorGUI()
        {
            var emitterNode = target as MarkerEmitterNode;
            var markerCaption = (emitterNode.Marker != null) ? emitterNode.Marker.Caption : "Unknown";
            GUILayout.Label("Marker Emitter Node: " + markerCaption, EditorStyles.boldLabel);
        }
    }

    /// <summary>
    /// Renders a MarkerEmitterNode
    /// </summary>
    public class MarkerEmitterNodeRenderer : GraphNodeRenderer
    {
        public override void Draw(GraphRendererContext rendererContext, GraphNode node, GraphCamera camera)
        {
            // Draw the background base texture
            DrawNodeTexture(rendererContext, node, camera, DungeonEditorResources.TEXTURE_MARKER_NODE_BG);

            var style = GUI.skin.GetStyle("Label");
            style.alignment = TextAnchor.MiddleCenter;

            var emitterNode = node as MarkerEmitterNode;
            var title = (emitterNode.Marker != null) ? emitterNode.Marker.Caption : "{NONE}";

			DrawTextCentered(rendererContext, node, camera, title, new Vector2(0, -2));

            // Draw the foreground frame textures
            DrawNodeTexture(rendererContext, node, camera, DungeonEditorResources.TEXTURE_MARKER_EMITTER_NODE_FRAME);

            if (node.Selected)
            {
                DrawNodeTexture(rendererContext, node, camera, DungeonEditorResources.TEXTURE_MARKER_NODE_SELECTION);
            }

            // Draw the pins
            base.Draw(rendererContext, node, camera);
        }

        protected override Color getBackgroundColor(GraphNode node)
        {
            var color = new Color(0.2f, 0.25f, 0.4f);
            return node.Selected ? GraphEditorConstants.NODE_COLOR_SELECTED : color;
        }

    }
}
