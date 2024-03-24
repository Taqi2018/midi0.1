using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DAW;
using DAW.CommonClasses;

public class EditCurorMovement : MonoBehaviour
{
    public GameObject cursorObj;
    public RectTransform editViewPort;

    private RectTransform canvasRect;
    // Start is called before the first frame update
    void Start()
    {
        canvasRect = FindObjectOfType<Canvas>().GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Global._editState != EditState.TRACK) return;

        if (IsOnEditor(Input.mousePosition))
        {
            if (!cursorObj.activeSelf) cursorObj.SetActive(true);
            cursorObj.transform.position = Input.mousePosition;
        }
        else
        {
            if (cursorObj.activeSelf) cursorObj.SetActive(false);
        }
    }

    private bool IsOnEditor(Vector3 v3)
    {
        Vector3 rectPos = editViewPort.position;
        if (rectPos.x <= v3.x && rectPos.x + editViewPort.rect.width >= v3.x
            && rectPos.y >= v3.y && rectPos.y - editViewPort.rect.height <= v3.y)
            return true;
        return false;
    }
}
