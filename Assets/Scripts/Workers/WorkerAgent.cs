using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animates a single worker walking to/from a building or ship.
/// Lifecycle: spawn at SpawnPoint → walk to slot → hide inside → walk back → destroy.
/// </summary>
public class WorkerAgent : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;

    private enum State { Entering, Inside, Leaving, Done }

    private State   _state = State.Entering;
    private Vector3 _target;
    private List<Vector3> _path;
    private int           _pathIndex;

    public bool IsInside => _state == State.Inside;
    public bool IsDone   => _state == State.Done;

    public void Initialize(Vector3 startPos, Vector3 targetPos, Sprite sprite,
                           System.Collections.Generic.List<UnityEngine.Vector3> path = null)
    {
        _path      = path;
        _pathIndex = 0;
        transform.position = startPos;
        _target = targetPos;
        _state  = State.Entering;

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 15;
        transform.localScale = new Vector3(0.45f, 0.45f, 1f);
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

    private void SetVisible(bool visible)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = visible;
    }
}
