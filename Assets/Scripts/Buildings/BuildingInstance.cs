using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A production building on the island. Handles worker assignment,
/// hourly production, and — if IsShipyard — ship construction.
/// </summary>
public class BuildingInstance : MonoBehaviour
{
    // ---- Public state ----
    public string       DisplayName  { get; private set; }
    public string _baseDisplayName;
    public ResourceType OutputType   { get; private set; }
    public bool         IsShipyard   { get; private set; }
    public bool         IsTownHall      { get; private set; }
    public Vector2      Size            { get; private set; }
    public BuildingType BuildingTypeEnum { get; private set; }
    public int          MaxWorkers   { get; private set; }
    public int          AssignedWorkers   => _workers.Count;
    public int          AssignedEngineers => _engineers.Count;
    public int          MaxEngineers      => Mathf.Max(1, MaxWorkers / 5); // max 20% engineers
    public bool         HasEngineer       => EngineersInside > 0;
    public int          WorkersInside
    {
        get
        {
            int n = 0;
            foreach (var w in _workers)
                if (w == null || w.IsInside) n++;
            return n;
        }
    }
    public int          EngineersInside
    {
        get
        {
            int n = 0;
            foreach (var e in _engineers)
                if (e == null || e.IsInside) n++;
            return n;
        }
    }

    // Production info (read by UI)
    // Stopa i ukupan izlaz su cijeli brojevi — vidi EconomyCalculator.
    public int OutputPerWorkerPerHour =>
        BuildingTypeEnum == BuildingType.HuntersHut
            ? EconomyCalculator.RawFoodPerWorkerPerHour()   // Hunter's Hut outputs Raw Food, not cooked Food
            : EconomyCalculator.ProductionPerWorkerPerHour(OutputType);
    public float EngineerBonus => HasEngineer
        ? (IsShipyard ? BalanceConfig.EngineerShipBonus : BalanceConfig.EngineerProductionBonus)
        : 1.0f;
    public int TotalOutputPerHour =>
        EconomyCalculator.TotalOutputPerHour(OutputPerWorkerPerHour, WorkersInside,
                                             EngineerBonus * ProductionMultiplier);

    /// <summary>
    /// Drugi izlazni resurs zgrade, ili null ako ga nema. Zasad samo Fiberworks
    /// (Cloth + Rope). Rope se proizvodio u ProduceHourly, ali ga TotalOutputPerHour
    /// nije obuhvacao pa ga UI nije imao odakle procitati — resurs je rastao bez
    /// ijednog vidljivog izvora.
    /// Logika ostaje ovdje, a ne u UIControlleru, da UI ne pocne poznavati formule.
    /// </summary>
    public ResourceType? SecondaryOutputType =>
        BuildingTypeEnum == BuildingType.Fiberworks ? ResourceType.Rope : (ResourceType?)null;

    public int SecondaryOutputPerWorkerPerHour =>
        SecondaryOutputType.HasValue
            ? EconomyCalculator.ProductionPerWorkerPerHour(SecondaryOutputType.Value)
            : 0;

    public int SecondaryTotalPerHour =>
        EconomyCalculator.TotalOutputPerHour(SecondaryOutputPerWorkerPerHour, WorkersInside,
                                             EngineerBonus * ProductionMultiplier);

    // ---- Shipyard info (read by UI) ----
    public float ShipProgress        => _shipProgress;
    public int   ShipCount           => _shipCount;

    /// <summary>True kad je fiksni trosak za tekuci brod vec placen.</summary>
    public bool  KeelLaid            => _keelLaid;

    /// <summary>
    /// True kad je igrac narucio brod. Bez narudzbe brodogradiliste ne trosi
    /// resurse ni kad je puno radnika — gradnja vise ne krece sama od sebe.
    /// </summary>
    public bool  ShipOrdered         => _shipOrdered;

    /// <summary>Ima li igrac resurse za fiksni trosak jednog broda.</summary>
    public bool  CanAffordShip => _game != null && _game.HasResources(
        BalanceConfig.ShipWoodCost, BalanceConfig.ShipSteelCost,
        BalanceConfig.ShipClothCost, BalanceConfig.ShipRopeCost);

    public float ShipProgressPercent => BalanceConfig.ShipProgressRequired > 0f
        ? _shipProgress / BalanceConfig.ShipProgressRequired * 100f
        : 0f;

    /// <summary>Procjena preostalih sati igre; -1 kad nema radnika.</summary>
    public float ShipHoursRemaining =>
        EconomyCalculator.ShipHoursRemaining(_shipProgress, WorkersInside, EngineerBonus);

