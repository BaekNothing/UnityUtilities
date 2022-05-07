using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OptimizedScrollview : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] protected int index = 0;
    public int getIndex() => index;
    public void setIndex(int inputIndex)
    {
        index = inputIndex;
        if (index > cellCount - cellCountInContent)
            index = cellCount - cellCountInContent;
    }

    protected int cellCount = 0;
    protected int cellCountInViewport = 0;
    protected int cellCountInContent = 0;
    
    float cellSize;

    public bool isReverse = false;
    public RectTransform thisRect;
    public RectTransform viewport;
    public RectTransform content;
    float recoverySpeed = 15f;
    float cap_Hard_ContentPosValue = 100f;
    float cap_contentPosUP = 0f;
    Vector2 scrollDir = new Vector2(0, 1);

    //Input pos
    Vector2? originPos;
    Vector2 nowPos = new Vector2(0, 0);
    Vector2 originContentPos;

    //private void Start() => init();
    public void Init(int inputCellCount)
    {
        if (thisRect == null)
            thisRect = this.GetComponent<RectTransform>();
        if(viewport == null)
            viewport = this.transform.Find("Viewport").GetComponent<RectTransform>();
        if (content == null)
            content = viewport.Find("Content").GetComponent<RectTransform>();

        cellCount = inputCellCount;
        cellSize = content.GetChild(0).GetComponent<RectTransform>().sizeDelta.y;
        cap_Hard_ContentPosValue = cellSize + cellSize * 0.5f;
        cellCountInViewport = 1;
        while (cellSize * cellCountInViewport < thisRect.sizeDelta.y)
            cellCountInViewport++;
        cellCountInContent = content.transform.childCount;
        scrollDir = new Vector2(0, 1);
        SetCell();
    }

    protected void SetCell()
    {
        RecycleScrollRect thisRect = this.GetComponent<RecycleScrollRect>();
        for (int i = 0; i < content.childCount; i++)
            content.GetChild(i).GetComponent<AScrollCell>().init(thisRect, i + index);
    }

    public void OnPointerDown(PointerEventData data)
    {
        originPos = data.position;
        originContentPos = content.anchoredPosition;
    }

    public void OnPointerUp(PointerEventData data) => originPos = null;

    public void Refresh() => SetCell();
    
   
    private void LateUpdate()
    {
        if (thisRect == null)
            return;
        if (originPos != null)
        {
            if (!(Input.GetMouseButton(0) || Input.touchCount > 0))
            {
                originPos = null;
                return;
            }
            if (Input.GetMouseButton(0)) nowPos = Input.mousePosition;
            if (Input.touchCount > 0) nowPos = Input.GetTouch(0).position;

            if (cap_contentPosUP <= 0f)
                cap_contentPosUP = content.sizeDelta.y - thisRect.sizeDelta.y;
            Vector2 posDiff = (nowPos - (Vector2)originPos) * scrollDir;
            if (isReverse)
                posDiff *= -1;

            if (posDiff.magnitude > cellSize && !isReverse)
            {
                if (((Vector2)originPos).y > nowPos.y && index > 0)
                {
                    index--;
                    originPos = nowPos;
                    posDiff = new Vector2(0, 0);
                    SetCell();
                }
                if (((Vector2)originPos).y < nowPos.y && index < cellCount - cellCountInContent)
                {
                    index++;
                    originPos = nowPos;
                    posDiff = new Vector2(0, 0);
                    SetCell();
                }
            }
            if (posDiff.magnitude > cellSize && isReverse)
            {
                if (((Vector2)originPos).y < nowPos.y && index > 0)
                {
                    index--;
                    originPos = nowPos;
                    posDiff = new Vector2(0, 0);
                    SetCell();
                }
                if (((Vector2)originPos).y > nowPos.y && index < cellCount - cellCountInContent)
                {
                    index++;
                    originPos = nowPos;
                    posDiff = new Vector2(0, 0);
                    SetCell();
                }
            }

            Vector2 destPos = originContentPos + posDiff;
            content.anchoredPosition = destPos;

            if (content.anchoredPosition.y < -cap_Hard_ContentPosValue)
                content.anchoredPosition = new Vector2(0, -cap_Hard_ContentPosValue);
            if(content.anchoredPosition.y > cap_Hard_ContentPosValue + cap_contentPosUP)
                content.anchoredPosition = new Vector2(0, cap_Hard_ContentPosValue + cap_contentPosUP);
        }
        else
        {
            if (content.anchoredPosition.y < 0)
                content.anchoredPosition += scrollDir * recoverySpeed;
            if (content.anchoredPosition.y > cap_contentPosUP)
                content.anchoredPosition -= scrollDir * recoverySpeed;
        }
    }
}
