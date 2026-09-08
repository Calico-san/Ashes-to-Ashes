using System.Collections.Generic;
using UnityEngine;

public class EventManager
{
    private readonly List<GameEventData> events = new();
    private GameController gameController;

    public void Initialize(GameController controller)
    {
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
}
