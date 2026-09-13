using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Objectives", menuName = "Ashes/Objective Campaign")]
public class ObjectiveCampaign : ScriptableObject
{
    [SerializeField] private GameEventData startAfterEvent;
    [SerializeField] private List<ObjectivePhase> phases = new();

    public GameEventData StartAfterEvent => startAfterEvent;
    public IReadOnlyList<ObjectivePhase> Phases => phases;
}

[Serializable]
public class ObjectivePhase
{
    [SerializeField] private string title = "New objective phase";

    [SerializeField] private List<ObjectiveDefinition> objectives = new();

    public string Title => title;
    public IReadOnlyList<ObjectiveDefinition> Objectives => objectives;
}

public enum ObjectiveType
{
    BuildBuilding,
    ReachResourceAmount,
    ReachHope,
    OwnShips
}

[Serializable]
public class ObjectiveDefinition
{
    [TextArea]
    [SerializeField] private string title = "New objective";
    [SerializeField] private ObjectiveType type;
    [Min(1)] [SerializeField] private int targetValue = 1;

    [SerializeField] private BuildingType buildingType;
    [SerializeField] private ResourceType resourceType;

    public string Title => title;
    public ObjectiveType Type => type;
    public int TargetValue => targetValue;
    public BuildingType BuildingType => buildingType;
    public ResourceType ResourceType => resourceType;
}
