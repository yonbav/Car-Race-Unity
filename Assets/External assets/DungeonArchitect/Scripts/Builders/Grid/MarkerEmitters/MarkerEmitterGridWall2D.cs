using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect;
using DungeonArchitect.Utils;

namespace DungeonArchitect.Builders.Grid
{
    public class MarkerEmitterGridWall2D : DungeonMarkerEmitter
    {
        public enum WallPushType
        {
            WallsOutside,
            WallsInside
        }
        public WallPushType wallPushType = WallPushType.WallsOutside;
        public bool fixCorners = true;

        class Wall2DMarkerInfo
        {
            public string markerName;
            public Matrix4x4 transform;
            public IntVector gridPosition;
            public int cellId;
        }

        public override void EmitMarkers(DungeonBuilder builder)
        {
            var gridModel = builder.Model as GridDungeonModel;
            if (gridModel == null)
            {
                Debug.LogWarning("invalid builder used with this marker emitter");
                return;
            }

            var config = gridModel.Config;
            var gridSize = config.GridCellSize;
            var wall2DMarkers = new List<Wall2DMarkerInfo>();
            var occupied = new Dictionary<string, HashSet<IntVector>>();

            foreach (var prop in builder.PropSockets)
            {
                if (prop.SocketType == DungeonConstants.ST_WALL)
                {
                    var markerInfo = GetMarker2D(prop, DungeonConstants.ST_WALL2D, gridSize);
                    RegisterMarker(markerInfo, wall2DMarkers, occupied);
                }
                else if (prop.SocketType == DungeonConstants.ST_FENCE)
                {
                    var markerInfo = GetMarker2D(prop, DungeonConstants.ST_WALL2D, gridSize);
                    RegisterMarker(markerInfo, wall2DMarkers, occupied);
                }
                else if (prop.SocketType == DungeonConstants.ST_DOOR)
                {
                    var rotation = Matrix.GetRotation(ref prop.Transform);
                    var angleStep = (Mathf.RoundToInt(rotation.eulerAngles.y / 90.0f) + 4) % 4;
                    var doorMarker = DungeonConstants.ST_DOOR2D;
                    if (angleStep == 1 || angleStep == 3) { 
                        // Angle is 90 or 270
                        doorMarker = DungeonConstants.ST_DOOR2D_90;
                    }

                    var markerInfo = GetMarker2D(prop, doorMarker, gridSize);
                    RegisterMarker(markerInfo, wall2DMarkers, occupied);
                }
            }

            // fix the corners 
            if (fixCorners)
            {
                FixCornerWalls(wall2DMarkers, builder.PropSockets, occupied, gridSize);
            }

            {
                // Add Ground2d markers. These are the same as ground markers, except that are not placed over 2d walls
                AddGround2DMarkers(wall2DMarkers, occupied, builder.PropSockets, gridSize);
            }

            foreach (var markerInfo in wall2DMarkers)
            {
                builder.EmitMarker(markerInfo.markerName, markerInfo.transform, markerInfo.gridPosition, markerInfo.cellId);
            }
        }

        void AddGround2DMarkers(List<Wall2DMarkerInfo> markerList, Dictionary<string, HashSet<IntVector>> occupied, List<PropSocket> gridMarkers, Vector3 gridSize)
        {
            var walls = GetHashSet(DungeonConstants.ST_WALL2D, occupied);

            foreach (var marker in gridMarkers)
            {
                if (marker.SocketType == DungeonConstants.ST_GROUND)
                {
                    // Make sure we don't have a wall here
                    if (walls.Contains(marker.gridPosition))
                    {
                        // Contains a wall here. We don't want a 2D ground here
                        continue;
                    }

                    var wall2D = new Wall2DMarkerInfo();
                    wall2D.cellId = marker.cellId;
                    wall2D.gridPosition = marker.gridPosition;
                    wall2D.markerName = DungeonConstants.ST_GROUND2D;
                    var position = marker.gridPosition * gridSize + gridSize / 2.0f;
                    wall2D.transform = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);

                    RegisterMarker(wall2D, markerList, occupied);
                }
            }
        }


        HashSet<IntVector> GetHashSet(string name, Dictionary<string, HashSet<IntVector>> occupied)
        {
            if (occupied.ContainsKey(name))
            {
                return occupied[name];
            }
            return new HashSet<IntVector>();
        }

