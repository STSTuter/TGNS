using JohnStairs.RPG.Character.Motor.Subcomponents;
using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character.Motor.Subcomponents {
    [CustomEditor(typeof(ClimbingHandler)), CanEditMultipleObjects]
    public class ClimbingHandlerEditor : BaseEditor {
        SerializedProperty Script;

        SerializedProperty ClimbableLayers;
        SerializedProperty ClimbingSpeed;
        SerializedProperty HandsGrabHeight;
        SerializedProperty FeetGrabHeight;
        SerializedProperty GrabRange;
        SerializedProperty GrabDiameter;
        SerializedProperty MaxGrabDistance;
        SerializedProperty LedgeGrabCooldown;
        SerializedProperty LedgePullUpSpeed;
        SerializedProperty LedgePullUpCheckHeight;
        SerializedProperty DrawLedgeGrabGizmos;
        SerializedProperty DrawClimbingGizmos;
        SerializedProperty DrawLedgePullUpGizmos;

        public void OnEnable() {
            Script = serializedObject.FindProperty("m_Script");

            ClimbableLayers = serializedObject.FindProperty("ClimbableLayers");
            ClimbingSpeed = serializedObject.FindProperty("ClimbingSpeed");
            HandsGrabHeight = serializedObject.FindProperty("HandsGrabHeight");
            FeetGrabHeight = serializedObject.FindProperty("FeetGrabHeight");
            GrabRange = serializedObject.FindProperty("GrabRange");
            GrabDiameter = serializedObject.FindProperty("GrabDiameter");
            MaxGrabDistance = serializedObject.FindProperty("MaxGrabDistance");
            LedgeGrabCooldown = serializedObject.FindProperty("LedgeGrabCooldown");
            LedgePullUpSpeed = serializedObject.FindProperty("LedgePullUpSpeed");
            LedgePullUpCheckHeight = serializedObject.FindProperty("LedgePullUpCheckHeight");
            DrawLedgeGrabGizmos = serializedObject.FindProperty("DrawLedgeGrabGizmos");
            DrawClimbingGizmos = serializedObject.FindProperty("DrawClimbingGizmos");
            DrawLedgePullUpGizmos = serializedObject.FindProperty("DrawLedgePullUpGizmos");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(Script);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(ClimbableLayers);
            EditorGUILayout.PropertyField(ClimbingSpeed);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Check variables", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(HandsGrabHeight);
            EditorGUILayout.PropertyField(FeetGrabHeight);
            EditorGUILayout.PropertyField(GrabRange);
            EditorGUILayout.PropertyField(GrabDiameter);
            EditorGUILayout.PropertyField(MaxGrabDistance);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Ledge grabbing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(LedgeGrabCooldown);
            EditorGUILayout.PropertyField(LedgePullUpSpeed);
            EditorGUILayout.PropertyField(LedgePullUpCheckHeight);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Setup gizmos", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(DrawLedgeGrabGizmos);
            EditorGUILayout.PropertyField(DrawClimbingGizmos);
            EditorGUILayout.PropertyField(DrawLedgePullUpGizmos);

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}