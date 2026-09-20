using UnityEngine;

/// <summary>
/// A completed ship. Manages passenger boarding and food loading.
///
/// Mornari su uklonjeni: brod se ne popunjava posadom nego iskljucivo putnicima.
/// Putnici se vode odvojeno po djeci i odraslima jer se pri iskrcaju moraju vratiti
/// u ispravan bazen (dijete u `children`, odrasli u `_freeWorkers`).
///
/// Klasa NE dira GameController — naplatu hrane i populacije radi pozivatelj
/// (GameController.AddPassengersToSelectedShip / LoadFoodToSelectedShip).
/// Ranije je LoadFood sam zvao _game.TryConsumeFood, a GameController je zatim
/// jos jednom oduzimao istu kolicinu — hrana se skidala dvostruko.
/// </summary>
public class ShipInstance : MonoBehaviour
{
    public string DisplayName   { get; private set; }
    public int    ShipNumber    { get; private set; }
    public int    MaxPassengers => BalanceConfig.ShipMaxPassengers;
    public int    RequiredFood  => BalanceConfig.ShipFoodRequired;

    public int  PassengerChildren { get; private set; }
    public int  PassengerAdults   { get; private set; }
    public int  Passengers        => PassengerChildren + PassengerAdults;
    public int  FreeSpace         => Mathf.Max(0, MaxPassengers - Passengers);
    public int  FoodLoaded        { get; private set; }
    public int  FoodMissing       => Mathf.Max(0, RequiredFood - FoodLoaded);

    public bool HasVisual { get; private set; }

    public bool IsReadyToSail => Passengers > 0 && FoodLoaded >= RequiredFood;

    /// <summary>True cim je brod krenuo — vise ne prima teret i ceka brisanje.</summary>
    public bool IsSailing { get; private set; }

    /// <summary>True kad je brod dovoljno daleko da ga se moze unistiti.</summary>
    public bool HasLeft { get; private set; }

    // ---- Private ----
    private SpriteRenderer _hullRenderer;
    private Color          _normalColor;
    private Color          _selectedColor;
    private Vector3        _sailDirection;
    private float          _sailedDistance;
    private float          _departDelay;   // sekundi do pokreta

    // Animacija broda. Postavlja se samo kad art postoji; placeholder od
    // pravokutnika nema frameove pa se petlja preskace.
    private bool  _animated;
    private float _frameTimer;
    private int   _frame;

    private const float SAIL_SPEED     = 1.6f;   // world unita/s
    private const float SAIL_DISTANCE  = 14f;    // koliko daleko prije brisanja

    // Kad cijela flota krene odjednom, brodovi bi se inace preklopili u jednu
    // mrlju. Zato svaki dobije svoje kasnjenje (krecu jedan za drugim) i svoj
    // kut u lepezi, pa se raztrkaju umjesto da putuju u koloni.
    //
    // Brzina je NAMJERNO jednaka za sve. Uz nasumicnu brzinu brod koji krene
    // kasnije zna biti brzi, pa kroz 14 jedinica plovidbe stigne onoga ispred
    // i prode kroz njega — provjereno simulacijom na flotama do 14 brodova.
    private const float DEPART_INTERVAL = 1.0f;   // razmak izmedu polazaka
    private const float FAN_MAX         = 20f;    // 40°
    private const float FAN_JITTER      = 3f;     // sitno rasipanje da lepeza ne bude pravilna

    private static readonly Vector2 IslandCenter = new Vector2(0f, 0.5f);

    // ---- Init ----

