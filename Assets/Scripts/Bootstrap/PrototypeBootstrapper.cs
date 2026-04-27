using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class PrototypeBootstrapper : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (FindFirstObjectByType<PrototypeBootstrapper>() != null) return;
        new GameObject("PrototypeBootstrapper").AddComponent<PrototypeBootstrapper>();
    }

    private void Awake()
    {
        Application.targetFrameRate = 120;
        SetupScene();
    }

    private void SetupScene()
    {
        CreateEventSystemIfMissing();
        SetupSpriteRegistry();
        SetupCameraAndBackground(out Camera mainCamera);

        var gameController = new GameObject("GameController").AddComponent<PrototypeGameController>();
        var uiController   = new GameObject("UIController").AddComponent<PrototypeUIController>();
        uiController.Build(gameController);

        Sprite workerSprite = SimpleShapeFactory.CreateFilledTriangleSprite(new Color(1f, 0.9f, 0.25f, 1f));
        gameController.Initialize(uiController, workerSprite, new Vector3(-3.5f, -1.8f, 0f));

        CreateTownHall(gameController);
        CreateBuildSlots(gameController);
        CreateWorkerSpawnMarker();

        var selection = new GameObject("SelectionController").AddComponent<PrototypeSelectionController>();
        selection.Initialize(gameController, mainCamera);
    }

    // ---- Town Hall — pre-built, no production, just visual anchor ----
    private void SetupSpriteRegistry()
    {
        // Creates SpriteRegistry with no sprites assigned (all null = fallback to SimpleShapeFactory).
        // Assign pixel art sprites here later via Inspector or by loading from Resources/.
        var go = new GameObject("SpriteRegistry");
        go.AddComponent<SpriteRegistry>();
    }

    private void CreateTownHall(PrototypeGameController game)
    {
        var go = new GameObject("TownHall");

        // BuildingInstance.Initialize adds SpriteRenderer, Collider and BuildingAnimator itself.
        // Do NOT add SpriteRenderer manually before calling Initialize.
        var building = go.AddComponent<BuildingInstance>();
        building.Initialize(game, "Town Hall", ResourceType.Wood,
            new Color(0.72f, 0.58f, 0.22f), new Vector3(0f, 0.6f, 0f),
            new Vector2(2.0f, 2.0f), false);
        building.SetTownHall(true);
        game.RegisterBuilding(building);

        // Label
        var lbl = new GameObject("TownHallLabel");
        lbl.transform.SetParent(go.transform, false);
        lbl.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        var tmp = lbl.AddComponent<TextMeshPro>();
        tmp.text        = "Town Hall";
        tmp.fontSize    = 1.8f;
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.color       = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
        tmp.sortingOrder = 20;
    }

    // ---- Build slots — empty plots where buildings can be constructed ----
    private void CreateBuildSlots(PrototypeGameController game)
    {
        var slots = new Vector3[]
        {
            new Vector3(-3.8f,  2.2f, 0f),   // top left
            new Vector3(-1.2f,  2.8f, 0f),   // top centre-left
            new Vector3( 1.4f,  2.4f, 0f),   // top centre-right
            new Vector3( 3.6f,  1.8f, 0f),   // top right
            new Vector3( 0.0f, -1.8f, 0f),   // bottom — Shipyard spot
        };

        foreach (var pos in slots)
        {
            var go   = new GameObject("BuildSlot");
            var slot = go.AddComponent<BuildSlot>();
            slot.Initialize(game, pos, new Vector2(1.6f, 1.6f));
            game.RegisterBuildSlot(slot);

            // "+" label so player knows it's buildable
            var lbl = new GameObject("SlotLabel");
            lbl.transform.SetParent(go.transform, false);
            lbl.transform.localPosition = new Vector3(0f, 0f, 0f);
            var tmp = lbl.AddComponent<TextMeshPro>();
            tmp.text        = "+";
            tmp.fontSize    = 2.8f;
            tmp.alignment   = TextAlignmentOptions.Center;
            tmp.color       = new Color(0.8f, 0.8f, 0.8f, 0.6f);
            tmp.rectTransform.sizeDelta = new Vector2(2f, 2f);
            tmp.sortingOrder = 6;
        }
    }

    private void CreateWorkerSpawnMarker()
    {
        var spawn = new GameObject("WorkerSpawn");
        spawn.transform.position   = new Vector3(-3.5f, -1.8f, 0f);
        spawn.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        var renderer  = spawn.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleShapeFactory.CreateFilledSquareSprite(new Color(0.15f, 0.45f, 0.85f, 1f));
        renderer.sortingOrder = 4;

        var lbl = new GameObject("SpawnLabel");
        lbl.transform.SetParent(spawn.transform, false);
        lbl.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        var tmp = lbl.AddComponent<TextMeshPro>();
        tmp.text        = "Worker Entry";
        tmp.fontSize    = 1.6f;
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.color       = Color.white;
        tmp.sortingOrder = 20;
    }

    private void SetupCameraAndBackground(out Camera mainCamera)
    {
        var cameraObject = new GameObject("Main Camera");
        mainCamera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        mainCamera.clearFlags       = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor  = new Color(0.09f, 0.28f, 0.52f, 1f);
        mainCamera.orthographic     = true;
        mainCamera.orthographicSize = 7f;
        cameraObject.transform.position = new Vector3(0f, 0.5f, -10f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<PrototypeCameraController>();

        var ocean   = new GameObject("Ocean");
        var oceanSR = ocean.AddComponent<SpriteRenderer>();
        oceanSR.sprite = SimpleShapeFactory.CreateFilledSquareSprite(new Color(0.09f, 0.28f, 0.52f, 1f));
        oceanSR.sortingOrder = -20;
        ocean.transform.localScale = new Vector3(60f, 40f, 1f);

        CreateIsland();
    }

    private void CreateIsland()
    {
        int size = 256;
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels       = new Color[size * size];
        var islandColor  = new Color(0.15f, 0.42f, 0.18f, 1f);
        var sandColor    = new Color(0.72f, 0.62f, 0.38f, 1f);
        float cx = size * 0.5f, cy = size * 0.5f;
        float r    = size * 0.46f;
        float sand = size * 0.42f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = dist < sand ? islandColor
                                      : dist < r   ? sandColor
                                      : new Color(0, 0, 0, 0);
            }

        tex.SetPixels(pixels);
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

        var islandGO   = new GameObject("Island");
        var sr         = islandGO.AddComponent<SpriteRenderer>();
        sr.sprite      = sprite;
        sr.sortingOrder = -10;
        islandGO.transform.position   = new Vector3(0f, 0.5f, 0f);
        islandGO.transform.localScale = new Vector3(13f, 10f, 1f);

        var dock   = new GameObject("Dock");
        var dockSR = dock.AddComponent<SpriteRenderer>();
        dockSR.sprite = SimpleShapeFactory.CreateFilledSquareSprite(new Color(0.45f, 0.35f, 0.20f, 1f));
        dockSR.sortingOrder = -9;
        dock.transform.position   = new Vector3(0f, -2.8f, 0f);
        dock.transform.localScale = new Vector3(3.5f, 1.2f, 1f);
    }

    private void CreateEventSystemIfMissing()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }
}
