using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Ortografska kamera nad kartom otoka.
///
/// Granice se čitaju iz TilemapData (Width, Height, TileSize, Offset), ne iz
/// hardkodiranih brojeva — ako Tomislav promijeni dimenzije karte, kamera se
/// sama prilagodi.
///
/// Pravilo: kamera nikad ne prikazuje prazninu izvan karte. Maksimalni odzum je
/// zato ograničen omjerom stranica ekrana, a ne samo visinom karte.
/// </summary>
[RequireComponent(typeof(Camera))]
public class PrototypeCameraController : MonoBehaviour
{
    [Header("Kretanje")]
    [SerializeField] private float moveSpeed = 18f;

    [Header("Zoom")]
    [SerializeField] private float minZoom  = 4f;    // najviše približeno
    [SerializeField] private float zoomStep = 1.5f;  // promjena po jednom kliku kotačića
    [SerializeField] private float zoomLerp = 20f;   // brzina glađenja

    [Tooltip("false = kamera NIKAD ne izlazi iz karte (na max odzumu se odsiječe vrh i dno).\n" +
             "true  = na max odzumu se vidi CIJELA karta, ali se lijevo i desno vidi praznina.")]
    [SerializeField] private bool fitWholeMap = false;

    [Header("Fallback ako karta nije učitana")]
    [SerializeField] private Vector2 fallbackCenter   = Vector2.zero;
    [SerializeField] private Vector2 fallbackHalfSize = new Vector2(24f, 16f);

    private Camera  _camera;
    private float   _targetZoom;
    private Vector2 _center;   // središte karte u svjetskim koordinatama
    private Vector2 _half;     // pola širine i pola visine karte

    private void Awake() => _camera = GetComponent<Camera>();

    private void Start()
    {
        // Start() se izvršava nakon Bootstrapper.Awake(), pa je karta već učitana.
        ReadMapBounds();

        _targetZoom              = MaxZoom();
        _camera.orthographicSize = _targetZoom;
        transform.position       = new Vector3(_center.x, _center.y, transform.position.z);

        ClampPosition();
    }

    private void Update()
    {
        HandleMove();
        HandleZoom();
        ClampPosition();
    }

    // ---- Granice karte ----

    private void ReadMapBounds()
    {
        var map = IslandTilemapRenderer.Instance != null
                ? IslandTilemapRenderer.Instance.Map
                : null;

        if (map == null)
        {
            _center = fallbackCenter;
            _half   = fallbackHalfSize;
            return;
        }

        _half   = new Vector2(map.Width  * map.TileSize * 0.5f,
                              map.Height * map.TileSize * 0.5f);
        _center = map.Offset + _half;
    }

    /// <summary>
    /// Najveći orthographicSize pri kojem vidno polje još stane unutar karte.
    /// Pri 48x32 i omjeru 16:9 to je min(16, 24/1.778) = 13.5.
    /// </summary>
    private float MaxZoom()
    {
        float aspect   = Mathf.Max(0.01f, _camera.aspect);
        float byHeight = _half.y;             // ograničenje visinom karte
        float byWidth  = _half.x / aspect;    // ograničenje širinom karte

        float max = fitWholeMap ? Mathf.Max(byHeight, byWidth)
                                : Mathf.Min(byHeight, byWidth);

        return Mathf.Max(minZoom + 0.01f, max);
    }

    // ---- Ulaz ----

    private void HandleMove()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector2 move = Vector2.zero;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  move.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    move.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  move.y -= 1f;
        if (move == Vector2.zero) return;

        float speedScale = _camera.orthographicSize / 10f;
        transform.position += new Vector3(move.x, move.y, 0f).normalized
                              * moveSpeed * speedScale * Time.deltaTime;
    }

    private void HandleZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Ne zumiraj dok je kursor iznad UI-ja (npr. skrolanje liste brodova).
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        float scroll = mouse.scroll.ReadValue().y;

        // Sign, a ne sirova vrijednost: Input System vraća 120 po kliku kotačića,
        // pa bi jedan klik inače prebacio zoom s jednog kraja raspona na drugi.
        if (Mathf.Abs(scroll) > 0.01f)
            _targetZoom -= Mathf.Sign(scroll) * zoomStep;

        _targetZoom = Mathf.Clamp(_targetZoom, minZoom, MaxZoom());

        // Snap kad je dovoljno blizu — kontinuirana interpolacija ortografske
        // veličine stvara tanke linije između susjednih polja.
        if (Mathf.Abs(_camera.orthographicSize - _targetZoom) < 0.01f)
            _camera.orthographicSize = _targetZoom;
        else
            _camera.orthographicSize = Mathf.MoveTowards(
                _camera.orthographicSize, _targetZoom, zoomLerp * Time.deltaTime);
    }

    // ---- Ograničenje pozicije ----

    private void ClampPosition()
    {
        float h = _camera.orthographicSize;          // pola visine vidnog polja
        float w = h * _camera.aspect;                // pola širine vidnog polja

        // Ako je vidno polje šire/više od karte, centriraj tu os.
        float x = (w >= _half.x)
                ? _center.x
                : Mathf.Clamp(transform.position.x, _center.x - _half.x + w,
                                                    _center.x + _half.x - w);

        float y = (h >= _half.y)
                ? _center.y
                : Mathf.Clamp(transform.position.y, _center.y - _half.y + h,
                                                    _center.y + _half.y - h);

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
