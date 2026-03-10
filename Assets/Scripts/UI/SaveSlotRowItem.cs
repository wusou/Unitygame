using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SaveSlotRowItem : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    private int slotIndex;
    private float doubleClickInterval = 0.35f;
    private float lastLeftClickTime = -10f;

    private Action<int> onLeftClick;
    private Action<int> onDoubleClick;
    private Action<int, Vector2> onRightClick;

    public void Configure(
        int index,
        float doubleClickSeconds,
        Action<int> leftClick,
        Action<int> doubleClick,
        Action<int, Vector2> rightClick)
    {
        slotIndex = index;
        doubleClickInterval = Mathf.Max(0.15f, doubleClickSeconds);
        onLeftClick = leftClick;
        onDoubleClick = doubleClick;
        onRightClick = rightClick;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        var now = Time.unscaledTime;
        if (now - lastLeftClickTime <= doubleClickInterval)
        {
            onDoubleClick?.Invoke(slotIndex);
            lastLeftClickTime = -10f;
            return;
        }

        lastLeftClickTime = now;
        onLeftClick?.Invoke(slotIndex);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        onRightClick?.Invoke(slotIndex, eventData.position);
        eventData.Use();
    }
}
