using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
    [SerializeField] private TextMeshProUGUI _resourcesText;  // ikone + iznosi, lijevi dio trake
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

    // Otvara Manage fleet plocu s popisom brodova. Ako nije povucen u
    // Inspectoru, klonira se iz _assignBtn i smjesta odmah ispod Build ship.
    [SerializeField] private Button          _manageFleetBtn;

    // Otkazivanje gradnje i rusenje. Ako nisu povuceni u Inspectoru, kloniraju
    // se iz _assignBtn kao i Manage fleet, pa scena ne treba nikakvu izmjenu.
    [SerializeField] private Button          _cancelBtn;
    [SerializeField] private Button          _demolishBtn;

    [SerializeField] private GameObject      _titleSeparator;
    [SerializeField] private GameObject      _actionSeparator;
    [SerializeField] private GameObject      _footerSeparator;

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
        // Samo ako su povuceni u Inspectoru — klonirani gumbi dobiju vezu pri
        // stvaranju, pa bi ovdje dobili drugu i okinuli se dvaput po kliku.
        _cancelBtn?   .onClick.AddListener(() => _game.CancelSelectedSlot());
        _demolishBtn? .onClick.AddListener(() => _game.DemolishSelectedBuilding());
        _buildShipBtn?.onClick.AddListener(() => _game.OrderShipOnSelectedBuilding());
        _manageFleetBtn?.onClick.AddListener(OpenFleet);

        WireShipButtons();

        // Initialize sub-panels
        _buildPanel?   .Build(game, GetComponentInParent<Canvas>()?.transform ?? transform, cam);
        _saveLoadPanel?.Build(game, GetComponentInParent<Canvas>()?.transform ?? transform);

        if (_tutorialBtn != null)
        {
            _tutorialBtn.onClick.RemoveAllListeners();
            _tutorialBtn.onClick.AddListener(ToggleTutorialPanel);
        }

        NormalizeRightPanelSize();

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
        // "< Back" se povezuje kontekstno u SetBackBtn, jer u ploci flote vodi
        // na popis, a iz popisa natrag na brodogradiliste.
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
        if (_workerText      != null) _workerText.color      = PanelInk;
        if (_outputText      != null) _outputText.color      = PanelInk;
        if (_shipDetailTitle != null) _shipDetailTitle.color = PanelInk;
        if (_shipDetailInfo  != null) _shipDetailInfo.color  = PanelInkSoft;
    }

    private void RefreshTopBar(GameController game)
    {
        if (_resourcesText != null)
            _resourcesText.text =
                $"{ResourceIcons.Wood} {game.Wood}   {ResourceIcons.Steel} {game.Steel}" +
                $"   {ResourceIcons.Cloth} {game.Cloth}   {ResourceIcons.Rope} {game.Rope}" +
                $"   {ResourceIcons.Ships} {game.Ships}";

        if (_clockText != null)
            _clockText.text = $"Day {game.Day}  {game.Hour:00}:{game.Minute:00}";

        if (_resourcesRightText != null)
            _resourcesRightText.text =
                $"{ResourceIcons.RawFood} {game.RawFood}   {ResourceIcons.Food} {game.Food}";
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
        if (_titleText       != null) _titleText.gameObject.SetActive(true);
        if (_assignEngBtn    != null) _assignEngBtn.gameObject.SetActive(false);
        if (_removeEngBtn    != null) _removeEngBtn.gameObject.SetActive(false);
        if (_upgradeBtn      != null) _upgradeBtn.gameObject.SetActive(false);
        if (_cancelBtn       != null) _cancelBtn.gameObject.SetActive(false);
        if (_demolishBtn     != null) _demolishBtn.gameObject.SetActive(false);
        if (_buildShipBtn    != null) _buildShipBtn.gameObject.SetActive(false);
        if (_manageFleetBtn  != null) _manageFleetBtn.gameObject.SetActive(false);
        if (_shipDetailBack  != null) _shipDetailBack.gameObject.SetActive(false);
        if (_evacuateBtn     != null) _evacuateBtn.gameObject.SetActive(false);
        if (_evacuateTopGap  != null) _evacuateTopGap.SetActive(false);
        if (_evacuateBottomGap != null) _evacuateBottomGap.SetActive(false);
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

    // -------------------------------------------------------
    // Manage fleet — stanje ploce
    // -------------------------------------------------------

    /// <summary>
    /// True dok je otvorena ploca flote. Cisto stanje sucelja — GameController
    /// ne zna za nju, pa se ovaj podatak namjerno ne sprema u igru.
    /// </summary>
    private bool             _fleetOpen;
    private BuildingInstance _lastSelBuilding;

    private void OpenFleet()
    {
        _fleetOpen = true;
        RefreshCurrentRightPanel();
    }

    /// <summary>Zatvara flotu i vraca se na plocu brodogradilista.</summary>
    private void CloseFleet()
    {
        _fleetOpen = false;
        if (_game.GetSelectedShip() != null)
            _game.SelectShip(null);
        else
            RefreshCurrentRightPanel();
    }

    private void RefreshCurrentRightPanel()
    {
        if (_game == null) return;
        RefreshRightPanel(_game, _game.GetSelectedBuilding(),
                          _game.GetSelectedShip(), _game.GetSelectedSlot());
    }

    public bool IsShowingShipyardSubmenu =>
        _fleetOpen || (_game != null && _game.GetSelectedShip() != null);

    public void ShowMainShipyardPanel()
    {
        _fleetOpen = false;
        if (_game == null) return;

        if (_game.GetSelectedShip() != null)
            _game.SelectShip(null);
        else
            RefreshCurrentRightPanel();
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

        // Promjena odabrane gradevine zatvara flotu, da se ploca flote ne
        // pojavi nad nekom drugom zgradom.
        if (sel != _lastSelBuilding)
        {
            _lastSelBuilding = sel;
            _fleetOpen = false;
        }

        // Klik na brod u svijetu otvara flotu izravno, bez prolaska kroz
        // brodogradiliste.
        if (showShip) _fleetOpen = true;

        _rightPanel?.SetActive(anySelected);
        if (!anySelected)
        {
            _shipListContainer?  .SetActive(false);
            _shipDetailContainer?.SetActive(false);
            _fleetOpen = false;
            _game.SelectShip(null);
            return;
        }

        // Reset all sub-containers and optional controls
        _shipListContainer?  .SetActive(false);
        _shipDetailContainer?.SetActive(false);
        if (_buildSlotContainer != null) _buildSlotContainer.SetActive(false);
        ResetPanelControls();

        // Ploca flote ima prednost: otvorena je ili preko gumba Manage fleet,
        // ili klikom na brod u svijetu.
        bool fleetMode = showShip || (showShipyard && _fleetOpen);

        if (fleetMode)
        {
            RefreshFleetPanel(game, selShip, showShipyard);
        }
        else if (showShipyard)
        {
            SetTitle("Shipyard");
            SetWorkerLine($"Workers: {sel.AssignedWorkers}/{sel.MaxWorkers}" +
                (sel.WorkersInside > 0 ? $" ({sel.WorkersInside} active)" : "") +
                (sel.AssignedEngineers > 0
                    ? $"  | Eng: {sel.AssignedEngineers} +{(int)((sel.EngineerBonus - 1) * 100)}%"
                    : ""));

            SetOutputLine($"Ship cost: {ShipCostString()}");

            SetButtons("+ Add worker",    () => game.AssignWorkerToSelectedBuilding(),
                       "- Remove worker", () => game.RemoveWorkerFromSelectedBuilding(),
                       sel.AssignedWorkers < sel.MaxWorkers && game.FreeWorkers > 0,
                       sel.AssignedWorkers > 0);
            SetEngButtons(sel.AssignedEngineers < sel.MaxEngineers && game.FreeEngineers > 0,
                          sel.AssignedEngineers > 0);
            SetBuildShipBtn(sel);
            SetManageFleetBtn(game);
            SetDemolishBtn(sel);
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

            if (selSlot.State == BuildSlot.SlotState.UnderConstruction)
                SetCancelBtn();
        }
        else if (showBuilding)
        {
            // Hunter's Hut ima OutputType Food, ali isporucuje sirovu hranu.
            string outputIcon = sel.BuildingTypeEnum == BuildingType.HuntersHut
                ? ResourceIcons.RawFood
                : ResourceIcons.Tag(sel.OutputType);
            SetTitle(sel.DisplayName);
            {
                string engStr = sel.AssignedEngineers > 0
                    ? $"  | Eng: {sel.AssignedEngineers} +{(int)((sel.EngineerBonus-1)*100)}%"
                    : "";
                SetWorkerLine($"Workers: {sel.AssignedWorkers}/{sel.MaxWorkers}" +
                    (sel.WorkersInside > 0 ? $" ({sel.WorkersInside} active)" : "") +
                    engStr);
            }

            string outLine = $"{outputIcon} {sel.TotalOutputPerHour} /h";
            if (sel.SecondaryOutputType.HasValue)
                outLine += $"\n{ResourceIcons.Tag(sel.SecondaryOutputType.Value)} " +
                           $"{sel.SecondaryTotalPerHour} /h";
            SetOutputLine(outLine);

            SetButtons("+ Add worker",    () => game.AssignWorkerToSelectedBuilding(),
                       "- Remove worker", () => game.RemoveWorkerFromSelectedBuilding(),
                       sel.AssignedWorkers < sel.MaxWorkers && game.FreeWorkers > 0,
                       sel.AssignedWorkers > 0);
            SetEngButtons(sel.AssignedEngineers < sel.MaxEngineers && game.FreeEngineers > 0,
                          sel.AssignedEngineers > 0);
            SetUpgradeBtn(true, game.CanAffordUpgrade(sel));
            SetDemolishBtn(sel);
        }

        if (_titleSeparator != null) _titleSeparator.SetActive(!showShip);
        if (_actionSeparator != null)
            _actionSeparator.SetActive(!showTownHall && !showSlot && !fleetMode);
        if (_footerSeparator != null)
            _footerSeparator.SetActive(showTownHall || (showShipyard && !fleetMode));
    }

    // -------------------------------------------------------
    // Manage fleet — popis brodova i pojedini brod
    // -------------------------------------------------------

    /// <summary>
    /// Bez odabranog broda prikazuje popis flote; s odabranim brodom prikazuje
    /// njegov teret. Gumbi za radnike i inzenjere su ovdje ugaseni
    /// jer pripadaju brodogradilistu, ne floti.
    /// </summary>
    private void RefreshFleetPanel(GameController game, ShipInstance selShip, bool fromShipyard)
    {
        if (_workerText != null) _workerText.gameObject.SetActive(false);
        if (_assignBtn  != null) _assignBtn.gameObject.SetActive(false);
        if (_removeBtn  != null) _removeBtn.gameObject.SetActive(false);

        bool inDetail = selShip != null;

        _shipListContainer?  .SetActive(!inDetail);
        _shipDetailContainer?.SetActive(inDetail);

        if (inDetail)
        {
            if (_titleText != null) _titleText.gameObject.SetActive(false);
            if (_outputText != null) _outputText.gameObject.SetActive(false);
            RefreshShipDetail(game, selShip);
            // Natrag na popis flote.
            SetBackBtn("< Back to fleet", () => _game.SelectShip(null));
        }
        else
        {
            var ships = game.GetShips();
            int ready = game.ReadyShipCount;

            SetTitle("Manage fleet");
            SetOutputLine(ships.Count == 0
                ? "No ships yet. Order one in the Shipyard."
                : $"Ships in port: {ships.Count}\nReady for evacuation: {ready}");

            RefreshShipList(game);
            // Iz flote natrag na brodogradiliste, ako smo dosli odande.
            SetBackBtn(fromShipyard ? "< Back to Shipyard" : "< Close",
                       fromShipyard ? (UnityEngine.Events.UnityAction)CloseFleet
                                    : () => { _fleetOpen = false; _game.SelectShip(null); _game.SelectBuilding(null); });
        }
    }

    private bool _backBtnMoved;

    /// <summary>
    /// Gumb za povratak; koristi postojeci _shipDetailBack iz scene.
    ///
    /// U sceni taj gumb stoji UNUTAR kontejnera detalja broda, pa bi nestao cim
    /// se prikaze popis flote. Zato se jednom premjesta u desnu plocu, odmah
    /// iznad popisa — zadrzava svoj sprite, font i velicinu iz scene, a postaje
    /// vidljiv u oba prikaza flote.
    /// </summary>
    private void SetBackBtn(string label, UnityEngine.Events.UnityAction cb)
    {
        if (_shipDetailBack == null) return;

        if (!_backBtnMoved && _rightPanel != null)
        {
            _backBtnMoved = true;
            _shipDetailBack.transform.SetParent(_rightPanel.transform, false);
            if (_shipListContainer != null)
                _shipDetailBack.transform.SetSiblingIndex(
                    _shipListContainer.transform.GetSiblingIndex());
        }

        _shipDetailBack.gameObject.SetActive(true);
        _shipDetailBack.interactable = true;
        _shipDetailBack.onClick.RemoveAllListeners();
        _shipDetailBack.onClick.AddListener(cb);
        SetLabel(_shipDetailBack, label);
    }

    private Button     _evacuateBtn;
    private GameObject _evacuateTopGap;
    private GameObject _evacuateBottomGap;
    private bool       _evacuationInProgress;

    /// <summary>
    /// Zavrsna evakuacija iz Town Halla. Salje SVE spremne brodove odjednom;
    /// oni zatim krecu jedan za drugim i razilaze se u lepezu.
    /// Stvara se pri prvom prikazu i klonira _assignBtn, pa preuzima sprite,
    /// font i visinu ostalih gumba ploce.
    /// </summary>
    private void SetEvacuateBtn(GameController game)
    {
        if (_evacuateBtn == null)
        {
            Transform parent = _rightPanel != null ? _rightPanel.transform : transform;
            _evacuateTopGap = Gap(parent, 4f, "EvacuateGapTop");
            _evacuateBtn = CloneStyledButton(parent, "Evacuate", "EVACUATE");
            _evacuateBtn.onClick.AddListener(BeginEvacuation);
            _evacuateBottomGap = Gap(parent, 4f, "EvacuateGapBottom");
        }

        if (_footerSeparator != null)
        {
            _evacuateTopGap.transform.SetSiblingIndex(_footerSeparator.transform.GetSiblingIndex() + 1);
            _evacuateBtn.transform.SetSiblingIndex(_evacuateTopGap.transform.GetSiblingIndex() + 1);
            _evacuateBottomGap.transform.SetSiblingIndex(_evacuateBtn.transform.GetSiblingIndex() + 1);
        }

        _evacuateTopGap.SetActive(true);
        _evacuateBtn.gameObject.SetActive(true);
        _evacuateBottomGap.SetActive(true);
        _evacuateBtn.interactable = !_evacuationInProgress && game.ReadyShipCount > 0;
        SetLabel(_evacuateBtn, "EVACUATE");
    }

    private void BeginEvacuation()
    {
        if (_evacuationInProgress || _game == null) return;

        var departingShips = new List<ShipInstance>();
        foreach (var ship in _game.GetShips())
            if (ship != null && !ship.IsSailing && ship.IsReadyToSail)
                departingShips.Add(ship);

        if (departingShips.Count == 0 || _game.SailAllReadyShips() == 0) return;

        _evacuationInProgress = true;
        if (_evacuateBtn != null) _evacuateBtn.interactable = false;
        StartCoroutine(LoadEndSceneAfterDeparture(departingShips));
    }

    private static IEnumerator LoadEndSceneAfterDeparture(List<ShipInstance> departingShips)
    {
        bool shipsStillVisible;
        do
        {
            shipsStillVisible = false;
            foreach (var ship in departingShips)
            {
                if (ship == null || ship.HasLeft) continue;
                shipsStillVisible = true;
                break;
            }
            if (shipsStillVisible) yield return null;
        }
        while (shipsStillVisible);

        SceneManager.LoadScene("TheEnd");
    }

    /// <summary>
    /// "Manage fleet" u ploci brodogradilista. Gumb se stvara tek pri prvom
    /// prikazu i klonira _assignBtn, pa preuzme sprite, font i visinu ostalih
    /// gumba ploce.
    /// </summary>
    private void SetManageFleetBtn(GameController game)
    {
        if (_manageFleetBtn == null)
        {
            Transform parent = _rightPanel != null ? _rightPanel.transform : transform;
            _manageFleetBtn = CloneStyledButton(parent, "ManageFleet", "Manage fleet");
            _manageFleetBtn.onClick.AddListener(OpenFleet);

            // Odmah ispod gumba Build ship, da red gumba ostane logican.
            if (_buildShipBtn != null)
                _manageFleetBtn.transform.SetSiblingIndex(
                    _buildShipBtn.transform.GetSiblingIndex() + 1);
        }

        int count = game.GetShips().Count;
        if (_buildShipBtn != null)
            _manageFleetBtn.transform.SetSiblingIndex(_buildShipBtn.transform.GetSiblingIndex() + 1);

        _manageFleetBtn.gameObject.SetActive(true);
        _manageFleetBtn.interactable = count > 0;
        SetLabel(_manageFleetBtn, count > 0 ? $"Manage fleet ({count})" : "Manage fleet (no ships)");
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
            $"Produced: {ResourceIcons.Food} {game.FoodProducedPerDay:F0} / day",
            $"Consumed: {ResourceIcons.Food} {game.FoodConsumedPerDay:F0} / day",
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
        SetEvacuateBtn(game);
    }

    // ---- Visina i omatanje retka izlaza ----
    // Town Hall treba znatno vise redaka od proizvodne zgrade. Izvorne vrijednosti
    // iz Inspectora se zapamte pri prvom koristenju i vrate cim se odabere nesto drugo.

    private LayoutElement     _outputLE;
    private float             _outputPrefH = -1f;
    private float             _outputMinH  = -1f;
    private TextWrappingModes _outputWrap  = TextWrappingModes.Normal;
    private bool              _outputDefaultsCached;
    private bool              _outputExpanded;

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
        _outputExpanded = expanded;

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
        => $"{ResourceIcons.Wood} {BalanceConfig.ShipWoodCost}  " +
           $"{ResourceIcons.Steel} {BalanceConfig.ShipSteelCost}  " +
           $"{ResourceIcons.Cloth} {BalanceConfig.ShipClothCost}  " +
           $"{ResourceIcons.Rope} {BalanceConfig.ShipRopeCost}";

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
                btn = CloneStyledButton(_shipListContainer.transform, "ShipRow", "");
            }
            _shipRowBtns.Add(btn);
        }

        for (int i = 0; i < ships.Count; i++)
        {
            var ship = ships[i];
            var btn  = _shipRowBtns[i];
            if (btn == null) continue;

            // Boja se vise ne prepisuje — gumb zadrzi sprite i boje iz scene.
            // Redak nosi samo stanje; brojke putnika i hrane stoje u ploci
            // pojedinog broda, jedan klik dalje.
            //
            // SAILING je zaseban slucaj, a ne "ready": brod koji je vec otplovio
            // i dalje zadovoljava IsReadyToSail, pa bi inace pisalo READY.
            string state = ship.IsSailing     ? "SAILING"
                         : ship.IsReadyToSail ? "READY"
                                              : "NOT READY";
            var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null)
                lbl.text = $"{ship.DisplayName}   {state}";

            // Hvata se sam brod, ne indeks: brod moze otploviti i nestati iz
            // popisa izmedu vezanja i klika, pa bi indeks promasio.
            var target = ship;
            btn.interactable = true;   // redak je uvijek klikabilan
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => game.SelectShip(target));
        }
    }

    private void RefreshShipDetail(GameController game, ShipInstance ship)
    {
        if (ship == null) return;
        if (_shipDetailTitle != null) _shipDetailTitle.text = ship.DisplayName;
        // Status je samo tekst. Evakuacija je zavrsna radnja u Town Hallu.
        if (_shipDetailInfo != null)
        {
            _shipDetailInfo.gameObject.SetActive(true);
            _shipDetailInfo.text = ship.IsSailing      ? "Sailing…"
                                 : ship.IsReadyToSail  ? "Ready for evacuation"
                                                       : "Not ready";
        }

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
            _foodRowLabel.text =
                $"{ResourceIcons.Food}  {ship.FoodLoaded} / {ship.RequiredFood}";

        if (_passengerPlus  != null) _passengerPlus.interactable  = canBoard;
        if (_passengerMinus != null) _passengerMinus.interactable = ship.Passengers > 0;
        if (_foodPlus  != null) _foodPlus.interactable  = game.Food > 0 && ship.FoodMissing > 0;
        if (_foodMinus != null) _foodMinus.interactable = ship.FoodLoaded > 0;

        if (_fillShipBtn != null)
        {
            bool canFillFood = game.Food > 0 && ship.FoodMissing > 0;
            _fillShipBtn.interactable = canBoard || canFillFood;
            SetLabel(_fillShipBtn, "Fill Ship");
        }
    }

    // ---- Kompaktni redci tereta ----

    private GameObject      _cargoRows;
    private TextMeshProUGUI _passengerRowLabel, _foodRowLabel;
    private Button          _passengerPlus, _passengerMinus, _foodPlus, _foodMinus, _fillShipBtn;

    private void HideLegacyCargoButtons()
    {
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
        Gap(_cargoRows.transform, 7f, "FillShipGap");
        _fillShipBtn = CloneStyledButton(_cargoRows.transform, "FillShip", "Fill Ship");
        LE(_fillShipBtn.gameObject, prefH: 26f, minH: 26f);
        _fillShipBtn.onClick.AddListener(() =>
        {
            _game.AddPassengersToSelectedShip(BalanceConfig.ShipMaxPassengers);
            _game.LoadAllFoodToSelectedShip();
        });

        // Evakuacija cijele flote pokrece se iz Town Halla.
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
    /// Mali gumb za korak. Sirina je fiksna, a visina, font i sprite dolaze iz
    /// predloska, pa se uklapa medu ostale gumbe ploce. Ako je ikona povucena u
    /// Inspectoru koristi se ona; inace se ispisuje znak kao tekst.
    /// </summary>
    private Button MakeStepBtn(Transform parent, string name, Sprite icon, string fallback)
    {
        var btn = CloneStyledButton(parent, name, icon != null ? "" : fallback, squareWidth: 24f);

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

    private void SetOutputLine(string text)
    {
        if (_outputText == null) return;
        _outputText.text = text;
        if (_outputExpanded) return;

        CacheOutputDefaults();
        if (_outputLE == null) return;

        int lineCount = 1;
        for (int i = 0; i < text.Length; i++)
            if (text[i] == '\n') lineCount++;

        float height = Mathf.Max(16f, lineCount * _outputText.fontSize * 1.2f + 4f);
        _outputLE.preferredHeight = height;
        _outputLE.minHeight = height;
    }

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

        if (_footerSeparator != null)
            _buildShipBtn.transform.SetSiblingIndex(_footerSeparator.transform.GetSiblingIndex() + 1);

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

    // -------------------------------------------------------
    // Otkazivanje gradnje i rusenje
    // -------------------------------------------------------

    private void SetCancelBtn()
    {
        if (_cancelBtn == null)
        {
            Transform parent = _rightPanel != null ? _rightPanel.transform : transform;
            _cancelBtn = CloneStyledButton(parent, "CancelBuild", "Cancel");
            _cancelBtn.onClick.AddListener(() => _game.CancelSelectedSlot());
        }

        _cancelBtn.gameObject.SetActive(true);
        _cancelBtn.interactable = true;
        SetLabel(_cancelBtn, "Cancel");
        MoveToBottom(_cancelBtn);
    }

    private void SetDemolishBtn(BuildingInstance building)
    {
        if (_demolishBtn == null)
        {
            Transform parent = _rightPanel != null ? _rightPanel.transform : transform;
            _demolishBtn = CloneStyledButton(parent, "Demolish", "Demolish");
            _demolishBtn.onClick.AddListener(() => _game.DemolishSelectedBuilding());
        }

        // Town Hall se ne rusi — bez njega nema evidencije populacije.
        if (building == null || building.IsTownHall)
        {
            _demolishBtn.gameObject.SetActive(false);
            return;
        }

        _demolishBtn.gameObject.SetActive(true);
        _demolishBtn.interactable = true;
        SetLabel(_demolishBtn, "Demolish");
        MoveToBottom(_demolishBtn);
    }

    /// <summary>
    /// Gura gumb na dno desneploce. Provjera indeksa je tu jer se Refresh vrti
    /// svaki okvir, a bezuvjetni SetAsLastSibling bi svaki put prljao layout.
    /// </summary>
    private static void MoveToBottom(Button btn)
    {
        if (btn == null) return;
        var t = btn.transform;
        if (t.parent == null) return;
        int last = t.parent.childCount - 1;
        if (t.GetSiblingIndex() != last) t.SetAsLastSibling();
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
        float squareWidth = 0f)
    {
        Button btn;

        if (_assignBtn != null)
        {
            var go = Instantiate(_assignBtn.gameObject, parent);
            go.name = $"Btn_{name}";
            go.SetActive(true);
            btn = go.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();

            // Predlozak je cesto onemogucen (npr. "+ Add worker" kad je
            // brodogradiliste puno), a klon nasljeduje to stanje i nikad se ne
            // okine. Svaki novi gumb zato krece kao klikabilan; tko treba,
            // postavi interactable poslije.
            btn.interactable = true;

            // RemoveAllListeners brise samo veze dodane iz koda. Ako je predlozak
            // dobio vezu i u Inspectoru, klon bi ju naslijedio — pa bi "Manage
            // fleet" usput dodavao radnika. Zato se gase i trajne veze.
            for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
                btn.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);

            // Font, sprite, boje i LayoutElement NAMJERNO se ne diraju — klon
            // nosi tocno onaj izgled koji predlozak ima u sceni, pa svi gumbi
            // desne ploce ostaju jednaki bez obzira gdje su stvoreni.
        }
        else
        {
            // Fallback bez scene: samo tada se velicina zadaje iz koda.
            btn = MakeBtn(parent, label, null, squareWidth > 0f ? squareWidth : 166f, 24f);
            btn.name = $"Btn_{name}";
        }

        SetLabel(btn, label);

        // Jedina dopustena iznimka: mali kvadratni gumbi + i -, kojima se zadaje
        // samo sirina. Visina i font i dalje dolaze iz predloska.
        if (squareWidth > 0f)
        {
            var le = btn.GetComponent<LayoutElement>() ?? btn.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = squareWidth;
            le.minWidth       = squareWidth;
            le.flexibleWidth  = 0f;
        }

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

        NormalizeRightPanelSize();

        _rightPanel?.SetActive(false);
    }

    /// <summary>
    /// Kontejneri popisa i detalja broda imali su fiksnu zadanu visinu
    /// (120 i 160) koja nije pratila stvarni sadrzaj, pa su redci tereta i gumb
    /// Fill ship ispadali izvan ploce. -1 znaci "ne namecem visinu", pa ih
    /// VerticalLayoutGroup izmjeri po sadrzaju, a ContentSizeFitter zatim
    /// prilagodi visinu cijelog RightPanela.
    /// </summary>
    private void NormalizeRightPanelSize()
    {
        if (_workerText != null)
        {
            _workerText.enableAutoSizing = true;
            _workerText.fontSizeMin = 7f;
            _workerText.fontSizeMax = 10f;
            _workerText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        WrapHeightToContent(_shipListContainer);
        WrapHeightToContent(_shipDetailContainer);
    }

    private static void WrapHeightToContent(GameObject go)
    {
        if (go == null) return;
        var le = go.GetComponent<LayoutElement>();
        if (le == null) return;
        le.preferredHeight = -1f;
        le.minHeight       = -1f;
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
        _workerText.color = PanelInk;
        _workerText.textWrappingMode = TextWrappingModes.Normal;
        LE(_workerText.gameObject, prefH:28, minH:14);
        _outputText = MakeTMP(rp.transform, "Output", 10, TextAlignmentOptions.TopLeft);
        _outputText.color = PanelInk;
        _outputText.textWrappingMode = TextWrappingModes.Normal;
        LE(_outputText.gameObject, prefH:30, minH:30);
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
        LE(_shipDetailInfo.gameObject, prefH:18, minH:18);
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
        go.AddComponent<Image>().color = new Color(0.28f, 0.28f, 0.28f, 0.8f);
        LE(go, prefH: 1f, minH: 1f);
    }

    private GameObject Gap(Transform parent, float height, string name = "Gap")
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        LE(go, prefH:height, minH:height);
        return go;
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
