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
        SerializedProperty rewards = property.FindPropertyRelative("Rewards");

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
        line.y += line.height + EditorGUIUtility.standardVerticalSpacing;
        line.height = EditorGUI.GetPropertyHeight(rewards, true);
        EditorGUI.PropertyField(line, rewards, new GUIContent("Rewards"), true);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lineCount = property.FindPropertyRelative("ChangesHope").boolValue ? 3 : 2;
        return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Explanation"))
             + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Requirements"), true)
             + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Rewards"), true)
             + lineCount * EditorGUIUtility.singleLineHeight
             + (lineCount + 2) * EditorGUIUtility.standardVerticalSpacing;
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
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(new GUIContent("Required Building")).x + 8f;
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Building"), new GUIContent("Required Building"));
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }
        else if (type == EventOptionRequirementType.ResourceAmount)
        {
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Resource"));
            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Amount"));
        }
        else if (type == EventOptionRequirementType.Manpower)
        {
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Manpower"));
            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Amount"));
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        EventOptionRequirementType type = (EventOptionRequirementType)property.FindPropertyRelative("Type").enumValueIndex;
        int lineCount = type == EventOptionRequirementType.ResourceAmount || type == EventOptionRequirementType.Manpower ? 3 : type == EventOptionRequirementType.Building ? 2 : 1;
        return lineCount * EditorGUIUtility.singleLineHeight
             + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
    }
}

[CustomPropertyDrawer(typeof(EventOptionReward))]
public class EventOptionRewardDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.PropertyField(line, property.FindPropertyRelative("Type"));
        line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EventOptionRewardType type = (EventOptionRewardType)property.FindPropertyRelative("Type").enumValueIndex;
        if (type == EventOptionRewardType.ResourceAmount)
        {
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Resource"));
            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Amount"));
        }
        else if (type == EventOptionRewardType.Manpower)
        {
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Manpower"));
            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Amount"));
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        EventOptionRewardType type = (EventOptionRewardType)property.FindPropertyRelative("Type").enumValueIndex;
        int lineCount = type == EventOptionRewardType.ResourceAmount || type == EventOptionRewardType.Manpower ? 3 : 1;
        return lineCount * EditorGUIUtility.singleLineHeight
             + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
    }
}
