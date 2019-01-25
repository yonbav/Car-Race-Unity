//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEditor;
using UnityEngine;
using System.Collections;
using DungeonArchitect.Builders.Grid;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Custom property editor for the grid based dungeon configuration
    /// </summary>
	[CustomEditor(typeof(GridDungeonConfig))]
	public class GridDungeonConfigEditor : Editor {
		SerializedObject sobject;
		SerializedProperty Seed;
		SerializedProperty NumCells;
		SerializedProperty MinCellSize;
		SerializedProperty MaxCellSize;
		SerializedProperty RoomAreaThreshold;
		//SerializedProperty FloorHeight;
		SerializedProperty RoomAspectDelta;
		SerializedProperty CorridorPadding;
		SerializedProperty CorridorPaddingDoubleSided;
		SerializedProperty InitialRoomRadius;
		SerializedProperty SpanningTreeLoopProbability;
        SerializedProperty StairConnectionTollerance;
        SerializedProperty HeightVariationProbability;
		SerializedProperty NormalMean;
		SerializedProperty NormalStd;
		SerializedProperty GridCellSize;
		SerializedProperty MaxAllowedStairHeight;
		SerializedProperty Mode2D;
        SerializedProperty DoorProximitySteps;

        public void OnEnable() {
			sobject = new SerializedObject(target);
			Seed = sobject.FindProperty("Seed");
			NumCells = sobject.FindProperty("NumCells");
			MinCellSize = sobject.FindProperty("MinCellSize");
			MaxCellSize = sobject.FindProperty("MaxCellSize");
			RoomAreaThreshold = sobject.FindProperty("RoomAreaThreshold");
			//FloorHeight = sobject.FindProperty("FloorHeight");
			RoomAspectDelta = sobject.FindProperty("RoomAspectDelta");
			CorridorPadding = sobject.FindProperty("CorridorPadding");
			CorridorPaddingDoubleSided = sobject.FindProperty("CorridorPaddingDoubleSided");
			InitialRoomRadius = sobject.FindProperty("InitialRoomRadius");
			SpanningTreeLoopProbability = sobject.FindProperty("SpanningTreeLoopProbability");
			StairConnectionTollerance = sobject.FindProperty("StairConnectionTollerance");
			HeightVariationProbability = sobject.FindProperty("HeightVariationProbability");
			NormalMean = sobject.FindProperty("NormalMean");
			NormalStd = sobject.FindProperty("NormalStd");
			GridCellSize = sobject.FindProperty("GridCellSize");
			MaxAllowedStairHeight = sobject.FindProperty("MaxAllowedStairHeight");
            Mode2D = sobject.FindProperty("Mode2D");
            DoorProximitySteps = sobject.FindProperty("DoorProximitySteps");
        }

		public override void OnInspectorGUI()
		{
			sobject.Update();
			GUILayout.Label("Core Config", EditorStyles.boldLabel);
			// Core
			GUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(Seed);
			if (GUILayout.Button("R", GUILayout.Width(20), GUILayout.MaxHeight(15))) {
				RandomizeSeed();
			}
			GUILayout.EndHorizontal();

			EditorGUILayout.PropertyField(NumCells);
			EditorGUILayout.PropertyField(GridCellSize);

			// Cell dimensions
			GUILayout.Label("Cell Dimensions", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(MinCellSize);
			EditorGUILayout.PropertyField(MaxCellSize);
			EditorGUILayout.PropertyField(RoomAreaThreshold);
			EditorGUILayout.PropertyField(RoomAspectDelta);
			EditorGUILayout.PropertyField(CorridorPadding);
			EditorGUILayout.PropertyField(CorridorPaddingDoubleSided);

			// Height variations
			GUILayout.Label("Height Variations", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(HeightVariationProbability);
			EditorGUILayout.PropertyField(MaxAllowedStairHeight);
			EditorGUILayout.PropertyField(StairConnectionTollerance);
			EditorGUILayout.PropertyField(SpanningTreeLoopProbability);

			// Misc
			GUILayout.Label("Misc", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(Mode2D);
			EditorGUILayout.PropertyField(NormalMean);
			EditorGUILayout.PropertyField(NormalStd);
			EditorGUILayout.PropertyField(InitialRoomRadius);

            GUILayout.Label("Experimental", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(DoorProximitySteps);

            //EditorGUILayout.PropertyField(FloorHeight);

            sobject.ApplyModifiedProperties();
		}

		void RandomizeSeed() {
			Seed.intValue = Mathf.RoundToInt(Random.value * int.MaxValue);
		}
	}

}
