//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections;

namespace DungeonArchitect
{
    /// <summary>
    /// Base dungeon configuration.  Create your own implementation of this configuration based on your dungeon builder's needs
    /// </summary>
	public class DungeonConfig : MonoBehaviour {
        [Tooltip(@"Change this number to completely change the layout of your level")]
        public uint Seed = 0;

        public bool Mode2D = false;
	}
}
