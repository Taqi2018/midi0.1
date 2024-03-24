using System.Globalization;
using UnityEngine;

namespace DAW
{
    public class DAWInputMgr : MonoBehaviour
    {
        [Header("Touch Parameters")]
        Vector2 m_InitialTouchPosition;
        float m_InitialDistance;
        DAWEventMgr eventMgr;
        DAWUIMgr uiMgr;

        // Start is called before the first frame update
        void Start()
        {
            eventMgr = GetComponent<DAWEventMgr>();
            uiMgr = GetComponent<DAWUIMgr>();
        }

        // Update is called once per frame
        void Update()
        {
/*#if UNITY_ANDROID
            if (Input.touchCount == 2)
            {
                eventMgr.OnTouchEvent += ChangeEditorScaleWithSCroll;
            }
#endif
#if UNITY_EDITOR
            if (Input.GetKey(KeyCode.LeftControl))
            {
                float scrollInput = Input.GetAxis("Mouse ScrollWheel");
                if (scrollInput != 0)
                {
                    eventMgr.OnTouchEvent += ChangeEditorScaleWithSCroll;
                }
            }
#endif*/
        }

        void ChangeEditorScaleWithTouch()
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
            {
                // m_InitialTouchPosition = (touch1.position + touch2.position) / 2f;
                m_InitialDistance = Vector2.Distance(touch1.position, touch2.position);
            }

            if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                // Vector2 currentTouchPosition = (touch1.position + touch2.position) / 2f;
                float currentDistance = Vector2.Distance(touch1.position, touch2.position);
                float scaleFactor = currentDistance / m_InitialDistance;
                scaleFactor = scaleFactor > 1 ? 1f / scaleFactor * (-1) : scaleFactor;
                Global._trackHeight += 10 * scaleFactor;
                uiMgr.m_TrackEditPanel.SendMessage("ChangeKeyboardAndTrackHeight", SendMessageOptions.DontRequireReceiver);
            }
        }

    /*    void ChangeEditorScaleWithSCroll()
        {
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            Global._trackHeight += 10 * scrollDelta;
            uiMgr.m_TrackEditPanel.SendMessage("ChangeKeyboardAndTrackHeight", SendMessageOptions.DontRequireReceiver);
        }*/
    }
}
