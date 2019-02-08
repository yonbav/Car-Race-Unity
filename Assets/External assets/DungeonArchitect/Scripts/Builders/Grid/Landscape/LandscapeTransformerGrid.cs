//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections.Generic;
using DungeonArchitect.Utils;
using DungeonArchitect.Terrains;

namespace DungeonArchitect.Builders.Grid
{

    /// <summary>
    /// The terrain modifier that works with the grid based dungeon builder (DungeonBuilderGrid)
    /// It modifies the terrain by adjusting the height around the layout of the dungeon and painting 
    /// it based on the specified texture settings 
    /// </summary>
	public class LandscapeTransformerGrid : DungeonEventListener {
		public Terrain terrain;
		public LandscapeTexture[] textures;

		/// <summary>
		/// The height of the default ground level
		/// </summary>
		public float groundLevelHeight = 0;

		// The offset to apply on the terrain at the rooms and corridors. 
		// If 0, then it would touch the rooms / corridors so players can walk over it
		// Give a negative value if you want it to be below it (e.g. if you already have a ground mesh supported by pillars standing on this terrain)
		public float layoutLevelOffset = 0;

		public int smoothingDistance = 5;
		public AnimationCurve roomElevationCurve;
		public AnimationCurve corridorElevationCurve;

        public int roadBlurDistance = 6;
        public float corridorBlurThreshold = 0.5f;
        public float roomBlurThreshold = 0.5f;

		public override void OnPostDungeonLayoutBuild(Dungeon dungeon, DungeonModel model)
		{
			if (model is GridDungeonModel && terrain != null) {
				var gridModel = model as GridDungeonModel;
				BuildTerrain(gridModel);
			}
		}

        void BuildTerrain(GridDungeonModel model) {
			SetupTextures();
			UpdateHeights(model);
			UpdateTerrainTextures(model);
		}

		void SetupTextures() {
            if (terrain == null || terrain.terrainData == null) return;

			var splats = new List<SplatPrototype>();
			foreach (var texture in textures) {
				var splat = new SplatPrototype();
				splat.texture = texture.diffuse;
				splat.normalMap = texture.normal;
				splat.metallic = texture.metallic;
				splat.tileOffset = texture.offset;
				if (texture.size == Vector2.zero) {
					texture.size = new Vector2(15, 15);
				}
				splat.tileSize = texture.size;
				splats.Add(splat);
			}

			var data = terrain.terrainData;
			data.splatPrototypes = splats.ToArray();
		}

		void UpdateHeights(GridDungeonModel model) {
			if (terrain == null || terrain.terrainData == null) return;
			var rasterizer = new LandscapeDataRasterizer(terrain, groundLevelHeight);
			rasterizer.LoadData();
			var gridSize = model.Config.GridCellSize;

			// Raise the terrain
			foreach (var cell in model.Cells) {
				var locationGrid = cell.Bounds.Location;
				var location = locationGrid * gridSize;
				var size = cell.Bounds.Size * gridSize;
				var cellY = location.y + layoutLevelOffset;
				rasterizer.DrawCell(location.x, location.z, size.x, size.z, cellY);
			}

            // Smooth the terrain
            ApplySmoothing(model, rasterizer);
            
			rasterizer.SaveData();
		}

        protected virtual void ApplySmoothing(GridDungeonModel model, LandscapeDataRasterizer rasterizer)
        {
            var gridSize = model.Config.GridCellSize;
            foreach (var cell in model.Cells)
            {
                var locationGrid = cell.Bounds.Location;
                var location = locationGrid * gridSize;
                var size = cell.Bounds.Size * gridSize;
                var cellY = location.y + layoutLevelOffset;
                var curve = (cell.CellType == CellType.Room) ? roomElevationCurve : corridorElevationCurve;
                rasterizer.SmoothCell(location.x, location.z, size.x, size.z, cellY, smoothingDistance, curve);
            }
        }

		void UpdateTerrainTextures(GridDungeonModel model) {
            if (terrain == null || terrain.terrainData == null) return;

			var numTextures = textures.Length;
			var data = terrain.terrainData;
			var map = new float[data.alphamapWidth, data.alphamapHeight, numTextures];
			UpdateBaseTexture(model, map);
			UpdateCliffTexture(map);
			
			data.SetAlphamaps(0, 0, map);
		}

