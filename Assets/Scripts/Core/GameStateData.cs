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
    // 1.1 — dodan KeelLaid (fiksni trosak broda) i podjela putnika na djecu/odrasle.
    //       Stari zapisi (1.0) se i dalje ucitavaju: nova polja dobiju default,
    //       a AssignedSailors se ignorira jer mornari vise ne postoje.
    public string   SaveVersion  = "1.1";
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
    public int      RawFood;        // Hunter's Hut → Cookhouse intermediate

    // ---- Population ----
    public int      TotalPopulation;
    public int      Children;
    public int      Engineers = -1;
    public int      FreeWorkers;
    public int      FreeEngineers;
    /// <summary>Duse koje su vec otplovile — brodovi u odlasku se ne spremaju.</summary>
    public int      EvacuatedSouls;

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
    /// <summary>Je li fiksni trosak za brod u izradi vec placen.</summary>
    public bool     KeelLaid;
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
    /// <summary>Zadrzano radi kompatibilnosti s 1.0; uvijek se pise 0 i ne cita se.</summary>
    public int      AssignedSailors;
    /// <summary>Ukupno putnika — zadrzano radi kompatibilnosti i za fallback iz 1.0.</summary>
    public int      Passengers;
    public int      PassengerChildren;
    public int      PassengerAdults;
    public int      FoodLoaded;
    public bool     HasVisual;
}
