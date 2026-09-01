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

    // ---- Tilemap ----
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

    // ---- Rub Shore -> Ocean ----
    // Smjer u nazivu je smjer u kojem se nalazi OCEAN, gledano s tog Shore polja.
    [Header("Rub Shore -> Ocean (voda na toj strani)")]
    public Sprite ShoreOceanN;
    public Sprite ShoreOceanE;
    public Sprite ShoreOceanS;
    public Sprite ShoreOceanW;

    [Header("Rub Shore -> Ocean (vanjski kut)")]
    public Sprite ShoreOceanNE;
    public Sprite ShoreOceanSE;
    public Sprite ShoreOceanSW;
    public Sprite ShoreOceanNW;

    public Sprite GetShoreOceanEdge(EdgeDir dir)
    {
        switch (dir)
        {
            case EdgeDir.N:  return ShoreOceanN;
            case EdgeDir.E:  return ShoreOceanE;
            case EdgeDir.S:  return ShoreOceanS;
            case EdgeDir.W:  return ShoreOceanW;
            case EdgeDir.NE: return ShoreOceanNE;
            case EdgeDir.SE: return ShoreOceanSE;
            case EdgeDir.SW: return ShoreOceanSW;
            case EdgeDir.NW: return ShoreOceanNW;
            default:         return null;
        }
    }

    // ---- Rub Shore -> Land ----
    // Smjer u nazivu je smjer u kojem se nalazi KOPNO.
    [Header("Rub Shore -> Land (trava na toj strani)")]
    public Sprite ShoreLandN;
    public Sprite ShoreLandE;
    public Sprite ShoreLandS;
    public Sprite ShoreLandW;

    public Sprite GetShoreLandEdge(EdgeDir dir)
    {
        switch (dir)
        {
            case EdgeDir.N: return ShoreLandN;
            case EdgeDir.E: return ShoreLandE;
            case EdgeDir.S: return ShoreLandS;
            case EdgeDir.W: return ShoreLandW;
            default:        return null;
        }
    }

    // ---- Volcano (jedan veliki animirani objekt, ne tile) ----
    [Header("Volcano — animated object")]
    [Tooltip("Frameovi iz volcano.png. Prazno = vulkan se ne iscrtava kao objekt.")]
    public Sprite[] VolcanoFrames;
    [Tooltip("Brzina animacije vulkana u frameovima po sekundi.")]
    public float VolcanoFps = 8f;

    public Sprite GetVolcanoFrame(int frame)
    {
        if (VolcanoFrames == null || VolcanoFrames.Length == 0) return null;
        int len = VolcanoFrames.Length;
        return VolcanoFrames[((frame % len) + len) % len];
    }

    // ---- Buildings — Under Construction ----
    [Header("Building — Under Construction")]
    [Tooltip("Koristi se za svaki tip koji nema vlastiti construction sprite.")]
    public Sprite GenericConstruction;
    public Sprite TownHallConstruction;
    public Sprite SawmillConstruction;
    public Sprite SteelworksConstruction;
    public Sprite FiberworksConstruction;
    public Sprite CookhouseConstruction;
    public Sprite ShipyardConstruction;
    public Sprite HuntersHutConstruction;
    public Sprite ScoutStationConstruction;

    // ---- Buildings — Built / Idle ----
    [Header("Building — Built / Idle")]
    public Sprite TownHallBuilt;
    public Sprite SawmillBuilt;
    public Sprite SteelworksBuilt;
    public Sprite FiberworksBuilt;
    public Sprite CookhouseBuilt;
    public Sprite ShipyardBuilt;
    public Sprite HuntersHutBuilt;
    public Sprite ScoutStationBuilt;

    // ---- Buildings — Producing ----
    [Header("Building — Producing")]
    public Sprite SawmillProducing;
    public Sprite SteelworksProducing;
    public Sprite FiberworksProducing;
    public Sprite CookhouseProducing;
    public Sprite ShipyardProducing;
    public Sprite HuntersHutProducing;
    public Sprite ScoutStationProducing;

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
        Sprite s;
        switch (type)
        {
            case BuildingType.TownHall:     s = TownHallConstruction;     break;
            case BuildingType.Sawmill:      s = SawmillConstruction;      break;
            case BuildingType.Steelworks:   s = SteelworksConstruction;   break;
            case BuildingType.Fiberworks:   s = FiberworksConstruction;   break;
            case BuildingType.Cookhouse:    s = CookhouseConstruction;    break;
            case BuildingType.Shipyard:     s = ShipyardConstruction;     break;
            case BuildingType.HuntersHut:   s = HuntersHutConstruction;   break;
            case BuildingType.ScoutStation: s = ScoutStationConstruction; break;
            default:                        s = null;                     break;
        }
        return s != null ? s : GenericConstruction;
    }

    private Sprite GetBuilt(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.TownHall:     return TownHallBuilt;
            case BuildingType.Sawmill:      return SawmillBuilt;
            case BuildingType.Steelworks:   return SteelworksBuilt;
            case BuildingType.Fiberworks:   return FiberworksBuilt;
            case BuildingType.Cookhouse:    return CookhouseBuilt;
            case BuildingType.Shipyard:     return ShipyardBuilt;
            case BuildingType.HuntersHut:   return HuntersHutBuilt;
            case BuildingType.ScoutStation: return ScoutStationBuilt;
            default:                        return null;
        }
    }

    private Sprite GetProducing(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Sawmill:      return SawmillProducing      != null ? SawmillProducing      : GetBuilt(type);
            case BuildingType.Steelworks:   return SteelworksProducing   != null ? SteelworksProducing   : GetBuilt(type);
            case BuildingType.Fiberworks:   return FiberworksProducing   != null ? FiberworksProducing   : GetBuilt(type);
            case BuildingType.Cookhouse:    return CookhouseProducing    != null ? CookhouseProducing    : GetBuilt(type);
            case BuildingType.Shipyard:     return ShipyardProducing     != null ? ShipyardProducing     : GetBuilt(type);
            case BuildingType.HuntersHut:   return HuntersHutProducing   != null ? HuntersHutProducing   : GetBuilt(type);
            case BuildingType.ScoutStation: return ScoutStationProducing != null ? ScoutStationProducing : GetBuilt(type);
            default:                        return GetBuilt(type);
        }
    }
}
