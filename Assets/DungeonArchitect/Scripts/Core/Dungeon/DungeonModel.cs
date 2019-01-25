//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System;
using System.Collections.Generic;
using DungeonArchitect.Utils;

namespace DungeonArchitect
{
    /// <summary>
    /// Abstract dungeon model.  Create your own implementation of the model depending on your builder's needs
    /// </summary>
	//[System.Serializable]
	public abstract class DungeonModel : MonoBehaviour
	{
        void Reset()
        {
            ResetModel();
        }

        public virtual void ResetModel() { }

        [SerializeField]
        //[HideInInspector]
        public DungeonToolData ToolData;

        public virtual DungeonToolData CreateToolDataInstance()
        {
            return ScriptableObject.CreateInstance<DungeonToolData>();
        }

	}


    /// <summary>
    /// Tool Data represented by the grid based builder
    /// </summary>
    [Serializable]
    public class DungeonToolData : ScriptableObject
    {
        // The cells painted by the "Paint" tool
        [SerializeField]
        List<IntVector> paintedCells = new List<IntVector>();
        public List<IntVector> PaintedCells
        {
            get
            {
                return paintedCells;
            }
        }
    }
}
