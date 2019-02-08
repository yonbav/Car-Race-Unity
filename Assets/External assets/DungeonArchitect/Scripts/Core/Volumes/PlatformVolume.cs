//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections;
using DungeonArchitect;

namespace DungeonArchitect.Builders.Grid
{
    /// <summary>
    /// Platform volumes add a platform in the scene encompassing the volume
    /// </summary>
    public class PlatformVolume : Volume
    {
        public CellType cellType = CellType.Corridor;
    }
}
