using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OptimizedScrollview : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    protected int index = 0;
    public int getIndex() => index;
    public void setIndex(int inputIndex)
    {
        index = inputIndex;
        if (index > cellCount - cellCountInContent)
            index = cellCount - cellCountInContent;
        if (index < 0)
            index = 0;

        PositionRearrangement();
    }

    protected int cellCount = 0;
    protected int cellCountInViewport = 0;
    protected int cellCountInContent = 0;
    protected float cellSize;

    public bool isReverse = false;
    public RectTransform thisRect;
    public RectTransform viewport;
    public RectTransform content;
    float recoverySpeed = 22f;
    float cap_Hard_ContentPosValue = 100f;
    float cap_contentPosUP = 0f;
    Vector2 scrollDir = new Vector2(0, 1);

    //Input pos
    Vector2? originPos;
    Vector2 nowPos = new Vector2(0, 0);
    Vector2 originContentPos;
    Vector2 posDiff = new Vector2(0, 0);

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
        cap_Hard_ContentPosValue = Mathf.Min(cellSize, 120f);
        cellCountInViewport = 1;
        while (cellSize * cellCountInViewport < thisRect.sizeDelta.y)
            cellCountInViewport++;
        cellCountInContent = content.transform.childCount;
        scrollDir = new Vector2(0, 1);
    }

    protected void SetCell()
    {
        RecycleScrollRect thisRect = this.GetComponent<RecycleScrollRect>();
        for (int i = 0; i < content.childCount; i++)
            content.GetChild(i).GetComponent<AScrollCell>().init(thisRect, i + index);
    }

    public void OnPointerDown(PointerEventData data)
    {
        //PositionRearrangement();
        originPos = data.position;
        originContentPos = content.anchoredPosition;
    }

    public void OnPointerUp(PointerEventData data)
    {
        originPos = null;
        PositionRearrangement();
    }

    protected void PositionRearrangement()
    {
        float topSpace = content.anchoredPosition.y;
        float bottomSpace = content.sizeDelta.y - (content.anchoredPosition.y + thisRect.sizeDelta.y);

        if (index < cellCount - cellCountInContent && bottomSpace < cellSize / 2)
        {
            index += 1;
            content.anchoredPosition -= cellSize * scrollDir;
            SetCell();
        }
        else if (0 < index && topSpace < cellSize / 2)
        {
            index += -1;
            content.anchoredPosition += cellSize * scrollDir;
            SetCell();
        }
    }

    public void Refresh() => SetCell();
   
    private void LateUpdate()
    {
        if (thisRect == null)
            return;
        if (originPos != null)
        {
            if (!GetInputSetPos())
                return;

            posDiff = (nowPos - (Vector2)originPos) * scrollDir;
            if (isReverse)
                posDiff *= -1;

            if (posDiff.magnitude > cellSize)
            {
                if (CheckNextToPrev(isReverse))
                {
                    index--;
                    SetMoveFlagAndPos();
                }
                if (CheckPrevToNext(isReverse))
                {
                    index++;
                    SetMoveFlagAndPos();
                }
            }
            Vector2 destPos = originContentPos + posDiff;
            content.anchoredPosition = destPos;

            BlockPosHardCap();
        }
        else
            RecoveryPos();
    }


    protected bool GetInputSetPos()
    {
        if (!(Input.GetMouseButton(0) || Input.touchCount > 0))
        {
            originPos = null;
            return false;
        }
        if (Input.GetMouseButton(0)) nowPos = Input.mousePosition;
        if (Input.touchCount > 0) nowPos = Input.GetTouch(0).position;

        return true;
    }

    protected void BlockPosHardCap()
    {
        if (content.sizeDelta.y < thisRect.sizeDelta.y)
            content.anchoredPosition = new Vector2(0, 0);
        if (cap_contentPosUP <= 0f && (cap_contentPosUP = content.sizeDelta.y - thisRect.sizeDelta.y) <= 0f)
            return;

        if (content.anchoredPosition.y < -cap_Hard_ContentPosValue)
            content.anchoredPosition = new Vector2(0, -cap_Hard_ContentPosValue);
        if (content.anchoredPosition.y > cap_Hard_ContentPosValue + cap_contentPosUP)
            content.anchoredPosition = new Vector2(0, cap_Hard_ContentPosValue + cap_contentPosUP);

        // It Blocked But Not End of Index;
        if (index > 0 && content.anchoredPosition.y <= -cap_Hard_ContentPosValue)
        {
            index--;
            content.anchoredPosition += cellSize * scrollDir;
            SetCell();
        }
        if (index < cellCount - cellCountInContent && content.anchoredPosition.y >= cap_Hard_ContentPosValue + cap_contentPosUP)
        {
            index++;
            content.anchoredPosition -= cellSize * scrollDir;
            SetCell();
        }
        SetCell();
    }

    protected void RecoveryPos()
    {
        if (cap_contentPosUP <= 0f)
            cap_contentPosUP = content.sizeDelta.y - thisRect.sizeDelta.y;
        if (cap_contentPosUP <= 0f)
            return;

        if (content.anchoredPosition.y < 0)
            content.anchoredPosition += scrollDir * recoverySpeed;
        if (content.anchoredPosition.y > cap_contentPosUP)
            content.anchoredPosition -= scrollDir * recoverySpeed;
    }

    protected bool CheckNextToPrev(bool isReverse)
    {
        if (isReverse && ((Vector2)originPos).y < nowPos.y && index > 0)
            return true;
        if (!isReverse && ((Vector2)originPos).y > nowPos.y && index > 0)
            return true;
        return false;
    }

    protected bool CheckPrevToNext(bool isReverse)
    {
        if (isReverse && ((Vector2)originPos).y > nowPos.y && index < cellCount - cellCountInContent)
            return true;
        if (!isReverse && ((Vector2)originPos).y < nowPos.y && index < cellCount - cellCountInContent)
            return true;
        return false;
    }

    protected void SetMoveFlagAndPos()
    {
        originPos = nowPos;
        posDiff = new Vector2(0, 0);
        SetCell();
    }
}
