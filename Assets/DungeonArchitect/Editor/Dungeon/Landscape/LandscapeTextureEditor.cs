//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEditor;
using UnityEngine;
using System.Collections;
using DungeonArchitect.Terrains;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Custom property editor for the Landscape texture data-structure
    /// </summary>
	[CustomEditor(typeof(LandscapeTexture))]
	public class LandscapeTextureEditor : Editor {
		SerializedObject sobject;
		
		SerializedProperty diffuse;
		SerializedProperty normal;
		SerializedProperty metallic;
		SerializedProperty size;
		SerializedProperty offset;

		public void OnEnable() {
			sobject = new SerializedObject(target);
			diffuse = sobject.FindProperty("diffuse");
			normal = sobject.FindProperty("normal");
			metallic = sobject.FindProperty("metallic");
			size = sobject.FindProperty("size");
			offset = sobject.FindProperty("offset");
		}

		public override void OnInspectorGUI()
		{
			sobject.Update();
			EditorGUILayout.PropertyField(diffuse);
			EditorGUILayout.PropertyField(normal);
			EditorGUILayout.PropertyField(metallic);
			EditorGUILayout.PropertyField(size);
			EditorGUILayout.PropertyField(offset);
			sobject.ApplyModifiedProperties();
		}

	}
}
