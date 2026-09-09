using JohnStairs.RPG.Character.Cam.Subcomponents;
using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam {
    [CustomEditor(typeof(RPGCamera)), CanEditMultipleObjects]
    public class RPGCameraEditor : RPGCameraLiteEditor {
        protected override void DrawComponentSetup() {
            showComponentSetup = EditorGUILayout.BeginFoldoutHeaderGroup(showComponentSetup, "Subcomponents");
            if (showComponentSetup) {
                DrawInfoMessageBox("Camera subcomponents found on this game object. Add the desired subcomponents to change camera behavior");
                DrawSubcomponentStatus("Pivot", typeof(IPivot), typeof(Pivot));
                DrawSubcomponentStatus("View Frustum", typeof(IViewFrustum), typeof(ViewFrustum));
                DrawSubcomponentStatus("Occlusion Handler", typeof(IOcclusionHandler), typeof(OcclusionHandler));
                DrawSubcomponentStatus("Character Fading Handler", typeof(ICharacterFadingHandler), typeof(CharacterFadingHandler));
                DrawSubcomponentStatus("Look Up Behavior", typeof(ILookUpBehavior), typeof(LookUpBehavior));
                DrawSubcomponentStatus("Underwater Handler", typeof(IUnderwaterHandler), typeof(UnderwaterHandler));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}