    // ---- Private ----
    private readonly List<WorkerAgent>  _workers     = new();
    private readonly List<WorkerAgent>  _engineers   = new();
    private readonly List<GameObject>   _shipObjects = new();
    private List<Vector3>               _pathToTownHall;

    public bool HasPath => _pathToTownHall != null && _pathToTownHall.Count > 0;
    private SpriteRenderer              _renderer;
    private BuildingAnimator            _animator;
    private Color                       _normalColor;
    private Color                       _selectedColor;
    private GameController     _game;
    private float                       _shipProgress;
    private int                         _shipCount;
    private bool                        _keelLaid;
    private bool                        _shipOrdered;

    // ---- Init ----

    public void Initialize(GameController game, string displayName,
        ResourceType outputType, Color color, Vector3 position, Vector2 size,
        bool isShipyard = false, BuildingType buildingType = BuildingType.Cookhouse)
    {
        _game       = game;
        _baseDisplayName = displayName;
        DisplayName = displayName;
        OutputType  = outputType;
        IsShipyard       = isShipyard;
        BuildingTypeEnum = buildingType != BuildingType.Cookhouse
            ? buildingType
            : BuildingTypeFromResource(outputType, isShipyard);

        if (BuildingTypeEnum == BuildingType.HuntersHut || BuildingTypeEnum == BuildingType.ScoutStation)
            MaxWorkers = BalanceConfig.HuntersHutMaxWorkers;
        else
            MaxWorkers = isShipyard ? BalanceConfig.ShipyardMaxWorkers : BalanceConfig.DefaultBuildingMaxWorkers;
        // Otisak se IZVODI IZ TIPA, nikad ne cita iz proslijedenog `size`. Taj je
        // dolazio iz tri izvora (TryPlaceBuilding, CompleteBuild, ApplyLoadData), a
        // onaj iz spremljene igre je mogao nositi zastarjele vrijednosti iz starijih
        // verzija (npr. 3x4), koje su zavrsavale kao localScale i mnozile velicinu
        // sprite-a. Jedan izvor istine to sprjecava, a promjena otiska automatski
        // vrijedi i za stare zapise.
        float footprint = BuildingFootprint.SizeFor(BuildingTypeEnum);
        Size = new Vector2(footprint, footprint);

        transform.position   = position;
        transform.localScale = new Vector3(footprint, footprint, 1f);

        // Kad postoji pravi sprite, tinta mora biti bijela — inace placeholder
        // boja tipa zgrade zaprlja pixel art.
        bool hasArt    = BuildingAnimator.HasArtFor(BuildingTypeEnum);
        _normalColor   = hasArt ? Color.white : color;
        _selectedColor = hasArt
            ? new Color(1f, 0.92f, 0.55f)
            : Color.Lerp(color, Color.white, 0.35f);

        _renderer        = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = SimpleShapeFactory.CreateFilledSquareSprite(_normalColor);
        _renderer.sortingOrder = 5;

        // Collider je u lokalnom prostoru, a localScale je vec otisak — zato 1x1
        // ovdje pokriva cijeli otisak bez obzira koliki on bio.
        var col  = gameObject.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        _animator = gameObject.AddComponent<BuildingAnimator>();
        _animator.Setup(BuildingTypeEnum, color);   // BuildingTypeEnum — HuntersHut i ScoutStation
                                                    // oboje daju Food pa bi ih BuildingTypeFromResource
                                                    // pogresno mapirao u Cookhouse
        _animator.SetBuilt();
    }

    // ---- Worker management ----

    /// <summary>Called by GameController after placement — stores pre-computed path.</summary>
    public void SetPath(List<Vector3> path)
    {
        _pathToTownHall = path;
    }

    public bool TryAssignWorker()
    {
        if (IsTownHall) return false;
        if (AssignedWorkers >= MaxWorkers || !_game.CanCreateWorkerAgent()) return false;

        var agent = new GameObject($"{DisplayName}_Worker_{AssignedWorkers + 1}")
            .AddComponent<WorkerAgent>();
        agent.Initialize(_game.WorkerSpawnPoint, WorkerSlot(_workers.Count), _game.WorkerSprite);
        var reg = SpriteRegistry.Instance;
        if (reg != null) agent.SetWalkFrames(reg.WorkerWalkFrames, reg.WalkFps);

        _workers.Add(agent);
        _game.OnWorkerAssigned();
        return true;
    }

