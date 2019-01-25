//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DungeonArchitect.Utils;

namespace DungeonArchitect.Builders.Grid
{
    using PropBySocketType_t = Dictionary<string, List<PropTypeData>>;
    using PropBySocketTypeByTheme_t = Dictionary<DungeonPropDataAsset, Dictionary<string, List<PropTypeData>>>;

    /// <summary>
    /// Contains meta data about the cells.  This structure is used for caching cell
    /// information for faster lookup during and after generation of the dungeon
    /// </summary>
    public class GridCellInfo
    {
        public int CellId;
        public CellType CellType;
        public bool ContainsDoor;

        public GridCellInfo()
        {
            CellId = 0;
            CellType = CellType.Unknown;
            ContainsDoor = false;
        }
        public GridCellInfo(int pCellId, CellType pCellType)
        {
            this.CellId = pCellId;
            this.CellType = pCellType;
            this.ContainsDoor = false;
        }
    }

    /// <summary>
    /// Temporary data-structure to hold the height data of the cell node
    /// A graph is build of the dungeon layout while the heights are assigned and this
    /// node contains the cell's height information
    /// </summary>
    public class CellHeightNode
    {
        public int CellId;
        public int Height;
        public bool MarkForIncrease;
        public bool MarkForDecrease;
    };

    /// <summary>
    /// Temporary data-structure used while assigning stairs on the dungeon.
    /// </summary>
    public class StairAdjacencyQueueNode
    {
        public StairAdjacencyQueueNode(int pCellId, int pDepth)
        {
            this.cellId = pCellId;
            this.depth = pDepth;
        }
        public int cellId;
        public int depth;
    };

    /// <summary>
    /// Temporary data-structure used while assigning heights on the dungeon.
    /// </summary>
    public class CellHeightFrameInfo
    {
        public CellHeightFrameInfo(int pCellId, int pCurrentHeight)
        {
            this.CellId = pCellId;
            this.CurrentHeight = pCurrentHeight;
        }
        public int CellId;
        public int CurrentHeight;
    };

    /// <summary>
    /// Data structure to hold the adjacent cells connected to the stairs (entry / exit)
    /// </summary>
    public struct StairEdgeInfo
    {
        public StairEdgeInfo(int pCellIdA, int pCellIdB)
        {
            this.CellIdA = pCellIdA;
            this.CellIdB = pCellIdB;
        }

        public int CellIdA;
        public int CellIdB;
    };

    /// <summary>
    /// A Dungeon Builder implementation that builds a grid based dungeon.   
    /// 
    /// It is based on the awesome algorithm described here by the TinyKeep game's author
    /// https://www.reddit.com/r/gamedev/comments/1dlwc4/procedural_dungeon_generation_algorithm_explained/ 
    /// </summary>
    [ExecuteInEditMode]
    public class GridDungeonBuilder : DungeonBuilder
    {
        int _CellIdCounter = 0;
        Dictionary<int, List<StairInfo>> CellStairs
        {
            get
            {
                return gridModel.CellStairs;
            }
		}

        GridDungeonModel gridModel;
        GridDungeonConfig gridConfig;
        Dictionary<int, Dictionary<int, GridCellInfo>> GridCellInfoLookup
        {
            get { return gridModel.GridCellInfoLookup; }
        }
        GridCellInfo GetGridCellLookup(int x, int z)
        {
            return gridModel.GetGridCellLookup(x, z);
        }

        Vector3 GridToMeshScale
        {
            get
            {
                if (gridConfig != null)
                {
                    return gridConfig.GridCellSize;
                }
                return Vector3.one;
            }

        }

        void Awake()
        {
            model = GetComponent<GridDungeonModel>();
            if (model is GridDungeonModel)
            {
                gridModel = model as GridDungeonModel;
            }
            else
            {
                Debug.LogError("Invalid dungeon model type provided to grid based dungeon builder");
                return;
            }

            config = GetComponent<GridDungeonConfig>();
            if (config is GridDungeonConfig)
            {
                gridConfig = config as GridDungeonConfig;
            }
            else
            {
                Debug.LogError("Invalid dungeon config type provided to grid based dungeon builder");
                return;
            }
        }


        /// <summary>
        /// Builds the dungeon
        /// </summary>
        /// <param name="config">The dungeon configuration</param>
        /// <param name="model">The dungeon model</param>
        public override void BuildDungeon(DungeonConfig config, DungeonModel model)
        {
            base.BuildDungeon(config, model);

            if (model is GridDungeonModel)
            {
                gridModel = model as GridDungeonModel;
            }
            else
            {
                Debug.LogError("Invalid dungeon model type provided to grid based dungeon builder");
                return;
            }

            if (config is GridDungeonConfig)
            {
                gridConfig = config as GridDungeonConfig;
            }
            else
            {
                Debug.LogError("Invalid dungeon config type provided to grid based dungeon builder");
                return;
            }
            
            BuildCells();

            int seperationTries = 0;
            while (gridModel.State == DungeonModelBuildState.Separation)
            {
                Seperate(gridModel);
                seperationTries++;
            }

            ApplyBaseOffset();

            // Add cells defined by platform volumes in the world
            AddUserDefinedPlatforms();

            // Connect the rooms with delaunay triangulation to have nice evenly spaced triangles
            TriangulateRooms(gridModel);

            // Build a minimum spanning tree of the above triangulation, to avoid having lots of loops
            BuildMinimumSpanningTree();

            // Connect the rooms by converting cells between the rooms into corridors.  Also adds new corridor cells if needed for the connection
            ConnectCorridors();

            // Apply negation volumes by removing procedural geometry that lie within it
            ApplyNegationVolumes();

            // Build a lookup of adjacent tiles for later use with height and stair creation
            GenerateAdjacencyLookup();

            GenerateDungeonHeights();
			
			ConnectStairs(100);
			//ConnectStairs(60);
			ConnectStairs(50);
			ConnectStairs(0);
			ConnectStairs(-100);

            RemoveAdjacentDoors();

            gridModel.State = DungeonModelBuildState.Complete;
        }


        void Initialize()
        {
            PropSockets.Clear();
            gridModel.GridCellInfoLookup.Clear();
            CellStairs.Clear();
            gridModel.Cells.Clear();
            gridModel.DoorManager.Clear();

            _CellIdCounter = 0;
            _SocketIdCounter = 0;
        }
        
        /// <summary>
        /// builds the cells in the dungeon
        /// </summary>
        public void BuildCells()
        {
            if (gridConfig.Seed == 0)
            {
                //gridConfig.Seed = (uint)Mathf.FloorToInt(Random.value * int.MaxValue);
            }

            var cells = new List<Cell>();
            _CellIdCounter = 0;
            for (int i = 0; i < gridConfig.NumCells; i++)
            {
                var cell = BuildCell();
                cells.Add(cell);
            }

            gridModel.Config = gridConfig;
            gridModel.Cells = cells;
            gridModel.BuildCellLookup();
            gridModel.State = DungeonModelBuildState.Separation;
        }

        Cell BuildCell()
        {
            var cell = new Cell();
            cell.Id = GetNextCellId();
            var bounds = new Rectangle();
            bounds.Location = GetRandomPointInCircle(gridConfig.InitialRoomRadius);
            var baseSize = GetRandomRoomSize();
            var width = baseSize;
            var aspectRatio = 1 + (random.GetNextUniformFloat() * 2 - 1) * gridConfig.RoomAspectDelta;
            var length = Mathf.RoundToInt(width * aspectRatio);
            bounds.Size = new IntVector(width, 0, length);
            cell.Bounds = bounds;

            var area = width * length;
            if (area >= gridConfig.RoomAreaThreshold)
            {
                cell.CellType = CellType.Room;
            }

            return cell;
        }

        int GetNextCellId()
        {
            ++_CellIdCounter;
            return _CellIdCounter;
        }

        void ApplyBaseOffset()
        {
            var dungeonPosition = gameObject.transform.position;
            var dungeonGridPosF = MathUtils.Divide(dungeonPosition, gridConfig.GridCellSize);
            var dungeonGridPos = MathUtils.RoundToIntVector(dungeonGridPosF);
            foreach (var cell in gridModel.Cells)
            {
                var bounds = cell.Bounds;
                var location = bounds.Location;
                location += dungeonGridPos;
                bounds.Location = location;
                cell.Bounds = bounds;
            }
        }

        void ApplyNegationVolumes()
		{
			var dungeon = GetComponent<Dungeon>();
            var negationVolumes = GameObject.FindObjectsOfType<NegationVolume>();
            foreach (var negationVolume in negationVolumes)
            {
				if (dungeon != null && negationVolume.dungeon == dungeon) {
					ApplyNegationVolume(negationVolume);
				}
            }

            gridModel.BuildCellLookup();
        }

        void ApplyNegationVolume(NegationVolume volume)
        {

            IntVector position, scale;
            volume.GetVolumeGridTransform(out position, out scale, GridToMeshScale);
            position = position - scale / 2;
            var bounds = new Rectangle();
            bounds.Location = position;
            bounds.Size = scale;

            var cellsToRemove = new List<Cell>();
            foreach (var cell in gridModel.Cells)
            {
                bool removeCell;
                if (volume.inverse)
                {
                    // Inverse the negation and remove everything outside the volume
                    removeCell = !bounds.Contains(cell.Bounds);
                }
                else
                {
                    removeCell = cell.Bounds.IntersectsWith(bounds);
                }

                if (removeCell)
                {
                    cellsToRemove.Add(cell);
                }
            }

            foreach (var cell in cellsToRemove)
            {
                gridModel.Cells.Remove(cell);
            }
        }

        void GetCellBounds(Cell cell, ref Bounds bounds)
        {
            var gridSize = gridConfig.GridCellSize;
            var width = cell.Bounds.Width * gridSize.x;
            var length = cell.Bounds.Width * gridSize.z;
            var center = Vector3.Scale(cell.Bounds.CenterF(), gridSize);
            bounds.center = center;
            bounds.size = new Vector3(width, 2, length);
        }

        void AddUserDefinedPlatforms()
        {
            // Add geometry defined by the editor paint tool
            if (gridModel.ToolData != null && gridModel.ToolData.PaintedCells != null)
            {
                foreach (var cellPoint in gridModel.ToolData.PaintedCells)
                {
                    var cell = new Cell();
                    cell.Id = GetNextCellId();
                    cell.UserDefined = true;
                    var bounds = new Rectangle();
                    bounds.Location = cellPoint;
                    bounds.Size = new IntVector(1, 1, 1);
                    cell.Bounds = bounds;
                    cell.CellType = CellType.Corridor;
                    gridModel.Cells.Add(cell);
                }
            }

			var dungeon = GetComponent<Dungeon>();
            // Add platform volumes defined in the world
            var platformVolumes = GameObject.FindObjectsOfType<PlatformVolume>();
            foreach (var platformVolume in platformVolumes)
            {
				if (dungeon != null && platformVolume.dungeon == dungeon) {
					AddPlatformVolume(platformVolume);
				}
            }

            gridModel.BuildCellLookup();
        }

