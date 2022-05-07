using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class AOptimizedScrollCell : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    protected RecycleScrollRect scrollRect;
    public bool active = true;

    public  abstract void init(RecycleScrollRect parentScroll, int index);

    public void OnPointerDown(PointerEventData data) => scrollRect.OnPointerDown(data);
    public void OnPointerUp(PointerEventData data) => scrollRect.OnPointerUp(data);
    public bool isActive() => active;
}
