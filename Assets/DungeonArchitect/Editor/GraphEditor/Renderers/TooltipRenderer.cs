//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Graph tooltip singleton
    /// </summary>
    public class GraphTooltip
    {
        /// <summary>
        /// Set this to display a tooltip in the graph editor
        /// </summary>
        public static string message = "";
        public static void Clear()
        {
            message = "";
        }
    }

    /// <summary>
    /// Renders a tooltip in the graph editor. The tooltip message is defined in GraphTooltip.message
    /// </summary>
    public class GraphTooltipRenderer
    {
        public static void Draw(GraphRendererContext rendererContext, Vector2 mousePosition)
        {
            if (GraphTooltip.message == null || GraphTooltip.message.Trim().Length == 0) return;

            var tooltipPadding = new Vector2(4, 4);

            var drawPosition = mousePosition + new Vector2(15, 5);

            var tooltipString = GraphTooltip.message;
            var style = GUI.skin.GetStyle("label");
            var contentSize = style.CalcSize(new GUIContent(tooltipString));
            drawPosition -= tooltipPadding;
            contentSize += tooltipPadding * 2;
            var bounds = new Rect(drawPosition.x, drawPosition.y, contentSize.x, contentSize.y);

            GUI.backgroundColor = new Color(.15f, .15f, .15f);
            GUI.Box(bounds, "");

            var innerGlow = rendererContext.Resources.GetResource<Texture2D>(DungeonEditorResources.TEXTURE_PIN_GLOW);
            GUI.DrawTexture(bounds, innerGlow);

            style.alignment = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            GUI.Label(bounds, tooltipString, style);

        }
    }
}
