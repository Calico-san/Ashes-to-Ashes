using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class PrototypeCameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed  = 18f;
    [SerializeField] private float zoomSpeed  = 6f;
    [SerializeField] private float minZoom    = 4f;
    [SerializeField] private float maxZoom    = 16f;  // 32 tiles / 2 = full map height visible

    private Camera _camera;
    private float  _targetZoom;

    private void Awake() => _camera = GetComponent<Camera>();

    private void Start()
    {
        _targetZoom = _camera.orthographicSize;
    }

    private void Update()
    {
        HandleMove();
        HandleZoom();
        ClampPosition();
    }

    private void HandleMove()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector2 move = Vector2.zero;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  move.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    move.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  move.y -= 1f;

        float speedScale = _camera.orthographicSize / 10f;
        transform.position += new Vector3(move.x, move.y, 0f).normalized
                              * moveSpeed * speedScale * Time.deltaTime;
    }

    private void HandleZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            _targetZoom -= scroll * zoomSpeed * 0.1f;

        _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);

        // Snap to target when close enough to avoid tiling artefacts
        if (Mathf.Abs(_camera.orthographicSize - _targetZoom) < 0.01f)
            _camera.orthographicSize = _targetZoom;
        else
            _camera.orthographicSize = Mathf.MoveTowards(
                _camera.orthographicSize, _targetZoom, 20f * Time.deltaTime);
    }

    private void ClampPosition()
    {
        float h = _camera.orthographicSize;
        float w = h * _camera.aspect;

        float x = Mathf.Clamp(transform.position.x, -24f + w,  24f - w);
        float y = Mathf.Clamp(transform.position.y, -16f + h,  16f - h);

        // Ako je mapa manja od viewporta, centriraj
        if (w >= 24f) x = 0f;
        if (h >= 16f) y = 0f;

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
