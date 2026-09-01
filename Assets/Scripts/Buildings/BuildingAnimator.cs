using UnityEngine;

/// <summary>
/// Manages the visual state of a building:
///   UnderConstruction — construction sprite + world-space progress bar
///   Idle              — built sprite, no workers inside
///   Producing         — built/producing sprite, workers active
///
/// Attach to any BuildingInstance GameObject.
/// Reads sprites from SpriteRegistry; falls back to SimpleShapeFactory colors if null.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BuildingAnimator : MonoBehaviour
{
    // ---- Config ----
    [SerializeField] private BuildingType _buildingType = BuildingType.Sawmill;
    [SerializeField] private Color        _fallbackConstructionColor = new Color(0.55f, 0.45f, 0.20f);
    [SerializeField] private Color        _fallbackBuiltColor        = Color.white;

    // Postavlja se u Setup() iz ArtWidthFor(). Komponenta se dodaje kroz AddComponent
    // u runtimeu, pa vrijednost upisana u Inspectoru ne bi prezivjela Play — mijenjaj
    // brojeve u tablici ispod.
    private float _artWidth      = 1f;
    private float _artHalfHeight = 0.5f;

    /// <summary>
    /// Vizualna sirina zgrade u poljima. Otisak za validaciju i koliziju ostaje 1x1
    /// (BalanceConfig.BuildingPlacementSize) — ovo je cisto izgled, po uzoru na Frostpunk
    /// gdje zgrada vizualno prelazi preko svog otiska.
    /// Visina se ne postavlja rucno; racuna se iz omjera stranica arta.
    /// </summary>
    private static float ArtWidthFor(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Shipyard:     return 3.0f;   // harbor       144x112
            case BuildingType.TownHall:     return 2.5f;   // house         32x32
            case BuildingType.Sawmill:      return 2.0f;   // sawmill       80x64
            case BuildingType.Steelworks:   return 2.0f;   // steelworks    80x64
            case BuildingType.Fiberworks:   return 2.0f;   // fiberworks    80x48
            case BuildingType.Cookhouse:    return 2.0f;   // cookhouse     64x64
            case BuildingType.ScoutStation: return 1.5f;   // scoutstation  64x96 — visok i uzak
            case BuildingType.HuntersHut:   return 1.5f;   // huntershut    48x32
            default:                        return 2.0f;
        }
    }

    // ---- Progress bar (world space, shown during construction) ----
    private GameObject     _progressBarBg;
    private GameObject     _progressBarFill;
    private SpriteRenderer _progressFillSR;
    private const float    BAR_WIDTH    = 1.2f;
    private const float    BAR_HEIGHT   = 0.12f;
    private const float    BAR_OFFSET_Y = 0.72f;

    // ---- State ----
    private SpriteRenderer      _sr;
    private BuildingVisualState _currentState = BuildingVisualState.UnderConstruction;

    /// <summary>True ako trenutno stanje koristi pravi sprite, a ne placeholder kvadrat.</summary>
    public bool UsesArt { get; private set; }

    /// <summary>True ako za ovaj tip zgrade postoji built sprite — cita BuildingInstance za tintu.</summary>
    public static bool HasArtFor(BuildingType type)
        => SpriteRegistry.Instance != null
        && SpriteRegistry.Instance.GetBuildingSprite(type, BuildingVisualState.Idle) != null;

    // ---- Init ----

    public void Setup(BuildingType type, Color fallbackBuiltColor)
    {
        _buildingType       = type;
        _fallbackBuiltColor = fallbackBuiltColor;
        _artWidth           = ArtWidthFor(type);
        _sr = GetComponent<SpriteRenderer>();

        BuildProgressBar();
        SetState(BuildingVisualState.UnderConstruction);
    }

    /// <summary>
    /// Lebdecu oznaku kaci BuildingFactory tek nakon Initialize(), pa je pri prvom
    /// SetState jos nema. Start() se izvrsi kad postoji.
    /// </summary>
    private void Start() => PlaceOverlays();

    // ---- Public API ----

    /// <summary>Call every frame during construction with progress 0..1.</summary>
    public void SetConstructionProgress(float progress)
    {
        if (_currentState != BuildingVisualState.UnderConstruction)
            SetState(BuildingVisualState.UnderConstruction);

        if (_progressBarBg   != null) _progressBarBg.SetActive(true);
        if (_progressBarFill != null) _progressBarFill.SetActive(true);

        if (_progressFillSR != null)
        {
            float clampedW = Mathf.Clamp01(progress) * BAR_WIDTH;
            _progressBarFill.transform.localScale = new Vector3(clampedW, BAR_HEIGHT, 1f);

            // Color shifts green -> yellow -> orange as progress increases
            _progressFillSR.color = Color.Lerp(
                new Color(0.20f, 0.75f, 0.25f),
                new Color(0.90f, 0.55f, 0.10f),
                progress);
        }
    }

    /// <summary>Call when construction finishes.</summary>
    public void SetBuilt()
    {
        HideProgressBar();
        SetState(BuildingVisualState.Idle);
    }

    /// <summary>Call each frame — pass true when workers are actively producing.</summary>
    public void SetProducing(bool producing)
    {
        if (_currentState == BuildingVisualState.UnderConstruction) return;
        var target = producing ? BuildingVisualState.Producing : BuildingVisualState.Idle;
        if (_currentState != target) SetState(target);
    }

    // ---- Private ----

    private void SetState(BuildingVisualState state)
    {
        _currentState = state;

        var registry = SpriteRegistry.Instance;
        Sprite art   = registry != null
            ? registry.GetBuildingSprite(_buildingType, state)
            : null;

        if (art != null)
        {
            UsesArt    = true;
            _sr.sprite = art;
            _sr.color  = Color.white;              // placeholder tinta bi zaprljala pixel art
            SpriteFit.FitWidth(_sr, _artWidth);    // PPU-neovisno, cuva omjer stranica
            _artHalfHeight = _sr.size.y * 0.5f;
        }
        else
        {
            // Fallback — colored square
            UsesArt        = false;
            _artHalfHeight = 0.5f;
            SpriteFit.Reset(_sr);
            Color fallback = state == BuildingVisualState.UnderConstruction
                ? _fallbackConstructionColor
                : _fallbackBuiltColor;
            _sr.sprite = SimpleShapeFactory.CreateFilledSquareSprite(fallback);
        }

        PlaceOverlays();
    }

    /// <summary>Traka napretka i lebdeca oznaka idu iznad arta, ne iznad polja.</summary>
    private void PlaceOverlays()
    {
        float top = _artHalfHeight + 0.15f;

        if (_progressBarBg != null)
            _progressBarBg.transform.localPosition = new Vector3(0f, top, -0.1f);
        if (_progressBarFill != null)
            _progressBarFill.transform.localPosition = new Vector3(-BAR_WIDTH * 0.5f, top, -0.2f);

        var label = GetComponentInChildren<TMPro.TextMeshPro>();
        if (label != null)
            label.transform.localPosition = new Vector3(0f, top + 0.20f, 0f);
    }

    private void BuildProgressBar()
    {
        // Background (dark bar)
        _progressBarBg = new GameObject("ProgressBar_BG");
        _progressBarBg.transform.SetParent(transform);
        _progressBarBg.transform.localPosition = new Vector3(0f, BAR_OFFSET_Y, -0.1f);
        _progressBarBg.transform.localRotation = Quaternion.identity;
        _progressBarBg.transform.localScale    = new Vector3(BAR_WIDTH, BAR_HEIGHT, 1f);

        var bgSR = _progressBarBg.AddComponent<SpriteRenderer>();
        bgSR.sprite = SimpleShapeFactory.CreateFilledSquareSprite(new Color(0.15f, 0.15f, 0.15f, 0.85f));
        bgSR.sortingOrder = 10;

        // Fill (colored bar — scaled on x axis, pivot at left edge)
        _progressBarFill = new GameObject("ProgressBar_Fill");
        _progressBarFill.transform.SetParent(transform);
        _progressBarFill.transform.localPosition = new Vector3(-BAR_WIDTH * 0.5f, BAR_OFFSET_Y, -0.2f);
        _progressBarFill.transform.localRotation = Quaternion.identity;
        _progressBarFill.transform.localScale    = new Vector3(0f, BAR_HEIGHT, 1f);

        _progressFillSR = _progressBarFill.AddComponent<SpriteRenderer>();
        _progressFillSR.sprite = SimpleShapeFactory.CreateFilledSquareSprite(Color.green);
        _progressFillSR.sortingOrder = 11;

        HideProgressBar();
    }

    private void HideProgressBar()
    {
        if (_progressBarBg   != null) _progressBarBg.SetActive(false);
        if (_progressBarFill != null) _progressBarFill.SetActive(false);
    }
}
