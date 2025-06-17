using UnityEngine;
using UnityEngine.EventSystems;

public class CustomButton : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    public bool IsPressed {  get; private set; }

    public void OnPointerDown(PointerEventData pointerEventData)
    {
        IsPressed = true;
    }

    public void OnPointerUp(PointerEventData pointerEventData)
    {
        IsPressed = false;
    }
}