using System;
using System.Collections.Generic;
using UnityEngine;

public enum EventTriggerType
{
    ExactDay,
    DayRange
}

public enum EventOptionRequirementType
{
    None,
    Building,
    ResourceAmount,
    Manpower
}

public enum EventOptionRewardType
{
    ResourceAmount,
    Manpower
}

public enum ManpowerType
{
    Workers,
    Engineers
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
        if (Options != null)
            foreach (GameEventOption option in Options)
                option?.MigrateLegacyRequirement();

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

    private void OnValidate()
    {
        if (Options == null) return;

        foreach (GameEventOption option in Options)
            option?.MigrateLegacyRequirement();
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

    public List<EventOptionRequirement> Requirements = new();
    public List<EventOptionReward> Rewards = new();

    [HideInInspector] public EventOptionRequirementType RequirementType;
    [HideInInspector] public BuildingType RequiredBuilding;
    [HideInInspector] public ResourceType RequiredResourceType;
    [HideInInspector] public int RequiredResourceAmount = 1;

    public GameEventOption(string label, string explanation, bool changesHope, float hopeChange)
    {
        Label = label;
        Explanation = explanation;
        ChangesHope = changesHope;
        HopeChange = hopeChange;
    }

    public bool MeetsRequirements(GameController game)
    {
        if (Requirements == null || Requirements.Count == 0) return true;
        if (game == null) return false;

        var resourceCosts = new Dictionary<ResourceType, int>();
        var manpowerCosts = new Dictionary<ManpowerType, int>();
        foreach (EventOptionRequirement requirement in Requirements)
        {
            if (requirement == null || requirement.Type == EventOptionRequirementType.None) continue;
            if (requirement.Type == EventOptionRequirementType.Building && !requirement.HasRequiredBuilding(game))
                return false;
            if (requirement.Type == EventOptionRequirementType.ResourceAmount)
            {
                resourceCosts.TryGetValue(requirement.Resource, out int currentCost);
                resourceCosts[requirement.Resource] = currentCost + requirement.Amount;
            }
            if (requirement.Type == EventOptionRequirementType.Manpower)
            {
                manpowerCosts.TryGetValue(requirement.Manpower, out int currentCost);
                manpowerCosts[requirement.Manpower] = currentCost + requirement.Amount;
            }
        }

        foreach (KeyValuePair<ResourceType, int> cost in resourceCosts)
            if (GetResourceAmount(game, cost.Key) < cost.Value) return false;
        foreach (KeyValuePair<ManpowerType, int> cost in manpowerCosts)
            if (game.GetManpower(cost.Key) < cost.Value) return false;

        return true;
    }

    public bool TryPayRequirements(GameController game)
    {
        if (!MeetsRequirements(game)) return false;
        if (Requirements == null) return true;

        foreach (EventOptionRequirement requirement in Requirements)
            if (requirement != null && requirement.Type == EventOptionRequirementType.ResourceAmount)
                game.TryConsumeResource(requirement.Resource, requirement.Amount);
            else if (requirement != null && requirement.Type == EventOptionRequirementType.Manpower)
                game.TryConsumeManpower(requirement.Manpower, requirement.Amount);

        return true;
    }

    public void ApplyRewards(GameController game)
    {
        if (game == null || Rewards == null) return;

        foreach (EventOptionReward reward in Rewards)
        {
            if (reward == null || reward.Amount <= 0) continue;
            if (reward.Type == EventOptionRewardType.ResourceAmount)
                game.AddResource(reward.Resource, reward.Amount);
            else if (reward.Type == EventOptionRewardType.Manpower)
                game.AddManpower(reward.Manpower, reward.Amount);
        }
    }

    public void MigrateLegacyRequirement()
    {
        Requirements ??= new List<EventOptionRequirement>();
        if (Requirements.Count > 0 || RequirementType == EventOptionRequirementType.None) return;

        Requirements.Add(new EventOptionRequirement
        {
            Type = RequirementType,
            Building = RequiredBuilding,
            Resource = RequiredResourceType,
            Amount = Mathf.Max(1, RequiredResourceAmount)
        });
        RequirementType = EventOptionRequirementType.None;
    }

    private static int GetResourceAmount(GameController game, ResourceType type)
    {
        return type switch
        {
            ResourceType.Food => game.Food,
            ResourceType.Wood => game.Wood,
            ResourceType.Steel => game.Steel,
            ResourceType.Cloth => game.Cloth,
            ResourceType.Rope => game.Rope,
            ResourceType.Ships => game.Ships,
            _ => 0
        };
    }
}

[Serializable]
public class EventOptionRequirement
{
    public EventOptionRequirementType Type;
    public BuildingType Building;
    public ResourceType Resource;
    public ManpowerType Manpower;
    [Min(1)] public int Amount = 1;

    public bool HasRequiredBuilding(GameController game)
    {
        foreach (BuildingInstance building in game.Buildings)
            if (building != null && building.BuildingTypeEnum == Building) return true;

        return false;
    }
}

[Serializable]
public class EventOptionReward
{
    public EventOptionRewardType Type;
    public ResourceType Resource;
    public ManpowerType Manpower;
    [Min(1)] public int Amount = 1;
}
