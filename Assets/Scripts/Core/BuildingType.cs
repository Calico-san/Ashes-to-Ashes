using UnityEngine;

/// <summary>All building types. TownHall is pre-built. Others must be constructed.</summary>
public enum BuildingType
{
    TownHall,
    Sawmill,
    Steelworks,
    Fiberworks,
    Cookhouse,
    Shipyard,
    HuntersHut,
    ScoutStation
}

/// <summary>
/// Otisak gradevine u poljima karte. Sve osim brodogradilista zauzima jedno
/// polje; brodogradiliste zauzima 2x2.
///
/// Velicina se NIKAD ne cita iz spremljene igre nego uvijek izvodi iz tipa —
/// tako promjena otiska vrijedi i za stare zapise, a zastarjela vrijednost ne
/// moze zavrsiti kao localScale i napuhati gradevinu.
/// </summary>
public static class BuildingFootprint
{
    /// <summary>Stranica otiska u svjetskim jedinicama (1 polje = 1).</summary>
    public static float SizeFor(BuildingType type)
        => type == BuildingType.Shipyard
            ? BalanceConfig.ShipyardPlacementSize
            : BalanceConfig.BuildingPlacementSize;

    public static Vector2 VectorFor(BuildingType type)
    {
        float s = SizeFor(type);
        return new Vector2(s, s);
    }

    /// <summary>
    /// True ako otisak pokriva paran broj polja. Parni otisci se centriraju na
    /// kriziste mreze, neparni na srediste polja.
    /// </summary>
    public static bool IsEven(BuildingType type)
        => Mathf.RoundToInt(SizeFor(type)) % 2 == 0;
}

/// <summary>Construction cost and time for a building type.</summary>
public struct BuildingCost
{
    public int  Wood;
    public int  Steel;
    public int  Cloth;
    public int  Rope;
    public int  Hours;   // in-game hours to build

    public static BuildingCost For(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Sawmill:
                return new BuildingCost { Wood = BalanceConfig.SawmillWoodCost,    Steel = BalanceConfig.SawmillSteelCost,    Cloth = BalanceConfig.SawmillClothCost,    Hours = BalanceConfig.SawmillBuildHours };
            case BuildingType.Steelworks:
                return new BuildingCost { Wood = BalanceConfig.SteelworksWoodCost, Steel = BalanceConfig.SteelworksSteelCost, Cloth = BalanceConfig.SteelworksClothCost, Hours = BalanceConfig.SteelworksBuildHours };
            case BuildingType.Fiberworks:
                return new BuildingCost { Wood = BalanceConfig.FiberworksWoodCost, Steel = BalanceConfig.FiberworksSteelCost, Cloth = BalanceConfig.FiberworksClothCost, Hours = BalanceConfig.FiberworksBuildHours };
            case BuildingType.Cookhouse:
                return new BuildingCost { Wood = BalanceConfig.CookhouseWoodCost,  Steel = BalanceConfig.CookhouseSteelCost,  Cloth = BalanceConfig.CookhouseClothCost,  Hours = BalanceConfig.CookhouseBuildHours };
            case BuildingType.Shipyard:
                return new BuildingCost { Wood = BalanceConfig.ShipyardWoodCost,   Steel = BalanceConfig.ShipyardSteelCost,   Cloth = BalanceConfig.ShipyardClothCost,   Hours = BalanceConfig.ShipyardBuildHours };
            case BuildingType.HuntersHut:
                return new BuildingCost { Wood = BalanceConfig.HuntersHutWoodCost, Steel = BalanceConfig.HuntersHutSteelCost, Cloth = 0, Hours = BalanceConfig.HuntersHutBuildHours };
            case BuildingType.ScoutStation:
                return new BuildingCost { Wood = BalanceConfig.ScoutStationWoodCost, Steel = BalanceConfig.ScoutStationSteelCost, Cloth = 0, Hours = BalanceConfig.ScoutStationBuildHours };
            default:
                return new BuildingCost();
        }
    }

    public bool CanAfford(int wood, int steel, int cloth, int rope = 0)
        => wood >= Wood && steel >= Steel && cloth >= Cloth && rope >= Rope;
}

/// <summary>Visual state of a building, used to pick the correct sprite.</summary>
public enum BuildingVisualState
{
    UnderConstruction,
    Idle,
    Producing
}
