using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Initializes gameplay systems at runtime.
/// All GameObjects (Camera, Canvas, UI) are created by the SceneBootstrapper
/// Editor tool (Ashes → Setup Scene) and persist in the scene.
/// This script only wires references and starts the game loop.
/// </summary>
public class PrototypeBootstrapper : MonoBehaviour
{
    private EventManager eventManager;

    // PrototypeBootstrapper must exist as a GameObject in the Game scene.
    // Add it via: Hierarchy → Create Empty → Add Component → PrototypeBootstrapper
    // It will call Boot() automatically on Awake every time the scene loads.

    private void Awake()
    {
        Debug.Log($"[Bootstrapper] Awake — scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Application.targetFrameRate = 120;
        Boot();
    }

    private void Update()
    {
        eventManager?.Update();
    }

    private void Boot()
    {
        EnsureEventSystem();

        // ---- Find scene objects ----
        var gameController = FindFirstObjectByType<GameController>();
        var uiController   = FindFirstObjectByType<UIController>();
        var selection      = FindFirstObjectByType<PrototypeSelectionController>();

        // Camera.main requires tag "MainCamera" — find directly if null
        var mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();

        if (gameController == null)
        {
            Debug.LogError("[Bootstrapper] GameController not found. Run Ashes → Setup Scene first.");
            return;
        }

        // ---- Sprite Registry ----
        var registry = Resources.Load<SpriteRegistry>("SpriteRegistry");
        registry?.Register();

        // ---- Placement Validator ----
        if (FindFirstObjectByType<PlacementValidator>() == null)
            new GameObject("PlacementValidator").AddComponent<PlacementValidator>();

        // ---- Tilemap FIRST — needed for FindTownHallPosition ----
        SetupTilemap();
        var tilemapCheck = IslandTilemapRenderer.Instance;
        Debug.Log($"[Bootstrapper] TilemapRenderer.Instance = {tilemapCheck}, Map = {tilemapCheck?.Map}");

        // ---- Town Hall ----
        var townHallPos  = FindTownHallPosition();
        Debug.Log($"[Bootstrapper] TownHallPos = {townHallPos}, Camera = {mainCamera}");
        var workerSprite = SimpleShapeFactory.CreateFilledTriangleSprite(new Color(1f, 0.9f, 0.25f, 1f));

        bool hasTownHall = false;
        foreach (var b in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            if (b.IsTownHall) { hasTownHall = true; break; }

        // ---- GameController init ----
        gameController.Initialize(uiController, workerSprite, townHallPos);

        if (!hasTownHall)
            CreateTownHall(gameController, townHallPos);

        // ---- UI (after GameController so Build() can wire game references) ----
        if (uiController != null)
            uiController.Build(gameController, mainCamera);
        else
            Debug.LogWarning("[Bootstrapper] UIController not found. Run Ashes → Setup Scene.");

        // ---- Events ----
        eventManager = new EventManager();
        eventManager.Initialize(gameController);
        gameController.DayEnding += eventManager.TryOpenEvent;

        // ---- Selection Controller ----
        if (selection != null)
            selection.Initialize(gameController, mainCamera);
        else
            Debug.LogWarning("[Bootstrapper] SelectionController not found. Run Ashes → Setup Scene.");
    }

    // ---- Tilemap ----

    private void SetupTilemap()
    {
        var renderer = FindFirstObjectByType<IslandTilemapRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning("[Bootstrapper] IslandTilemapRenderer not found. Run Ashes → Setup Scene.");
            return;
        }

        // Always reinitialize — map may be null when loading from MainMenu
        var map = Resources.Load<TilemapData>("IslandMap");
        if (map == null)
        {
            map = ScriptableObject.CreateInstance<TilemapData>();
            map.GeneratePlaceholder();
            Debug.LogWarning("[Bootstrapper] IslandMap.asset not found in Resources/. " +
                             "Using placeholder. Create via Assets > Create > Ashes > TilemapData.");
        }
        renderer.Initialize(map);

        // Details/Objects vizualni slojevi za Tomislava — na ISTOM Gridu kao Ground,
        // pa mora doci nakon Initialize() jer tada renderer.WorldGrid vec postoji.
        UnityTilemapSetup.CreateDecorationLayers();

        // Vulkan je jedan veliki animirani objekt, ne tile. Vraca null ako karta
        // nema Volcano polja ili ako VolcanoFrames u SpriteRegistryju nisu popunjeni.
        VolcanoRenderer.Spawn(renderer.Map);
    }

    // ---- Town Hall ----

    private static Vector3 FindTownHallPosition()
    {
        var renderer = IslandTilemapRenderer.Instance;
        if (renderer == null || renderer.Map == null)
            return new Vector3(0f, 0f, 0f);

        var map = renderer.Map;
        int cx = map.Width / 2, cy = map.Height / 2;
        int best = int.MaxValue, bestC = cx, bestR = cy;

        for (int radius = 0; radius < Mathf.Max(map.Width, map.Height); radius++)
        for (int row = cy - radius; row <= cy + radius; row++)
        for (int col = cx - radius; col <= cx + radius; col++)
        {
            if (!map.InBounds(col, row)) continue;
            if (map.GetTile(col, row) != TileType.Land) continue;
            int dist = (col-cx)*(col-cx) + (row-cy)*(row-cy);
            if (dist < best) { best = dist; bestC = col; bestR = row; }
        }

        var world = map.TileToWorld(bestC, bestR);
        return new Vector3(world.x, world.y, 0f);
    }

    private void CreateTownHall(GameController game, Vector3 position)
    {
        var townHall = BuildingFactory.Create(game, BuildingType.TownHall, position, new Vector2(1f, 1f));
        game.RegisterBuilding(townHall);
    }

    // ---- Helpers ----

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }
}
