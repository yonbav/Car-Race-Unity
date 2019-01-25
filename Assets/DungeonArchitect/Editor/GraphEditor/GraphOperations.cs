//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using DungeonArchitect;
using DungeonArchitect.Graphs;
using DungeonArchitect.Utils;

namespace DungeonArchitect.Editors
{
    public class GraphOperations
    {
        /// <summary>
        /// Creates a new graph node of the specified type
        /// </summary>
        /// <typeparam name="T">The type of node to create. Should be a subclass of GraphNode</typeparam>
        /// <returns>The created graph node</returns>
        public static T CreateNode<T>(Graph graph) where T : GraphNode
        {
            T node = ScriptableObject.CreateInstance<T>();
            InitializeCreatedNode(graph, node);
            return node;
        }

        /// <summary>
        /// Creates a graph node of the specified type
        /// </summary>
        /// <param name="t">The type of node to create. Should be a subclass of GraphNode</param>
        /// <returns>The created graph node</returns>
        public static GraphNode CreateNode(Graph graph, Type t)
        {
            GraphNode node = ScriptableObject.CreateInstance(t) as GraphNode;
            InitializeCreatedNode(graph, node);
            return node;
        }

        private static void InitializeCreatedNode(Graph graph, GraphNode node)
        {
			var id = System.Guid.NewGuid().ToString(); // graph.IndexCounter.GetNext();
            Undo.RecordObject(graph, "Create Node");
            node.Initialize(id, graph);
            Undo.RegisterCreatedObjectUndo(node, "Create Node");

            var pins = new List<GraphPin>();
            pins.AddRange(node.InputPins);
            pins.AddRange(node.OutputPins);
            foreach (var pin in pins)
            {
                Undo.RegisterCompleteObjectUndo(pin, "Create Node");
            }

            graph.Nodes.Add(node);
        }

        private static void DestroyEmitterNodes(Graph graph, MarkerNode markerNode)
        {
            var emitterNodes = new List<GraphNode>();
            foreach (var node in graph.Nodes)
            {
                if (node is MarkerEmitterNode)
                {
                    var emitterNode = node as MarkerEmitterNode;
                    if (emitterNode.Marker == markerNode)
                    {
                        emitterNodes.Add(emitterNode);
                    }
                }
            }

            // delete the emitter nodes
            foreach (var emitterNode in emitterNodes)
            {
                DestroyNode(emitterNode);
            }
        }

        /// <summary>
        /// Makes a deep copy of a node.  Called when a node is copy pasted
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="originalNode"></param>
        /// <returns></returns>
        public static T DuplicateNode<T>(Graph graph, T originalNode) where T : GraphNode
        {
            var node = CreateNode(graph, originalNode.GetType());
            node.CopyFrom(originalNode);
            return node as T;
        }

        /// <summary>
        /// Destroys a node and removes all references of it from the graph model. Called when the node is deleted from the editor
        /// </summary>
        /// <param name="node"></param>
        public static void DestroyNode(GraphNode node)
        {
            var graph = node.Graph;
            Undo.RegisterCompleteObjectUndo(graph, "Delete Node");

            // If this is a marker node, delete all the referenced emitter nodes as well
            if (node is MarkerNode)
            {
                DestroyEmitterNodes(graph, node as MarkerNode);
            }

            Undo.RegisterCompleteObjectUndo(node, "Delete node");
            
            // Break link connections
            BreakInputLinks(node);
            BreakOutputLinks(node);

            // De-register from the graph
            graph.Nodes.Remove(node);
            graph.MarkAsDirty();

            // Destroy the pins
            var pins = new List<GraphPin>();
            pins.AddRange(node.InputPins);
            pins.AddRange(node.OutputPins);
            foreach (var pin in pins)
            {
                Undo.DestroyObjectImmediate(pin);
            }

            graph.Nodes.Remove(node);
            Undo.DestroyObjectImmediate(node);
        }

        
        /// <summary>
        /// Destroys a node and removes all references of it from the graph model. Called when the node is deleted from the editor
        /// </summary>
        /// <param name="node"></param>
        public static void DestroyLink(GraphLink link)
        {
            var graph = link.Graph;
            Undo.RecordObject(graph, "Destroy Link");
            graph.Links.Remove(link);
            Undo.DestroyObjectImmediate(link);
        }

        /// <summary>
        /// Breaks all links connected to the input pins
        /// </summary>
        public static void BreakInputLinks(GraphNode node)
        {
            BreakLinks(node.InputPins.ToArray());
        }

        /// <summary>
        /// Breaks all links connected to the output pins
        /// </summary>
        public static void BreakOutputLinks(GraphNode node)
        {
            BreakLinks(node.OutputPins.ToArray());
        }

        private static void BreakLinks(GraphPin[] pins)
        {
            foreach (var pin in pins)
            {
                BreakLinks(pin);
            }
        }

        // Breaks all the links attached to the pin
        private static void BreakLinks(GraphPin pin)
        {
            GraphLink[] links = pin.GetConntectedLinks();
            foreach (var link in links)
            {
                DestroyLink(link);
            }
        }


