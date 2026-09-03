using UnityEngine;
using UnityEngine.EventSystems;

public class EventOptionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UniversalPopup popup;
    private int optionIndex;

    public void Setup(UniversalPopup popupController, int index)
    {
        popup = popupController;
        optionIndex = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        popup.ShowExplanation(optionIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        popup.HideExplanation();
    }
}
