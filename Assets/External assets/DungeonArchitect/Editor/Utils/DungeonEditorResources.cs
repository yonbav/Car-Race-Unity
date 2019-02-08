//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections.Generic;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// The resource filename constants used by dungeon architect editor
    /// </summary>
    public class DungeonEditorResources
    {
		public static readonly string TEXTURE_GO_NODE_SELECTION = "graph_node_go_selection";
		public static readonly string TEXTURE_GO_NODE_FRAME = "graph_node_go_frame";
        public static readonly string TEXTURE_GO_NODE_BG = "graph_node_go_bg";
        public static readonly string TEXTURE_PIN_GLOW = "graph_pin_glow";
		public static readonly string TEXTURE_MULTI_GO_NODE_FRAME = "graph_node_multi_go_frame";

        public static readonly string TEXTURE_MARKER_NODE_SELECTION = "graph_node_marker_selection";
        public static readonly string TEXTURE_MARKER_NODE_FRAME = "graph_node_marker_frame";
        public static readonly string TEXTURE_MARKER_EMITTER_NODE_FRAME = "graph_node_marker_emitter_frame";
        public static readonly string TEXTURE_MARKER_NODE_BG = "graph_node_marker_bg";

        public static readonly string GUI_STYLE_BANNER = "DABannerStyle";


        Dictionary<string, Object> resources = new Dictionary<string, Object>();

        /// <summary>
        /// Loads and retrieves the resource of the specified type
        /// </summary>
        /// <typeparam name="T">The type of the resource (e.g. Texture2D)</typeparam>
        /// <param name="path">The path to load the resource from.  Usually specified from the constants defined in this class</param>
        /// <returns>The loaded resource</returns>
        public T GetResource<T>(string path) where T : Object
        {
            if (!resources.ContainsKey(path))
            {
                var resource = Resources.Load<T>(path);
                resources.Add(path, resource);
            }

            return resources[path] as T;
        }
    }
}
