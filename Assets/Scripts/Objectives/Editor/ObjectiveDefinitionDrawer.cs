using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ObjectiveDefinition))]
public class ObjectiveDefinitionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.PropertyField(line, property.FindPropertyRelative("title"));
        line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
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
        int lineCount = type == ObjectiveType.BuildBuilding || type == ObjectiveType.ReachResourceAmount ? 4 : 3;
        return lineCount * EditorGUIUtility.singleLineHeight
             + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
    }
}
