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
        }
        else
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FirstDay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LastDay"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
