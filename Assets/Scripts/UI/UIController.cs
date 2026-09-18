using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Refreshes all HUD elements each frame.
/// All UI references assigned in the Inspector — no runtime UI creation.
///
/// MISLAV — povuci elemente iz Hierarchy u polja u Inspectoru.
/// Pogledaj komentare uz svako polje.
/// </summary>
public class UIController : MonoBehaviour
{
    // ---- Top bar ----
    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI _resourcesText;  // "Wood: X  Steel: X ..."
    [SerializeField] private TextMeshProUGUI _clockText;      // "Day X  HH:MM"
    [SerializeField] private TextMeshProUGUI _resourcesRightText;
    [SerializeField] private Button          _pauseBtn;
    [SerializeField] private Button          _speed1Btn;
    [SerializeField] private Button          _speed2Btn;
    [SerializeField] private Button          _speed3Btn;
    [SerializeField] private Sprite          _activeSpeedSprite;

    private Sprite _pauseNormalSprite;
    private Sprite _speed1NormalSprite;
    private Sprite _speed2NormalSprite;
    private Sprite _speed3NormalSprite;
    private bool _speedSpritesSaved;

    // ---- Hope ----
    [Header("Hope")]
    [SerializeField] private HopeBar         _hopeBar;

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

    // Brodogradnja vise ne krece sama — igrac mora naruciti brod.
    [SerializeField] private Button          _buildShipBtn;

    [Header("Right Panel Spacing")]
    [SerializeField] private GameObject[]    _optionalSpacing;

    // Ikone za male gumbe tereta. Povuci iz "Sprite sheet for Basic Pack":
    //   _plusSprite  -> Sprite sheet for Basic Pack_35
    //   _minusSprite -> Sprite sheet for Basic Pack_60
    // Ako ostanu prazni, gumbi ispisuju "+" i "-" kao tekst.
    [Header("Ikone + / -")]
    [SerializeField] private Sprite          _plusSprite;
    [SerializeField] private Sprite          _minusSprite;

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
    [SerializeField] private Button          _shipDetailAssign;   // prenamijenjeno: + putnici
    [SerializeField] private Button          _shipDetailRemove;   // prenamijenjeno: − putnici
    [SerializeField] private Button          _addPassengerBtn;
    [SerializeField] private Button          _removePassengerBtn;
    [SerializeField] private Button          _boardAllBtn;        // opcionalno
    [SerializeField] private Button          _loadFoodBtn;
    [SerializeField] private Button          _unloadFoodBtn;
    [SerializeField] private Button          _loadAllFoodBtn;     // opcionalno

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

    // ---- Tutorial ----
    [Header("Tutorial")]
    [SerializeField] private GameObject      _tutorialPanel;
    [SerializeField] private Button          _tutorialBtn;

    // ---- Runtime ----
    private readonly List<Button> _shipRowBtns = new();
    private GameController _game;
    private Camera                  _mainCamera;

    // -------------------------------------------------------
    // Init — called by Bootstrapper
    // -------------------------------------------------------

    private bool _initialized;

