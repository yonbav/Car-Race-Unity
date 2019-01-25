//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using UnityEditor;

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using DungeonArchitect;
using DungeonArchitect.Utils;
using DungeonArchitect.Graphs;
using DungeonArchitect.Constraints;

namespace DungeonArchitect.Editors
{
    /// <summary>
    /// Custom property editor for visual nodes
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(VisualNode))]
    public class VisualNodeEditor : PlaceableNodeEditor
	{
        SerializedProperty IsStatic;
        SerializedProperty affectsNavigation;
        SerializedProperty useSpatialConstraint;
        InstanceCache instanceCache = new InstanceCache();

        public override void OnEnable()
        {
            base.OnEnable();
            drawOffset = true;
			drawAttachments = true;
            IsStatic = sobject.FindProperty("IsStatic");
            affectsNavigation = sobject.FindProperty("affectsNavigation");
            useSpatialConstraint = sobject.FindProperty("useSpatialConstraint");
        }

        protected override void DrawPreInspectorGUI()
		{
			EditorGUILayout.PropertyField(IsStatic);

			// affectsNavigation flag is only valid if the object is static.  So disable it if not static
			GUI.enabled = IsStatic.boolValue;
			EditorGUILayout.PropertyField(affectsNavigation);
			GUI.enabled = true;

            GUILayout.Space(CATEGORY_SPACING);
        }
        protected override void DrawPostInspectorGUI()
        {
            GUILayout.Label("Rules", EditorStyles.boldLabel);

            var meshNode = target as VisualNode;
            DrawRule<SelectorRule>(" Selection Rule", ref meshNode.selectionRuleEnabled, ref meshNode.selectionRuleClassName);
            DrawRule<TransformationRule>(" Transform Rule", ref meshNode.transformRuleEnabled, ref meshNode.transformRuleClassName);

            GUI.enabled = true;

            GUILayout.Space(CATEGORY_SPACING);

            DrawSpatialConstraintCategory();
        }

        void DrawSpatialConstraintCategory()
        {
            GUILayout.Label("SpatialConstraint", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(CATEGORY_SPACING);
            EditorGUILayout.BeginVertical();

            if (targets != null && targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Multiple objects selected. Please edit individually", MessageType.Info);
            }
            else
            {
                //EditorGUILayout.PropertyField(useSpatialConstraint);
                useSpatialConstraint.boolValue = EditorGUILayout.ToggleLeft(" Use Spatial Constraints", useSpatialConstraint.boolValue);

                {
                    var node = target as VisualNode;
                    bool oldRotateToFit = false;
                    bool oldApplyMarkerRotation = false;
                    if (node.spatialConstraint != null)
                    {
                        oldRotateToFit = node.spatialConstraint.rotateToFit;
                        oldApplyMarkerRotation = node.spatialConstraint.applyMarkerRotation;
                    }
                    bool rotateToFit = EditorGUILayout.ToggleLeft("Rotate to Fit", oldRotateToFit);
                    bool applyMarkerRotation = EditorGUILayout.ToggleLeft("Apply Marker Rotation", oldApplyMarkerRotation);
                    if (node.spatialConstraint != null)
                    {
                        if (rotateToFit != oldRotateToFit)
                        {
                            node.spatialConstraint.rotateToFit = rotateToFit;
                        }
                        if (applyMarkerRotation != oldApplyMarkerRotation)
                        {
                            node.spatialConstraint.applyMarkerRotation = applyMarkerRotation;
                        }
                    }
                }


                GUI.enabled = useSpatialConstraint.boolValue;

                System.Type[] constraintTypes = ReflectionUtils.GetAllSubtypes(typeof(SpatialConstraint), true);

                var visualNode = target as VisualNode;
                int index = 0;
                var constraintNames = new List<string>();
                constraintNames.Add("None");
                var constraint = visualNode.spatialConstraint;

                for (int i = 0; i < constraintTypes.Count(); i++)
                {
                    var type = constraintTypes[i];
                    if (constraint != null && constraint.GetType() == type)
                    {
                        index = i + 1;      // +1 to accommodate for the first "None" entry
                    }
                    constraintNames.Add(GetTypeName(type));
                }
                var newIndex = EditorGUILayout.Popup(index, constraintNames.ToArray());
                if (index != newIndex)
                {
                    // Destroy the old constraint
                    if (constraint != null)
                    {
                        DestroyImmediate(constraint, true);
                    }

                    index = newIndex;
                    if (index <= 0)
                    {
                        constraint = null;
                    }
                    else
                    {
                        var typeIndex = index - 1;  // -1 to accommodate for the first "None" entry
                        var constraintType = constraintTypes[typeIndex];
                        constraint = ScriptableObject.CreateInstance(constraintType) as SpatialConstraint;
                    }

                    //spatialConstraint.objectReferenceValue = constraint;  // Doesn't seem to work
                    visualNode.spatialConstraint = constraint;
                    if (constraint != null)
                    {
                        AssetDatabase.AddObjectToAsset(constraint, visualNode.Graph);
                        visualNode.Graph.MarkAsDirty();
                        visualNode.Graph.NotifyStateChanged();
                    }
                }

                DrawSpatialConstraint(constraint);
            }


			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			GUI.enabled = true;

            GUILayout.Space(CATEGORY_SPACING);
        }

        string GetTypeName(System.Type type)
        {
            var meta = System.Attribute.GetCustomAttribute(type, typeof(MetaAttribute)) as MetaAttribute;
            if (meta != null)
            {
                return meta.displayText;
            }
            return type.Name;
        }

        void DrawSpatialConstraint(SpatialConstraint constraint) {
            if (constraint == null) return;

            var editorTypes = ReflectionUtils.GetAllSubtypes(typeof(SpatialConstraintEditor), false);
            foreach (var editorType in editorTypes)
            {
                var editorAttribute = System.Attribute.GetCustomAttribute(editorType, typeof(ConstraintEditorAttribute)) as ConstraintEditorAttribute;
                if (editorAttribute != null)
                {
                    if (editorAttribute.constraintType == constraint.GetType())
                    {
                        var editor = ScriptableObject.CreateInstance(editorType) as SpatialConstraintEditor;
                        editor.DrawConstraintEditor(constraint);
                        ScriptableObject.DestroyImmediate(editor);
                        editor = null;
                        break;
                    }
                }
            }
        }

        void DrawRule<T>(string caption, ref bool ruleEnabled, ref string ruleClassName) where T : ScriptableObject
        {
            GUI.enabled = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(CATEGORY_SPACING);
            ruleEnabled = EditorGUILayout.ToggleLeft(caption, ruleEnabled);
            GUI.enabled = ruleEnabled;
            MonoScript script = null;
            if (ruleClassName != null)
            {
                var rule = instanceCache.GetInstance(ruleClassName) as ScriptableObject;
                if (rule != null)
                {
                    script = MonoScript.FromScriptableObject(rule);
                }
            }
            var oldScript = script;
            script = EditorGUILayout.ObjectField(script, typeof(MonoScript), false) as MonoScript;
            if (oldScript != script && script != null)
            {
                ruleClassName = script.GetClass().FullName;
            }
            else if (script == null)
            {
                ruleClassName = null;
            }

            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
        }
    }

    /// <summary>
    /// Renders a visual node
    /// </summary>
    public abstract class VisualNodeRenderer : GraphNodeRenderer
    {
		protected virtual void DrawFrameTexture(GraphRendererContext rendererContext, GraphNode node, GraphCamera camera) {
			DrawNodeTexture(rendererContext, node, camera, DungeonEditorResources.TEXTURE_GO_NODE_FRAME);
		}

		protected virtual void DrawBackgroundTexture(GraphRendererContext rendererContext, GraphNode node, GraphCamera camera) {
			DrawNodeTexture(rendererContext, node, camera, DungeonEditorResources.TEXTURE_GO_NODE_BG);
		}

		protected virtual void DrawThumbnail(GraphRendererContext rendererContext, GraphNode node, GraphCamera camera) {
			var thumbObject = GetThumbObject(node);
			var visualNode = node as VisualNode;
			var thumbnailSize = 96 / camera.ZoomLevel;
			if (thumbObject != null)
			{
				Texture texture = AssetThumbnailCache.Instance.GetThumb(thumbObject);
				if (texture != null)
				{
					var positionWorld = new Vector2(12, 12) + visualNode.Position;
					var positionScreen = camera.WorldToScreen(positionWorld);
					GUI.DrawTexture(new Rect(positionScreen.x, positionScreen.y, thumbnailSize, thumbnailSize), texture);
				}
			}
			else
			{
				DrawTextCentered(rendererContext, node, camera, "None");
			}
		}

        public override void Draw(GraphRendererContext rendererContext, GraphNode node, GraphCamera camera)
        {
			DrawBackgroundTexture(rendererContext, node, camera);

			DrawThumbnail(rendererContext, node, camera);

			DrawFrameTexture(rendererContext, node, camera);

            base.Draw(rendererContext, node, camera);

            if (node.Selected)
            {
                DrawNodeTexture(rendererContext, node, camera, DungeonEditorResources.TEXTURE_GO_NODE_SELECTION);
            }
        }

        protected abstract Object GetThumbObject(GraphNode node);
    }
}
