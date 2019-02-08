//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System;
using System.Collections.Generic;

namespace DungeonArchitect.Constraints.Grid
{
    public enum SpatialConstraintGridCellType
    {
        DontCare,
        Occupied,
        Empty
    }

    [Serializable]
    public class SpatialConstraintGridCell
    {
        public SpatialConstraintGridCellType CellType = SpatialConstraintGridCellType.DontCare;

        public override string ToString() {
            return CellType.ToString();
        }
    }
}
