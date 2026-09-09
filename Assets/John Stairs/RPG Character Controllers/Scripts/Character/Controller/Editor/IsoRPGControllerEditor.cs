using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character.Controller {
    [CustomEditor(typeof(IsoRPGController)), CanEditMultipleObjects]
    public class IsoRPGControllerEditor : RPGControllerEditor {
        SerializedProperty Script;

        public override void OnEnable() {
            base.OnEnable();

            Script = serializedObject.FindProperty("m_Script");
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
    }
}