    public bool RemoveWorker()
    {
        if (_workers.Count == 0) return false;
        var worker = _workers[_workers.Count - 1];
        _workers.RemoveAt(_workers.Count - 1);
        worker?.LeaveBuilding(_game.WorkerSpawnPoint);
        RepositionWorkers();
        _game.OnWorkerRemoved();
        return true;
    }

    public bool TryAssignEngineer()
    {
        if (IsTownHall) return false;
        if (AssignedEngineers >= MaxEngineers || !_game.CanCreateEngineerAgent()) return false;
        var agent = new GameObject($"{DisplayName}_Engineer_{AssignedEngineers + 1}")
            .AddComponent<WorkerAgent>();
        agent.Initialize(_game.WorkerSpawnPoint, EngineerSlot(_engineers.Count), _game.EngineerSprite, _pathToTownHall);
        var engReg = SpriteRegistry.Instance;
        if (engReg != null)
            agent.SetWalkFrames(
                engReg.EngineerWalkFrames != null && engReg.EngineerWalkFrames.Length >= 2
                    ? engReg.EngineerWalkFrames
                    : engReg.WorkerWalkFrames,
                engReg.WalkFps);
        _engineers.Add(agent);
        _game.OnEngineerAssigned();
        return true;
    }

    public bool RemoveEngineer()
    {
        if (_engineers.Count == 0) return false;
        var eng = _engineers[_engineers.Count - 1];
        _engineers.RemoveAt(_engineers.Count - 1);
        eng?.LeaveBuilding(_game.WorkerSpawnPoint);
        _game.OnEngineerRemoved();
        return true;
    }

    public bool TryAssignEngineerSilent()
    {
        if (AssignedEngineers >= MaxEngineers || !_game.CanCreateEngineerAgent()) return false;
        _engineers.Add(null);
        _game.OnEngineerAssigned();
        return true;
    }

    private void Update()
    {
        _animator?.SetProducing(WorkersInside > 0 && !IsTownHall);
    }

    /// <summary>
    /// Osigurac nad velicinom. Glavni popravak je u BuildingAnimator.SetState, koje
    /// vraca scale odmah nakon sto ga Unity prepise prirodnom velicinom sprite-a.
    /// Ovo hvata eventualni preostali slucaj (npr. buduce stanje koje ne prolazi
    /// kroz SetState) i ne ispisuje nista da ne zatrpava Console.
    /// </summary>
    private void LateUpdate()
    {
        float   f        = Size.x;
        Vector3 expected = new Vector3(f, f, 1f);

        if ((transform.localScale - expected).sqrMagnitude > 0.000001f)
            transform.localScale = expected;
    }

    /// <summary>Assign worker instantly without walk animation. Used on game load.</summary>
    public bool TryAssignWorkerSilent()
    {
        if (AssignedWorkers >= MaxWorkers || !_game.CanCreateWorkerAgent()) return false;
        _workers.Add(null); // null = worker is "inside" but no visual agent
        _game.OnWorkerAssigned();
        return true;
    }

    // ---- Hourly tick ----

    public void ProduceHourly()
    {
        int active = WorkersInside;
        if (active <= 0) return;

        float multiplier = EngineerBonus * ProductionMultiplier;

        if (IsShipyard) { TickShipyard(active, multiplier); return; }

        // Hunter's Hut — produces Raw Food at its own per-worker rate
        if (BuildingTypeEnum == BuildingType.HuntersHut)
        {
            int raw = EconomyCalculator.TotalOutputPerHour(OutputPerWorkerPerHour, active, multiplier);
            if (raw > 0) _game.AddRawFood(raw);
            return;
        }

        // Fiberworks — produces both Cloth and Rope from the same workers
        if (OutputType == ResourceType.Cloth)
        {
            int cloth = EconomyCalculator.TotalOutputPerHour(OutputPerWorkerPerHour, active, multiplier);
            int rope  = EconomyCalculator.TotalOutputPerHour(SecondaryOutputPerWorkerPerHour, active, multiplier);
            if (cloth > 0) _game.AddResource(ResourceType.Cloth, cloth);
            if (rope  > 0) _game.AddResource(ResourceType.Rope,  rope);
            return;
        }

        if (BuildingTypeEnum == BuildingType.Cookhouse)
        {
            // Consume raw food — CookhouseRawFoodPerWorker per worker per hour
            int rawNeeded   = Mathf.RoundToInt(active * BalanceConfig.CookhouseRawFoodPerWorker);
            int rawConsumed = _game.ConsumeRawFood(rawNeeded);
            multiplier *= rawConsumed >= rawNeeded
                ? BalanceConfig.CookhouseNormalMultiplier
                : BalanceConfig.CookhouseLowMultiplier;
        }

        // Standard single-output buildings (Sawmill, Steelworks, Cookhouse, ...)
        int output = EconomyCalculator.TotalOutputPerHour(OutputPerWorkerPerHour, active, multiplier);
        if (output > 0) _game.AddResource(OutputType, output);
    }

