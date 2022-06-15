using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
    * Cell 들을 Area로 묶어 1개의 기본 Area와 2개의 동적으로 생성된 Area를 가지고 재활용 하며 Cell을 보여 주는 클래스
    * 
    * 사용 방법 : 
    * 1. Content 오브젝트 안에 viewport가 꽉차게 cell 들을 배치, 이 때 cell들은 ICell을 상속받았어야 합니다.
    * 2. 보여줘야 할 Cell 들의 내용을 연결해 줄 DataConnector를 RecyclingScrollRect의 컴포넌트에 추가
    * 
    */
public class OptimizedScrollview : ScrollRect
{
    readonly private int RecyclingAreaCount = 3;

    private IDataConnector _dataConnector;
    private List<List<Tuple<RectTransform, ICell>>> _cellAreas = new List<List<Tuple<RectTransform, ICell>>>();
    private float _thresholdPrevArea = 0f;
    private float _thresholdNextArea = 0f;
    private float _prevPos = 0f;
    private float _areaSize = 0f;
    private float _cellSize = 0f;
    private int _cellCountPerArea = 0;
    private int _curDataAreaIndex = 0;
    public int GetCurIndex() => _curDataAreaIndex;
    private int _lastDataAreaIndex = 0;
    private int _dataCellCount = 0;
    private bool _isRecycling = false;
    private Vector2 _scrollDir = Vector2.zero;
    private Vector2 _firstCellPos = Vector2.zero;

    private bool _isInitialize = false;

    // 현재 보이고 있는 Cell 들의 데이터를 갱신한다.
    public void RefreshCell()
    {
        if(_dataConnector == null)
        {
            throw new UnityException("No Data Connector : " + gameObject.name);
        }
        if (_cellAreas.Count < RecyclingAreaCount)
            return;

        int refreshAreaIndex = _curDataAreaIndex % 3;
        int cellIndex = refreshAreaIndex * _cellCountPerArea;

        if (_curDataAreaIndex == 0)
        {
            refreshAreaIndex = 0;
            cellIndex = 0;
        }
        else if(_curDataAreaIndex == _lastDataAreaIndex)
        {
            refreshAreaIndex = (_curDataAreaIndex - 2) % RecyclingAreaCount;
            cellIndex = (_curDataAreaIndex - 2) * _cellCountPerArea;
        }
        else
        {
            var curAreaPos = (_cellAreas[_curDataAreaIndex % 3][0].Item1.anchoredPosition * _scrollDir).magnitude;
            var prevAreaEndPos = (_cellAreas[(_curDataAreaIndex - 1) % 3][_cellCountPerArea - 1].Item1.anchoredPosition * _scrollDir).magnitude; 
                
            if(curAreaPos > prevAreaEndPos)
            {
                refreshAreaIndex = (_curDataAreaIndex - 1) % 3;
                cellIndex = (_curDataAreaIndex - 1) * _cellCountPerArea;
            }
        }

        for (int i = 0; i < RecyclingAreaCount; i++)
        {
            foreach (var cell in _cellAreas[(refreshAreaIndex + i) % RecyclingAreaCount])
            {
                _dataConnector.SetCell(cell.Item2, cellIndex++);
            }
        }
    }


    bool isForceMove = false;
    public void ForceSetScrollIndex(int cellIndex)
    {
        if (!isForceMove)
        {
            isForceMove = true;
            StartCoroutine(ForceMove(cellIndex));
        }
    }
    
    
    IEnumerator ForceMove(int cellIndex)
    {
        int direction = 0;
        movementType = MovementType.Clamped;
        int targetIndex = cellIndex / _cellCountPerArea;
        int stopper = 0;

        while (targetIndex != _curDataAreaIndex)
        {
            if (stopper++ > 2000)
                break;
            if (targetIndex < _curDataAreaIndex)
            {
                content.anchoredPosition -= _cellSize * _scrollDir;
                direction = 1;
            }
            else
            {
                content.anchoredPosition += _cellSize * _scrollDir;
                direction = -1;
            }
            yield return new WaitForEndOfFrame();
        }
        if(direction == 1)
            content.anchoredPosition -= _cellSize * _scrollDir;
        else if (direction == -1)
            content.anchoredPosition += _cellSize * _scrollDir;

        if (_scrollDir == new Vector2(0, 1))
            content.anchoredPosition += -direction * (content.anchoredPosition.y % _cellSize) * _scrollDir;

        yield return new WaitForEndOfFrame();
        isForceMove = false;
        movementType = MovementType.Elastic;
    }
    
