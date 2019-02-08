//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using DungeonArchitect.Utils;
using DungeonArchitect.Graphs;

namespace DungeonArchitect
{
    /// <summary>
    /// The main dungeon behavior that manages the creation and destruction of dungeons
    /// </summary>
	[ExecuteInEditMode]
	public class Dungeon : MonoBehaviour {
		DungeonConfig config;
        PooledDungeonSceneProvider sceneProvider;
        DungeonBuilder dungeonBuilder;
		DungeonModel dungeonModel;

        /// <summary>
        /// Active model used by the dungeon
        /// </summary>
		public DungeonModel ActiveModel {
			get {
				if (dungeonModel == null) {
					dungeonModel = GetComponent<DungeonModel> ();
				}
				return dungeonModel;
			}
		}

        /// <summary>
        /// Flag to check if the layout has been built.  
        /// This is used to quickly reapply the theme after the theme graph has been modified,
        /// without rebuilding the layout, if it has already been built
        /// </summary>
		public bool IsLayoutBuilt {
			get {
                if (dungeonBuilder == null)
                {
                    return false;
                }
                return dungeonBuilder.IsLayoutBuilt;
			}
		}

		public bool debugDraw = false;

        //[SerializeField]
        List<PropSocket> markers = new List<PropSocket>();
        public List<PropSocket> Markers
        {
            get { return markers; }
        }

        /// <summary>
        /// List of themes assigned to this dungeon
        /// </summary>
		public List<Graph> dungeonThemes;

        /// <summary>
        /// Flag to rebuild the dungeon. Set this to true if you want to rebuild it in the next update
        /// </summary>
		bool requestedRebuild = false;

		public DungeonConfig Config {
			get {
				if (config == null) {
					config = GetComponent<DungeonConfig> ();
				}
				return config;
			}
		}

        void Awake() {
            Initialize();
		}

		void Start () {
		}


		void Initialize() {
			if (config == null) {
				config = GetComponent<DungeonConfig> ();
			}
			
			if (sceneProvider == null) {
				sceneProvider = GetComponent<PooledDungeonSceneProvider> ();
			}
			
			if (dungeonBuilder == null) {
				dungeonBuilder = GetComponent<DungeonBuilder> ();
			}

			if (dungeonModel == null) {
				dungeonModel = GetComponent<DungeonModel> ();
			}
		}

        List<DungeonPropDataAsset> GetThemeAssets()
        {
            var themes = new List<DungeonPropDataAsset>();
            foreach (var themeGraph in dungeonThemes)
            {
                DungeonPropDataAsset theme = new DungeonPropDataAsset();
                theme.BuildFromGraph(themeGraph);
                themes.Add(theme);
            }
            return themes;
        }

        /// <summary>
        /// Builds the complete dungeon (layout and visual phase)
        /// </summary>
		public void Build() {
			Initialize();
			dungeonModel.ResetModel();

			dungeonBuilder.BuildDungeon(config, dungeonModel);
            markers = dungeonBuilder.PropSockets;

			NotifyPostLayoutBuild();

            if (dungeonBuilder.IsThemingSupported())
            {
                ReapplyTheme();
            }
            else
            {
                dungeonBuilder.BuildNonThemedDungeon(sceneProvider);
            }

            NotifyPostBuild();
        }

        /// <summary>
        /// Runs the theming engine over the existing layout to rebuild the game objects from the theme file.  
        /// The layout is not built in this stage
        /// </summary>
		public void ReapplyTheme() {
            // Emit markers defined by this builder
			dungeonBuilder.EmitMarkers();

            // Emit markers defined by the users (by attaching implementation of DungeonMarkerEmitter behaviors)
            dungeonBuilder.EmitCustomMarkers();

            NotifyMarkersEmitted(dungeonBuilder.PropSockets);

			var themes = GetThemeAssets();
			
			sceneProvider.OnDungeonBuildStart ();

			dungeonBuilder.ApplyTheme(themes, sceneProvider);

			sceneProvider.OnDungeonBuildStop ();
		}

		DungeonEventListener[] GetListeners() {
			var listeners = GetComponents<DungeonEventListener>();

			var enabledListeners = from listener in listeners
					where listener.enabled
					select listener;

			return enabledListeners.ToArray();
		}

