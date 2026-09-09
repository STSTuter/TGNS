using UnityEditor;
using UnityEngine;

namespace JohnStairs.RPG.Character {
    public abstract class BaseEditor : Editor {
        protected virtual void DrawSubcomponentStatus(MonoBehaviour component, string componentName, System.Type componentType, System.Type defaultComponent) {
            bool componentFound = component.GetComponentInChildren(componentType) != null;
            Color temp = EditorStyles.label.normal.textColor;
            EditorStyles.label.normal.textColor = componentFound ? Color.green : Color.yellow;

            string label = "<" + componentName + ">" + (componentFound ? " found" : " not found");
            EditorGUILayout.LabelField(label);

            if (!componentFound
                && defaultComponent != null
                && GUILayout.Button("Add default")) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("\t");
                component.gameObject.AddComponent(defaultComponent);
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
    }
}