    // Cell의 개수가 바뀌었을 때 호출
    public void OnChangedCellCount()
    {
        MoveToInitPos();
        _dataCellCount = _dataConnector.GetItemCount();
        _lastDataAreaIndex = _dataCellCount / _cellCountPerArea;
        SetContentSize();
        SetActiveInViewport();
    }

    public void OnChangeCellCountWithoutMovePos()
    {
        _dataCellCount = _dataConnector.GetItemCount();
        _lastDataAreaIndex = _dataCellCount / _cellCountPerArea;
        SetContentSize();
        SetActiveInViewport();
    }

    // Cell이 Viewport 안에 있는지 검사
    public bool IsInVeiwport(RectTransform cellObject)
    {
        if(_areaSize == 0)
            return true;
        //Vector2 sizeDelta = new Vector2(this.GetComponent<RectTransform>().rect.width, this.GetComponent<RectTransform>().rect.height);

        var viewportSize = (viewport.rect.size * _scrollDir).magnitude;
        var contentStartPos = (this.content.anchoredPosition * _scrollDir).magnitude;

        var cellRectStart = (cellObject.anchoredPosition * _scrollDir).magnitude - _cellSize * 0.5f - contentStartPos;
        var cellRectEnd = (cellObject.anchoredPosition * _scrollDir).magnitude + _cellSize * 0.5f - contentStartPos;
            
        if ((cellRectEnd > 0 && cellRectEnd < viewportSize) || (cellRectStart < viewportSize && cellRectStart > 0))
            return true;
            
        return false;
    }

    // DataConnector가 연결되어 있는지 검사
    public bool IsBindedDataConnector()
    {
        return _dataConnector != null ? true : false;
    }

    // 현재 보이고 있는 영역을 첫 번째로 이동
    public void MoveToInitPos()
    {
        if (_curDataAreaIndex != 0)
        {
            if (_curDataAreaIndex == _lastDataAreaIndex)
            {
                this.content.sizeDelta += Vector2.Scale(_scrollDir, _scrollDir) * _cellSize * (_cellCountPerArea - (_dataCellCount % _cellCountPerArea));
                _curDataAreaIndex--;
            }
            int trasitionAreaIndex = (_curDataAreaIndex) % RecyclingAreaCount;
            Vector2 cellMoveVec = _areaSize * 2 * _scrollDir;

            if (trasitionAreaIndex == 2)
            {
                foreach (var cell in _cellAreas[0])
                {
                    cell.Item1.anchoredPosition += cellMoveVec;
                }
                cellMoveVec = _areaSize * _scrollDir;
                for (int i = 1; i < RecyclingAreaCount; i++)
                {
                    foreach (var cell in _cellAreas[i])
                    {
                        cell.Item1.anchoredPosition -= cellMoveVec;
                    }
                }
            }
            else if(trasitionAreaIndex == 0)
            {
                cellMoveVec *= -1;
                foreach (var cell in _cellAreas[2])
                {
                    cell.Item1.anchoredPosition += cellMoveVec;
                }
                cellMoveVec = _areaSize * _scrollDir * -1;
                for (int i = 0; i < 2; i++)
                {
                    foreach (var cell in _cellAreas[i])
                    {
                        cell.Item1.anchoredPosition -= cellMoveVec;
                    }
                }
            }
        }

        m_ContentStartPosition = Vector2.zero;
        content.anchoredPosition = Vector2.zero;

        _curDataAreaIndex = 0;
        _prevPos = 0;

        m_ContentBounds.center = m_ContentBounds.extents * _scrollDir;
    }

    //protected override void Awake()
    //{
    //    base.Awake();

    //    SetScrollDir();

    //    if (Application.isPlaying == true)
    //    {
    //        onValueChanged.RemoveAllListeners();
    //        onValueChanged.AddListener(onItemScrolled);

    //        _curDataAreaIndex = 0;

    //        BindDataConnector();

    //        var cellObjects = GetCellObjects();

    //        _cellCountPerArea = cellObjects.Count;

    //        _areaSize = _cellSize * _cellCountPerArea;
    //        _lastDataAreaIndex = _dataCellCount / _cellCountPerArea;

    //        CopyCellArea(cellObjects);
    //    }
    //}

    //protected override void Start()
    //{
    //    base.Start();

    //    if(Application.isPlaying)
    //    {
    //        SetContentSize();
    //        RefreshCell();
    //        SetActiveInViewport();
    //    }
    //}

    public void Initialize()
    {
        if (_isInitialize) return;

        SetScrollDir();

        if (Application.isPlaying == true)
        {
            onValueChanged.RemoveAllListeners();
            onValueChanged.AddListener(onItemScrolled);

            _curDataAreaIndex = 0;

            BindDataConnector();

            var cellObjects = GetCellObjects();

            _cellCountPerArea = cellObjects.Count;

            _areaSize = _cellSize * _cellCountPerArea;
            _lastDataAreaIndex = _dataCellCount / _cellCountPerArea;

            CopyCellArea(cellObjects);
        }

        if (Application.isPlaying)
        {
            SetContentSize();
            RefreshCell();
            SetActiveInViewport();
        }

        _isInitialize = true;
    }

