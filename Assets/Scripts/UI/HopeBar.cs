using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shows the current hope value and its exact percentage on hover.
/// </summary>
public class HopeBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image            _fill;
    [SerializeField] private TextMeshProUGUI _labelText;
    [SerializeField] private float            _animationSpeed = 100f;

    private float _targetHope = 100f;
    private float _shownHope  = 100f;

    private void Awake()
    {
        SetFill(_shownHope);
    }

    private void Update()
    {
        if (Mathf.Approximately(_shownHope, _targetHope)) return;

        _shownHope = Mathf.MoveTowards(
            _shownHope, _targetHope, _animationSpeed * Time.unscaledDeltaTime);
        SetFill(_shownHope);
    }

    public void SetHope(float value)
    {
        _targetHope = Mathf.Clamp(value, 0f, 100f);
        if (_labelText != null && _labelText.text != "HOPE")
            _labelText.text = $"{_targetHope:F0}%";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_labelText != null)
            _labelText.text = $"{_targetHope:F0}%";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_labelText != null)
            _labelText.text = "HOPE";
    }

    private void SetFill(float value)
    {
        if (_fill != null) _fill.fillAmount = value / 100f;
    }
}
