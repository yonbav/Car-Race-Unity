//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Custom property editor for Theme override volumes
    /// </summary>
    [CustomEditor(typeof(ThemeOverrideVolume))]
    public class ThemeOverrideVolumeEditor : VolumeEditor
    {

        public override void OnUpdate(SceneView sceneView)
        {
            onlyReapplyTheme = true;
            base.OnUpdate(sceneView);
        }
    }
}