		void UpdateBaseTexture(GridDungeonModel model, float[,,] map) {
			if (terrain == null) return;
			int fillIndex = GetTextureIndex(LandscapeTextureType.Fill);
			if (fillIndex < 0) return;
			
			var data = terrain.terrainData;

			// Fill up the entire space with the fill texture
			for (var y = 0; y < data.alphamapHeight; y++) {
				for (var x = 0; x < data.alphamapWidth; x++) {
					for (int t = 0; t < textures.Length; t++) {
						var ratio = (t == fillIndex) ? 1 : 0;
						map[y, x, t] = ratio;
					}
				}
			}

            int corridorIndex = GetTextureIndex(LandscapeTextureType.Corridor);
            int roomIndex = GetTextureIndex(LandscapeTextureType.Room);

            if (roomIndex < 0 && corridorIndex < 0)
            {
                // Both corridor and room textures were not provided.  Fill them up with the fill index
                roomIndex = fillIndex;
                corridorIndex = fillIndex;
            }
            else if (roomIndex < 0)
            {
                // Use the same texture for the room used by the corridor
                roomIndex = corridorIndex;
            }
            else if (corridorIndex < 0)
            {
                // Use the same texture for the corridor used by the room
                corridorIndex = roomIndex;
            }

            // Apply the room/corridor texture
            {
				var gridSize = model.Config.GridCellSize;
                var roomMap = new float[map.GetLength(0), map.GetLength(1)];
                var corridorMap = new float[map.GetLength(0), map.GetLength(1)];
                foreach (var cell in model.Cells)
                {
					var bounds = cell.Bounds;
					var locationGrid = bounds.Location;
					var location = locationGrid * gridSize;
					var size = bounds.Size * gridSize;
					int gx1, gy1, gx2, gy2;
					LandscapeDataRasterizer.WorldToTerrainTextureCoord(terrain, location.x, location.z, out gx1, out gy1);
					LandscapeDataRasterizer.WorldToTerrainTextureCoord(terrain, location.x + size.x, location.z + size.z, out gx2, out gy2);
					for (var gx = gx1; gx <= gx2; gx++) {
						for (var gy = gy1; gy <= gy2; gy++) {
                            if (cell.CellType == CellType.Unknown) continue;
                            if (cell.CellType == CellType.Room)
                            {
                                roomMap[gy, gx] = 1;
                            }
                            else
                            {
                                corridorMap[gy, gx] = 1;
                            }
						}
					}
				}

				// Blur the layout data
				var filter = new BlurFilter(roadBlurDistance);
                roomMap = filter.ApplyFilter(roomMap);
                corridorMap = filter.ApplyFilter(corridorMap);
				
				// Fill up the inner region with corridor index
				for (var y = 0; y < data.alphamapHeight; y++) {
					for (var x = 0; x < data.alphamapWidth; x++) {
                        bool corridor = (corridorMap[y, x] > corridorBlurThreshold);
						if (corridor) {
                            map[y, x, fillIndex] = 0;
                            map[y, x, roomIndex] = 0;
                            map[y, x, corridorIndex] = 1;
                        }
                    
                        bool room = (roomMap[y, x] > roomBlurThreshold);
                        if (room)
                        {
                            map[y, x, fillIndex] = 0;
                            map[y, x, corridorIndex] = 0;
                            map[y, x, roomIndex] = 1;
                        }
                    }
				}
			}
		}

		void UpdateCliffTexture(float[,,] map) {
			if (terrain == null) return;
			int cliffIndex = GetTextureIndex(LandscapeTextureType.Cliff);
			if (cliffIndex < 0) return;
			
			var data = terrain.terrainData;
			
			// For each point on the alphamap...
			for (var y = 0; y < data.alphamapHeight; y++) {
				for (var x = 0; x < data.alphamapWidth; x++) {
					// Get the normalized terrain coordinate that
					// corresponds to the the point.
					var normX = x * 1.0f / (data.alphamapWidth - 1);
					var normY = y * 1.0f / (data.alphamapHeight - 1);
					
					// Get the steepness value at the normalized coordinate.
					var angle = data.GetSteepness(normX, normY);
					
					// Steepness is given as an angle, 0..90 degrees. Divide
					// by 90 to get an alpha blending value in the range 0..1.
					var frac = angle / 90.0f;
					frac *= 2;
					frac = Mathf.Clamp01(frac);
					var cliffRatio = frac;
					var nonCliffRatio = 1 - frac;
					
					for (int t = 0; t < textures.Length; t++) {
						if (t == cliffIndex) {
							map[y, x, t] = cliffRatio;
						} else {
							map[y, x, t] *= nonCliffRatio;
						}
					}
				}
			}
		}
		
		/// <summary>
		/// Returns the index of the landscape texture.  -1 if not found
		/// </summary>
		/// <returns>The texture index. -1 if not found</returns>
		/// <param name="textureType">Texture type.</param>
		int GetTextureIndex(LandscapeTextureType textureType) {
			for (int i = 0; i < textures.Length; i++) {
				if (textures[i].textureType == textureType) {
					return i;
				}
			}
			return -1;	// Doesn't exist
		}

	}
}
