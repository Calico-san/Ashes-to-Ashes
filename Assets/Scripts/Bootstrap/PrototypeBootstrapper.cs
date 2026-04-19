using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class PrototypeBootstrapper : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (FindFirstObjectByType<PrototypeBootstrapper>() != null)
        {
            return;
        }

        var root = new GameObject("PrototypeBootstrapper");
        root.AddComponent<PrototypeBootstrapper>();
    }

    private void Awake()
    {
        Application.targetFrameRate = 120;
        SetupScene();
    }

    private void SetupScene()
    {
        CreateEventSystemIfMissing();
        SetupCameraAndBackground(out Camera mainCamera);

        var gameController = new GameObject("GameController").AddComponent<PrototypeGameController>();
        var uiController = new GameObject("UIController").AddComponent<PrototypeUIController>();
        uiController.Build(gameController);

        Sprite workerSprite = SimpleShapeFactory.CreateFilledTriangleSprite(new Color(1f, 0.9f, 0.25f, 1f));
        gameController.Initialize(uiController, workerSprite, new Vector3(-3.5f, -1.8f, 0f));

        CreateBuildings(gameController);
        CreateWorkerSpawnMarker();

        var selection = new GameObject("SelectionController").AddComponent<PrototypeSelectionController>();
        selection.Initialize(gameController, mainCamera);
    }

    private void CreateBuildings(PrototypeGameController gameController)
    {
        // Zgrade raspoređene prirodno po otoku
        // Gornji red — lijevo na otoku
        CreateBuilding(gameController, "Sawmill",    ResourceType.Wood,  new Color(0.22f, 0.65f, 0.25f), new Vector3(-3.8f,  2.2f, 0f), new Vector2(1.6f, 1.6f));
        CreateBuilding(gameController, "Steelworks", ResourceType.Steel, new Color(0.55f, 0.55f, 0.58f), new Vector3(-1.2f,  2.8f, 0f), new Vector2(1.6f, 1.6f));
        // Desno gornji
        CreateBuilding(gameController, "Cloth Works",ResourceType.Cloth, new Color(0.70f, 0.44f, 0.74f), new Vector3( 1.4f,  2.4f, 0f), new Vector2(1.6f, 1.6f));
        CreateBuilding(gameController, "Cookhouse",  ResourceType.Food,  new Color(0.82f, 0.53f, 0.20f), new Vector3( 3.6f,  1.8f, 0f), new Vector2(1.6f, 1.6f));
        // Shipyard pri luci — dnu otoka
        CreateBuilding(gameController, "Shipyard",   ResourceType.Ships, new Color(0.25f, 0.38f, 0.82f), new Vector3( 0.0f, -1.8f, 0f), new Vector2(2.2f, 1.8f), true);
    }

    private void CreateBuilding(PrototypeGameController gameController, string displayName, ResourceType outputType, Color color, Vector3 position, Vector2 size, bool isShipyard = false)
    {
        var buildingObject = new GameObject(displayName);
        var building = buildingObject.AddComponent<BuildingInstance>();
        building.Initialize(gameController, displayName, outputType, color, position, size, isShipyard);
        gameController.RegisterBuilding(building);

        var labelObject = new GameObject(displayName + "Label");
        labelObject.transform.SetParent(buildingObject.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        var text = labelObject.AddComponent<TextMeshPro>();
        text.text = displayName;
        text.fontSize = 1.8f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.rectTransform.sizeDelta = new Vector2(4f, 1f);
        text.sortingOrder = 20;
    }

    private void CreateWorkerSpawnMarker()
    {
        var spawn = new GameObject("WorkerSpawn");
        spawn.transform.position = new Vector3(-3.5f, -1.8f, 0f);

        var renderer = spawn.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleShapeFactory.CreateFilledSquareSprite(new Color(0.15f, 0.45f, 0.85f, 1f));
        renderer.sortingOrder = 4;
        spawn.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        var labelObject = new GameObject("SpawnLabel");
        labelObject.transform.SetParent(spawn.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        var text = labelObject.AddComponent<TextMeshPro>();
        text.text = "Worker Entry";
        text.fontSize = 1.6f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.sortingOrder = 20;
    }

    private void SetupCameraAndBackground(out Camera mainCamera)
    {
        // Kamera — centrirana na otoku, vidi sve zgrade
        var cameraObject = new GameObject("Main Camera");
        mainCamera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.09f, 0.28f, 0.52f, 1f); // ocean boja
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 7f;
        cameraObject.transform.position = new Vector3(0f, 0.5f, -10f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<PrototypeCameraController>();

        // Ocean — pokriva sve iza otoka (ista boja kao kamera bg, samo za slojeve)
        var ocean = new GameObject("Ocean");
        var oceanSR = ocean.AddComponent<SpriteRenderer>();
        oceanSR.sprite = SimpleShapeFactory.CreateFilledSquareSprite(new Color(0.09f, 0.28f, 0.52f, 1f));
        oceanSR.sortingOrder = -20;
        ocean.transform.localScale = new Vector3(60f, 40f, 1f);

        // Otok — ovalni zeleni oblik
        CreateIsland();
    }

    private void CreateIsland()
    {
        // Glavni ovalni otok — generiramo kružni sprite i skaliramo ga na oval
        int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        var islandColor   = new Color(0.15f, 0.42f, 0.18f, 1f);
        var sandColor     = new Color(0.72f, 0.62f, 0.38f, 1f);
        float cx = size * 0.5f, cy = size * 0.5f;
        float r = size * 0.46f;   // radius glavnog otoka
        float sand = size * 0.42f; // sand ring

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist < sand)
                    pixels[y * size + x] = islandColor;
                else if (dist < r)
                    pixels[y * size + x] = sandColor;
                else
                    pixels[y * size + x] = new Color(0,0,0,0);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), size);

        // Otok malo ovalan — širi po X
        var islandGO = new GameObject("Island");
        var sr = islandGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10;
        islandGO.transform.position   = new Vector3(0f, 0.5f, 0f);
        islandGO.transform.localScale  = new Vector3(13f, 10f, 1f);

        // Luka/pristanište — tamniji pravokutnik pri dnu otoka (za Shipyard)
        var dock = new GameObject("Dock");
        var dockSR = dock.AddComponent<SpriteRenderer>();
        dockSR.sprite = SimpleShapeFactory.CreateFilledSquareSprite(new Color(0.45f, 0.35f, 0.20f, 1f));
        dockSR.sortingOrder = -9;
        dock.transform.position   = new Vector3(0f, -2.8f, 0f);
        dock.transform.localScale  = new Vector3(3.5f, 1.2f, 1f);
    }

    private void CreateEventSystemIfMissing()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }
}
