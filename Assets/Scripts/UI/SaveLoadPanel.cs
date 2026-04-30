using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause menu overlay with save and load slot buttons.
/// Opened by pressing Escape during gameplay.
/// 
/// Layout:
///   [Resume]
///   ── SAVE ──
///   [Slot 1 — Day 4]  [Slot 2 — Empty]  [Slot 3 — Day 1]
///   ── LOAD ──
///   [Slot 1 — Day 4]  [Slot 2 — Empty]  [Slot 3 — Day 1]
/// </summary>
public class SaveLoadPanel : MonoBehaviour
{
    private GameObject                _overlay;
    private bool                      _open;
    private PrototypeGameController   _game;

    private TextMeshProUGUI[] _saveSlotLabels = new TextMeshProUGUI[SaveSystem.MaxSlots];
    private TextMeshProUGUI[] _loadSlotLabels = new TextMeshProUGUI[SaveSystem.MaxSlots];
    private Button[]          _loadSlotBtns   = new Button[SaveSystem.MaxSlots];

    private TextMeshProUGUI   _feedbackText;
    private float             _feedbackTimer;
    private const float       FeedbackDuration = 2.5f;

    // -------------------------------------------------------
    // Init
    // -------------------------------------------------------

    public void Build(PrototypeGameController game, Transform canvasTransform)
    {
        _game = game;
        BuildOverlay(canvasTransform);
        _overlay.SetActive(false);
    }

    // -------------------------------------------------------
    // Update
    // -------------------------------------------------------

    private void Update()
    {
        // Escape toggles pause menu
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Toggle();
        }