        void FixCornerWalls(List<Wall2DMarkerInfo> wall2DMarkers, List<PropSocket> gridMarkers, Dictionary<string, HashSet<IntVector>> occupied, Vector3 gridSize) {
            HashSet<IntVector> wall2DPositions = GetHashSet(DungeonConstants.ST_WALL2D, occupied);
            HashSet<IntVector> door2DPositions = GetHashSet(DungeonConstants.ST_DOOR2D, occupied);
            {
                HashSet<IntVector> door2DPositions90 = GetHashSet(DungeonConstants.ST_DOOR2D_90, occupied);
                foreach (var door90Pos in door2DPositions90)
                {
                    door2DPositions.Add(door90Pos);
                }
            }


            if (occupied.ContainsKey(DungeonConstants.ST_DOOR2D))
            {
                door2DPositions = occupied[DungeonConstants.ST_DOOR2D];
            }
            else
            {
                door2DPositions = new HashSet<IntVector>();
            }

            foreach (var marker in gridMarkers)
            {
                if (marker.SocketType == DungeonConstants.ST_GROUND)
                {
                    if (wallPushType == WallPushType.WallsInside)
                    {
                        var center = marker.gridPosition;
                        var insertCorner = false;
                        if (!wall2DPositions.Contains(center))
                        {
                            insertCorner = HasValidCornerNeighbors(wall2DPositions, door2DPositions, center, -1, 0, 0, 1) ||
                                HasValidCornerNeighbors(wall2DPositions, door2DPositions, center, 0, 1, 1, 0) ||
                                HasValidCornerNeighbors(wall2DPositions, door2DPositions, center, 1, 0, 0, -1) ||
                                HasValidCornerNeighbors(wall2DPositions, door2DPositions, center, 0, -1, -1, 0);
                            if (insertCorner)
                            {
                                InsertCornerMarker(marker.cellId, marker.gridPosition, gridSize, wall2DMarkers, occupied);
                            }
                        }
                    }
                    else if (wallPushType == WallPushType.WallsOutside)
                    {
                        var center = marker.gridPosition;
                        if (!wall2DPositions.Contains(center))
                        {
                            if (HasValidCornerNeighbors(wall2DPositions, door2DPositions, center, -1, 0, 0, 1))
                            {
                                var corner = center + new IntVector(-1, 0, 1);
                                InsertCornerMarker(marker.cellId, corner, gridSize, wall2DMarkers, occupied);
                            }
                            if (HasValidCornerNeighbors(wall2DPositions, door2DPositions, center, 0, 1, 1, 0))
                            {
                                var corner = center + new IntVector(1, 0, 1);
                                InsertCornerMarker(marker.cellId, corner, gridSize, wall2DMarkers, occupied);
                            }
                            if (HasValidCornerNeighbors(wall2DPositions, door2DPositions, center, 1, 0, 0, -1))
                            {
                                var corner = center + new IntVector(1, 0, -1);
                                InsertCornerMarker(marker.cellId, corner, gridSize, wall2DMarkers, occupied);
                            }
                            if (HasValidCornerNeighbors(wall2DPositions, door2DPositions, center, 0, -1, -1, 0))
                            {
                                var corner = center + new IntVector(-1, 0, -1);
                                InsertCornerMarker(marker.cellId, corner, gridSize, wall2DMarkers, occupied);
                            }
                        }
                    }
                }
            }
        }

        void InsertCornerMarker(int cellId, IntVector gridPosition, Vector3 gridSize, List<Wall2DMarkerInfo> wall2DMarkers, Dictionary<string, HashSet<IntVector>> occupied)
        {
            var markerInfo = new Wall2DMarkerInfo();
            markerInfo.markerName = DungeonConstants.ST_WALL2D;
            markerInfo.cellId = cellId;
            markerInfo.gridPosition = gridPosition;
            var position = gridPosition * gridSize + (gridSize / 2.0f);
            var transform = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            markerInfo.transform = transform;
            RegisterMarker(markerInfo, wall2DMarkers, occupied);
        }

        bool ContainsWall2D(HashSet<IntVector> walls, HashSet<IntVector> doors, IntVector position)
        {
            return walls.Contains(position) || doors.Contains(position);
        }

        bool HasValidCornerNeighbors(HashSet<IntVector> walls, HashSet<IntVector> doors, IntVector center, int dx1, int dz1, int dx2, int dz2)
        {
            var d1 = new IntVector(dx1, 0, dz1);
            var d2 = new IntVector(dx2, 0, dz2);
            var pos1 = center + d1;
            var pos2 = center + d2;
            var pos12 = center + d1 + d2;
            return ContainsWall2D(walls, doors, pos1) && ContainsWall2D(walls, doors, pos2) && !ContainsWall2D(walls, doors, pos12);
        }

        void RegisterMarker(Wall2DMarkerInfo markerInfo, List<Wall2DMarkerInfo> markerList, Dictionary<string, HashSet<IntVector>> occupied)
        {
            if (!occupied.ContainsKey(markerInfo.markerName))
            {
                occupied.Add(markerInfo.markerName, new HashSet<IntVector>());
            }
            if (occupied[markerInfo.markerName].Contains(markerInfo.gridPosition))
            {
                // Already added
                return;
            }

            occupied[markerInfo.markerName].Add(markerInfo.gridPosition);
            markerList.Add(markerInfo);
        }

        Wall2DMarkerInfo GetMarker2D(PropSocket prop, string markerName, Vector3 gridSize)
        {
            var position = Matrix.GetTranslation(ref prop.Transform);
            var x = Mathf.FloorToInt(position.x / gridSize.x);
            var z = Mathf.FloorToInt(position.z / gridSize.z);

            var rotation = Matrix.GetRotation(ref prop.Transform);
            var offset = rotation * new Vector3(0, 0, 1);

            if (wallPushType == WallPushType.WallsInside)
            {
                if (offset.z > 0.5f) z--;
                if (offset.x > 0.5f) x--;
            }
            else if (wallPushType == WallPushType.WallsOutside)
            {

                if (offset.z < -0.5f) z--;
                if (offset.x < -0.5f) x--;
            }


            var gridPosition = new IntVector(x, 0, z);
            var wall2DPosition = gridPosition * gridSize;
            wall2DPosition += gridSize / 2.0f;
            wall2DPosition.y = 0;



            var markerInfo = new Wall2DMarkerInfo();
            markerInfo.transform = Matrix4x4.TRS(wall2DPosition, Quaternion.identity, Vector3.one);
            markerInfo.gridPosition = gridPosition;
            markerInfo.cellId = prop.cellId;
            markerInfo.markerName = markerName;
            return markerInfo;
        }
    }
}
