using UnityEngine;

/// <summary>
/// Central registry for all game sprites.
/// Returns null if no sprite is registered — callers fall back to SimpleShapeFactory.
///
/// Usage:
///   var sprite = SpriteRegistry.Instance.GetBuildingSprite(BuildingType.Sawmill, BuildingVisualState.Built);
///   if (sprite == null) sprite = SimpleShapeFactory.CreateFilledSquareSprite(fallbackColor);
///
/// To add pixel art sprites later:
///   1. Import sprite sheets into Assets/Art/
///   2. Assign in the Inspector on the SpriteRegistry GameObject
///   3. No other code changes needed
/// </summary>
public class SpriteRegistry : MonoBehaviour
{
    public static SpriteRegistry Instance { get; private set; }

    [Header("Building — Under Construction")]
    public Sprite TownHallConstruction;
    public Sprite SawmillConstruction;
    public Sprite SteelworksConstruction;
    public Sprite ClothWorksConstruction;
    public Sprite CookhouseConstruction;
    public Sprite ShipyardConstruction;

    [Header("Building — Built / Idle")]
    public Sprite TownHallBuilt;
    public Sprite SawmillBuilt;
    public Sprite SteelworksBuilt;
    public Sprite ClothWorksBuilt;
    public Sprite CookhouseBuilt;
    public Sprite ShipyardBuilt;

    [Header("Building — Producing")]
    public Sprite SawmillProducing;
    public Sprite SteelworksProducing;
    public Sprite ClothWorksProducing;
    public Sprite CookhouseProducing;
    public Sprite ShipyardProducing;

    [Header("Worker")]
    public Sprite WorkerIdle;
    public Sprite[] WorkerWalkFrames;   // assigned later for walk cycle

    [Header("Ship")]
    public Sprite ShipHull;
    public Sprite ShipMast;
    public Sprite ShipSail;
    public Sprite[] ShipIdleFrames;     // assigned later for idle bob animation

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---- Building sprites ----

    public Sprite GetBuildingSprite(BuildingType type, BuildingVisualState state)
    {
        switch (state)
        {
            case BuildingVisualState.UnderConstruction: return GetConstructionSprite(type);
            case BuildingVisualState.Producing:         return GetProducingSprite(type);
            default:                                    return GetBuiltSprite(type);
        }
    }

    private Sprite GetConstructionSprite(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.TownHall:    return TownHallConstruction;
            case BuildingType.Sawmill:     return SawmillConstruction;
            case BuildingType.Steelworks:  return SteelworksConstruction;
            case BuildingType.ClothWorks:  return ClothWorksConstruction;
            case BuildingType.Cookhouse:   return CookhouseConstruction;
            case BuildingType.Shipyard:    return ShipyardConstruction;
            default:                       return null;
        }
    }

    private Sprite GetBuiltSprite(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.TownHall:    return TownHallBuilt;
            case BuildingType.Sawmill:     return SawmillBuilt;
            case BuildingType.Steelworks:  return SteelworksBuilt;
            case BuildingType.ClothWorks:  return ClothWorksBuilt;
            case BuildingType.Cookhouse:   return CookhouseBuilt;
            case BuildingType.Shipyard:    return ShipyardBuilt;
            default:                       return null;
        }
    }

    private Sprite GetProducingSprite(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Sawmill:     return SawmillProducing;
            case BuildingType.Steelworks:  return SteelworksProducing;
            case BuildingType.ClothWorks:  return ClothWorksProducing;
            case BuildingType.Cookhouse:   return CookhouseProducing;
            case BuildingType.Shipyard:    return ShipyardProducing;
            default:                       return GetBuiltSprite(type);  // fallback to built
        }
    }

    // ---- Worker sprites ----

    public Sprite GetWorkerSprite(int walkFrame = -1)
    {
        if (walkFrame >= 0 && WorkerWalkFrames != null && WorkerWalkFrames.Length > 0)
            return WorkerWalkFrames[walkFrame % WorkerWalkFrames.Length];
        return WorkerIdle;
    }

    // ---- Ship sprites ----

    public Sprite GetShipIdleFrame(int frame)
    {
        if (ShipIdleFrames != null && ShipIdleFrames.Length > 0)
            return ShipIdleFrames[frame % ShipIdleFrames.Length];
        return ShipHull;
    }
}

