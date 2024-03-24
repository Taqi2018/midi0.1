using DAW;
using DAW.CommonClasses;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrackEditPanel : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] GridLayoutGroup m_KeyboardContent;
    public GridLayoutGroup m_NoteTrackContent;
    public Transform measureLineContent;
    public Transform measureStickContent;

    [Header("Variables")]
    public float m_NoteWidth;
    public int m_MinNote; // min Midi Note
    public int m_MaxNote; // max Midi Note
    private GameObject tempLineObj;
    private GameObject tempStickObj;
    [SerializeField] GameObject playLine;
    [SerializeField] Button playButton;
    [SerializeField] Button pauseButton;
    [SerializeField] TMP_Text detailText;


    [Header("LineColors")]
    [SerializeField] Color measureColor = Color.green;
    [SerializeField] Color quarterColor = new Color(0.7f, 0.7f, 0.7f, 1);
    [SerializeField] Color auxilaryColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    void Start()
    {
        
        if (!tempLineObj) tempLineObj = measureLineContent.GetChild(0).gameObject;

        measureColor = new Color(0f, 1f, 0f, 0.5f);
        quarterColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        auxilaryColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        playLine.SetActive(false);
    }

    public void InitKeyboardAndTrackContent(int minMidiNote, int maxMidiNote)
    {
        ClearContents();
        for (int i = minMidiNote; i < maxMidiNote + 1; i++)
        {
            GameObject keyObj = GameObject.Instantiate(m_KeyboardContent.transform.GetChild(0).gameObject);
            keyObj.transform.SetParent(m_KeyboardContent.transform);
            keyObj.transform.localScale = Vector3.one;
            KeyboardUI keyui = keyObj.GetComponent<KeyboardUI>();
            keyObj.SetActive(true);
            KeyInfo kInfo = new KeyInfo(i);
            keyui.InitKeyboard(kInfo);

            GameObject trackObj = GameObject.Instantiate(m_NoteTrackContent.transform.GetChild(0).gameObject);
            trackObj.transform.SetParent(m_NoteTrackContent.transform);
            trackObj.transform.localScale = Vector3.one;
            TrackUI trackUI = trackObj.GetComponent<TrackUI>();
            trackObj.SetActive(true);
            TrackInfo t = new TrackInfo(i);
            trackUI.InitTrack(t);
        }
    }

    void ClearContents()
    {
        Debug.Log("Clear Track content!!!!");
        ClearKeyboardContent();
        ClearNoteTrackContent();
    }

    void ClearKeyboardContent()
    {
        foreach (Transform child in m_KeyboardContent.transform)
        {
            if (child.GetSiblingIndex() == 0) continue;
            Destroy(child.gameObject);
        }
    }

    void ClearNoteTrackContent()
    {
        foreach (Transform child in m_NoteTrackContent.transform)
        {
            if (child.GetSiblingIndex() == 0) continue;
            Destroy(child.gameObject);
        }

    }

    public void ChangeKeyboardAndTrackHeight()
    {
        Vector2 newTrackSize = new Vector2(m_NoteTrackContent.cellSize.x, Global._trackHeight - m_NoteTrackContent.spacing.y);
        Vector2 newKeyboardSize = new Vector2(m_KeyboardContent.cellSize.x, Global._trackHeight);
        m_NoteTrackContent.cellSize = newTrackSize;
        m_KeyboardContent.cellSize = newKeyboardSize;
    }

    public void DrawMeasureLines(float x, MeasureLineType mlt)
    {
        GameObject lineObj = GameObject.Instantiate(tempLineObj);
        lineObj.transform.SetParent(measureLineContent);
        lineObj.transform.localScale = Vector3.one;
        Image lineImg = lineObj.GetComponent<Image>();
        Vector2 newPos = new Vector2(x /*- measureLineContent.GetComponent<RectTransform>().rect.width/2f*/, 0f);
        lineObj.transform.localPosition = newPos;
        switch (mlt)
        {
            case MeasureLineType.MEASURE:
                lineImg.color = measureColor;
                break;
            case MeasureLineType.QUARTER:
                lineImg.color = quarterColor;
                break;
            case MeasureLineType.AUXILARY:
                lineImg.color = auxilaryColor;
                /*lineObj.GetComponent<RectTransform>().rect.width = 1.5f;*/
                break;
        }
        lineObj.SetActive(true);
    }

    public void ClearMeasureLine() {
        foreach (Transform child in measureLineContent)
        {
            if (child.GetSiblingIndex() == 0) continue;
            Destroy(child.gameObject);
        }
    }

    public void ClearStickLine() {
        foreach (Transform child in measureStickContent) {
            if (child.GetSiblingIndex() == 0) continue;
            Destroy(child.gameObject);
        }
    }

    public void DrawMeasureStick(float x, MeasureLineType mlt, string str)
    {
        if (!tempStickObj) tempStickObj = GameObject.Instantiate(measureStickContent.GetChild(0).gameObject);
        GameObject lineObj = GameObject.Instantiate(tempStickObj);
        lineObj.transform.SetParent(measureStickContent);
        lineObj.transform.localScale = Vector3.one;
        Image lineImg = lineObj.GetComponent<Image>();
        Vector2 newPos = new Vector2(x /*- measureLineContent.GetComponent<RectTransform>().rect.width/2f*/, 0f);
        lineObj.transform.localPosition = newPos;
        switch (mlt)
        {
            case MeasureLineType.MEASURE:
                lineImg.color = measureColor;
                break;
            case MeasureLineType.QUARTER:
                lineImg.color = quarterColor;
                break;
            case MeasureLineType.AUXILARY:
                lineImg.color = auxilaryColor;
                /*lineObj.GetComponent<RectTransform>().rect.width = 1.5f;*/
                break;
        }
        TMP_Text lineText = lineObj.transform.GetComponentInChildren<TMP_Text>();
        lineText.text = str;
        lineObj.SetActive(true);
    }

    public void SetTrackWidth(float width) {
        if (width < 1800f) width = 1800f;
        Vector2 newCellSize = m_NoteTrackContent.cellSize;
        newCellSize = new Vector2(width, newCellSize.y);
        m_NoteTrackContent.cellSize = newCellSize;
    }

    // it is called in mainEngine class, The parameter x is local position's x
    public void DrawOneMidiNoteCell(int index, int note, float x, float width) {
        /*Debug.Log($"child count  {m_NoteTrackContent.transform.childCount}");*/
        foreach (Transform child in m_NoteTrackContent.transform) {
            if (!child.gameObject.activeSelf) continue;
            TrackUI trackUI = child.GetComponent<TrackUI>();
            if (trackUI.trackInfo.midiNote != note) continue;
            Vector2 newPos = new Vector2(x, 0f);
            trackUI.DrawSnap(index, newPos, width, true);
            break;
        }
    }

    public void DrawPlayLine(float x) {
        if (!playLine) return;
        if (!playLine.activeSelf) playLine.SetActive(true);
        TMP_Text playText = playButton.transform.GetChild(0).GetComponent<TMP_Text>();
        if (playText.text == "Play") playText.text = "Stop";
        Vector2 pos = new Vector2(x, 0f);
        playLine.transform.localPosition = pos;
    }

    public void StopPlayLine() {
        if (!playLine) return;
        if (playLine.activeSelf) playLine.SetActive(false);
        TMP_Text playText = playButton.transform.GetChild(0).GetComponent<TMP_Text>();
        if (playText.text == "Stop") playText.text = "Play";
    }

    public void PausePlayLine(bool b) {
        TMP_Text pauseText = pauseButton.transform.GetChild(0).GetComponent<TMP_Text>();
        if (b)
        {
            if (pauseText.text == "Pause") pauseText.text = "Resume";
        }
        else
        {
            if (pauseText.text == "Resume") pauseText.text = "Pause";
        }
    }

    public void WriteDetailText(string str) {
        detailText.text = str;
    }
}