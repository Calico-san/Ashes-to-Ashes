using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor alat: slaze hijerarhiju build panela u otvorenoj sceni i povezuje
/// reference na komponenti BuildPanel.
///
/// Tools ▸ Ashes to Ashes ▸ Build Panel ▸ Slozi u sceni
///
/// Sve je pod Undo, pa Ctrl+Z vraca cijeli zahvat. Ponovno pokretanje brise
/// prethodno slozene objekte i slaze ih iznova, tako da alat mozes vrtjeti
/// koliko puta zelis dok namjestas brojke.
///
/// Alat NE dira spriteove — sve pocinje s praznim Imageom u placeholder boji.
/// Tomislavovu grafiku povlacis rucno, i tada na svakom Imageu postavis
/// Image Type: Sliced (vidi napomenu o 9-sliceu na dnu datoteke).
/// </summary>
public static class BuildPanelSceneBuilder
{
    // ---- Nazivi objekata; koriste se i pri brisanju stare verzije ----
    private const string ToggleName = "BuildToggleBtn";
    private const string PanelName  = "BuildMenuPanel";

    // ---- Mjere (iz dosadasnjeg koda koji je gradio panel) ----
    private const float PanelHeight   = 126f;
    private const float PanelBottomY  =  62f;   // iznad donje trake
    private const float ButtonWidth   =  96f;
    private const float ToggleSize    =  75f;
    private const float IconHeight    =  44f;
    private const float NameHeight    =  18f;
    private const float CostHeight    =  14f;
    private const float HoursHeight   =  12f;

    // ---- Placeholder boje dok ne dode grafika ----
    private static readonly Color PanelColor   = new Color(0.06f, 0.06f, 0.06f, 0.93f);
    private static readonly Color ButtonColor  = new Color(0.18f, 0.18f, 0.18f, 0.95f);
    private static readonly Color ToggleColor  = new Color(0.15f, 0.15f, 0.15f, 0.95f);
    private static readonly Color CostColor    = new Color(0.70f, 0.85f, 0.70f);
    private static readonly Color HoursColor   = new Color(0.60f, 0.60f, 0.60f);
    private static readonly Color ToggleInk    = new Color(0.90f, 0.75f, 0.20f);