    // ---- Selection ----

    public void SetSelected(bool selected)
        => _renderer.color = selected ? _selectedColor : _normalColor;

    /// <summary>Mark as Town Hall — disables worker assignment and production.</summary>
    public void SetTownHall(bool value) => IsTownHall = value;

    /// <summary>Restore shipyard progress from a save (O5 — ranije se gubilo).</summary>
    public void RestoreShipyardState(float progress, int shipCount, bool keelLaid, bool ordered)
    {
        _shipProgress = progress;
        _shipCount    = shipCount;
        _keelLaid     = keelLaid;
        _shipOrdered  = ordered;
    }

    /// <summary>
    /// Igrac narucuje jedan brod. Vraca false ako je narudzba vec u tijeku —
    /// brodogradiliste gradi tocno jedan brod odjednom, pa za svaki sljedeci
    /// treba novi klik.
    /// </summary>
    public bool OrderShip()
    {
        if (!IsShipyard || _shipOrdered) return false;
        _shipOrdered  = true;
        _shipProgress = 0f;   // nova narudzba uvijek krece od nule
        return true;
    }

    // ---- Private ----

    /// <summary>
    /// Brodogradnja: broj radnika odreduje ISKLJUCIVO brzinu, trosak je fiksan
    /// po brodu i naplacuje se jednokratno pri polaganju kobilice.
    ///
    /// Ranije se svaki sat naplacivalo po aktivnom radniku, pa je ukupna cijena
    /// broda ovisila o broju radnika i trajanju gradnje (1 radnik = 20 cloth,
    /// 10 radnika = 10 cloth za isti brod). Igrac to nije mogao ni vidjeti ni
    /// planirati jer je panel prikazivao samo "Cost/tick".
    /// </summary>
    private void TickShipyard(int activeWorkers, float bonus = 1f)
    {
        // Bez narudzbe se ne radi nista: brodogradiliste ceka da igrac pritisne
        // "Build ship". Ranije je gradnja kretala sama cim bi radnik usao unutra,
        // pa su resursi nestajali bez ijedne igraceve odluke.
        if (!_shipOrdered) return;

        // Kobilica: naplati fiksni trosak jednom po brodu. "Sve ili nista" —
        // ako nedostaje ijedan resurs, ne trosi se nista i napredak stoji.
        if (!_keelLaid)
        {
            if (!_game.HasResources(BalanceConfig.ShipWoodCost,
                                    BalanceConfig.ShipSteelCost,
                                    BalanceConfig.ShipClothCost,
                                    BalanceConfig.ShipRopeCost)) return;

            _game.ConsumeShipResources(BalanceConfig.ShipWoodCost,
                                       BalanceConfig.ShipSteelCost,
                                       BalanceConfig.ShipClothCost,
                                       BalanceConfig.ShipRopeCost);
            _keelLaid = true;
        }

        _shipProgress += EconomyCalculator.ShipProgressPerHour(activeWorkers) * bonus;

        // if, a NE while: jedna narudzba = tocno jedan brod. Petlja je mogla
        // isporuciti dva broda iz jedne narudzbe, a naplacen je bio samo jedan.
        if (_shipProgress >= BalanceConfig.ShipProgressRequired)
        {
            _game.AddResource(ResourceType.Ships, 1);
            SpawnShip();

            // Visak napretka se NE prenosi na sljedeci brod — svaki brod je
            // zasebna odluka i krece od nule.
            _shipProgress = 0f;
            _keelLaid     = false;   // sljedeci brod trazi novu naplatu
            _shipOrdered  = false;   // i novu narudzbu
        }
    }

    private void SpawnShip()
    {
        int shipNumber = _game.GetNextShipNumber();
        var go   = new GameObject($"Ship_{shipNumber}");
        var ship = go.AddComponent<ShipInstance>();

        // Svaki brod sada ima vizual. Prvi je najblizi brodogradilistu i crta se
        // na vrhu; sljedeci se slazu kao lepeza karata prema pucini, pa im viri
        // po jedan kut. Sortiranje pada s indeksom da poredak ostane citljiv.
        ship.Initialize(_game, shipNumber, ShipSlot(_shipCount), true,
                        Mathf.Max(8, 60 - _shipCount * 4));

        _shipObjects.Add(go);
        _game.RegisterShip(ship);
        _shipCount++;
    }

