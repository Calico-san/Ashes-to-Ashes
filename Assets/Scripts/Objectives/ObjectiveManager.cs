using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ObjectiveCampaign campaign;

    private readonly List<ObjectiveProgress> activeObjectives = new();
    private GameController gameController;
    private int activePhaseIndex = -1;
    private bool campaignStarted;
    private bool campaignFinished;

    public ObjectivePhase ActivePhase => activePhaseIndex >= 0 && campaign != null
        && activePhaseIndex < campaign.Phases.Count ? campaign.Phases[activePhaseIndex] : null;
    public string ActivePhaseTitle => ActivePhase != null ? ActivePhase.Title : string.Empty;
    public IReadOnlyList<ObjectiveProgress> ActiveObjectives => activeObjectives;
    public bool CampaignStarted => campaignStarted;
    public bool CampaignFinished => campaignFinished;

    public event Action ObjectivesChanged;

    public void Initialize(GameController game)
    {
        gameController = game;
        activeObjectives.Clear();
        activePhaseIndex = -1;
        campaignStarted = false;
        campaignFinished = false;

        if (campaign == null)
            campaign = Resources.Load<ObjectiveCampaign>("Objectives");

        if (campaign == null) return;

        if (campaign.StartAfterEvent == null)
            StartCampaign();
        else
            UniversalPopup.EventClosed += HandleEventClosed;
    }

    public void ResumeAfterLoad()
    {
        if (campaignStarted || campaign == null || campaign.StartAfterEvent == null
            || !campaign.StartAfterEvent.HasTriggered) return;

        UniversalPopup.EventClosed -= HandleEventClosed;
        StartCampaign();
    }

    private void Update()
    {
        if (gameController != null && ActivePhase != null)
            EvaluateActiveObjectives();
    }

    private void ActivateNextPhase()
    {
        activePhaseIndex++;
        activeObjectives.Clear();

        if (campaign == null || activePhaseIndex >= campaign.Phases.Count)
        {
            campaignFinished = true;
            ObjectivesChanged?.Invoke();
            enabled = false;
            return;
        }

        ObjectivePhase phase = campaign.Phases[activePhaseIndex];
        if (phase == null || phase.Objectives.Count == 0)
        {
            Debug.LogWarning($"[Objectives] Phase {activePhaseIndex + 1} has no objectives and was skipped.");
            ActivateNextPhase();
            return;
        }

        foreach (ObjectiveDefinition definition in phase.Objectives)
            if (definition != null) activeObjectives.Add(new ObjectiveProgress(definition));

        if (activeObjectives.Count == 0)
        {
            Debug.LogWarning($"[Objectives] Phase {activePhaseIndex + 1} has no valid objectives and was skipped.");
            ActivateNextPhase();
            return;
        }

        EvaluateActiveObjectives();
        ObjectivesChanged?.Invoke();
    }

    private void StartCampaign()
    {
        campaignStarted = true;
        ActivateNextPhase();
    }

    private void HandleEventClosed(GameEventData closedEvent)
    {
        if (campaignStarted || closedEvent != campaign.StartAfterEvent) return;

        UniversalPopup.EventClosed -= HandleEventClosed;
        StartCampaign();
    }

    private void OnDestroy()
    {
        UniversalPopup.EventClosed -= HandleEventClosed;
    }

    private void EvaluateActiveObjectives()
    {
        bool changed = false;
        bool allCompleted = true;

        foreach (ObjectiveProgress objective in activeObjectives)
        {
            int value = GetCurrentValue(objective.Definition);
            changed |= objective.SetCurrentValue(value);
            allCompleted &= objective.IsCompleted;
        }

        if (changed) ObjectivesChanged?.Invoke();
        if (allCompleted) ActivateNextPhase();
    }

    private int GetCurrentValue(ObjectiveDefinition definition)
    {
        switch (definition.Type)
        {
            case ObjectiveType.BuildBuilding:
                int count = 0;
                foreach (BuildingInstance building in gameController.Buildings)
                    if (building != null && building.BuildingTypeEnum == definition.BuildingType) count++;
                return count;

            case ObjectiveType.ReachResourceAmount:
                return GetResourceAmount(definition.ResourceType);
            case ObjectiveType.ReachHope:
                return Mathf.FloorToInt(gameController.Hope);
            case ObjectiveType.Evacuate:
                return gameController.EvacuationStarted ? 1 : 0;
            default:
                return 0;
        }
    }

    private int GetResourceAmount(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Food: return gameController.Food;
            case ResourceType.Wood: return gameController.Wood;
            case ResourceType.Steel: return gameController.Steel;
            case ResourceType.Cloth: return gameController.Cloth;
            case ResourceType.Rope: return gameController.Rope;
            case ResourceType.Ships: return gameController.Ships;
            case ResourceType.RawFood: return gameController.RawFood;
            default: return 0;
        }
    }
}

public sealed class ObjectiveProgress
{
    public ObjectiveDefinition Definition { get; }
    public int CurrentValue { get; private set; }
    public bool IsCompleted { get; private set; }

    public ObjectiveProgress(ObjectiveDefinition definition) => Definition = definition;

    internal bool SetCurrentValue(int value)
    {
        int clampedValue = Mathf.Max(0, value);
        bool completed = clampedValue >= Definition.TargetValue;
        bool changed = clampedValue != CurrentValue || completed != IsCompleted;
        CurrentValue = clampedValue;
        IsCompleted = completed;
        return changed;
    }
}
