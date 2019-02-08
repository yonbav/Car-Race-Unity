//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections;
using DungeonArchitect.Utils;

namespace DungeonArchitect
{
    /// <summary>
    /// Manages the landscape data and performs various rasterization algorithms (draw cells, lines etc)
    /// </summary>
    public class LandscapeDataRasterizer
    {
        Terrain terrain;
        float[,] heights;
        int terrainWidth;
        int terrainHeight;
        float baseElevation;
        bool[,] lockedCells;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="terrain">The terrain object to modify</param>
        /// <param name="elevation">The prefered ground level elevation</param>
        public LandscapeDataRasterizer(Terrain terrain, float elevation)
        {
            this.terrain = terrain;
            baseElevation = GetElevation(elevation);
        }

        /// <summary>
        /// Loads the data from the terrain into memory for modification
        /// </summary>
        public void LoadData()
        {
            var data = terrain.terrainData;
            terrainWidth = data.heightmapWidth;
            terrainHeight = data.heightmapHeight;

            heights = data.GetHeights(0, 0, terrainWidth, terrainHeight);

            lockedCells = new bool[terrainWidth, terrainHeight];

            for (int ix = 0; ix < terrainWidth; ix++)
            {
                for (int iy = 0; iy < terrainHeight; iy++)
                {
                    heights[iy, ix] = baseElevation;
                    lockedCells[iy, ix] = false;
                }
            }
        }

        /// <summary>
        /// Gets the elevation in normalized space
        /// </summary>
        /// <param name="worldElevation"></param>
        /// <returns></returns>
        float GetElevation(float worldElevation)
        {
            var resolution = terrain.terrainData.size.y;
            return (worldElevation - terrain.transform.position.y) / resolution;
        }

        /// <summary>
        /// Gets the height of the terrain at the specified world space
        /// </summary>
        /// <param name="terrain">The terrain object</param>
        /// <param name="worldX">X cooridnate in world space</param>
        /// <param name="worldZ">Z cooridnate in world space</param>
        /// <returns>The Y height of the terrain at the specified location</returns>
        public static float GetHeight(Terrain terrain, float worldX, float worldZ)
        {
            int gx, gy;
            LandscapeDataRasterizer.WorldToTerrainCoord(terrain, worldX, worldZ, out gx, out gy);
            var height = terrain.terrainData.GetHeight(gx, gy);

            return height + terrain.transform.position.y;
        }

        /// <summary>
        /// Converts the world coordinate to internal terrain coordinate where the data is loaded
        /// </summary>
        /// <param name="terrain">The terrain to query</param>
        /// <param name="x">x coordinate in world coordinate</param>
        /// <param name="y">z coordinate in world coordinate</param>
        /// <param name="gx">x cooridnate in the 2D terrain height data coordinate</param>
        /// <param name="gy">y cooridnate in the 2D terrain height data coordinate</param>
        public static void WorldToTerrainCoord(Terrain terrain, float x, float y, out int gx, out int gy)
        {
            var terrainSize = terrain.terrainData.size;
            var data = terrain.terrainData;

            var terrainWidth = data.heightmapWidth;
            var terrainHeight = data.heightmapHeight;

            var multiplierX = (terrainWidth - 1) / terrainSize.x;
            var multiplierZ = (terrainHeight - 1) / terrainSize.z;

            var offset = new Vector2();
            offset.x = -terrain.transform.position.x;
            offset.y = -terrain.transform.position.z;
            var xf = (x + offset.x) * multiplierX;
            var yf = (y + offset.y) * multiplierZ;

            gx = Mathf.RoundToInt(xf);
            gy = Mathf.RoundToInt(yf);
        }

        /// <summary>
        /// Converts the world coordinate to terrain texture coordinate
        /// </summary>
        /// <param name="terrain">The terrain to query</param>
        /// <param name="x">x coordinate in world coordinate</param>
        /// <param name="y">z coordinate in world coordinate</param>
        /// <param name="tx">x cooridnate in the 2D terrain texture data coordinate</param>
        /// <param name="ty">y cooridnate in the 2D terrain texture data coordinate</param>
        public static void WorldToTerrainTextureCoord(Terrain terrain, float x, float y, out int tx, out int ty)
        {
            var terrainSize = terrain.terrainData.size;
            var data = terrain.terrainData;

            var terrainWidth = data.alphamapWidth;
            var terrainHeight = data.alphamapHeight;

            var multiplierX = (terrainWidth - 1) / terrainSize.x;
            var multiplierZ = (terrainHeight - 1) / terrainSize.z;

            var offset = new Vector2();
            offset.x = -terrain.transform.position.x;
            offset.y = -terrain.transform.position.z;
            var xf = (x + offset.x) * multiplierX;
            var yf = (y + offset.y) * multiplierZ;

            tx = Mathf.RoundToInt(xf);
            ty = Mathf.RoundToInt(yf);
        }

