using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadPanel : MonoBehaviour
{
    private GameObject              _overlay;
    private bool                    _open;
    private PrototypeGameController _game;

    private readonly TextMeshProUGUI[] _saveLabels = new TextMeshProUGUI[SaveSystem.MaxSlots];
    private readonly TextMeshProUGUI[] _loadLabels = new TextMeshProUGUI[SaveSystem.MaxSlots];
    private readonly Button[]          _loadBtns   = new Button[SaveSystem.MaxSlots];

    private TextMeshProUGUI _feedbackText;
    private float           _feedbackTimer;
    private const float     FEEDBACK_DURATION = 2.5f;

    public void Build(PrototypeGameController game, Transform canvas)
    {
        _game = game;
        BuildOverlay(canvas);
        _overlay.SetActive(false);
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
    }

    private void Close()  { _open = false; _overlay.SetActive(false); }

    private void OnSave(int slot)
    {
        bool ok = _game.SaveGame(slot);
        ShowFeedback(ok ? $"Saved to slot {slot + 1}." : "Save failed.");
        RefreshLabels();
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
        pRT.sizeDelta        = new Vector2(400f, 310f);
        pRT.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.97f);

        TMP(panel.transform,  "PAUSED", 17, Color.white,          0f, 0.88f, 1f, 1f);
        Btn(panel.transform,  "Resume", () => { _game.SetSpeed(1); Close(); }, 0.1f, 0.78f, 0.9f, 0.93f);
        TMP(panel.transform,  "SAVE",   10, new Color(.6f,.6f,.6f), 0f, 0.63f, 1f, 0.71f);

        for (int i = 0; i < SaveSystem.MaxSlots; i++)
        {
            float x0 = 0.04f + i * 0.32f, x1 = x0 + 0.28f;
            int s = i;
            var b = Btn(panel.transform, $"Slot {i+1}", () => OnSave(s), x0, 0.44f, x1, 0.63f);
            _saveLabels[i] = SetupSlotLabel(b);
        }

        TMP(panel.transform, "LOAD", 10, new Color(.6f,.6f,.6f), 0f, 0.29f, 1f, 0.37f);

        for (int i = 0; i < SaveSystem.MaxSlots; i++)
        {
            float x0 = 0.04f + i * 0.32f, x1 = x0 + 0.28f;
            int s = i;
            var b = Btn(panel.transform, $"Slot {i+1}", () => OnLoad(s), x0, 0.10f, x1, 0.29f);
            _loadLabels[i] = SetupSlotLabel(b);
            _loadBtns[i]   = b;
        }

        _feedbackText = TMP(panel.transform, "", 10, new Color(.4f,.9f,.4f), 0f, 0f, 1f, 0.10f);
    }

    private static TextMeshProUGUI SetupSlotLabel(Button btn)
    {
        var t = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (t == null) return null;
        t.fontSize         = 9f;
        t.textWrappingMode = TMPro.TextWrappingModes.Normal;
        t.alignment        = TextAlignmentOptions.Center;
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

    private static Button Btn(Transform parent, string label,
        UnityEngine.Events.UnityAction cb,
        float x0, float y0, float x1, float y1)
    {
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
        t.text = label; t.fontSize = 10f; t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        return btn;
    }
}
