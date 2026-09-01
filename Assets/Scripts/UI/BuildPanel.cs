using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Bottom build panel — opened/closed by the hammer button.
/// Shows available buildings with cost. Selecting one enters
/// ghost placement mode: a transparent preview follows the cursor,
/// green = valid, red = invalid. Left click places, right click/Escape cancels.
///
/// SRP: only handles build panel UI and ghost preview.
///      Actual placement delegated to PrototypeGameController.TryPlaceBuilding().
/// </summary>
public class BuildPanel : MonoBehaviour
{
    // ---- State ----
    private bool              _panelOpen;
    public  bool              IsPlacing => _pendingType.HasValue;
    private BuildingType?     _pendingType;
    private GameObject        _ghost;
    private SpriteRenderer    _ghostSR;
    private bool              _ghostValid;

    // ---- UI refs ----
    private GameObject        _panelRoot;
    private Button            _toggleBtn;
    private TextMeshProUGUI   _toggleBtnLabel;
    private readonly List<Button> _buildBtns = new();

    // ---- Config ----
    private static readonly Color GhostValid   = new Color(0.3f, 1.0f, 0.3f, 0.55f);
    private static readonly Color GhostInvalid = new Color(1.0f, 0.2f, 0.2f, 0.55f);
    private const float GhostSize = BalanceConfig.BuildingPlacementSize;

    // Build panel — visina panela mora primiti ikonu + tri retka teksta
    private const float PanelHeight = 126f;
    private const float ButtonWidth =  96f;
    private const float IconHeight  =  44f;

    private GameController _game;
    private Camera                  _cam;
    private Camera Cam => _cam != null ? _cam : (_cam = Camera.main);

    // -------------------------------------------------------
    // Init
    // -------------------------------------------------------

    public void Build(GameController game, Transform canvasTransform, Camera cam)
    {
        _game = game;
        _cam  = cam;

        BuildToggleButton(canvasTransform);
        BuildPanelRoot(canvasTransform);
        _panelRoot.SetActive(false);
    }

    // -------------------------------------------------------
    // Update — ghost follows mouse, validates each frame
    // -------------------------------------------------------

    private void Update()
    {
        if (_pendingType == null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        // Cancel on right click or Escape
        if (mouse.rightButton.wasPressedThisFrame ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            CancelPlacement();
            return;
        }

        // Update ghost position
        if (Cam == null) return;
        Vector3 rawPos = Cam.ScreenToWorldPoint(mouse.position.ReadValue());
        rawPos.z = 0f;

        // Snap to tile centre so buildings always land inside one tile
        Vector3 worldPos = SnapToTile(rawPos);

        if (_ghost != null) _ghost.transform.position = worldPos;

        // Validate using explicit BuildingType (Steelworks needs proximity check)
        var  validator  = PlacementValidator.Instance;
        _ghostValid = validator != null
            ? validator.IsValidForType(worldPos, new Vector2(GhostSize, GhostSize), _pendingType.Value)
            : IslandBounds.IsValidPlacement(worldPos, new Vector2(GhostSize, GhostSize),
                _pendingType == BuildingType.Shipyard);

        if (_ghostSR != null)
            _ghostSR.color = _ghostValid ? GhostValid : GhostInvalid;

        // Place on left click
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (_ghostValid)
            {
                bool placed = _game.TryPlaceBuilding(_pendingType.Value, worldPos);
                if (placed) CancelPlacement();
            }
        }
    }

    // -------------------------------------------------------
    // Public
    // -------------------------------------------------------

    public void Refresh(GameController game)
    {
        RefreshButtonStates(game);
    }

    // -------------------------------------------------------
    // Toggle panel
    // -------------------------------------------------------

    private void TogglePanel()
    {
        _panelOpen = !_panelOpen;
        _panelRoot.SetActive(_panelOpen);
        if (!_panelOpen) CancelPlacement();
        if (_toggleBtnLabel != null)
            _toggleBtnLabel.text = _panelOpen ? "[X]" : "[B]";
    }

    // -------------------------------------------------------
    // Placement
    // -------------------------------------------------------

