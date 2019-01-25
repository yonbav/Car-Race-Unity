//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;
using DungeonArchitect;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Custom property editor for volumes game objects
    /// </summary>
    [ExecuteInEditMode]
    public class VolumeEditor : Editor
    {
        IntVector positionOnGrid;
        IntVector sizeOnGrid;
        protected bool dynamicUpdate = true;
        protected bool onlyReapplyTheme = false;    // If true, Does not rebuild the layout and only applies the theme again over the existing layout

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Rebuild Dungeon"))
            {
                RequestRebuild(true);
            }
        }


        void OnEnable()
        {
            SceneView.onSceneGUIDelegate += OnUpdate;
        }

        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnUpdate;
        }

        public virtual void OnUpdate(SceneView sceneView)
        {
            if (dynamicUpdate)
            {
                var volume = target as Volume;
                if (volume != null)
                {
                    var transform = volume.gameObject.transform;
                    if (transform.hasChanged)
                    {
                        OnTransformModified(volume);
                        transform.hasChanged = false;
                    }
                }
            }
        }

        void RequestRebuild(bool force)
        {
            var volume = target as Volume;
            if (volume != null && volume.dungeon != null)
            {
                var dungeon = volume.dungeon;
                if (onlyReapplyTheme)
                {
                    dungeon.ReapplyTheme();
                }
                else
                {
                    if (force)
                    {
                        dungeon.Build();
                    }
                    else
                    {
                        dungeon.RequestRebuild();
                    }

                }
            }
        }

        protected void OnTransformModified(Volume volume)
        {
            if (volume == null || volume.dungeon == null)
            {
                return;
            }
            var builder = volume.dungeon.GetComponent<DungeonBuilder>();
            if (builder == null)
            {
                return;
            }

            IntVector newPositionOnGrid, newSizeOnGrid;
            builder.OnVolumePositionModified(volume, out newPositionOnGrid, out newSizeOnGrid);

            if (!positionOnGrid.Equals(newPositionOnGrid) || !sizeOnGrid.Equals(newSizeOnGrid))
            {
                positionOnGrid = newPositionOnGrid;
                sizeOnGrid = newSizeOnGrid;
                OnGridTransformModified();
            }

        }

        void OnGridTransformModified()
        {
            RequestRebuild(false);
        }
    }
}