        // Feedback timer
        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= Time.unscaledDeltaTime;
            if (_feedbackTimer <= 0f && _feedbackText != null)
                _feedbackText.text = string.Empty;
        }
    }

    // -------------------------------------------------------
    // Toggle
    // -------------------------------------------------------

    private void Toggle()
    {
        _open = !_open;
        _overlay.SetActive(_open);

        if (_open)
        {
            _game.SetSpeed(0); // pause on open
            RefreshSlotLabels();
        }
    }

    private void Close()
    {
        _open = false;
        _overlay.SetActive(false);
    }

    // -------------------------------------------------------
    // Actions
    // -------------------------------------------------------

    private void OnSave(int slot)
    {
        bool ok = _game.SaveGame(slot);
        ShowFeedback(ok ? $"Game saved to slot {slot + 1}." : "Save failed.");
        RefreshSlotLabels();
    }

    private void OnLoad(int slot)
    {
        if (!SaveSystem.SlotExists(slot))
        {
            ShowFeedback($"Slot {slot + 1} is empty.");
            return;
        }

        bool ok = _game.LoadGame(slot);
        ShowFeedback(ok ? $"Game loaded from slot {slot + 1}." : "Load failed.");
        if (ok) Close();
    }

    // -------------------------------------------------------
    // UI helpers
    // -------------------------------------------------------

    private void RefreshSlotLabels()
    {
        for (int i = 0; i < SaveSystem.MaxSlots; i++)
        {
            var info  = SaveSystem.GetSlotInfo(i);
            string lbl = info != null ? info.DisplayString : $"Slot {i + 1}  —  Empty";

            if (_saveSlotLabels[i] != null) _saveSlotLabels[i].text = lbl;
            if (_loadSlotLabels[i] != null) _loadSlotLabels[i].text = lbl;
            if (_loadSlotBtns[i]   != null) _loadSlotBtns[i].interactable = info != null;
        }
    }

    private void ShowFeedback(string msg)
    {
        if (_feedbackText == null) return;
        _feedbackText.text = msg;
        _feedbackTimer = FeedbackDuration;
    }

    // -------------------------------------------------------
    // Build UI
    // -------------------------------------------------------

    private void BuildOverlay(Transform canvas)
    {
        // Full-screen dim
        _overlay = new GameObject("SaveLoadOverlay", typeof(RectTransform), typeof(Image));
        _overlay.transform.SetParent(canvas, false);
        var dimRT = _overlay.GetComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
        _overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        _overlay.GetComponent<Image>().raycastTarget = true;

        // Centre panel
        var panel = MakePanel(_overlay.transform, new Vector2(380f, 340f));

        var vg = panel.AddComponent<VerticalLayoutGroup>();
        vg.padding               = new RectOffset(16, 16, 16, 16);
        vg.spacing               = 10f;
        vg.childControlWidth     = true;
        vg.childControlHeight    = false;
        vg.childForceExpandWidth = true;

        // Title
        var title = MakeLabel(panel.transform, "PAUSED", 18, Color.white);
        LE(title.gameObject, prefH: 28);

        // Resume button
        var resumeBtn = MakeButton(panel.transform, "Resume", () =>
        {
            _game.SetSpeed(1);
            Close();
        });
        LE(resumeBtn.gameObject, prefH: 28);

        // Separator
        MakeSep(panel.transform);

        // ---- Save ----
        var saveLabel = MakeLabel(panel.transform, "SAVE", 11, new Color(0.7f, 0.7f, 0.7f));
        LE(saveLabel.gameObject, prefH: 16);

        var saveRow = MakeRow(panel.transform);
        for (int i = 0; i < SaveSystem.MaxSlots; i++)
        {
            int slot = i;
            var btn  = MakeButton(saveRow.transform, $"Slot {i + 1}", () => OnSave(slot));
            LE(btn.gameObject, prefH: 40, flexW: 1);
            var lbl  = btn.GetComponentInChildren<TextMeshProUGUI>();
            lbl.fontSize = 9f;
            lbl.textWrappingMode = TMPro.TextWrappingModes.Normal;
            _saveSlotLabels[i] = lbl;
        }

        // ---- Load ----
        var loadLabel = MakeLabel(panel.transform, "LOAD", 11, new Color(0.7f, 0.7f, 0.7f));
        LE(loadLabel.gameObject, prefH: 16);

        var loadRow = MakeRow(panel.transform);
        for (int i = 0; i < SaveSystem.MaxSlots; i++)
        {
            int slot = i;
            var btn  = MakeButton(loadRow.transform, $"Slot {i + 1}", () => OnLoad(slot));
            LE(btn.gameObject, prefH: 40, flexW: 1);
            var lbl  = btn.GetComponentInChildren<TextMeshProUGUI>();
            lbl.fontSize = 9f;
            lbl.textWrappingMode = TMPro.TextWrappingModes.Normal;
            _loadSlotLabels[i] = lbl;
            _loadSlotBtns[i]   = btn;
        }

        MakeSep(panel.transform);

        // Feedback text
        _feedbackText = MakeLabel(panel.transform, "", 10, new Color(0.4f, 0.9f, 0.4f));
        LE(_feedbackText.gameObject, prefH: 18);
    }

    private static GameObject MakePanel(Transform parent, Vector2 size)
    {
        var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.07f, 0.07f, 0.07f, 0.97f);
        return go;
    }

    private static TextMeshProUGUI MakeLabel(Transform parent, string text, float size, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = size;
        t.color     = color;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    private static Button MakeButton(Transform parent, string label, UnityEngine.Events.UnityAction cb)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.20f, 0.95f);
        var btn = go.GetComponent<Button>();
        var bc  = btn.colors;
        bc.highlightedColor = new Color(0.32f, 0.32f, 0.32f);
        bc.pressedColor     = new Color(0.10f, 0.10f, 0.10f);
        bc.disabledColor    = new Color(0.12f, 0.12f, 0.12f, 0.5f);
        btn.colors = bc;
        btn.onClick.AddListener(cb);

        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(go.transform, false);
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(4, 4); lrt.offsetMax = new Vector2(-4, -4);
        var t = lblGO.AddComponent<TextMeshProUGUI>();
        t.text      = label;
        t.fontSize  = 11f;
        t.color     = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    private static GameObject MakeRow(Transform parent)
    {
        var go = new GameObject("Row", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        LE(go, prefH: 44);
        var hg = go.AddComponent<HorizontalLayoutGroup>();
        hg.spacing               = 8f;
        hg.childControlWidth     = false;
        hg.childControlHeight    = true;
        hg.childForceExpandWidth = false;
        hg.childForceExpandHeight= true;
        return go;
    }

    private static void MakeSep(Transform parent)
    {
        var go = new GameObject("Sep", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.28f, 0.28f, 0.28f, 0.8f);
        LE(go, prefH: 1);
    }

    private static void LE(GameObject go, float prefH = -1, float prefW = -1, float flexW = -1)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        if (prefH >= 0) le.preferredHeight = prefH;
        if (prefW >= 0) le.preferredWidth  = prefW;
        if (flexW >= 0) le.flexibleWidth   = flexW;
    }
}
