using UnityEngine;
using UnityEngine.UI;

public class DrawLine : MonoBehaviour
{
    public RectTransform canvas;
    public Material lineMaterial;

    private LineRenderer lineRenderer;
    private Vector2 startPosition;
    private Vector2 endPosition;

    void Start()
    {
        GameObject lineObject = new GameObject("Line");
        lineObject.transform.SetParent(canvas, false);

        lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = 5f;
        lineRenderer.endWidth = 5f;
        lineRenderer.positionCount = 2;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPosition = GetMousePosition();
            endPosition = startPosition;
            UpdateLine();
        }

        if (Input.GetMouseButton(0))
        {
            endPosition = GetMousePosition();
            UpdateLine();
        }
    }

    Vector2 GetMousePosition()
    {
        Vector2 mousePosition = Input.mousePosition;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, mousePosition, null, out localPoint);
        return localPoint;
    }

    void UpdateLine()
    {
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
    }
}