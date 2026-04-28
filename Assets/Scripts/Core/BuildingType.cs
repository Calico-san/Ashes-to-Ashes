/// <summary>All building types. TownHall is pre-built. Others must be constructed.</summary>
public enum BuildingType
{
    TownHall,
    Sawmill,
    Steelworks,
    Fiberworks,
    Cookhouse,
    Shipyard
}

/// <summary>Construction cost and time for a building type.</summary>
public struct BuildingCost
{
    public int  Wood;
    public int  Steel;
    public int  Cloth;
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
            default:
                return new BuildingCost();
        }
    }

    public bool CanAfford(int wood, int steel, int cloth)
        => wood >= Wood && steel >= Steel && cloth >= Cloth;
}

/// <summary>Visual state of a building, used to pick the correct sprite.</summary>
public enum BuildingVisualState
{
    UnderConstruction,
    Idle,
    Producing
}
