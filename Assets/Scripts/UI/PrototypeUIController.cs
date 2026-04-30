using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and refreshes all HUD elements.
/// Three zones: top bar (resources + clock), bottom bar (population),
/// right panel (selected building or ship).
/// </summary>
public class PrototypeUIController : MonoBehaviour
{
    // Top bar
    private TextMeshProUGUI _resourcesText;
    private TextMeshProUGUI _clockText;
    private Button          _pauseBtn, _speed1Btn, _speed2Btn, _speed3Btn;

    // Bottom bar
    private TextMeshProUGUI _populationText;

    // Right panel — shared elements
    private GameObject      _rightPanel;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _workerText;
    private TextMeshProUGUI _outputText;
    private Button          _assignBtn;
    private Button          _removeBtn;

    // Shipyard ship list
    private GameObject           _shipListContainer;
    private readonly List<Button> _shipRowBtns = new();

    // Build slot panel
    private GameObject      _buildSlotContainer;
    private TMPro.TextMeshProUGUI _buildSlotTitle;
    private readonly List<Button> _buildOptionBtns = new();

    // Ship detail submenu (shown inside right panel when a ship row is clicked)
    private GameObject      _shipDetailContainer;
    private TextMeshProUGUI _shipDetailTitle;
    private TextMeshProUGUI _shipDetailInfo;
    private Button          _shipDetailBack;
    private Button          _shipDetailAssign;
    private Button          _shipDetailRemove;

    private PrototypeGameController _game;
    private BuildPanel              _buildPanel;

    // -------------------------------------------------------
    // Build
    // -------------------------------------------------------

    public void Build(PrototypeGameController game)
    {
        _game = game;

        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        BuildTopBar(canvas.transform);
        BuildBottomBar(canvas.transform);
        BuildRightPanel(canvas.transform);

        // Build panel — needs camera reference
        _buildPanel = canvasGO.AddComponent<BuildPanel>();
        var cam = UnityEngine.Camera.main;
        _buildPanel.Build(game, canvas.transform, cam);
    }

    // -------------------------------------------------------
    // Refresh — called every frame by GameController
    // -------------------------------------------------------

    public void Refresh(PrototypeGameController game, BuildingInstance selBuilding, ShipInstance selShip = null, BuildSlot selSlot = null)
    {
        RefreshTopBar(game);
        RefreshBottomBar(game);
        RefreshRightPanel(game, selBuilding, selShip, selSlot);
        RefreshSpeedButtons(game.SpeedMultiplier);
        _buildPanel?.Refresh(game);
    }

    // -------------------------------------------------------
    // Top bar
    // -------------------------------------------------------

    private void BuildTopBar(Transform parent)
    {
        var bar = MakePanel(parent, "TopBar", new Vector2(0,1), new Vector2(1,1), Vector2.zero, new Vector2(0,26));
        var hg  = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hg.padding = new RectOffset(10, 10, 4, 4);
        hg.spacing = 6;
        hg.childAlignment         = TextAnchor.MiddleLeft;
        hg.childControlWidth      = true;
        hg.childControlHeight     = true;
        hg.childForceExpandWidth  = false;
        hg.childForceExpandHeight = true;

        _resourcesText = MakeText(bar, "Resources", 10, TextAlignmentOptions.MidlineLeft);
        _resourcesText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        LE(_resourcesText.gameObject, flexW: 1);

        Spacer(bar);

        _clockText = MakeText(bar, "Clock", 10, TextAlignmentOptions.MidlineRight);
        _clockText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        LE(_clockText.gameObject, minW: 90);

        _pauseBtn  = MakeBtn(bar, "||", () => _game.SetSpeed(0), 28, 18);
        _speed1Btn = MakeBtn(bar, "1x", () => _game.SetSpeed(1), 28, 18);
        _speed2Btn = MakeBtn(bar, "2x", () => _game.SetSpeed(2), 28, 18);
        _speed3Btn = MakeBtn(bar, "3x", () => _game.SetSpeed(3), 28, 18);
    }

    private void RefreshTopBar(PrototypeGameController game)
    {
        if (_resourcesText != null)
            _resourcesText.text =
                $"Wood: {game.Wood}   Steel: {game.Steel}   Cloth: {game.Cloth}   Rope: {game.Rope}   Food: {game.Food}   Ships: {game.Ships}";
        if (_clockText != null)
            _clockText.text = $"Day {game.Day}  {game.Hour:00}:{game.Minute:00}";
    }

