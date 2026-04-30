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
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (FindFirstObjectByType<PrototypeBootstrapper>() != null) return;
        new GameObject("PrototypeBootstrapper").AddComponent<PrototypeBootstrapper>();
    }

    private void Awake()
    {
        Application.targetFrameRate = 120;
        Boot();
    }

    private void Boot()
    {
        EnsureEventSystem();

        // ---- Find scene objects ----
        var gameController = FindFirstObjectByType<PrototypeGameController>();
        var uiController   = FindFirstObjectByType<PrototypeUIController>();
        var selection      = FindFirstObjectByType<PrototypeSelectionController>();
        var mainCamera     = Camera.main;

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

        // ---- Tilemap ----
        SetupTilemap();

        // ---- UI ----
        if (uiController != null)
            uiController.Build(gameController, mainCamera);
        else
            Debug.LogWarning("[Bootstrapper] UIController not found. Run Ashes → Setup Scene.");

        // ---- Town Hall ----
        var townHallPos = FindTownHallPosition();
        var workerSprite = SimpleShapeFactory.CreateFilledTriangleSprite(new Color(1f, 0.9f, 0.25f, 1f));

        // Check if Town Hall already exists in scene
        var existingTownHall = FindFirstObjectByType<BuildingInstance>();
        bool hasTownHall = false;
        if (existingTownHall != null)
        {
            foreach (var b in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
                if (b.IsTownHall) { hasTownHall = true; break; }
        }

        gameController.Initialize(uiController, workerSprite, townHallPos);

        if (!hasTownHall)
            CreateTownHall(gameController, townHallPos);

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

        if (renderer.Map != null) return; // already initialized

        var map = Resources.Load<TilemapData>("IslandMap");
        if (map == null)
        {
            map = ScriptableObject.CreateInstance<TilemapData>();
            map.GeneratePlaceholder();
            Debug.LogWarning("[Bootstrapper] IslandMap.asset not found in Resources/. " +
                             "Using placeholder. Create via Assets > Create > Ashes > TilemapData.");
        }
        renderer.Initialize(map);
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

    private void CreateTownHall(PrototypeGameController game, Vector3 position)
    {
        var go       = new GameObject("TownHall");
        var building = go.AddComponent<BuildingInstance>();
        building.Initialize(game, "Town Hall", ResourceType.Wood,
            new Color(0.72f, 0.58f, 0.22f), position, new Vector2(1f, 1f), false);
        building.SetTownHall(true);
        game.RegisterBuilding(building);

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(go.transform, false);
        lbl.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        var tmp = lbl.AddComponent<TMPro.TextMeshPro>();
        tmp.text = "Town Hall"; tmp.fontSize = 1.8f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white; tmp.sortingOrder = 20;
        tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
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
