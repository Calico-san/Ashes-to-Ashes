using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animates a single worker walking to/from a building or ship.
/// Lifecycle: spawn at SpawnPoint → walk to slot → hide inside → walk back → destroy.
/// </summary>
public class WorkerAgent : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;

    // Visina lika u svjetskim jedinicama (polje je 1). Velicina se postavlja
    // preko SpriteFita, ne preko localScale — inace bi o njoj odlucivao PPU iz
    // import postavki, pa bi sprite od 16px bio cetiri puta manji od onog od 64px.
    [SerializeField] private float visualSize = 0.45f;

    private enum State { Entering, Inside, Leaving, Done }

    private State   _state = State.Entering;
    private Vector3 _target;
    private List<Vector3> _path;

    public bool IsInside => _state == State.Inside;
    public bool IsDone   => _state == State.Done;

    private SpriteRenderer _renderer;
    private Sprite[]       _walkFrames;
    private float          _walkFps = 6f;
    private float          _walkTimer;
    private int            _walkFrame;

    public void Initialize(Vector3 startPos, Vector3 targetPos, Sprite sprite,
                           System.Collections.Generic.List<UnityEngine.Vector3> path = null)
    {
        _path      = path;
        transform.position = startPos;
        _target = targetPos;
        _state  = State.Entering;

        _renderer             = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite      = sprite;
        _renderer.sortingOrder = 15;
        transform.localScale  = Vector3.one;
        SpriteFit.FitInside(_renderer, visualSize);
    }

    /// <summary>
    /// Ukljucuje animaciju hoda. Frameove daje pozivatelj (BuildingInstance) jer
    /// radnik i inzenjer koriste razlicite setove iz SpriteRegistryja.
    /// </summary>
    public void SetWalkFrames(Sprite[] frames, float fps)
    {
        _walkFrames = frames;
        _walkFps    = fps > 0f ? fps : 6f;
    }

    /// <summary>Reassign slot (e.g. after another worker is removed).</summary>
    public void SetNewTarget(Vector3 target)
    {
        _target = target;
        _state  = State.Entering;
        SetVisible(true);
    }

    /// <summary>Begin exit animation toward exitTarget; destroys self on arrival.</summary>
    public void LeaveBuilding(Vector3 exitTarget)
    {
        _target = exitTarget;
        _state  = State.Leaving;
        SetVisible(true);
    }

    private void Update()
    {
        if (_state == State.Inside || _state == State.Done) return;

        transform.position = Vector3.MoveTowards(transform.position, _target, moveSpeed * Time.deltaTime);
        TickWalkAnimation();

        if (Vector3.Distance(transform.position, _target) > 0.02f) return;

        transform.position = _target;

        if (_state == State.Entering)
        {
            _state = State.Inside;
            SetVisible(false);
        }
        else if (_state == State.Leaving)
        {
            _state = State.Done;
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Izmjena framea hoda. Velicina se cuva jer SpriteFit koristi Sliced nacin,
    /// a zamjena sprajta bi inace vratila renderer na prirodnu velicinu.
    /// </summary>
    private void TickWalkAnimation()
    {
        if (_renderer == null || _walkFrames == null || _walkFrames.Length < 2) return;

        float frameTime = 1f / Mathf.Max(0.1f, _walkFps);
        _walkTimer += Time.deltaTime;

        while (_walkTimer >= frameTime)
        {
            _walkTimer -= frameTime;
            _walkFrame++;
        }

        var next = _walkFrames[((_walkFrame % _walkFrames.Length) + _walkFrames.Length) % _walkFrames.Length];
        if (next == null || next == _renderer.sprite) return;

        var size = _renderer.size;
        _renderer.sprite = next;
        _renderer.size   = size;
    }

    private void SetVisible(bool visible)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = visible;
    }
}
