//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect.Graphs;

namespace DungeonArchitect.Constraints
{
    [System.Serializable]
    public class SpatialConstraint : ScriptableObject
    {
        public bool rotateToFit = true;
        public bool applyMarkerRotation = true;
        public virtual void OnEnable() 
        {
            hideFlags = HideFlags.HideInHierarchy;
        }
    }
}

