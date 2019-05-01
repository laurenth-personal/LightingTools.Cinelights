using UnityEditor;
using UnityEngine;
using EditorLightUtilities;
using LightUtilities;

[CustomPropertyDrawer(typeof(CineLightParameters))]
public class CineLightParametersPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        EditorGUILayout.PropertyField(property.FindPropertyRelative("displayName"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("drawGizmo"));
        LightUIUtilities.DrawSplitter();

        EditorGUI.indentLevel--;
        LightUIUtilities.DrawHeader("Rig");
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(property.FindPropertyRelative("linkToCameraRotation"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Yaw"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Pitch"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("Roll"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("distance"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("offset"));

        EditorGUI.EndProperty();
    }
}