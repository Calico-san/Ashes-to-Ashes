using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PrototypeSelectionController : MonoBehaviour
{
    private GameController _game;
    private Camera                  _camera;

    // Lazy lookup — works even if Initialize() wasn't called yet
    private Camera Cam =>
        _camera != null ? _camera : (_camera = Camera.main ?? FindFirstObjectByType<Camera>());
    private GameController Game =>
        _game != null ? _game : (_game = FindFirstObjectByType<GameController>());

    public void Initialize(GameController game, Camera cameraComponent)
    {
        _game   = game;
        _camera = cameraComponent;
    }

    private void Update()
    {
        if (Cam == null || Game == null) return;

        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var buildPanel = FindFirstObjectByType<BuildPanel>();
        if (buildPanel != null && buildPanel.IsPlacing) return;

        Vector2    worldPoint = Cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Collider2D hit        = Physics2D.OverlapPoint(worldPoint);

        if (hit == null)
        {
            Game.SelectBuilding(null);
            Game.SelectSlot(null);
            return;
        }

        var building = hit.GetComponent<BuildingInstance>();
        if (building != null)
        {
            Game.SelectShip(null);
            Game.SelectSlot(null);
            Game.SelectBuilding(building);
            return;
        }

        var slot = hit.GetComponent<BuildSlot>();
        if (slot != null)
        {
            Game.SelectBuilding(null);
            Game.SelectSlot(slot);
            return;
        }

        var ship = hit.GetComponent<ShipInstance>();
        if (ship != null)
        {
            Game.SelectShip(null);
            Game.SelectBuilding(Game.GetShipyard());
            return;
        }

        Game.SelectBuilding(null);
        Game.SelectSlot(null);
        Game.SelectShip(null);
    }
}
