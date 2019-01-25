//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using DungeonArchitect;
using DungeonArchitect.Utils;
using DungeonArchitect.Graphs;
using DMathUtils = DungeonArchitect.Utils.MathUtils;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// The rendering context for drawing the theme editor
    /// </summary>
    public class GraphRendererContext
    {
        DungeonEditorResources resources = new DungeonEditorResources();
        public DungeonEditorResources Resources
        {
            get { return resources; }
        }
    }

    /// <summary>
    /// The graph editor script for managing a graph.  This contains the bulk of the logic for graph editing
    /// </summary>
    [Serializable]
    public class GraphEditor : ScriptableObject
    {
        [SerializeField]
        Graph graph;

        [SerializeField]
        GraphCamera camera;

        GraphSelectionBox selectionBox;
        KeyboardState keyboardState;
        CursorDragLink cursorDragLink;
        GraphContextMenu contextMenu;
        GraphNodeRendererFactory nodeRenderers;
        GraphRendererContext rendererContext = new GraphRendererContext();
        bool realtimeUpdate = true;

        Vector2 lastMousePosition = new Vector2();

        // tracks dungeon objects in the scene that have the same graph being edited. This is used for realtime updates
        DungeonObjectTraker dungeonObjectTraker = new DungeonObjectTraker();

        /// <summary>
        /// The owning graph
        /// </summary>
        public Graph Graph
        {
            get
            {
                return graph;
            }
        }

        /// <summary>
        /// If set, updates the dungeon in the viewport whenever the state of the graph is modified
        /// </summary>
        public bool RealtimeUpdate
        {
            get
            {
                return realtimeUpdate;
            }
            set
            {
                realtimeUpdate = value;
            }
        }

        /// <summary>
        /// Initializes the graph editor with the specified graph
        /// </summary>
        /// <param name="graph">The owning graph</param>
        /// <param name="editorBounds">The bounds of the editor window</param>
        public void Init(Graph graph, Rect editorBounds)
        {
            if (this.graph != graph)
            {
                RemoveGraphListeners();
                this.graph = graph;
                AddGraphListeners();

                // Reset the camera
                camera = new GraphCamera();
                FocusCameraOnBestFit(editorBounds);
            }
        }

        /// <summary>
        /// Moves the graph editor viewport to show the marker on the screen
        /// </summary>
        /// <param name="markerName">The name of the marker to focus on</param>
        /// <param name="editorBounds">The bounds of the editor</param>
        public void FocusCameraOnMarker(string markerName, Rect editorBounds)
        {
            camera.FocusOnMarker(graph, editorBounds, markerName);
        }

        /// <summary>
        /// Moves the graph editor viewport to show as many markers as possible. 
        /// Called when a new graph is loaded
        /// </summary>
        /// <param name="editorBounds">The bounds of the editor window</param>
        public void FocusCameraOnBestFit(Rect editorBounds)
        {
            camera.FocusOnBestFit(graph, editorBounds);
        }

        public void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            if (camera == null)
            {
                camera = new GraphCamera();
            }
            if (selectionBox == null)
            {
                selectionBox = new GraphSelectionBox();
                selectionBox.SelectionPerformed += HandleBoxSelection;
            }
            if (keyboardState == null)
            {
                keyboardState = new KeyboardState();
            }
            if (cursorDragLink == null)
            {
                cursorDragLink = new CursorDragLink(this);
                cursorDragLink.DraggedLinkReleased += HandleMouseDraggedLinkReleased;
            }
            if (contextMenu == null)
            {
                contextMenu = new GraphContextMenu();
                contextMenu.RequestContextMenuCreation += OnRequestContextMenuCreation;
                contextMenu.MenuItemClicked += OnMenuItemClicked;
            }

            RemoveGraphListeners();
            AddGraphListeners();

            InitializeNodeRenderers();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        public void OnDisable()
        {
            if (cursorDragLink != null)
            {
                cursorDragLink.DraggedLinkReleased -= HandleMouseDraggedLinkReleased;
                cursorDragLink.Destroy();
                cursorDragLink = null;
            }

            RemoveGraphListeners();

            if (selectionBox != null)
            {
                selectionBox.SelectionPerformed -= HandleBoxSelection;
            }
            if (cursorDragLink != null)
            {
                cursorDragLink.DraggedLinkReleased -= HandleMouseDraggedLinkReleased;
            }
            if (contextMenu != null)
            {
                contextMenu.RequestContextMenuCreation -= OnRequestContextMenuCreation;
                contextMenu.MenuItemClicked -= OnMenuItemClicked;
            }
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            // TODO: Check if we need to un-subscribe from the events registered in OnEnable

        }

        public void OnDestroy()
        {
            if (cursorDragLink != null)
            {
                cursorDragLink.Destroy();
                cursorDragLink = null;
            }
        }

        void OnUndoRedoPerformed()
        {
			if (graph != null) {
            	graph.NotifyStateChanged();
			}
        }

        void HandleMarkedDirty(Graph graph)
        {
            EditorUtility.SetDirty(graph);
        }

        void AddGraphListeners()
        {
            if (graph != null)
            {
                graph.MarkedDirty += HandleMarkedDirty;
                graph.GraphStateChanged += HandleGraphStateChanged;
            }
        }
        void RemoveGraphListeners()
        {
            if (graph != null)
            {
                graph.MarkedDirty -= HandleMarkedDirty;
                graph.GraphStateChanged -= HandleGraphStateChanged;
            }
        }

        public void Update()
        {
            if (realtimeUpdate)
            {
                dungeonObjectTraker.ActiveGraph = graph;
                dungeonObjectTraker.Update();
            }
        }

        void HandleGraphStateChanged(Graph graph)
        {
            if (realtimeUpdate)
            {
                dungeonObjectTraker.RequestRebuild();
            }
        }

        void InitializeNodeRenderers()
        {
            if (nodeRenderers == null)
            {
                nodeRenderers = new GraphNodeRendererFactory();
				nodeRenderers.RegisterNodeRenderer(typeof(MarkerNode), new MarkerNodeRenderer());
				nodeRenderers.RegisterNodeRenderer(typeof(GameObjectNode), new MeshNodeRenderer());
				nodeRenderers.RegisterNodeRenderer(typeof(GameObjectArrayNode), new MeshArrayNodeRenderer());
                nodeRenderers.RegisterNodeRenderer(typeof(SpriteNode), new SpriteNodeRenderer());
                nodeRenderers.RegisterNodeRenderer(typeof(MarkerEmitterNode), new MarkerEmitterNodeRenderer());
            }
        }

        void HandleBoxSelection(Rect boundsScreenSpace)
        {
            bool multiSelect = keyboardState.ShiftPressed;
            bool selectedStateChanged = false;
            foreach (var node in graph.Nodes)
            {
                // node bounds in world space
                var nodeBounds = new Rect(node.Bounds);

                // convert the position to screen space
                nodeBounds.position = camera.WorldToScreen(nodeBounds.position);
				nodeBounds.size /= camera.ZoomLevel;

                var selected = nodeBounds.Overlaps(boundsScreenSpace);
                if (multiSelect)
                {
                    if (selected)
                    {
                        selectedStateChanged |= SetSelectedState(node, selected);
                    }
                }
                else
                {
                    selectedStateChanged |= SetSelectedState(node, selected);
                }
            }

            if (selectedStateChanged)
            {
                OnNodeSelectionChanged();
            }
        }

        bool SetSelectedState(GraphNode node, bool selected)
        {
            bool stateChanged = (node.Selected != selected);
            node.Selected = selected;
            return stateChanged;
        }

        void HandleSelect(Event e)
        {
            // Update the node selected flag
            var mousePosition = e.mousePosition;
            var mousePositionWorld = camera.ScreenToWorld(mousePosition);
            var buttonId = 0;
            if (e.type == EventType.MouseDown && e.button == buttonId)
            {
                bool multiSelect = keyboardState.ShiftPressed;
                bool toggleSelect = keyboardState.ControlPressed;
                // sort the nodes front to back
                GraphNode[] sortedNodes = graph.Nodes.ToArray();
                System.Array.Sort(sortedNodes, new NodeReversedZIndexComparer());

                GraphNode mouseOverNode = null;
                foreach (var node in sortedNodes)
                {
                    var mouseOver = node.Bounds.Contains(mousePositionWorld);
                    if (mouseOver)
                    {
                        mouseOverNode = node;
                        break;
                    }
                }

                foreach (var node in sortedNodes)
                {
                    var mouseOver = (node == mouseOverNode);

                    if (mouseOverNode != null && mouseOverNode.Selected && !toggleSelect)
                    {
                        multiSelect = true;	// select multi-select so that we can drag multiple objects
                    }
                    if (multiSelect || toggleSelect)
                    {
                        if (mouseOver && multiSelect)
                        {
                            node.Selected = true;
                        }
                        else if (mouseOver && toggleSelect)
                        {
                            node.Selected = !node.Selected;
                        }
                    }
                    else
                    {
                        node.Selected = mouseOver;
                    }

                    if (node.Selected)
                    {
                        BringToFront(node);
                    }
                }

                if (mouseOverNode == null)
                {
                    // No nodes were selected 
                    Selection.activeObject = null;
                }

                OnNodeSelectionChanged();
            }
        }

        bool draggingNodes = false;
        void HandleDrag(Event e)
        {
            int dragButton = 0;
            if (draggingNodes)
            {
                if (e.type == EventType.MouseUp && e.button == dragButton)
                {
                    draggingNodes = false;
                }
                else if (e.type == EventType.MouseDrag && e.button == dragButton)
                {
                    // Drag all the selected nodes
                    foreach (var node in graph.Nodes)
                    {
                        if (node.Selected)
                        {
                            Undo.RecordObject(node, "Move Node");
							var delta = e.delta * camera.ZoomLevel;
                            node.DragNode(delta);
                        }
                    }
                }
            }
            else
            {
                // Check if we have started to drag
                if (e.type == EventType.MouseDown && e.button == dragButton)
                {
                    // Find the node that was clicked below the mouse
                    var mousePosition = e.mousePosition;
                    var mousePositionWorld = camera.ScreenToWorld(mousePosition);

                    // sort the nodes front to back
                    GraphNode[] sortedNodes = graph.Nodes.ToArray();
                    System.Array.Sort(sortedNodes, new NodeReversedZIndexComparer());

                    GraphNode mouseOverNode = null;
                    foreach (var node in sortedNodes)
                    {
                        var mouseOver = node.Bounds.Contains(mousePositionWorld);
                        if (mouseOver)
                        {
                            mouseOverNode = node;
                            break;
                        }
                    }

                    if (mouseOverNode != null && mouseOverNode.Selected)
                    {
                        // Make sure we are not over a pin
                        var pins = new List<GraphPin>();
                        pins.AddRange(mouseOverNode.InputPins);
                        pins.AddRange(mouseOverNode.OutputPins);
                        bool isOverPin = false;
                        GraphPin overlappingPin = null;
                        foreach (var pin in pins)
                        {
                            if (pin.ContainsPoint(mousePositionWorld))
                            {
                                isOverPin = true;
                                overlappingPin = pin;
                                break;
                            }
                        }
                        if (!isOverPin)
                        {
                            draggingNodes = true;
                        }
                        else
                        {
                            HandleDragPin(overlappingPin);
                        }
                    }
                }
            }
        }

        void HandleDragPin(GraphPin pin)
        {
            cursorDragLink.Activate(pin);
        }

        /// <summary>
        /// Handles user input (mouse and keyboard)
        /// </summary>
        /// <param name="e"></param>
        public void HandleInput(Event e)
        {
            if (graph == null)
            {
                // Graph is not yet initialized
                return;
            }
            lastMousePosition = e.mousePosition;
            camera.HandleInput(e);
            keyboardState.HandleInput(e);

			HandleKeyboard(e);
            HandleDelete(e);
            HandleSelect(e);
            HandleDrag(e);

            // sort the nodes front to back
            GraphNode[] sortedNodes = graph.Nodes.ToArray();
            System.Array.Sort(sortedNodes, new NodeReversedZIndexComparer());

            // Handle the input from front to back
            bool inputProcessed = false;
            foreach (var node in sortedNodes)
            {
                if (node == null) continue;
                inputProcessed = GraphInputHandler.HandleNodeInput(node, e, camera);
                if (inputProcessed)
                {
                    break;
                }
            }

            cursorDragLink.HandleInput(e);
            contextMenu.HandleInput(e);

            if (!inputProcessed)
            {
                selectionBox.HandleInput(e);
            }

        }

        void PerformCopy(Event e)
        {
            // Fetch all selected nodes
            var selectedNodes = from node in graph.Nodes
                                where node.Selected
                                select node.Id;

			var serializer = new System.Xml.Serialization.XmlSerializer(typeof(string[]));
            var writer = new System.IO.StringWriter();
            serializer.Serialize(writer, selectedNodes.ToArray());
            var copyText = writer.GetStringBuilder().ToString();

            EditorGUIUtility.systemCopyBuffer = copyText;
        }

        void PerformPaste(Event e)
        {
            var copyText = EditorGUIUtility.systemCopyBuffer;
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(string[]));
            string[] copyNodeIds = (string[])serializer.Deserialize(new System.IO.StringReader(copyText));

            var mouseWorld = camera.ScreenToWorld(e.mousePosition);
            float offsetXDelta = 130;
            float offsetX = 0;

            foreach (var id in copyNodeIds)
            {
                var sourceNode = graph.GetNode(id);
                var copiedNode = GraphOperations.DuplicateNode(graph, sourceNode);

                // Add the copied node to the asset file
                DungeonEditorHelper.AddToAsset(graph, copiedNode);

                // Update the bounds of the node to move it near the cursor
                var bounds = copiedNode.Bounds;
                bounds.x = mouseWorld.x + offsetX;
                bounds.y = mouseWorld.y;
                copiedNode.Bounds = bounds;

                offsetX += offsetXDelta;
            }
        }

        void HandleKeyboard(Event e)
        {
            if (!e.isKey) return;
			var controlPressed = (e.control || e.command);
			if (e.keyCode == KeyCode.C && controlPressed && e.type == EventType.KeyUp)
            {
                PerformCopy(e);
            }
			else if (e.keyCode == KeyCode.V && controlPressed && e.type == EventType.KeyUp)
            {
                PerformPaste(e);
            }
        }

        public void OnNodeSelectionChanged()
        {
            // Fetch all selected nodes
            var selectedNodes = from node in graph.Nodes
                                where node.Selected
                                select node;

            Selection.objects = selectedNodes.ToArray();
        }

		/// <summary>
		/// Called when the user right clicks on a node
		/// </summary>
		/// <param name="e">E.</param>
		void OnRequestNodeContextMenuCreation(GraphNode node, Event e) {
			var nodeMenu = new GenericMenu();
			nodeMenu.AddItem(new GUIContent("Copy"), false, () => PerformCopy(e));
			nodeMenu.AddItem(new GUIContent("Delete"), false, () => PerformDelete(e));
			nodeMenu.ShowAsContext();
        }
        
        void OnRequestContextMenuCreation(Event e)
        {
            // Make sure we are not over an existing node
            var mouseWorld = camera.ScreenToWorld(e.mousePosition);
            foreach (var node in graph.Nodes)
            {
                if (node.Bounds.Contains(mouseWorld))
                {
                    // the user has clicked on a node. Handle this with a separate logic
					//OnRequestNodeContextMenuCreation(node, e);
					return;
                }
            }

            contextMenu.Show(graph);
        }

        MarkerEmitterNode CreateMarkerEmitterNode(Vector2 mouseScreenPos, string markerName)
        {
            // find the marker node with this name
            MarkerNode markerNode = null;
            foreach (var node in graph.Nodes)
            {
                if (node is MarkerNode)
                {
                    var marker = node as MarkerNode;
                    if (marker.Caption == markerName)
                    {
                        markerNode = marker;
                        break;
                    }
                }
            }

            if (markerNode == null)
            {
                // No marker node found with this ids
                return null;
            }
            var emitterNode = CreateNode<MarkerEmitterNode>(mouseScreenPos);
            emitterNode.Marker = markerNode;
            return emitterNode;
        }

        void OnMenuItemClicked(GraphMenuAction action, GraphContextMenuEvent e)
        {
            var mouseScreen = lastMousePosition;
            GraphNode node = null;
            if (action == GraphMenuAction.AddGameObjectNode)
            {
                node = CreateNode<GameObjectNode>(mouseScreen);
                SelectNode(node);
			}
			if (action == GraphMenuAction.AddGameObjectArrayNode)
			{
				node = CreateNode<GameObjectArrayNode>(mouseScreen);
				SelectNode(node);
			}
            else if (action == GraphMenuAction.AddSpriteNode)
            {
                node = CreateNode<SpriteNode>(mouseScreen);
                SelectNode(node);
            }
            else if (action == GraphMenuAction.AddMarkerNode)
            {
                node = CreateNode<MarkerNode>(mouseScreen);
                SelectNode(node);
            }
            else if (action == GraphMenuAction.AddMarkerEmitterNode)
            {
                if (e.userdata != null)
                {
                    var markerName = e.userdata as String;
                    node = CreateMarkerEmitterNode(mouseScreen, markerName);
                    if (node != null)
                    {
                        SelectNode(node);
                    }
                }
            }


            if (node != null)
            {
                // Check if the menu was created by dragging out a link
                if (e.sourcePin != null)
                {
                    GraphPin targetPin =
                            e.sourcePin.PinType == GraphPinType.Input ?
                            node.OutputPins[0] :
                            node.InputPins[0];

                    // Align the target pin with the mouse position where the link was dragged and released
                    node.Position = e.mouseWorldPosition - targetPin.Position;

                    GraphPin inputPin, outputPin;
                    if (e.sourcePin.PinType == GraphPinType.Input)
                    {
                        inputPin = e.sourcePin;
                        outputPin = targetPin;
                    }
                    else
                    {
                        inputPin = targetPin;
                        outputPin = e.sourcePin;
                    }
                    CreateLinkBetweenPins(outputPin, inputPin);
                }
            }
        }

        void HandleDelete(Event e)
        {
            if (e.type == EventType.KeyDown) {
				var deletePressed = (e.keyCode == KeyCode.Delete);
				deletePressed |= (e.keyCode == KeyCode.Backspace && e.command);

				if (deletePressed) {
					PerformDelete(e);
				}
            }
        }

        public void DeleteNodes(GraphNode[] nodesToDelete)
        {
            if (nodesToDelete.Length == 0)
            {
                return;
            }

            System.Array.Sort(nodesToDelete, new NodeDeletionOrderComparer());
            foreach (var node in nodesToDelete)
            {
                GraphOperations.DestroyNode(node);
            }

            graph.MarkAsDirty();
            graph.NotifyStateChanged();
        }

        void PerformDelete(Event e) {
            var nodesToDelete = new List<GraphNode>();
            foreach (var node in graph.Nodes)
            {
                if (node.Selected)
                {
                    nodesToDelete.Add(node);
                }
            }
            var deletionList = nodesToDelete.ToArray();
            System.Array.Sort(deletionList, new NodeDeletionOrderComparer());
            foreach (var node in deletionList)
            {
                GraphOperations.DestroyNode(node);
            }

            if (deletionList.Length > 0)
            {
                graph.MarkAsDirty();
                graph.NotifyStateChanged();
            }
        }
		
		/// <summary>
        /// Renders the graph editor in the editor window
        /// </summary>
        /// <param name="bounds">The bounds of the editor window</param>
        public void Draw(Rect bounds)
        {
            if (graph == null)
            {
                // Graph is not yet initialized
                DrawGraphNotInitializedMessage(bounds);
                return;
            }
            var windowWorldPos = camera.ScreenToWorld(Vector3.zero);
			var windowWorldBounds = new Rect(windowWorldPos, bounds.size * camera.ZoomLevel);

            DrawGrid(windowWorldBounds);
            DrawBranding(bounds);
			DrawEditorStats(bounds);


            // Draw the links
            cursorDragLink.Draw(rendererContext, camera);
            foreach (var link in graph.Links)
            {
                if (DMathUtils.Intersects(windowWorldBounds, link))
                {
                    GraphLinkRenderer.DrawGraphLink(rendererContext, link, camera);
                }
            }

            // Draw the nodes
            GraphNode[] sortedNodes = graph.Nodes.ToArray();
            System.Array.Sort(sortedNodes, new NodeZIndexComparer());

            foreach (var node in sortedNodes)
            {
                if (node == null) continue;
                // Draw only if this node is visible in the editor
                if (DMathUtils.Intersects(windowWorldBounds, node.Bounds))
                {
                    var renderer = nodeRenderers.GetRenderer(node.GetType());
                    renderer.Draw(rendererContext, node, camera);
                }
            }
            selectionBox.Draw();
            DrawHUD(bounds);

            GraphTooltipRenderer.Draw(rendererContext, lastMousePosition);
            GraphTooltip.Clear();
        }

		void DrawEditorStats(Rect bounds) {
			var skin = rendererContext.Resources.GetResource<GUISkin>(DungeonEditorResources.GUI_STYLE_BANNER);
			var style = skin.GetStyle("label");
			style.fontSize = 20;
			style.normal.textColor = new Color(1, 1, 1, 0.2f);
			var x = 20;
			var y = bounds.height - 100;
			var textBounds = new Rect(x, y, bounds.width - 20, 70);
			style.alignment = TextAnchor.LowerLeft;
			if (camera.ZoomLevel > 1) {
				float zoomLevel = (float)System.Math.Round (camera.ZoomLevel, 1);
				GUI.Label(textBounds, "Zoom Level: " + zoomLevel.ToString("0.0"), style);
			}
		}

        void DrawBranding(Rect bounds)
        {
            var skin = rendererContext.Resources.GetResource<GUISkin>(DungeonEditorResources.GUI_STYLE_BANNER);
            var style = skin.GetStyle("label");
            style.fontSize = 40;
            style.normal.textColor = new Color(1, 1, 1, 0.1f);
            var x = 0;
            var y = bounds.height - 80;
            var textBounds = new Rect(x, y, bounds.width - 20, 70);
            style.alignment = TextAnchor.LowerRight;
            GUI.Label(textBounds, "Dungeon Architect", style);
        }

        /// <summary>
        /// Draws non-interactive textual information for the user
        /// </summary>
        /// <param name="bounds">Bounds.</param>
        void DrawHUD(Rect bounds)
        {
            // Print out the current file being edited
            {
                var style = GUI.skin.GetStyle("label");
                style.normal.textColor = new Color(1, 1, 1, 0.6f);
                var x = 10;
                var y = bounds.height - 50;
                var textBounds = new Rect(x, y, bounds.width, 40);
                style.alignment = TextAnchor.LowerLeft;
                var path = AssetDatabase.GetAssetPath(graph);
                GUI.Label(textBounds, "Editing file: " + path);
            }
        }

        /// <summary>
        /// Creates a new node in the specified screen coordinate
        /// </summary>
        /// <typeparam name="T">The type of node to created. Should be a subclass of GraphNode</typeparam>
        /// <param name="screenCoord">The screen coordinate to place the node at</param>
        /// <returns>The created graph node</returns>
        public T CreateNode<T>(Vector2 screenCoord) where T : GraphNode, new()
        {
            var node = GraphOperations.CreateNode<T>(graph);
            DungeonEditorHelper.AddToAsset(graph, node);
			var nodeScreenSize = node.Bounds.size / camera.ZoomLevel;
			var screenPosition = screenCoord - nodeScreenSize / 2;
            node.Position = camera.ScreenToWorld(screenPosition);
            BringToFront(node);
            return node;
        }

        void BringToFront(GraphNode node)
        {
            node.ZIndex = graph.TopZIndex.GetNext();
        }

        void DrawGrid(Rect bounds)
        {
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            GUI.Box(new Rect(0, 0, bounds.width, bounds.height), "");
			float GRID_CELL_SIZE = 20 * camera.ZoomLevel;
            int GRID_NUM_CELLS = 150;
            float sx = Mathf.FloorToInt(bounds.position.x / GRID_CELL_SIZE);
            float sy = Mathf.FloorToInt(bounds.position.y / GRID_CELL_SIZE);

            var sxw = sx * GRID_CELL_SIZE;
            var syw = sy * GRID_CELL_SIZE;

			float GRID_TOTAL_SIZE = GRID_CELL_SIZE * GRID_NUM_CELLS;
            for (int i = 0; i < GRID_NUM_CELLS; i++)
            {
                // Draw the vertical lines
                {
                    var idx = i + sx;
                    var p = idx * GRID_CELL_SIZE;
                    var colorAlpha = (idx % 2 == 0) ? 0.1f : 0.05f;
                    Handles.color = new Color(1, 1, 1, colorAlpha);

                    var start = camera.WorldToScreen(new Vector3(p, syw, 0));
                    var end = camera.WorldToScreen(new Vector3(p, syw + GRID_TOTAL_SIZE, 0));
                    Handles.DrawLine(start, end);
                }

                // Draw the horizontal lines
                {
                    var idx = i + sy;
                    var p = idx * GRID_CELL_SIZE;
                    var colorAlpha = (idx % 2 == 0) ? 0.1f : 0.05f;
                    Handles.color = new Color(1, 1, 1, colorAlpha);

                    var start = camera.WorldToScreen(new Vector3(sxw, p, 0));
                    var end = camera.WorldToScreen(new Vector3(sxw + GRID_TOTAL_SIZE, p, 0));
                    Handles.DrawLine(start, end);
                }
            }
        }

        /// <summary>
        /// Selects and highlights a node 
        /// </summary>
        /// <param name="nodeToSelect"></param>
        public void SelectNode(GraphNode nodeToSelect)
        {
            foreach (var node in graph.Nodes)
            {
                node.Selected = (node == nodeToSelect);
            }
        }

        /// <summary>
        /// Gets the node pin under the mouse position.   Takes the owning node's Z-order into consideration
        /// </summary>
        /// <param name="worldPosition">The world position in graph coordinates</param>
        /// <returns>The pin under the specified position. null otherwise</returns>
        public GraphPin GetPinUnderPosition(Vector2 worldPosition)
        {
            // Check if the mouse was released over a pin
            GraphNode[] sortedNodes = graph.Nodes.ToArray();
            System.Array.Sort(sortedNodes, new NodeReversedZIndexComparer());

            foreach (var node in sortedNodes)
            {
                if (node.Bounds.Contains(worldPosition))
                {
                    // Check if we are above a pin in this node
                    var pins = new List<GraphPin>();
                    pins.AddRange(node.InputPins);
                    pins.AddRange(node.OutputPins);
                    foreach (var pin in pins)
                    {
                        if (pin.ContainsPoint(worldPosition))
                        {
                            return pin;
                        }
                    }
                }
            }
            return null;
        }

        // Called when the mouse is released after dragging a link out of an existing pin
        void HandleMouseDraggedLinkReleased(Vector2 mousePositionScreen)
        {
            var mouseWorld = camera.ScreenToWorld(mousePositionScreen);
            var sourcePin = cursorDragLink.AttachedPin;

            // Check if the mouse was released over a pin
            GraphPin targetPin = null;
            GraphNode[] sortedNodes = graph.Nodes.ToArray();
            System.Array.Sort(sortedNodes, new NodeReversedZIndexComparer());

            foreach (var node in sortedNodes)
            {
                if (node.Bounds.Contains(mouseWorld))
                {
                    // Check if we are above a pin in this node
                    var pins = new List<GraphPin>();
                    pins.AddRange(node.InputPins);
                    pins.AddRange(node.OutputPins);
                    foreach (var pin in pins)
                    {
                        if (pin.ContainsPoint(mouseWorld))
                        {
                            targetPin = pin;
                            break;
                        }
                    }
                    break;
                }
            }

            if (targetPin != null)
            {
                if (sourcePin.PinType != targetPin.PinType)
                {
                    GraphPin source, target;
                    if (sourcePin.PinType == GraphPinType.Output)
                    {
                        source = sourcePin;
                        target = targetPin;
                    }
                    else
                    {
                        source = targetPin;
                        target = sourcePin;
                    }
                    if (source.Node != target.Node)
                    {
                        CreateLinkBetweenPins(source, target);
                    }
                }
            }
            else
            {
                // We stopped drag on an empty space.  Show a context menu to allow user to create nodes from this position
                contextMenu.Show(graph, sourcePin, mouseWorld);
            }
        }

        void CreateLinkBetweenPins(GraphPin outputPin, GraphPin inputPin)
        {
            if (outputPin.PinType != GraphPinType.Output && inputPin.PinType != GraphPinType.Input)
            {
                Debug.LogError("Pin type mismatch");
                return;
            }

            // Make sure they are not from the same node
            if (outputPin.Node == inputPin.Node)
            {
                Debug.LogError("Linking pins from the same node");
                return;
            }

            // Create a link
            var link = GraphOperations.CreateLink<GraphLink>(graph, outputPin, inputPin);
            if (link != null)
            {
                DungeonEditorHelper.AddToAsset(graph, link);
                graph.NotifyStateChanged();
            }
            else
            {
                Debug.Log("GraphSchema: Link not allowed");
            }
        }

        void DrawGraphNotInitializedMessage(Rect bounds)
        {
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            var area = new Rect(0, 0, bounds.width, bounds.height);
            GUI.Box(area, "");
            var style = GUI.skin.GetStyle("label");
            style.normal.textColor = new Color(1, 1, 1, 0.8f);
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(area, "Please select a theme to edit", style);
        }
    }

    /// <summary>
    /// Manages the selection box for selecting multiple objects in the graph editor
    /// </summary>
    class GraphSelectionBox
    {
        public delegate void OnSelectionPerformed(Rect boundsScreenSpace);
        public event OnSelectionPerformed SelectionPerformed;

        // The bounds of the selection box in screen space
        Rect bounds = new Rect();
        public Rect Bounds
        {
            get
            {
                return bounds;
            }
            set
            {
                bounds = value;
            }
        }

        Vector2 dragStart = new Vector2();
        int dragButton = 0;
        bool dragging = false;
        public bool Dragging
        {
            get
            {
                return dragging;
            }
        }

        /// <summary>
        /// Handles user input (mouse)
        /// </summary>
        /// <param name="e"></param>
        public void HandleInput(Event e)
        {

            switch (e.type)
            {
                case EventType.MouseDown:
                    ProcessMouseDown(e);
                    break;

                case EventType.MouseDrag:
                    ProcessMouseDrag(e);
                    break;

                case EventType.MouseUp:
                    ProcessMouseUp(e);
                    break;
            }
            // Handled captured mouse up event
            {
                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                if (GUIUtility.hotControl == controlId && Event.current.rawType == EventType.MouseUp)
                {
                    ProcessMouseUp(e);
                }
            }
        }

        void ProcessMouseDrag(Event e)
        {
            if (dragging && e.button == dragButton)
            {
                var dragEnd = e.mousePosition;
                UpdateBounds(dragStart, dragEnd);

                if (IsSelectionValid() && SelectionPerformed != null)
                {
                    SelectionPerformed(bounds);
                }
            }
        }

        void ProcessMouseDown(Event e)
        {
            if (e.button == dragButton)
            {
                dragStart = e.mousePosition;
                UpdateBounds(dragStart, dragStart);
                dragging = true;
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            }
        }

        void ProcessMouseUp(Event e)
        {
            if (e.button == dragButton && dragging)
            {
                dragging = false;
                if (IsSelectionValid() && SelectionPerformed != null)
                {
                    SelectionPerformed(bounds);
                }
                GUIUtility.hotControl = 0;
            }
        }

        public bool IsSelectionValid()
        {
            return bounds.width > 0 && bounds.height > 0;
        }

        public void Draw()
        {
            if (!dragging || !IsSelectionValid()) return;

            GUI.backgroundColor = new Color(1, 0.6f, 0, 0.6f);
            GUI.Box(bounds, "");
        }

        void UpdateBounds(Vector2 start, Vector2 end)
        {
            var x0 = Mathf.Min(start.x, end.x);
            var x1 = Mathf.Max(start.x, end.x);
            var y0 = Mathf.Min(start.y, end.y);
            var y1 = Mathf.Max(start.y, end.y);
            bounds.Set(x0, y0, x1 - x0, y1 - y0);
        }

    }

    /// <summary>
    /// Caches the keyboard state 
    /// </summary>
    class KeyboardState
    {
        Dictionary<KeyCode, bool> state = new Dictionary<KeyCode, bool>();
        bool shift;
        bool control;
        bool alt;

        public void SetState(KeyCode keyCode, bool pressed)
        {
            if (!state.ContainsKey(keyCode))
            {
                state.Add(keyCode, false);
            }
            state[keyCode] = pressed;
        }

        public void HandleInput(Event e)
        {

            if (e.type == EventType.KeyDown)
            {
                SetState(e.keyCode, true);
            }
            else if (e.type == EventType.KeyUp)
            {
                SetState(e.keyCode, false);
            }

            alt = e.alt;
            shift = e.shift;
            control = e.control || e.command;
        }

        public bool GetSate(KeyCode keyCode)
        {
            if (!state.ContainsKey(keyCode))
            {
                return false;
            }
            return state[keyCode];
        }

        public bool ControlPressed
        {
            get
            {
                return control;
            }
        }
        public bool ShiftPressed
        {
            get
            {
                return shift;
            }
        }
        public bool AltPressed
        {
            get
            {
                return alt;
            }
        }
    }

    /// <summary>
    /// Manages a link dragged out of a node with the other end following the mouse cursor
    /// </summary>
    class CursorDragLink
    {
        GraphLink link;

        GraphPin attachedPin;
        public GraphPin AttachedPin
        {
            get
            {
                return attachedPin;
            }
        }

        GraphEditor graphEditor;
        GraphPin mousePin;
        bool active = false;
        Vector2 mouseScreenPosition = new Vector2();

        public delegate void OnDraggedLinkReleased(Vector2 mousePositionScreen);
        public event OnDraggedLinkReleased DraggedLinkReleased;

        public CursorDragLink(GraphEditor graphEditor)
        {
            this.graphEditor = graphEditor;
            mousePin = ScriptableObject.CreateInstance<GraphPin>();
            mousePin.PinType = GraphPinType.Input;
            mousePin.name = "Cursor_DragPin";

            link = ScriptableObject.CreateInstance<GraphLink>();
            link.name = "Cursor_DragLink";

            mousePin.hideFlags = HideFlags.HideAndDontSave;
            link.hideFlags = HideFlags.HideAndDontSave;
        }

        public void Destroy()
        {
            UnityEngine.Object.DestroyImmediate(mousePin);
            UnityEngine.Object.DestroyImmediate(link);
            mousePin = null;
            link = null;
        }

        public void Activate(GraphPin fromPin)
        {
            active = true;
            attachedPin = fromPin;
            mousePin.PinType = (attachedPin.PinType == GraphPinType.Input) ? GraphPinType.Output : GraphPinType.Input;
            mousePin.Tangent = -attachedPin.Tangent;
            mousePin.TangentStrength = attachedPin.TangentStrength;
            AttachPinToLink(mousePin);
            AttachPinToLink(attachedPin);
        }

        public void Deactivate()
        {
            active = false;
            if (DraggedLinkReleased != null)
            {
                DraggedLinkReleased(mouseScreenPosition);
            }
        }

        public void Draw(GraphRendererContext rendererContext, GraphCamera camera)
        {
            if (!active)
            {
                return;
            }

            var mouseWorld = camera.ScreenToWorld(mouseScreenPosition);
            mousePin.Position = mouseWorld;


            GraphLinkRenderer.DrawGraphLink(rendererContext, link, camera);

            // Check the pin that comes under the mouse pin
            var targetPin = graphEditor.GetPinUnderPosition(mouseWorld);
            if (targetPin != null)
            {
                var sourcePin = attachedPin;
                var pins = new GraphPin[] { sourcePin, targetPin };
                Array.Sort(pins, new GraphPinHierarchyComparer());
                string errorMessage;
                if (!GraphSchema.CanCreateLink(pins[0], pins[1], out errorMessage))
                {
                    GraphTooltip.message = errorMessage;
                }
            }
        }

        public void HandleInput(Event e)
        {
            mouseScreenPosition = e.mousePosition;
            if (!active) return;
            int dragButton = 0;
            if (e.type == EventType.MouseUp && e.button == dragButton)
            {
                Deactivate();
            }
        }

        void AttachPinToLink(GraphPin pin)
        {
            if (pin.PinType == GraphPinType.Input)
            {
                link.Input = pin;
            }
            else
            {
                link.Output = pin;
            }
        }
    }

    /// <summary>
    /// Tracks active dungeon objects in the scene and finds ones that have the active graph being edited
    /// This is used for real-time updates on the dungeon object as the graph is modified from the editor
    /// </summary>
    class DungeonObjectTraker
    {
        Graph activeGraph;

        /// <summary>
        /// The active graph being edited by the theme graph editor
        /// </summary>
        public Graph ActiveGraph
        {
            get
            {
                return activeGraph;
            }
            set
            {
                activeGraph = value;
            }
        }

        /// <summary>
        /// Update frequency to search of the dungeon object (in seconds)
        /// </summary>
        float updateFrequence = 1.0f;

        Dungeon[] dungeons = new Dungeon[0];
        /// <summary>
        /// The dungeon objects in the scene that uses the graph tracked by this object
        /// </summary>
        public Dungeon[] Dungeons
        {
            get
            {
                return dungeons;
            }
        }

        double timeAtLastUpdate = 0;
        public void Update()
        {
            if (activeGraph == null)
            {
                return;
            }
            var frameTime = EditorApplication.timeSinceStartup;
            if (frameTime - timeAtLastUpdate > updateFrequence)
            {
                timeAtLastUpdate = frameTime;
                FindDungeonObjects();
            }
        }

        /// <summary>
        /// Finds all dungeon objects in the scene that use the theme graph tracked by this object
        /// </summary>
        void FindDungeonObjects()
        {
            dungeons = GameObject.FindObjectsOfType<Dungeon>();
        }

        /// <summary>
        /// Rebuilds the dungeons that reference the theme graphs tracked by this object
        /// </summary>
        public void RequestRebuild()
        {
            foreach (var dungeon in dungeons)
            {
                if (dungeon == null) continue;
                if (!dungeon.IsLayoutBuilt)
                {
                    dungeon.Build();
                }
                else
                {
                    // Do not rebuild the layout as it has already been built. Just reapply the theme on the existing layout
                    dungeon.ReapplyTheme();
                }
            }
        }
    }

    /// <summary>
    /// Sorts the pins based on their owning node's type 
    /// </summary>
    class GraphPinHierarchyComparer : IComparer<GraphPin>
    {
        int GetWeight(GraphNode node)
        {
            if (node is MarkerNode) return 1;
            else if (node is VisualNode) return 2;
            else if (node is MarkerEmitterNode) return 3;
            else return 4;
        }

        public int Compare(GraphPin x, GraphPin y)
        {
            if (x == null || y == null) return 0;
            var wx = GetWeight(x.Node);
            var wy = GetWeight(y.Node);

            if (wx == wy) return 0;
            return wx < wy ? -1 : 1;
        }
    }

    /// <summary>
    /// Sorts based on the node's Z-index
    /// </summary>
    class NodeZIndexComparer : IComparer<GraphNode>
    {
        public int Compare(GraphNode x, GraphNode y)
        {
            if (x == null || y == null) return 0;
            if (x.ZIndex == y.ZIndex) return 0;
            return x.ZIndex < y.ZIndex ? -1 : 1;
        }
    }

    /// <summary>
    /// Sorts based on the node's Z-index in decending order
    /// </summary>
    class NodeReversedZIndexComparer : IComparer<GraphNode>
    {
        public int Compare(GraphNode x, GraphNode y)
        {
            if (x == null || y == null) return 0;
            if (x.ZIndex == y.ZIndex) return 0;
            return x.ZIndex > y.ZIndex ? -1 : 1;
        }
    }

	
	/// <summary>
	/// Sorts based on the node's Z-index in decending order
	/// </summary>
	class NodeDeletionOrderComparer : IComparer<GraphNode>
	{
		int GetWeight(GraphNode node) {
			if (node is MarkerEmitterNode) return 0;
			if (node is VisualNode) return 1;
			if (node is MarkerNode) return 2;
			return 3;
		}

		public int Compare(GraphNode x, GraphNode y)
		{
			int wx = GetWeight(x);
			int wy = GetWeight(y); 
			if (wx == wy) return 0;
			return (wx < wy) ? -1 : 1;
		}
	}
}
