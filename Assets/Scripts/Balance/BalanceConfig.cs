public static class BalanceConfig
{
    // Time
    public const float DayLengthSeconds       = 240f;

    // Population food consumption (per day)
    public const float AdultFoodPerDay         = 1.0f;
    public const float ChildFoodPerDay         = 0.5f;

    // Building production (per worker per day)
    public const float SawmillWoodPerWorker    = 1.2f;
    public const float SteelworksPerWorker     = 0.6f;
    public const float FiberworksClothPerWorker = 0.5f;   // Cloth per worker per day
    public const float FiberworksRopePerWorker  = 0.5f;   // Rope per worker per day
    public const float CookhouseFoodPerWorker  = 2.0f;

    // Shipyard
    public const float ShipProgressPerWorker   = 12f;
    public const float ShipProgressRequired    = 100f;

    // Ship build cost (per active worker per tick)
    public const int   ShipWoodCostPerWorker   = 1;
    public const int   ShipSteelCostPerWorker  = 1;
    public const int   ShipClothCostDivisor    = 2;
    public const int   ShipRopeCostDivisor     = 2;

    // Ship capacity
    public const int   ShipMaxPassengers       = 50;
    public const int   ShipFoodRequired        = 750;
    public const int   ShipMaxSailors          = 5;
    public const int   ShipyardMaxWorkers      = 10;

    // Worker limits
    public const int   DefaultBuildingMaxWorkers = 10;

    // Production bonuses
    public const float EngineerProductionBonus = 1.25f;
    public const float EngineerShipBonus       = 1.35f;

    // Building construction costs [wood, steel, cloth] + time in hours
    public const int SawmillWoodCost        = 15;
    public const int SawmillSteelCost       = 0;
    public const int SawmillClothCost       = 0;
    public const int SawmillBuildHours      = 2;

    public const int SteelworksWoodCost     = 20;
    public const int SteelworksSteelCost    = 0;
    public const int SteelworksClothCost    = 0;
    public const int SteelworksBuildHours   = 3;

    public const int FiberworksWoodCost     = 10;
    public const int FiberworksSteelCost    = 5;
    public const int FiberworksClothCost    = 5;
    public const int FiberworksBuildHours   = 2;

    public const int CookhouseWoodCost      = 15;
    public const int CookhouseSteelCost     = 5;
    public const int CookhouseClothCost     = 5;
    public const int CookhouseBuildHours    = 2;

    public const int ShipyardWoodCost       = 30;
    public const int ShipyardSteelCost      = 20;
    public const int ShipyardClothCost      = 10;
    public const int ShipyardBuildHours     = 5;

    // Free placement building size (world units)
    public const float BuildingPlacementSize = 1.12f;
}