        /// <summary>
        /// Creates a link of the specified type
        /// </summary>
        /// <typeparam name="T">The type of the link. Should be GraphLink or one of its subclass</typeparam>
        /// <returns></returns>
        private static T CreateLink<T>(Graph graph) where T : GraphLink
        {
            T link = ScriptableObject.CreateInstance<T>();
            link.Id = graph.IndexCounter.GetNext();
            link.Graph = graph;
            graph.Links.Add(link);
            return link;
        }

        /// <summary>
        /// Creates a graph link between the two specified pins
        /// </summary>
        /// <typeparam name="T">The type of the link. Should be GraphLink or one of its subclass</typeparam>
        /// <param name="output">The output pin from where the link originates</param>
        /// <param name="input">The input pin, where the link points to</param>
        /// <returns></returns>
        public static T CreateLink<T>(Graph graph, GraphPin output, GraphPin input) where T : GraphLink
        {
            // Make sure the pin types are correct
            if (output.PinType != GraphPinType.Output || input.PinType != GraphPinType.Input)
            {
                throw new System.ApplicationException("Invalid pin types while creating a link");
            }

            if (!GraphSchema.CanCreateLink(output, input))
            {
                return null;
            }

            // Make sure a link doesn't already exists
            foreach (T link in graph.Links)
            {
                if (link.Input == input && link.Output == output)
                {
                    return link;
                }
            }

            {
                Undo.RecordObject(graph, "Create Link");

                T link = CreateLink<T>(graph);
                link.Input = input;
                link.Output = output;

                Undo.RegisterCreatedObjectUndo(link, "Create Link");
                return link;
            }
        }



    }


    class GraphInputHandler
    {
        /// <summary>
        /// Handles user input (keyboard and mouse)
        /// </summary>
        /// <param name="e">Input event</param>
        /// <param name="camera">Graph camera to convert to / from screen to world coordinates</param>
        /// <returns>true if the input was processed, false otherwise.</returns>
        public static bool HandleNodeInput(GraphNode node, Event e, GraphCamera camera)
        {
            bool inputProcessed = false;
            if (!node.Dragging)
            {
                // let the pins handle the input first
                foreach (var pin in node.InputPins)
                {
                    if (inputProcessed) break;
                    inputProcessed |= HandlePinInput(pin, e, camera);
                }
                foreach (var pin in node.OutputPins)
                {
                    if (inputProcessed) break;
                    inputProcessed |= HandlePinInput(pin, e, camera);
                }
            }

            var mousePosition = e.mousePosition;
            var mousePositionWorld = camera.ScreenToWorld(mousePosition);
            int dragButton = 0;
            // If the pins didn't already handle the input, then let the node handle it
            if (!inputProcessed)
            {
                bool insideRect = node.Bounds.Contains(mousePositionWorld);
                if (e.type == EventType.MouseDown && insideRect && e.button == dragButton)
                {
                    node.Dragging = true;
                    inputProcessed = true;
                }
                else if (e.type == EventType.MouseUp && insideRect && e.button == dragButton)
                {
                    node.Dragging = false;
                }
            }

            if (node.Dragging && !node.Selected)
            {
                node.Dragging = false;
            }

            if (node.Dragging && e.type == EventType.MouseDrag)
            {
                inputProcessed = true;
            }

            return inputProcessed;
        }


        /// <summary>
        /// Handles the mouse input and returns true if handled
        /// </summary>
        public static bool HandlePinInput(GraphPin pin, Event e, GraphCamera camera)
        {
            var mousePosition = e.mousePosition;
            var mousePositionWorld = camera.ScreenToWorld(mousePosition);
            int buttonIdDrag = 0;
            int buttonIdDestroyLinks = 1;
            if (pin.ContainsPoint(mousePositionWorld))
            {
                if (e.type == EventType.MouseDown && e.button == buttonIdDrag)
                {
                    pin.ClickState = GraphPinMouseState.Clicked;
                    return true;
                }

                if (e.button == buttonIdDestroyLinks)
                {
                    if (e.type == EventType.MouseDown)
                    {
                        pin.RequestLinkDeletionInitiated = true;
                    }
                    else if (e.type == EventType.MouseDrag)
                    {
                        pin.RequestLinkDeletionInitiated = false;
                    }
                    else if (e.type == EventType.MouseUp)
                    {
                        if (pin.RequestLinkDeletionInitiated)
                        {
                            DestroyPinLinks(pin);
                            if (pin.Node != null && pin.Node.Graph != null)
                            {
                                pin.Node.Graph.NotifyStateChanged();
                            }
                        }
                    }
                    return true;
                }

                if (pin.ClickState != GraphPinMouseState.Clicked)
                {
                    pin.ClickState = GraphPinMouseState.Hover;
                }
            }
            else
            {
                pin.ClickState = GraphPinMouseState.None;
            }

            return false;
        }

        /// <summary>
        /// Destroys all links connected to this pin
        /// </summary>
        private static void DestroyPinLinks(GraphPin pin)
        {
            var pinLinks = pin.GetConntectedLinks();
            foreach (var link in pinLinks)
            {
                GraphOperations.DestroyLink(link);
            }

            pin.NotifyPinLinksDestroyed();
        }

    }
}