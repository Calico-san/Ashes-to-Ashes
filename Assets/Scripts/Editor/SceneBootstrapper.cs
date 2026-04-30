#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class SceneBootstrapper
{
    [MenuItem("Ashes/Setup Scene")]
    public static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("Setup Scene",
            "This will create all GameObjects in the current scene.\n" +
            "Run this once on a new empty scene.\n\nContinue?", "Yes", "Cancel"))
            return;

        CreateGameController();
        CreateCamera();
        CreateEventSystem();
        CreatePlacementValidator();
        CreateIslandTilemap();
        CreateOceanBackground();
        CreateCanvas();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Done",
            "Scene setup complete!\n\n" +
            "Next steps:\n" +
            "1. Save scene (Ctrl+S)\n" +
            "2. Create IslandMap asset (Assets > Create > Ashes > TilemapData)\n" +
            "3. Create SpriteRegistry asset (Assets > Create > Ashes > SpriteRegistry)\n" +
            "4. Press Play to test", "OK");
    }

    // -------------------------------------------------------
    // Core objects
    // -------------------------------------------------------

    private static void CreateGameController()
    {
        if (GameObject.Find("GameController") != null) return;
        new GameObject("GameController").AddComponent<PrototypeGameController>();
    }

    private static void CreateCamera()
    {
        if (GameObject.FindWithTag("MainCamera") != null) return;
        var go  = new GameObject("Main Camera");
        go.tag  = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.09f, 0.28f, 0.52f, 1f);
        cam.orthographic     = true;
        cam.orthographicSize = 10f;
        go.transform.position = new Vector3(0f, 0f, -15f);
        go.AddComponent<AudioListener>();
        go.AddComponent<PrototypeCameraController>();

        var sel = new GameObject("SelectionController");
        sel.AddComponent<PrototypeSelectionController>();
    }

    private static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    private static void CreatePlacementValidator()
    {
        if (Object.FindFirstObjectByType<PlacementValidator>() != null) return;
        new GameObject("PlacementValidator").AddComponent<PlacementValidator>();
    }

    private static void CreateIslandTilemap()
    {
        if (Object.FindFirstObjectByType<IslandTilemapRenderer>() != null) return;
        new GameObject("IslandTilemap").AddComponent<IslandTilemapRenderer>();
    }

    private static void CreateOceanBackground()
    {
        if (GameObject.Find("Ocean") != null) return;
        var ocean = new GameObject("Ocean");
        var sr    = ocean.AddComponent<SpriteRenderer>();
        sr.color  = new Color(0.09f, 0.28f, 0.52f, 1f);
        sr.sortingOrder = -20;
        var tex   = new Texture2D(4, 4);
        var px    = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(.5f,.5f), 4);
        ocean.transform.localScale = new Vector3(80f, 60f, 1f);
    }

    // -------------------------------------------------------
    // Canvas — follows wireframe layout
    // -------------------------------------------------------

    private static void CreateCanvas()
    {
        if (Object.FindFirstObjectByType<PrototypeUIController>() != null) return;

        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var ui = canvasGO.AddComponent<PrototypeUIController>();

        // ---- DAY — circle, top centre ----
        var dayPanel = AnchoredGO(canvasGO.transform, "DayPanel",
            0.5f, 1f, 0.5f, 1f, -50f, -100f, 100f, 0f);
        Outline(dayPanel);
        var dayText = TMP(dayPanel.transform, "ClockText", 22, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(dayText.gameObject);

        // ---- RESOURCES LEFT — top left of DAY ----
        var resLeft = AnchoredGO(canvasGO.transform, "ResourcesLeft",
            0.1f, 1f, 0.42f, 1f, 0f, -50f, 0f, -10f);
        Outline(resLeft);
        var resLeftText = TMP(resLeft.transform, "ResourcesText", 11, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(resLeftText.gameObject);

        // ---- RESOURCES RIGHT — top right of DAY ----
        var resRight = AnchoredGO(canvasGO.transform, "ResourcesRight",
            0.58f, 1f, 0.9f, 1f, 0f, -50f, 0f, -10f);
        Outline(resRight);
        var resRightText = TMP(resRight.transform, "ResourcesRightText", 11, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(resRightText.gameObject);

        // ---- TUTORIAL — circle, top left ----
        var tutBtn = AnchoredGO(canvasGO.transform, "TutorialButton",
            0f, 1f, 0f, 1f, 20f, -120f, 100f, -20f);
        Outline(tutBtn);
        tutBtn.AddComponent<Button>();
        var tutText = TMP(tutBtn.transform, "Label", 10, FontStyles.Normal, TextAlignmentOptions.Center);
        tutText.text = "TUTORIAL";
        Stretch(tutText.gameObject);

        // ---- OBJECTIVES — panel, bottom left ----
        var objPanel = AnchoredGO(canvasGO.transform, "ObjectivesPanel",
            0f, 0f, 0.22f, 0f, 20f, 60f, -10f, 160f);
        Outline(objPanel);
        var objText = TMP(objPanel.transform, "ObjectivesText", 12, FontStyles.Bold, TextAlignmentOptions.Center);
        objText.text = "OBJECTIVES";
        Stretch(objText.gameObject);

        // ---- HOPE bar — bottom centre ----
        var hopeBar = AnchoredGO(canvasGO.transform, "HopeBar",
            0.3f, 0f, 0.7f, 0f, 0f, 80f, 0f, 110f);
        Outline(hopeBar);
        var hopeText = TMP(hopeBar.transform, "HopeText", 12, FontStyles.Bold, TextAlignmentOptions.Center);
        hopeText.text = "HOPE";
        Stretch(hopeText.gameObject);

        // ---- TIME bar — below HOPE (speed controls) ----
        var timeBar = AnchoredGO(canvasGO.transform, "TimeBar",
            0.32f, 0f, 0.68f, 0f, 0f, 40f, 0f, 75f);
        Outline(timeBar);
        var timeHG = timeBar.AddComponent<HorizontalLayoutGroup>();
        timeHG.padding = new RectOffset(8,8,4,4); timeHG.spacing = 4;
        timeHG.childAlignment = TextAnchor.MiddleCenter;
        timeHG.childControlWidth = timeHG.childControlHeight = false;
        timeHG.childForceExpandWidth = timeHG.childForceExpandHeight = false;

        var pauseBtn  = Btn(timeBar.transform, "PauseBtn",  "||",  32, 28);
        var speed1Btn = Btn(timeBar.transform, "Speed1Btn", "1x",  32, 28);
        var speed2Btn = Btn(timeBar.transform, "Speed2Btn", "2x",  32, 28);

        // ---- POPULATION — bottom right ----
        var popBar = AnchoredGO(canvasGO.transform, "PopulationBar",
            0.72f, 0f, 1f, 0f, -10f, 10f, -80f, 40f);
        Outline(popBar);
        var popText = TMP(popBar.transform, "PopulationText", 10, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(popText.gameObject);

        // BUILD MENU button is created by BuildPanel.Build() at runtime
        // positioned at bottom right corner — see BuildPanel.BuildToggleButton()

        // ---- RIGHT PANEL — building/ship info (right edge, hidden by default) ----
        var rightPanel = AnchoredGO(canvasGO.transform, "RightPanel",
            1f, 0f, 1f, 1f, -200f, 40f, 0f, -40f);
        rightPanel.AddComponent<Image>().color = new Color(0.05f,0.05f,0.05f,0.90f);
        var rpVG = rightPanel.AddComponent<VerticalLayoutGroup>();
        rpVG.padding = new RectOffset(12,12,12,12); rpVG.spacing = 6;
        rpVG.childAlignment = TextAnchor.UpperLeft;
        rpVG.childControlWidth = rpVG.childControlHeight = rpVG.childForceExpandWidth = true;
        rpVG.childForceExpandHeight = false;

        var titleText  = TMP(rightPanel.transform, "TitleText",  12, FontStyles.Bold,   TextAlignmentOptions.TopLeft);
        LE(titleText.gameObject, 18, 18);
        Sep(rightPanel.transform);
        var workerText = TMP(rightPanel.transform, "WorkerText", 10, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        workerText.color = new Color(0.85f,0.85f,0.85f);
        LE(workerText.gameObject, 14, 14);
        var outputText = TMP(rightPanel.transform, "OutputText", 10, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        outputText.color = new Color(0.65f,0.82f,0.65f);
        outputText.textWrappingMode = TextWrappingModes.Normal;
        LE(outputText.gameObject, 52, 52);
        Sep(rightPanel.transform);
        var assignBtn    = Btn(rightPanel.transform, "AssignBtn",    "+ Add worker",     166, 26);
        LE(assignBtn.gameObject, 26, 26);
        Gap(rightPanel.transform, 3);
        var removeBtn    = Btn(rightPanel.transform, "RemoveBtn",    "- Remove worker",  166, 26);
        LE(removeBtn.gameObject, 26, 26);
        Gap(rightPanel.transform, 3);
        var assignEngBtn = Btn(rightPanel.transform, "AssignEngBtn", "+ Add engineer",   166, 26);
        LE(assignEngBtn.gameObject, 26, 26);
        Gap(rightPanel.transform, 3);
        var removeEngBtn = Btn(rightPanel.transform, "RemoveEngBtn", "- Remove engineer",166, 26);
        LE(removeEngBtn.gameObject, 26, 26);
        Sep(rightPanel.transform);

        // Build slot container (shown when empty slot selected)
        var buildSlotContainer = new GameObject("BuildSlotContainer", typeof(RectTransform));
        buildSlotContainer.transform.SetParent(rightPanel.transform, false);
        LE(buildSlotContainer, 60, 20);
        buildSlotContainer.SetActive(false);

        // Ship list
        var shipList = new GameObject("ShipListContainer", typeof(RectTransform));
        shipList.transform.SetParent(rightPanel.transform, false);
        var slVG = shipList.AddComponent<VerticalLayoutGroup>();
        slVG.spacing = 3;
        slVG.childControlWidth = slVG.childControlHeight = slVG.childForceExpandWidth = true;
        slVG.childForceExpandHeight = false;
        LE(shipList, 120, 20);
        shipList.SetActive(false);

        // Ship detail
        var shipDetail = new GameObject("ShipDetailContainer", typeof(RectTransform));
        shipDetail.transform.SetParent(rightPanel.transform, false);
        var sdVG = shipDetail.AddComponent<VerticalLayoutGroup>();
        sdVG.spacing = 5;
        sdVG.childControlWidth = sdVG.childControlHeight = sdVG.childForceExpandWidth = true;
        sdVG.childForceExpandHeight = false;
        LE(shipDetail, 160, 20);

        var backBtn         = Btn(shipDetail.transform, "BackBtn",         "< Back",         166, 22); LE(backBtn.gameObject, 22, 22);
        Sep(shipDetail.transform);
        var shipDetailTitle = TMP(shipDetail.transform, "ShipDetailTitle", 11, FontStyles.Bold,   TextAlignmentOptions.TopLeft); LE(shipDetailTitle.gameObject, 16, 16);
        var shipDetailInfo  = TMP(shipDetail.transform, "ShipDetailInfo",  10, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        shipDetailInfo.color = new Color(0.75f,0.88f,0.75f);
        shipDetailInfo.textWrappingMode = TextWrappingModes.Normal;
        LE(shipDetailInfo.gameObject, 48, 48);
        Sep(shipDetail.transform);
        var sailorAssign    = Btn(shipDetail.transform, "SailorAssignBtn", "+ Add sailor",   166, 24); LE(sailorAssign.gameObject, 24, 24);
        Gap(shipDetail.transform, 3);
        var sailorRemove    = Btn(shipDetail.transform, "SailorRemoveBtn", "- Remove sailor",166, 24); LE(sailorRemove.gameObject, 24, 24);
        shipDetail.SetActive(false);
        rightPanel.SetActive(false);

        // ---- BuildPanel + SaveLoadPanel (outside canvas) ----
        var buildPanelGO   = new GameObject("BuildPanel");
        var buildPanel     = buildPanelGO.AddComponent<BuildPanel>();
        var saveLoadGO     = new GameObject("SaveLoadPanel");
        var saveLoadPanel  = saveLoadGO.AddComponent<SaveLoadPanel>();

        // ---- Wire all SerializeField references ----
        var so = new SerializedObject(ui);
        SetRef(so, "_resourcesText",       resLeftText);
        SetRef(so, "_clockText",           dayText);
        SetRef(so, "_pauseBtn",            pauseBtn);
        SetRef(so, "_speed1Btn",           speed1Btn);
        SetRef(so, "_speed2Btn",           speed2Btn);
        SetRef(so, "_populationText",      popText);
        SetRef(so, "_rightPanel",          rightPanel);
        SetRef(so, "_titleText",           titleText);
        SetRef(so, "_workerText",          workerText);
        SetRef(so, "_outputText",          outputText);
        SetRef(so, "_assignBtn",           assignBtn);
        SetRef(so, "_removeBtn",           removeBtn);
        SetRef(so, "_assignEngBtn",        assignEngBtn);
        SetRef(so, "_removeEngBtn",        removeEngBtn);
        SetRef(so, "_buildSlotContainer",  buildSlotContainer);
        SetRef(so, "_shipListContainer",   shipList);
        SetRef(so, "_shipDetailContainer", shipDetail);
        SetRef(so, "_shipDetailTitle",     shipDetailTitle);
        SetRef(so, "_shipDetailInfo",      shipDetailInfo);
        SetRef(so, "_shipDetailBack",      backBtn);
        SetRef(so, "_shipDetailAssign",    sailorAssign);
        SetRef(so, "_shipDetailRemove",    sailorRemove);
        SetRef(so, "_buildPanel",          buildPanel);
        SetRef(so, "_saveLoadPanel",       saveLoadPanel);
        so.ApplyModifiedProperties();

        Debug.Log("[SceneBootstrapper] Canvas created. Mislav can now edit elements visually.");
    }

    // -------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------

    /// <summary>Creates a RectTransform GO anchored by normalized values + pixel offsets.</summary>
    private static GameObject AnchoredGO(Transform parent, string name,
        float ancMinX, float ancMinY, float ancMaxX, float ancMaxY,
        float offMinX, float offMinY, float offMaxX, float offMaxY)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ancMinX, ancMinY);
        rt.anchorMax = new Vector2(ancMaxX, ancMaxY);
        rt.offsetMin = new Vector2(offMinX,  offMinY);
        rt.offsetMax = new Vector2(offMaxX, offMaxY);
        return go;
    }

    private static void Outline(GameObject go)
    {
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);  // grey, 50% alpha
    }

    private static TextMeshProUGUI TMP(Transform parent, string name,
        float size, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size; t.fontStyle = style;
        t.alignment = align; t.color = Color.white;
        t.enableAutoSizing = false;
        return t;
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(4,4); rt.offsetMax = new Vector2(-4,-4);
    }

    private static Button Btn(Transform parent, string name, string label, float w, float h)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = new Color(0.15f,0.15f,0.15f,0.9f);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.preferredHeight = h;
        le.minWidth = w; le.minHeight = h;

        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(go.transform, false);
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var t = lblGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 11f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.textWrappingMode = TextWrappingModes.NoWrap;

        return go.GetComponent<Button>();
    }

    private static void Sep(Transform parent)
    {
        var go = new GameObject("Sep", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.28f,0.28f,0.28f,0.8f);
        LE(go, 1, 1);
    }

    private static void Gap(Transform parent, float h)
    {
        var go = new GameObject("Gap", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        LE(go, h, h);
    }

    private static void LE(GameObject go, float prefH, float minH)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = prefH; le.minHeight = minH;
    }

    private static void SetRef(SerializedObject so, string field, Object obj)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = obj;
        else Debug.LogWarning($"[SceneBootstrapper] Field not found: {field}");
    }
}
#endif
