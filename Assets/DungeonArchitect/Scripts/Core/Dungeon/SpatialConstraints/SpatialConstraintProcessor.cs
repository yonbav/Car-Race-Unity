//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections.Generic;

namespace DungeonArchitect.Constraints
{
    public abstract class SpatialConstraintProcessor : MonoBehaviour
    {
        public virtual void Initialize(DungeonModel model, List<PropSocket> levelSockets) { }
        public abstract bool ProcessSpatialConstraint(SpatialConstraint constraint, PropSocket socket, DungeonModel model, List<PropSocket> levelSockets, out Matrix4x4 outOffset);
        public virtual void Cleanup() { }
    }
}