    private void StartPlacement(BuildingType type)
    {
        _pendingType = type;
        CreateGhost(type);
    }

    private void CancelPlacement()
    {
        _pendingType = null;
        if (_ghost != null) { Destroy(_ghost); _ghost = null; }
        _ghostSR = null;
    }

    /// <summary>Snap world position to nearest tile centre.</summary>
    private static Vector3 SnapToTile(Vector3 world)
    {
        var renderer = IslandTilemapRenderer.Instance;
        if (renderer == null || renderer.Map == null)
            return world;

        var map  = renderer.Map;
        var tile = map.WorldToTile(world);
        var snapped = map.TileToWorld(tile.x, tile.y);
        return new Vector3(snapped.x, snapped.y, world.z);
    }

    private void CreateGhost(BuildingType type)
    {
        if (_ghost != null) Destroy(_ghost);

        _ghost = new GameObject("Ghost");
        _ghost.transform.localScale = new Vector3(GhostSize, GhostSize, 1f);

        _ghostSR = _ghost.AddComponent<SpriteRenderer>();

        // Always use a fresh colored square — no sprite registry dependency
        _ghostSR.sprite           = SimpleShapeFactory.CreateFilledSquareSprite(Color.white);
        _ghostSR.color            = GhostInvalid;
        _ghostSR.sortingLayerName = "Default";
        _ghostSR.sortingOrder     = 100;   // well above all tilemap tiles
    }

    // -------------------------------------------------------
    // UI builders
    // -------------------------------------------------------

    private void BuildToggleButton(Transform canvas)
    {
        var go = new GameObject("BuildToggleBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvas, false);

        // Bottom right corner — matches wireframe BUILD MENU circle
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-10f, 10f);
        rt.sizeDelta        = new Vector2(75f, 75f);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        _toggleBtn = go.GetComponent<Button>();
        var bc = _toggleBtn.colors;
        bc.highlightedColor = new Color(0.28f, 0.28f, 0.28f);
        bc.pressedColor     = new Color(0.08f, 0.08f, 0.08f);
        _toggleBtn.colors   = bc;
        _toggleBtn.onClick.AddListener(TogglePanel);

        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(go.transform, false);
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        _toggleBtnLabel = lblGO.AddComponent<TextMeshProUGUI>();
        _toggleBtnLabel.text      = "[B]";
        _toggleBtnLabel.fontSize  = 18f;
        _toggleBtnLabel.alignment = TextAlignmentOptions.Center;
        _toggleBtnLabel.color     = new Color(0.9f, 0.75f, 0.2f);
    }

    private void BuildPanelRoot(Transform canvas)
    {
        _panelRoot = new GameObject("BuildPanel", typeof(RectTransform), typeof(Image));
        _panelRoot.transform.SetParent(canvas, false);

        var rt = _panelRoot.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 62f);
        rt.sizeDelta        = new Vector2(0f, PanelHeight);

