using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Refreshes all HUD elements each frame.
/// All UI references assigned in the Inspector — no runtime UI creation.
///
/// MISLAV — povuci elemente iz Hierarchy u polja u Inspectoru.
/// Pogledaj komentare uz svako polje.
/// </summary>
public class PrototypeUIController : MonoBehaviour
{
    // ---- Top bar ----
    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI _resourcesText;  // "Wood: X  Steel: X ..."
    [SerializeField] private TextMeshProUGUI _clockText;      // "Day X  HH:MM"
    [SerializeField] private Button          _pauseBtn;
    [SerializeField] private Button          _speed1Btn;
    [SerializeField] private Button          _speed2Btn;
    [SerializeField] private Button          _speed3Btn;

    // ---- Bottom bar ----
    [Header("Bottom Bar")]
    [SerializeField] private TextMeshProUGUI _populationText; // "Population: X  ..."

    // ---- Right panel ----
    [Header("Right Panel")]
    [SerializeField] private GameObject      _rightPanel;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _workerText;
    [SerializeField] private TextMeshProUGUI _outputText;
    [SerializeField] private Button          _assignBtn;
    [SerializeField] private Button          _removeBtn;
    [SerializeField] private Button          _assignEngBtn;
    [SerializeField] private Button          _removeEngBtn;
    [SerializeField] private Button          _upgradeBtn;

    // ---- Ship list (Shipyard panel) ----
    [Header("Ship List")]
    [SerializeField] private GameObject      _shipListContainer;  // parent for ship row buttons
    [SerializeField] private GameObject      _shipRowPrefab;      // prefab: Button with TMP label child

    // ---- Ship detail (submenu) ----
    [Header("Ship Detail")]
    [SerializeField] private GameObject      _shipDetailContainer;
    [SerializeField] private TextMeshProUGUI _shipDetailTitle;
    [SerializeField] private TextMeshProUGUI _shipDetailInfo;
    [SerializeField] private Button          _shipDetailBack;
    [SerializeField] private Button          _shipDetailAssign;
    [SerializeField] private Button          _shipDetailRemove;

    // ---- Build slot panel ----
    [Header("Build Slot")]
    [SerializeField] private GameObject      _buildSlotContainer;
    [SerializeField] private TextMeshProUGUI _buildSlotTitle;
    [SerializeField] private Button[]        _buildOptionBtns;    // 5 buttons: Sawmill,Steelworks,Fiberworks,Cookhouse,Shipyard

    // ---- Build panel (bottom hammer button) ----
    [Header("Build Panel")]
    [SerializeField] private BuildPanel      _buildPanel;

    // ---- Save/Load panel ----
    [Header("Save/Load")]
    [SerializeField] private SaveLoadPanel   _saveLoadPanel;

    // ---- Runtime ----
    private readonly List<Button> _shipRowBtns = new();
    private PrototypeGameController _game;
    private Camera                  _mainCamera;

    // -------------------------------------------------------
    // Init — called by Bootstrapper
    // -------------------------------------------------------

    private bool _initialized;

    public void Initialize(PrototypeGameController game, Camera cam)
    {
        if (_initialized) return;
        _initialized = true;
        _game = game;

        // Wire speed buttons
        _pauseBtn? .onClick.AddListener(() => _game.SetSpeed(0));
        _speed1Btn?.onClick.AddListener(() => _game.SetSpeed(1));
        _speed2Btn?.onClick.AddListener(() => _game.SetSpeed(2));
        _speed3Btn?.onClick.AddListener(() => _game.SetSpeed(3));

        // Wire assign/remove buttons (labels updated in Refresh)
        _assignBtn?   .onClick.AddListener(() => _game.AssignWorkerToSelectedBuilding());
        _removeBtn?   .onClick.AddListener(() => _game.RemoveWorkerFromSelectedBuilding());
        _assignEngBtn?.onClick.AddListener(() => _game.AssignEngineerToSelectedBuilding());
        _removeEngBtn?.onClick.AddListener(() => _game.RemoveEngineerFromSelectedBuilding());
        _upgradeBtn?  .onClick.AddListener(() => _game.TryUpgradeSelectedBuilding());

        // Wire ship detail buttons
        _shipDetailBack?  .onClick.AddListener(() => _game.SelectShip(null));
        _shipDetailAssign?.onClick.AddListener(() => _game.AssignSailorToSelectedShip());
        _shipDetailRemove?.onClick.AddListener(() => _game.RemoveSailorFromSelectedShip());

        // Initialize sub-panels
        _buildPanel?   .Build(game, GetComponentInParent<Canvas>()?.transform ?? transform, cam);
        _saveLoadPanel?.Build(game, GetComponentInParent<Canvas>()?.transform ?? transform);

        _rightPanel?.SetActive(false);
    }