    // ---- Razmjestaj brodova ----

    private bool    _shipAnchorReady;
    private Vector3 _shipAnchor;
    private Vector2 _shipAway;   // od otoka prema pucini
    private Vector2 _shipPerp;

    /// <summary>
    /// Pozicija i-tog broda: usidreno na najblize Ocean polje uz brodogradiliste,
    /// pa svaki sljedeci pomaknut prema pucini i malo bocno — slaganje kao karte.
    /// </summary>
    private Vector3 ShipSlot(int index)
    {
        EnsureShipAnchor();
        Vector2 offset = _shipAway * (index * 0.26f) + _shipPerp * (index * 0.11f);
        return _shipAnchor + new Vector3(offset.x, offset.y, 0f);
    }

    /// <summary>
    /// Trazi najblize Ocean polje oko brodogradilista. Pravilo postavljanja jamci
    /// da ga ima u radijusu 1, ali se pretraga siri do 4 polja za slucaj da je
    /// karta u meduvremenu precrtana.
    /// </summary>
    private void EnsureShipAnchor()
    {
        if (_shipAnchorReady) return;
        _shipAnchorReady = true;

        Vector3 origin = transform.position;
        _shipAnchor    = origin + new Vector3(0f, -1.5f, 0f);   // fallback bez karte

        var renderer = IslandTilemapRenderer.Instance;
        if (renderer != null)
        {
            float best = float.MaxValue;
            for (int dx = -4; dx <= 4; dx++)
            for (int dy = -4; dy <= 4; dy++)
            {
                var candidate = new Vector2(origin.x + dx, origin.y + dy);
                if (renderer.GetTileAtWorld(candidate) != TileType.Ocean) continue;

                float d = (candidate - new Vector2(origin.x, origin.y)).sqrMagnitude;
                if (d >= best) continue;
                best        = d;
                _shipAnchor = new Vector3(candidate.x, candidate.y, 0f);
            }
        }

        Vector2 away = new Vector2(_shipAnchor.x - origin.x, _shipAnchor.y - origin.y);
        _shipAway = away.sqrMagnitude > 0.0001f ? away.normalized : new Vector2(0f, -1f);
        _shipPerp = new Vector2(-_shipAway.y, _shipAway.x);

        // Pomak od obale da trup ne sjedi na rubnom polju kopna.
        _shipAnchor += new Vector3(_shipAway.x, _shipAway.y, 0f) * 0.5f;
    }

    private void RepositionWorkers()
    {
        for (int i = 0; i < _workers.Count; i++)
            _workers[i]?.SetNewTarget(WorkerSlot(i));
    }

    private static BuildingType BuildingTypeFromResource(ResourceType type, bool isShipyard)
    {
        if (isShipyard) return BuildingType.Shipyard;
        switch (type)
        {
            case ResourceType.Wood:  return BuildingType.Sawmill;
            case ResourceType.Steel: return BuildingType.Steelworks;
            case ResourceType.Cloth: return BuildingType.Fiberworks;
            case ResourceType.Food:  return BuildingType.Cookhouse;
            default:                 return BuildingType.Sawmill;
        }
    }

    // Mjesta radnika su u svjetskim jedinicama, pa se moraju skalirati otiskom —
    // inace bi se na brodogradilistu 2x2 svi zbili u sredinu.
    private Vector3 WorkerSlot(int index)
    {
        int col = index % 5, row = index / 5;
        float f = Size.x;
        return transform.position
             + new Vector3((-0.45f + col * 0.22f) * f, (0.35f - row * 0.22f) * f, 0f);
    }

    private Vector3 EngineerSlot(int index)
    {
        // Engineers stand slightly above workers, tinted differently
        float f = Size.x;
        return transform.position
             + new Vector3((-0.45f + index * 0.22f) * f, 0.58f * f, 0f);
    }

    // ---- Upgrade state ----
    private int _upgradeLevel = 1;
    public int UpgradeLevel => _upgradeLevel;

    public float ProductionMultiplier => 1.0f + (_upgradeLevel * (BalanceConfig.UpgradeProductionBonus - 1.0f));

    public void UpgradeBuilding()
    {
        _upgradeLevel++;
        DisplayName = _baseDisplayName + $" (Level {_upgradeLevel})";
    }

}
