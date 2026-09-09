using JohnStairs.RPG.Character.Cam.Subcomponents.Enums;
using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    [CustomEditor(typeof(ViewFrustum)), CanEditMultipleObjects]
    public class ViewFrustumEditor : Editor {
        SerializedProperty Script;
        SerializedProperty Shape;
        SerializedProperty RaysPerEdge;
        SerializedProperty CheckedLayers;
        SerializedProperty IgnoredTags;

        public void OnEnable() {
            Script = serializedObject.FindProperty("m_Script");
            Shape = serializedObject.FindProperty("Shape");
            RaysPerEdge = serializedObject.FindProperty("RaysPerEdge");
            CheckedLayers = serializedObject.FindProperty("CheckedLayers");
            IgnoredTags = serializedObject.FindProperty("IgnoredTags");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(Script);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(Shape);
            if (Shape.enumValueIndex == (int)FrustumShape.Pyramid) {
                EditorGUILayout.PropertyField(RaysPerEdge, new GUIContent("└ Rays Per Edge "));
            }
            EditorGUILayout.PropertyField(CheckedLayers);
            EditorGUILayout.PropertyField(IgnoredTags);

            serializedObject.ApplyModifiedProperties();
        }
    }
}