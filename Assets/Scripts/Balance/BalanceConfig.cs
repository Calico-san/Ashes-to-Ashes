public static class BalanceConfig
{
    // ---- Radni dan ----
    // Dan traje od DayStartHour do DayEndHour; sve stope "po danu" dijele se s
    // HoursPerDay, NE s 24. Ranije je dijeljenje s 24 uz 14-satni dan davalo
    // 58,3 % nominalnog ucinka i za proizvodnju i za potrosnju hrane.
    // GameController cita ove vrijednosti — ne drzi vlastite konstante.
    public const int   DayStartHour  = 6;
    public const int   DayEndHour    = 20;
    public const float HoursPerDay   = DayEndHour - DayStartHour;   // 14

    public const float DayStartMinutes = DayStartHour * 60f;   // 360
    public const float DayEndMinutes   = DayEndHour   * 60f;   // 1200

    // Population food consumption (per day)
    public const float AdultFoodPerDay         = 1.0f;
    public const float ChildFoodPerDay         = 0.5f;

    // ---- Proizvodnja: CIJELIH jedinica po radniku po SATU ----
    // Stopa je cijeli broj da izlaz zgrade bude cijeli broj pri svakom broju radnika.
    // Ranije su stope bile "po danu" i dijelile se sa 14, pa je 15 radnika u Sawmillu
    // davalo 4,29 drva na sat. Zaokruzivanje takve stope nije rjesenje: pri
    // 0,143/radniku/sat (Steelworks) radnici 1-3 daju 0, a 4-10 svi daju 1 — dodavanje
    // radnika nema vidljiv ucinak. Cjelobrojna stopa po radniku to uklanja u korijenu.
    public const int SawmillWoodPerWorkerPerHour     = 2;  // 15 rad. = 30/h  = 420/dan
    public const int SteelworksSteelPerWorkerPerHour = 1;  // 15 rad. = 15/h  = 210/dan
    public const int FiberworksClothPerWorkerPerHour = 1;  // 15 rad. = 15/h  = 210/dan
    public const int FiberworksRopePerWorkerPerHour  = 1;  // isti radnici, drugi izlaz
    public const int CookhouseFoodPerWorkerPerHour   = 1;  // 15 rad. = 15/h  = 210/dan

    // ---- Brodogradnja ----
    // Radnici odreduju SAMO brzinu; trosak je fiksan po brodu.
    // 1 bod po radniku po satu, a potrebno je HoursPerDay * ShipyardMaxWorkers
    // bodova — dakle puno brodogradiliste isporuci tocno jedan brod na dan.
    // Promjena duljine dana ili kapaciteta ne razbija taj omjer.
    public const float ShipProgressPerWorker   = 1.0f;
    public const float ShipProgressRequired    = HoursPerDay * ShipyardMaxWorkers;  // 140

    // Fiksni trosak jednog broda — naplacuje se jednokratno pri polaganju kobilice.
    // Izvedeno iz raspolozive radne snage: 495 slobodnih − 126 za prehranu = 369,
    // od cega 120 pokriva 3 brodogradilista + 2 Sawmilla + 2 Steelworksa + 2 Fiberworksa.
    // Uz udvostrucenu cijenu 3 broda dnevno traze 1680 drva i po 840 celika,
    // tkanine i konopa — 4 Sawmilla i po 4 Steelworksa i Fiberworksa (180 radnika).
    // Cijeli run: 11 brodova = 6160 / 3080 / 3080 / 3080.
    // Udvostruceno: pri prijasnjoj cijeni 3 brodogradilista su pokrivala potrebu
    // s dva Sawmilla, pa je brod bio prejeftin. Sada trazi cetiri.
    public const int   ShipWoodCost            = 560;
    public const int   ShipSteelCost           = 280;
    public const int   ShipClothCost           = 280;
    public const int   ShipRopeCost            = 280;

    // Ship capacity
    public const int   ShipFoodLoadStep        = 100; // hrane po kliku
    public const int   ShipPassengerLoadStep   = 10;  // putnika po kliku
    public const int   ShipMaxPassengers       = 100;
    public const int   ShipFoodRequired        = 700;
    public const int   ShipyardMaxWorkers      = 10;

    // Worker limits
    public const int   DefaultBuildingMaxWorkers = 15;

    // Production bonuses
    public const float EngineerProductionBonus = 1.25f;
    public const float EngineerShipBonus       = 1.35f;

    // Building construction costs [wood, steel, cloth] + time in hours
    //
    // Vrijeme gradnje prati cijenu, mjereno u 14-satnom danu:
    //   Hunter's Hut  2h  (56 wood)                 — najjeftinija, gradi se u nizu
    //   Sawmill       3h  (70 wood)
    //   Cookhouse     3h  (70 wood + 35 steel)      — bilo 1h "za testiranje"
    //   Scout Station 3h  (70 wood + 35 steel)
    //   Fiberworks    4h  (105 wood + 35 steel)
    //   Steelworks    5h  (140 wood)                — najskuplja obicna zgrada
    //   Shipyard      8h  (280/105/70)              — vise od pola dana, prava odluka
    public const int SawmillWoodCost        = 70;
    public const int SawmillSteelCost       = 0;
    public const int SawmillClothCost       = 0;
    public const int SawmillBuildHours      = 3;

    public const int SteelworksWoodCost     = 140;
    public const int SteelworksSteelCost    = 0;
    public const int SteelworksClothCost    = 0;
    public const int SteelworksBuildHours   = 5;

    public const int FiberworksWoodCost     = 105;
    public const int FiberworksSteelCost    = 35;
    public const int FiberworksClothCost    = 0;
    public const int FiberworksBuildHours   = 4;

    public const int CookhouseWoodCost      = 70;
    public const int CookhouseSteelCost     = 35;
    public const int CookhouseClothCost     = 0;
    public const int CookhouseBuildHours    = 3;

    public const int ShipyardWoodCost       = 280;
    public const int ShipyardSteelCost      = 105;
    public const int ShipyardClothCost      = 70;
    public const int ShipyardBuildHours     = 8;

    // Free placement building size (world units)
    public const float BuildingPlacementSize = 1.0f;   // 32x32px @ PPU=32

    // ---- Raw Food ----
    // Lanac je 1:1 i po satu: jedan lovac donese 1 sirove hrane, jedan kuhar ju
    // pretvori u 1 kuhane. Stopa je morala postati cjelobrojna zajedno s ostalima
    // (5/dan = 0,357/radniku/sat nije davalo cijeli broj ni pri jednom broju radnika).
    public const int   RawFoodPerWorkerPerHour   = 1;   // Hunter's Hut, 5 rad. = 5/h = 70/dan
    public const float CookhouseRawFoodPerWorker = 1f;  // Cookhouse trosi 1 raw food/radniku/sat
    public const float CookhouseNormalMultiplier = 1.0f;
    public const float CookhouseLowMultiplier    = 0.5f; // no raw food

    // ---- Hunters Hut ----
    public const int   HuntersHutMaxWorkers   = 5;
    public const int   HuntersHutWoodCost      = 56;
    public const int   HuntersHutSteelCost     = 0;
    public const int   HuntersHutBuildHours    = 2;

    // ---- Scout Station ----
    public const int   ScoutStationMaxWorkers  = 5;
    public const int   ScoutStationWoodCost    = 70;
    public const int   ScoutStationSteelCost   = 35;
    public const int   ScoutStationBuildHours  = 3;

    // ---- Rusenje i otkazivanje ----
    // Polovica ulozenog se vraca. Puni povrat bi gradnju ucinio bezrizicnom
    // (igrac bi mogao zauzeti svako polje i predomisliti se bez posljedice),
    // a nula bi gumb pretvorila u cistu kaznu. Zaokruzuje se AwayFromZero, isto
    // kao proizvodnja, da se pravilo ne mijenja od mjesta do mjesta.
    public const float RefundRatio = 0.5f;

    /// <summary>Polovica iznosa, zaokruzeno na vise na pola (2.5 -> 3, ne 2).</summary>
    public static int Refund(int amount)
        => amount <= 0
            ? 0
            : (int)System.Math.Round(amount * (double)RefundRatio,
                                     System.MidpointRounding.AwayFromZero);

    // ---- Building Upgrade Costs ----
    public const int UpgradeWoodCost = 7;
    public const int UpgradeSteelCost = 7;
    public const int UpgradeClothCost = 7;
    public const float UpgradeProductionBonus = 1.2f;
}
