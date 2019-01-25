using UnityEngine;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Utils;

namespace DungeonArchitect.Builders.FloorPlan
{
    public class FloorPlanConfig : DungeonConfig
    {

        public Vector3 BuildingSize;

        public Vector3 GridSize;

        public int MinRoomSize;

        public int MaxRoomSize;

        public int HallWidth;

        public int MinRoomChunkArea;

        public int RoomSplitProbabilityOffset;
    }
}

