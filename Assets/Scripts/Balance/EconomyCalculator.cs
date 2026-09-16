/// <summary>
/// Pure static helper — all game economy formulas in one place.
/// No Unity dependencies, fully testable.
///
/// Dan u igri traje 06:00–20:00, pa se ProduceHourly izvrsi HoursPerDay (14) puta
/// dnevno. Stope proizvodnje su zato definirane po radniku po SATU i cjelobrojne su,
/// da izlaz zgrade bude cijeli broj pri svakom broju radnika.
/// </summary>
public static class EconomyCalculator
{
    public static float FoodConsumptionPerHour(int adults, int children)
    {
        return FoodConsumptionPerDay(adults, children) / BalanceConfig.HoursPerDay;
    }

    public static float FoodConsumptionPerDay(int adults, int children)
        => adults * BalanceConfig.AdultFoodPerDay + children * BalanceConfig.ChildFoodPerDay;

    /// <summary>Cjelobrojna stopa po radniku po satu za zadani resurs.</summary>
    public static int ProductionPerWorkerPerHour(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Food:  return BalanceConfig.CookhouseFoodPerWorkerPerHour;
            case ResourceType.Wood:  return BalanceConfig.SawmillWoodPerWorkerPerHour;
            case ResourceType.Steel: return BalanceConfig.SteelworksSteelPerWorkerPerHour;
            case ResourceType.Cloth: return BalanceConfig.FiberworksClothPerWorkerPerHour;
            case ResourceType.Rope:  return BalanceConfig.FiberworksRopePerWorkerPerHour;
            default: return 0;
        }
    }

    public static int RawFoodPerWorkerPerHour() => BalanceConfig.RawFoodPerWorkerPerHour;

    /// <summary>
    /// Ukupan satni izlaz zgrade. Baza (stopa × radnici) je vec cijeli broj; jedini
    /// izvor razlomka su multiplikatori (inzenjer 1,25 / 1,35, nadogradnja 1,2,
    /// Cookhouse bez sirove hrane 0,5), pa se rezultat zaokruzuje jednom, ovdje.
    /// Zaokruzivanje je "od nule" jer .NET po zadanom zaokruzuje na parni broj —
    /// inace bi i 37,5 i 38,5 zavrsili kao 38.
    /// </summary>
    public static int TotalOutputPerHour(int perWorkerPerHour, int workers, float multiplier)
    {
        if (perWorkerPerHour <= 0 || workers <= 0) return 0;
        double raw = (double)perWorkerPerHour * workers * multiplier;
        return (int)System.Math.Round(raw, System.MidpointRounding.AwayFromZero);
    }

    /// <summary>Dnevni izlaz iz satnog — za prikaz i proracune balansa.</summary>
    public static float PerDay(int perHour) => perHour * BalanceConfig.HoursPerDay;

    // ---- Brodogradnja ----
    // Trosak je fiksan (BalanceConfig.ShipWoodCost i dalje), radnici odreduju brzinu.

    /// <summary>Bodova napretka po satu za zadani broj aktivnih radnika.</summary>
    public static float ShipProgressPerHour(int activeWorkers)
        => activeWorkers * BalanceConfig.ShipProgressPerWorker;

    /// <summary>
    /// Procjena preostalih sati igre do isporuke broda. Vraca -1 kad nema radnika
    /// (brod nikad ne bi bio gotov), pa UI moze ispisati "—" umjesto beskonacnosti.
    /// </summary>
    public static float ShipHoursRemaining(float progress, int activeWorkers, float bonus)
    {
        float perHour = ShipProgressPerHour(activeWorkers) * bonus;
        if (perHour <= 0f) return -1f;
        return (BalanceConfig.ShipProgressRequired - progress) / perHour;
    }
}
