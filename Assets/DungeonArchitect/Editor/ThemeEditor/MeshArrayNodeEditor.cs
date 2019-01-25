//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;
using System.Collections;
using DungeonArchitect;
using DungeonArchitect.Utils;
using DungeonArchitect.Graphs;

namespace DungeonArchitect.Editors
{
	/// <summary>
	/// Custom property editors for GameObjectNode
	/// </summary>
	[CanEditMultipleObjects]
	[CustomEditor(typeof(GameObjectArrayNode))]
	public class MeshArrayNodeEditor : VisualNodeEditor
	{
		SerializedProperty Templates;

		public override void OnEnable()
		{
			base.OnEnable();
			drawOffset = true;
			drawAttachments = true;
			Templates = sobject.FindProperty("Templates");
		}

		protected override void DrawPreInspectorGUI()
		{
			GUILayout.Label("Game Object Array Node", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(Templates, true);

			base.DrawPreInspectorGUI();
		}
	}

	/// <summary>
	/// Renders a mesh node
	/// </summary>
	public class MeshArrayNodeRenderer : VisualNodeRenderer
	{
		protected override Object GetThumbObject(GraphNode node)
		{
			var meshNode = node as GameObjectArrayNode;
			if (meshNode == null || meshNode.Templates == null || meshNode.Templates.Length == 0) return null;
			return meshNode.Templates[0];
		}

		protected override void DrawFrameTexture(GraphRendererContext rendererContext, GraphNode node, GraphCamera camera) {
			DrawNodeTexture(rendererContext, node, camera, DungeonEditorResources.TEXTURE_GO_NODE_FRAME);
			DrawNodeTexture(rendererContext, node, camera, DungeonEditorResources.TEXTURE_MULTI_GO_NODE_FRAME);
		}
	}

}
