//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Custom property editor for Negation volumes
    /// </summary>
    [CustomEditor(typeof(MarkerReplaceVolume))]
    public class MarkerReplaceVolumeEditor : VolumeEditor
    {
        public override void OnUpdate(SceneView sceneView)
        {
            dynamicUpdate = false;
            onlyReapplyTheme = true;
            base.OnUpdate(sceneView);
        }
    }
}
