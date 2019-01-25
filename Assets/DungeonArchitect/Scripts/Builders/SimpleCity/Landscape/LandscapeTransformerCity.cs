//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect.Utils;
using DungeonArchitect.Terrains;

namespace DungeonArchitect.Builders.SimpleCity
{

    /// <summary>
    /// The type of the texture defined in the landscape paint settings.  
    /// This determines how the specified texture would be painted in the modified terrain
    /// </summary>
    public enum SimpleCityLandscapeTextureType
    {
        Fill,
        Road,
        Park,
        CityWallPadding
    }

    /// <summary>
    /// Data-structure to hold the texture settings.  This contains enough information to paint the texture 
    /// on to the terrain
    /// </summary>
    [System.Serializable]
    public class SimpleCityLandscapeTexture
    {
        public SimpleCityLandscapeTextureType textureType;
        public Texture2D diffuse;
        public Texture2D normal;
        public float metallic = 0;
        public Vector2 size = new Vector2(15, 15);
        public Vector2 offset = Vector2.zero;
        public AnimationCurve curve;
    }

    [System.Serializable]
    public class SimpleCityFoliageEntry
    {
        public int grassIndex;
        public float density;
    }

    [System.Serializable]
    public class SimpleCityFoliageTheme
    {
        public SimpleCityLandscapeTextureType textureType = SimpleCityLandscapeTextureType.Park;
        public SimpleCityFoliageEntry[] foliageEntries;
        public AnimationCurve curve;
        public float density;
    }


    /// <summary>
    /// The terrain modifier that works with the grid based dungeon builder (DungeonBuilderGrid)
    /// It modifies the terrain by adjusting the height around the layout of the dungeon and painting 
    /// it based on the specified texture settings 
    /// </summary>
    public class LandscapeTransformerCity : DungeonEventListener
    {
        public Terrain terrain;
        public SimpleCityLandscapeTexture[] textures;

        public SimpleCityFoliageTheme[] foliage;
        //SimpleCityFoliageTheme roadFoliage;
        //SimpleCityFoliageTheme openSpaceFoliage;

        public int roadBlurDistance = 6;
        public float corridorBlurThreshold = 0.5f;
        public float roomBlurThreshold = 0.5f;
        

        public override void OnPostDungeonLayoutBuild(Dungeon dungeon, DungeonModel model)
        {
            if (model is SimpleCityDungeonModel && terrain != null)
            {
                var cityModel = model as SimpleCityDungeonModel;
                BuildTerrain(cityModel);
            }
        }

        void BuildTerrain(SimpleCityDungeonModel model)
        {
            SetupTextures();
            UpdateTerrainTextures(model);
        }

        void SetupTextures()
        {
            if (terrain == null || terrain.terrainData == null) return;

            var splats = new List<SplatPrototype>();
            foreach (var texture in textures)
            {
                var splat = new SplatPrototype();
                splat.texture = texture.diffuse;
                splat.normalMap = texture.normal;
                splat.metallic = texture.metallic;
                splat.tileOffset = texture.offset;
                if (texture.size == Vector2.zero)
                {
                    texture.size = new Vector2(15, 15);
                }
                splat.tileSize = texture.size;
                splats.Add(splat);
            }

            var data = terrain.terrainData;
            data.splatPrototypes = splats.ToArray();
        }
        
        void UpdateTerrainTextures(SimpleCityDungeonModel model)
        {
            if (terrain == null || terrain.terrainData == null) return;

            var numTextures = textures.Length;
            var data = terrain.terrainData;
            var map = new float[data.alphamapWidth, data.alphamapHeight, numTextures];
            UpdateBaseTexture(model, map);

            data.SetAlphamaps(0, 0, map);
        }
        

