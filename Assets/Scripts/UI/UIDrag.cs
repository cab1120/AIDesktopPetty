using UnityEngine;
using UnityEngine.EventSystems;

public class UIDrag : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    private Vector2 offset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransform rectTransform = transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out offset
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform rectTransform = transform as RectTransform;

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        rectTransform.localPosition = localPoint - offset;
    }
}