using JohnStairs.RPG.Character.Combat;
using JohnStairs.RPG.Character.Controller.Subcomponents;
using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character.Controller {
    public abstract class RPGControllerEditor : BaseEditor {
        static bool showComponentSetup = true;
        static bool showCombatSettings = true;
        protected static bool showCameraControlSettings = true;
        static bool showShakingSettings = true;

        RPGController Component;

        #region Combat related variables
        SerializedProperty AreaSelectionByCursor;
        #endregion

        #region Camera related variables
        protected SerializedProperty InvertYawAxis;
        protected SerializedProperty InvertPitchAxis;
        protected SerializedProperty YawSensitivity;
        protected SerializedProperty PitchSensitivity;
        protected SerializedProperty ZoomSensitivity;
        protected SerializedProperty SyncRotations;
        #endregion        

        #region Shaking variables
        SerializedProperty ShakeFrequency;
        SerializedProperty ShakeAmplitude;
        SerializedProperty ShakeAmplitudeVariance;
        #endregion

        public virtual void OnEnable() {
            Component = (RPGController)serializedObject.targetObject;

            #region Combat related variables
            AreaSelectionByCursor = serializedObject.FindProperty("AreaSelectionByCursor");
            #endregion

            #region Camera related variables
            InvertYawAxis = serializedObject.FindProperty("InvertYawAxis");
            InvertPitchAxis = serializedObject.FindProperty("InvertPitchAxis");
            YawSensitivity = serializedObject.FindProperty("YawSensitivity");
            PitchSensitivity = serializedObject.FindProperty("PitchSensitivity");
            ZoomSensitivity = serializedObject.FindProperty("ZoomSensitivity");
            SyncRotations = serializedObject.FindProperty("SyncRotations");
            #endregion

            #region Shaking variables
            ShakeFrequency = serializedObject.FindProperty("ShakeFrequency");
            ShakeAmplitude = serializedObject.FindProperty("ShakeAmplitude");
            ShakeAmplitudeVariance = serializedObject.FindProperty("ShakeAmplitudeVariance");
            #endregion
        }

        protected void DrawComponentSetup() {
            showComponentSetup = EditorGUILayout.BeginFoldoutHeaderGroup(showComponentSetup, "Subcomponents");
            if (showComponentSetup) {
                DrawInfoMessageBox("Controller subcomponents found on this game object");
                DrawSubcomponentStatus(Component, "Input Handler", typeof(IInputHandler), typeof(InputHandler));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected bool HasCombatSettings() {
            return Component.GetComponent<ICharacter>() != null;
        }

        protected void ShowCombatSettings() {
            showCombatSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showCombatSettings, "Combat");
            if (showCombatSettings) {
                EditorGUILayout.PropertyField(AreaSelectionByCursor);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected void ShowCameraSettings() {
            showCameraControlSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showCameraControlSettings, "Camera control");
            if (showCameraControlSettings) {
                CameraSettings();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected virtual void CameraSettings() {
            EditorGUILayout.PropertyField(InvertYawAxis);
            EditorGUILayout.PropertyField(YawSensitivity);
            EditorGUILayout.PropertyField(InvertPitchAxis);
            EditorGUILayout.PropertyField(PitchSensitivity);
            EditorGUILayout.PropertyField(ZoomSensitivity);
            EditorGUILayout.PropertyField(SyncRotations);
        }

        protected void ShowShakingSettings() {
            showShakingSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showShakingSettings, "Camera shaking");
            if (showShakingSettings) {
                EditorGUILayout.PropertyField(ShakeFrequency);
                EditorGUILayout.PropertyField(ShakeAmplitude);
                EditorGUILayout.PropertyField(ShakeAmplitudeVariance);
                string buttonText = "Enter play mode first";
                if (Application.isPlaying) {
                    buttonText = Component.IsShakingCamera() ? "Stop simulation" : "Start simulation";
                }
                if (GUILayout.Button(buttonText)) {
                    ToggleShakingSimulation(Component);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected void ToggleShakingSimulation(RPGController rpgController) {
            if (!Application.isPlaying) {
                Debug.Log("You have to enter play mode first to be able to simulate camera shaking");
                return;
            }

            if (rpgController.IsShakingCamera()) {
                rpgController.StopShakingCamera();
            } else {
                rpgController.StartShakingCamera(ShakeFrequency.floatValue, ShakeAmplitude.floatValue, ShakeAmplitudeVariance.floatValue);
            }
        }
    }
}