        void AddPlatformVolume(PlatformVolume platform)
        {
            var cell = new Cell();
            cell.Id = GetNextCellId();
            IntVector position, scale;
            platform.GetVolumeGridTransform(out position, out scale, GridToMeshScale);
            position = position - scale / 2;
            var bounds = new Rectangle();
            bounds.Location = position;
            bounds.Size = scale;
            cell.Bounds = bounds;
            cell.CellType = platform.cellType;
            cell.UserDefined = true;

            // Remove any cells that intersect with this cell
            var insertedCells = gridModel.Cells.ToArray();
            foreach (var insertedCell in insertedCells)
            {
                if (insertedCell.Bounds.IntersectsWith(cell.Bounds))
                {
                    gridModel.Cells.Remove(insertedCell);
                }
            }

            gridModel.Cells.Add(cell);
        }

        int GetRandomRoomSize()
        {
            float r = 0;
            while (r <= 0) r = nrandom.NextGaussianFloat(gridConfig.NormalMean, gridConfig.NormalStd);
            var roomSize = gridConfig.MinCellSize + r * (gridConfig.MaxCellSize - gridConfig.MinCellSize);
            return Mathf.RoundToInt(roomSize);
        }

        IntVector GetRandomPointInCircle(float radius)
        {
            var angle = random.GetNextUniformFloat() * Mathf.PI * 2;
            var u = random.GetNextUniformFloat() + random.GetNextUniformFloat();
            var r = (u > 1) ? 2 - u : u;
            r *= radius;
            var x = Mathf.RoundToInt(Mathf.Cos(angle) * r);
            var z = Mathf.RoundToInt(Mathf.Sin(angle) * r);
            return new IntVector(x, 0, z);
        }

        static void Shuffle(GridDungeonModel gridModel)
        {
            if (gridModel == null) return;
            var cells = gridModel.Cells;
            if (cells == null) return;
            var random = new PMRandom(gridModel.Config.Seed);
            int n = cells.Count;
            for (int i = 0; i < n; i++)
            {
                int r = i + (int)(random.GetNextUniformFloat() * (n - i));
                Cell t = cells[r];
                cells[r] = cells[i];
                cells[i] = t;
            }
        }

        static int CompareFromCenter(Cell cellA, Cell cellB)
        {
            float distA = cellA.Center.DistanceSq();
            float distB = cellB.Center.DistanceSq();
            if (distA == distB)
            {
                return 0;
            }
            return (distA < distB) ? -1 : 1;
        }

        /// <summary>
        /// Separates the cells built in the previous phase
        /// </summary>
        /// <param name="gridModel"></param>
        public static void Seperate(GridDungeonModel gridModel)
        {
            if (gridModel == null) return;
            if (gridModel.Cells == null) return;
            if (gridModel.State != DungeonModelBuildState.Separation) return;

            var cells = gridModel.Cells.ToArray();
            System.Array.Sort(cells, CompareFromCenter);

            //Shuffle(model);
            var count = gridModel.Cells.Count;
            var forces = new IntVector[count];
            for (int a = 0; a < count; a++)
            {
                forces[a] = new IntVector();
            }
            bool separated = false;
            var random = new PMRandom(gridModel.Config.Seed);
            for (int a = 0; a < count; a++)
            {
                for (int b = 0; b < count; b++)
                {
                    if (a == b) continue;
                    var c0 = cells[a].Bounds;
                    var c1 = cells[b].Bounds;
                    if (c0.IntersectsWith(c1))
                    {
                        var force = new IntVector();
                        var intersection = Rectangle.Intersect(c0, c1);
                        //var intersection = new Rectangle(c0.Location, c0.Size);
                        //intersection.Intersect(c1);

                        bool applyOnX = (intersection.Width < intersection.Length);
                        if (intersection.Width == intersection.Length)
                        {
                            applyOnX = random.GetNextUniformFloat() > 0.5;
                        }
                        if (applyOnX)
                        {
                            force.x = intersection.Width;
                            force.x *= GetForceDirectionMultiplier(c0.X, c1.X, c0.Z, c1.Z);
                        }
                        else
                        {
                            force.z = intersection.Length;
                            force.z *= GetForceDirectionMultiplier(c0.Z, c1.Z, c0.X, c1.X);
                        }

                        forces[a].x += force.x;
                        forces[a].z += force.z;

                        forces[b].x -= force.x;
                        forces[b].z -= force.z;

                        separated = true;
                    }
                }

                {
                    var cell = cells[a];
                    var location = cell.Bounds.Location;
                    location += forces[a];
                    var size = cell.Bounds.Size;
                    cell.Bounds = new Rectangle(location, size);
                }
            }

            if (!separated)
            {
                gridModel.State = DungeonModelBuildState.Triangulation;
            }
        }

        /// <summary>
        /// Triangulates the rooms identified in the previous phase
        /// This is required to connect the corridors.   
        /// Delauney triangulation is used to find nice evenly spaced triangles for good connections
        /// </summary>
        /// <param name="gridModel"></param>
        public static void TriangulateRooms(GridDungeonModel gridModel)
        {
            var nodePositions = new List<Triangulator.Geometry.Point>();

            var rooms = new List<Cell>();
            foreach (var cell in gridModel.Cells)
            {
                if (cell.CellType == CellType.Room)
                {
                    rooms.Add(cell);
                    var center = cell.Center;
                    nodePositions.Add(new Triangulator.Geometry.Point(center.x, center.z));
                }
            }
            if (rooms.Count > 2)
            {
                var triangles = Triangulator.Delauney.Triangulate(nodePositions);
                if (triangles.Count > 0)
                {
                    foreach (var triangle in triangles)
                    {
                        var c1 = rooms[triangle.p1];
                        var c2 = rooms[triangle.p2];
                        var c3 = rooms[triangle.p3];

                        ConnectCells(c1, c2);
                        ConnectCells(c2, c3);
                        ConnectCells(c3, c1);
                    }
                }
                else
                {
                    // manually connect them since we cannot successfully triangulate (happens if the room positions lie in the same line)
                    for (int i = 0; i < rooms.Count; i++)
                    {
                        int nextIndex = (i + 1) % rooms.Count;
                        var c1 = rooms[i];
                        var c2 = rooms[nextIndex];
                        ConnectCells(c1, c2);
                    }
                }
            }
			else if (rooms.Count == 2) {
				ConnectCells(rooms[0], rooms[1]);
			}
            gridModel.State = DungeonModelBuildState.SpanningTree;
        }

        static void ConnectCells(Cell c1, Cell c2)
        {
            if (!c1.ConnectedRooms.Contains(c2.Id))
            {
                c1.ConnectedRooms.Add(c2.Id);
            }
            if (!c2.ConnectedRooms.Contains(c1.Id))
            {
                c2.ConnectedRooms.Add(c1.Id);
            }
        }

        class Edge : System.IComparable<Edge>
        {
            public Edge(int cellA, int cellB, float weight)
            {
                this.cellA = cellA;
                this.cellB = cellB;
                this.weight = weight;
            }

            public int cellA;
            public int cellB;
            public float weight;


            public int CompareTo(Edge other)
            {
                if (weight == other.weight) return 0;
                return (weight < other.weight) ? -1 : 1;
            }
        }

        static List<Cell> GetRooms(List<Cell> cells)
        {
            var rooms = new List<Cell>();
            foreach (var cell in cells)
            {
                if (cell.CellType == CellType.Room)
                {
                    rooms.Add(cell);
                }
            }
            return rooms;
        }

        void AddUnique<T>(List<T> list, T value)
        {
            if (!list.Contains(value))
            {
                list.Add(value);
            }
        }

        void BuildMinimumSpanningTree()
        {
            List<Cell> rooms = GetCellsOfType(CellType.Room);
            var edgesMapped = new Dictionary<int, HashSet<int>>();

            // Generate unique edge list
            var edges = new List<Edge>();
            foreach (Cell room in rooms)
            {
                if (room == null) continue;
                foreach (int connectedRoomId in room.ConnectedRooms)
                {
                    Cell other = gridModel.GetCell(connectedRoomId);
                    if (other == null) continue;
                    float distance = GetDistance(room.Center, other.Center);
                    int id0 = room.Id;
                    int id1 = other.Id;
                    if (!edgesMapped.ContainsKey(id0) || !edgesMapped[id0].Contains(id1))
                    {
                        Edge edge = new Edge(id0, id1, distance);
                        edges.Add(edge);

                        if (!edgesMapped.ContainsKey(id0)) edgesMapped.Add(id0, new HashSet<int>());
                        if (!edgesMapped.ContainsKey(id1)) edgesMapped.Add(id1, new HashSet<int>());
                        edgesMapped[id0].Add(id1);
                        edgesMapped[id1].Add(id0);
                    }
                }
            }

            edges.Sort();

            foreach (Edge edge in edges)
            {
                Cell cell0 = gridModel.GetCell(edge.cellA);
                Cell cell1 = gridModel.GetCell(edge.cellB);
                if (cell0 != null && cell1 != null)
                {
                    cell0.FixedRoomConnections.Add(cell1.Id);
                    cell1.FixedRoomConnections.Add(cell0.Id);

                    // Check if this new edge insertion caused a loop in the MST
                    bool loop = ContainsLoop(rooms);
                    if (loop)
                    {
                        cell0.FixedRoomConnections.Remove(cell1.Id);
                        cell1.FixedRoomConnections.Remove(cell0.Id);
                    }
                }
            }

            // Add some edges from the Delauney triangulation based on a probability
            PMRandom srandom = new PMRandom(gridConfig.Seed);
            foreach (Cell room in rooms)
            {
                foreach (int otherDelauney in room.ConnectedRooms)
                {
                    if (!room.FixedRoomConnections.Contains(otherDelauney))
                    {
                        float probability = srandom.GetNextUniformFloat();
                        if (probability < gridConfig.SpanningTreeLoopProbability)
                        {
                            Cell other = gridModel.GetCell(otherDelauney);
                            if (other != null)
                            {
                                room.FixedRoomConnections.Add(otherDelauney);
                                other.FixedRoomConnections.Add(room.Id);
                            }
                        }
                    }
                }
            }


            gridModel.State = DungeonModelBuildState.Corridors;
        }

        bool ContainsLoop(List<Cell> rooms)
        {
            foreach (Cell room in rooms)
            {
                if (room != null)
                {
                    HashSet<Cell> visited = new HashSet<Cell>();
                    bool hasLoop = CheckLoop(room, null, visited);
                    if (hasLoop) return true;
                }
            }

            return false;
        }

        bool CheckLoop(Cell currentNode, Cell comingFrom, HashSet<Cell> visited)
        {
            visited.Add(currentNode);
            // check if any of the children have already been visited
            foreach (int childId in currentNode.FixedRoomConnections)
            {
                Cell child = gridModel.GetCell(childId);
                if (child == null) continue;

                if (child == comingFrom) continue;
                if (visited.Contains(child))
                {
                    return true;
                }
                bool branchHasLoop = CheckLoop(child, currentNode, visited);
                if (branchHasLoop) return true;
            }
            return false;
        }

