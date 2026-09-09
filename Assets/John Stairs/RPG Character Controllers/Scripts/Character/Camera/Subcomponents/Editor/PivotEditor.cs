using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    [CustomEditor(typeof(Pivot)), CanEditMultipleObjects]
    public class PivotEditor : Editor {
        Pivot Component;
        SerializedProperty Script;
        SerializedProperty LocalPosition;
        SerializedProperty EnableEvasion;
        SerializedProperty EvasionSmoothTime;
        SerializedProperty PositionSmoothTime;

        public void OnEnable() {
            Component = (Pivot)serializedObject.targetObject;
            Script = serializedObject.FindProperty("m_Script");
            LocalPosition = serializedObject.FindProperty("LocalPosition");
            EnableEvasion = serializedObject.FindProperty("EnableEvasion");
            EvasionSmoothTime = serializedObject.FindProperty("EvasionSmoothTime");
            PositionSmoothTime = serializedObject.FindProperty("PositionSmoothTime");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(Script);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(LocalPosition);
            bool internalPivot = Component.IsInternal();
            EditorGUILayout.LabelField("└> " + (internalPivot ? "Internal pivot logic applies" : "External pivot logic applies"));
            if (internalPivot) {
                EditorGUILayout.PropertyField(EnableEvasion, new GUIContent("└ Enable Evasive Pivot "));
                if (EnableEvasion.boolValue) {
                    EditorGUILayout.PropertyField(EvasionSmoothTime, new GUIContent("   └ Smooth Time "));
                }
            }
            EditorGUILayout.PropertyField(PositionSmoothTime);

            serializedObject.ApplyModifiedProperties();
        }
    }
}