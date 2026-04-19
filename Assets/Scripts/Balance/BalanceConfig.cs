public static class BalanceConfig
{
    // Time
    public const float DayLengthSeconds       = 240f;   // 1 in-game day = 4 real minutes

    // Population food consumption (per day)
    public const float AdultFoodPerDay         = 1.0f;
    public const float ChildFoodPerDay         = 0.5f;

    // Building production (per worker per day)
    public const float SawmillWoodPerWorker    = 1.2f;
    public const float SteelworksPerWorker     = 0.6f;
    public const float ClothWorksPerWorker     = 0.5f;
    public const float CookhouseFoodPerWorker  = 2.0f;

    // Shipyard
    public const float ShipProgressPerWorker   = 12f;   // progress points per hourly tick
    public const float ShipProgressRequired    = 100f;

    // Ship build cost (per active worker per tick)
    public const int   ShipWoodCostPerWorker   = 1;
    public const int   ShipSteelCostPerWorker  = 1;
    public const int   ShipClothCostDivisor    = 2;     // cloth = max(1, workers / divisor)

    // Ship capacity
    public const int   ShipMaxPassengers       = 50;
    public const int   ShipFoodRequired        = 750;   // 15 food/passenger x 5 days
    public const int   ShipMaxSailors          = 5;
    public const int   ShipyardMaxWorkers      = 10;

    // Worker limits
    public const int   DefaultBuildingMaxWorkers = 10;

    // Production bonuses
    public const float EngineerProductionBonus = 1.25f;
    public const float EngineerShipBonus       = 1.35f;
}
