using JohnStairs.RPG.Character.Cam.Subcomponents;
using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam {
    [CustomEditor(typeof(RPGCameraLite)), CanEditMultipleObjects]
    public class RPGCameraLiteEditor : Editor {
        protected static bool showComponentSetup = true;
        static bool showGeneralSettings = true;
        static bool showYawSettings = true;
        static bool showPitchSettings = true;
        static bool showDistanceSettings = true;

        RPGCameraLite Component;
        SerializedProperty Script;

        SerializedProperty UsedCamera;
        SerializedProperty ViewportMargin;
        SerializedProperty RotationSmoothTime;
        SerializedProperty DistanceSmoothing;
        SerializedProperty DistanceSmoothTime;
        SerializedProperty MinPitch;
        SerializedProperty MaxPitch;
        SerializedProperty MinDistance;
        SerializedProperty MaxDistance;
        SerializedProperty LockYaw;
        SerializedProperty LockPitch;
        SerializedProperty LockDistance;
        SerializedProperty StartYaw;
        SerializedProperty StartYawRelativeToCharacterRotation;
        SerializedProperty StartPitch;
        SerializedProperty StartDistance;

        public void OnEnable() {
            Script = serializedObject.FindProperty("m_Script");
            Component = (RPGCameraLite)serializedObject.targetObject;

            #region General variables
            UsedCamera = serializedObject.FindProperty("UsedCamera");
            ViewportMargin = serializedObject.FindProperty("ViewportMargin");
            RotationSmoothTime = serializedObject.FindProperty("RotationSmoothTime");
            #endregion

            #region Yaw variables
            StartYaw = serializedObject.FindProperty("StartYaw");
            StartYawRelativeToCharacterRotation = serializedObject.FindProperty("StartYawRelativeToCharacterRotation");
            LockYaw = serializedObject.FindProperty("LockYaw");
            #endregion

            #region Pitch variables
            LockPitch = serializedObject.FindProperty("LockPitch");
            MinPitch = serializedObject.FindProperty("MinPitch");
            MaxPitch = serializedObject.FindProperty("MaxPitch");
            StartPitch = serializedObject.FindProperty("StartPitch");
            #endregion

            #region Distance variables
            DistanceSmoothing = serializedObject.FindProperty("DistanceSmoothing");
            DistanceSmoothTime = serializedObject.FindProperty("DistanceSmoothTime");
            MinDistance = serializedObject.FindProperty("MinDistance");
            MaxDistance = serializedObject.FindProperty("MaxDistance");
            StartDistance = serializedObject.FindProperty("StartDistance");
            LockDistance = serializedObject.FindProperty("LockDistance");
            #endregion
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(Script);
            GUI.enabled = true;

            #region Component setup
            DrawComponentSetup();
            #endregion

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            #region General settings
            showGeneralSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneralSettings, "General");
            if (showGeneralSettings) {
                EditorGUILayout.PropertyField(UsedCamera);
                EditorGUILayout.PropertyField(ViewportMargin);
                EditorGUILayout.PropertyField(RotationSmoothTime);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            #region Yaw settings
            showYawSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showYawSettings, "Yaw");
            if (showYawSettings) {
                EditorGUILayout.PropertyField(StartYaw);
                EditorGUILayout.PropertyField(StartYawRelativeToCharacterRotation, new GUIContent("└ Relative To Character"));
                EditorGUILayout.PropertyField(LockYaw);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            #region Pitch settings
            showPitchSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showPitchSettings, "Pitch");
            if (showPitchSettings) {
                EditorGUILayout.PropertyField(StartPitch);
                EditorGUILayout.PropertyField(LockPitch);
                EditorGUILayout.PropertyField(MinPitch);
                EditorGUILayout.PropertyField(MaxPitch);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            #region Distance settings
            showDistanceSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDistanceSettings, "Distance");
            if (showDistanceSettings) {
                EditorGUILayout.PropertyField(StartDistance);
                EditorGUILayout.PropertyField(LockDistance);
                EditorGUILayout.PropertyField(DistanceSmoothing);
                EditorGUILayout.PropertyField(DistanceSmoothTime);
                EditorGUILayout.PropertyField(MinDistance);
                EditorGUILayout.PropertyField(MaxDistance);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawSubcomponentStatus(string componentName, System.Type componentType, System.Type defaultComponent) {
            bool componentFound = Component.GetComponent(componentType) != null;
            Color temp = EditorStyles.label.normal.textColor;
            EditorStyles.label.normal.textColor = componentFound ? Color.green : Color.yellow;

            string label = "<" + componentName + ">" + (componentFound ? " found" : " not found");
            if (componentFound) {
                EditorGUILayout.LabelField(label);
            } else {
                EditorGUILayout.LabelField(label);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("\t");
                if (GUILayout.Button("Add default")) {
                    if (defaultComponent == null) {
                        Debug.LogWarning("No default \"" + componentName + "\" subcomponent found. It might only be available with the full version of this asset");
                    } else {
                        Component.gameObject.AddComponent(defaultComponent);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorStyles.label.normal.textColor = temp;
        }

        protected virtual void DrawInfoMessageBox(string message) {
            GUIStyle style = new GUIStyle(EditorStyles.textArea) {
                wordWrap = true
            };
            EditorGUILayout.LabelField(message, style);
        }

        protected virtual void DrawComponentSetup() {
            showComponentSetup = EditorGUILayout.BeginFoldoutHeaderGroup(showComponentSetup, "Subcomponents");
            if (showComponentSetup) {
                DrawInfoMessageBox("Camera subcomponents found on this game object. Add the desired subcomponents to change camera behavior");
                DrawSubcomponentStatus("Pivot", typeof(IPivot), typeof(PivotLite));
                DrawSubcomponentStatus("View Frustum", typeof(IViewFrustum), typeof(ViewFrustum));
                DrawSubcomponentStatus("Occlusion Handler", typeof(IOcclusionHandler), typeof(OcclusionHandlerLite));
                DrawSubcomponentStatus("Character Fading Handler", typeof(ICharacterFadingHandler), null);
                DrawSubcomponentStatus("Look Up Behavior", typeof(ILookUpBehavior), null);
                DrawSubcomponentStatus("Underwater Handler", typeof(IUnderwaterHandler), typeof(UnderwaterHandlerLite));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}