    // -------------------------------------------------------
    // Bottom bar
    // -------------------------------------------------------

    private void BuildBottomBar(Transform parent)
    {
        var bar = MakePanel(parent, "BottomBar", new Vector2(0,0), new Vector2(1,0), Vector2.zero, new Vector2(0,22));
        var hg  = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hg.padding = new RectOffset(10, 10, 4, 4);
        hg.childAlignment         = TextAnchor.MiddleLeft;
        hg.childControlWidth      = true;
        hg.childControlHeight     = true;
        hg.childForceExpandWidth  = false;
        hg.childForceExpandHeight = true;

        _populationText = MakeText(bar, "Population", 10, TextAlignmentOptions.MidlineLeft);
        _populationText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        LE(_populationText.gameObject, flexW: 1);
    }

    private void RefreshBottomBar(PrototypeGameController game)
    {
        if (_populationText == null) return;
        float foodPerDay = EconomyCalculator.FoodConsumptionPerDay(game.AdultPopulation, game.Children);
        _populationText.text =
            $"Population: {game.TotalPopulation}   Children: {game.Children}   " +
            $"Free workers: {game.FreeWorkers}   Food/day: -{foodPerDay:F0}";
    }

    // -------------------------------------------------------
    // Right panel
    // -------------------------------------------------------

    private void BuildRightPanel(Transform parent)
    {
        _rightPanel = new GameObject("RightPanel", typeof(RectTransform));
        _rightPanel.transform.SetParent(parent, false);
        var rt = _rightPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(1, 1);
        rt.offsetMin = new Vector2(-190, 22);
        rt.offsetMax = new Vector2(0, -26);

        _rightPanel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.90f);

        var vg = _rightPanel.AddComponent<VerticalLayoutGroup>();
        vg.padding               = new RectOffset(12, 12, 12, 12);
        vg.spacing               = 6;
        vg.childAlignment        = TextAnchor.UpperLeft;
        vg.childControlWidth     = true;
        vg.childControlHeight    = true;
        vg.childForceExpandWidth = true;
        vg.childForceExpandHeight= false;

        _titleText = MakeText(_rightPanel.transform, "Title", 12, TextAlignmentOptions.TopLeft);
        _titleText.fontStyle = FontStyles.Bold;
        LE(_titleText.gameObject, prefH: 18, minH: 18);

        Separator(_rightPanel.transform);

        _workerText = MakeText(_rightPanel.transform, "Workers", 10, TextAlignmentOptions.TopLeft);
        _workerText.color = new Color(0.85f, 0.85f, 0.85f);
        LE(_workerText.gameObject, prefH: 14, minH: 14);

        _outputText = MakeText(_rightPanel.transform, "Output", 10, TextAlignmentOptions.TopLeft);
        _outputText.color = new Color(0.65f, 0.82f, 0.65f);
        _outputText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        LE(_outputText.gameObject, prefH: 52, minH: 52);

        Separator(_rightPanel.transform);

        _assignBtn = MakeBtn(_rightPanel.transform, "+ Add worker", () => _game.AssignWorkerToSelectedBuilding(), 166, 26);
        LE(_assignBtn.gameObject, prefH: 26, minH: 26);

        Gap(_rightPanel.transform, 4);

        _removeBtn = MakeBtn(_rightPanel.transform, "- Remove worker", () => _game.RemoveWorkerFromSelectedBuilding(), 166, 26);
        LE(_removeBtn.gameObject, prefH: 26, minH: 26);

        Separator(_rightPanel.transform);

        // Ship list (Shipyard only)
        _shipListContainer = new GameObject("ShipList", typeof(RectTransform));
        _shipListContainer.transform.SetParent(_rightPanel.transform, false);
        LE(_shipListContainer, prefH: 120, minH: 20);
        var svg = _shipListContainer.AddComponent<VerticalLayoutGroup>();
        svg.spacing               = 3;
        svg.childControlWidth     = true;
        svg.childControlHeight    = true;
        svg.childForceExpandWidth = true;
        svg.childForceExpandHeight= false;
        _shipListContainer.SetActive(false);

        BuildBuildSlotPanel(_rightPanel.transform);
        BuildShipDetail(_rightPanel.transform);

