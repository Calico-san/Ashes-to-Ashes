using System.Collections.Generic;
using UnityEngine;

public class EventManager
{
    /// <summary>Zadnji inicijalizirani manager — GameController ga treba za save/load.</summary>
    public static EventManager Instance { get; private set; }

    private readonly List<GameEventData> events = new();
    private GameController gameController;

    public void Initialize(GameController controller)
    {
        Instance       = this;
        gameController = controller;
        events.Clear();

        GameEventData[] gameEvents = Resources.LoadAll<GameEventData>("Events");
        foreach (GameEventData gameEvent in gameEvents)
        {
            AddEvent(gameEvent);
        }
    }

    public void AddEvent(GameEventData gameEvent)
    {
        if (gameEvent == null || string.IsNullOrWhiteSpace(gameEvent.Title))
        {
            return;
        }

        gameEvent.PrepareTrigger();
        events.Add(gameEvent);
    }

    public void Update()
    {
        if (gameController == null)
        {
            return;
        }

        TryOpenEvent(gameController.Day, gameController.Hour);
    }

    public bool TryOpenEvent(int day, int hour)
    {
        if (UniversalPopup.Instance == null || UniversalPopup.Instance.IsOpen)
        {
            return false;
        }

        foreach (GameEventData gameEvent in events)
        {
            if (!gameEvent.IsReady(day, hour))
            {
                continue;
            }

            gameEvent.MarkTriggered(day);
            UniversalPopup.Instance.OpenEvent(gameEvent);
            return true;
        }

        return false;
    }

    // ---- Spremanje ----

    /// <summary>Zapis stanja okidaca svih dogadaja.</summary>
    public List<EventStateData> BuildSaveData()
    {
        var list = new List<EventStateData>(events.Count);
        foreach (GameEventData e in events)
        {
            if (e == null) continue;
            list.Add(new EventStateData
            {
                Title            = e.Title,
                HasTriggered     = e.HasTriggered,
                ScheduledDay     = e.ScheduledDay,
                ScheduledHour    = e.ScheduledHour,
                LastTriggeredDay = e.LastTriggeredDay,
            });
        }
        return list;
    }

    /// <summary>
    /// Vraca stanje okidaca. Dogadaj kojeg nema u zapisu (novi asset dodan nakon
    /// spremanja) zadrzava svjeze pripremljen okidac.
    /// </summary>
    public void ApplyLoadData(List<EventStateData> data)
    {
        if (data == null) return;

        foreach (EventStateData d in data)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.Title)) continue;

            foreach (GameEventData e in events)
            {
                if (e == null || e.Title != d.Title) continue;
                e.RestoreTrigger(d.HasTriggered, d.ScheduledDay, d.ScheduledHour, d.LastTriggeredDay);
                break;
            }
        }
    }
}
