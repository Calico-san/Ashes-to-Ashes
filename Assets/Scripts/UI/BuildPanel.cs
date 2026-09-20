using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Bottom build panel — opened/closed by the build button.
/// Shows available buildings with cost. Selecting one enters ghost placement mode:
/// a transparent preview follows the cursor, green = valid, red = invalid.
/// Left click places, right click/Escape cancels.
///
/// SUCELJE ZIVI U SCENI. Ova skripta ne gradi nijedan GameObject osim duha
/// (ghost) koji je dio svijeta, a ne UI-ja. Panel, gumb za otvaranje i gumbi
/// zgrada povlace se u Inspectoru — vidi README za tocnu hijerarhiju.
///
/// Natpise i ikone svejedno popunjava kod, iz BalanceConfiga i BuildingFactory.Meta:
/// brojevi upisani rukom u sceni zastare pri prvoj promjeni balansa.
///
/// SRP: samo UI panela i duh za postavljanje.
///      Samo postavljanje delegira GameController.TryPlaceBuilding().
/// </summary>
public class BuildPanel : MonoBehaviour
{
    /// <summary>Gradivi tipovi — jedan izvor istine za redoslijed u panelu.</summary>
    public static readonly BuildingType[] BuildableTypes =
    {
        BuildingType.Sawmill,
        BuildingType.Steelworks,
        BuildingType.Fiberworks,
        BuildingType.Cookhouse,
        BuildingType.Shipyard,
        BuildingType.HuntersHut,
        BuildingType.ScoutStation,
    };

    /// <summary>
    /// Jedan gumb zgrade iz scene. Obavezni su samo Type i Button; oznake i ikona
    /// su neobavezne i prazno polje se preskace.
    /// </summary>
    [System.Serializable]
    public class BuildButtonRef
    {
        public BuildingType    Type;
        public Button          Button;
        public Image           Icon;
        public TextMeshProUGUI NameLabel;
        public TextMeshProUGUI CostLabel;
    }

