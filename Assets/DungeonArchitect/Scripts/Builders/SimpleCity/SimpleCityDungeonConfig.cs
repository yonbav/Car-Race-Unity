using UnityEngine;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Utils;

namespace DungeonArchitect.Builders.SimpleCity
{
    [System.Serializable]
    public struct CityBlockDimension
    {
        [Tooltip(@"If this block is inserted, this marker name take from the theme file")]
        public string markerName;

        [Tooltip(@"The width of the block, in grid coordinates.  E.g. 2 would take up 2 blocks")]
        public int sizeX;

        [Tooltip(@"The length of the block, in grid coordinates.  E.g. 2 would take up 2 blocks")]
        public int sizeZ;

        [Tooltip(@"The chance of this block appearing.  0 - No chance, 1 - Every time.   0.5 = 50% chance of appearing")]
        public float probability;
    }

    public class SimpleCityDungeonConfig : DungeonConfig
    {
        public Vector2 CellSize = new Vector2(4, 4);

        public int minSize = 15;
        public int maxSize = 20;

        public int minBlockSize = 2;
        public int maxBlockSize = 4;

        public float biggerHouseProbability = 0;

        public int cityWallPadding = 1;
        public int cityDoorSize = 1;

		public float roadEdgeRemovalProbability = 0;

        public CityBlockDimension[] customBlockDimensions;
    }
}

