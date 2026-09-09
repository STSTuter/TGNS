using JohnStairs.RPG.Character.Motor.Subcomponents;
using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character.Motor {
    [CustomEditor(typeof(RPGMotor)), CanEditMultipleObjects]
    public class RPGMotorEditor : BaseEditor {
        protected static bool showComponentSetup = true;

        RPGMotor Component;
        SerializedProperty Script;

        SerializedProperty WalkableLayers;
        SerializedProperty GroundedTolerance;
        SerializedProperty DefaultSpeed;
        SerializedProperty StrafeSpeed;
        SerializedProperty BackwardsSpeedMultiplier;
        SerializedProperty SprintSpeedMultiplier;
        SerializedProperty WalkSpeed;
        SerializedProperty CrouchSpeed;
        SerializedProperty FlyingSpeed;
        SerializedProperty MovementSpeedChangeRate;
        SerializedProperty JumpHeight;
        SerializedProperty EnableMidairJumps;
        SerializedProperty AllowedMidairJumps;
        SerializedProperty AllowMidairMovement;
        SerializedProperty CarrierAffectsJumps;
        SerializedProperty RotationSpeed;
        SerializedProperty AlignmentTime;
        SerializedProperty Gravity;
        SerializedProperty GroundStickiness;

        public void OnEnable() {
            Script = serializedObject.FindProperty("m_Script");
            Component = (RPGMotor)serializedObject.targetObject;

            WalkableLayers = serializedObject.FindProperty("WalkableLayers");
            GroundedTolerance = serializedObject.FindProperty("GroundedTolerance");
            DefaultSpeed = serializedObject.FindProperty("DefaultSpeed");
            StrafeSpeed = serializedObject.FindProperty("StrafeSpeed");
            BackwardsSpeedMultiplier = serializedObject.FindProperty("BackwardsSpeedMultiplier");
            SprintSpeedMultiplier = serializedObject.FindProperty("SprintSpeedMultiplier");
            FlyingSpeed = serializedObject.FindProperty("FlyingSpeed");
            WalkSpeed = serializedObject.FindProperty("WalkSpeed");
            CrouchSpeed = serializedObject.FindProperty("CrouchSpeed");
            MovementSpeedChangeRate = serializedObject.FindProperty("MovementSpeedChangeRate");
            JumpHeight = serializedObject.FindProperty("JumpHeight");
            EnableMidairJumps = serializedObject.FindProperty("EnableMidairJumps");
            AllowedMidairJumps = serializedObject.FindProperty("AllowedMidairJumps");
            AllowMidairMovement = serializedObject.FindProperty("AllowMidairMovement");
            CarrierAffectsJumps = serializedObject.FindProperty("CarrierAffectsJumps");
            RotationSpeed = serializedObject.FindProperty("RotationSpeed");
            AlignmentTime = serializedObject.FindProperty("AlignmentTime");
            Gravity = serializedObject.FindProperty("Gravity");
            GroundStickiness = serializedObject.FindProperty("GroundStickiness");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(Script);
            GUI.enabled = true;

            DrawComponentSetup();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.PropertyField(WalkableLayers);
            EditorGUILayout.PropertyField(GroundedTolerance);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Movement speed", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(DefaultSpeed);
            EditorGUILayout.PropertyField(StrafeSpeed);
            EditorGUILayout.PropertyField(BackwardsSpeedMultiplier);
            EditorGUILayout.PropertyField(SprintSpeedMultiplier);
            EditorGUILayout.PropertyField(WalkSpeed);
            EditorGUILayout.PropertyField(CrouchSpeed);
            EditorGUILayout.PropertyField(FlyingSpeed);
            EditorGUILayout.PropertyField(MovementSpeedChangeRate, new GUIContent("Speed Change Rate"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Jumping", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(JumpHeight);
            EditorGUILayout.PropertyField(EnableMidairJumps);
            if (EnableMidairJumps.boolValue) {
                EditorGUILayout.PropertyField(AllowedMidairJumps);
            }
            EditorGUILayout.PropertyField(AllowMidairMovement);
            EditorGUILayout.PropertyField(CarrierAffectsJumps);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(RotationSpeed);
            EditorGUILayout.PropertyField(AlignmentTime);
            EditorGUILayout.PropertyField(Gravity);
            EditorGUILayout.PropertyField(GroundStickiness);

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawComponentSetup() {
            showComponentSetup = EditorGUILayout.BeginFoldoutHeaderGroup(showComponentSetup, "Subcomponents");
            if (showComponentSetup) {
                DrawInfoMessageBox("Motor subcomponents found on this game object. Add the desired subcomponents to change motor behavior");
                DrawSubcomponentStatus(Component, "Sliding Handler", typeof(ISlidingHandler), typeof(SlidingHandler));
                DrawSubcomponentStatus(Component, "Swimming Handler", typeof(ISwimmingHandler), typeof(SwimmingHandler));
                DrawSubcomponentStatus(Component, "Climbing Handler", typeof(IClimbingHandler), typeof(ClimbingHandler));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}