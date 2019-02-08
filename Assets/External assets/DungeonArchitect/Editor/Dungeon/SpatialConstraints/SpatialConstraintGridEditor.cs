//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Constraints;
using DungeonArchitect.Constraints.Grid;

namespace DungeonArchitect.Editors
{
    public abstract class SpatialConstraintGridEditor : SpatialConstraintEditor
    {
        protected int minCellHeight = 40;
        protected bool DrawGridCell(SpatialConstraintGridCell cell)
        {
            string title = GetConstraintString(cell.CellType);
            bool modified = false;
            if (GUILayout.Button(title, GUILayout.MinHeight(minCellHeight)))
            {
                cell.CellType = GetNextType(cell.CellType);
                modified = true;
            }
            return modified;
        }

        SpatialConstraintGridCellType GetNextType(SpatialConstraintGridCellType cellType)
        {
            return (SpatialConstraintGridCellType)(((int)cellType + 1) % 3);
        }

        string GetConstraintString(SpatialConstraintGridCellType type)
        {
            if (type == SpatialConstraintGridCellType.DontCare)
            {
                return "Ignore";
            }
            return type.ToString();
        }
    }
}
