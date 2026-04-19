using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class PrototypeCameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 12f;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _camera.orthographic = true;
        _camera.orthographicSize = 6f;
        transform.position = new Vector3(0f, 0f, -10f);
    }

    private void Update()
    {
        Vector2 move = Vector2.zero;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
        }

        Vector3 delta = new Vector3(move.x, move.y, 0f).normalized * moveSpeed * Time.deltaTime;
        transform.position += delta;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _camera.orthographicSize -= scroll * 0.01f * zoomSpeed;
                _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minZoom, maxZoom);
            }
        }
    }
}
