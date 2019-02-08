//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect.Graphs;

namespace DungeonArchitect.Constraints.Grid
{
    //[Meta(displayText: "2x2 (Grid)")]
    [System.Serializable]
    public class SpatialConstraintGrid2x2 : SpatialConstraint
    {
        [SerializeField]
        public SpatialConstraintGridCell[] cells = new SpatialConstraintGridCell[4];
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