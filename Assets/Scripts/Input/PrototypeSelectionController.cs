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
            // I brod se mora odznaciti, inace bi ploca flote ostala otvorena
            // nakon klika u prazno.
            Game.SelectShip(null);
            Game.SelectBuilding(null);
            Game.SelectSlot(null);
            return;
        }

        var building = hit.GetComponent<BuildingInstance>();
        if (building != null)
        {
            // Town Hall se istim klikom i otvara i zatvara.
            if (building.IsTownHall && Game.GetSelectedBuilding() == building)
            {
                Game.SelectShip(null);
                Game.SelectSlot(null);
                Game.SelectBuilding(null);
                UISoundManager.Instance?.PlayButtonSound();
                return;
            }

            Game.SelectShip(null);
            Game.SelectSlot(null);
            Game.SelectBuilding(building);
            UISoundManager.Instance?.PlayButtonSound();
            return;
        }

        var slot = hit.GetComponent<BuildSlot>();
        if (slot != null)
        {
            Game.SelectBuilding(null);
            Game.SelectSlot(slot);
            UISoundManager.Instance?.PlayButtonSound();
            return;
        }

        var ship = hit.GetComponent<ShipInstance>();
        if (ship != null)
        {
            // Klik na brod otvara Manage fleet plocu na tom brodu. Uz brod se
            // odabire i brodogradiliste, pa gumb "< Back to fleet" ima kamo
            // voditi — bez toga bi povratak zatvorio cijelu plocu.
            Game.SelectSlot(null);
            Game.SelectBuilding(Game.GetShipyard());
            Game.SelectShip(ship);
            UISoundManager.Instance?.PlayButtonSound();
            return;
        }

        Game.SelectBuilding(null);
        Game.SelectSlot(null);
        Game.SelectShip(null);
    }
}
