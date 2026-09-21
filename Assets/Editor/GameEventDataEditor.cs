using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameEventData))]
public class GameEventDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Title"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Message"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PopupSound"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        SerializedProperty options = serializedObject.FindProperty("Options");
        EditorGUILayout.PropertyField(options, GUIContent.none, true);
        options.arraySize = UnityEngine.Mathf.Clamp(options.arraySize, 1, 3);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Trigger", EditorStyles.boldLabel);
        SerializedProperty triggerType = serializedObject.FindProperty("TriggerType");
        EditorGUILayout.PropertyField(triggerType);

        if (triggerType.enumValueIndex == (int)EventTriggerType.ExactDay)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TriggerDay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TriggerHour"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("RepeatEveryDay"));
        }
        else
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FirstDay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LastDay"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Condition", EditorStyles.boldLabel);

        SerializedProperty conditionEvent = serializedObject.FindProperty("conditionEvent");
        SerializedProperty conditionOptionIndex = serializedObject.FindProperty("conditionOptionIndex");
        EditorGUILayout.PropertyField(conditionEvent, new GUIContent(
            "Condition To Trigger",
            "Leave as None for an unconditional event, or select an earlier event whose option must have been chosen."));

        GameEventData previousEvent = conditionEvent.objectReferenceValue as GameEventData;
        if (previousEvent == target)
        {
            EditorGUILayout.HelpBox("An event cannot depend on itself.", MessageType.Error);
            conditionEvent.objectReferenceValue = null;
            conditionOptionIndex.intValue = -1;
        }
        else if (previousEvent != null)
        {
            GameEventOption[] previousOptions = previousEvent.Options;
            if (previousOptions == null || previousOptions.Length == 0)
            {
                EditorGUILayout.HelpBox("The selected event has no options.", MessageType.Warning);
                conditionOptionIndex.intValue = -1;
            }
            else
            {
                string[] optionNames = previousOptions
                    .Select((option, index) => string.IsNullOrWhiteSpace(option?.Label)
                        ? $"Option {index + 1}"
                        : $"Option {index + 1}: {option.Label}")
                    .ToArray();

                int currentIndex = Mathf.Clamp(conditionOptionIndex.intValue, 0, optionNames.Length - 1);
                conditionOptionIndex.intValue = EditorGUILayout.Popup("Required Option", currentIndex, optionNames);
            }
        }
        else
        {
            conditionOptionIndex.intValue = -1;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
