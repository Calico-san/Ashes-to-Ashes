using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Complete serializable snapshot of the game state.
/// JsonUtility requires [Serializable] on the class and all nested types.
/// No Unity object references — only primitive types, strings and nested structs.
/// </summary>
[Serializable]
public class GameStateData
{
    // ---- Meta ----
    public string   SaveVersion  = "1.0";
    public string   SaveDateTime;          // ISO 8601, informational only

    // ---- Time ----
    public int      Day;
    public float    SimulatedMinutes;
    public int      SpeedMultiplier;

    // ---- Resources ----
    public int      Food;
    public int      Wood;
    public int      Steel;
    public int      Cloth;
    public int      Rope;
    public int      Ships;

    // ---- Population ----
    public int      TotalPopulation;
    public int      Children;
    public int      FreeWorkers;
    public int      FreeEngineers;

    // ---- Buildings ----
    public List<BuildingData>  Buildings  = new List<BuildingData>();

    // ---- Build slots (under construction) ----
    public List<BuildSlotData> BuildSlots = new List<BuildSlotData>();

    // ---- Ships ----
    public List<ShipData>      ShipList   = new List<ShipData>();
}

// -------------------------------------------------------

[Serializable]
public class BuildingData
{
    public string   DisplayName;
    public string   BuildingTypeName;   // BuildingType enum → string
    public string   ResourceTypeName;   // ResourceType enum → string
    public float    PositionX;
    public float    PositionY;
    public float    SizeX;
    public float    SizeY;
    public int      AssignedWorkers;
    public int      AssignedEngineers;
    public bool     IsShipyard;
    public bool     IsTownHall;

    // Shipyard-specific
    public float    ShipProgress;
    public int      ShipCount;
}

// -------------------------------------------------------

[Serializable]
public class BuildSlotData
{
    public float    PositionX;
    public float    PositionY;
    public float    SizeX;
    public float    SizeY;
    public string   QueuedTypeName;             // BuildingType enum → string
    public float    ConstructionHoursRemaining;
    public float    ConstructionHoursTotal;
}

// -------------------------------------------------------

[Serializable]
public class ShipData
{
    public int      ShipNumber;
    public float    PositionX;
    public float    PositionY;
    public int      AssignedSailors;
    public int      Passengers;
    public int      FoodLoaded;
    public bool     HasVisual;
}
