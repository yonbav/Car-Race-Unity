//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect.Graphs;
using DungeonArchitect.Constraints;
using DungeonArchitect.Constraints.Grid;

namespace DungeonArchitect.Constraints.Grid
{
    //[Meta(displayText: "3x3 (Grid)")]
    [System.Serializable]
    public class SpatialConstraintGrid3x3 : SpatialConstraint
    { 
        [SerializeField] 
        public SpatialConstraintGridCell[] cells = new SpatialConstraintGridCell[9];
        public override void OnEnable()
        { 
            base.OnEnable();

            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null)
                {
                    cells[i] = new SpatialConstraintGridCell();
                }
            }
        }
    }
}