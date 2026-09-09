using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character.Controller {
    [CustomEditor(typeof(MMORPGController)), CanEditMultipleObjects]
    public class MMORPGControllerEditor : RPGControllerEditor {
        SerializedProperty Script;

        SerializedProperty AlignCameraOnCharacterMovement;

        public override void OnEnable() {
            base.OnEnable();

            Script = serializedObject.FindProperty("m_Script");

            AlignCameraOnCharacterMovement = serializedObject.FindProperty("AlignCameraOnCharacterMovement");
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

            ShowCameraSettings();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            base.ShowShakingSettings();

            serializedObject.ApplyModifiedProperties();
        }

        protected override void CameraSettings() {
            base.CameraSettings();
            EditorGUILayout.PropertyField(AlignCameraOnCharacterMovement, new GUIContent("Align On Character Movement"));
        }
    }
}