        _rightPanel.SetActive(false);
    }

    private void RefreshRightPanel(PrototypeGameController game, BuildingInstance sel, ShipInstance selShip, BuildSlot selSlot = null)
    {
        bool showShipyard = sel != null && sel.IsShipyard;
        bool showTownHall = sel != null && sel.IsTownHall;
        bool showBuilding = sel != null && !sel.IsShipyard && !sel.IsTownHall;
        bool showShip     = selShip != null;
        bool showSlot     = selSlot != null;
        bool anySelected  = showShipyard || showTownHall || showBuilding || showShip || showSlot;

        _rightPanel?.SetActive(anySelected);
        if (!anySelected)
        {
            _shipListContainer?.SetActive(false);
            _shipDetailContainer?.SetActive(false);
            _game.SelectShip(null);
            return;
        }

        // Always reset containers — each branch decides what to show
        _shipListContainer?.SetActive(false);
        _shipDetailContainer?.SetActive(false);
        _buildSlotContainer?.SetActive(false);

        if (showShipyard)
        {
            float pct = sel.ShipProgress / BalanceConfig.ShipProgressRequired * 100f;
            SetTitle("Shipyard");
            SetWorkerLine($"Workers: {sel.AssignedWorkers} / {sel.MaxWorkers}  |  Progress: {pct:F0}%");
            SetOutputLine($"Cost/tick — Wood: {sel.ShipWoodPerCycle}  Steel: {sel.ShipSteelPerCycle}  Cloth: {sel.ShipClothPerCycle}  Rope: {sel.ShipRopePerCycle}\nShips built: {sel.ShipCount}");
            SetButtons("+ Add worker",    () => game.AssignWorkerToSelectedBuilding(),
                       "- Remove worker", () => game.RemoveWorkerFromSelectedBuilding(),
                       sel.AssignedWorkers < sel.MaxWorkers && game.FreeWorkers > 0,
                       sel.AssignedWorkers > 0);

            bool inDetail = selShip != null;
            _shipListContainer?.SetActive(!inDetail);
            _shipDetailContainer?.SetActive(inDetail);

            if (inDetail)
                RefreshShipDetail(game, selShip);
            else
                RefreshShipList(game);
        }
        else if (showTownHall)
        {
            SetTitle("Town Hall");
            SetWorkerLine("The heart of New Haven.");
            SetOutputLine("Population: " + game.TotalPopulation +
                "\nChildren: " + game.Children +
                "\nFree workers: " + game.FreeWorkers);
            SetButtons("", null, "", null, false, false);
            _shipListContainer?.SetActive(false);
        }
        else if (showSlot)
        {
            // Show construction progress only
            SetTitle("Under Construction");
            if (selSlot.State == BuildSlot.SlotState.UnderConstruction)
            {
                SetWorkerLine(selSlot.QueuedType.ToString());
                SetOutputLine(
                    $"Progress: {selSlot.ConstructionProgress * 100f:F0}%\n"
                    + $"Time left: {selSlot.ConstructionHoursRemaining:F0}h");
            }
            else
            {
                SetWorkerLine("Empty plot");
                SetOutputLine("Use build panel (⚒) to place a building.");
            }
            SetButtons("", null, "", null, false, false);
            _shipListContainer?.SetActive(false);
            _buildSlotContainer?.SetActive(false);
        }
        else if (showBuilding)
        {
            float perW = sel.OutputPerWorkerPerHour;
            float tot  = sel.TotalOutputPerHour;
            SetTitle(sel.DisplayName);
            SetWorkerLine($"Workers: {sel.AssignedWorkers} / {sel.MaxWorkers}" +
                (sel.WorkersInside > 0 ? $"  ({sel.WorkersInside} active)" : ""));
            SetOutputLine(
                $"{sel.OutputType}/worker/h: {perW:F2}\n" +
                $"Total/h: {tot:F2}  |  /day: {tot * 24f:F1}");
            SetButtons("+ Add worker",    () => game.AssignWorkerToSelectedBuilding(),
                       "- Remove worker", () => game.RemoveWorkerFromSelectedBuilding(),
                       sel.AssignedWorkers < sel.MaxWorkers && game.FreeWorkers > 0,
                       sel.AssignedWorkers > 0);
            _shipListContainer?.SetActive(false);
        }
        else if (showShip)
        {
            // Direct ship click without Shipyard selected (edge case)
            SetTitle(selShip.DisplayName);
            SetWorkerLine($"Sailors: {selShip.AssignedSailors} / {selShip.MaxSailors}" +
                (selShip.SailorsInside > 0 ? $"  ({selShip.SailorsInside} aboard)" : ""));
            SetOutputLine(
                $"Passengers: {selShip.Passengers} / {selShip.MaxPassengers}\n" +
                $"Food loaded: {selShip.FoodLoaded} / {selShip.RequiredFood}\n" +
                (selShip.IsReadyToSail ? "Ready to sail!" : "Not ready to sail"));
            SetButtons("+ Add sailor",    () => game.AssignSailorToSelectedShip(),
                       "- Remove sailor", () => game.RemoveSailorFromSelectedShip(),
                       selShip.AssignedSailors < selShip.MaxSailors && game.FreeWorkers > 0,
                       selShip.AssignedSailors > 0);
            _shipListContainer?.SetActive(false);
        }
    }

    private void BuildBuildSlotPanel(Transform parent)
    {
        _buildSlotContainer = new GameObject("BuildSlotPanel", typeof(RectTransform));
        _buildSlotContainer.transform.SetParent(parent, false);
        LE(_buildSlotContainer, prefH: 200, minH: 20);

        var vg = _buildSlotContainer.AddComponent<VerticalLayoutGroup>();
        vg.spacing                = 4;
        vg.childControlWidth      = true;
        vg.childControlHeight     = true;
        vg.childForceExpandWidth  = true;
        vg.childForceExpandHeight = false;

        _buildSlotTitle = MakeText(_buildSlotContainer.transform, "SlotTitle", 10,
            TextAlignmentOptions.TopLeft);
        _buildSlotTitle.color = new Color(0.75f, 0.75f, 0.75f);
        LE(_buildSlotTitle.gameObject, prefH: 30, minH: 30);
        _buildSlotTitle.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // One button per buildable type (excluding TownHall)
        var types = new BuildingType[]
        {
            BuildingType.Sawmill, BuildingType.Steelworks,
            BuildingType.Fiberworks, BuildingType.Cookhouse, BuildingType.Shipyard
        };
        foreach (var type in types)
        {
            var t = type; // capture
            var cost = BuildingCost.For(type);
            string lbl = BuildingLabel(type, cost);
            var btn = MakeBtn(_buildSlotContainer.transform, lbl,
                () => _game.TryBuildOnSelectedSlot(t), 166, 26);
            LE(btn.gameObject, prefH: 26, minH: 26);
            _buildOptionBtns.Add(btn);
        }

        _buildSlotContainer.SetActive(false);
    }

    private void RefreshBuildSlotPanel(PrototypeGameController game, BuildSlot slot)
    {
        if (_buildSlotContainer == null || slot == null) return;

        if (slot.State == BuildSlot.SlotState.UnderConstruction)
        {
            // Show construction progress
            if (_buildSlotTitle != null)
                _buildSlotTitle.text =
                    $"Building {slot.QueuedType}...\n" +
                    $"Progress: {slot.ConstructionProgress * 100f:F0}%\n" +
                    $"Time left: {slot.ConstructionHoursRemaining:F0}h";
            foreach (var b in _buildOptionBtns)
                if (b != null) b.gameObject.SetActive(false);
            return;
        }

        // Empty slot — show build options
        if (_buildSlotTitle != null)
            _buildSlotTitle.text = "Empty plot — choose a building:";

        var types = new BuildingType[]
        {
            BuildingType.Sawmill, BuildingType.Steelworks,
            BuildingType.Fiberworks, BuildingType.Cookhouse, BuildingType.Shipyard
        };

        for (int i = 0; i < _buildOptionBtns.Count && i < types.Length; i++)
        {
            var btn  = _buildOptionBtns[i];
            var cost = BuildingCost.For(types[i]);
            if (btn == null) continue;
            btn.gameObject.SetActive(true);
            SetBtnLabel(btn, BuildingLabel(types[i], cost));
            btn.interactable = cost.CanAfford(game.Wood, game.Steel, game.Cloth, game.Rope);
        }
    }

    private static string BuildingLabel(BuildingType type, BuildingCost cost)
    {
        string parts = "";
        if (cost.Wood  > 0) parts += $"W:{cost.Wood} ";
        if (cost.Steel > 0) parts += $"S:{cost.Steel} ";
        if (cost.Cloth > 0) parts += $"C:{cost.Cloth} ";
        return $"{type}  [{parts.Trim()}  {cost.Hours}h]";
    }

    private void BuildShipDetail(Transform parent)
    {
        _shipDetailContainer = new GameObject("ShipDetail", typeof(RectTransform));
        _shipDetailContainer.transform.SetParent(parent, false);
        LE(_shipDetailContainer, prefH: 160, minH: 20);

        var vg = _shipDetailContainer.AddComponent<VerticalLayoutGroup>();
        vg.spacing               = 5;
        vg.childControlWidth     = true;
        vg.childControlHeight    = true;
        vg.childForceExpandWidth = true;
        vg.childForceExpandHeight= false;

        // Back button row
        _shipDetailBack = MakeBtn(_shipDetailContainer.transform,
            "< Back to list", () => {
                _game.SelectShip(null);
            }, 166, 22);
        LE(_shipDetailBack.gameObject, prefH: 22, minH: 22);

        Separator(_shipDetailContainer.transform);

        _shipDetailTitle = MakeText(_shipDetailContainer.transform, "ShipDetailTitle", 11,
            TextAlignmentOptions.TopLeft);
        _shipDetailTitle.fontStyle = FontStyles.Bold;
        LE(_shipDetailTitle.gameObject, prefH: 16, minH: 16);

        _shipDetailInfo = MakeText(_shipDetailContainer.transform, "ShipDetailInfo", 10,
            TextAlignmentOptions.TopLeft);
        _shipDetailInfo.color = new Color(0.75f, 0.88f, 0.75f);
        _shipDetailInfo.textWrappingMode = TMPro.TextWrappingModes.Normal;
        LE(_shipDetailInfo.gameObject, prefH: 48, minH: 48);

        Separator(_shipDetailContainer.transform);

        _shipDetailAssign = MakeBtn(_shipDetailContainer.transform,
            "+ Add sailor", () => _game.AssignSailorToSelectedShip(), 166, 24);
        LE(_shipDetailAssign.gameObject, prefH: 24, minH: 24);

        Gap(_shipDetailContainer.transform, 3);

        _shipDetailRemove = MakeBtn(_shipDetailContainer.transform,
            "- Remove sailor", () => _game.RemoveSailorFromSelectedShip(), 166, 24);
        LE(_shipDetailRemove.gameObject, prefH: 24, minH: 24);

        _shipDetailContainer.SetActive(false);
    }

    private void RefreshShipDetail(PrototypeGameController game, ShipInstance ship)
    {
        if (_shipDetailContainer == null || ship == null) return;

        if (_shipDetailTitle != null)
            _shipDetailTitle.text = ship.DisplayName;

        if (_shipDetailInfo != null)
            _shipDetailInfo.text =
                $"Sailors: {ship.AssignedSailors} / {ship.MaxSailors}\n" +
                $"Passengers: {ship.Passengers} / {ship.MaxPassengers}\n" +
                $"Food: {ship.FoodLoaded} / {ship.RequiredFood}\n" +
                (ship.IsReadyToSail ? "Ready to sail!" : "Not ready");

        if (_shipDetailAssign != null)
            _shipDetailAssign.interactable = ship.AssignedSailors < ship.MaxSailors && game.FreeWorkers > 0;
        if (_shipDetailRemove != null)
            _shipDetailRemove.interactable = ship.AssignedSailors > 0;
    }

    private void RefreshShipList(PrototypeGameController game)
    {
        if (_shipListContainer == null) return;
        var ships = game.GetShips();

        // Sync row count
        while (_shipRowBtns.Count > ships.Count)
        {
            var last = _shipRowBtns[_shipRowBtns.Count - 1];
            _shipRowBtns.RemoveAt(_shipRowBtns.Count - 1);
            if (last != null) Destroy(last.gameObject);
        }
        while (_shipRowBtns.Count < ships.Count)
        {
            var btn = MakeBtn(_shipListContainer.transform, "", () => { }, 166, 22);
            LE(btn.gameObject, prefH: 22, minH: 22);
            _shipRowBtns.Add(btn);
        }

        // Update each row
        for (int i = 0; i < ships.Count; i++)
        {
            var ship = ships[i];
            var btn  = _shipRowBtns[i];
            if (btn == null) continue;

            btn.GetComponent<Image>().color = game.GetSelectedShip() == ship
                ? new Color(0.22f, 0.46f, 0.26f)
                : new Color(0.18f, 0.18f, 0.18f, 0.95f);

            var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null)
                lbl.text = $"{ship.DisplayName}  —  Sailors: {ship.AssignedSailors}/{ship.MaxSailors}";

            int idx = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                game.SelectShip(game.GetShips()[idx]);
                // Keep Shipyard selected — submenu opens inside same panel
            });
        }
    }

    private void RefreshSpeedButtons(int speed)
    {
        SetHighlight(_pauseBtn,  speed == 0);
        SetHighlight(_speed1Btn, speed == 1);
        SetHighlight(_speed2Btn, speed == 2);
        SetHighlight(_speed3Btn, speed == 3);
    }

    // -------------------------------------------------------
    // Panel helpers
    // -------------------------------------------------------

    private void SetTitle(string text)       { if (_titleText  != null) _titleText.text  = text; }
    private void SetWorkerLine(string text)  { if (_workerText != null) _workerText.text = text; }
    private void SetOutputLine(string text)  { if (_outputText != null) _outputText.text = text; }

    private void SetButtons(string assignLabel, UnityEngine.Events.UnityAction assignCb,
                             string removeLabel, UnityEngine.Events.UnityAction removeCb,
                             bool assignEnabled, bool removeEnabled)
    {
        if (_assignBtn != null)
        {
            SetBtnLabel(_assignBtn, assignLabel);
            _assignBtn.onClick.RemoveAllListeners();
            if (assignCb != null) _assignBtn.onClick.AddListener(assignCb);
            _assignBtn.interactable = assignEnabled;
        }
        if (_removeBtn != null)
        {
            SetBtnLabel(_removeBtn, removeLabel);
            _removeBtn.onClick.RemoveAllListeners();
            if (removeCb != null) _removeBtn.onClick.AddListener(removeCb);
            _removeBtn.interactable = removeEnabled;
        }
    }

    // -------------------------------------------------------
    // UI factory
    // -------------------------------------------------------

    private RectTransform MakePanel(Transform parent, string name,
        Vector2 ancMin, Vector2 ancMax, Vector2 ancPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = ancMin;
        rt.anchorMax        = ancMax;
        rt.pivot            = new Vector2(Mathf.Approximately(ancMin.x, ancMax.x) ? ancMin.x : 0.5f,
                                          Mathf.Approximately(ancMin.y, ancMax.y) ? ancMin.y : 0.5f);
        rt.anchoredPosition = ancPos;
        rt.sizeDelta        = size;
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        return rt;
    }

    private TextMeshProUGUI MakeText(Transform parent, string name, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize         = fontSize;
        t.alignment        = align;
        t.color            = Color.white;
        t.enableAutoSizing = false;
        t.margin           = new Vector4(0, 1, 0, 1);
        return t;
    }

    private Button MakeBtn(Transform parent, string label, UnityEngine.Events.UnityAction cb, float w, float h)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        LE(go, prefW: w, prefH: h, minW: w * 0.6f, minH: h);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.20f, 0.20f, 0.20f, 0.95f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var bc = btn.colors;
        bc.highlightedColor = new Color(0.32f, 0.32f, 0.32f);
        bc.pressedColor     = new Color(0.10f, 0.10f, 0.10f);
        btn.colors = bc;
        btn.onClick.AddListener(cb);

        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(go.transform, false);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;

        var t = lblGO.AddComponent<TextMeshProUGUI>();
        t.text               = label;
        t.fontSize           = 10f;
        t.alignment          = TextAlignmentOptions.Center;
        t.color              = Color.white;
        t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        return btn;
    }

    private void Separator(Transform parent)
    {
        var go = new GameObject("Sep", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.28f, 0.28f, 0.28f, 0.8f);
        LE(go, prefH: 1);
    }

    private void Gap(Transform parent, float height)
    {
        var go = new GameObject("Gap", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        LE(go, prefH: height, minH: height);
    }

    private void Spacer(Transform parent)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().flexibleWidth = 1;
    }

    private static void LE(GameObject go,
        float prefW = -1, float prefH = -1, float minW = -1, float minH = -1,
        float flexW = -1, float flexH = -1)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        if (prefW >= 0) le.preferredWidth  = prefW;
        if (prefH >= 0) le.preferredHeight = prefH;
        if (minW  >= 0) le.minWidth        = minW;
        if (minH  >= 0) le.minHeight       = minH;
        if (flexW >= 0) le.flexibleWidth   = flexW;
        if (flexH >= 0) le.flexibleHeight  = flexH;
    }

    private static void SetBtnLabel(Button btn, string text)
    {
        var lbl = btn?.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl != null) lbl.text = text;
    }

    private static void SetHighlight(Button btn, bool on)
    {
        if (btn == null) return;
        btn.GetComponent<Image>().color = on
            ? new Color(0.20f, 0.46f, 0.24f)
            : new Color(0.20f, 0.20f, 0.20f, 0.95f);
    }
}
