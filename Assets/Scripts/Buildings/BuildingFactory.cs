using UnityEngine;

/// <summary>
/// Creates building GameObjects (sprite, collider and animator are set up inside
/// BuildingInstance.Initialize) plus their floating name label.
///
/// Pure construction — does NOT touch GameController state. The caller is responsible
/// for registering the result and restoring workers, so all "how to spawn a building
/// in the world" knowledge lives in one place instead of being duplicated across
/// GameController.CompleteBuild, GameController.ApplyLoadData and the Bootstrapper.
/// </summary>
public static class BuildingFactory
{
    private const float LabelHeight    = 0.85f;
    private const float TownHallLabelY = 0.65f;
    private const float LabelFontSize  = 1.8f;
    private const int   LabelSortOrder = 20;

    /// <summary>Display name, output resource and tint for a building type.</summary>
    public static (string name, ResourceType type, Color color) Meta(BuildingType t)
    {
        switch (t)
        {
            case BuildingType.Sawmill:      return ("Sawmill",      ResourceType.Wood,  new Color(0.22f, 0.65f, 0.25f));
            case BuildingType.Steelworks:   return ("Steelworks",   ResourceType.Steel, new Color(0.55f, 0.55f, 0.58f));
            case BuildingType.Fiberworks:   return ("Fiberworks",   ResourceType.Cloth, new Color(0.70f, 0.44f, 0.74f));
            case BuildingType.Cookhouse:    return ("Cookhouse",    ResourceType.Food,  new Color(0.82f, 0.53f, 0.20f));
            case BuildingType.Shipyard:     return ("Shipyard",     ResourceType.Ships, new Color(0.25f, 0.38f, 0.82f));
            case BuildingType.HuntersHut:   return ("Hunters Hut",  ResourceType.Food,  new Color(0.55f, 0.35f, 0.15f));
            case BuildingType.ScoutStation: return ("Scout Station", ResourceType.Food, new Color(0.20f, 0.50f, 0.50f));
            case BuildingType.TownHall:     return ("Town Hall",    ResourceType.Wood,  new Color(0.72f, 0.58f, 0.22f));
            default:                        return ("Building",     ResourceType.Wood,  Color.white);
        }
    }

    /// <summary>
    /// Spawn a building of <paramref name="type"/> at a world position, with its label.
    /// Returns the BuildingInstance — the caller must register it via
    /// GameController.RegisterBuilding and restore any workers.
    /// </summary>
    public static BuildingInstance Create(GameController game, BuildingType type, Vector3 position, Vector2 size)
    {
        var (displayName, outputType, color) = Meta(type);
        bool isShipyard = type == BuildingType.Shipyard;

        var go       = new GameObject(displayName);
        var building = go.AddComponent<BuildingInstance>();
        building.Initialize(game, displayName, outputType, color, position, size, isShipyard, type);

        if (type == BuildingType.TownHall)
            building.SetTownHall(true);

        AttachLabel(go.transform, displayName,
            type == BuildingType.TownHall ? TownHallLabelY : LabelHeight);

        return building;
    }

    /// <summary>Floating white name label above a building.</summary>
    private static void AttachLabel(Transform parent, string text, float localY)
    {
        var lbl = new GameObject(text + "Label");
        lbl.transform.SetParent(parent, false);
        lbl.transform.localPosition = new Vector3(0f, localY, 0f);

        var tmp = lbl.AddComponent<TMPro.TextMeshPro>();
        tmp.text                    = text;
        tmp.fontSize                = LabelFontSize;
        tmp.alignment               = TMPro.TextAlignmentOptions.Center;
        tmp.color                   = Color.white;
        tmp.sortingOrder            = LabelSortOrder;
        tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
    }
}