    public void Initialize(GameController game, Camera cam)
    {
        if (_initialized) return;
        _initialized = true;
        _game = game;

        // Wire speed buttons
        _pauseBtn? .onClick.AddListener(() => _game.SetSpeed(0));
        _speed1Btn?.onClick.AddListener(() => _game.SetSpeed(1));
        _speed2Btn?.onClick.AddListener(() => _game.SetSpeed(2));
        _speed3Btn?.onClick.AddListener(() => _game.SetSpeed(5));

        // Wire assign/remove buttons (labels updated in Refresh)
        _assignBtn?   .onClick.AddListener(() => _game.AssignWorkerToSelectedBuilding());
        _removeBtn?   .onClick.AddListener(() => _game.RemoveWorkerFromSelectedBuilding());
        _assignEngBtn?.onClick.AddListener(() => _game.AssignEngineerToSelectedBuilding());
        _removeEngBtn?.onClick.AddListener(() => _game.RemoveEngineerFromSelectedBuilding());
        _upgradeBtn?  .onClick.AddListener(() => _game.TryUpgradeSelectedBuilding());
        _buildShipBtn?.onClick.AddListener(() => _game.OrderShipOnSelectedBuilding());

        WireShipButtons();

        // Initialize sub-panels
        _buildPanel?   .Build(game, GetComponentInParent<Canvas>()?.transform ?? transform, cam);
        _saveLoadPanel?.Build(game, GetComponentInParent<Canvas>()?.transform ?? transform);

        if (_tutorialBtn != null)
        {
            _tutorialBtn.onClick.RemoveAllListeners();
            _tutorialBtn.onClick.AddListener(ToggleTutorialPanel);
        }

        _rightPanel?.SetActive(false);
        _tutorialPanel?.SetActive(false);
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (!_initialized || mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Vector2 pointer = mouse.position.ReadValue();
        var tutorialRect = _tutorialPanel != null ? _tutorialPanel.transform as RectTransform : null;
        var tutorialBtnRect = _tutorialBtn != null ? _tutorialBtn.transform as RectTransform : null;

        // Tutorial se zatvara klikom bilo gdje izvan ploce. Njegov gumb je
        // iznimka jer sam ToggleTutorialPanel odlucuje treba li otvoriti ili zatvoriti.
        if (_tutorialPanel != null && _tutorialPanel.activeSelf &&
            !ContainsScreenPoint(tutorialRect, pointer) &&
            !ContainsScreenPoint(tutorialBtnRect, pointer))
        {
            _tutorialPanel.SetActive(false);
        }

        // Klikove na svijet vec obraduje PrototypeSelectionController. Ovdje
        // zatvaramo desnu plocu kada igrac klikne neki drugi dio UI-ja.
        var rightPanelRect = _rightPanel != null ? _rightPanel.transform as RectTransform : null;
        if (_rightPanel != null && _rightPanel.activeSelf &&
            !ContainsScreenPoint(rightPanelRect, pointer) &&
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ClearSelection();
        }
    }

    private void ToggleTutorialPanel()
    {
        if (_tutorialPanel != null)
            _tutorialPanel.SetActive(!_tutorialPanel.activeSelf);
    }

    private void ClearSelection()
    {
        if (_game == null) return;
        _game.SelectShip(null);
        _game.SelectSlot(null);
        _game.SelectBuilding(null);
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

    /// <summary>
    /// Povezuje se samo "< Back". Gumbi za teret vise ne dolaze iz scene nego iz
    /// EnsureCargoRows(), koji gradi dva kompaktna retka i sam ih poveze — ranije
    /// su _addPassengerBtn, _loadFoodBtn i ostali imali natpis i interactable, ali
    /// nikad nisu dobili onClick.
    /// Povezivanje NE smije ici u Refresh(): on se izvrsava svaki frame.
    /// </summary>
    private void WireShipButtons()
    {
        _shipDetailBack?.onClick.AddListener(() => _game.SelectShip(null));
    }

    // Keep Build() as alias so Bootstrapper doesn't break before migration
    public void Build(GameController game, Camera cam = null) => Initialize(game, cam);

    // -------------------------------------------------------
    // Refresh — called every frame by GameController
    // -------------------------------------------------------

    public void Refresh(GameController game,
        BuildingInstance selBuilding, ShipInstance selShip = null, BuildSlot selSlot = null)
    {
        if (game == null) return;
        _game = game; // ensure _game is set even before Build() is called
        RefreshTopBar(game);
        ApplyInkColors();
        _hopeBar?.SetHope(game.Hope);
        RefreshRightPanel(game, selBuilding, selShip, selSlot);
        RefreshSpeedButtons(game.SpeedMultiplier);
        _buildPanel?.Refresh(game);
    }

    // -------------------------------------------------------
    // Top bar
    // -------------------------------------------------------

    /// <summary>
    /// Ploca je svijetla, pa je svijetli tekst na njoj bio jedva citljiv.
    /// Boje se postavljaju iz koda da ne ovise o tome sto je zapisano u sceni.
    /// Natpisi gumba ostaju bijeli — gumbi imaju tamnu podlogu.
    /// </summary>
    private static readonly Color PanelInk      = new Color(0.08f, 0.07f, 0.06f);
    private static readonly Color PanelInkSoft  = new Color(0.24f, 0.21f, 0.17f);

    private void ApplyInkColors()
    {
        if (_titleText       != null) _titleText.color       = PanelInk;
        if (_workerText      != null) _workerText.color      = PanelInkSoft;
        if (_outputText      != null) _outputText.color      = PanelInk;
        if (_shipDetailTitle != null) _shipDetailTitle.color = PanelInk;
        if (_shipDetailInfo  != null) _shipDetailInfo.color  = PanelInkSoft;
    }

    private void RefreshTopBar(GameController game)
    {
        if (_resourcesText != null)
            _resourcesText.text =
                $"Wood: {game.Wood}   Steel: {game.Steel}   Cloth: {game.Cloth}" +
                $"   Rope: {game.Rope}   Ships: {game.Ships}";

        if (_clockText != null)
            _clockText.text = $"Day {game.Day}  {game.Hour:00}:{game.Minute:00}";

        if (_resourcesRightText != null)
            _resourcesRightText.text = $"Raw Food: {game.RawFood}   Food: {game.Food}";
    }

    // -------------------------------------------------------
    // Right panel
    // -------------------------------------------------------

    /// <summary>
    /// Gasi sve opcionalne kontrole prije nego sto ih grana ponovno upali.
    /// Bez ovoga gumbi zadrze stanje iz prethodne selekcije — zato su se inzenjeri
    /// i nadogradnja pojavljivali u Town Hallu, gradilistu i brodu.
    /// </summary>
    private void ResetPanelControls()
    {
        if (_assignEngBtn    != null) _assignEngBtn.gameObject.SetActive(false);
        if (_removeEngBtn    != null) _removeEngBtn.gameObject.SetActive(false);
        if (_upgradeBtn      != null) _upgradeBtn.gameObject.SetActive(false);
        if (_buildShipBtn    != null) _buildShipBtn.gameObject.SetActive(false);
        if (_addPassengerBtn != null) _addPassengerBtn.gameObject.SetActive(false);
        if (_removePassengerBtn != null) _removePassengerBtn.gameObject.SetActive(false);
        if (_boardAllBtn     != null) _boardAllBtn.gameObject.SetActive(false);
        if (_loadFoodBtn     != null) _loadFoodBtn.gameObject.SetActive(false);
        if (_unloadFoodBtn   != null) _unloadFoodBtn.gameObject.SetActive(false);
        if (_loadAllFoodBtn  != null) _loadAllFoodBtn.gameObject.SetActive(false);
        if (_assignBtn       != null) _assignBtn.gameObject.SetActive(true);
        if (_removeBtn       != null) _removeBtn.gameObject.SetActive(true);
        if (_workerText      != null) _workerText.gameObject.SetActive(true);
        if (_outputText      != null) _outputText.gameObject.SetActive(true);
        SetOutputExpanded(false);
        if (_cargoRows != null) _cargoRows.SetActive(false);
    }

    private void RefreshRightPanel(GameController game,
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

        // Reset all sub-containers and optional controls
        _shipListContainer?  .SetActive(false);
        _shipDetailContainer?.SetActive(false);
        if (_buildSlotContainer != null) _buildSlotContainer.SetActive(false);
        ResetPanelControls();

        if (showShipyard)
        {
            bool inDetail = selShip != null;

            _shipListContainer?  .SetActive(!inDetail);
            _shipDetailContainer?.SetActive(inDetail);

            if (inDetail)
            {
                // Dok je otvoren brod, sazetak brodogradilista i gumbi za radnike
                // samo trose visinu — ploca inace ne stane na ekran. Povratak je
                // jedan klik na "< Back".
                SetTitle(selShip.DisplayName);
                if (_workerText != null) _workerText.gameObject.SetActive(false);
                if (_outputText != null) _outputText.gameObject.SetActive(false);
                if (_assignBtn  != null) _assignBtn.gameObject.SetActive(false);
                if (_removeBtn  != null) _removeBtn.gameObject.SetActive(false);

                RefreshShipDetail(game, selShip);
            }
            else
            {
                SetTitle("Shipyard");
                SetWorkerLine($"Workers: {sel.AssignedWorkers}/{sel.MaxWorkers}" +
                    (sel.WorkersInside > 0 ? $" ({sel.WorkersInside} active)" : "") +
                    (sel.AssignedEngineers > 0
                        ? $"  | Eng: {sel.AssignedEngineers} +{(int)((sel.EngineerBonus - 1) * 100)}%"
                        : ""));

                // Fiksni trosak po brodu — radnici odreduju samo brzinu.
                string eta = sel.ShipHoursRemaining >= 0f ? $"{sel.ShipHoursRemaining:F1}h" : "—";
                SetOutputLine($"Cost: {ShipCostString()}  ({(sel.KeelLaid ? "paid" : "unpaid")})\n" +
                              $"Progress: {sel.ShipProgressPercent:F0}%   ETA: {eta}\n" +
                              $"Ships built: {sel.ShipCount}");

                SetButtons("+ Add worker",    () => game.AssignWorkerToSelectedBuilding(),
                           "- Remove worker", () => game.RemoveWorkerFromSelectedBuilding(),
                           sel.AssignedWorkers < sel.MaxWorkers && game.FreeWorkers > 0,
                           sel.AssignedWorkers > 0);
                SetEngButtons(sel.AssignedEngineers < sel.MaxEngineers && game.FreeEngineers > 0,
                              sel.AssignedEngineers > 0);
                SetBuildShipBtn(sel);

                RefreshShipList(game);
            }
        }
        else if (showTownHall)
        {
            RefreshTownHall(game);
        }
        else if (showSlot)
        {
            SetTitle("Under Construction");
            if (selSlot.State == BuildSlot.SlotState.UnderConstruction)
            {
                SetWorkerLine(BuildingFactory.Meta(selSlot.QueuedType).name);
                SetOutputLine($"Progress: {selSlot.ConstructionProgress * 100f:F0}%\n" +
                              $"Time left: {selSlot.HoursRemainingSmooth:F1}h");
            }
            else
            {
                SetWorkerLine("Empty plot");
                SetOutputLine("Use build panel [B] to place a building.");
            }
            SetButtons("", null, "", null, false, false);
            if (_assignBtn != null) _assignBtn.gameObject.SetActive(false);
            if (_removeBtn != null) _removeBtn.gameObject.SetActive(false);
        }
        else if (showBuilding)
        {
            string outputName = sel.BuildingTypeEnum == BuildingType.HuntersHut
                ? "Raw Food"
                : sel.OutputType.ToString();
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

            // Samo total/h. Stopa po radniku i "/day" su maknuti — "/day" je uz
            // 14-satni dan ionako bio racunat s 24 i prikazivao krivu brojku.
            string outLine = $"{outputName}/h: {sel.TotalOutputPerHour}";

            // Fiberworks isporucuje i Rope; bez ovog retka resurs je rastao bez
            // ijednog vidljivog izvora.
            if (sel.SecondaryOutputType.HasValue)
                outLine += $"\n{sel.SecondaryOutputType.Value}/h: {sel.SecondaryTotalPerHour}";

            SetOutputLine(outLine);

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
            _shipDetailContainer?.SetActive(true);
            if (_workerText != null) _workerText.gameObject.SetActive(false);
            if (_outputText != null) _outputText.gameObject.SetActive(false);
            if (_assignBtn  != null) _assignBtn.gameObject.SetActive(false);
            if (_removeBtn  != null) _removeBtn.gameObject.SetActive(false);
            RefreshShipDetail(game, selShip);
        }

        SetOptionalSpacingVisible(!showTownHall && !showSlot && !showShip);
    }

    private void SetOptionalSpacingVisible(bool visible)
    {
        if (_optionalSpacing == null) return;
        foreach (var element in _optionalSpacing)
            if (element != null && element.activeSelf != visible)
                element.SetActive(visible);
    }

    // -------------------------------------------------------
    // Town Hall — evidencija populacije
    // -------------------------------------------------------

    private void RefreshTownHall(GameController game)
    {
        SetTitle("Town Hall");

        // Svaka stavka je vlastiti redak. Neto, zaliha, ukrcani i evakuirani su
        // maknuti — zaliha stoji u gornjoj traci, a ostalo nije brojka po kojoj
        // igrac odlucuje na ovoj ploci.
        var lines = new List<string>
        {
            "<b>Population</b>",
            $"Souls on the island: {game.TotalPopulation}",
            $"Children: {game.Children}",
            $"Adults: {game.AdultPopulation}",
            $"Workers: {game.EmployedWorkers} working / {game.FreeWorkers} free",
            $"Engineers: {game.EmployedEngineers} working / {game.FreeEngineers} free",
            "",
            "<b>Food</b>",
            $"Produced: {game.FoodProducedPerDay:F0} / day",
            $"Consumed: {game.FoodConsumedPerDay:F0} / day",
        };

        string text = string.Join("\n", lines);

        // Town Hall koristi standardni tekst desnog panela. Visina se privremeno
        // poveca kako bi svi redci stali; ResetPanelControls() je vraca za druge
        // vrste selekcije.
        if (_workerText != null) _workerText.gameObject.SetActive(false);
        SetOutputExpanded(true, lines.Count);
        SetOutputLine(text);

        // Town Hall ne zaposljava — gumbi za radnike se skrivaju, a inzenjeri i
        // nadogradnja su vec ugaseni u ResetPanelControls().
        if (_assignBtn != null) _assignBtn.gameObject.SetActive(false);
        if (_removeBtn != null) _removeBtn.gameObject.SetActive(false);
    }

    // ---- Visina i omatanje retka izlaza ----
    // Town Hall treba znatno vise redaka od proizvodne zgrade. Izvorne vrijednosti
    // iz Inspectora se zapamte pri prvom koristenju i vrate cim se odabere nesto drugo.

    private LayoutElement     _outputLE;
    private float             _outputPrefH = -1f;
    private float             _outputMinH  = -1f;
    private TextWrappingModes _outputWrap  = TextWrappingModes.Normal;
    private bool              _outputDefaultsCached;

    private void CacheOutputDefaults()
    {
        if (_outputDefaultsCached || _outputText == null) return;
        _outputLE = _outputText.GetComponent<LayoutElement>();
        if (_outputLE != null)
        {
            _outputPrefH = _outputLE.preferredHeight;
            _outputMinH  = _outputLE.minHeight;
        }
        _outputWrap           = _outputText.textWrappingMode;
        _outputDefaultsCached = true;
    }

    private void SetOutputExpanded(bool expanded, int lineCount = 0)
    {
        if (_outputText == null) return;
        CacheOutputDefaults();

        if (expanded)
        {
            _outputText.textWrappingMode = TextWrappingModes.NoWrap;
            if (_outputLE != null)
            {
                float h = lineCount * _outputText.fontSize * 1.35f + 8f;
                _outputLE.preferredHeight = h;
                _outputLE.minHeight       = h;
            }
        }
        else
        {
            _outputText.textWrappingMode = _outputWrap;
            if (_outputLE != null && _outputPrefH >= 0f)
            {
                _outputLE.preferredHeight = _outputPrefH;
                _outputLE.minHeight       = _outputMinH;
            }
        }
    }

    // -------------------------------------------------------
    // Ships
    // -------------------------------------------------------

    private static string ShipCostString()
        => $"W:{BalanceConfig.ShipWoodCost}  S:{BalanceConfig.ShipSteelCost}" +
           $"  C:{BalanceConfig.ShipClothCost}  R:{BalanceConfig.ShipRopeCost}";

    private void RefreshShipList(GameController game)
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
                // Bez prefaba se klonira gumb iz ploce, pa redak izgleda kao
                // ostali gumbi umjesto kao tamna traka.
                btn = CloneStyledButton(_shipListContainer.transform, "ShipRow", "", 166, 22, 9f);
            }
            _shipRowBtns.Add(btn);
        }

