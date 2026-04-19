using UnityEngine;

/// <summary>
/// Stavlja se na svaki SpawnShipVisual GameObject.
/// Kada igrač klikne na brod, PrototypeSelectionController
/// pronalazi ovu komponentu i selektira Shipyard.
/// </summary>
public class ShipReference : MonoBehaviour
{
    public BuildingInstance Shipyard { get; set; }
}