    private void SetScrollDir()
    {
        if (vertical ^ horizontal == false)
        {
            throw new UnityException("Recycling Scroll Rect can have only one direction(" + this.name + ")");
        }
        if (vertical == true)
        {
            _scrollDir = Vector2.up;
        }
        else if (horizontal == true)
        {
            _scrollDir = Vector2.left;
        }
    }

    // Content Rect의 사이즈를 조절
    private void SetContentSize()
    {
        var absScrollDir = _scrollDir * _scrollDir;

        this.content.sizeDelta -= this.content.sizeDelta * absScrollDir;

        LayoutGroup layoutGroup = null;
        if (this.content.TryGetComponent<LayoutGroup>(out layoutGroup) == true)
        {
            if (absScrollDir.x == 0)
            {
                this.content.sizeDelta += layoutGroup.padding.vertical * absScrollDir;
            }
            else
            {
                this.content.sizeDelta += layoutGroup.padding.horizontal * absScrollDir;
            }
            layoutGroup.enabled = false;
        }

        // 데이터 아레아의 마지막 인덱스가 3보다 적다면 컨텐츠 사이즈를 필요한 만큼한 할당
        if (_lastDataAreaIndex < RecyclingAreaCount)
        {
            this.content.sizeDelta += _cellSize * _dataCellCount * absScrollDir;
            _thresholdPrevArea = 0;
            _thresholdNextArea = _areaSize * RecyclingAreaCount * 3;
        }
        else
        {
            this.content.sizeDelta += absScrollDir * _areaSize * RecyclingAreaCount;
            _thresholdPrevArea = _areaSize * 0.5f;
            _thresholdNextArea = _areaSize * 2.5f;
        }
    }

    private List<RectTransform> GetCellObjects()
    {
        List<RectTransform> cellObjects = new List<RectTransform>();

        if (this.content.childCount < 2)
        {
            if (this.content.childCount == 0)
            {
                throw new UnityException("Recycling Scroll Rect의 Content 자식이 없습니다.");
            }
            _cellSize = (this.content.GetChild(0).GetComponent<RectTransform>().sizeDelta * _scrollDir).magnitude;
        }
        else
        {
            _cellSize = (this.content.GetChild(1).GetComponent<RectTransform>().anchoredPosition - this.content.GetChild(0).GetComponent<RectTransform>().anchoredPosition).magnitude;
        }

        _firstCellPos = this.content.GetChild(0).GetComponent<RectTransform>().anchoredPosition;

        for (int i = 0; i < this.content.childCount; i++)
        {
            var cellObject = this.content.GetChild(i).GetComponent<RectTransform>();
            if (IsInVeiwport(cellObject) == false || i > _dataCellCount)
            {
                cellObject.gameObject.SetActive(false);
            }
            else
            {
                cellObjects.Add(cellObject);
            }
        }

        return cellObjects;
    }
    private void BindDataConnector()
    {
        if (TryGetComponent<IDataConnector>(out _dataConnector) == false)
        {
            throw new UnityException($"No DataConnector in Recycling Scroll Rect : {this.name}");
        }

        _dataConnector.StartDataConnect();
        _dataCellCount = _dataConnector.GetItemCount();
    }


    private void CopyCellArea(List<RectTransform> cellObjects)
    {
        if(cellObjects.Count == 0)
        {
            Debug.Log("cellObjects Count is zero");
            return;
        }
        LayoutGroup layoutGroup = null;
        if (this.content.TryGetComponent<LayoutGroup>(out layoutGroup) == true)
        {
            layoutGroup.enabled = false;
        }

        ICell cell = null;

        _cellAreas.Add(new List<Tuple<RectTransform, ICell>>());

        foreach (var cellObject in cellObjects)
        {
            cell = cellObject.GetComponentInChildren<ICell>();
            _cellAreas[0].Add(new Tuple<RectTransform, ICell>(cellObject, cell));
        }

        int copiedCellCount = 0;

        for (int areaIndex = 1; areaIndex < RecyclingAreaCount; areaIndex++)
        {
            _cellAreas.Add(new List<Tuple<RectTransform, ICell>>());

            var cellMoveDist = _cellAreas[0].Count * areaIndex * _cellSize;

            foreach (var cellObject in _cellAreas[0])
            {
                var copiedCell = GameObject.Instantiate(cellObject.Item1.gameObject, this.content);
                copiedCellCount++;
                var copiedCellTransform = copiedCell.GetComponent<RectTransform>();
                copiedCellTransform.anchoredPosition -= _scrollDir * cellMoveDist;

                cell = copiedCell.GetComponentInChildren<ICell>();
                _cellAreas[areaIndex].Add(new Tuple<RectTransform, ICell>(copiedCellTransform, cell));
            }
        }
    }

