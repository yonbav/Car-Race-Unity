//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections;

namespace DungeonArchitect {

    /// <summary>
    /// A data filter applied over a 2D data array
    /// </summary>
	public interface Filter {
		float[,] ApplyFilter(float[,] data);
	}
}
