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
    public const int FirstTriggerHour = 6;
    public const int LastTriggerHour = 20;

    public string Title;

    [TextArea(3, 8)]
    public string Message;

    public GameEventOption[] Options;

    public EventTriggerType TriggerType = EventTriggerType.ExactDay;

    [Min(1)] public int TriggerDay = 1;
    [Range(FirstTriggerHour, LastTriggerHour)] public int TriggerHour = 8;
    public bool RepeatEveryDay;

    [Min(1)] public int FirstDay = 1;
    [Min(1)] public int LastDay = 1;

    [NonSerialized] public bool HasTriggered;

    private int scheduledDay;
    private int scheduledHour;
    private int lastTriggeredDay;

    public void PrepareTrigger()
    {
        HasTriggered = false;
        lastTriggeredDay = -1;

        if (TriggerType == EventTriggerType.ExactDay)
        {
            scheduledDay = Mathf.Max(1, TriggerDay);
            scheduledHour = Mathf.Clamp(TriggerHour, FirstTriggerHour, LastTriggerHour);
            return;
        }

        int firstDay = Mathf.Max(1, FirstDay);
        int lastDay = Mathf.Max(firstDay, LastDay);
        scheduledDay = UnityEngine.Random.Range(firstDay, lastDay + 1);
        scheduledHour = UnityEngine.Random.Range(FirstTriggerHour, LastTriggerHour);
    }

    public bool IsReady(int day, int hour)
    {
        if (hour < FirstTriggerHour || hour > LastTriggerHour)
        {
            return false;
        }

        if (RepeatEveryDay)
        {
            return day >= scheduledDay && hour >= scheduledHour && lastTriggeredDay != day;
        }

        if (HasTriggered)
        {
            return false;
        }

        return day == scheduledDay && hour >= scheduledHour;
    }

    public void MarkTriggered(int day)
    {
        if (RepeatEveryDay)
        {
            lastTriggeredDay = day;
        }
        else
        {
            HasTriggered = true;
        }
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
