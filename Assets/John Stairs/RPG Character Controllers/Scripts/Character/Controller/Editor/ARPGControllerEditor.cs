using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character.Controller {
    [CustomEditor(typeof(ARPGController)), CanEditMultipleObjects]
    public class ARPGControllerEditor : RPGControllerEditor {
        SerializedProperty Script;

        SerializedProperty AlwaysOrbitCamera;

        public override void OnEnable() {
            base.OnEnable();

            Script = serializedObject.FindProperty("m_Script");

            AlwaysOrbitCamera = serializedObject.FindProperty("AlwaysOrbitCamera");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(Script);
            GUI.enabled = true;

            base.DrawComponentSetup();

            if (base.HasCombatSettings()) {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                base.ShowCombatSettings();
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            base.ShowCameraSettings();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            base.ShowShakingSettings();

            serializedObject.ApplyModifiedProperties();
        }

        protected override void CameraSettings() {
            base.CameraSettings();
            EditorGUILayout.PropertyField(AlwaysOrbitCamera, new GUIContent("Always Orbit Camera"));
        }
    }
}