using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DAW
{
    public class NoteCellContentUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IPointerDownHandler
    {
        public Vector2 m_InitialMousePos;
        Vector2 m_InitialRectTransformPos;
        public RectTransform m_MyRectTrans;
        // Start is called before the first frame update
        void Start()
        {
            m_MyRectTrans = transform.parent.GetComponent<RectTransform>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Store the initial mouse and RectTransform positions
            m_InitialMousePos = eventData.position;
            m_InitialRectTransformPos = m_MyRectTrans.anchoredPosition;
            m_MyRectTrans.transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData data)
        {
            // Calculate the difference in mouse position
            Vector2 localMousePosition = m_MyRectTrans.transform.parent.GetComponent<RectTransform>().InverseTransformPoint(data.position);
            MoveCell(localMousePosition);
        }

        public void MoveCell(Vector2 pos){
            /*pos.x = pos.x * Screen.currentResolution.width / Screen.width;
            pos.y = pos.y * Screen.currentResolution.height / Screen.height;*/
            float newX = Global.GetSnapPosX(pos.x);
            if (newX == m_MyRectTrans.transform.position.x) return;
            m_MyRectTrans.transform.localPosition = new Vector2(newX, 0f);
            Global._engine.EventMoveMidiEvent(m_MyRectTrans.GetComponent<NoteCellUI>().cellInfo.midiNote, newX);
        }

        public void OnPointerDown(PointerEventData data){
            NoteCellUI parentUI = m_MyRectTrans.GetComponent<NoteCellUI>();
            if(!parentUI) return;
            if (parentUI.m_IsSelected) return;
            parentUI.OnSelectItem();
        }
    }
}
