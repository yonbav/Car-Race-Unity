//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;
using DungeonArchitect.Graphs;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Renders the graph link in the graph editor
    /// </summary>
    public class GraphLinkRenderer
    {
        public static void DrawGraphLink(GraphRendererContext rendererContext, GraphLink link, GraphCamera camera)
        {
            if (link.Input == null || link.Output == null)
            {
                // Link not initialized yet. nothing to draw
                return;
            }

            Vector2 startPos = camera.WorldToScreen(link.Output.WorldPosition);
            Vector2 endPos = camera.WorldToScreen(link.Input.WorldPosition);
			var tangentStrength = link.GetTangentStrength() / camera.ZoomLevel;
            Vector3 startTan = startPos + link.Output.Tangent * tangentStrength;
            Vector3 endTan = endPos + link.Input.Tangent * tangentStrength;
            var lineColor = new Color(1, 1, 1, 0.75f);
            Handles.DrawBezier(startPos, endPos, startTan, endTan, lineColor, null, 3);

            // Draw the arrow cap
            var rotation = Quaternion.FromToRotation(new Vector3(1, 0, 0), link.Input.Tangent.normalized);
			float arrowSize = 10.0f / camera.ZoomLevel;
			float arrowWidth = 0.5f / camera.ZoomLevel;
            var arrowTails = new Vector2[] {
			rotation * new Vector3(1, arrowWidth) * arrowSize, 
			rotation * new Vector3(1, -arrowWidth) * arrowSize, 
		};
            Handles.color = lineColor;

            //Handles.DrawPolyLine(arrowTails);
            Handles.DrawLine(endPos, endPos + arrowTails[0]);
            Handles.DrawLine(endPos, endPos + arrowTails[1]);
            Handles.DrawLine(endPos + arrowTails[0], endPos + arrowTails[1]);

        }
    }
}
