using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using DAW.CommonClasses;
using System;
/*using Unity.PlasticSCM.Editor.WebApi;*/
namespace DAW
{
    [RequireComponent(typeof(Image))]
    public class NoteCellUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [SerializeField] Color m_SelectedContentColor = Color.white;
        [SerializeField] Color m_DefaultContentColor = Color.blue;
        Image m_CellImage;
        public NoteCellInfo cellInfo = new NoteCellInfo();
        public bool m_IsSelected;
        public bool m_IsDragged;

        RectTransform m_MyRectTrans;
        Vector2 initialOffsetMin;
        Vector2 initialOffsetMax;
        Vector2 initialMousePos;
        SideType side;
        NoteCellContentUI contentUI;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        void OnEnable()
        {
            FindComponents();
            InitVariable();
        }

        // Update is called once per frame
        void Update()
        {

        }

        void InitVariable()
        {
            m_IsSelected = false;
            m_IsDragged = false;
            m_SelectedContentColor = Color.white;
            m_DefaultContentColor = Color.blue;
        }

        void FindComponents()
        {
            if (!m_MyRectTrans) m_MyRectTrans = GetComponent<RectTransform>();
            if (!m_CellImage) m_CellImage = GetComponent<Image>();
            if (!contentUI) contentUI = GetComponentInChildren<NoteCellContentUI>();
        }

        public void InitNoteCell(NoteCellInfo data)
        {
            cellInfo = data;
            OnSelectItem();
        }

        public void OnPointerDown(PointerEventData data)
        {
            transform.SetAsLastSibling();
            Vector2 trackLocalPos = transform.parent.GetComponent<RectTransform>().InverseTransformPoint(data.position);
            side = GetSide(trackLocalPos);
            float newX = /*Global.GetSnapPosX(trackLocalPos.x, side);*/trackLocalPos.x;
            initialMousePos = new Vector2(newX, trackLocalPos.y);
            OnSelectItem();
            // save myself in Global
        }

        public void OnDrag(PointerEventData data)
        {
            Debug.Log($"dragging start, side: {side}, dragged: {m_IsDragged}");
            if (!m_IsDragged) return;
            if (side == SideType.NONE) return;
            Vector2 trackLocalPos = transform.parent.GetComponent<RectTransform>().InverseTransformPoint(data.position);
            float lastX = /*Global.GetSnapPosX(trackLocalPos.x, side);*/ trackLocalPos.x;

            if (Mathf.Abs(lastX - initialMousePos.x) == 0) return;
            Global.SetCurSnapWidthByDragging(initialMousePos.x, lastX, side);
            if (side == SideType.LEFT)
            {
                if (m_MyRectTrans.offsetMax.x - lastX <= 0) lastX = m_MyRectTrans.offsetMax.x - Global._xNote16th;
                m_MyRectTrans.offsetMin = new Vector2(lastX, 0f);  // Set bottom offset to 0
                m_MyRectTrans.offsetMax = new Vector2(m_MyRectTrans.offsetMax.x, 0f);  // Set top offset to 0
                /*Debug.Log($"lastx: {lastX}, offsetMax: {m_MyRectTrans.offsetMax.x}, calMax: {lastX + Global._curSnapWidth}");*/
            }
            else
            {
                if (lastX - m_MyRectTrans.offsetMin.x <= 0) lastX = m_MyRectTrans.offsetMin.x + Global._xNote16th;
                m_MyRectTrans.offsetMin = new Vector2(m_MyRectTrans.offsetMin.x, 0f);  // Set bottom offset to 0
                m_MyRectTrans.offsetMax = new Vector2(lastX, 0f);  // Set top offset to 0
            }
            Global._engine.EventResizeMidiEvent(m_MyRectTrans.offsetMin.x, m_MyRectTrans.offsetMax.x - m_MyRectTrans.offsetMin.x);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (m_IsDragged) return;
            m_IsDragged = true;
            initialOffsetMin = m_MyRectTrans.offsetMin;
            initialOffsetMax = m_MyRectTrans.offsetMax;
            /*side = GetSide(initialMousePos);*/
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (!m_IsDragged) return;
            m_IsDragged = false;
        }

        SideType GetSide(Vector2 pos)
        {
            float imageSize = m_MyRectTrans.rect.width;
            Vector2 imagePosition = m_MyRectTrans.transform.localPosition;
            // Debug.Log($"mouse: {pos}, image: {imagePosition}, size:{imageSize}");
            float right = Mathf.Abs(imagePosition.x + imageSize - pos.x);
            float left = Mathf.Abs(imagePosition.x - pos.x);
            if (right > left && left <= 15) return SideType.LEFT;
            if (right < left && right <= 15) return SideType.RIGHT;
            return SideType.NONE;
        }

        // if b is true, the state is selected, else deselected
        public void SetSelectedState(int cellIndex)
        {
            m_IsSelected = (cellIndex == cellInfo.index);
            // Debug.Log($"SetSelectedState: {cellIndex}, {m_IsSelected}");
            if (contentUI == null) FindComponents();
            Image contentImage = contentUI.GetComponent<Image>();
            m_CellImage.color = m_IsSelected ? m_DefaultContentColor : m_SelectedContentColor;
            contentImage.color = m_IsSelected ? m_SelectedContentColor : m_DefaultContentColor;
        }

        public void OnSelectItem(){
            TabParentUI tapParent = GetComponentInParent<TabParentUI>();
            tapParent.OnSelectItem(cellInfo.index);
            Global._engine.SelectMidiEvent(cellInfo.index);
        }
    }
}