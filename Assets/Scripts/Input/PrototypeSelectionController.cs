using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PrototypeSelectionController : MonoBehaviour
{
    private PrototypeGameController _game;
    private Camera _camera;

    public void Initialize(PrototypeGameController game, Camera cameraComponent)
    {
        _game   = game;
        _camera = cameraComponent;
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 worldPoint = _camera.ScreenToWorldPoint(mouse.position.ReadValue());
        Collider2D hit     = Physics2D.OverlapPoint(worldPoint);

        if (hit == null)
        {
            _game.SelectBuilding(null);
            _game.SelectSlot(null);
            return;
        }

        var building = hit.GetComponent<BuildingInstance>();
        if (building != null)
        {
            _game.SelectShip(null);
            _game.SelectSlot(null);
            _game.SelectBuilding(building);
            return;
        }

        var slot = hit.GetComponent<BuildSlot>();
        if (slot != null)
        {
            _game.SelectBuilding(null);
            _game.SelectSlot(slot);
            return;
        }

        var ship = hit.GetComponent<ShipInstance>();
        if (ship != null)
        {
            _game.SelectShip(null);
            _game.SelectBuilding(_game.GetShipyard());
            return;
        }

        _game.SelectBuilding(null);
        _game.SelectSlot(null);
        _game.SelectShip(null);
    }
}
