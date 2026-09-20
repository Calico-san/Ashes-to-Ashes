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

    // ---- Animirani ocean ----
    [Header("Ocean — animacija (ostavi prazno za staticni TileOcean)")]
    [Tooltip("Frameovi animacije oceana. Najmanje 2, tipicno 4. Prazno = koristi se staticni TileOcean.")]
    public Sprite[] OceanFrames;
    [Tooltip("Brzina animacije oceana u frameovima po sekundi. 1.5–3 djeluje kao mirno more.")]
    public float OceanFps = 2f;

    /// <summary>True ako je ocean animiran (ima barem dva framea).</summary>
    public bool HasOceanAnimation => OceanFrames != null && OceanFrames.Length >= 2;

    // ---- Prijelazi na Shore poljima ----
    // Indeks u nizu je 4-bitna maska susjeda: N=1, E=2, S=4, W=8.
    // Popunjava se desnim klikom na asset -> "Popuni rubne setove iz sheetova".
    [Header("Prijelaz Shore -> Ocean (16 komada, indeks = maska susjednog oceana)")]
    public Sprite[] ShoreOceanEdges = new Sprite[16];

    [Header("Prijelaz Shore -> Land (16 komada, indeks = maska susjednog kopna)")]
    public Sprite[] ShoreLandEdges = new Sprite[16];

    public Sprite GetShoreOceanEdge(int mask) => Pick(ShoreOceanEdges, mask);
    public Sprite GetShoreLandEdge(int mask)  => Pick(ShoreLandEdges,  mask);

    private static Sprite Pick(Sprite[] set, int mask)
        => set != null && mask >= 0 && mask < set.Length ? set[mask] : null;

    // ---- Iron Mine (objekt u velicini zgrade, ne tile) ----
    [Header("Iron Mine")]
    [Tooltip("Mnozitelj velicine rudnika. 1 = tocno onoliko polja koliko je nacrtano " +
             "na karti; 1.5 znaci da art prelazi pola polja preko ruba.")]
    public float IronMineScale = 1f;

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
    [Tooltip("Brzina animacije hoda u frameovima po sekundi.")]
    public float    WalkFps = 6f;

    // ---- Engineer ----
    [Header("Engineer")]
    [Tooltip("Ostavi prazno da inzenjeri koriste isti art kao radnici.")]
    public Sprite   EngineerIdle;
    public Sprite[] EngineerWalkFrames;

    // ---- Ship ----
    // Brod je jedan sprite, ne sastavljen od trupa, jarbola i jedra.
    [Header("Ship")]
    [Tooltip("Cijeli brod u jednom spriteu. Prazno = crta se placeholder od pravokutnika.")]
    public Sprite   ShipIdle;
    [Tooltip("Frameovi animacije broda. Prazno = koristi se staticni ShipIdle.")]
    public Sprite[] ShipIdleFrames;
    [Tooltip("Sirina broda u poljima. Visina se racuna iz omjera stranica sprajta.")]
    public float    ShipWidthInTiles = 1f;
    [Tooltip("Brzina animacije broda u frameovima po sekundi.")]
    public float    ShipFps = 4f;

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

    /// <summary>
    /// Art inzenjera. Ako EngineerIdle nije postavljen, vraca se art radnika —
    /// inzenjeri tada izgledaju kao radnici, sto je bolje od trokuta.
    /// </summary>
    public Sprite GetEngineerSprite(int walkFrame = -1)
    {
        if (walkFrame >= 0 && EngineerWalkFrames != null && EngineerWalkFrames.Length > 0)
            return EngineerWalkFrames[walkFrame % EngineerWalkFrames.Length];
        return EngineerIdle != null ? EngineerIdle : GetWorkerSprite(walkFrame);
    }

    public Sprite GetShipIdleFrame(int frame)
    {
        if (ShipIdleFrames != null && ShipIdleFrames.Length > 0)
            return ShipIdleFrames[frame % ShipIdleFrames.Length];
        return ShipIdle;
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

#if UNITY_EDITOR
    // ---- Editor pomagalo ----
    // Rucno slaganje 32 sprite-a u dva niza je i sporo i sklono gresci: Project
    // prozor sortira abecedno (_0, _1, _10, _11, ...) pa visestruko povlacenje
    // odjednom da krivi redoslijed. Ovo ih ucita i sortira brojcano.

    private const string ShoreOceanSheet = "Assets/Sprites/tiles-shore-ocean-16.png";
    private const string ShoreLandSheet  = "Assets/Sprites/tiles-shore-land-16.png";

    [ContextMenu("Popuni rubne setove iz sheetova")]
    private void AutoFillEdgeSets()
    {
        ShoreOceanEdges = LoadOrdered(ShoreOceanSheet);
        ShoreLandEdges  = LoadOrdered(ShoreLandSheet);

        Debug.Log($"[SpriteRegistry] Ucitano {ShoreOceanEdges.Length} + {ShoreLandEdges.Length} rubnih komada.");
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    }

    private static Sprite[] LoadOrdered(string path)
    {
        var all  = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        var list = new System.Collections.Generic.List<Sprite>();

        foreach (var obj in all)
            if (obj is Sprite s) list.Add(s);

        if (list.Count == 0)
        {
            Debug.LogError($"[SpriteRegistry] Nema izrezanih sprite-ova u {path}. " +
                           "Postavi Sprite Mode na Multiple i izrezi na 16x16.");
            return new Sprite[16];
        }

        list.Sort((a, b) => TrailingNumber(a.name).CompareTo(TrailingNumber(b.name)));
        return list.ToArray();
    }

    /// <summary>Broj iza zadnje donje crte u nazivu, npr. "tiles-shore-ocean-16_11" -> 11.</summary>
    private static int TrailingNumber(string name)
    {
        int i = name.LastIndexOf('_');
        return i >= 0 && int.TryParse(name.Substring(i + 1), out int n) ? n : 0;
    }
#endif
}