        static float GetDistance(IntVector a, IntVector b)
        {
            var dx = a.x - b.x;
            var dy = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        List<Cell> GetCellsOfType(CellType cellType)
        {
            List<Cell> filtered = new List<Cell>();
            foreach (var cell in gridModel.Cells)
            {
                if (cell.CellType == cellType)
                {
                    filtered.Add(cell);
                }
            }
            return filtered;
        }

        void ConnectCorridors()
        {
            List<Cell> rooms = GetCellsOfType(CellType.Room);
            if (rooms.Count < 2) return;
            HashSet<int> visited = new HashSet<int>();
            Cell startingRoom = rooms[0];
            ConnectCooridorRecursive(-1, startingRoom.Id, visited);

            // TODO: Remove unused cells
            for (int i = 0; i < gridModel.Cells.Count; )
            {
                if (gridModel.Cells[i].CellType == CellType.Unknown)
                {
                    gridModel.Cells.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
            // Rebuild the cell cache list, since it has been modified
            gridModel.BuildCellLookup();

            gridModel.State = DungeonModelBuildState.Complete;
        }

        void ConnectCooridorRecursive(int incomingRoomId, int currentRoomId, HashSet<int> visited)
        {
            if (incomingRoomId >= 0)
            {
                int c0 = incomingRoomId;
                int c1 = currentRoomId;
                if (visited.Contains(HASH(c0, c1))) return;
                visited.Add(HASH(c0, c1));
                visited.Add(HASH(c1, c0));

                ConnectRooms(incomingRoomId, currentRoomId);
            }

            Cell currentRoom = gridModel.GetCell(currentRoomId);
            if (currentRoom == null)
            {
                return;
            }
            HashSet<int> children = currentRoom.FixedRoomConnections;
            foreach (int otherRoomId in children)
            {
                Cell otherRoom = gridModel.GetCell(otherRoomId);
                if (otherRoom == null) continue;
                int i0 = currentRoomId;
                int i1 = otherRoomId;
                if (!visited.Contains(HASH(i0, i1)))
                {
                    ConnectCooridorRecursive(currentRoomId, otherRoomId, visited);
                }
            }
        }

        bool AreCellsAdjacent(int cellAId, int cellBId)
        {
            Cell cellA = gridModel.GetCell(cellAId);
            Cell cellB = gridModel.GetCell(cellBId);
            if (cellA == null || cellB == null)
            {
                return false;
            }
            Rectangle intersection = Rectangle.Intersect(cellA.Bounds, cellB.Bounds);
            bool adjacent = (intersection.Width > 0 || intersection.Length > 0);
            return adjacent;
        }

        void ConnectAdjacentCells(int roomA, int roomB)
        {
            Cell cellA = gridModel.GetCell(roomA);
            Cell cellB = gridModel.GetCell(roomB);
            if (cellA == null || cellB == null)
            {
                return;
            }

            Rectangle intersection = Rectangle.Intersect(cellA.Bounds, cellB.Bounds);
            bool adjacent = (intersection.Width > 0 || intersection.Length > 0);
            if (adjacent)
            {
                IntVector doorPointA = new IntVector();
                IntVector doorPointB = new IntVector();
                doorPointA.y = cellA.Bounds.Location.y;
                doorPointB.y = cellB.Bounds.Location.y;
                if (intersection.Width > 0)
                {
                    // shares a horizontal edge
                    doorPointA.x = intersection.X + intersection.Width / 2;
                    doorPointA.z = intersection.Z - 1;

                    doorPointB.x = doorPointA.x;
                    doorPointB.z = doorPointA.z + 1;
                }
                else
                {
                    // shares a vertical edge
                    doorPointA.x = intersection.X - 1;
                    doorPointA.z = intersection.Z + intersection.Length / 2;

                    doorPointB.x = doorPointA.x + 1;
                    doorPointB.z = doorPointA.z;
                }

                // Add a door and return (no corridors needed for adjacent rooms)
                gridModel.DoorManager.CreateDoor(doorPointA, doorPointB, roomA, roomB);
            }
        }

        void ConnectRooms(int roomAId, int roomBId)
        {
            Cell roomA = gridModel.GetCell(roomAId);
            Cell roomB = gridModel.GetCell(roomBId);
            if (roomA == null || roomB == null) return;

            Rectangle intersection = Rectangle.Intersect(roomA.Bounds, roomB.Bounds);
            bool adjacent = (intersection.Width > 0 || intersection.Length > 0);
            if (adjacent)
            {
                ConnectAdjacentCells(roomAId, roomBId);
            }
            else
            {
                // Create a corridor segment as the rooms are not touching each other
                IntVector centerA = roomA.Bounds.Center();
                IntVector centerB = roomB.Bounds.Center();
                // Sweep X axis
                {
                    int sourceX = centerA.x;
                    int targetX = centerB.x;
                    int sourceZ = centerA.z;
                    int direction = (int)Mathf.Sign(targetX - sourceX);
                    for (int x = sourceX; x != targetX; x += direction)
                    {
                        int z = sourceZ;
                        // add a corridor cell
                        RegisterCorridorCell(x, z, roomAId, roomBId, true);
                        for (int i = 1; i <= gridConfig.CorridorPadding; i++)
                        {
                            RegisterCorridorCell(x, z - i, roomAId, roomBId);
                            if (gridConfig.CorridorPaddingDoubleSided)
                            {
                                RegisterCorridorCell(x, z + i, roomAId, roomBId);
                            }
                        }

                        ConnectIfRoomCorridor(x, z, x + direction, z);
                    }
                }

                // Sweep Y axis
                {
                    int sourceZ = centerA.z;
                    int targetZ = centerB.z;
                    int sourceX = centerB.x;
                    int direction = (int)Mathf.Sign(targetZ - sourceZ);
                    for (int z = sourceZ; z != targetZ; z += direction)
                    {
                        int x = sourceX;
                        // add a corridor cell
                        RegisterCorridorCell(x, z, roomAId, roomBId, true);
                        for (int i = 1; i <= gridConfig.CorridorPadding; i++)
                        {
                            RegisterCorridorCell(x - i, z, roomAId, roomBId);
                            if (gridConfig.CorridorPaddingDoubleSided)
                            {
                                RegisterCorridorCell(x + i, z, roomAId, roomBId);
                            }
                        }

                        ConnectIfRoomCorridor(x, z, x, z + direction);
                    }
                }
            }

        }

        void ConnectIfRoomCorridor(int cellAX, int cellAZ, int cellBX, int cellBZ)
        {
            var cellA = gridModel.FindCellByPosition(new IntVector(cellAX, 0, cellAZ));
            var cellB = gridModel.FindCellByPosition(new IntVector(cellBX, 0, cellBZ));
            if (cellA == null || cellB == null)
            {
                return;
            }

            int roomCount = 0;
            int corridorCount = 0;
            roomCount += (cellA.CellType == CellType.Room) ? 1 : 0;
            roomCount += (cellB.CellType == CellType.Room) ? 1 : 0;
            corridorCount += (cellA.CellType == CellType.Corridor || cellA.CellType == CellType.CorridorPadding) ? 1 : 0;
            corridorCount += (cellB.CellType == CellType.Corridor || cellB.CellType == CellType.CorridorPadding) ? 1 : 0;
            if (roomCount == 1 && corridorCount == 1)
            {
                ConnectAdjacentCells(cellA.Id, cellB.Id);
            }
        }

        bool RegisterCorridorCell(int cellX, int cellZ, int roomA, int roomB)
        {
            return RegisterCorridorCell(cellX, cellZ, roomA, roomB, /* canRegisterDoors = */ false);
        }

        bool RegisterCorridorCell(int cellX, int cellZ, int roomA, int roomB, bool canRegisterDoors)
        {
            Cell corridorCell = new Cell();
            corridorCell.Id = GetNextCellId();
            corridorCell.UserDefined = false;
            corridorCell.Bounds = new Rectangle(cellX, cellZ, 1, 1);
            corridorCell.CellType = CellType.CorridorPadding;

            Cell cellA = gridModel.GetCell(roomA);
            Cell cellB = gridModel.GetCell(roomB);
            if (cellA == null || cellB == null)
            {
                return false;
            }

            if (cellA.Bounds.Contains(corridorCell.Bounds.Location) || cellB.Bounds.Contains(corridorCell.Bounds.Location))
            {
                // ignore
                return false;
            }

            foreach (Cell cell in gridModel.Cells)
            {
                if (cell.Bounds.Contains(corridorCell.Bounds.Location))
                {
                    if (cell.Id == roomA || cell.Id == roomB)
                    {
                        // collides with inside of the room.
                        return false;
                    }

                    if (cell.CellType == CellType.Unknown)
                    {
                        // Convert this cell into a corridor 
                        cell.CellType = CellType.Corridor;

                        if (canRegisterDoors)
                        {
                            // Check if we are adjacent to to any of the room nodes
                            if (AreCellsAdjacent(cell.Id, roomA))
                            {
                                ConnectAdjacentCells(cell.Id, roomA);
                            }
                            if (AreCellsAdjacent(cell.Id, roomB))
                            {
                                ConnectAdjacentCells(cell.Id, roomB);
                            }
                        }
                    }

                    // Intersects with a cell. do not add corridor padding
                    return true;
                }
            }

            gridModel.Cells.Add(corridorCell);
            gridModel.BuildCellLookup();

            {
                var corridorX = corridorCell.Bounds.X;
                var corridorZ = corridorCell.Bounds.Z;
                if (!gridModel.GridCellInfoLookup.ContainsKey(corridorX))
                {
                    gridModel.GridCellInfoLookup.Add(corridorX, new Dictionary<int, GridCellInfo>());
                }

                var cellInfo = new GridCellInfo(corridorCell.Id, corridorCell.CellType);
                if (!gridModel.GridCellInfoLookup[corridorX].ContainsKey(corridorZ))
                {
                    gridModel.GridCellInfoLookup[corridorX].Add(corridorZ, cellInfo);
                }
                else
                {
                    gridModel.GridCellInfoLookup[corridorX][corridorZ] = cellInfo;
                }

            }

            if (canRegisterDoors)
            {
                // Check if we are adjacent to to any of the room nodes
                if (AreCellsAdjacent(corridorCell.Id, roomA))
                {
                    ConnectAdjacentCells(corridorCell.Id, roomA);
                }
                if (AreCellsAdjacent(corridorCell.Id, roomB))
                {
                    ConnectAdjacentCells(corridorCell.Id, roomB);
                }
            }

            return true;    // Indicate used
        }


        static int GetForceDirectionMultiplier(float a, float b, float a1, float b1)
        {
            if (a == b)
            {
                return (a1 < b1) ? -1 : 1;
            }
            return (a < b) ? -1 : 1;
        }

        void GenerateDungeonHeights()
        {
            // build the adjacency graph in memory
            if (gridModel.Cells.Count == 0) return;
            Dictionary<int, CellHeightNode> CellHeightNodes = new Dictionary<int, CellHeightNode>();

            HashSet<int> visited = new HashSet<int>();
            Stack<CellHeightFrameInfo> stack = new Stack<CellHeightFrameInfo>(); ;
            var initialCell = gridModel.Cells[0];
            stack.Push(new CellHeightFrameInfo(initialCell.Id, initialCell.Bounds.Location.y));
            var srandom = new PMRandom(gridConfig.Seed);

            while (stack.Count > 0)
            {
                CellHeightFrameInfo top = stack.Pop();
                if (visited.Contains(top.CellId)) continue;
                visited.Add(top.CellId);

                Cell cell = gridModel.GetCell(top.CellId);
                if (cell == null) continue;

                bool applyHeightVariation = (cell.Bounds.Size.x > 1 && cell.Bounds.Size.z > 1);
                applyHeightVariation &= (cell.CellType != CellType.Room);
                applyHeightVariation &= (cell.CellType != CellType.CorridorPadding);
                applyHeightVariation &= !cell.UserDefined;

                if (applyHeightVariation)
                {
                    float rand = srandom.GetNextUniformFloat();
                    if (rand < gridConfig.HeightVariationProbability / 2.0f)
                    {
                        top.CurrentHeight--;
                    }
                    else if (rand < gridConfig.HeightVariationProbability)
                    {
                        top.CurrentHeight++;
                    }
                }
                if (cell.UserDefined)
                {
                    top.CurrentHeight = cell.Bounds.Location.y;
                }

                CellHeightNode node = new CellHeightNode();
                node.CellId = cell.Id;
                node.Height = top.CurrentHeight;
                node.MarkForIncrease = false;
                node.MarkForDecrease = false;
                CellHeightNodes.Add(node.CellId, node);

                // Add the child nodes
                foreach (int childId in cell.AdjacentCells)
                {
                    if (visited.Contains(childId)) continue;
                    stack.Push(new CellHeightFrameInfo(childId, top.CurrentHeight));
                }
            }

            // Fix the dungeon heights
            const int FIX_MAX_TRIES = 50;	// TODO: Move to config
            int fixIterations = 0;
            while (fixIterations < FIX_MAX_TRIES && FixDungeonCellHeights(CellHeightNodes))
            {
                fixIterations++;
            }

            // Assign the calculated heights
            foreach (Cell cell in gridModel.Cells)
            {
                if (CellHeightNodes.ContainsKey(cell.Id))
                {
                    CellHeightNode node = CellHeightNodes[cell.Id];
                    var bounds = cell.Bounds;
                    var location = cell.Bounds.Location;
                    location.y = node.Height;
                    bounds.Location = location;
                    cell.Bounds = bounds;
                }
            }
        }


        bool FixDungeonCellHeights(Dictionary<int, CellHeightNode> CellHeightNodes)
        {
            bool bContinueIteration = false;
            if (gridModel.Cells.Count == 0) return bContinueIteration;

            HashSet<int> visited = new HashSet<int>();
            Stack<int> stack = new Stack<int>();
            Cell rootCell = gridModel.Cells[0];
            stack.Push(rootCell.Id);
            while (stack.Count > 0)
            {
                int cellId = stack.Pop();
                if (visited.Contains(cellId)) continue;
                visited.Add(cellId);

                Cell cell = gridModel.GetCell(cellId);
                if (cell == null) continue;

                if (!CellHeightNodes.ContainsKey(cellId)) continue;
                CellHeightNode heightNode = CellHeightNodes[cellId];

                heightNode.MarkForIncrease = false;
                heightNode.MarkForDecrease = false;

                // Check if the adjacent cells have unreachable heights
                foreach (int childId in cell.AdjacentCells)
                {
                    Cell childCell = gridModel.GetCell(childId);
                    if (childCell == null || !CellHeightNodes.ContainsKey(childId)) continue;
                    CellHeightNode childHeightNode = CellHeightNodes[childId];
                    int heightDifference = Mathf.Abs(childHeightNode.Height - heightNode.Height);
                    if (heightDifference > gridConfig.MaxAllowedStairHeight)
                    {
                        if (heightNode.Height > childHeightNode.Height)
                        {
                            heightNode.MarkForDecrease = true;
                        }
                        else
                        {
                            heightNode.MarkForIncrease = true;
                        }
                        break;
                    }
                }

                // Add the child nodes
                foreach (int childId in cell.AdjacentCells)
                {
                    if (visited.Contains(childId)) continue;
                    stack.Push(childId);
                }
            }


            bool bHeightChanged = false;
            foreach (int cellId in CellHeightNodes.Keys)
            {
                CellHeightNode heightNode = CellHeightNodes[cellId];
                if (heightNode.MarkForDecrease)
                {
                    heightNode.Height--;
                    bHeightChanged = true;
                }
                else if (heightNode.MarkForIncrease)
                {
                    heightNode.Height++;
                    bHeightChanged = true;
                }
            }

            // Iterate this function again if the height was changed in this step
            bContinueIteration = bHeightChanged;
            return bContinueIteration;
        }


        int HASH(int a, int b)
        {
            return (a << 16) + b;
        }

        void RemoveAdjacentDoors()
        {
            var doorsToRemove = new List<CellDoor>();
            foreach (var door in gridModel.DoorManager.Doors)
            {
                var cellIdA = door.AdjacentCells[0];
                var cellIdB = door.AdjacentCells[1];

                door.Enabled = false;
                // Check if it is really required to have a door here. We do this by disabling the door and 
                // check if the two adjacent cells (now with a wall instead of a door) can reach each other 
                // within the specified steps
                bool pathExists = ContainsAdjacencyPath(cellIdA, cellIdB, (int)gridConfig.DoorProximitySteps);
                if (!pathExists)
                {
                    // No path exists. We need a door here
                    door.Enabled = true;
                }
                else
                {
                    // Path exists between the doors even when the door is removed.  Remove the door for good
                    // Remove the door for good
                    doorsToRemove.Add(door);
                }
            }

            foreach (var doorToRemove in doorsToRemove)
            {
                gridModel.DoorManager.RemoveDoor(doorToRemove);
            }

        }

        bool ContainsAdjacencyPath(int cellIdA, int cellIdB, int maxDepth)
        {
            Cell cellA = gridModel.GetCell(cellIdA);
            Cell cellB = gridModel.GetCell(cellIdB);
            if (cellA == null || cellB == null)
            {
                return false;
            }
            if (cellA.CellType == CellType.Room || cellB.CellType == CellType.Room)
            {
                // Force a connection if any one is a room
                //return false;
            }

            var queue = new Queue<StairAdjacencyQueueNode>();
            var visited = new HashSet<int>();
            queue.Enqueue(new StairAdjacencyQueueNode(cellIdA, 0));

            while (queue.Count > 0)
            {
                StairAdjacencyQueueNode topNode = queue.Dequeue();
                if (topNode.depth > maxDepth) continue;

                int topId = topNode.cellId;
                if (visited.Contains(topId)) continue;
                visited.Add(topId);
                if (topId == cellIdB)
                {
                    // Reached the target cell
                    return true;
                }
                Cell top = gridModel.GetCell(topId);
                if (top == null) continue;
                foreach (int adjacentCellId in top.AdjacentCells)
                {
                    if (visited.Contains(adjacentCellId)) continue;

                    // Check if we have a valid path between these two adjacent cells 
                    // (either through same height or by a already registered stair)
                    Cell adjacentCell = gridModel.GetCell(adjacentCellId);
                    if (adjacentCell == null) continue;

                    bool pathExists = (adjacentCell.Bounds.Location.y == top.Bounds.Location.y);
                    if (!pathExists)
                    {
                        // Cells are on different heights.  Check if we have a stair connecting these cells
                        StairInfo stair = new StairInfo();
                        if (GetStair(topId, adjacentCellId, ref stair))
                        {
                            pathExists = true;
                        }
                        if (!pathExists)
                        {
                            if (GetStair(adjacentCellId, topId, ref stair))
                            {
                                pathExists = true;
                            }
                        }
                    }

					if (pathExists) {
						// If any one of the cells is a room, then make sure we have a door between them
						if (top.CellType == CellType.Room || adjacentCell.CellType == CellType.Room) {
							var containsDoor = gridModel.DoorManager.ContainsDoorBetweenCells(top.Id, adjacentCellId);
							if (!containsDoor) {
								pathExists = false;
							}
						}
					}

                    if (pathExists)
                    {
                        queue.Enqueue(new StairAdjacencyQueueNode(adjacentCellId, topNode.depth + 1));
                    }
                }
            }
            return false;
        }


		void AddCorridorPadding(int x, int y, int z) {
			Cell padding = new Cell();
			padding.Id = GetNextCellId();
			padding.UserDefined = false;
			
			var bounds = new Rectangle(x, z, 1, 1);
			bounds.SetY(y);
			padding.Bounds = bounds;
			padding.CellType = CellType.CorridorPadding;
			
			gridModel.Cells.Add(padding);
		}
        
        class StairConnectionWeight {
	        public StairConnectionWeight(int position, int weight)  {
                this.position = position;
                this.weight = weight;
            }
	        public int position;
            public int weight;

        }

        class StairConnectionWeightComparer : IComparer<StairConnectionWeight>
        {
            public int Compare(StairConnectionWeight x, StairConnectionWeight y)
            {
                if (x.weight == y.weight) return 0;
                return (x.weight < y.weight) ? 1 : -1;
            }
        }

        void ConnectStairs(int WeightThreshold)
        {
            if (gridModel.Cells.Count == 0) return;
            Stack<StairEdgeInfo> stack = new Stack<StairEdgeInfo>();
			HashSet<int> visited = new HashSet<int>();
			HashSet<int> islandsVisited = new HashSet<int>();

			for (int i = 0; i < gridModel.Cells.Count; i++) {
				var startCell = gridModel.Cells[i];
				if (islandsVisited.Contains (startCell.Id)) {
					continue;
				}
				stack.Push(new StairEdgeInfo(-1, startCell.Id));
	            while (stack.Count > 0)
	            {
	                StairEdgeInfo top = stack.Pop();
	                if (top.CellIdA >= 0)
	                {
	                    int hash1 = HASH(top.CellIdA, top.CellIdB);
	                    int hash2 = HASH(top.CellIdB, top.CellIdA);
	                    if (visited.Contains(hash1) || visited.Contains(hash2))
	                    {
	                        // Already processed
	                        continue;
						}
						// Mark as processed
						visited.Add(hash1);
						visited.Add(hash2);

						// Mark the island as processed
						islandsVisited.Add(top.CellIdA);
						islandsVisited.Add(top.CellIdB);

	                    // Check if it is really required to place a stair here.  There might be other paths nearby to this cell
	                    bool pathExists = ContainsAdjacencyPath(top.CellIdA, top.CellIdB, (int)gridConfig.StairConnectionTollerance);
	                    if (!pathExists)
	                    {
	                        // Process the edge
	                        Cell cellA = gridModel.GetCell(top.CellIdA);
	                        Cell cellB = gridModel.GetCell(top.CellIdB);
	                        if (cellA == null || cellB == null) continue;
	                        if (cellA.Bounds.Location.y != cellB.Bounds.Location.y)
	                        {
	                            // Find the adjacent line
	                            Rectangle intersection = Rectangle.Intersect(cellA.Bounds, cellB.Bounds);
	                            if (intersection.Size.x > 0)
	                            {
	                                bool cellAAbove = (cellA.Bounds.Location.y > cellB.Bounds.Location.y);
	                                Cell stairOwner = (cellAAbove ? cellB : cellA);
	                                Cell stairConnectedTo = (!cellAAbove ? cellB : cellA);

	                                if (ContainsStair(stairOwner.Id, stairConnectedTo.Id))
	                                {
	                                    // Stair already exists here. Move to the next one
	                                    continue;
	                                }

	                                bool cellOwnerOnLeft = (stairOwner.Bounds.Center().z < intersection.Location.z);
	                                int validX = intersection.Location.x;
									//int preferedLocation = MathUtils.INVALID_LOCATION;

	                                int validZ = intersection.Location.z;
	                                if (cellOwnerOnLeft) validZ--;

	                                var StairConnectionCandidates = new List<StairConnectionWeight>();
	                                for (validX = intersection.Location.x; validX < intersection.Location.x + intersection.Size.x; validX++)
	                                {
	                                    var currentPointInfo = gridModel.GetGridCellLookup(validX, validZ);
									    if (stairOwner.CellType == CellType.Room || stairConnectedTo.CellType == CellType.Room) {
										    // Make sure the stair is on a door cell
										    GridCellInfo stairCellInfo = gridModel.GetGridCellLookup(validX, validZ);
										    if (!stairCellInfo.ContainsDoor) {
											    // Stair not connected to a door. Probably trying to attach itself to a room wall. ignore
											    continue;
										    }

										    // We have a door here.  A stair case is a must, but first make sure we have a door between these two cells 
										    bool hasDoor = gridModel.DoorManager.ContainsDoorBetweenCells(stairOwner.Id, stairConnectedTo.Id);
										    if (!hasDoor) continue;

										    // Check again in more detail
										    var tz1 = validZ;
										    var tz2 = validZ - 1;
										    if (cellOwnerOnLeft) {
											    tz2 = validZ + 1;
										    }

										    hasDoor = gridModel.DoorManager.ContainsDoor(validX, tz1, validX, tz2);
										    if (hasDoor) {
											    StairConnectionCandidates.Add(new StairConnectionWeight(validX, 100));
											    break;
										    }
									    }
									    else {	// Both the cells are non-rooms (corridors)
										    int weight = 0;

										    GridCellInfo cellInfo0 = gridModel.GetGridCellLookup(validX, validZ - 1);
										    GridCellInfo cellInfo1 = gridModel.GetGridCellLookup(validX, validZ + 1);
										    weight += (cellInfo0.CellType != CellType.Unknown) ? 10 : 0;
										    weight += (cellInfo1.CellType != CellType.Unknown) ? 10 : 0;

											int adjacentOwnerZ = cellOwnerOnLeft ? (validZ - 1) : (validZ + 1);
											int adjacentConnectedToZ = !cellOwnerOnLeft ? (validZ - 1) : (validZ + 1);
										    if (currentPointInfo.ContainsDoor) {
											    // Increase the weight if we connect into a door
											    int adjacentZ = cellOwnerOnLeft ? (validZ - 1) : (validZ + 1);
											    bool ownerOnDoor = gridModel.DoorManager.ContainsDoor(validX, validZ, validX, adjacentZ);
											    if (ownerOnDoor) {
												    // Connect to this
												    weight += 100;
											    }
											    else {
												    // Add a penalty if we are creating a stair blocking a door entry/exit
												    weight -= 100;
											    }
										    }
										    else {
											    // Make sure we don't connect to a wall
												GridCellInfo adjacentOwnerCellInfo = gridModel.GetGridCellLookup(validX, adjacentOwnerZ);
											    if (adjacentOwnerCellInfo.CellType == CellType.Room) {
												    // We are connecting to a wall. Add a penalty
												    weight -= 100;
											    }
										    }

										    // Check the side of the stairs to see if we are not blocking a stair entry / exit
										    if (gridModel.ContainsStairAtLocation(validX - 1, validZ)) {
											    weight -= 60;
										    }
	                                        if (gridModel.ContainsStairAtLocation(validX + 1, validZ))
	                                        {
											    weight -= 60;
										    }

											for (int dx = -1; dx <= 1; dx++) {
												var adjacentStair = gridModel.GetStairAtLocation(validX + dx, adjacentOwnerZ);
												if (adjacentStair != null) {
													var currentRotation = Quaternion.AngleAxis(cellOwnerOnLeft ? -90 : 90, new Vector3(0, 1, 0));
													var angle = Quaternion.Angle(adjacentStair.Rotation, currentRotation);
													if (dx == 0) {
														// If we have a stair case in a perpendicular direction right near the owner, add a penalty
														var angleDelta = Mathf.Abs (Mathf.Abs(angle) - 90);
														if (angleDelta < 2) {
															weight -= 100;
														}
													} else {
														var angleDelta = Mathf.Abs (Mathf.Abs(angle) - 180);
														if (angleDelta < 2) {
															weight -= 60;
														}
													}
												}
											}
											
											// If we connect to another stair with the same angle, then increase the weight
											if (gridModel.ContainsStairAtLocation(validX, adjacentConnectedToZ)) {
												var adjacentStair = gridModel.GetStairAtLocation(validX, adjacentConnectedToZ);
												if (adjacentStair != null) {
													var currentRotation = Quaternion.AngleAxis(cellOwnerOnLeft ? -90 : 90, new Vector3(0, 1, 0));
													var angle = Quaternion.Angle(adjacentStair.Rotation, currentRotation);
													var angleDelta = Mathf.Abs(angle) % 360;
													if (angleDelta < 2) {
														weight += 50;
													}
													else {
														weight -= 50;
													}
												}
											}
											

											// check if the entry of the stair is not in a different height
											{
												var adjacentEntryCellInfo = gridModel.GetGridCellLookup(validX, adjacentOwnerZ);
												if (adjacentEntryCellInfo.CellType != CellType.Unknown) {
													var adjacentEntryCell = gridModel.GetCell(adjacentEntryCellInfo.CellId);
													if (stairOwner.Bounds.Location.y != adjacentEntryCell.Bounds.Location.y) {
														// The entry is in a different height. Check if we have a stair here
														if (!gridModel.ContainsStair(validX, adjacentOwnerZ)) {
															//Add a penalty
															weight -= 10;
														}
													}
												}
											}

										    StairConnectionCandidates.Add(new StairConnectionWeight(validX, weight));
									    }
	                                }


	                                // Create a stair if necessary
	                                if (StairConnectionCandidates.Count > 0)
	                                {
	                                    StairConnectionCandidates.Sort(new StairConnectionWeightComparer());
	                                    var candidate = StairConnectionCandidates[0];
	                                    if (candidate.weight < WeightThreshold)
	                                    {
	                                        continue;
	                                    }
	                                    validX = candidate.position;

										int stairY = stairOwner.Bounds.Location.y;
										var paddingOffset = (stairOwner.Bounds.Z > stairConnectedTo.Bounds.Z) ? 1 : -1;
										// Add a corridor padding here
										//AddCorridorPadding(validX, stairY, validZ - 1);
										for (int dx = -1; dx <= 1; dx++) {
											bool requiresPadding = false;
											if (dx == 0) {
												requiresPadding = true;
											} else {
												var cellInfo = GetGridCellLookup(validX + dx, validZ);
												if (cellInfo.CellType != CellType.Unknown) {
													requiresPadding = true;
												}
											}
											
											if (requiresPadding) {
												var paddingInfo = GetGridCellLookup(validX + dx, validZ + paddingOffset);
												if (paddingInfo.CellType == CellType.Unknown) {
													AddCorridorPadding(validX + dx, stairY, validZ + paddingOffset);
												}
											}
										}
										gridModel.BuildCellLookup();
										gridModel.BuildSpatialCellLookup();
										GenerateAdjacencyLookup();
									}
	                                else
	                                {
	                                    continue;
	                                }

	                                float validY = stairOwner.Bounds.Location.y;
	                                Vector3 StairLocation = new Vector3(validX, validY, validZ);
	                                StairLocation += new Vector3(0.5f, 0, 0.5f);
	                                StairLocation = Vector3.Scale(StairLocation, GridToMeshScale);

	                                Quaternion StairRotation = Quaternion.AngleAxis(cellOwnerOnLeft ? -90 : 90, new Vector3(0, 1, 0));

	                                if (!CellStairs.ContainsKey(stairOwner.Id))
	                                {
	                                    CellStairs.Add(stairOwner.Id, new List<StairInfo>());
	                                }
	                                StairInfo Stair = new StairInfo();
	                                Stair.OwnerCell = stairOwner.Id;
	                                Stair.ConnectedToCell = stairConnectedTo.Id;
	                                Stair.Position = StairLocation;
	                                Stair.IPosition = new IntVector(validX, (int)validY, validZ);
	                                Stair.Rotation = StairRotation;
	                                if (!gridModel.ContainsStairAtLocation(validX, validZ))
	                                {
	                                    CellStairs[stairOwner.Id].Add(Stair);
	                                }
	                            }
	                            else if (intersection.Size.z > 0)
	                            {
	                                bool cellAAbove = (cellA.Bounds.Location.y > cellB.Bounds.Location.y);

	                                Cell stairOwner = (cellAAbove ? cellB : cellA);
	                                Cell stairConnectedTo = (!cellAAbove ? cellB : cellA);

	                                if (ContainsStair(stairOwner.Id, stairConnectedTo.Id))
	                                {
	                                    // Stair already exists here. Move to the next one
	                                    continue;
	                                }

	                                bool cellOwnerOnLeft = (stairOwner.Bounds.Center().x < intersection.Location.x);

	                                int validX = intersection.Location.x;
	                                if (cellOwnerOnLeft) validX--;

									int validZ = intersection.Location.z;

	                                var StairConnectionCandidates = new List<StairConnectionWeight>();
	                                for (validZ = intersection.Location.z; validZ < intersection.Location.z + intersection.Size.z; validZ++)
	                                {
	                                    var currentPointInfo = gridModel.GetGridCellLookup(validX, validZ);
									    if (stairOwner.CellType == CellType.Room || stairConnectedTo.CellType == CellType.Room) {
										    // Make sure the stair is on a door cell
										    GridCellInfo stairCellInfo = gridModel.GetGridCellLookup(validX, validZ);
										    if (!stairCellInfo.ContainsDoor) {
											    // Stair not connected to a door. Probably trying to attach itself to a room wall. ignore
											    continue;
										    }

										    // We have a door here.  A stair case is a must, but first make sure we have a door between these two cells 
										    bool hasDoor = gridModel.DoorManager.ContainsDoorBetweenCells(stairOwner.Id, stairConnectedTo.Id);
										    if (!hasDoor) continue;

										    // Check again in more detail
										    var tx1 = validX;
										    var tx2 = validX - 1;
										    if (cellOwnerOnLeft) {
											    tx2 = validX + 1;
										    }

										    hasDoor = gridModel.DoorManager.ContainsDoor(tx1, validZ, tx2, validZ);
										    if (hasDoor) {
											    StairConnectionCandidates.Add(new StairConnectionWeight(validZ, 100));
											    break;
										    }
									    }
									    else {	// Both the cells are non-rooms (corridors)
										    int weight = 0;

										    GridCellInfo cellInfo0 = gridModel.GetGridCellLookup(validX - 1, validZ);
										    GridCellInfo cellInfo1 = gridModel.GetGridCellLookup(validX + 1, validZ);
										    weight += (cellInfo0.CellType != CellType.Unknown) ? 10 : 0;
										    weight += (cellInfo1.CellType != CellType.Unknown) ? 10 : 0;
											
											int adjacentOwnerX = cellOwnerOnLeft ? (validX - 1) : (validX + 1);
											int adjacentConnectedToX = !cellOwnerOnLeft ? (validX - 1) : (validX + 1);
											if (currentPointInfo.ContainsDoor) {
											    // Increase the weight if we connect into a door
												bool ownerOnDoor = gridModel.DoorManager.ContainsDoor(validX, validZ, adjacentOwnerX, validZ);
											    if (ownerOnDoor) {
												    // Connect to this
												    weight += 100;
											    }
											    else {
												    // Add a penalty if we are creating a stair blocking a door entry/exit
												    weight -= 100;
											    }
										    }
										    else {
											    // Make sure we don't connect to a wall
											    int adjacentX = cellOwnerOnLeft ? (validX - 1) : (validX + 1);
											    GridCellInfo adjacentOwnerCellInfo = gridModel.GetGridCellLookup(adjacentX, validZ);
											    if (adjacentOwnerCellInfo.CellType == CellType.Room) {
												    // We are connecting to a wall. Add a penalty
												    weight -= 100;
											    }
										    }

										    // Check the side of the stairs to see if we are not blocking a stair entry / exit
										    if (gridModel.ContainsStairAtLocation(validX, validZ - 1)) {
											    weight -= 60;
										    }
										    if (gridModel.ContainsStairAtLocation(validX, validZ + 1)) {
											    weight -= 60;
										    }

											// If we have a stair coming out in the opposite direction, near the entry of the stair, add a penalty
											for (int dz = -1; dz <= 1; dz++) {
												var adjacentStair = gridModel.GetStairAtLocation(adjacentOwnerX, validZ + dz);
												if (adjacentStair != null) {
													var currentRotation = Quaternion.AngleAxis(cellOwnerOnLeft ? 0 : 180, new Vector3(0, 1, 0));
													var angle = Quaternion.Angle(adjacentStair.Rotation, currentRotation);
													if (dz == 0) {
														// If we have a stair case in a perpendicular direction right near the owner, add a penalty
														var angleDelta = Mathf.Abs (Mathf.Abs(angle) - 90);
														if (angleDelta < 2) {
															weight -= 100;
														}
													} else {
														var angleDelta = Mathf.Abs (Mathf.Abs(angle) - 180);
														if (angleDelta < 2) {
															weight -= 60;
														}
													}
												}
											}

											// If we connect to another stair with the same angle, the increase the weight
											if (gridModel.ContainsStairAtLocation(adjacentConnectedToX, validZ)) {
												var adjacentStair = gridModel.GetStairAtLocation(adjacentConnectedToX, validZ);
												if (adjacentStair != null) {
													var currentRotation = Quaternion.AngleAxis(cellOwnerOnLeft ? 0 : 180, new Vector3(0, 1, 0));
													var angle = Quaternion.Angle(adjacentStair.Rotation, currentRotation);
													var angleDelta = Mathf.Abs(angle) % 360;
													if (angleDelta < 2) {
														weight += 50;
													}
													else {
														weight -= 50;
													}
												}
											}


											// check if the entry of the stair is not in a different height
											{
												var adjacentEntryCellInfo = gridModel.GetGridCellLookup(adjacentOwnerX, validZ);
												if (adjacentEntryCellInfo.CellType != CellType.Unknown) {
													var adjacentEntryCell = gridModel.GetCell(adjacentEntryCellInfo.CellId);
													if (stairOwner.Bounds.Location.y != adjacentEntryCell.Bounds.Location.y) {
														// The entry is in a different height. Check if we have a stair here
														if (!gridModel.ContainsStair(adjacentOwnerX, validZ)) {
															//Add a penalty
															weight -= 10;
														}
													}
												}
											}

										    StairConnectionCandidates.Add(new StairConnectionWeight(validZ, weight));
									    }
	                                }

	                                // Connect the stairs if necessary
	                                if (StairConnectionCandidates.Count > 0)
	                                {
	                                    StairConnectionCandidates.Sort(new StairConnectionWeightComparer());
	                                    StairConnectionWeight candidate = StairConnectionCandidates[0];
	                                    if (candidate.weight < WeightThreshold)
	                                    {
	                                        continue;
	                                    }
	                                    validZ = candidate.position;

										int stairY = stairOwner.Bounds.Location.y;
										var paddingOffset = (stairOwner.Bounds.X > stairConnectedTo.Bounds.X) ? 1 : -1;
										// Add a corridor padding here
										for (int dz = -1; dz <= 1; dz++) {
											bool requiresPadding = false;
											if (dz == 0) {
												requiresPadding = true;
											} else {
												var cellInfo = GetGridCellLookup(validX, validZ + dz);
												if (cellInfo.CellType != CellType.Unknown) {
													requiresPadding = true;
												}
											}
											
											if (requiresPadding) {
												var paddingInfo = GetGridCellLookup(validX + paddingOffset, validZ + dz);
												if (paddingInfo.CellType == CellType.Unknown) {
													AddCorridorPadding(validX + paddingOffset, stairY, validZ + dz);
												}
											}
										}
										gridModel.BuildCellLookup();
										gridModel.BuildSpatialCellLookup();
										GenerateAdjacencyLookup();
									}
	                                else
	                                {
	                                    continue;
	                                }

	                                float validY = stairOwner.Bounds.Location.y;
	                                Vector3 StairLocation = new Vector3(validX, validY, validZ);
	                                StairLocation += new Vector3(0.5f, 0, 0.5f);
	                                StairLocation = Vector3.Scale(StairLocation, GridToMeshScale);

	                                Quaternion StairRotation = Quaternion.AngleAxis(cellOwnerOnLeft ? 0 : 180, new Vector3(0, 1, 0));

	                                if (!CellStairs.ContainsKey(stairOwner.Id))
	                                {
	                                    CellStairs.Add(stairOwner.Id, new List<StairInfo>());
	                                }
	                                StairInfo Stair = new StairInfo();
	                                Stair.OwnerCell = stairOwner.Id;
	                                Stair.ConnectedToCell = stairConnectedTo.Id;
	                                Stair.Position = StairLocation;
	                                Stair.IPosition = new IntVector(validX, (int)validY, validZ);
	                                Stair.Rotation = StairRotation;
	                                if (!gridModel.ContainsStairAtLocation(validX, validZ))
	                                {
	                                    CellStairs[stairOwner.Id].Add(Stair);
	                                }
	                            }
	                        }
	                    }
	                }

	                // Move to the next adjacent nodes
	                {
	                    Cell cellB = gridModel.GetCell(top.CellIdB);
	                    if (cellB == null) continue;
	                    foreach (int adjacentCell in cellB.AdjacentCells)
	                    {
	                        int hash1 = HASH(cellB.Id, adjacentCell);
	                        int hash2 = HASH(adjacentCell, cellB.Id);
	                        if (visited.Contains(hash1) || visited.Contains(hash2)) continue;
	                        StairEdgeInfo edge = new StairEdgeInfo(top.CellIdB, adjacentCell);
	                        stack.Push(edge);
	                    }
	                }
	            }
			}
        }

        public override void OnVolumePositionModified(Volume volume, out IntVector newPositionOnGrid, out IntVector newSizeOnGrid)
        {
			volume.GetVolumeGridTransform(out newPositionOnGrid, out newSizeOnGrid, GridToMeshScale);
        }

        void CheckAndMarkAdjacent(Cell cell, int otherCellX, int otherCellZ)
        {
            GridCellInfo info = gridModel.GetGridCellLookup(otherCellX, otherCellZ);
            if (info.CellId == cell.Id) return;
            Cell otherCell = gridModel.GetCell(info.CellId);
            if (otherCell == null) return;
            if (otherCell.CellType == CellType.Unknown || cell.CellType == CellType.Unknown) return;

            // Mark the two cells as adjacent
            cell.AdjacentCells.Add(otherCell.Id);
            otherCell.AdjacentCells.Add(cell.Id);
        }

        public void GenerateAdjacencyLookup()
        {
            // Cache the cell types based on their positions
            gridModel.BuildSpatialCellLookup();

            // Create cell adjacency list
            foreach (var cell in gridModel.Cells)
            {
                if (cell.CellType == CellType.Unknown) continue;
                int SizeX = cell.Bounds.Size.x;
                int SizeZ = cell.Bounds.Size.z;
                for (int dx = 0; dx < SizeX; dx++)
                {
                    for (int dz = 0; dz < SizeZ; dz++)
                    {
                        if (dx >= 0 && dx < SizeX - 1 && dz >= 0 && dz < SizeZ - 1)
                        {
                            // Ignore the cells in the middle
                            continue;
                        }

                        int x = cell.Bounds.Location.x + dx;
                        int z = cell.Bounds.Location.z + dz;
                        CheckAndMarkAdjacent(cell, x + 1, z);
                        CheckAndMarkAdjacent(cell, x, z + 1);
                    }
                }
            }

            // Cache the positions of the doors in the grid
            foreach (CellDoor Door in gridModel.DoorManager.Doors)
            {
                int x0 = Door.AdjacentTiles[0].x;
                int z0 = Door.AdjacentTiles[0].z;
                int x1 = Door.AdjacentTiles[1].x;
                int z1 = Door.AdjacentTiles[1].z;
                if (GridCellInfoLookup.ContainsKey(x0) && GridCellInfoLookup[x0].ContainsKey(z0)) GridCellInfoLookup[x0][z0].ContainsDoor = true;
                if (GridCellInfoLookup.ContainsKey(x1) && GridCellInfoLookup[x1].ContainsKey(z1)) GridCellInfoLookup[x1][z1].ContainsDoor = true;
            }

        }

        public override void EmitMarkers()
        {
			base.EmitMarkers ();
            if (gridModel.Cells.Count == 0) return;

            // Populate the prop sockets all over the map
            foreach (var cell in gridModel.Cells)
            {
                switch (cell.CellType)
                {
                    case CellType.Room:
                        BuildMesh_Room(cell);
                        BuildMesh_RoomDecoration(cell);
                        break;

                    case CellType.Corridor:
                    case CellType.CorridorPadding:
                        BuildMesh_Corridor(cell);
                        break;
                }
                BuildMesh_Stairs(cell);
            }

            RemoveOverlappingMarkers();
            ProcessMarkerOverrideVolumes();
        }


        int GetElevation(Cell baseCell, int x, int z, out int OutYOffset)
        {
            OutYOffset = 0;
            GridCellInfo info = GetGridCellLookup(x, z);
            int elevation = gridConfig.FloorHeight;
            if (info.CellType == CellType.Unknown) return elevation;
            Cell otherCell = gridModel.GetCell(info.CellId);
            if (otherCell == null)
            {
                return elevation;
            }
            OutYOffset = otherCell.Bounds.Location.y - baseCell.Bounds.Location.y;
            elevation = Mathf.Max(elevation, Mathf.Abs(OutYOffset));
            OutYOffset = Mathf.Max(0, OutYOffset);

            //return FMath::Max(elevation, baseCell.Bounds.Location.Z - otherCell->Bounds.Location.Z);
            return elevation;
        }

        void OffsetTransformY(float Y, ref Matrix4x4 OutTransform)
        {
            Vector3 Location = Matrix.GetTranslation(ref OutTransform);
            Location.y += Y;
            Matrix.SetTranslation(ref OutTransform, Location);
        }

        bool GetStair(int ownerCell, int connectedToCell, ref StairInfo outStair)
        {
            if (CellStairs.ContainsKey(ownerCell))
            {
                foreach (StairInfo stair in CellStairs[ownerCell])
                {
                    if (stair.ConnectedToCell == connectedToCell)
                    {
                        outStair = stair;
                        return true;
                    }
                }
            }
            return false;
        }

        bool ContainsStair(int ownerCellId, int connectedToCellId)
        {
            if (!gridModel.CellStairs.ContainsKey(ownerCellId))
            {
                return false;
            }

            foreach (var stairInfo in gridModel.CellStairs[ownerCellId])
            {
                if (stairInfo.ConnectedToCell == connectedToCellId)
                {
                    return true;
                }
            }
            return false;
        }

        bool ContainsStair(Cell baseCell, int x, int z)
        {
            GridCellInfo info = GetGridCellLookup(x, z);
            if (info.CellType == CellType.Unknown) return false;

            Cell cell = gridModel.GetCell(info.CellId);
            if (cell == null) return false;

            StairInfo stair = new StairInfo();
            if (GetStair(cell.Id, baseCell.Id, ref stair))
            {
                Vector3 IPosition = MathUtils.Divide(stair.Position, GridToMeshScale);
                int ix = Mathf.FloorToInt(IPosition.x);
                int iz = Mathf.FloorToInt(IPosition.z);
                if (ix == x && iz == z)
                {
                    return true;
                }
            }
            return false;
        }

        public bool V3Equal(Vector3 a, Vector3 b)
        {
            return Vector3.SqrMagnitude(a - b) < 1e-6f;
        }
        
        bool CanDrawFence(Cell baseCell, int x, int z, out bool isElevatedFence, out bool drawPillar, out int elevationHeight)
        {
            GridCellInfo info = GetGridCellLookup(x, z);
            isElevatedFence = false;
            drawPillar = false;
            elevationHeight = 0;
            if (info.CellType == CellType.Unknown)
            {
                isElevatedFence = false;
                drawPillar = true;
                return true;
            }
            Cell otherCell = gridModel.GetCell(info.CellId);
            if (otherCell != null && otherCell.Bounds.Location.y < baseCell.Bounds.Location.y)
            {
                isElevatedFence = true;
                elevationHeight = baseCell.Bounds.Location.y - otherCell.Bounds.Location.y;
                drawPillar = true;
                // Make sure we dont have a stair between the two cells
                if (!ContainsStair(baseCell, x, z))
                {
                    return true;
                }
            }
            return false;
        }


        bool ShouldMakeDoor(int x1, int z1, int x2, int z2)
        {
            var cellLookupA = GetGridCellLookup(x1, z1);
            var cellLookupB = GetGridCellLookup(x2, z2);
            bool makeDoor = (cellLookupA.ContainsDoor && cellLookupB.ContainsDoor);
            if (makeDoor)
            {
                // Now perform an exaustive search to see if a door really exists between them
                makeDoor = gridModel.DoorManager.ContainsDoorBetweenCells(cellLookupA.CellId, cellLookupB.CellId);
            }
            return makeDoor;
        }

        void BuildMesh_Room(Cell cell)
        {
            BuildMesh_Floor(cell);


            Vector3 HalfWallOffset = Vector3.Scale(GridToMeshScale, new Vector3(0, -1, 0));
            // Build the room walls

            IntVector basePosition = cell.Bounds.Location;
            int elevation;

            var gridPosition = new IntVector();
            // build walls along the width
            for (int dx = 0; dx < cell.Bounds.Size.x; dx++)
            {
                int x = basePosition.x + dx;
                int y = basePosition.y;
                int z = basePosition.z;
                gridPosition.Set(x, y, z);

                Matrix4x4 transform = Matrix.Identity();
                Vector3 position = new Vector3(x, y, z);
                position += new Vector3(0.5f, 0, 0);
                position = Vector3.Scale(position, GridToMeshScale);
                var rotation = Quaternion.AngleAxis(180, new Vector3(0, 1, 0));
                transform = Matrix4x4.TRS(position, rotation, Vector3.one);
                int OffsetY = 0;

                //var cellLookupA = GetGridCellLookup(x, z);
                //var cellLookupB = GetGridCellLookup(x, z - 1);
                bool makeDoor = ShouldMakeDoor(x, z, x, z - 1);

                elevation = GetElevation(cell, x, z - 1, out OffsetY);
                OffsetTransformY(OffsetY * GridToMeshScale.y, ref transform);

                string SocketType = makeDoor ? DungeonConstants.ST_DOOR : DungeonConstants.ST_WALL;
                EmitMarker(SocketType, transform, gridPosition, cell.Id);
                EmitMarker(DungeonConstants.ST_WALLHALF, transform, elevation, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);

                // Add the pillar
                Matrix.SetTranslation(ref transform, Vector3.Scale(new Vector3(x, y, z), GridToMeshScale));
                OffsetTransformY(OffsetY * GridToMeshScale.y, ref transform);
                EmitMarker(DungeonConstants.ST_WALLSEPARATOR, transform, gridPosition, cell.Id);
                EmitMarker(DungeonConstants.ST_WALLHALFSEPARATOR, transform, elevation, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);

                z += cell.Bounds.Size.z;
                gridPosition.Set(x, y, z);

                elevation = GetElevation(cell, x, z, out OffsetY);
                GridCellInfo AdjacentCellInfo = GetGridCellLookup(x, z);
                if (AdjacentCellInfo.CellType != CellType.Room)
                {
                    position.z = z * GridToMeshScale.z;
                    rotation = Quaternion.AngleAxis(0, new Vector3(0, 1, 0));
                    transform = Matrix4x4.TRS(position, rotation, Vector3.one);

                    makeDoor = ShouldMakeDoor(x, z, x, z - 1);

                    SocketType = makeDoor ? DungeonConstants.ST_DOOR : DungeonConstants.ST_WALL;
                    OffsetTransformY(OffsetY * GridToMeshScale.y, ref transform);
                    EmitMarker(SocketType, transform, gridPosition, cell.Id);
                    EmitMarker(DungeonConstants.ST_WALLHALF, transform, elevation, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);

                    // Add the pillar
                    Matrix.SetTranslation(ref transform, Vector3.Scale(new Vector3(x + 1, y, z), GridToMeshScale));
                    OffsetTransformY(OffsetY * GridToMeshScale.y, ref transform);
                    gridPosition.x++;
                    EmitMarker(DungeonConstants.ST_WALLSEPARATOR, transform, gridPosition, cell.Id);
                    EmitMarker(DungeonConstants.ST_WALLHALFSEPARATOR, transform, elevation, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);
                }
            }

            // build walls along the length
            for (int dz = 0; dz < cell.Bounds.Size.z; dz++)
            {
                int x = basePosition.x;
                int y = basePosition.y;
                int z = basePosition.z + dz;
                gridPosition.Set(x, y, z);

                Matrix4x4 transform = Matrix.Identity();
                Vector3 position = new Vector3(x, y, z);
                position += new Vector3(0, 0, 0.5f);
                position = Vector3.Scale(position, GridToMeshScale);
                var rotation = Quaternion.AngleAxis(-90, new Vector3(0, 1, 0));
                transform = Matrix4x4.TRS(position, rotation, Vector3.one);
                int OffsetY = 0;

                bool makeDoor = ShouldMakeDoor(x, z, x - 1, z);

                elevation = GetElevation(cell, x - 1, z, out OffsetY);

                string SocketType = makeDoor ? DungeonConstants.ST_DOOR : DungeonConstants.ST_WALL;
                OffsetTransformY(OffsetY * GridToMeshScale.y, ref transform);
                EmitMarker(SocketType, transform, gridPosition, cell.Id);
                EmitMarker(DungeonConstants.ST_WALLHALF, transform, elevation, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);

                // Add the pillar
                Matrix.SetTranslation(ref transform, Vector3.Scale(new Vector3(x, y, z + 1), GridToMeshScale));
                OffsetTransformY(OffsetY * GridToMeshScale.y, ref transform);
                gridPosition.z++;
                EmitMarker(DungeonConstants.ST_WALLSEPARATOR, transform, gridPosition, cell.Id);
                EmitMarker(DungeonConstants.ST_WALLHALFSEPARATOR, transform, elevation, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);

                x += cell.Bounds.Size.x;
                gridPosition.Set(x, y, z);

                elevation = GetElevation(cell, x, z, out OffsetY);
                GridCellInfo AdjacentCellInfo = GetGridCellLookup(x, z);
                if (AdjacentCellInfo.CellType != CellType.Room)
                {
                    position.x = x * GridToMeshScale.x;
                    rotation = Quaternion.AngleAxis(90, new Vector3(0, 1, 0));
                    transform = Matrix4x4.TRS(position, rotation, Vector3.one);

                    makeDoor = ShouldMakeDoor(x, z, x - 1, z);

                    SocketType = makeDoor ? DungeonConstants.ST_DOOR : DungeonConstants.ST_WALL;
                    OffsetTransformY(OffsetY * GridToMeshScale.y, ref transform);
                    EmitMarker(SocketType, transform, gridPosition, cell.Id);
                    EmitMarker(DungeonConstants.ST_WALLHALF, transform, elevation, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);

                    // Add the pillar
                    Matrix.SetTranslation(ref transform, Vector3.Scale(new Vector3(x, y, z), GridToMeshScale));
                    OffsetTransformY(OffsetY * GridToMeshScale.y, ref transform);
                    EmitMarker(DungeonConstants.ST_WALLSEPARATOR, transform, gridPosition, cell.Id);
                    EmitMarker(DungeonConstants.ST_WALLHALFSEPARATOR, transform, elevation, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);
                }
            }
        }

        void BuildMesh_RoomDecoration(Cell cell)
        {
        }

        void BuildMesh_Corridor(Cell cell)
        {
            BuildMesh_Floor(cell);


            Vector3 HalfWallOffset = Vector3.Scale(GridToMeshScale, new Vector3(0, -1, 0));
            IntVector basePosition = cell.Bounds.Location;
            var gridPosition = new IntVector();

            // build fence along the width
            for (int dx = 0; dx < cell.Bounds.Size.x; dx++)
            {
                int x = basePosition.x + dx;
                int y = basePosition.y;
                int z = basePosition.z;
                gridPosition.Set(x, y, z);

                int elevationHeight;
                bool isElevatedFence, drawPillar;
                bool drawFence = CanDrawFence(cell, x, z - 1, out isElevatedFence, out drawPillar, out elevationHeight);
                Matrix4x4 transform = Matrix.Identity();
                if (drawFence || isElevatedFence)
                {
                    Vector3 position = new Vector3(x, y, z);
                    position += new Vector3(0.5f, 0, 0);
                    position = Vector3.Scale(position, GridToMeshScale);
                    var rotation = Quaternion.AngleAxis(180, new Vector3(0, 1, 0));
                    transform = Matrix4x4.TRS(position, rotation, Vector3.one);
                    if (drawFence)
                    {
                        EmitMarker(DungeonConstants.ST_FENCE, transform, gridPosition, cell.Id);
                    }
                    if (isElevatedFence)
                    {
                        EmitMarker(DungeonConstants.ST_WALLHALF, transform, elevationHeight, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);
                    }
                }
                if (drawFence || drawPillar)
                {
                    Matrix.SetTranslation(ref transform, Vector3.Scale(new Vector3(x, y, z), GridToMeshScale));
                    EmitMarker(DungeonConstants.ST_FENCESEPARATOR, transform, gridPosition, cell.Id);
                    if (isElevatedFence)
                    {
                        EmitMarker(DungeonConstants.ST_WALLHALFSEPARATOR, transform, elevationHeight, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);
                    }
                }


                z += cell.Bounds.Size.z;
                gridPosition.Set(x, y, z);

                drawFence = CanDrawFence(cell, x, z, out isElevatedFence, out drawPillar, out elevationHeight);
                transform = Matrix.Identity();
                if (drawFence || isElevatedFence)
                {
                    Vector3 position = new Vector3(x, y, z);
                    position += new Vector3(0.5f, 0, 0);
                    position = Vector3.Scale(position, GridToMeshScale);
                    var rotation = Quaternion.AngleAxis(0, new Vector3(0, 1, 0));
                    transform = Matrix4x4.TRS(position, rotation, Vector3.one);
                    if (drawFence)
                    {
                        EmitMarker(DungeonConstants.ST_FENCE, transform, gridPosition, cell.Id);
                    }
                    if (isElevatedFence)
                    {
                        EmitMarker(DungeonConstants.ST_WALLHALF, transform, elevationHeight, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);
                    }
                }
                if (drawFence || drawPillar)
                {
                    Matrix.SetTranslation(ref transform, Vector3.Scale(new Vector3(x + 1, y, z), GridToMeshScale));
                    gridPosition.x++;
                    EmitMarker(DungeonConstants.ST_FENCESEPARATOR, transform, gridPosition, cell.Id);
                    if (isElevatedFence)
                    {
                        EmitMarker(DungeonConstants.ST_WALLHALFSEPARATOR, transform, elevationHeight, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);
                    }
                }

            }

            // build fence along the length
            for (int dz = 0; dz < cell.Bounds.Size.z; dz++)
            {
                int x = basePosition.x;
                int y = basePosition.y;
                int z = basePosition.z + dz;
                gridPosition.Set(x, y, z);

                int elevationHeight;
                bool isElevatedFence, drawPillar;
                bool drawFence = CanDrawFence(cell, x - 1, z, out isElevatedFence, out drawPillar, out elevationHeight);
                Matrix4x4 transform = Matrix.Identity();
                if (drawFence || isElevatedFence)
                {
                    Vector3 position = new Vector3(x, y, z);
                    position += new Vector3(0, 0, 0.5f);
                    position = Vector3.Scale(position, GridToMeshScale);
                    var rotation = Quaternion.AngleAxis(-90, new Vector3(0, 1, 0));
                    transform = Matrix4x4.TRS(position, rotation, Vector3.one);
                    if (drawFence)
                    {
                        EmitMarker(DungeonConstants.ST_FENCE, transform, gridPosition, cell.Id);
                    }
                    if (isElevatedFence)
                    {
                        EmitMarker(DungeonConstants.ST_WALLHALF, transform, elevationHeight, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);
                    }
                }
                if (drawFence || drawPillar)
                {
                    Matrix.SetTranslation(ref transform, Vector3.Scale(new Vector3(x, y, z + 1), GridToMeshScale));
                    gridPosition.z++;
                    EmitMarker(DungeonConstants.ST_FENCESEPARATOR, transform, gridPosition, cell.Id);
                    if (isElevatedFence)
                    {
                        EmitMarker(DungeonConstants.ST_WALLHALFSEPARATOR, transform, elevationHeight, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);
                    }
                }

                x += cell.Bounds.Size.x;
                gridPosition.Set(x, y, z);
                drawFence = CanDrawFence(cell, x, z, out isElevatedFence, out drawPillar, out elevationHeight);
                transform = Matrix.Identity();
                if (drawFence || isElevatedFence)
                {
                    Vector3 position = new Vector3(x, y, z);
                    position += new Vector3(0, 0, 0.5f);
                    position = Vector3.Scale(position, GridToMeshScale);
                    var rotation = Quaternion.AngleAxis(90, new Vector3(0, 1, 0));
                    transform = Matrix4x4.TRS(position, rotation, Vector3.one);
                    if (drawFence)
                    {
                        EmitMarker(DungeonConstants.ST_FENCE, transform, gridPosition, cell.Id);
                    }
                    if (isElevatedFence)
                    {
                        EmitMarker(DungeonConstants.ST_WALLHALF, transform, elevationHeight, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);
                    }
                }
                if (drawFence || drawPillar)
                {
                    Matrix.SetTranslation(ref transform, Vector3.Scale(new Vector3(x, y, z), GridToMeshScale));
                    EmitMarker(DungeonConstants.ST_FENCESEPARATOR, transform, gridPosition, cell.Id);
                    if (isElevatedFence)
                    {
                        EmitMarker(DungeonConstants.ST_WALLHALFSEPARATOR, transform, elevationHeight, HalfWallOffset, gridPosition, cell.Id, GridToMeshScale);
                    }
                }
            }
        }

        int GetStairHeight(StairInfo stair)
        {
            Cell owner = gridModel.GetCell(stair.OwnerCell);
            Cell target = gridModel.GetCell(stair.ConnectedToCell);
            if (owner == null || target == null) return 1;
            return Mathf.Abs(owner.Bounds.Location.y - target.Bounds.Location.y);
        }

        void RemoveOverlappingMarkers()
        {
            var wallPositions = new HashSet<Vector3>();
            var wallSeparaterPositions = new HashSet<Vector3>();
            foreach (PropSocket marker in propSockets)
            {
                if (marker.SocketType == DungeonConstants.ST_WALL)
                {
                    var position = Matrix.GetTranslation(ref marker.Transform);
                    wallPositions.Add(position);
                }
                if (marker.SocketType == DungeonConstants.ST_WALLSEPARATOR)
                {
                    var position = Matrix.GetTranslation(ref marker.Transform);
                    wallSeparaterPositions.Add(position);
                }
            }

            var overlappingMarkers = new List<PropSocket>();
            foreach (PropSocket marker in propSockets)
            {
                if (marker.SocketType == DungeonConstants.ST_FENCE)
                {
                    var position = Matrix.GetTranslation(ref marker.Transform);
                    if (wallPositions.Contains(position))
                    {
                        overlappingMarkers.Add(marker);
                    }
                }
                if (marker.SocketType == DungeonConstants.ST_FENCESEPARATOR)
                {
                    var position = Matrix.GetTranslation(ref marker.Transform);
                    if (wallSeparaterPositions.Contains(position))
                    {
                        overlappingMarkers.Add(marker);
                    }
                }
            }

            // Remove all the overlapping markers
            foreach (var overlappingMarker in overlappingMarkers)
            {
                propSockets.Remove(overlappingMarker);
            }
        }

        void BuildMesh_Stairs(Cell cell)
        {
            // Draw all the stairs registered with this cell
            if (!CellStairs.ContainsKey(cell.Id))
            {
                // No stairs registered here
                return;
            }
            
            foreach (StairInfo stair in CellStairs[cell.Id])
            {
                Matrix4x4 transform = Matrix4x4.TRS(stair.Position, stair.Rotation, Vector3.one);
                int stairHeight = GetStairHeight(stair);
                string StairType = (stairHeight > 1) ? DungeonConstants.ST_STAIR2X : DungeonConstants.ST_STAIR;
                EmitMarker(StairType, transform, stair.IPosition, cell.Id);
            }
        }

        void BuildMesh_Floor(Cell cell)
        {
            var basePosition = cell.Bounds.Location;
            var gridPosition = new IntVector();
            for (int dx = 0; dx < cell.Bounds.Width; dx++)
            {
                for (int dz = 0; dz < cell.Bounds.Length; dz++)
                {
                    int x = basePosition.x + dx;
                    int y = basePosition.y;
                    int z = basePosition.z + dz;
                    gridPosition.Set(x, y, z);

                    Vector3 position = new Vector3(x, y, z);
                    position += new Vector3(0.5f, 0, 0.5f);
                    position.Scale(GridToMeshScale);

                    Matrix4x4 transform = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
                    EmitMarker(DungeonConstants.ST_GROUND, transform, gridPosition, cell.Id);
                }
            }
        }

        public override void DebugDraw()
        {
            if (!gridModel) return;
            foreach (var cell in gridModel.Cells)
            {
                GridDebugDrawUtils.DrawCell(cell, Color.white, gridConfig.GridCellSize, gridConfig.Mode2D);
                GridDebugDrawUtils.DrawAdjacentCells(cell, gridModel, Color.green, gridConfig.Mode2D);
            }

            foreach (var door in gridModel.DoorManager.Doors)
            {
                var start = door.AdjacentTiles[0];
                var end = door.AdjacentTiles[1];
                var boundsStart = new Rectangle(start.x, start.z, 1, 1);
                var boundsEnd = new Rectangle(end.x, end.z, 1, 1);
                IntVector location = boundsStart.Location;
                location.y = start.y;
                boundsStart.Location = location;

                location = boundsEnd.Location;
                location.y = end.y;
                boundsEnd.Location = location;

                GridDebugDrawUtils.DrawBounds(boundsStart, Color.yellow, gridConfig.GridCellSize, gridConfig.Mode2D);
                GridDebugDrawUtils.DrawBounds(boundsEnd, Color.yellow, gridConfig.GridCellSize, gridConfig.Mode2D);
            }
        }

    }


}