		void NotifyPostLayoutBuild() {
			// Notify all listeners of the post build event
			foreach (var listener in GetListeners()) {
				listener.OnPostDungeonLayoutBuild(this, ActiveModel);
			}
		}

		void NotifyPostBuild() {
			// Notify all listeners of the post build event
			foreach (var listener in GetListeners()) {
				listener.OnPostDungeonBuild(this, ActiveModel);
			}
		}

        void NotifyMarkersEmitted(List<PropSocket> markers)
        {
            // Notify all listeners of the post build event
            foreach (var listener in GetListeners())
            {
                if (listener == null) continue;
                listener.OnDungeonMarkersEmitted(this, ActiveModel, markers);
            }
        }

		void NotifyDungeonDestroyed() {
			// Notify all listeners that the dungeon is destroyed
			foreach (var listener in GetListeners()) {
				listener.OnDungeonDestroyed(this);
			}
		}

        /// <summary>
        /// Destroys the dungeon
        /// </summary>
		public void DestroyDungeon() {
            var itemList = GameObject.FindObjectsOfType<DungeonSceneProviderData>();
            var dungeonItems = new List<GameObject>();
            foreach (var item in itemList)
            {
                if (item == null) continue;
                if (item.dungeon == this)
                {
                    dungeonItems.Add(item.gameObject);
                }
            }
			foreach(var item in dungeonItems) {
				if (Application.isPlaying) {
					Destroy(item);
				} else {
					DestroyImmediate(item);
				}
			}
            
			if (dungeonModel != null) {
				dungeonModel.ResetModel();
			}

			if (dungeonBuilder != null) {
				dungeonBuilder.OnDestroyed();
			}
			NotifyDungeonDestroyed();
		}

		/// <summary>
		/// Requests the dungeon to be rebuilt in the next update phase
		/// </summary>
		public void RequestRebuild() {
			requestedRebuild = true;
		}


		void Update() {
			if (dungeonModel == null) return;
			
			if (requestedRebuild) {
				requestedRebuild = false;
				Build();
            }
            if (debugDraw)
            {
                DrawModel();
            }
        }

		void OnGUI()
        {
        }

		void DrawModel() {
            if (dungeonBuilder != null)
            {
                dungeonBuilder.DebugDraw();
            }
		}

        /// <summary>
        /// Registers a painted cell
        /// </summary>
        /// <param name="location">the location of the painted cell, in grid cooridnates</param>
        /// <param name="automaticRebuild">if true, the dungeon would be rebuilt, if the data model has changed due to this request</param>
		public void AddPaintCell(IntVector location, bool automaticRebuild) {
			bool overlappingCell = false;
			IntVector overlappingCellValue = new IntVector();
            var toolOverlayData = ActiveModel.ToolData;
            if (toolOverlayData == null) return;

			foreach (var cellData in toolOverlayData.PaintedCells) {
				if (cellData.x == location.x && cellData.z == location.z) {
					if (cellData.y != location.y) {
						overlappingCell = true;
						overlappingCellValue = cellData;
						break;
					}
					else {
						// Cell with this data already exists.  Ignore the request
						return;
					}
				}
			}
			if (overlappingCell) {
				toolOverlayData.PaintedCells.Remove(overlappingCellValue);
			}

			toolOverlayData.PaintedCells.Add(location);
			if (automaticRebuild) {
				Build();
			}
		}

        /// <summary>
        /// Remove a previous painted cell
        /// </summary>
        /// <param name="location">the location of the painted cell to remove, in grid cooridnates</param>
        /// <param name="automaticRebuild">if true, the dungeon would be rebuilt, if the data model has changed due to this request</param>
        public void RemovePaintCell(IntVector location, bool automaticRebuild)
        {
            var toolOverlayData = ActiveModel.ToolData;
            if (toolOverlayData == null) return;

			if (toolOverlayData.PaintedCells.Contains(location)) {
				toolOverlayData.PaintedCells.Remove(location);
				if (automaticRebuild) {
					Build ();
				}
			}
		}

        /// <summary>
        /// Clears all overlay data
        /// </summary>
        /// <param name="automaticRebuild"></param>
        public void ClearToolOverlayData(bool automaticRebuild)
        {
            var toolOverlayData = ActiveModel.ToolData;
            if (toolOverlayData == null) return;

			toolOverlayData.PaintedCells.Clear();
			if (automaticRebuild) {
				Build ();
			}
		}
	}
	
}
