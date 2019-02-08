//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEditor;
using UnityEngine;
using System.Collections;

namespace DungeonArchitect.Editors
{
	/*
	[CustomEditor(typeof(LandscapeTransformerGrid))]
	public class LandscapeTransformerGridEditor : Editor {
		SerializedObject sobject;
		
		SerializedProperty fillTexture;
		SerializedProperty roomTexture;
		SerializedProperty corridorTexture;
		SerializedProperty cliffTexture;
		
		public void OnEnable() {
			sobject = new SerializedObject(target);
			fillTexture = sobject.FindProperty("fillTexture");
			roomTexture = sobject.FindProperty("roomTexture");
			corridorTexture = sobject.FindProperty("corridorTexture");
			cliffTexture = sobject.FindProperty("cliffTexture");

		}
		
		public override void OnInspectorGUI()
		{
			sobject.Update();
			GUILayout.Label("Textures", EditorStyles.boldLabel);
			//ShowProperty(fillTexture);
			EditorGUILayout.PropertyField(fillTexture);

		}

		void ShowProperty(SerializedProperty property) {
			GUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			GUILayout.EndHorizontal();
		}
	}
	*/
}
