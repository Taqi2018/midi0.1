using UnityEngine;

using DAW.CommonClasses;

namespace DAW
{
    public class Global : MonoBehaviour
    {
        [Header("Managers")]
        public static DAWMainEngine _engine;

        [Header("Objects")]
        public static EditState _editState;
        public static NoteCellInfo _noteCell;
        public static float _trackWidth;
        public static float _trackHeight;
        public static int _maxCellIndex;

        public static float _xNote16th;
        public static float _curSnapWidth;

        
        

        void Start()
        {
            _editState = EditState.TRACK;
            _noteCell = new NoteCellInfo();
            _trackWidth = 3000f;
            _trackHeight = 40f;
            _maxCellIndex = 0;
            _engine = FindObjectOfType<DAWMainEngine>();
            _curSnapWidth = 50f;
    }


        public static (string, int) GetKeyNameFromNote(int midiNote)
        {
            int octave = midiNote / 12 - 1;
            int keyNumber = midiNote % 12;
            string[] keyboard = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            return (keyboard[keyNumber], octave);
        }

        public static float GetTrackWidth() {
            float width = 0f;
            width = Global._engine.uiMgr.m_TrackEditPanel.GetComponent<RectTransform>().rect.width;
            return width;
        }

        public static float GetSnapPosX(float curX, SideType st = SideType.LEFT) {
            if (_xNote16th == 0f) return 0f;
            float snapPosX = 0f;
            int snapCnt = st==SideType.LEFT? (int)(curX / _xNote16th) : (int)(curX / _xNote16th) + 1;
            snapPosX = _xNote16th * snapCnt;
            return snapPosX;
        }

        public static void SetCurSnapWidthByDragging(float first, float last, SideType st = SideType.LEFT) {
            float offset = st == SideType.LEFT? first - last : last - first;
            if (Mathf.Abs(offset) < _xNote16th) return;
            /*_curSnapWidth += offset;*/
            
        }

        //The measure ract's local start and end x values for the note line.
        public static (float, float) GetMeasureLineRect() {
            float xStart, xEnd = 0f;
            RectTransform noteLineContent = _engine.uiMgr.m_TrackEditPanel.GetComponent<TrackEditPanel>().m_NoteTrackContent.transform.GetChild(1).GetComponent<RectTransform>();
            RectTransform measureContent = _engine.uiMgr.m_TrackEditPanel.GetComponent<TrackEditPanel>().measureLineContent.GetComponent<RectTransform>();
            Vector2 worldpos_start = measureContent.position;
            Vector2 worldpos_end = new Vector2(worldpos_start.x+measureContent.rect.width, worldpos_start.y);
            Vector2 localpos = noteLineContent.InverseTransformPoint(worldpos_start);
            xStart = localpos.x;
            localpos = noteLineContent.InverseTransformPoint(worldpos_end);
            xEnd = localpos.x;
            if (xStart < -2) { xStart = -2; xEnd = xStart + measureContent.rect.width; }
            return (xStart, xEnd);
        }
    }

}
