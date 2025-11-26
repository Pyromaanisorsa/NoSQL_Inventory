using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomButton : Button
{
    public delegate void RightClickHandler();
    public event RightClickHandler OnRightClick;

    public delegate void DoubleClickHandler();
    public event DoubleClickHandler OnDoubleClick;

    public float lastClickTimer = 0f;
    private const float doubleClickThreshold = 0.3f;

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        // Button not interactable; don't run doubleClick and rightClick checks either
        if (interactable == false)
            return;

        if (eventData.button == PointerEventData.InputButton.Left) 
        {
            float timeSinceLastClick = Time.time - lastClickTimer;
            lastClickTimer = Time.time;

            if(timeSinceLastClick <= doubleClickThreshold) 
            {
                OnDoubleClick?.Invoke();
            }
        }
        else if(eventData.button == PointerEventData.InputButton.Right) 
        {
            OnRightClick?.Invoke();
        }
    }
}
