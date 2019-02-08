//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using DungeonArchitect;
using DungeonArchitect.Utils;
using DungeonArchitect.Graphs;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Utility functions for various editor based features of Dungeon Architect
    /// </summary>
    public class DungeonEditorHelper
    {
        /// <summary>
        /// Creates a new Dungeon Theme in the specified asset folder.  Access from the Create context menu in the Project window
        /// </summary>
        [MenuItem("Assets/Create/Dungeon Theme")]
        static void CreateAssetInBrowser()
        {
            var defaultFileName = "DungeonTheme.asset";
            var path = GetAssetBrowserPath();
            var fileName = MakeFilenameUnique(path, defaultFileName);
            var fullPath = path + "/" + fileName;

            var graph = ScriptableObject.CreateInstance<Graph>();
            AssetDatabase.CreateAsset(graph, fullPath);

            // Add default marker nodes to the newly create graph
            CreateDefaultMarkerNodes(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ProjectWindowUtil.ShowCreatedAsset(graph);
        }

        /// <summary>
        /// Handle opening of theme graphs.
        /// When the user right clicks on the theme graph and selects open, the graph is shown in the theme editor
        /// </summary>
        /// <param name="instanceID"></param>
        /// <param name="line"></param>
        /// <returns>true if trying to open a dungeon theme, indicating that it has been handled.  false otherwise</returns>
        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var graph = Selection.activeObject as Graph;
            if (graph != null)
            {
                ShowEditor(graph);
                return true; //catch open file
            }

            return false; // let unity open the file
        }

        /// <summary>
        /// Shows the dungeon theme editor window and loads the specified graph into it
        /// </summary>
        /// <param name="graph">The graph to load in the dungeon theme editor window</param>
        public static void ShowEditor(Graph graph)
        {
            if (graph != null)
            {
                var window = EditorWindow.GetWindow<DungeonArchitectGraphEditor>();
                if (window != null)
                {
                    window.Init(graph);
                }
            }
            else
            {
                Debug.LogWarning("Invalid Dungeon theme file");
            }
        }

        /// <summary>
        /// Creates a unique filename in the specified asset directory
        /// </summary>
        /// <param name="dir">The target directory this file will be placed in.  Used for finding non-colliding filenames</param>
        /// <param name="filename">The prefered filename.  Will add incremental numbers to it till it finds a free filename</param>
        /// <returns>A filename not currently used in the specified directory</returns>
        public static string MakeFilenameUnique(string dir, string filename)
        {
            string fileNamePart = System.IO.Path.GetFileNameWithoutExtension(filename);
            string fileExt = System.IO.Path.GetExtension(filename);
            var indexedFileName = fileNamePart + fileExt;
            string path = System.IO.Path.Combine(dir, indexedFileName);
            for (int i = 1; ; ++i)
            {
                if (!System.IO.File.Exists(path))
                    return indexedFileName;

                indexedFileName = fileNamePart + " " + i + fileExt;
                path = System.IO.Path.Combine(dir, indexedFileName);
            }
        }

        /// <summary>
        /// Adds the node to the graph asset so it can be serialized to disk
        /// </summary>
        /// <param name="graph">The owning graph</param>
        /// <param name="node">The node to add to the graph</param>
        public static void AddToAsset(Graph graph, GraphNode node)
        {
            AssetDatabase.AddObjectToAsset(node, graph);
            // Add all the pins in this node to the graph asset as well
            var pins = new List<GraphPin>();
            pins.AddRange(node.InputPins);
            pins.AddRange(node.OutputPins);
            foreach (var pin in pins)
            {
                AssetDatabase.AddObjectToAsset(pin, graph);
            }
        }

        static string GetAssetBrowserPath()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }

            else if (System.IO.Path.GetExtension(path) != "")
            {
                path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }
            return path;
        }

        /// <summary>
        /// Adds the node to the graph asset so it can be serialized to disk
        /// </summary>
        /// <param name="graph">The owning graph</param>
        /// <param name="link">The link to add to the graph</param>
        public static void AddToAsset(Graph graph, GraphLink link)
        {
            AssetDatabase.AddObjectToAsset(link, graph);
        }

        /// <summary>
        /// Marks the graph as dirty so that it is serialized to disk again when saved
        /// </summary>
        /// <param name="graph"></param>
        public static void MarkAsDirty(Graph graph)
        {
            EditorUtility.SetDirty(graph);
        }

        /// <summary>
        /// Creates default marker nodes when a new graph is created
        /// </summary>
        static void CreateDefaultMarkerNodes(Graph graph)
        {
            if (graph == null)
            {
                Debug.LogWarning("Cannot create default marker nodes. graph is null");
                return;
            }
            var markerNames = new string[] {
			    DungeonConstants.ST_GROUND,
			    DungeonConstants.ST_WALL,
			    DungeonConstants.ST_WALLSEPARATOR,
			    DungeonConstants.ST_FENCE,
			    DungeonConstants.ST_FENCESEPARATOR,
			    DungeonConstants.ST_DOOR,
			    DungeonConstants.ST_STAIR,
			    DungeonConstants.ST_STAIR2X,
			    DungeonConstants.ST_WALLHALF,
			    DungeonConstants.ST_WALLHALFSEPARATOR
		    };

            // Make sure we don't have any nodes in the graph
            if (graph.Nodes.Count > 0)
            {
                return;
            }

            const int INTER_NODE_X = 200;
            const int INTER_NODE_Y = 300;
            int itemsPerRow = markerNames.Length / 2;
            for (int i = 0; i < markerNames.Length; i++)
            {
                int ix = i % itemsPerRow;
                int iy = i / itemsPerRow;
                int x = ix * INTER_NODE_X;
                int y = iy * INTER_NODE_Y;
                var node = GraphOperations.CreateNode<MarkerNode>(graph);
                AddToAsset(graph, node);
                node.Position = new Vector2(x, y);
                node.Caption = markerNames[i];
            }
        }

        /// <summary>
        /// Creates an editor tag
        /// </summary>
        /// <param name="tag"></param>
        public static void CreateEditorTag(string tag)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // Check if the tag is already present
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tag))
                {
                    // Tag already exists.  do not add a duplicate
                    return;
                }
            }

            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty n = tagsProp.GetArrayElementAtIndex(0);
            n.stringValue = tag;

            tagManager.ApplyModifiedProperties();
        }


		// Resets the node IDs of the graph. Useful if you have cloned another graph
		//[MenuItem("Debug DA/Fix Node Ids")]
		public static void _Advanced_RecreateGraphNodeIds()
		{
			var editor = EditorWindow.GetWindow<DungeonArchitectGraphEditor>();
			if (editor != null && editor.GraphEditor != null && editor.GraphEditor.Graph != null)
			{
				var graph = editor.GraphEditor.Graph;
				foreach (var node in graph.Nodes)
				{
					node.Id = System.Guid.NewGuid().ToString();
				}
			}
			
		}

        public static void MarkSceneDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

    }
}
