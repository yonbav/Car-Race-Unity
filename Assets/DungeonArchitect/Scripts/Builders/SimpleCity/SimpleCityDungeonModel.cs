using UnityEngine;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Utils;

namespace DungeonArchitect.Builders.SimpleCity
{

    public enum SimpleCityCellType
    {
        Road,
        House,
        Park,
        CityWallPadding,
        UserDefined,
        Empty
    }

    public class SimpleCityCell
    {
        public IntVector Position;
        public SimpleCityCellType CellType;
        public Quaternion Rotation;
        public Vector3 BlockSize = new Vector3(1, 0, 1);
        public string MarkerNameOverride;
    }

    public class SimpleCityDungeonModel : DungeonModel
    {
        [HideInInspector]
        public SimpleCityCell[,] Cells = new SimpleCityCell[0, 0];

        [HideInInspector]
        public SimpleCityCell[] WallPaddingCells;

        [HideInInspector]
        public SimpleCityDungeonConfig Config;

        [HideInInspector]
        public int CityWidth;

        [HideInInspector]
        public int CityHeight;

    }

}