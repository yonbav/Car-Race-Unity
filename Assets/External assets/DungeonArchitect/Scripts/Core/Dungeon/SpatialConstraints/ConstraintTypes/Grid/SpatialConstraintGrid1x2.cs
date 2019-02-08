//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect.Graphs;

namespace DungeonArchitect.Constraints.Grid
{
    //[Meta(displayText: "Edge (Grid)")]
    [System.Serializable]
    public class SpatialConstraintGrid1x2 : SpatialConstraint
    {
        public SpatialConstraintGridCell left = new SpatialConstraintGridCell();
        public SpatialConstraintGridCell right = new SpatialConstraintGridCell();
        public override void OnEnable() 
        {
            base.OnEnable();
        }
    }
}