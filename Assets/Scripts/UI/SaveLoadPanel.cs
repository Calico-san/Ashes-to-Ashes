using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveLoadPanel : MonoBehaviour
{
    // Predlozak stila. Povuci bilo koji gotov gumb iz scene (npr. Btn_Add worker)
    // i svi gumbi ove ploce preuzet ce njegov sprite, boje i font. Ako ostane
    // prazno, gradi se stari ravni tamni gumb iz koda.
    [Header("Izgled")]
    [SerializeField] private Button _buttonTemplate;
    [SerializeField] private Sprite _panelSprite;   // neobavezno, 9-slice pozadina
    [SerializeField] private float  _panelPixelsPerUnitMultiplier = 3f;

    private float PanelPixelsPerUnitMultiplier =>
        _panelPixelsPerUnitMultiplier > 0f ? _panelPixelsPerUnitMultiplier : 1f;

    /// <summary>Jedinstvena boja teksta: #1B0D00.</summary>
    private static readonly Color Ink     = new Color32(0x1B, 0x0D, 0x00, 0xFF);
    private static readonly Color InkSoft = Ink;

    private GameObject              _overlay;
    private bool                    _open;
    private GameController _game;

    private readonly TextMeshProUGUI[] _saveLabels = new TextMeshProUGUI[SaveSystem.MaxSlots];
    private readonly TextMeshProUGUI[] _loadLabels = new TextMeshProUGUI[SaveSystem.MaxSlots];
    private readonly Button[]          _loadBtns   = new Button[SaveSystem.MaxSlots];

    private TextMeshProUGUI _feedbackText;
    private float           _feedbackTimer;
    private const float     FEEDBACK_DURATION = 2.5f;

    public void Build(GameController game, Transform canvas)
    {
        _game = game;
        ResolveButtonTemplate();
        BuildOverlay(canvas);
        _overlay.SetActive(false);
    }

    /// <summary>
    /// Ako predlozak nije povucen u Inspectoru, uzima se gumb desne ploce preko
    /// UIControllera. Bez toga bi ploca ostala u starom ravnom stilu samo zato
    /// sto je scena spremljena prije nego je polje uopce postojalo — komponenta
    /// tada u sceni nema nijednu serijaliziranu vrijednost.
    /// </summary>
    private void ResolveButtonTemplate()
    {
        if (_buttonTemplate != null && _panelSprite != null) return;

        var ui = FindFirstObjectByType<UIController>();
        if (ui != null)
        {
            if (_buttonTemplate == null) _buttonTemplate = ui.StyleTemplate;
            if (_panelSprite    == null) _panelSprite    = ui.PanelBackground;
        }

        if (_buttonTemplate == null)
            Debug.LogWarning("[SaveLoadPanel] Nema predloska gumba — ploca koristi stari izgled iz koda.", this);
    }

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();

        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= Time.unscaledDeltaTime;
            if (_feedbackTimer <= 0f && _feedbackText != null)
                _feedbackText.text = string.Empty;
        }
    }

    private void Toggle()
    {
        _open = !_open;
        _overlay.SetActive(_open);
        if (_open) { _game.SetSpeed(0); RefreshLabels(); }
        else _game.SetSpeed(1);
    }

    private void Close()  { _open = false; _overlay.SetActive(false); }

    private void OnSave(int slot)
    {
        bool ok = _game.SaveGame(slot);
        ShowFeedback(ok ? $"Saved to slot {slot + 1}." : "Save failed.");
        RefreshLabels();
    }

    private void OnMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnLoad(int slot)
    {
        if (!SaveSystem.SlotExists(slot)) { ShowFeedback($"Slot {slot + 1} is empty."); return; }
        bool ok = _game.LoadGame(slot);
        ShowFeedback(ok ? $"Loaded slot {slot + 1}." : "Load failed.");
        if (ok) Close();
    }

    private void ShowFeedback(string msg)
    {
        if (_feedbackText == null) return;
        _feedbackText.text = msg;
        _feedbackTimer     = FEEDBACK_DURATION;
    }

    private void RefreshLabels()
    {
        for (int i = 0; i < SaveSystem.MaxSlots; i++)
        {
            var    info = SaveSystem.GetSlotInfo(i);
            string lbl  = info != null ? info.DisplayString : $"Slot {i + 1}  —  Empty";
            if (_saveLabels[i] != null) _saveLabels[i].text       = lbl;
            if (_loadLabels[i] != null) _loadLabels[i].text       = lbl;
            if (_loadBtns[i]   != null) _loadBtns[i].interactable = info != null;
        }
    }

    // ---- Build UI ----

    private void BuildOverlay(Transform canvas)
    {
        _overlay = Make("Overlay", canvas);
        var dimRT = _overlay.AddComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
        _overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        var panel = Make("Panel", _overlay.transform);
        var pRT   = panel.AddComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = pRT.pivot = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta        = new Vector2(400f, 360f);
        pRT.anchoredPosition = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        if (_panelSprite != null)
        {
            panelImg.sprite = _panelSprite;
            panelImg.type   = Image.Type.Sliced;   // rubovi se ne rastezu
            panelImg.color  = Color.white;
            // Rub je nacrtan sitno; bez mnozitelja se na ploci 400x360 gubi.
            panelImg.pixelsPerUnitMultiplier = PanelPixelsPerUnitMultiplier;
        }
        else
        {
            panelImg.color = new Color(0.08f, 0.08f, 0.08f, 0.97f);
        }

        // Naslov i Resume su se preklapali: naslov je isao do 1.0, a gumb do 0.97.
        TMP(panel.transform,  "PAUSED", 17, Ink,     0f, 0.90f, 1f, 1f);
        Btn(panel.transform,  "Resume", () => { _game.SetSpeed(1); Close(); }, 0.1f, 0.79f, 0.9f, 0.885f);
        TMP(panel.transform,  "SAVE",   11, InkSoft, 0f, 0.68f, 1f, 0.76f);

        for (int i = 0; i < SaveSystem.MaxSlots; i++)
        {
            float x0 = 0.04f + i * 0.32f, x1 = x0 + 0.28f;
            int s = i;
            var b = Btn(panel.transform, $"Slot {i+1}", () => OnSave(s), x0, 0.50f, x1, 0.68f);
            _saveLabels[i] = SetupSlotLabel(b);
        }

        TMP(panel.transform, "LOAD", 11, InkSoft, 0f, 0.38f, 1f, 0.46f);

        for (int i = 0; i < SaveSystem.MaxSlots; i++)
        {
            float x0 = 0.04f + i * 0.32f, x1 = x0 + 0.28f;
            int s = i;
            var b = Btn(panel.transform, $"Slot {i+1}", () => OnLoad(s), x0, 0.20f, x1, 0.38f);
            _loadLabels[i] = SetupSlotLabel(b);
            _loadBtns[i]   = b;
        }

        _feedbackText = TMP(panel.transform, "", 11, Ink, 0f, 0.13f, 1f, 0.19f);

        // Main Menu na dnu — visina 0.10 ploce, isto kao Resume, umjesto
        // prijasnjih 0.05 zbog kojih je gumb bio duplo nizi od ostalih.
        Btn(panel.transform, "Main Menu", () => OnMainMenu(), 0.25f, 0.02f, 0.75f, 0.12f);
    }

    private static TextMeshProUGUI SetupSlotLabel(Button btn)
    {
        var t = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (t == null) return null;
        t.fontSize         = 10f;
        t.textWrappingMode = TMPro.TextWrappingModes.Normal;
        t.alignment        = TextAlignmentOptions.Center;
        t.color            = Ink;
        return t;
    }

    // ---- Factory (no overloads) ----

    private static GameObject Make(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform ART(Transform parent, string name,
        float x0, float y0, float x1, float y1,
        float pl = 6f, float pb = 4f, float pr = 6f, float pt = 4f)
    {
        var go = Make(name, parent);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = new Vector2(pl, pb);
        rt.offsetMax = new Vector2(-pr, -pt);
        return rt;
    }

    private static TextMeshProUGUI TMP(Transform parent, string text, float size,
        Color color, float x0, float y0, float x1, float y1)
    {
        var rt = ART(parent, "TMP_" + text, x0, y0, x1, y1);
        var t  = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    /// <summary>
    /// Gumb ploce. Ako je _buttonTemplate povucen, klonira se pa izgleda kao
    /// ostali gumbi u igri; inace se gradi stari ravni tamni gumb.
    /// </summary>
    private Button Btn(Transform parent, string label,
        UnityEngine.Events.UnityAction cb,
        float x0, float y0, float x1, float y1)
    {
        if (_buttonTemplate != null)
            return CloneButton(parent, label, cb, x0, y0, x1, y1);

        var rt = ART(parent, "Btn_" + label, x0, y0, x1, y1, 4f, 4f, 4f, 4f);
        rt.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.20f, 0.20f, 0.95f);
        var btn = rt.gameObject.AddComponent<Button>();
        var bc  = btn.colors;
        bc.highlightedColor = new Color(0.32f, 0.32f, 0.32f);
        bc.pressedColor     = new Color(0.10f, 0.10f, 0.10f);
        bc.disabledColor    = new Color(0.12f, 0.12f, 0.12f, 0.5f);
        btn.colors = bc;
        btn.onClick.AddListener(cb);

        var lblGO = Make("Label", rt);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;  lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = new Vector2(4f, 4f); lblRT.offsetMax = new Vector2(-4f, -4f);
        var t = lblGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 10f; t.color = Ink;
        t.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    private Button CloneButton(Transform parent, string label,
        UnityEngine.Events.UnityAction cb,
        float x0, float y0, float x1, float y1)
    {
        var go = Instantiate(_buttonTemplate.gameObject, parent);
        go.name = "Btn_" + label;
        go.SetActive(true);

        // Predlozak moze doci iz retka s layoutom; ovdje se pozicionira anchorima,
        // pa bi LayoutElement samo smetao.
        var le = go.GetComponent<LayoutElement>();
        if (le != null) Destroy(le);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = new Vector2(4f, 4f);
        rt.offsetMax = new Vector2(-4f, -4f);

        var btn = go.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(cb);
        btn.interactable = true;

        var t = go.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null)
        {
            t.text     = label;
            t.fontSize = 11f;
            t.color    = Ink;
        }

        return btn;
    }
}
