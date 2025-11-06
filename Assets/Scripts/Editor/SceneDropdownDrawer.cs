using UnityEditor;
using UnityEngine;
using HeistNSeek.Core;

namespace HeistNSeek.Editor
{
    [CustomPropertyDrawer(typeof(SceneDropdownAttribute))]
    public class SceneDropdownDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(position, label.text, "Use SceneDropdown with int fields only.");
                return;
            }

            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                EditorGUI.LabelField(position, label.text, "No scenes in Build Settings.");
                return;
            }

            // Build scene names array
            var sceneNames = new string[scenes.Length + 1];
            sceneNames[0] = "None";

            for (int i = 0; i < scenes.Length; i++)
            {
                var scenePath = scenes[i].path;
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                sceneNames[i + 1] = $"{i}: {sceneName}";
            }

            // Get current value and adjust for "None" option
            int currentIndex = property.intValue;
            int dropdownIndex = currentIndex + 1; // +1 because of "None" at index 0

            // Clamp to valid range
            if (dropdownIndex < 0 || dropdownIndex >= sceneNames.Length)
            {
                dropdownIndex = 0;
            }

            // Draw dropdown
            EditorGUI.BeginProperty(position, label, property);

            int newDropdownIndex = EditorGUI.Popup(position, label.text, dropdownIndex, sceneNames);

            // Convert back to scene index (-1 for "None", or actual index)
            int newSceneIndex = newDropdownIndex - 1;

            if (newSceneIndex != currentIndex)
            {
                property.intValue = newSceneIndex;
            }

            EditorGUI.EndProperty();
        }
    }
}
