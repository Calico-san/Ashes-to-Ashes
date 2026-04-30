using UnityEngine;

/// <summary>
/// ScriptableObject asset — created once via
/// Assets > Create > Ashes > SpriteRegistry.
/// Assign pixel art sprites in the Inspector at any time (no Play needed).
/// Loaded at runtime via Resources.Load or direct field reference in Bootstrapper.
///
/// All systems call SpriteRegistry.Instance which is set when the
/// Bootstrapper loads the asset.
/// </summary>
[CreateAssetMenu(fileName = "SpriteRegistry", menuName = "Ashes/SpriteRegistry")]
public class SpriteRegistry : ScriptableObject
{
    public static SpriteRegistry Instance { get; private set; }

    /// <summary>Called by PrototypeBootstrapper on startup.</summary>
    public void Register() => Instance = this;

    // ---- Buildings — Under Construction ----
    [Header("Tilemap Tiles")]
    public Sprite TileOcean;
    public Sprite TileShore;
    public Sprite TileLand;
    public Sprite TileForest;
    public Sprite TileIronMine;
    public Sprite TileVolcano;

    public Sprite GetTileSprite(TileType type)
    {
        switch (type)
        {
            case TileType.Ocean:    return TileOcean;
            case TileType.Shore:    return TileShore;
            case TileType.Land:     return TileLand;
            case TileType.Forest:   return TileForest;
            case TileType.IronMine: return TileIronMine;
            case TileType.Volcano:  return TileVolcano;
            default:                return null;
        }
    }

    [Header("Building — Under Construction")]
    public Sprite TownHallConstruction;
    public Sprite SawmillConstruction;
    public Sprite SteelworksConstruction;
    public Sprite FiberworksConstruction;
    public Sprite CookhouseConstruction;
    public Sprite ShipyardConstruction;

    // ---- Buildings — Built / Idle ----
    [Header("Building — Built / Idle")]
    public Sprite TownHallBuilt;
    public Sprite SawmillBuilt;
    public Sprite SteelworksBuilt;
    public Sprite FiberworksBuilt;
    public Sprite CookhouseBuilt;
    public Sprite ShipyardBuilt;

    // ---- Buildings — Producing ----
    [Header("Building — Producing")]
    public Sprite SawmillProducing;
    public Sprite SteelworksProducing;
    public Sprite FiberworksProducing;
    public Sprite CookhouseProducing;
    public Sprite ShipyardProducing;

    // ---- Worker ----
    [Header("Worker")]
    public Sprite   WorkerIdle;
    public Sprite[] WorkerWalkFrames;

    // ---- Ship ----
    [Header("Ship")]
    public Sprite   ShipHull;
    public Sprite   ShipMast;
    public Sprite   ShipSail;
    public Sprite[] ShipIdleFrames;

    // ---- API ----

    public Sprite GetBuildingSprite(BuildingType type, BuildingVisualState state)
    {
        switch (state)
        {
            case BuildingVisualState.UnderConstruction: return GetConstruction(type);
            case BuildingVisualState.Producing:         return GetProducing(type);
            default:                                    return GetBuilt(type);
        }
    }

    public Sprite GetWorkerSprite(int walkFrame = -1)
    {
        if (walkFrame >= 0 && WorkerWalkFrames != null && WorkerWalkFrames.Length > 0)
            return WorkerWalkFrames[walkFrame % WorkerWalkFrames.Length];
        return WorkerIdle;
    }

    public Sprite GetShipIdleFrame(int frame)
    {
        if (ShipIdleFrames != null && ShipIdleFrames.Length > 0)
            return ShipIdleFrames[frame % ShipIdleFrames.Length];
        return ShipHull;
    }

    // ---- Private ----

    private Sprite GetConstruction(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.TownHall:   return TownHallConstruction;
            case BuildingType.Sawmill:    return SawmillConstruction;
            case BuildingType.Steelworks: return SteelworksConstruction;
            case BuildingType.Fiberworks: return FiberworksConstruction;
            case BuildingType.Cookhouse:  return CookhouseConstruction;
            case BuildingType.Shipyard:   return ShipyardConstruction;
            default:                      return null;
        }
    }

    private Sprite GetBuilt(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.TownHall:   return TownHallBuilt;
            case BuildingType.Sawmill:    return SawmillBuilt;
            case BuildingType.Steelworks: return SteelworksBuilt;
            case BuildingType.Fiberworks: return FiberworksBuilt;
            case BuildingType.Cookhouse:  return CookhouseBuilt;
            case BuildingType.Shipyard:   return ShipyardBuilt;
            default:                      return null;
        }
    }

    private Sprite GetProducing(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Sawmill:    return SawmillProducing    != null ? SawmillProducing    : GetBuilt(type);
            case BuildingType.Steelworks: return SteelworksProducing != null ? SteelworksProducing : GetBuilt(type);
            case BuildingType.Fiberworks: return FiberworksProducing != null ? FiberworksProducing : GetBuilt(type);
            case BuildingType.Cookhouse:  return CookhouseProducing  != null ? CookhouseProducing  : GetBuilt(type);
            case BuildingType.Shipyard:   return ShipyardProducing   != null ? ShipyardProducing   : GetBuilt(type);
            default:                      return GetBuilt(type);
        }
    }
}
