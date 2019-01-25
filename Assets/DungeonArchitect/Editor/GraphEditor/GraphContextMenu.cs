//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using DungeonArchitect;
using DungeonArchitect.Graphs;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// The type of menu action to perform
    /// </summary>
    public enum GraphMenuAction
	{
		AddGameObjectNode,
		AddGameObjectArrayNode,
        AddSpriteNode,
        AddMarkerNode,
        AddMarkerEmitterNode
    }


    /// <summary>
    /// The graph context menu event data
    /// </summary>
    public class GraphContextMenuEvent
    {
        public GraphPin sourcePin;
        public Vector2 mouseWorldPosition;
        public object userdata;
    }

    /// <summary>
    /// The context menu shown when the user right clicks on the theme graph editor
    /// </summary>
    public class GraphContextMenu
    {
        string[] GetMarkers(Graph graph)
        {
            var markers = new List<string>();
            if (graph != null)
            {
                foreach (var node in graph.Nodes)
                {
                    if (node is MarkerNode)
                    {
                        markers.Add(node.Caption);
                    }
                }
            }
            var markerArray = markers.ToArray();
            System.Array.Sort(markerArray);
            return markerArray;
        }

        bool dragged;
        int dragButtonId = 1;

        bool showItemMeshNode;
        bool showItemMarkerNode;
        bool showItemMarkerEmitterNode;

        GraphPin sourcePin;
        Vector2 mouseWorldPosition;

        public delegate void OnRequestContextMenuCreation(Event e);
        public event OnRequestContextMenuCreation RequestContextMenuCreation;

        public delegate void OnMenuItemClicked(GraphMenuAction action, GraphContextMenuEvent e);
        public event OnMenuItemClicked MenuItemClicked;

        /// <summary>
        /// Handles mouse input
        /// </summary>
        /// <param name="e">Input event data</param>
        public void HandleInput(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == dragButtonId)
                    {
                        dragged = false;
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == dragButtonId)
                    {
                        dragged = true;
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == dragButtonId && !dragged)
                    {
                        if (RequestContextMenuCreation != null)
                        {
                            RequestContextMenuCreation(e);
                        }
                    }
                    break;
            }

        }

        /// <summary>
        /// Shows the context menu in the theme graph editor
        /// </summary>
        /// <param name="graph">The graph shown in the graph editor</param>
        /// <param name="sourcePin">The source pin, if the user dragged a link out of a pin. null otherwise</param>
        /// <param name="mouseWorld">The position of the mouse. The context menu would be shown from here</param>
        public void Show(Graph graph, GraphPin sourcePin, Vector2 mouseWorld)
        {
            showItemMeshNode = false;
            showItemMarkerNode = false;
            showItemMarkerEmitterNode = false;
            this.sourcePin = sourcePin;
            this.mouseWorldPosition = mouseWorld;
            if (sourcePin.Node is MarkerNode)
            {
                showItemMeshNode = true;
            }
            else if (sourcePin.Node is VisualNode)
            {
                if (sourcePin.PinType == GraphPinType.Input)
                {
                    // We can only create marker nodes from here
                    showItemMarkerNode = true;
                }
                else
                {
                    // We can only create marker emitter nodes from here
                    showItemMarkerEmitterNode = true;
                }
            }
            else if (sourcePin.Node is MarkerEmitterNode)
            {
                // we can only create mesh nodes from the input pin of this node
                showItemMeshNode = true;
            }

            ShowMenu(graph);
        }

        /// <summary>
        /// Show the context menu
        /// </summary>
        /// <param name="graph">The owning graph</param>
        public void Show(Graph graph)
        {
            showItemMeshNode = true;
            showItemMarkerNode = true;
            showItemMarkerEmitterNode = true;
            sourcePin = null;
            mouseWorldPosition = Vector2.zero;
            ShowMenu(graph);
        }

        private void ShowMenu(Graph graph)
        {
            var menu = new GenericMenu();
            if (showItemMeshNode)
			{
				menu.AddItem(new GUIContent("Add Game Object Node"), false, AddGameObjectNode);
				menu.AddItem(new GUIContent("Add Game Object Array Node"), false, AddGameObjectArrayNode);
                menu.AddItem(new GUIContent("Add Sprite Node"), false, AddSpriteNode);
            }
            if (showItemMarkerNode)
            {
                menu.AddItem(new GUIContent("Add Marker Node"), false, AddMarkerNode);
            }

            if (showItemMarkerEmitterNode)
            {
                var markers = GetMarkers(graph);
                if (markers.Length > 0)
                {
                    if (showItemMeshNode || showItemMarkerNode)
                    {
                        menu.AddSeparator("");
                    }
                    foreach (var marker in markers)
                    {
                        menu.AddItem(new GUIContent("Add Marker Emitter: " + marker), false, AddMarkerEmitterNode, marker);
                    }
                }
            }
            menu.ShowAsContext();
        }

        void AddGameObjectNode()
        {
            DispatchMenuItemEvent(GraphMenuAction.AddGameObjectNode, BuildEvent(null));
        }

		void AddGameObjectArrayNode()
		{
			DispatchMenuItemEvent(GraphMenuAction.AddGameObjectArrayNode, BuildEvent(null));
		}

        void AddSpriteNode()
        {
            DispatchMenuItemEvent(GraphMenuAction.AddSpriteNode, BuildEvent(null));
        }

        void AddMarkerNode()
        {
            DispatchMenuItemEvent(GraphMenuAction.AddMarkerNode, BuildEvent(null));
        }

        void AddMarkerEmitterNode(object userdata)
        {
            DispatchMenuItemEvent(GraphMenuAction.AddMarkerEmitterNode, BuildEvent(userdata));
        }

        GraphContextMenuEvent BuildEvent(object userdata)
        {
            var e = new GraphContextMenuEvent();
            e.userdata = userdata;
            e.sourcePin = sourcePin;
            e.mouseWorldPosition = mouseWorldPosition;
            return e;
        }

        void DispatchMenuItemEvent(GraphMenuAction action, GraphContextMenuEvent e)
        {
            if (MenuItemClicked != null)
            {
                MenuItemClicked(action, e);
            }
        }
    }
}
