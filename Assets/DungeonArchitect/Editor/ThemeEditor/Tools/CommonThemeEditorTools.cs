using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using DungeonArchitect;
using DungeonArchitect.Editors;
using DungeonArchitect.Graphs;
using DungeonArchitect.Builders;
using DungeonArchitect.Builders.Grid;
using DungeonArchitect.Builders.Isaac;
using DungeonArchitect.Builders.SimpleCity;
using DungeonArchitect.Builders.FloorPlan;


namespace DungeonArchitect.Editors.ThemeEditorTools
{
    public class CommonThemeEditorTools
    {
        [ThemeEditorTool("Refresh Thumbnails", 100)]
        public static void RefreshThumbnails(DungeonArchitectGraphEditor editor)
        {
            if (editor.GraphEditor != null)
            {
                AssetThumbnailCache.Instance.Reset();
            }
        }

        [ThemeEditorTool("Center On Graph", 200)]
        public static void CenterOnGraph(DungeonArchitectGraphEditor editor)
        {
            if (editor.GraphEditor != null)
            {
                editor.GraphEditor.FocusCameraOnBestFit(editor.position);
            }
        }

        [ThemeEditorTool("Advanced/Recreate Node Ids", 100100)]
        public static void RecreateNodeIds(DungeonArchitectGraphEditor editor)
        {
            var confirm = EditorUtility.DisplayDialog("Recreate Node Ids?",
                    "Are you sure you want to recreate node Ids?  You should do this after cloning a theme file", "Yes", "Cancel");
            if (confirm)
            {
                DungeonEditorHelper._Advanced_RecreateGraphNodeIds();
            }
        }
    }


    public class BuilderThemeEditorTools
    {
        [ThemeEditorTool("Create Default Markers For/Grid Builder", 10100)]
        public static void CreateDefaultMarkersForGrid(DungeonArchitectGraphEditor editor)
        {
            CreateDefaultMarkersFor(editor.GraphEditor, typeof(GridDungeonBuilder));
        }

        [ThemeEditorTool("Create Default Markers For/Simple City Builder", 10200)]
        public static void CreateDefaultMarkersForSimpleCity(DungeonArchitectGraphEditor editor)
        {
            CreateDefaultMarkersFor(editor.GraphEditor, typeof(SimpleCityDungeonBuilder));
        }

        [ThemeEditorTool("Create Default Markers For/Floor Plan Builder", 10300)]
        public static void CreateDefaultMarkersForFloorPlan(DungeonArchitectGraphEditor editor)
        {
            CreateDefaultMarkersFor(editor.GraphEditor, typeof(FloorPlanBuilder));
        }

        [ThemeEditorTool("Create Default Markers For/Isaac Builder", 10400)]
        public static void CreateDefaultMarkersForIsaac(DungeonArchitectGraphEditor editor)
        {
            CreateDefaultMarkersFor(editor.GraphEditor, typeof(IsaacDungeonBuilder));
        }


        static void CreateDefaultMarkersFor(GraphEditor graphEditor, Type builderType)
        {
            // Remove unused nodes
            // Grab the names of all the markers nodes in the graph
            var markerNames = new List<string>();
            foreach (var node in graphEditor.Graph.Nodes)
            {
                if (node is MarkerNode)
                {
                    var markerNode = node as MarkerNode;
                    markerNames.Add(markerNode.Caption);
                }
            }

            var unusedMarkers = new List<string>(markerNames.ToArray());


            // Remove markers from the unused list that have child nodes attached to it
            foreach (var node in graphEditor.Graph.Nodes)
            {
                if (node is VisualNode)
                {
                    var visualNode = node as VisualNode;
                    foreach (var parentNode in visualNode.GetParentNodes())
                    {
                        if (parentNode is MarkerNode)
                        {
                            var markerNode = parentNode as MarkerNode;
                            unusedMarkers.Remove(markerNode.Caption);
                        }
                    }
                }
            }
            
            // Remove markers from the unused list that are referenced by other marker emitters
            foreach (var node in graphEditor.Graph.Nodes)
            {
                if (node is MarkerEmitterNode)
                {
                    var emitterNode = node as MarkerEmitterNode;
                    string markerName = emitterNode.Caption;
                    // this marker is referenced by an emitter.  Remove it from the unused list
                    unusedMarkers.Remove(markerName);
                }
            }

            // Remove markers from the unused list that are required by the new builder type
            var defaultMarkerRepository = new DungeonBuilderDefaultMarkers();
            var builderMarkers = defaultMarkerRepository.GetDefaultMarkers(builderType);
            foreach (var builderMarker in builderMarkers)
            {
                unusedMarkers.Remove(builderMarker);
            }

            // Remove all the unused markers
            var markerNodesToDelete = new List<MarkerNode>();
            foreach (var node in graphEditor.Graph.Nodes)
            {
                if (node is MarkerNode)
                {
                    var markerNode = node as MarkerNode;
                    if (unusedMarkers.Contains(markerNode.Caption)) {
                        markerNodesToDelete.Add(markerNode);
                    }
                }
            }

            graphEditor.DeleteNodes(markerNodesToDelete.ToArray());


            // Grab the names of all the markers nodes in the graph
            markerNames.Clear();
            foreach (var node in graphEditor.Graph.Nodes)
            {
                if (node is MarkerNode)
                {
                    var markerNode = node as MarkerNode;
                    markerNames.Add(markerNode.Caption);
                }
            }

            var markersToCreate = new List<string>();
            foreach (var builderMarker in builderMarkers)
            {
                if (!markerNames.Contains(builderMarker))
                {
                    markersToCreate.Add(builderMarker);
                }
            }

            var existingBounds = new List<Rect>();
            foreach (var node in graphEditor.Graph.Nodes)
            {
                existingBounds.Add(node.Bounds);
            }
            
            // Add the new nodes
            const int INTER_NODE_X = 200;
            const int INTER_NODE_Y = 300;
            int itemsPerRow = 5;
            int positionIndex = 0;
            int ix, iy, x, y;
            var markerNodeSize = new Vector2(120, 50);
            for (int i = 0; i < markersToCreate.Count; i++)
            {
                bool overlaps;
                int numOverlapTries = 0;
                int MAX_OVERLAP_TRIES = 100;
                do
                {
                    ix = positionIndex % itemsPerRow;
                    iy = positionIndex / itemsPerRow;
                    x = ix * INTER_NODE_X;
                    y = iy * INTER_NODE_Y;
                    positionIndex++;

                    overlaps = false;
                    var newNodeBounds = new Rect(x, y, markerNodeSize.x, markerNodeSize.y);
                    foreach (var existingBound in existingBounds)
                    {
                        if (newNodeBounds.Overlaps(existingBound))
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    numOverlapTries++;
                } while (overlaps && numOverlapTries < MAX_OVERLAP_TRIES);

                var newNode = GraphOperations.CreateNode<MarkerNode>(graphEditor.Graph);
                DungeonEditorHelper.AddToAsset(graphEditor.Graph, newNode);
                newNode.Position = new Vector2(x, y);
                newNode.Caption = markersToCreate[i];
                
            }
        }
    }
}