    // Keep Build() as alias so Bootstrapper doesn't break before migration
    public void Build(PrototypeGameController game, Camera cam = null) => Initialize(game, cam);

    // -------------------------------------------------------
    // Refresh — called every frame by GameController
    // -------------------------------------------------------

    public void Refresh(PrototypeGameController game,
        BuildingInstance selBuilding, ShipInstance selShip = null, BuildSlot selSlot = null)
    {
        if (game == null) return;
        _game = game; // ensure _game is set even before Build() is called
        RefreshTopBar(game);
        RefreshBottomBar(game);
        RefreshRightPanel(game, selBuilding, selShip, selSlot);
        RefreshSpeedButtons(game.SpeedMultiplier);
        _buildPanel?.Refresh(game);
    }

    // -------------------------------------------------------
    // Top bar
    // -------------------------------------------------------

    private void RefreshTopBar(PrototypeGameController game)
    {
        if (_resourcesText != null)
            _resourcesText.text =
                $"Wood: {game.Wood}   Steel: {game.Steel}   Cloth: {game.Cloth}" +
                $"   Rope: {game.Rope}   Food: {game.Food}   Ships: {game.Ships}";

        if (_clockText != null)
            _clockText.text = $"Day {game.Day}  {game.Hour:00}:{game.Minute:00}";
    }

    // -------------------------------------------------------
    // Bottom bar
    // -------------------------------------------------------

    private void RefreshBottomBar(PrototypeGameController game)
    {
        if (_populationText == null) return;
        float foodPerDay = EconomyCalculator.FoodConsumptionPerDay(game.AdultPopulation, game.Children);
        _populationText.text =
            $"Population: {game.TotalPopulation}   Children: {game.Children}   " +
            $"Workers: {game.FreeWorkers}   Engineers: {game.FreeEngineers}   Food/day: -{foodPerDay:F0}";
    }

    // -------------------------------------------------------
    // Right panel
    // -------------------------------------------------------

