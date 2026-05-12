using UnityEngine;

/// <summary>
/// An empty plot on the island. Shows a placeholder visual.
/// When selected, the UI offers available buildings to construct.
/// Transitions: Empty → UnderConstruction → Built (replaced by BuildingInstance).
/// </summary>
public class BuildSlot : MonoBehaviour
{
    public enum SlotState { Empty, UnderConstruction }

    public SlotState        State             { get; private set; } = SlotState.Empty;
    public BuildingType     QueuedType        { get; private set; }
    public float            ConstructionHoursRemaining { get; private set; }
    public float            ConstructionHoursTotal     { get; private set; }
    public float            ConstructionProgress =>
        ConstructionHoursTotal > 0
            ? 1f - ConstructionHoursRemaining / ConstructionHoursTotal
            : 0f;

    public Vector3 Position => transform.position;
    public Vector2 Size     { get; private set; }

    private SpriteRenderer          _renderer;
    private BuildingAnimator        _animator;
    private GameController _game;
    private Color                   _normalColor;
    private Color                   _selectedColor;
    private Color                   _constructionColor;

    // ---- Init ----

    public void Initialize(GameController game, Vector3 position, Vector2 size)
    {
        _game    = game;
        Size     = size;

        transform.position   = position;
        transform.localScale = new Vector3(size.x, size.y, 1f);

        _normalColor      = new Color(0.28f, 0.28f, 0.28f, 0.55f);
        _selectedColor    = new Color(0.55f, 0.55f, 0.30f, 0.80f);
        _constructionColor= new Color(0.60f, 0.45f, 0.15f, 0.75f);

        _renderer             = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite      = SimpleShapeFactory.CreateFilledSquareSprite(_normalColor);
        _renderer.sortingOrder = 4;

        var col  = gameObject.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        _animator = gameObject.AddComponent<BuildingAnimator>();
        _animator.Setup(BuildingType.Sawmill, _normalColor); // placeholder type, updated on build
    }

    private void Update()
    {
        if (_animator == null || State != SlotState.UnderConstruction) return;
        _animator.SetConstructionProgress(ConstructionProgress);
    }

    // ---- Selection ----

    public void SetSelected(bool selected)
    {
        if (State == SlotState.UnderConstruction) return; // keep construction colour
        _renderer.color = selected ? _selectedColor : _normalColor;
    }

    // ---- Construction ----

    /// <summary>Start construction. Caller must have already consumed resources.</summary>
    public void StartConstruction(BuildingType type)
    {
        var cost = BuildingCost.For(type);
        QueuedType                = type;
        ConstructionHoursTotal    = cost.Hours;
        ConstructionHoursRemaining = cost.Hours;
        State                     = SlotState.UnderConstruction;
        _renderer.color           = _constructionColor;
        _animator?.Setup(type, _constructionColor);
    }

    /// <summary>Restore construction state from save data (no resource cost).</summary>
    public void RestoreConstruction(BuildingType type, float hoursRemaining, float hoursTotal)
    {
        QueuedType                 = type;
        ConstructionHoursTotal     = hoursTotal;
        ConstructionHoursRemaining = hoursRemaining;
        State                      = SlotState.UnderConstruction;
        if (_renderer != null) _renderer.color = _constructionColor;
        _animator?.Setup(type, _constructionColor);
    }

    /// <summary>Called each in-game hour. Returns true when construction finishes.</summary>
    public bool TickHour()
    {
        if (State != SlotState.UnderConstruction) return false;
        ConstructionHoursRemaining = Mathf.Max(0f, ConstructionHoursRemaining - 1f);
        return ConstructionHoursRemaining <= 0f;
    }
}
