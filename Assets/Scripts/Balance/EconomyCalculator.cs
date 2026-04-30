/// <summary>
/// Pure static helper — all game economy formulas in one place.
/// No Unity dependencies, fully testable.
/// </summary>
public static class EconomyCalculator
{
    public static float FoodConsumptionPerHour(int adults, int children)
    {
        return (adults * BalanceConfig.AdultFoodPerDay
              + children * BalanceConfig.ChildFoodPerDay) / 24f;
    }

    public static float FoodConsumptionPerDay(int adults, int children)
        => adults * BalanceConfig.AdultFoodPerDay + children * BalanceConfig.ChildFoodPerDay;

    public static float ProductionPerWorkerPerHour(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Food:  return BalanceConfig.CookhouseFoodPerWorker  / 24f;
            case ResourceType.Wood:  return BalanceConfig.SawmillWoodPerWorker    / 24f;
            case ResourceType.Steel: return BalanceConfig.SteelworksPerWorker     / 24f;
            case ResourceType.Cloth: return BalanceConfig.FiberworksClothPerWorker / 24f;
            case ResourceType.Rope:  return BalanceConfig.FiberworksRopePerWorker  / 24f;
            default: return 0f;
        }
    }

    public static int ShipWoodCost(int activeWorkers)   => activeWorkers * BalanceConfig.ShipWoodCostPerWorker;
    public static int ShipSteelCost(int activeWorkers)  => activeWorkers * BalanceConfig.ShipSteelCostPerWorker;
    public static int ShipClothCost(int activeWorkers) => UnityEngine.Mathf.Max(1, activeWorkers / BalanceConfig.ShipClothCostDivisor);
    public static int ShipRopeCost(int activeWorkers)  => UnityEngine.Mathf.Max(1, activeWorkers / BalanceConfig.ShipRopeCostDivisor);
    public static float ShipProgress(int activeWorkers) => activeWorkers * BalanceConfig.ShipProgressPerWorker;
}