    private void RefreshRightPanel(PrototypeGameController game,
        BuildingInstance sel, ShipInstance selShip, BuildSlot selSlot)
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
            _shipListContainer?  .SetActive(false);
            _shipDetailContainer?.SetActive(false);
            _game.SelectShip(null);
            return;
        }

        // Reset all sub-containers
        _shipListContainer?  .SetActive(false);
        _shipDetailContainer?.SetActive(false);
        if (_buildSlotContainer != null) _buildSlotContainer.SetActive(false);

        if (showShipyard)
        {
            float pct = sel.ShipProgress / BalanceConfig.ShipProgressRequired * 100f;
            SetTitle("Shipyard");
            SetWorkerLine($"Workers: {sel.AssignedWorkers}/{sel.MaxWorkers}  Engineers: {sel.AssignedEngineers}/{sel.MaxEngineers}  |  {pct:F0}%");
            SetOutputLine($"Cost/tick — Wood: {sel.ShipWoodPerCycle}  Steel: {sel.ShipSteelPerCycle}" +
                          $"  Cloth: {sel.ShipClothPerCycle}  Rope: {sel.ShipRopePerCycle}" +
                          $"\nShips built: {sel.ShipCount}");
            SetButtons("+ Add worker",    () => game.AssignWorkerToSelectedBuilding(),
                       "- Remove worker", () => game.RemoveWorkerFromSelectedBuilding(),
                       sel.AssignedWorkers < sel.MaxWorkers && game.FreeWorkers > 0,
                       sel.AssignedWorkers > 0);
            SetEngButtons(sel.AssignedEngineers < sel.MaxEngineers && game.FreeEngineers > 0,
                          sel.AssignedEngineers > 0);

            bool inDetail = selShip != null;
            _shipListContainer?  .SetActive(!inDetail);
            _shipDetailContainer?.SetActive(inDetail);
            if (inDetail) RefreshShipDetail(game, selShip);
            else          RefreshShipList(game);
        }
        else if (showTownHall)
        {
            SetTitle("Town Hall");
            SetWorkerLine("The heart of New Haven.");
            SetOutputLine($"Population: {game.TotalPopulation}\n" +
                          $"Children: {game.Children}\n" +
                          $"Free workers: {game.FreeWorkers}");
            SetButtons("", null, "", null, false, false);
        }
        else if (showSlot)
        {
            SetTitle("Under Construction");
            if (selSlot.State == BuildSlot.SlotState.UnderConstruction)
            {
                SetWorkerLine(selSlot.QueuedType.ToString());
                SetOutputLine($"Progress: {selSlot.ConstructionProgress * 100f:F0}%\n" +
                              $"Time left: {selSlot.ConstructionHoursRemaining:F0}h");
            }
            else
            {
                SetWorkerLine("Empty plot");
                SetOutputLine("Use build panel [B] to place a building.");
            }
            SetButtons("", null, "", null, false, false);
        }
        else if (showBuilding)
        {
            float perW = sel.OutputPerWorkerPerHour;
            float tot  = sel.TotalOutputPerHour;
            SetTitle(sel.DisplayName);
            {
                string engStr = sel.AssignedEngineers > 0
                    ? $"  | Eng: {sel.AssignedEngineers} +{(int)((sel.EngineerBonus-1)*100)}%"
                    : "";
                string rawStr = sel.BuildingTypeEnum == BuildingType.Cookhouse
                    ? $"  | Raw Food: {(int)game.RawFood}"
                    : "";
                SetWorkerLine($"Workers: {sel.AssignedWorkers}/{sel.MaxWorkers}" +
                    (sel.WorkersInside > 0 ? $" ({sel.WorkersInside} active)" : "") +
                    engStr + rawStr);
            }
            SetOutputLine($"{sel.OutputType}/worker/h: {perW:F2}\n" +
                          $"Total/h: {tot:F2}  |  /day: {tot * 24f:F1}");
            SetButtons("+ Add worker",    () => game.AssignWorkerToSelectedBuilding(),
                       "- Remove worker", () => game.RemoveWorkerFromSelectedBuilding(),
                       sel.AssignedWorkers < sel.MaxWorkers && game.FreeWorkers > 0,
                       sel.AssignedWorkers > 0);
            SetEngButtons(sel.AssignedEngineers < sel.MaxEngineers && game.FreeEngineers > 0,
                          sel.AssignedEngineers > 0);
            SetUpgradeBtn(true, game.CanAffordUpgrade(sel));
        }
        else if (showShip)
        {
            SetTitle(selShip.DisplayName);
            SetWorkerLine($"Sailors: {selShip.AssignedSailors} / {selShip.MaxSailors}" +
                (selShip.SailorsInside > 0 ? $"  ({selShip.SailorsInside} aboard)" : ""));
            SetOutputLine($"Passengers: {selShip.Passengers} / {selShip.MaxPassengers}\n" +
                          $"Food loaded: {selShip.FoodLoaded} / {selShip.RequiredFood}\n" +
                          (selShip.IsReadyToSail ? "Ready to sail!" : "Not ready to sail"));
            SetButtons("+ Add sailor",    () => game.AssignSailorToSelectedShip(),
                       "- Remove sailor", () => game.RemoveSailorFromSelectedShip(),
                       selShip.AssignedSailors < selShip.MaxSailors && game.FreeWorkers > 0,
                       selShip.AssignedSailors > 0);
            SetShipPassengerButtons(selShip, game);
            SetShipFoodButtons(selShip, game);
        }
    }

    private void RefreshShipList(PrototypeGameController game)
    {
        if (_shipListContainer == null) return;
        var ships = game.GetShips();

        // Remove extra rows
        while (_shipRowBtns.Count > ships.Count)
        {
            var last = _shipRowBtns[_shipRowBtns.Count - 1];
            _shipRowBtns.RemoveAt(_shipRowBtns.Count - 1);
            if (last != null) Destroy(last.gameObject);
        }

        // Add missing rows
        while (_shipRowBtns.Count < ships.Count)
        {
            Button btn;
            if (_shipRowPrefab != null)
            {
                var go = Instantiate(_shipRowPrefab, _shipListContainer.transform);
                btn    = go.GetComponent<Button>();
            }
            else
            {
                // Fallback: create button in code if no prefab assigned
                btn = CreateShipRowButton(_shipListContainer.transform);
            }
            _shipRowBtns.Add(btn);
        }

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
            btn.onClick.AddListener(() => game.SelectShip(game.GetShips()[idx]));
        }
    }

    private void RefreshShipDetail(PrototypeGameController game, ShipInstance ship)
    {
        if (ship == null) return;
        if (_shipDetailTitle != null) _shipDetailTitle.text = ship.DisplayName;
        if (_shipDetailInfo  != null)
            _shipDetailInfo.text =
                $"Sailors: {ship.AssignedSailors} / {ship.MaxSailors}\n" +
                $"Passengers: {ship.Passengers} / {ship.MaxPassengers}\n" +
                $"Food: {ship.FoodLoaded} / {ship.RequiredFood}\n" +
                (ship.IsReadyToSail ? "Ready to sail!" : "Not ready");

        if (_shipDetailAssign != null)
            _shipDetailAssign.interactable =
                ship.AssignedSailors < ship.MaxSailors && game.FreeWorkers > 0;
        if (_shipDetailRemove != null)
            _shipDetailRemove.interactable = ship.AssignedSailors > 0;
        SetShipPassengerButtons(ship, game);
        SetShipFoodButtons(ship, game);
    }

    private void RefreshSpeedButtons(int speed)
    {
        Highlight(_pauseBtn,  speed == 0);
        Highlight(_speed1Btn, speed == 1);
        Highlight(_speed2Btn, speed == 2);
        // No 3x — Frostpunk style: pause, 1x, 2x only
        if (_speed3Btn != null) _speed3Btn.gameObject.SetActive(false);
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    private void SetTitle(string text)      { if (_titleText  != null) _titleText.text  = text; }
    private void SetWorkerLine(string text) { if (_workerText != null) _workerText.text = text; }
    private void SetOutputLine(string text) { if (_outputText != null) _outputText.text = text; }

    private void SetButtons(string assignLabel, UnityEngine.Events.UnityAction assignCb,
                             string removeLabel, UnityEngine.Events.UnityAction removeCb,
                             bool assignEnabled, bool removeEnabled)
    {
        if (_assignBtn != null)
        {
            SetLabel(_assignBtn, assignLabel);
            _assignBtn.onClick.RemoveAllListeners();
            if (assignCb != null) _assignBtn.onClick.AddListener(assignCb);
            _assignBtn.interactable = assignEnabled;
        }
        if (_removeBtn != null)
        {
            SetLabel(_removeBtn, removeLabel);
            _removeBtn.onClick.RemoveAllListeners();
            if (removeCb != null) _removeBtn.onClick.AddListener(removeCb);
            _removeBtn.interactable = removeEnabled;
        }
    }

    private void SetShipPassengerButtons(ShipInstance ship, PrototypeGameController game)
    {
        if (_addPassengerBtn != null)
        {
            _addPassengerBtn.gameObject.SetActive(true);
            _addPassengerBtn.interactable = ship.Passengers < ship.MaxPassengers && game.FreeWorkers > 0;
            SetLabel(_addPassengerBtn, $"+ Passenger ({ship.Passengers}/{ship.MaxPassengers})");
        }
        if (_removePassengerBtn != null)
        {
            _removePassengerBtn.gameObject.SetActive(true);
            _removePassengerBtn.interactable = ship.Passengers > 0;
            SetLabel(_removePassengerBtn, "- Passenger");
        }
    }

    private void SetShipFoodButtons(ShipInstance ship, PrototypeGameController game)
    {
        if (_loadFoodBtn != null)
        {
            _loadFoodBtn.gameObject.SetActive(true);
            _loadFoodBtn.interactable = game.Food >= BalanceConfig.ShipFoodLoadStep
                                      && ship.FoodLoaded < ship.RequiredFood;
            SetLabel(_loadFoodBtn, $"+ Load Food ({ship.FoodLoaded}/{ship.RequiredFood})");
        }
        if (_unloadFoodBtn != null)
        {
            _unloadFoodBtn.gameObject.SetActive(true);
            _unloadFoodBtn.interactable = ship.FoodLoaded > 0;
            SetLabel(_unloadFoodBtn, "- Unload Food");
        }
    }

    private void SetUpgradeBtn(bool visible, bool enabled)
    {
        if (_upgradeBtn == null) return;
        _upgradeBtn.gameObject.SetActive(visible);
        _upgradeBtn.interactable = enabled;
        SetLabel(_upgradeBtn, enabled ? "Upgrade ▲" : "Upgrade (insufficient resources)");
    }

    private void SetEngButtons(bool assignEnabled, bool removeEnabled)
    {
        if (_assignEngBtn != null) _assignEngBtn.interactable = assignEnabled;
        if (_removeEngBtn != null) _removeEngBtn.interactable = removeEnabled;
        if (_assignEngBtn != null) _assignEngBtn.gameObject.SetActive(true);
        if (_removeEngBtn != null) _removeEngBtn.gameObject.SetActive(true);
    }

    private static void SetLabel(Button btn, string text)
    {
        var t = btn?.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null) t.text = text;
    }

    private static void Highlight(Button btn, bool on)
    {
        if (btn == null) return;
        btn.GetComponent<Image>().color = on
            ? new Color(0.20f, 0.46f, 0.24f)
            : new Color(0.20f, 0.20f, 0.20f, 0.95f);
    }

    private static Button CreateShipRowButton(Transform parent)
    {
        var go  = new GameObject("ShipRow", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredHeight = 22f; le.minHeight = 22f;
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        var btn = go.GetComponent<Button>();

        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(go.transform, false);
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(4, 2); lrt.offsetMax = new Vector2(-4, -2);
        var t = lblGO.AddComponent<TextMeshProUGUI>();
        t.fontSize = 9f; t.color = Color.white;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        return btn;
    }

    // -------------------------------------------------------
    // BuildFromCode — fallback until Mislav sets up scene UI
    // Creates all UI elements programmatically (old approach).
    // Remove this when Inspector references are wired.
    // -------------------------------------------------------

    public void BuildFromCode(PrototypeGameController game, Transform canvasTransform, Camera cam)
    {
        _game       = game;
        _mainCamera = cam;

        BuildTopBarFromCode(canvasTransform);
        BuildBottomBarFromCode(canvasTransform);
        BuildRightPanelFromCode(canvasTransform);

        var buildPanelGO = new GameObject("BuildPanel");
        _buildPanel = buildPanelGO.AddComponent<BuildPanel>();
        _buildPanel.Build(game, canvasTransform, cam);

        var slGO = new GameObject("SaveLoadPanel");
        _saveLoadPanel = slGO.AddComponent<SaveLoadPanel>();
        _saveLoadPanel.Build(game, canvasTransform);

        // Wire speed buttons
        _pauseBtn? .onClick.AddListener(() => _game.SetSpeed(0));
        _speed1Btn?.onClick.AddListener(() => _game.SetSpeed(1));
        _speed2Btn?.onClick.AddListener(() => _game.SetSpeed(2));
        _speed3Btn?.onClick.AddListener(() => _game.SetSpeed(3));
        _assignBtn?   .onClick.AddListener(() => _game.AssignWorkerToSelectedBuilding());
        _removeBtn?   .onClick.AddListener(() => _game.RemoveWorkerFromSelectedBuilding());
        _assignEngBtn?.onClick.AddListener(() => _game.AssignEngineerToSelectedBuilding());
        _removeEngBtn?.onClick.AddListener(() => _game.RemoveEngineerFromSelectedBuilding());
        _upgradeBtn?  .onClick.AddListener(() => _game.TryUpgradeSelectedBuilding());
        _shipDetailBack?  .onClick.AddListener(() => _game.SelectShip(null));
        _shipDetailAssign?.onClick.AddListener(() => _game.AssignSailorToSelectedShip());
        _shipDetailRemove?.onClick.AddListener(() => _game.RemoveSailorFromSelectedShip());

        _rightPanel?.SetActive(false);
    }

    private void BuildTopBarFromCode(Transform parent)
    {
        var bar = MakePanel(parent, "TopBar", new Vector2(0,1), new Vector2(1,1), Vector2.zero, new Vector2(0,26));
        var hg  = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hg.padding = new RectOffset(10,10,4,4); hg.spacing = 6;
        hg.childAlignment = TextAnchor.MiddleLeft;
        hg.childControlWidth = hg.childControlHeight = true;
        hg.childForceExpandWidth = hg.childForceExpandHeight = false;
        hg.childForceExpandHeight = true;

        _resourcesText = MakeTMP(bar, "Resources", 10, TextAlignmentOptions.MidlineLeft);
        _resourcesText.textWrappingMode = TextWrappingModes.NoWrap;
        LE(_resourcesText.gameObject, flexW: 1);
        Spacer(bar);
        _clockText = MakeTMP(bar, "Clock", 10, TextAlignmentOptions.MidlineRight);
        _clockText.textWrappingMode = TextWrappingModes.NoWrap;
        LE(_clockText.gameObject, minW: 90);
        _pauseBtn  = MakeBtn(bar, "||", null, 28, 18);
        _speed1Btn = MakeBtn(bar, "1x", null, 28, 18);
        _speed2Btn = MakeBtn(bar, "2x", null, 28, 18);
        _speed3Btn = MakeBtn(bar, "3x", null, 28, 18);
    }

    private void BuildBottomBarFromCode(Transform parent)
    {
        var bar = MakePanel(parent, "BottomBar", new Vector2(0,0), new Vector2(1,0), Vector2.zero, new Vector2(0,22));
        var hg  = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hg.padding = new RectOffset(10,10,4,4);
        hg.childAlignment = TextAnchor.MiddleLeft;
        hg.childControlWidth = hg.childControlHeight = true;
        hg.childForceExpandWidth = false; hg.childForceExpandHeight = true;
        _populationText = MakeTMP(bar, "Population", 10, TextAlignmentOptions.MidlineLeft);
        _populationText.textWrappingMode = TextWrappingModes.NoWrap;
        LE(_populationText.gameObject, flexW: 1);
    }

    private void BuildRightPanelFromCode(Transform parent)
    {
        var rp = new GameObject("RightPanel", typeof(RectTransform));
        rp.transform.SetParent(parent, false);
        var rt = rp.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1,0); rt.anchorMax = new Vector2(1,1);
        rt.pivot = new Vector2(1,1);
        rt.offsetMin = new Vector2(-190,22); rt.offsetMax = new Vector2(0,-26);
        rp.AddComponent<Image>().color = new Color(0.05f,0.05f,0.05f,0.90f);
        var vg = rp.AddComponent<VerticalLayoutGroup>();
        vg.padding = new RectOffset(12,12,12,12); vg.spacing = 6;
        vg.childAlignment = TextAnchor.UpperLeft;
        vg.childControlWidth = vg.childControlHeight = vg.childForceExpandWidth = true;
        vg.childForceExpandHeight = false;
        _rightPanel = rp;

        _titleText = MakeTMP(rp.transform, "Title", 12, TextAlignmentOptions.TopLeft);
        _titleText.fontStyle = FontStyles.Bold;
        LE(_titleText.gameObject, prefH:18, minH:18);
        Sep(rp.transform);
        _workerText = MakeTMP(rp.transform, "Workers", 10, TextAlignmentOptions.TopLeft);
        _workerText.color = new Color(0.85f,0.85f,0.85f);
        LE(_workerText.gameObject, prefH:14, minH:14);
        _outputText = MakeTMP(rp.transform, "Output", 10, TextAlignmentOptions.TopLeft);
        _outputText.color = new Color(0.65f,0.82f,0.65f);
        _outputText.textWrappingMode = TextWrappingModes.Normal;
        LE(_outputText.gameObject, prefH:52, minH:52);
        Sep(rp.transform);
        _assignBtn = MakeBtn(rp.transform, "+ Add worker", null, 166, 26);
        LE(_assignBtn.gameObject, prefH:26, minH:26);
        Gap(rp.transform, 4);
        _removeBtn = MakeBtn(rp.transform, "- Remove worker", null, 166, 26);
        LE(_removeBtn.gameObject, prefH:26, minH:26);
        Sep(rp.transform);

        _shipListContainer = new GameObject("ShipList", typeof(RectTransform));
        _shipListContainer.transform.SetParent(rp.transform, false);
        LE(_shipListContainer, prefH:120, minH:20);
        var slvg = _shipListContainer.AddComponent<VerticalLayoutGroup>();
        slvg.spacing = 3; slvg.childControlWidth = slvg.childControlHeight = slvg.childForceExpandWidth = true;
        slvg.childForceExpandHeight = false;
        _shipListContainer.SetActive(false);

        BuildShipDetailFromCode(rp.transform);
        _rightPanel.SetActive(false);
    }

    private void BuildShipDetailFromCode(Transform parent)
    {
        var sd = new GameObject("ShipDetail", typeof(RectTransform));
        sd.transform.SetParent(parent, false);
        LE(sd, prefH:160, minH:20);
        var vg = sd.AddComponent<VerticalLayoutGroup>();
        vg.spacing = 5; vg.childControlWidth = vg.childControlHeight = vg.childForceExpandWidth = true;
        vg.childForceExpandHeight = false;
        _shipDetailContainer = sd;

        _shipDetailBack   = MakeBtn(sd.transform, "< Back to list", null, 166, 22);
        LE(_shipDetailBack.gameObject, prefH:22, minH:22);
        Sep(sd.transform);
        _shipDetailTitle = MakeTMP(sd.transform, "ShipTitle", 11, TextAlignmentOptions.TopLeft);
        _shipDetailTitle.fontStyle = FontStyles.Bold;
        LE(_shipDetailTitle.gameObject, prefH:16, minH:16);
        _shipDetailInfo = MakeTMP(sd.transform, "ShipInfo", 10, TextAlignmentOptions.TopLeft);
        _shipDetailInfo.color = new Color(0.75f,0.88f,0.75f);
        _shipDetailInfo.textWrappingMode = TextWrappingModes.Normal;
        LE(_shipDetailInfo.gameObject, prefH:48, minH:48);
        Sep(sd.transform);
        _shipDetailAssign = MakeBtn(sd.transform, "+ Add sailor",    null, 166, 24);
        LE(_shipDetailAssign.gameObject, prefH:24, minH:24);
        Gap(sd.transform, 3);
        _shipDetailRemove = MakeBtn(sd.transform, "- Remove sailor", null, 166, 24);
        LE(_shipDetailRemove.gameObject, prefH:24, minH:24);
        _shipDetailContainer.SetActive(false);
    }

    // ---- Code-gen helpers ----

    private RectTransform MakePanel(Transform parent, string name,
        Vector2 ancMin, Vector2 ancMax, Vector2 ancPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot = new Vector2(
            Mathf.Approximately(ancMin.x, ancMax.x) ? ancMin.x : 0.5f,
            Mathf.Approximately(ancMin.y, ancMax.y) ? ancMin.y : 0.5f);
        rt.anchoredPosition = ancPos; rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0f,0f,0f,0.78f);
        return rt;
    }

    private TextMeshProUGUI MakeTMP(Transform parent, string name, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size; t.alignment = align; t.color = Color.white;
        t.enableAutoSizing = false; t.margin = new Vector4(0,1,0,1);
        return t;
    }

    private Button MakeBtn(Transform parent, string label, UnityEngine.Events.UnityAction cb, float w, float h)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        LE(go, prefW:w, prefH:h, minW:w*0.6f, minH:h);
        var img = go.GetComponent<Image>(); img.color = new Color(0.20f,0.20f,0.20f,0.95f);
        var btn = go.GetComponent<Button>(); btn.targetGraphic = img;
        var bc = btn.colors;
        bc.highlightedColor = new Color(0.32f,0.32f,0.32f);
        bc.pressedColor = new Color(0.10f,0.10f,0.10f);
        btn.colors = bc;
        if (cb != null) btn.onClick.AddListener(cb);
        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(go.transform, false);
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var t = lblGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 10f;
        t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return btn;
    }

    private void Sep(Transform parent)
    {
        var go = new GameObject("Sep", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.28f,0.28f,0.28f,0.8f);
        LE(go, prefH:1);
    }

    private void Gap(Transform parent, float height)
    {
        var go = new GameObject("Gap", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        LE(go, prefH:height, minH:height);
    }

    private void Spacer(Transform parent)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().flexibleWidth = 1;
    }

    private static void LE(GameObject go,
        float prefW=-1, float prefH=-1, float minW=-1, float minH=-1,
        float flexW=-1, float flexH=-1)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        if (prefW >= 0) le.preferredWidth  = prefW;
        if (prefH >= 0) le.preferredHeight = prefH;
        if (minW  >= 0) le.minWidth        = minW;
        if (minH  >= 0) le.minHeight       = minH;
        if (flexW >= 0) le.flexibleWidth   = flexW;
        if (flexH >= 0) le.flexibleHeight  = flexH;
    }
}
