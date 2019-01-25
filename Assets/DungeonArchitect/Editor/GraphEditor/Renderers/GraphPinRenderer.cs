//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections;
using DungeonArchitect.Graphs;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Renders a graph pin hosted inside a node
    /// </summary>
    public class GraphPinRenderer
    {

        public static void Draw(GraphRendererContext rendererContext, GraphPin pin, GraphCamera camera)
        {
            var pinBounds = new Rect(pin.GetBounds());
			var positionWorld = pin.Node.Position + pinBounds.position;
            var positionScreen = camera.WorldToScreen(positionWorld);
			pinBounds.position = positionScreen;
			pinBounds.size /= camera.ZoomLevel;

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = GetPinColor(pin);
			GUI.Box(pinBounds, "");
            GUI.backgroundColor = originalColor;

            // Draw the pin texture
            var pinTexture = rendererContext.Resources.GetResource<Texture2D>(DungeonEditorResources.TEXTURE_PIN_GLOW);
			if (pinTexture != null)
            {
				GUI.DrawTexture(pinBounds, pinTexture);
            }
        }

        static Color GetPinColor(GraphPin pin)
        {
            Color color;
            if (pin.ClickState == GraphPinMouseState.Clicked)
            {
                color = GraphEditorConstants.PIN_COLOR_CLICK;
            }
            else if (pin.ClickState == GraphPinMouseState.Hover)
            {
                color = GraphEditorConstants.PIN_COLOR_HOVER;
            }
            else
            {
                color = GraphEditorConstants.PIN_COLOR;
            }
            return color;
        }

    }
}
