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

        EditorGUI.PropertyField(line, property.FindPropertyRelative("RequirementType"));
        line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EventOptionRequirementType type = (EventOptionRequirementType)property.FindPropertyRelative("RequirementType").enumValueIndex;
        if (type == EventOptionRequirementType.Building)
        {
            EditorGUI.PropertyField(line, property.FindPropertyRelative("RequiredBuilding"));
        }
        else if (type == EventOptionRequirementType.ResourceAmount)
        {
            EditorGUI.PropertyField(line, property.FindPropertyRelative("RequiredResourceType"), new GUIContent("Resource"));
            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(line, property.FindPropertyRelative("RequiredResourceAmount"), new GUIContent("Amount"));
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lineCount = 3;
        if (property.FindPropertyRelative("ChangesHope").boolValue) lineCount++;

        EventOptionRequirementType type = (EventOptionRequirementType)property.FindPropertyRelative("RequirementType").enumValueIndex;
        if (type == EventOptionRequirementType.Building) lineCount++;
        if (type == EventOptionRequirementType.ResourceAmount) lineCount += 2;

        return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Explanation"))
             + lineCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
    }
}
