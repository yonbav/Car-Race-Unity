using UnityEngine;
using System.Collections;
using DungeonArchitect;

namespace DungeonArchitect.Builders.Isaac
{
    public class IsaacDungeonConfig : DungeonConfig
    {
        public int minRooms;
        public int maxRooms;

        public int roomWidth = 10;
        public int roomHeight = 6;

        public Vector2 tileSize = new Vector2(1, 1);
        public Vector2 roomPadding = new Vector2(1, 1);

        public float growForwardProbablity = 0.75f;
        public float growSidewaysProbablity = 0.25f;

        public float spawnRoomBranchProbablity = 0.75f;
        public float cycleProbability = 1.0f;
    }
}