    [MenuItem("Tools/Ashes to Ashes/Build Panel/Slozi u sceni")]
    private static void BuildScene()
    {
        var panelScript = Object.FindFirstObjectByType<BuildPanel>();
        if (panelScript == null)
        {
            EditorUtility.DisplayDialog("Build Panel",
                "U sceni nema objekta s komponentom BuildPanel.\n\n" +
                "Dodaj prazan GameObject pod Canvas, stavi na njega skriptu BuildPanel, pa pokreni alat ponovno.",
                "U redu");
            return;
        }

        var canvas = panelScript.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Build Panel",
                "U sceni nema Canvasa.", "U redu");
            return;
        }

        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Slozi build panel");

        DeletePrevious(canvas.transform);

        var toggle = CreateToggleButton(canvas.transform, out TextMeshProUGUI toggleLabel);
        var panel  = CreatePanelRoot(canvas.transform);

        var refs = new (BuildingType type, Button btn, Image icon,
                        TextMeshProUGUI name, TextMeshProUGUI cost, TextMeshProUGUI hours)
                   [BuildPanel.BuildableTypes.Length];

        for (int i = 0; i < BuildPanel.BuildableTypes.Length; i++)
            refs[i] = CreateBuildButton(panel.transform, BuildPanel.BuildableTypes[i]);

        WireComponent(panelScript, panel, toggle, toggleLabel, refs);

        panel.SetActive(false);   // panel pocinje zatvoren, kao i u runtimeu

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(panelScript.gameObject.scene);

        Selection.activeGameObject = panelScript.gameObject;
        EditorGUIUtility.PingObject(panelScript.gameObject);

        Debug.Log($"[BuildPanel] Slozeno: {PanelName} sa {refs.Length} gumba i {ToggleName}. " +
                  "Reference su povezane. Spriteove i Image Type: Sliced postavi rucno.", panelScript);
    }

    // -------------------------------------------------------

    private static void DeletePrevious(Transform canvas)
    {
        foreach (var name in new[] { ToggleName, PanelName })
        {
            var existing = canvas.Find(name);
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);
        }
    }

    private static GameObject CreateToggleButton(Transform canvas, out TextMeshProUGUI label)
    {
        var go = NewUIObject(ToggleName, canvas);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-10f, 10f);
        rt.sizeDelta        = new Vector2(ToggleSize, ToggleSize);

        var img = go.AddComponent<Image>();
        img.color = ToggleColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.28f, 0.28f, 0.28f);
        colors.pressedColor     = new Color(0.08f, 0.08f, 0.08f);
        btn.colors = colors;

        label = CreateStretchedLabel(go.transform, "Label", "[B]", 18f, ToggleInk);
        return go;
    }

    private static GameObject CreatePanelRoot(Transform canvas)
    {
        var go = NewUIObject(PanelName, canvas);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, PanelBottomY);
        rt.sizeDelta        = new Vector2(0f, PanelHeight);

        go.AddComponent<Image>().color = PanelColor;

        var hg = go.AddComponent<HorizontalLayoutGroup>();
        hg.padding                = new RectOffset(12, 12, 8, 8);
        hg.spacing                = 8f;
        hg.childAlignment         = TextAnchor.MiddleLeft;
        hg.childControlWidth      = false;   // gumbi drze svojih 96 px
        hg.childControlHeight     = true;
        hg.childForceExpandWidth  = false;
        hg.childForceExpandHeight = true;

        return go;
    }

    private static (BuildingType, Button, Image, TextMeshProUGUI, TextMeshProUGUI, TextMeshProUGUI)
        CreateBuildButton(Transform parent, BuildingType type)
    {
        var go = NewUIObject($"Btn_{type}", parent);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(ButtonWidth, 0f);

        var img = go.AddComponent<Image>();
        img.color = ButtonColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.28f, 0.28f, 0.28f);
        colors.pressedColor     = new Color(0.08f, 0.08f, 0.08f);
        colors.disabledColor    = new Color(0.12f, 0.12f, 0.12f, 0.6f);
        btn.colors = colors;

        var vg = go.AddComponent<VerticalLayoutGroup>();
        vg.padding                = new RectOffset(6, 6, 6, 6);
        vg.spacing                = 2f;
        vg.childControlWidth      = true;
        vg.childControlHeight     = false;   // djeca drze svoj Preferred Height
        vg.childForceExpandWidth  = true;
        vg.childForceExpandHeight = false;

        // Icon
        var iconGO = NewUIObject("Icon", go.transform);
        iconGO.AddComponent<LayoutElement>().preferredHeight = IconHeight;
        var icon = iconGO.AddComponent<Image>();
        icon.color          = BuildingFactory.Meta(type).color;
        icon.preserveAspect = true;
        icon.raycastTarget  = false;   // inace ikona proguta klik na gumb

        // Tekst — sadrzaj se svejedno prepisuje iz koda pri pokretanju,
        // ovo je samo da se u Sceneu odmah vidi raspored.
        var nameLbl  = CreateLayoutLabel(go.transform, "Name",  BuildingFactory.Meta(type).name,
                                         11f, NameHeight,  Color.white);
        var costLbl  = CreateLayoutLabel(go.transform, "Cost",  "W:0",
                                          9f, CostHeight,  CostColor);
        var hoursLbl = CreateLayoutLabel(go.transform, "Hours", "0h",
                                          9f, HoursHeight, HoursColor);

        return (type, btn, icon, nameLbl, costLbl, hoursLbl);
    }

    // -------------------------------------------------------
    // Povezivanje referenci na komponenti
    // -------------------------------------------------------

    private static void WireComponent(BuildPanel script, GameObject panel, GameObject toggle,
        TextMeshProUGUI toggleLabel,
        (BuildingType type, Button btn, Image icon,
         TextMeshProUGUI name, TextMeshProUGUI cost, TextMeshProUGUI hours)[] refs)
    {
        Undo.RecordObject(script, "Povezi build panel");

        var so = new SerializedObject(script);
        so.FindProperty("_panelRoot").objectReferenceValue      = panel;
        so.FindProperty("_toggleBtn").objectReferenceValue      = toggle.GetComponent<Button>();
        so.FindProperty("_toggleBtnLabel").objectReferenceValue = toggleLabel;

        var list = so.FindProperty("_sceneBuildButtons");
        list.arraySize = refs.Length;

        for (int i = 0; i < refs.Length; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            // BuildingType krece od 0 i raste bez preskakanja, pa je indeks == vrijednost.
            e.FindPropertyRelative("Type").enumValueIndex          = (int)refs[i].type;
            e.FindPropertyRelative("Button").objectReferenceValue      = refs[i].btn;
            e.FindPropertyRelative("Icon").objectReferenceValue        = refs[i].icon;
            e.FindPropertyRelative("NameLabel").objectReferenceValue   = refs[i].name;
            e.FindPropertyRelative("CostLabel").objectReferenceValue   = refs[i].cost;
            e.FindPropertyRelative("HoursLabel").objectReferenceValue  = refs[i].hours;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(script);
    }

    // -------------------------------------------------------
    // Helperi
    // -------------------------------------------------------

    private static GameObject NewUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, $"Stvori {name}");
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>Natpis koji popunjava cijeli roditelj — za gumb s ikonom ili slovom.</summary>
    private static TextMeshProUGUI CreateStretchedLabel(Transform parent, string name,
        string text, float fontSize, Color color)
    {
        var go = NewUIObject(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    /// <summary>Natpis u vertikalnom layoutu — visinu drzi LayoutElement.</summary>
    private static TextMeshProUGUI CreateLayoutLabel(Transform parent, string name,
        string text, float fontSize, float height, Color color)
    {
        var go = NewUIObject(name, parent);
        go.AddComponent<LayoutElement>().preferredHeight = height;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = fontSize;
        tmp.color            = color;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }
}

// ---------------------------------------------------------------------------
// Napomena o 9-sliceu, jer se na tome vec izgubilo vrijeme:
//
// Kad povuces Tomislavov sprite na Image, postavi Image Type: Sliced i ukljuci
// Fill Center. Da bi Sliced uopce nesto radio, sprite mora imati Border razlicit
// od nule (Sprite Editor ▸ Border ▸ Apply).
//
// Debljina ruba na ekranu je:
//     border_px × Canvas.ReferencePixelsPerUnit / (sprite_PPU × PixelsPerUnitMultiplier)
//
// Uz Reference Pixels Per Unit 100 i sprite na PPU 16, rub se crta 6,25× deblji
// nego sto je nacrtan. Postavi Reference Pixels Per Unit na PPU spritea za
// odnos 1:1, a zumiraj kroz Pixels Per Unit Multiplier i to samo cijelim brojem.
//
// Ako se razvlaci SREDINA panela (uzorak drva, sum), Sliced ne pomaze jer on
// cuva samo rubove — za to treba Image Type: Tiled uz Wrap Mode: Repeat i
// Mesh Type: Full Rect.
// ---------------------------------------------------------------------------
