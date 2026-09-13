using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GameEventOption))]
public class GameEventOptionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        SerializedProperty explanation = property.FindPropertyRelative("Explanation");
        SerializedProperty requirements = property.FindPropertyRelative("Requirements");

        EditorGUI.PropertyField(line, property.FindPropertyRelative("Label"));
        line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        line.height = EditorGUI.GetPropertyHeight(explanation);
        EditorGUI.PropertyField(line, explanation);
        line.y += line.height + EditorGUIUtility.standardVerticalSpacing;
        line.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(line, property.FindPropertyRelative("ChangesHope"));
        line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if (property.FindPropertyRelative("ChangesHope").boolValue)
        {
            EditorGUI.PropertyField(line, property.FindPropertyRelative("HopeChange"));
            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        line.height = EditorGUI.GetPropertyHeight(requirements, true);
        EditorGUI.PropertyField(line, requirements, new GUIContent("Requirements"), true);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lineCount = property.FindPropertyRelative("ChangesHope").boolValue ? 3 : 2;
        return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Explanation"))
             + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Requirements"), true)
             + lineCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
    }
}

[CustomPropertyDrawer(typeof(EventOptionRequirement))]
public class EventOptionRequirementDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.PropertyField(line, property.FindPropertyRelative("Type"));
        line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EventOptionRequirementType type = (EventOptionRequirementType)property.FindPropertyRelative("Type").enumValueIndex;
        if (type == EventOptionRequirementType.Building)
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Building"), new GUIContent("Required Building"));
        else if (type == EventOptionRequirementType.ResourceAmount)
        {
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Resource"));
            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Amount"));
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        EventOptionRequirementType type = (EventOptionRequirementType)property.FindPropertyRelative("Type").enumValueIndex;
        int lineCount = type == EventOptionRequirementType.ResourceAmount ? 3 : type == EventOptionRequirementType.Building ? 2 : 1;
        return lineCount * EditorGUIUtility.singleLineHeight
             + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
    }
}