    protected void onItemScrolled(Vector2 pos)
    {
        if (_isRecycling == true)
        {
            return;
        }

        _isRecycling = true;

        SetActiveInViewport();

        bool isPrevToNextScroll = _prevPos - (this.content.anchoredPosition * _scrollDir).magnitude < 0 ? true : false;
        if (isPrevToNextScroll == true)
        {
            ScrollPrevToNext();
        }
        else
        {
            ScrollNextToPrev();
        }

        _prevPos = (this.content.anchoredPosition * _scrollDir).magnitude;
        _isRecycling = false;
        //RefreshCell();
    }

    // viewport 안에 있는 cell들을 활성화, 밖에 있으면 비활성화
    private void SetActiveInViewport()
    {
        foreach (var cellArea in _cellAreas)
        {
            foreach (var cell in cellArea)
            {
                if(IsInVeiwport(cell.Item1))
                {
                    cell.Item1.gameObject.SetActive(cell.Item2.GetIsActive());
                }
                else
                {
                    cell.Item1.gameObject.SetActive(false);
                }
            }
        }
    }

    private void ScrollPrevToNext()
    {
        if (_curDataAreaIndex == _lastDataAreaIndex)
            return;

        if ((this.content.anchoredPosition * _scrollDir).magnitude + _areaSize > _thresholdNextArea)
        {
            if (_curDataAreaIndex == 0)
            {
                _curDataAreaIndex++;
            }

            var transitionAreaIndex = (_curDataAreaIndex - 1) % RecyclingAreaCount;
            _curDataAreaIndex++;

            int cellIndex = (_curDataAreaIndex + 1) * _cellCountPerArea;

            if (_curDataAreaIndex + 1 == _lastDataAreaIndex)
            {
                _curDataAreaIndex++;
                this.content.sizeDelta -= Vector2.Scale(_scrollDir, _scrollDir) * _cellSize * (_cellCountPerArea - (_dataCellCount % _cellCountPerArea));
            }

            var cellMoveVec = _scrollDir * _areaSize * RecyclingAreaCount;
                
            foreach (var cell in _cellAreas[transitionAreaIndex])
            {
                cell.Item1.anchoredPosition -= cellMoveVec;
                _dataConnector.SetCell(cell.Item2, cellIndex++);
            }

            cellMoveVec = _scrollDir * _areaSize;

            foreach (var cellArea in _cellAreas)
            {
                foreach (var cell in cellArea)
                {
                    cell.Item1.anchoredPosition += cellMoveVec;
                }
            }
            this.content.anchoredPosition -= cellMoveVec;
            m_ContentStartPosition -= cellMoveVec;
            RefreshCell();
        }
    }

    private void ScrollNextToPrev()
    {
        if ((this.content.anchoredPosition * _scrollDir).magnitude < _thresholdPrevArea)
        {
            if (_curDataAreaIndex < 2)
            {
                if (_curDataAreaIndex < 0)
                {
                    throw new UnityException("Area Index < 0");
                }
                _curDataAreaIndex = 0;
                return;
            }
            else if (_curDataAreaIndex == _lastDataAreaIndex)
            {
                _curDataAreaIndex--;
                this.content.sizeDelta += Vector2.Scale(_scrollDir, _scrollDir) * _cellSize * (_cellCountPerArea - (_dataCellCount % _cellCountPerArea));
            }

            var transitionAreaIndex = (_curDataAreaIndex + 1) % RecyclingAreaCount;
            _curDataAreaIndex--;

            var cellIndex = (_curDataAreaIndex - 1) * _cellCountPerArea;
            var cellMoveVec = _scrollDir * _areaSize * RecyclingAreaCount;

            foreach (var cell in _cellAreas[transitionAreaIndex])
            {
                cell.Item1.anchoredPosition += cellMoveVec;
                _dataConnector.SetCell(cell.Item2, cellIndex++);
            }

            cellMoveVec = _scrollDir * _areaSize;

            foreach (var cellArea in _cellAreas)
            {
                foreach (var cell in cellArea)
                {
                    cell.Item1.anchoredPosition -= cellMoveVec;
                }
            }

            this.content.anchoredPosition += cellMoveVec;
            m_ContentStartPosition += cellMoveVec;
            RefreshCell();
        }
    }
}
