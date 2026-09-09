using JohnStairs.RPG.Character.Combat;
using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character {
    [CustomEditor(typeof(AnimationHandler)), CanEditMultipleObjects]
    public class AnimationHandlerEditor : BaseEditor {
        protected static bool showComponentSetup = true;

        AnimationHandler Component;
        SerializedProperty Script;

        SerializedProperty MeleeCastTrigger;
        SerializedProperty MeleeCastFinishedTrigger;
        SerializedProperty RangedWeaponCastTrigger;
        SerializedProperty RangedWeaponCastFinishedTrigger;
        SerializedProperty CastAggressiveTrigger;
        SerializedProperty CastAggressiveFinishedTrigger;
        SerializedProperty CastDefensiveTrigger;
        SerializedProperty CastDefensiveFinishedTrigger;

        public void OnEnable() {
            Script = serializedObject.FindProperty("m_Script");
            Component = (AnimationHandler)serializedObject.targetObject;

            MeleeCastTrigger = serializedObject.FindProperty("MeleeCastTrigger");
            MeleeCastFinishedTrigger = serializedObject.FindProperty("MeleeCastFinishedTrigger");
            RangedWeaponCastTrigger = serializedObject.FindProperty("RangedWeaponCastTrigger");
            RangedWeaponCastFinishedTrigger = serializedObject.FindProperty("RangedWeaponCastFinishedTrigger");
            CastAggressiveTrigger = serializedObject.FindProperty("CastAggressiveTrigger");
            CastAggressiveFinishedTrigger = serializedObject.FindProperty("CastAggressiveFinishedTrigger");
            CastDefensiveTrigger = serializedObject.FindProperty("CastDefensiveTrigger");
            CastDefensiveFinishedTrigger = serializedObject.FindProperty("CastDefensiveFinishedTrigger");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(Script);
            GUI.enabled = true;

            DrawComponentSetup();

            if (Component.GetComponent<ICharacter>() != null) {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                EditorGUILayout.PropertyField(MeleeCastTrigger);
                EditorGUILayout.PropertyField(MeleeCastFinishedTrigger);
                EditorGUILayout.PropertyField(RangedWeaponCastTrigger);
                EditorGUILayout.PropertyField(RangedWeaponCastFinishedTrigger);
                EditorGUILayout.PropertyField(CastAggressiveTrigger);
                EditorGUILayout.PropertyField(CastAggressiveFinishedTrigger);
                EditorGUILayout.PropertyField(CastDefensiveTrigger);
                EditorGUILayout.PropertyField(CastDefensiveFinishedTrigger);
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawComponentSetup() {
            showComponentSetup = EditorGUILayout.BeginFoldoutHeaderGroup(showComponentSetup, "Subcomponents");
            if (showComponentSetup) {
                DrawSubcomponentStatus(Component, "Mount Animation Handler", typeof(IMountAnimationHandler), null);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}