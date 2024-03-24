using UnityEngine;
using UnityEngine.UI;

public class TestMousePosition : MonoBehaviour
{
    public RectTransform givenImageRect;
    public GameObject obj;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 localMousePosition = givenImageRect.InverseTransformPoint(Input.mousePosition);
            /*if (givenImageRect.rect.Contains(localMousePosition))
            {*/
                /*Debug.Log($"mouse position{localMousePosition}");*/
                InstantiateObject(localMousePosition);
            /*}*/
        }

        void InstantiateObject(Vector2 pos) {
            GameObject ooo = GameObject.Instantiate(obj);
            ooo.transform.SetParent(transform);
            ooo.transform.localScale = Vector3.one;
            ooo.SetActive(true);

            ooo.transform.localPosition = new Vector2(pos.x, pos.y);
            RectTransform noteRectTrans = ooo.GetComponent<RectTransform>();
            noteRectTrans.offsetMin = new Vector2(noteRectTrans.offsetMin.x, 0f);  // Set bottom offset to 0
            noteRectTrans.offsetMax = new Vector2(noteRectTrans.offsetMax.x, 0f);  // Set top offset to 0
            Debug.Log($"minx; {noteRectTrans.offsetMin.x}, maxx: {noteRectTrans.offsetMax.x}, pos: {pos}");
            /*Global._noteCell = new NoteCellInfo(trackInfo.midiNote);
            noteUI.InitNoteCell(Global._noteCell);*/
        }
    }
}
