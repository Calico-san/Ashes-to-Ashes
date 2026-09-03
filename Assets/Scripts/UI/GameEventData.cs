using System;
using UnityEngine;

public enum EventTriggerType
{
    ExactDay,
    DayRange
}

[CreateAssetMenu(fileName = "New Game Event", menuName = "Ashes/Game Event")]
public class GameEventData : ScriptableObject
{
    public string Title;

    [TextArea(3, 8)]
    public string Message;

    public GameEventOption[] Options;

    public EventTriggerType TriggerType = EventTriggerType.ExactDay;

    [Min(1)] public int TriggerDay = 1;
    [Range(0, 23)] public int TriggerHour = 8;

    [Min(1)] public int FirstDay = 1;
    [Min(1)] public int LastDay = 1;

    [NonSerialized] public bool HasTriggered;

    private int scheduledDay;
    private int scheduledHour;

    public void PrepareTrigger()
    {
        HasTriggered = false;

        if (TriggerType == EventTriggerType.ExactDay)
        {
            scheduledDay = Mathf.Max(1, TriggerDay);
            scheduledHour = Mathf.Clamp(TriggerHour, 0, 23);
            return;
        }

        int firstDay = Mathf.Max(1, FirstDay);
        int lastDay = Mathf.Max(firstDay, LastDay);
        scheduledDay = UnityEngine.Random.Range(firstDay, lastDay + 1);
        scheduledHour = 8;
    }

    public bool IsReady(int day, int hour)
    {
        return day > scheduledDay || (day == scheduledDay && hour >= scheduledHour);
    }
}

[Serializable]
public class GameEventOption
{
    public string Label;

    [TextArea(2, 5)]
    public string Explanation;

    public bool ChangesHope;
    public float HopeChange;

    public GameEventOption(string label, string explanation, bool changesHope, float hopeChange)
    {
        Label = label;
        Explanation = explanation;
        ChangesHope = changesHope;
        HopeChange = hopeChange;
    }
}
