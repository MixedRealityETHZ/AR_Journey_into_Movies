using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class LongPressDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float holdTime = 1f;  // 长按 1 秒触发
    private float timer = 0;
    private bool pressing = false;

    public Action onLongPress;

    public void OnPointerDown(PointerEventData eventData)
    {
        pressing = true;
        timer = 0;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressing = false;
    }

    void Update()
    {
        if (pressing)
        {
            timer += Time.deltaTime;
            if (timer >= holdTime)
            {
                pressing = false;
                onLongPress?.Invoke();
            }
        }
    }
}