        for (int i = 0; i < ships.Count; i++)
        {
            var ship = ships[i];
            var btn  = _shipRowBtns[i];
            if (btn == null) continue;

            // Boja se vise ne prepisuje — gumb zadrzi sprite i boje iz scene.
            // Odabrani redak se oznacava strelicom u natpisu.
            bool selected = game.GetSelectedShip() == ship;
            var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null)
                lbl.text = (selected ? "> " : "") +
                           $"{ship.DisplayName}  P: {ship.Passengers}/{ship.MaxPassengers}" +
                           $"  F: {ship.FoodLoaded}/{ship.RequiredFood}" +
                           (ship.IsReadyToSail ? "  *" : "");

            int idx = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => game.SelectShip(game.GetShips()[idx]));
        }
    }

    private void RefreshShipDetail(GameController game, ShipInstance ship)
    {
        if (ship == null) return;
        if (_shipDetailTitle != null) _shipDetailTitle.text = ship.DisplayName;
        // Kad je brod spreman, umjesto poruke stoji gumb Sail.
        bool ready = ship.IsReadyToSail && !ship.IsSailing;
        if (_shipDetailInfo != null)
        {
            _shipDetailInfo.text = ship.IsSailing ? "Sailing…" : "Not ready to sail";
            _shipDetailInfo.gameObject.SetActive(!ready);
        }
        if (_sailBtn != null) _sailBtn.gameObject.SetActive(ready);

        // Sve stare varijante gumba se gase — teret se vodi kroz dva kompaktna
        // retka "Passengers [+] [-]" i "Food [+] [-]". Prije je ista akcija
        // postojala u tri oblika (stari sailor gumbi, namjenski, pa jos "All").
        HideLegacyCargoButtons();
        EnsureCargoRows();

        bool canBoard = ship.FreeSpace > 0 && !ship.IsSailing
                     && (game.FreeWorkers > 0 || game.AvailableChildren > 0);

        if (_cargoRows != null) _cargoRows.SetActive(true);
        if (_passengerRowLabel != null)
            _passengerRowLabel.text = $"Passengers  {ship.Passengers} / {ship.MaxPassengers}";
        if (_foodRowLabel != null)
            _foodRowLabel.text = $"Food  {ship.FoodLoaded} / {ship.RequiredFood}";

        if (_passengerPlus  != null) _passengerPlus.interactable  = canBoard;
        if (_passengerMinus != null) _passengerMinus.interactable = ship.Passengers > 0;
        if (_foodPlus  != null) _foodPlus.interactable  = game.Food > 0 && ship.FoodMissing > 0;
        if (_foodMinus != null) _foodMinus.interactable = ship.FoodLoaded > 0;

        if (_fillShipBtn != null)
        {
            int foodFill = Mathf.Min(game.Food, ship.FoodMissing);
            int peopleFill = Mathf.Min(ship.FreeSpace, game.FreeWorkers + game.AvailableChildren);

            _fillShipBtn.interactable = foodFill > 0 || peopleFill > 0;
            SetLabel(_fillShipBtn, _fillShipBtn.interactable
                ? $"Fill ship  (+{peopleFill} ppl, +{foodFill} food)"
                : "Fill ship");
        }
    }

    // ---- Kompaktni redci tereta ----

    private GameObject      _cargoRows;
    private TextMeshProUGUI _passengerRowLabel, _foodRowLabel;
    private Button          _passengerPlus, _passengerMinus, _foodPlus, _foodMinus, _fillShipBtn, _sailBtn;

    private void HideLegacyCargoButtons()
    {
        if (_shipDetailAssign   != null) _shipDetailAssign.gameObject.SetActive(false);
        if (_shipDetailRemove   != null) _shipDetailRemove.gameObject.SetActive(false);
        if (_addPassengerBtn    != null) _addPassengerBtn.gameObject.SetActive(false);
        if (_removePassengerBtn != null) _removePassengerBtn.gameObject.SetActive(false);
        if (_boardAllBtn        != null) _boardAllBtn.gameObject.SetActive(false);
        if (_loadFoodBtn        != null) _loadFoodBtn.gameObject.SetActive(false);
        if (_unloadFoodBtn      != null) _unloadFoodBtn.gameObject.SetActive(false);
        if (_loadAllFoodBtn     != null) _loadAllFoodBtn.gameObject.SetActive(false);
    }

    private void EnsureCargoRows()
    {
        if (_cargoRows != null) return;

        Transform parent = _shipDetailContainer != null ? _shipDetailContainer.transform
                         : _rightPanel != null          ? _rightPanel.transform
                         : transform;

        _cargoRows = new GameObject("ShipCargoRows", typeof(RectTransform));
        _cargoRows.transform.SetParent(parent, false);
        // flexH: 0 — bez toga roditeljski VerticalLayoutGroup iz scene rastegne
        // retke po visini i gumbi prestanu biti kvadratici.
        LE(_cargoRows, prefH: 110, minH: 110, flexH: 0);
        var vg = _cargoRows.AddComponent<VerticalLayoutGroup>();
        vg.spacing = 4;
        vg.childControlWidth = vg.childControlHeight = vg.childForceExpandWidth = true;
        vg.childForceExpandHeight = false;

        int pStep = BalanceConfig.ShipPassengerLoadStep;

        MakeCargoRow(_cargoRows.transform, "Passengers",
            out _passengerRowLabel, out _passengerMinus, out _passengerPlus);
        _passengerMinus.onClick.AddListener(() => _game.RemovePassengersFromSelectedShip(pStep));
        _passengerPlus .onClick.AddListener(() => _game.AddPassengersToSelectedShip(pStep));

        MakeCargoRow(_cargoRows.transform, "Food",
            out _foodRowLabel, out _foodMinus, out _foodPlus);
        _foodMinus.onClick.AddListener(() => _game.UnloadFoodFromSelectedShip());
        _foodPlus .onClick.AddListener(() => _game.LoadFoodToSelectedShip());

        // Puni brod do kraja: prvo ljudi, pa hrana koliko je ima na skladistu.
        // Redoslijed je bitan — ukrcani putnici ne diraju zalihu hrane, pa hrana
        // koja ostane ide sva na brod.
        _fillShipBtn = CloneStyledButton(_cargoRows.transform, "FillShip", "Fill ship", 166, 24);
        _fillShipBtn.onClick.AddListener(() =>
        {
            _game.AddPassengersToSelectedShip(BalanceConfig.ShipMaxPassengers);
            _game.LoadAllFoodToSelectedShip();
        });

        // Isplovljavanje je odluka igraca — gumb se pojavi tek kad je brod spreman.
        _sailBtn = CloneStyledButton(_cargoRows.transform, "Sail", "Sail", 166, 24);
        _sailBtn.onClick.AddListener(() => _game.SailSelectedShip());
        _sailBtn.gameObject.SetActive(false);
    }

    /// <summary>Jedan redak: natpis lijevo, pa mala kvadratna gumba [-] [+].</summary>
    private void MakeCargoRow(Transform parent, string caption,
        out TextMeshProUGUI label, out Button minus, out Button plus)
    {
        var row = new GameObject($"Row_{caption}", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        LE(row, prefH: 24, minH: 24, flexH: 0);
        var hg = row.AddComponent<HorizontalLayoutGroup>();
        hg.spacing = 4;
        hg.childAlignment = TextAnchor.MiddleLeft;
        hg.childControlWidth = hg.childControlHeight = true;
        hg.childForceExpandWidth  = false;
        hg.childForceExpandHeight = false;   // inace gumbi narastu na visinu retka

        label = MakeTMP(row.transform, "Label", 10, TextAlignmentOptions.MidlineLeft);
        label.text  = caption;
        label.color = PanelInk;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        LE(label.gameObject, flexW: 1, minW: 60);

        // Redoslijed je [-] pa [+]: oduzimanje lijevo, dodavanje desno.
        minus = MakeStepBtn(row.transform, "Minus", _minusSprite, "-");
        plus  = MakeStepBtn(row.transform, "Plus",  _plusSprite,  "+");
    }

    /// <summary>
    /// Mali kvadratni gumb 20×20. Ako je ikona povucena u Inspectoru, koristi se
    /// ona; inace se ispisuje znak kao tekst, pa panel radi i bez grafike.
    /// </summary>
    private Button MakeStepBtn(Transform parent, string name, Sprite icon, string fallback)
    {
        var btn = CloneStyledButton(parent, name, icon != null ? "" : fallback, 24, 22, 12f);

        if (icon != null)
        {
            var img = btn.GetComponent<Image>();
            img.sprite = icon;
            img.color  = Color.white;
            img.type   = Image.Type.Sliced;   // rubovi ikone se ne rastezu
        }

        return btn;
    }

    private void RefreshSpeedButtons(int speed)
    {
        SaveSpeedSprites();
        SetSpeedSprite(_pauseBtn, _pauseNormalSprite, speed == 0);
        SetSpeedSprite(_speed1Btn, _speed1NormalSprite, speed == 1);
        SetSpeedSprite(_speed2Btn, _speed2NormalSprite, speed == 2);
        SetSpeedSprite(_speed3Btn, _speed3NormalSprite, speed == 5);
    }

    private void SaveSpeedSprites()
    {
        if (_speedSpritesSaved) return;

        _pauseNormalSprite = _pauseBtn?.image.sprite;
        _speed1NormalSprite = _speed1Btn?.image.sprite;
        _speed2NormalSprite = _speed2Btn?.image.sprite;
        _speed3NormalSprite = _speed3Btn?.image.sprite;
        _speedSpritesSaved = true;
    }

    private void SetSpeedSprite(Button button, Sprite normalSprite, bool active)
    {
        if (button == null) return;

        button.image.sprite = active ? _activeSpeedSprite : normalSprite;
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

    /// <summary>
    /// "Build ship" dok nema narudzbe, pa napredak dok se gradi. Gumb je ugasen
    /// za sve osim brodogradilista (ResetPanelControls).
    /// </summary>
    private void SetBuildShipBtn(BuildingInstance shipyard)
    {
        if (_buildShipBtn == null) return;

        _buildShipBtn.gameObject.SetActive(true);

        if (!shipyard.ShipOrdered)
        {
            bool canOrder = shipyard.CanAffordShip;
            _buildShipBtn.interactable = canOrder;
            SetLabel(_buildShipBtn, canOrder ? "Build ship" : "Build ship (not enough resources)");
            return;
        }

        _buildShipBtn.interactable = false;
        SetLabel(_buildShipBtn, shipyard.KeelLaid
            ? $"Building… {shipyard.ShipProgressPercent:F0}%"
            : "Ordered — waiting for materials");
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
        if (_assignEngBtn != null)
        {
            _assignEngBtn.gameObject.SetActive(true);
            _assignEngBtn.interactable = assignEnabled;
        }
        if (_removeEngBtn != null)
        {
            _removeEngBtn.gameObject.SetActive(true);
            _removeEngBtn.interactable = removeEnabled;
        }
    }

    /// <summary>
    /// Gumb iz scene koji druge ploce koriste kao predlozak stila (SaveLoadPanel).
    /// Null dok UIController nije povezan u Inspectoru.
    /// </summary>
    public Button StyleTemplate => _assignBtn;

    /// <summary>
    /// Sprite pozadine desne ploce, da druge ploce ne moraju imati vlastitu
    /// referencu na istu grafiku. Null ako ploca nema sprite.
    /// </summary>
    public Sprite PanelBackground
    {
        get
        {
            if (_rightPanel == null) return null;
            var img = _rightPanel.GetComponent<Image>();
            return img != null ? img.sprite : null;
        }
    }

    /// <summary>
    /// Novi gumb koji izgleda kao ostali u ploci: klonira se postojeci gumb iz
    /// scene (_assignBtn) pa preuzme njegov sprite, boje i font. Bez toga su
    /// gumbi gradeni iz koda imali ravnu tamnu podlogu i strsili su medu
    /// devetodijelnim sprajtovima iz scene.
    /// </summary>
    private Button CloneStyledButton(Transform parent, string name, string label,
        float w, float h, float fontSize = 0f)
    {
        Button btn;

        if (_assignBtn != null)
        {
            var go = Instantiate(_assignBtn.gameObject, parent);
            go.name = $"Btn_{name}";
            go.SetActive(true);
            btn = go.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
        }
        else
        {
            btn = MakeBtn(parent, label, null, w, h);   // fallback bez scene
            btn.name = $"Btn_{name}";
        }

        SetLabel(btn, label);
        if (fontSize > 0f)
        {
            var t = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.fontSize = fontSize;
        }

        LE(btn.gameObject, prefW: w, minW: w, prefH: h, minH: h, flexW: 0, flexH: 0);
        return btn;
    }

    private static void SetLabel(Button btn, string text)
    {
        var t = btn?.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null) t.text = text;
    }

    // -------------------------------------------------------
    // BuildFromCode — fallback until Mislav sets up scene UI
    // Creates all UI elements programmatically (old approach).
    // Remove this when Inspector references are wired.
    // -------------------------------------------------------

    public void BuildFromCode(GameController game, Transform canvasTransform, Camera cam)
    {
        _game       = game;
        _mainCamera = cam;

        BuildTopBarFromCode(canvasTransform);
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
        _speed3Btn?.onClick.AddListener(() => _game.SetSpeed(5));
        _assignBtn?   .onClick.AddListener(() => _game.AssignWorkerToSelectedBuilding());
        _removeBtn?   .onClick.AddListener(() => _game.RemoveWorkerFromSelectedBuilding());
        _assignEngBtn?.onClick.AddListener(() => _game.AssignEngineerToSelectedBuilding());
        _removeEngBtn?.onClick.AddListener(() => _game.RemoveEngineerFromSelectedBuilding());
        _upgradeBtn?  .onClick.AddListener(() => _game.TryUpgradeSelectedBuilding());

        WireShipButtons();

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
        _workerText.textWrappingMode = TextWrappingModes.Normal;
        LE(_workerText.gameObject, prefH:28, minH:14);
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
        LE(sd, prefH:200, minH:20);
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

        // Redci tereta (Passengers / Food) gradi EnsureCargoRows() pri prvom prikazu.

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
