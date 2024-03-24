using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DAW.CommonClasses;

namespace DAW
{
    public class TrackUI : MonoBehaviour, IDropHandler, IPointerDownHandler, IPointerEnterHandler
    {
        Image trackImg;
        public TrackInfo trackInfo;
        [SerializeField] Color blackTrackColor = new Color(15, 17, 32, 1f);
        [SerializeField] Color whiteTrackColor = new Color(31, 32, 38, 1f);
        // Start is called before the first frame update
        void Start()
        {
            trackImg = GetComponent<Image>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnEnable()
        {
            FindComponents();
            InitVariables();
        }

        void FindComponents()
        {
            if (!trackImg) trackImg = GetComponent<Image>();
        }

        void InitVariables()
        {
            if (blackTrackColor != Color.white) return;
            blackTrackColor = new Color(15, 17, 32, 1f);
            whiteTrackColor = new Color(31, 32, 38, 1f);
        }

        public void InitTrack(TrackInfo t)
        {
            FindComponents();
            InitVariables();
            trackInfo = t;
            // Debug.Log($"InitTrack trackInfo = {t.ToString()}");
            if (t.areaType == AreaType.BlackKeys)
            {
                trackImg.color = blackTrackColor;
            }
            else
            {
                trackImg.color = whiteTrackColor;
            }
        }

        public void OnDrop(PointerEventData data)
        {

        }

        public void OnPointerDown(PointerEventData data)
        {
            // if(data.pointerDrag) return;
            CreateSnap(data.position);
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (data.pointerDrag == null) return;

            NoteCellContentUI noteContentUI = data.pointerDrag.GetComponent<NoteCellContentUI>();
            if (!noteContentUI) return;

            /*Vector2 delta = data.position - noteCellUI.m_InitialMousePos;*/
            if (data.pointerDrag.transform.parent.parent == transform) return;
            noteContentUI.transform.parent.SetParent(transform);
            Vector2 localMousePosition = GetComponent<RectTransform>().InverseTransformPoint(data.position);
            NoteCellUI noteUI = noteContentUI.m_MyRectTrans.GetComponent<NoteCellUI>();
            noteUI.cellInfo = new NoteCellInfo(trackInfo.midiNote, noteUI.cellInfo.index);
            noteContentUI.MoveCell(localMousePosition);
            Debug.Log($"minx; {noteContentUI.transform.parent.GetComponent<RectTransform>().offsetMin.x}, maxx: {noteContentUI.transform.parent.GetComponent<RectTransform>().offsetMax.x}, pos: {localMousePosition}");
            /*RectTransform noteRectTrans = noteCellUI.GetComponent<RectTransform>();
            noteRectTrans.offsetMin = new Vector2(noteRectTrans.offsetMin.x, 0f);  // Set bottom offset to 0
            noteRectTrans.offsetMax = new Vector2(noteRectTrans.offsetMax.x, 0f);  // Set top offset to 0*/
        }

        public void CreateSnap(Vector2 pos, float width = -1f, bool isLocalPos = false) {
            GameObject noteObj = GameObject.Instantiate(transform.GetChild(0).gameObject);
            noteObj.transform.SetParent(transform);
            noteObj.transform.localScale = Vector3.one;
            NoteCellUI noteUI = noteObj.GetComponent<NoteCellUI>();
            noteObj.SetActive(true);

            if (width < 0) width = Global._curSnapWidth;
            Vector2 localMousePosition = pos;
            if (!isLocalPos)
                localMousePosition = GetComponent<RectTransform>().InverseTransformPoint(pos);
          /*  float estimatedX = Global.GetSnapPosX(localMousePosition.x);*/
            RectTransform noteRectTrans = noteObj.GetComponent<RectTransform>();
            noteRectTrans.offsetMin = new Vector2(localMousePosition.x, 0f);  // Set bottom offset to 0
            noteRectTrans.offsetMax = new Vector2(localMousePosition.x + width, 0f);  // Set top offset to 0
            Global._noteCell = new NoteCellInfo(trackInfo.midiNote, -1);
            noteUI.InitNoteCell(Global._noteCell);
            Global._engine.EventCreateMidiEvent(Global._noteCell.index, trackInfo.midiNote, localMousePosition.x, width);
        }

        public void DrawSnap(int index, Vector2 pos, float width = -1f, bool isLocalPos = false)
        {
            GameObject noteObj = GameObject.Instantiate(transform.GetChild(0).gameObject);
            noteObj.transform.SetParent(transform);
            noteObj.transform.localScale = Vector3.one;
            NoteCellUI noteUI = noteObj.GetComponent<NoteCellUI>();
            noteObj.SetActive(true);

            if (width < 0) width = Global._curSnapWidth;
            Vector2 localMousePosition = pos;
            if (!isLocalPos)
                localMousePosition = GetComponent<RectTransform>().InverseTransformPoint(pos);
            float estimatedX = Global.GetSnapPosX(localMousePosition.x);
            RectTransform noteRectTrans = noteObj.GetComponent<RectTransform>();
            noteRectTrans.offsetMin = new Vector2(estimatedX, 0f);  // Set bottom offset to 0
            noteRectTrans.offsetMax = new Vector2(estimatedX + width, 0f);  // Set top offset to 0
            Global._noteCell = new NoteCellInfo(trackInfo.midiNote, index);
            noteUI.InitNoteCell(Global._noteCell);
        }
    }
}

