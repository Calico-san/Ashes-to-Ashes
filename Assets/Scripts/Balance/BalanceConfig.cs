public static class BalanceConfig
{
    // Time
    public const float DayLengthSeconds       = 480f;   // 8 min/day at 1x

    // Population food consumption (per day)
    public const float AdultFoodPerDay         = 1.0f;
    public const float ChildFoodPerDay         = 1.0f;  // same as adults

    // Building production (per worker per day)
    public const float SawmillWoodPerWorker    = 4.0f;  // 10 workers = 40 wood/day
    public const float SteelworksPerWorker     = 2.0f;  // 10 workers = 20 steel/day
    public const float FiberworksClothPerWorker = 2.0f;  // 10 workers = 20 cloth/day
    public const float FiberworksRopePerWorker  = 2.0f;  // 10 workers = 20 rope/day
    public const float CookhouseFoodPerWorker  = 8.0f;  // 15 workers = 120 food/day

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
    public const int   DefaultBuildingMaxWorkers = 15;

    // Production bonuses
    public const float EngineerProductionBonus = 1.25f;
    public const float EngineerShipBonus       = 1.35f;

    // Building construction costs [wood, steel, cloth] + time in hours
    public const int SawmillWoodCost        = 10;
    public const int SawmillSteelCost       = 0;
    public const int SawmillClothCost       = 0;
    public const int SawmillBuildHours      = 4;

    public const int SteelworksWoodCost     = 20;
    public const int SteelworksSteelCost    = 0;
    public const int SteelworksClothCost    = 0;
    public const int SteelworksBuildHours   = 5;

    public const int FiberworksWoodCost     = 15;
    public const int FiberworksSteelCost    = 5;
    public const int FiberworksClothCost    = 0;
    public const int FiberworksBuildHours   = 4;

    public const int CookhouseWoodCost      = 10;
    public const int CookhouseSteelCost     = 5;
    public const int CookhouseClothCost     = 0;
    public const int CookhouseBuildHours    = 3;

    public const int ShipyardWoodCost       = 40;
    public const int ShipyardSteelCost      = 15;
    public const int ShipyardClothCost      = 10;
    public const int ShipyardBuildHours     = 8;

    // Free placement building size (world units)
    public const float BuildingPlacementSize = 1.0f;   // 32x32px @ PPU=32

    // ---- Hunters Hut ----
    public const int   HuntersHutMaxWorkers   = 5;
    public const float HuntersHutFoodPerWorker = 12f;  // 60 food/day at full capacity
    public const int   HuntersHutWoodCost      = 8;
    public const int   HuntersHutSteelCost     = 0;
    public const int   HuntersHutBuildHours    = 3;

    // ---- Scout Station ----
    public const int   ScoutStationMaxWorkers  = 5;
    public const int   ScoutStationWoodCost    = 10;
    public const int   ScoutStationSteelCost   = 5;
    public const int   ScoutStationBuildHours  = 4;


}