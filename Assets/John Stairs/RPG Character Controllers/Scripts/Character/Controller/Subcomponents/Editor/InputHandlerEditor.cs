using JohnStairs.RPG.Character.Combat;
using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character.Controller.Subcomponents {
    [CustomEditor(typeof(InputHandler)), CanEditMultipleObjects]
    public class InputHandlerEditor : Editor {
        static bool showCameraVariables = true;
        static bool showMotorVariables = true;

        InputHandler Component;
        SerializedProperty Script;

        #region Camera related variables
        SerializedProperty _ActivateOrbitingStart;
        SerializedProperty _ActivateOrbitingStop;
        SerializedProperty _OrbitingAmount;
        SerializedProperty _ZoomAmount;
        SerializedProperty _MinDistanceZoom;
        SerializedProperty _MaxDistanceZoom;
        SerializedProperty _CursorPosition;
        SerializedProperty _ToggleCursorVisibility;
        #endregion        

        #region Motor related variables
        SerializedProperty _Movement;
        SerializedProperty _MoveForward;
        SerializedProperty _Rotate;
        SerializedProperty _Jump;
        SerializedProperty _Sprint;
        SerializedProperty _ToggleWalking;
        SerializedProperty _ToggleCrouching;
        SerializedProperty _ToggleAutorunning;
        SerializedProperty _AlignWithCamera;
        SerializedProperty _ToggleFlyingAbility;
        #endregion

        #region Combat related variables
        SerializedProperty _Select;
        SerializedProperty _Cancel;
        SerializedProperty _ActionBarSlots;
        SerializedProperty _LockOnTarget;
        #endregion

        public virtual void OnEnable() {
            Script = serializedObject.FindProperty("m_Script");
            Component = (InputHandler)serializedObject.targetObject;

            #region Camera related variables            
            _ActivateOrbitingStart = serializedObject.FindProperty("_ActivateOrbitingStart");
            _ActivateOrbitingStop = serializedObject.FindProperty("_ActivateOrbitingStop");
            _OrbitingAmount = serializedObject.FindProperty("_OrbitingAmount");
            _ZoomAmount = serializedObject.FindProperty("_ZoomAmount");
            _MinDistanceZoom = serializedObject.FindProperty("_MinDistanceZoom");
            _MaxDistanceZoom = serializedObject.FindProperty("_MaxDistanceZoom");
            _CursorPosition = serializedObject.FindProperty("_CursorPosition");
            _ToggleCursorVisibility = serializedObject.FindProperty("_ToggleCursorVisibility");
            #endregion

            #region Motor related variables
            _Movement = serializedObject.FindProperty("_Movement");
            _MoveForward = serializedObject.FindProperty("_MoveForward");
            _Rotate = serializedObject.FindProperty("_Rotate");
            _Jump = serializedObject.FindProperty("_Jump");
            _Sprint = serializedObject.FindProperty("_Sprint");
            _ToggleWalking = serializedObject.FindProperty("_ToggleWalking");
            _ToggleCrouching = serializedObject.FindProperty("_ToggleCrouching");
            _ToggleAutorunning = serializedObject.FindProperty("_ToggleAutorunning");
            _AlignWithCamera = serializedObject.FindProperty("_AlignWithCamera");
            _ToggleFlyingAbility = serializedObject.FindProperty("_ToggleFlyingAbility");
            #endregion

            #region Combat related variables
            _Select = serializedObject.FindProperty("_Select");
            _Cancel = serializedObject.FindProperty("_Cancel");
            _ActionBarSlots = serializedObject.FindProperty("_ActionBarSlots");
            _LockOnTarget = serializedObject.FindProperty("_LockOnTarget");
            #endregion
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(Script);
            GUI.enabled = true;

            ShowCheckInputSetup();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            #region Camera related settings
            showCameraVariables = EditorGUILayout.BeginFoldoutHeaderGroup(showCameraVariables, "Camera input values");
            if (showCameraVariables) {
                EditorGUILayout.PropertyField(_ActivateOrbitingStart);
                EditorGUILayout.PropertyField(_ActivateOrbitingStop);
                EditorGUILayout.PropertyField(_OrbitingAmount);
                EditorGUILayout.PropertyField(_ZoomAmount);
                EditorGUILayout.PropertyField(_MinDistanceZoom);
                EditorGUILayout.PropertyField(_MaxDistanceZoom);
                EditorGUILayout.PropertyField(_CursorPosition);
                EditorGUILayout.PropertyField(_ToggleCursorVisibility);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            #region Motor related settings
            showMotorVariables = EditorGUILayout.BeginFoldoutHeaderGroup(showMotorVariables, "Motor input values");
            if (showMotorVariables) {
                EditorGUILayout.PropertyField(_Movement);
                EditorGUILayout.PropertyField(_MoveForward);
                EditorGUILayout.PropertyField(_Rotate);
                EditorGUILayout.PropertyField(_Jump);
                EditorGUILayout.PropertyField(_Sprint);
                EditorGUILayout.PropertyField(_ToggleWalking);
                EditorGUILayout.PropertyField(_ToggleCrouching);
                EditorGUILayout.PropertyField(_ToggleAutorunning);
                EditorGUILayout.PropertyField(_AlignWithCamera);
                EditorGUILayout.PropertyField(_ToggleFlyingAbility);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion

            #region Combat related settings
            if (HasCombatSettings()) {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                EditorGUILayout.LabelField("Combat input values", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_Select);
                EditorGUILayout.PropertyField(_Cancel);
                EditorGUILayout.PropertyField(_ActionBarSlots);
                EditorGUILayout.PropertyField(_LockOnTarget);
            }
            #endregion

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual bool HasCombatSettings() {
            return Component.GetComponent<ICharacter>() != null;
        }

        protected virtual void ShowCheckInputSetup() {
            if (GUILayout.Button("Check input setup")) {
                Debug.Log("> Input setup check started");
                Component.InitializeInputActions(true);
                Debug.Log("> Input setup check done. If there were no warnings logged, all inputs could be found");
            }
        }
    }
}