        _panelRoot.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.06f, 0.93f);

        var hg = _panelRoot.AddComponent<HorizontalLayoutGroup>();
        hg.padding               = new RectOffset(12, 12, 8, 8);
        hg.spacing               = 8f;
        hg.childAlignment        = TextAnchor.MiddleLeft;
        hg.childControlWidth     = false;
        hg.childControlHeight    = true;
        hg.childForceExpandWidth = false;
        hg.childForceExpandHeight= true;

        // One button per buildable type
        var types = new[]
        {
            BuildingType.Sawmill,
            BuildingType.Steelworks,
            BuildingType.Fiberworks,
            BuildingType.Cookhouse,
            BuildingType.Shipyard,
            BuildingType.HuntersHut,
            BuildingType.ScoutStation,
        };

        foreach (var type in types)
        {
            var btn = CreateBuildButton(type);
            _buildBtns.Add(btn);
        }
    }

    private Button CreateBuildButton(BuildingType type)
    {
        var cost = BuildingCost.For(type);

        var go = new GameObject($"Btn_{type}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(_panelRoot.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(ButtonWidth, 0f);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);

        var btn = go.GetComponent<Button>();
        var bc  = btn.colors;
        bc.highlightedColor = new Color(0.28f, 0.28f, 0.28f);
        bc.pressedColor     = new Color(0.08f, 0.08f, 0.08f);
        bc.disabledColor    = new Color(0.12f, 0.12f, 0.12f, 0.6f);
        btn.colors = bc;

        var t  = type; // capture for closure
        btn.onClick.AddListener(() =>
        {
            if (!btn.interactable) return;
            StartPlacement(t);
        });

        // Inner layout — name on top, cost on bottom
        var vg = go.AddComponent<VerticalLayoutGroup>();
        vg.padding               = new RectOffset(6, 6, 6, 6);
        vg.spacing               = 2f;
        vg.childControlWidth     = true;
        vg.childControlHeight    = false;
        vg.childForceExpandWidth = true;
        vg.childForceExpandHeight= false;

        // Icon — sprite zgrade iz SpriteRegistryja, fallback na obojani kvadrat
        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(go.transform, false);
        var iconLE = iconGO.AddComponent<LayoutElement>();
        iconLE.preferredHeight = IconHeight;
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite        = BuildingIcon(type, out bool isArt);
        iconImg.color         = isArt ? Color.white : BuildingFactory.Meta(type).color;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;   // klik mora proci do gumba

        // Name
        var nameGO = new GameObject("Name", typeof(RectTransform));
        nameGO.transform.SetParent(go.transform, false);
        var nameLE = nameGO.AddComponent<LayoutElement>();
        nameLE.preferredHeight = 18f;
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text      = BuildingDisplayName(type);
        nameTMP.fontSize  = 11f;
        nameTMP.color     = Color.white;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        // Cost
        var costGO = new GameObject("Cost", typeof(RectTransform));
        costGO.transform.SetParent(go.transform, false);
        var costLE = costGO.AddComponent<LayoutElement>();
        costLE.preferredHeight = 14f;
        var costTMP = costGO.AddComponent<TextMeshProUGUI>();
        costTMP.text      = CostString(cost);
        costTMP.fontSize  = 9f;
        costTMP.color     = new Color(0.7f, 0.85f, 0.7f);
        costTMP.alignment = TextAlignmentOptions.Center;
        costTMP.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        // Hours
        var hoursGO = new GameObject("Hours", typeof(RectTransform));
        hoursGO.transform.SetParent(go.transform, false);
        var hoursLE = hoursGO.AddComponent<LayoutElement>();
        hoursLE.preferredHeight = 12f;
        var hoursTMP = hoursGO.AddComponent<TextMeshProUGUI>();
        hoursTMP.text      = $"{cost.Hours}h";
        hoursTMP.fontSize  = 9f;
        hoursTMP.color     = new Color(0.6f, 0.6f, 0.6f);
        hoursTMP.alignment = TextAlignmentOptions.Center;
        hoursTMP.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        return btn;
    }

    private void RefreshButtonStates(GameController game)
    {
        var types = new[]
        {
            BuildingType.Sawmill, BuildingType.Steelworks,
            BuildingType.Fiberworks, BuildingType.Cookhouse, BuildingType.Shipyard,
            BuildingType.HuntersHut, BuildingType.ScoutStation,
        };

        for (int i = 0; i < _buildBtns.Count && i < types.Length; i++)
        {
            var cost = BuildingCost.For(types[i]);
            _buildBtns[i].interactable = cost.CanAfford(game.Wood, game.Steel, game.Cloth, game.Rope);
        }
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

    private static string BuildingDisplayName(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Fiberworks: return "Fiberworks";
            default:                      return type.ToString();
        }
    }

    private static string CostString(BuildingCost cost)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (cost.Wood  > 0) parts.Add($"W:{cost.Wood}");
        if (cost.Steel > 0) parts.Add($"S:{cost.Steel}");
        if (cost.Cloth > 0) parts.Add($"C:{cost.Cloth}");
        if (cost.Rope  > 0) parts.Add($"R:{cost.Rope}");
        return parts.Count > 0 ? string.Join("  ", parts) : "Free";
    }
}