    public void Initialize(GameController game, int shipNumber, Vector3 position,
        bool hasVisual = true, int sortBase = 40)
    {
        ShipNumber  = shipNumber;
        DisplayName = $"Ship {shipNumber}";

        transform.position = position;
        // Brod je uvijek vodoravan — rotacija prema otoku je uklonjena. Slaganje
        // u lepezu (vidi BuildingInstance.ShipSlot) citljivo je samo ako su svi
        // trupovi poravnati isto.
        transform.rotation = Quaternion.identity;

        HasVisual = hasVisual;
        if (hasVisual)
        {
            BuildVisual(sortBase);
            var col  = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.5f);
        }
    }

    // ---- Isplovljavanje ----

    /// <summary>
    /// Brod krece od sredista otoka prema van, pa isplovi dalje od obale bez
    /// obzira na kojoj je strani nastao.
    ///
    /// <paramref name="queueIndex"/> je mjesto u redu polaska: 0 krece odmah,
    /// svaki sljedeci ceka DEPART_INTERVAL dulje. <paramref name="totalCount"/>
    /// je koliko ih ukupno krece, pa se lepeza rasporedi ravnomjerno preko
    /// cijele sirine bez obzira je li poslano dva broda ili cetrnaest.
    ///
    /// Ranija verzija je otklon povecavala po paru i zaustavljala ga na
    /// najvecoj vrijednosti; brodovi preko te granice dobivali su isti kut i
    /// plovili usporedo, prakticki jedan preko drugoga.
    /// </summary>
    public void BeginSail(int queueIndex = 0, int totalCount = 1)
    {
        if (IsSailing) return;
        IsSailing = true;

        _departDelay = queueIndex * DEPART_INTERVAL;

        Vector3 away = new Vector3(transform.position.x - IslandCenter.x,
                                   transform.position.y - IslandCenter.y, 0f);
        away = away.sqrMagnitude > 0.0001f ? away.normalized : new Vector3(0f, -1f, 0f);

        // Ravnomjerna lepeza preko [-FAN_MAX, +FAN_MAX]; jedan brod ide ravno.
        float angle = totalCount <= 1
            ? 0f
            : -FAN_MAX + 2f * FAN_MAX * (queueIndex / (float)(totalCount - 1));
        angle += Random.Range(-FAN_JITTER, FAN_JITTER);

        _sailDirection = Rotate(away, angle);
    }

    /// <summary>Rotacija vektora oko z osi, u stupnjevima.</summary>
    private static Vector3 Rotate(Vector3 v, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector3(v.x * c - v.y * s, v.x * s + v.y * c, 0f);
    }

    private void Update()
    {
        // Brod se njise i dok stoji u luci, pa animacija ide prije izlaza.
        TickIdleAnimation();

        if (!IsSailing || HasLeft) return;

        // Cekanje na svoj red. Brod je vec "IsSailing", pa vise ne prima teret
        // i ne moze biti poslan dvaput.
        if (_departDelay > 0f)
        {
            _departDelay -= Time.deltaTime;
            return;
        }

        float step = SAIL_SPEED * Time.deltaTime;
        transform.position = transform.position + _sailDirection * step;
        _sailedDistance   += step;

        if (_sailedDistance >= SAIL_DISTANCE) HasLeft = true;
    }

    /// <summary>Restore passengers and food from save.</summary>
    public void RestoreState(int children, int adults, int foodLoaded)
    {
        PassengerChildren = children;
        PassengerAdults   = adults;
        FoodLoaded        = foodLoaded;
    }

    // ---- Cargo ----

    /// <summary>
    /// Ukrca zadani broj djece i odraslih. Pozivatelj je vec provjerio da ih ima
    /// u populaciji i sam ih je oduzeo iz bazena.
    /// </summary>
    public void BoardPassengers(int children, int adults)
    {
        PassengerChildren += children;
        PassengerAdults   += adults;
    }

    /// <summary>
    /// Iskrca do <paramref name="count"/> putnika i vraca koliko je odraslih i
    /// djece sislo. Prvo silaze odrasli — igracu se tako radna snaga vraca odmah,
    /// a djeca (koja ne rade) ostaju na brodu.
    /// </summary>
    public void DisembarkPassengers(int count, out int children, out int adults)
    {
        adults   = Mathf.Min(count, PassengerAdults);
        children = Mathf.Min(count - adults, PassengerChildren);

        PassengerAdults   -= adults;
        PassengerChildren -= children;
    }

    /// <summary>Koliko se hrane jos moze ukrcati, ograniceno zahtjevom broda.</summary>
    public int LoadableFood(int requested) => Mathf.Min(requested, FoodMissing);

    /// <summary>Upise ukrcanu hranu. Pozivatelj ju je vec skinuo sa skladista.</summary>
    public void LoadFood(int amount)
    {
        FoodLoaded = Mathf.Min(RequiredFood, FoodLoaded + amount);
    }

    /// <summary>Skine hranu s broda i vrati koliko je stvarno iskrcano.</summary>
    public int UnloadFood(int amount)
    {
        int unloaded = Mathf.Min(amount, FoodLoaded);
        FoodLoaded -= unloaded;
        return unloaded;
    }

    // ---- Selection ----

    public void SetSelected(bool selected)
    {
        if (_hullRenderer != null)
            _hullRenderer.color = selected ? _selectedColor : _normalColor;
    }

    // ---- Visual ----

    private void BuildVisual(int sortBase)
    {
        const float s = 0.50f;
        _normalColor   = new Color(0.55f, 0.38f, 0.18f);
        _selectedColor = Color.Lerp(_normalColor, Color.white, 0.4f);

        var registry  = SpriteRegistry.Instance;
        var shipArt   = registry != null ? registry.GetShipIdleFrame(0) : null;

        if (shipArt != null)
        {
            // Jedan sprite za cijeli brod. Trup, jarbol i jedro vise nisu odvojeni
            // objekti — art ih nosi sam, pa nema ni slaganja po sortingOrderu.
            _normalColor   = Color.white;                         // sprite se ne boji
            _selectedColor = new Color(1f, 0.92f, 0.65f, 1f);     // topli odsjaj pri odabiru

            var go = new GameObject("Ship");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;

            _hullRenderer              = go.AddComponent<SpriteRenderer>();
            _hullRenderer.sprite       = shipArt;
            _hullRenderer.color        = _normalColor;
            _hullRenderer.sortingOrder = sortBase;

            // Omjer stranica se cuva, sirinu zadaje SpriteRegistry.ShipWidthInTiles.
            float width  = registry.ShipWidthInTiles > 0f ? registry.ShipWidthInTiles : 1f;
            float aspect = shipArt.rect.height > 0f ? shipArt.rect.width / shipArt.rect.height : 1f;
            SpriteFit.Fill(_hullRenderer, new Vector2(width, width / Mathf.Max(0.0001f, aspect)));

            _animated = registry.ShipIdleFrames != null && registry.ShipIdleFrames.Length >= 2;
        }
        else
        {
            // Placeholder od pravokutnika dok art nije postavljen.
            _hullRenderer = SpawnPart("Hull", Vector3.zero,
                new Vector3(1.8f * s, 0.7f * s, 1f), _normalColor, sortBase);

            SpawnPart("Mast", new Vector3(0f, 0.3f * s, 0f),
                new Vector3(0.08f * s, 1.0f * s, 1f), new Color(0.25f, 0.18f, 0.10f), sortBase + 2);

            SpawnPart("Sail", new Vector3(0f, 0.65f * s, 0f),
                new Vector3(0.35f * s, 0.9f * s, 1f), new Color(0.92f, 0.92f, 0.88f), sortBase + 1);
        }

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(transform);
        lbl.transform.localPosition = new Vector3(0f, -0.28f, 0f);
        lbl.transform.localRotation = Quaternion.identity;
        lbl.transform.localScale    = Vector3.one;
        var tmp = lbl.AddComponent<TMPro.TextMeshPro>();
        tmp.text                    = DisplayName;
        tmp.fontSize                = 1.3f;
        tmp.alignment               = TMPro.TextAlignmentOptions.Center;
        tmp.color                   = Color.white;
        tmp.sortingOrder            = sortBase + 3;
        tmp.rectTransform.sizeDelta = new Vector2(2f, 0.6f);
    }

    /// <summary>
    /// Izmjena framea broda. Velicina se cuva jer SpriteFit koristi Sliced nacin,
    /// a zamjena sprajta bi inace vratila renderer na prirodnu velicinu sprajta.
    /// </summary>
    private void TickIdleAnimation()
    {
        if (!_animated || _hullRenderer == null) return;

        var registry = SpriteRegistry.Instance;
        if (registry == null) return;
        if (registry.ShipIdleFrames == null || registry.ShipIdleFrames.Length < 2) return;

        float frameTime = 1f / Mathf.Max(0.1f, registry.ShipFps);
        _frameTimer += Time.deltaTime;

        while (_frameTimer >= frameTime)
        {
            _frameTimer -= frameTime;
            _frame++;
        }

        var next = registry.GetShipIdleFrame(_frame);
        if (next == null || next == _hullRenderer.sprite) return;

        var size = _hullRenderer.size;
        _hullRenderer.sprite = next;
        _hullRenderer.size   = size;
    }

    private SpriteRenderer SpawnPart(string partName, Vector3 localPos, Vector3 scale, Color color, int sortOrder)
    {
        var go = new GameObject(partName);
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = scale;
        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SimpleShapeFactory.CreateFilledSquareSprite(color);
        sr.sortingOrder = sortOrder;
        return sr;
    }
}
