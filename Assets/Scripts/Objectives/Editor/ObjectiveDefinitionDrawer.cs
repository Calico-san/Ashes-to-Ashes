using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ObjectiveDefinition))]
public class ObjectiveDefinitionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        SerializedProperty title = property.FindPropertyRelative("title");

        line.height = EditorGUI.GetPropertyHeight(title);
        EditorGUI.PropertyField(line, title);
        line.y += line.height + EditorGUIUtility.standardVerticalSpacing;
        line.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(line, property.FindPropertyRelative("type"));
        line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(line, property.FindPropertyRelative("targetValue"));
        line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        ObjectiveType type = (ObjectiveType)property.FindPropertyRelative("type").enumValueIndex;
        if (type == ObjectiveType.BuildBuilding)
            EditorGUI.PropertyField(line, property.FindPropertyRelative("buildingType"));
        else if (type == ObjectiveType.ReachResourceAmount)
            EditorGUI.PropertyField(line, property.FindPropertyRelative("resourceType"));

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ObjectiveType type = (ObjectiveType)property.FindPropertyRelative("type").enumValueIndex;
        int remainingLines = type == ObjectiveType.BuildBuilding || type == ObjectiveType.ReachResourceAmount ? 3 : 2;
        return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("title"))
             + EditorGUIUtility.standardVerticalSpacing
             + remainingLines * EditorGUIUtility.singleLineHeight
             + (remainingLines - 1) * EditorGUIUtility.standardVerticalSpacing;
    }
}