        void UpdateBaseTexture(SimpleCityDungeonModel model, float[,,] map)
        {
            if (terrain == null) return;
            int fillIndex = GetTextureIndex(SimpleCityLandscapeTextureType.Fill);
            if (fillIndex < 0) return;

            var data = terrain.terrainData;


            // Fill up the entire space with the fill texture
            for (var y = 0; y < data.alphamapHeight; y++)
            {
                for (var x = 0; x < data.alphamapWidth; x++)
                {
                    for (int t = 0; t < textures.Length; t++)
                    {
                        var ratio = (t == fillIndex) ? 1 : 0;
                        map[y, x, t] = ratio;
                    }
                }
            }

            var activeTextureTypes = new SimpleCityLandscapeTextureType[] {
                SimpleCityLandscapeTextureType.Park,
                SimpleCityLandscapeTextureType.Road,
                SimpleCityLandscapeTextureType.CityWallPadding,
            };

            var activeCellTypes = new SimpleCityCellType[] {
                SimpleCityCellType.Park,
                SimpleCityCellType.Road,
                SimpleCityCellType.CityWallPadding,
            };

            var dataMaps = new List<float[,]>();
            for (int i = 0; i < activeTextureTypes.Length; i++)
            {
                dataMaps.Add(new float[map.GetLength(0), map.GetLength(1)]);
            }
            
            var gridSize2D = model.Config.CellSize;
            var gridSize = new Vector3(gridSize2D.x, 0, gridSize2D.y);
            var cells = new List<SimpleCityCell>();
            foreach (var cell in model.Cells)
            {
                cells.Add(cell);
            }
            cells.AddRange(model.WallPaddingCells);

            foreach (var cell in cells)
            {
                var locationGrid = cell.Position;
                var location = locationGrid * gridSize - gridSize / 2.0f;
                var size = gridSize;
                int gx1, gy1, gx2, gy2;
                LandscapeDataRasterizer.WorldToTerrainTextureCoord(terrain, location.x, location.z, out gx1, out gy1);
                LandscapeDataRasterizer.WorldToTerrainTextureCoord(terrain, location.x + size.x, location.z + size.z, out gx2, out gy2);
                for (int i = 0; i < activeTextureTypes.Length; i++)
                {
                    //SimpleCityLandscapeTextureType activeTexType = activeTextureTypes[i];
                    SimpleCityCellType activeCellType = activeCellTypes[i];
                    //int textureIndex = GetTextureIndex(activeTexType);
                    var dataMap = dataMaps[i];

                    for (var gx = gx1; gx <= gx2; gx++)
                    {
                        for (var gy = gy1; gy <= gy2; gy++)
                        {
                            dataMap[gy, gx] = (cell.CellType == activeCellType) ? 1 : 0;
                        }
                    }
                }
            }

            // Blur the layout data
            var filter = new BlurFilter(roadBlurDistance);
            for (int i = 0; i < dataMaps.Count; i++) 
            {
                dataMaps[i] = filter.ApplyFilter(dataMaps[i]);
            }
                
            for (int i = 0; i < dataMaps.Count; i++)
            {
                var dataMap = dataMaps[i];
                int textureIndex = GetTextureIndex(activeTextureTypes[i]);
                if (textureIndex < 0) continue;
                for (var y = 0; y < data.alphamapHeight; y++)
                {
                    for (var x = 0; x < data.alphamapWidth; x++)
                    {
                        map[y, x, textureIndex] = dataMap[y, x];
                        if (textureIndex != fillIndex)
                        {
                            map[y, x, fillIndex] -= dataMap[y, x];
                            map[y, x, fillIndex] = Mathf.Clamp01(map[y, x, fillIndex]);
                        }
                    }
                }
            }

            // Normalize
            for (var y = 0; y < data.alphamapHeight; y++)
            {
                for (var x = 0; x < data.alphamapWidth; x++)
                {
                    // Apply the curves
                    for (int t = 0; t < textures.Length; t++)
                    {
                        var curve = textures[t].curve;
                        if (curve != null && curve.keys.Length > 0)
                        {
                            map[y, x, t] = curve.Evaluate(map[y, x, t]);
                        }
                    }

                    float sum = 0;
                    for (int t = 0; t < textures.Length; t++)
                    {
                        sum += map[y, x, t];
                    }

                    for (int t = 0; t < textures.Length; t++)
                    {
                        map[y, x, t] /= sum;
                    }
                }
            }

            for (int layer = 0; layer < data.detailPrototypes.Length; layer++)
            {
                var foliageMap = data.GetDetailLayer(0, 0, data.detailWidth, data.detailHeight, layer);

                for (int x = 0; x < data.detailWidth; x++)
                {
                    float nx = x / (float)(data.detailWidth - 1);
                    int sampleX = Mathf.RoundToInt(nx * (data.alphamapWidth - 1));
                    for (int y = 0; y < data.detailHeight; y++)
                    {
                        float ny = y / (float)(data.detailHeight - 1);
                        int sampleY = Mathf.RoundToInt(ny * (data.alphamapHeight - 1));

                        float influence = 0;
                        foreach (var foliageTheme in foliage)
                        {
                            var textureIndex = GetTextureIndex(foliageTheme.textureType);
                            if (textureIndex < 0) continue;
                            foreach (var entry in foliageTheme.foliageEntries)
                            {
                                if (entry.grassIndex == layer)
                                {
                                    float mapData = map[sampleY, sampleX, textureIndex];
                                    if (foliageTheme.curve != null && foliageTheme.curve.length > 0)
                                    {
                                        mapData = foliageTheme.curve.Evaluate(mapData);
                                    }
                                    float alpha = mapData * entry.density * foliageTheme.density;
                                    influence += alpha;
                                }
                            }
                        }

                        int value = Mathf.FloorToInt(influence);
                        float frac = influence - value;
                        if (Random.value < frac) value++;
                        foliageMap[y, x] = value;
                    }
                }

                data.SetDetailLayer(0, 0, layer, foliageMap);
            }

            /*
            // Update foliage
            foreach (var foliageTheme in foliage)
            {
                var textureIndex = GetTextureIndex(foliageTheme.textureType);
                if (textureIndex < 0) continue;
                foreach (var entry in foliageTheme.foliageEntries)
                {
                    int layer = entry.grassIndex;
                    var foliageMap = data.GetDetailLayer(0, 0, data.detailWidth, data.detailHeight, layer);
                    for (int x = 0; x < data.detailWidth; x++)
                    {
                        float nx = x / (float)(data.detailWidth - 1);
                        int sampleX = Mathf.RoundToInt(nx * (data.alphamapWidth - 1));
                        for (int y = 0; y < data.detailHeight; y++)
                        {
                            float ny = y / (float)(data.detailHeight - 1);
                            int sampleY = Mathf.RoundToInt(ny * (data.alphamapHeight - 1));

                            float alpha = map[sampleY, sampleX, textureIndex] * entry.density * foliageTheme.density;
                            int value = Mathf.FloorToInt(alpha);
                            float frac = alpha - value;
                            if (Random.value < frac) value++;
                            foliageMap[y, x] = value;
                        }
                    }

                    data.SetDetailLayer(0, 0, layer, foliageMap);
                }
            }
            */
        }

        /// <summary>
        /// Returns the index of the landscape texture.  -1 if not found
        /// </summary>
        /// <returns>The texture index. -1 if not found</returns>
        /// <param name="textureType">Texture type.</param>
        int GetTextureIndex(SimpleCityLandscapeTextureType textureType)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i].textureType == textureType)
                {
                    return i;
                }
            }
            return -1;  // Doesn't exist
        }

    }
}