    // ---- Scene refs ----
    [Header("Panel")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Button     _toggleBtn;

    [Header("Gumbi zgrada")]
    [SerializeField] private BuildButtonRef[] _sceneBuildButtons;

    // ---- State ----
    private bool           _panelOpen;

    /// <summary>
    /// True i jedan frame NAKON postavljanja. PrototypeSelectionController cita
    /// ovu zastavicu da ne bi odabrao upravo postavljeno gradiliste: njegov
    /// Update se izvrsava poslije ovoga, pa je isti klik inace otvarao desnu
    /// plocu cim bi zgrada bila postavljena.
    /// </summary>
    public  bool           IsPlacing => _pendingType.HasValue || Time.frameCount == _placedFrame;
    private int            _placedFrame = -1;
    private BuildingType?  _pendingType;
    private GameObject     _ghost;
    private SpriteRenderer _ghostSR;
    private bool           _ghostValid;
    private bool           _ready;

    private readonly List<(BuildingType type, Button btn)> _buttons = new();

    // ---- Config ----
    private static readonly Color GhostValid   = new Color(0.3f, 1.0f, 0.3f, 0.55f);
    private static readonly Color GhostInvalid = new Color(1.0f, 0.2f, 0.2f, 0.55f);
    // Duh poprima otisak gradevine koja se postavlja; postavlja se u CreateGhost.
    private float _ghostSize = BalanceConfig.BuildingPlacementSize;

    private GameController _game;
    private Camera         _cam;
    private Camera Cam => _cam != null ? _cam : (_cam = Camera.main);

    // -------------------------------------------------------
    // Init
    // -------------------------------------------------------

    /// <summary>
    /// canvasTransform se vise ne koristi — zadrzan je u potpisu da pozivatelji
    /// (UIController, Bootstrapper) ostanu nepromijenjeni.
    /// </summary>
    public void Build(GameController game, Transform canvasTransform, Camera cam)
    {
        _game = game;
        _cam  = cam;

        if (_panelRoot == null)
        {
            Debug.LogError("[BuildPanel] Panel Root nije povucen u Inspectoru. " +
                           "Panel se vise ne gradi iz koda — vidi README za hijerarhiju u Canvasu.", this);
            return;
        }

        WireToggle();
        WireBuildButtons();

        _ready     = true;
        _panelOpen = false;
        _panelRoot.SetActive(false);
    }

    private void WireToggle()
    {
        if (_toggleBtn == null)
        {
            Debug.LogWarning("[BuildPanel] Toggle Btn nije povucen — panel se nece moci otvoriti.", this);
            return;
        }

        _toggleBtn.onClick.RemoveAllListeners();
        _toggleBtn.onClick.AddListener(TogglePanel);
    }

    private void WireBuildButtons()
    {
        _buttons.Clear();

        if (_sceneBuildButtons == null || _sceneBuildButtons.Length == 0)
        {
            Debug.LogWarning("[BuildPanel] Scene Build Buttons je prazan — u panelu nece biti nijedne zgrade.", this);
            return;
        }

        foreach (var entry in _sceneBuildButtons)
        {
            if (entry == null || entry.Button == null) continue;

            var cost = BuildingCost.For(entry.Type);

            // Natpisi i ikona dolaze iz koda, ne iz scene — inace zastare pri
            // prvoj promjeni cijene ili naziva.
            if (entry.NameLabel  != null) entry.NameLabel.text  = BuildingFactory.Meta(entry.Type).name;
            if (entry.CostLabel  != null) entry.CostLabel.text  = CostString(cost);

            if (entry.Icon != null)
            {
                entry.Icon.sprite         = BuildingIcon(entry.Type, out bool isArt);
                entry.Icon.color          = isArt ? Color.white : BuildingFactory.Meta(entry.Type).color;
                entry.Icon.preserveAspect = true;
                entry.Icon.raycastTarget  = false;   // klik mora proci do gumba
            }

            var type = entry.Type;   // capture za closure
            var btn  = entry.Button;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (!btn.interactable) return;
                StartPlacement(type);
            });

            _buttons.Add((type, btn));
        }
    }

    // -------------------------------------------------------
    // Update — ghost follows mouse, validates each frame
    // -------------------------------------------------------

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Kao desni panel: klik na svijet ili drugi dio UI-ja zatvara otvoreni
        // build menu. Dok traje placement, klik na svijet pripada postavljanju
        // zgrade pa panel ne zatvaramo prije nego sto se taj klik obradi.
        if (_panelOpen && _pendingType == null && mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 pointer = mouse.position.ReadValue();
            var panelRect  = _panelRoot != null ? _panelRoot.transform as RectTransform : null;
            var toggleRect = _toggleBtn != null ? _toggleBtn.transform as RectTransform : null;

            if (!ContainsScreenPoint(panelRect, pointer) &&
                !ContainsScreenPoint(toggleRect, pointer))
            {
                ClosePanel();
            }
        }

        if (_pendingType == null) return;

        // Cancel on right click or Escape
        if (mouse.rightButton.wasPressedThisFrame ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            CancelPlacement();
            return;
        }

        if (Cam == null) return;
        Vector3 rawPos = Cam.ScreenToWorldPoint(mouse.position.ReadValue());
        rawPos.z = 0f;

        // Neparan otisak se lijepi na srediste polja, paran na kriziste mreze.
        Vector3 worldPos = SnapToGrid(rawPos, _pendingType.Value);

        if (_ghost != null) _ghost.transform.position = worldPos;

        // Validate using explicit BuildingType (Steelworks needs proximity check)
        var validator = PlacementValidator.Instance;
        var ghostFootprint = new Vector2(_ghostSize, _ghostSize);
        _ghostValid = validator != null
            ? validator.IsValidForType(worldPos, ghostFootprint, _pendingType.Value)
            : IslandBounds.IsValidPlacement(worldPos, ghostFootprint,
                _pendingType == BuildingType.Shipyard);

        if (_ghostSR != null)
            _ghostSR.color = _ghostValid ? GhostValid : GhostInvalid;

        if (mouse.leftButton.wasPressedThisFrame && _ghostValid)
        {
            if (_game.TryPlaceBuilding(_pendingType.Value, worldPos))
            {
                _placedFrame = Time.frameCount;
                ClosePanel();
            }
        }
    }

    // -------------------------------------------------------
    // Public
    // -------------------------------------------------------

    public void Refresh(GameController game)
    {
        if (!_ready) return;

        foreach (var (type, btn) in _buttons)
        {
            if (btn == null) continue;
            var cost = BuildingCost.For(type);
            btn.interactable = cost.CanAfford(game.Wood, game.Steel, game.Cloth, game.Rope);
        }
    }

    // -------------------------------------------------------
    // Toggle panel
    // -------------------------------------------------------

    private void TogglePanel()
    {
        if (_panelRoot == null) return;

        if (_panelOpen)
        {
            ClosePanel();
            return;
        }

        _panelOpen = true;
        _panelRoot.SetActive(true);
    }

    private void ClosePanel()
    {
        if (_panelRoot == null) return;

        _panelOpen = false;
        _panelRoot.SetActive(false);
        CancelPlacement();
    }

    private static bool ContainsScreenPoint(RectTransform rect, Vector2 screenPoint)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy) return false;

        var canvas = rect.GetComponentInParent<Canvas>();
        var uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, uiCamera);
    }

    // -------------------------------------------------------
    // Placement
    // -------------------------------------------------------

    private void StartPlacement(BuildingType type)
    {
        _pendingType = type;
        CreateGhost();
    }

    private void CancelPlacement()
    {
        _pendingType = null;
        if (_ghost != null) { Destroy(_ghost); _ghost = null; }
        _ghostSR = null;
    }

    /// <summary>
    /// Lijepi poziciju na mrezu karte. Neparan otisak (1x1) ide na SREDISTE
    /// polja; paran (2x2) na KRIZISTE, jer mu srediste pada izmedu polja.
    ///
    /// Bez te razlike bi brodogradiliste 2x2 centrirano na srediste polja
    /// visilo pola polja preko ruba i pokrivalo devet polja umjesto cetiri.
    /// </summary>
    private static Vector3 SnapToGrid(Vector3 world, BuildingType type)
    {
        var renderer = IslandTilemapRenderer.Instance;
        if (renderer == null || renderer.Map == null) return world;

        var map  = renderer.Map;
        var tile = map.WorldToTile(world);

        if (!BuildingFootprint.IsEven(type))
        {
            var centre = map.TileToWorld(tile.x, tile.y);
            return new Vector3(centre.x, centre.y, world.z);
        }

        // Kriziste je donji lijevi kut polja pod kursorom, pomaknut za pola
        // polja od sredista tog polja.
        var c = map.TileToWorld(tile.x, tile.y);
        return new Vector3(c.x - map.TileSize * 0.5f,
                           c.y - map.TileSize * 0.5f,
                           world.z);
    }

    /// <summary>Duh je objekt u svijetu, ne UI — zato se i dalje stvara iz koda.</summary>
    private void CreateGhost()
    {
        if (_ghost != null) Destroy(_ghost);

        _ghostSize = _pendingType.HasValue
            ? BuildingFootprint.SizeFor(_pendingType.Value)
            : BalanceConfig.BuildingPlacementSize;

        _ghost = new GameObject("Ghost");
        _ghost.transform.localScale = new Vector3(_ghostSize, _ghostSize, 1f);

        _ghostSR                  = _ghost.AddComponent<SpriteRenderer>();
        _ghostSR.sprite           = SimpleShapeFactory.CreateFilledSquareSprite(Color.white);
        _ghostSR.color            = GhostInvalid;
        _ghostSR.sortingLayerName = "Default";
        _ghostSR.sortingOrder     = 100;   // well above all tilemap tiles
    }

    // ---- Helpers ----

    /// <summary>
    /// Ikona za gumb. Vraca built sprite iz SpriteRegistryja; ako ga nema,
    /// vraca bijeli kvadrat koji pozivatelj oboji bojom tipa zgrade.
    /// </summary>
    private static Sprite BuildingIcon(BuildingType type, out bool isArt)
    {
        var registry = SpriteRegistry.Instance;
        var sprite   = registry != null
            ? registry.GetBuildingSprite(type, BuildingVisualState.Idle)
            : null;

        isArt = sprite != null;
        return isArt ? sprite : SimpleShapeFactory.CreateFilledSquareSprite(Color.white);
    }

    private static string CostString(BuildingCost cost)
    {
        var parts = new List<string>();
        if (cost.Wood  > 0) parts.Add(ResourceIcons.Line(ResourceType.Wood,  cost.Wood));
        if (cost.Steel > 0) parts.Add(ResourceIcons.Line(ResourceType.Steel, cost.Steel));
        if (cost.Cloth > 0) parts.Add(ResourceIcons.Line(ResourceType.Cloth, cost.Cloth));
        if (cost.Rope  > 0) parts.Add(ResourceIcons.Line(ResourceType.Rope,  cost.Rope));
        return parts.Count > 0 ? string.Join("  ", parts) : "Free";
    }
}