        /// <summary>
        /// Converts the world coordinate to terrain texture coordinate
        /// </summary>
        /// <param name="terrain">The terrain to query</param>
        /// <param name="x">x coordinate in world coordinate</param>
        /// <param name="y">z coordinate in world coordinate</param>
        /// <param name="tx">x cooridnate in the 2D terrain texture data coordinate</param>
        /// <param name="ty">y cooridnate in the 2D terrain texture data coordinate</param>
        public static void WorldToTerrainDetailCoord(Terrain terrain, float x, float y, out int tx, out int ty)
        {
            var terrainSize = terrain.terrainData.size;
            var data = terrain.terrainData;

            var terrainWidth = data.detailWidth;
            var terrainHeight = data.detailHeight;

            var multiplierX = (terrainWidth - 1) / terrainSize.x;
            var multiplierZ = (terrainHeight - 1) / terrainSize.z;

            var offset = new Vector2();
            offset.x = -terrain.transform.position.x;
            offset.y = -terrain.transform.position.z;
            var xf = (x + offset.x) * multiplierX;
            var yf = (y + offset.y) * multiplierZ;

            tx = Mathf.RoundToInt(xf);
            ty = Mathf.RoundToInt(yf);
        }

        /// <summary>
        /// Rasterizes the terrain height along the specified world cooridnate with the specified elevation height
        /// </summary>
        /// <param name="x">x cooridnate in world space</param>
        /// <param name="y">z coordinate in world space</param>
        /// <param name="w">width in world space</param>
        /// <param name="h">height in world space</param>
        /// <param name="elevation">The elevation to set in the specified bounds</param>
        public void DrawCell(float x, float y, float w, float h, float elevation)
        {
            int x1, y1, x2, y2;
            WorldToTerrainCoord(terrain, x, y, out x1, out y1);
            WorldToTerrainCoord(terrain, x + w, y + h, out x2, out y2);

            for (int ix = x1; ix <= x2; ix++)
            {
                for (int iy = y1; iy <= y2; iy++)
                {
                    if (ix < 0 || ix >= terrainWidth || iy < 0 || iy >= terrainHeight)
                    {
                        // Out of terrain boundaries. Ignore
                        continue;
                    }
                    var normalizedElevation = GetElevation(elevation);
                    heights[iy, ix] = normalizedElevation;
                    lockedCells[iy, ix] = true;
                }
            }
        }

        /// <summary>
        /// Applies a smoothing blur filter based on the user-defined smoothing curve 
        /// </summary>
        /// <param name="x">x cooridnate in world space</param>
        /// <param name="y">z coordinate in world space</param>
        /// <param name="w">width in world space</param>
        /// <param name="h">height in world space</param>
        /// <param name="elevation">The elevation to set in the specified bounds</param>
        /// <param name="smoothingDistance">The distance to apply the smoothing transition on.  For e.g. if the distance it 5, the smoothing would occur over 5 units</param>
        /// <param name="smoothingCurve">The user defined curve to control the steepness of cliffs</param>
        public void SmoothCell(float x, float y, float w, float h, float elevation, int smoothingDistance, AnimationCurve smoothingCurve)
        {
            int x1, y1, x2, y2;
            WorldToTerrainCoord(terrain, x, y, out x1, out y1);
            WorldToTerrainCoord(terrain, x + w, y + h, out x2, out y2);
            var bounds = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            var startElevation = GetElevation(elevation);
            var endElevation = baseElevation;
            for (int i = 1; i <= smoothingDistance; i++)
            {
                bounds = Rectangle.ExpandBounds(bounds, 1);
                var borderPoints = bounds.GetBorderPoints();
                foreach (var borderPoint in borderPoints)
                {
                    var ix = borderPoint.x;
                    var iy = borderPoint.z;
                    if (lockedCells[iy, ix])
                    {
                        continue;
                    }
                    var ratio = (float)i / (smoothingDistance + 1.0f);
                    var curveHeight = smoothingCurve.Evaluate(ratio);
                    var cellElevation = startElevation + (endElevation - startElevation) * curveHeight;
                    if (startElevation > endElevation)
                    {
                        heights[iy, ix] = Mathf.Max(heights[iy, ix], cellElevation);
                    }
                    else
                    {
                        heights[iy, ix] = Mathf.Min(heights[iy, ix], cellElevation);
                    }
                }
            }
        }

        /// <summary>
        /// Saves the data in memory back into the terrain. This modifies the terrain object
        /// </summary>
        public void SaveData()
        {
            terrain.terrainData.SetHeights(0, 0, heights);
        }
    }
}
