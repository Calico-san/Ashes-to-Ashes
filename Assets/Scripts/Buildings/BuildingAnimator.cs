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

    // Postavlja se u Setup(). Komponenta se dodaje kroz AddComponent u runtimeu,
    // pa vrijednost upisana u Inspectoru ne bi prezivjela Play.
    private float _artWidth      = BalanceConfig.BuildingPlacementSize;
    // Otisak vlasnika u poljima; postavlja ga Setup() iz tipa zgrade.
    private float _footprint     = BalanceConfig.BuildingPlacementSize;
    private float _artHalfHeight = 0.5f;

    // ---- Progress bar (world space, shown during construction) ----
    private GameObject     _progressBarBg;
    private GameObject     _progressBarFill;
    private SpriteRenderer _progressFillSR;
    // Sirina je 1.0 = tocno otisak zgrade; 1.2 je prelazilo u susjedno polje.
    private const float    BAR_WIDTH    = 1.0f;
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
        _footprint          = BuildingFootprint.SizeFor(type);
        _artWidth           = BalanceConfig.BuildingPlacementSize;
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
            // Ispuna ima pivot (0, 0.5), pa skaliranje po x raste iskljucivo udesno
            // od lijevog ruba pozadine. Pozicija je fiksna — ne pomice se s napretkom.
            float clamped = Mathf.Clamp01(progress);
            _progressBarFill.transform.localScale = new Vector3(clamped * BAR_WIDTH, BAR_HEIGHT, 1f);

            // Color shifts green -> yellow -> orange as progress increases
            _progressFillSR.color = Color.Lerp(
                new Color(0.20f, 0.75f, 0.25f),
                new Color(0.90f, 0.55f, 0.10f),
                clamped);
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
            SpriteFit.FitInside(_sr, _artWidth);   // cijela zgrada stane u svoj 1x1 otisak
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

        // Dodjela sprite-a uz Sliced draw mode natjera Unity da upise prirodnu velicinu
        // sprite-a (pikseli / PPU) u transform.localScale — za construction_building
        // 48x64 @ PPU 16 to je (3, 4), sto je mnozilo vec ispravno skaliran sr.size i
        // cinilo zgradu ~3x3 polja. Otisak je uvijek 1x1, pa ga vracamo odmah ovdje.
        // Mora biti NAKON PlaceOverlays jer i ono racuna polozaje u lokalnom prostoru.
        float f = _footprint;
        transform.localScale = new Vector3(f, f, 1f);
    }

    /// <summary>Traka napretka ide iznad arta, ne iznad polja.</summary>
    private void PlaceOverlays()
    {
        float top = _artHalfHeight + 0.15f;

        // Pozadina je centrirana (pivot 0.5), ispuna je usidrena na njezin lijevi rub
        // (pivot 0) — zato razliciti x. Ovdje se postavlja samo pozicija; sirinu
        // ispune mijenja iskljucivo SetConstructionProgress.
        if (_progressBarBg != null)
            _progressBarBg.transform.localPosition = new Vector3(0f, top, -0.1f);
        if (_progressBarFill != null)
            _progressBarFill.transform.localPosition = new Vector3(-BAR_WIDTH * 0.5f, top, -0.2f);
    }

    private void BuildProgressBar()
    {
        // Idempotentno: Setup() se na BuildSlotu zove dvaput (placeholder tip u
        // Initialize, pa stvarni tip u StartConstruction/RestoreConstruction).
        // Bez ove provjere svaki poziv stvarao je novi par GameObjecta.
        if (_progressBarBg != null && _progressBarFill != null) return;

        // Background (dark bar) — pivot u sredini
        _progressBarBg = new GameObject("ProgressBar_BG");
        _progressBarBg.transform.SetParent(transform);
        _progressBarBg.transform.localPosition = new Vector3(0f, BAR_OFFSET_Y, -0.1f);
        _progressBarBg.transform.localRotation = Quaternion.identity;
        _progressBarBg.transform.localScale    = new Vector3(BAR_WIDTH, BAR_HEIGHT, 1f);

        var bgSR = _progressBarBg.AddComponent<SpriteRenderer>();
        bgSR.sprite = SimpleShapeFactory.CreateFilledSquareSprite(new Color(0.15f, 0.15f, 0.15f, 0.85f));
        bgSR.sortingOrder = 10;

        // Fill — pivot na LIJEVOM rubu (0, 0.5) da skaliranje po x raste samo udesno
        _progressBarFill = new GameObject("ProgressBar_Fill");
        _progressBarFill.transform.SetParent(transform);
        _progressBarFill.transform.localPosition = new Vector3(-BAR_WIDTH * 0.5f, BAR_OFFSET_Y, -0.2f);
        _progressBarFill.transform.localRotation = Quaternion.identity;
        _progressBarFill.transform.localScale    = new Vector3(0f, BAR_HEIGHT, 1f);

        _progressFillSR = _progressBarFill.AddComponent<SpriteRenderer>();
        _progressFillSR.sprite = SimpleShapeFactory.CreateFilledSquareSprite(
            Color.green, new Vector2(0f, 0.5f));
        _progressFillSR.sortingOrder = 11;

        HideProgressBar();
    }

    private void HideProgressBar()
    {
        if (_progressBarBg   != null) _progressBarBg.SetActive(false);
        if (_progressBarFill != null) _progressBarFill.SetActive(false);
    }
}
