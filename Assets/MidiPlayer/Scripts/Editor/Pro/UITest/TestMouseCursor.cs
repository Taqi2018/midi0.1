#if UNITY_EDITOR
// Create a small window that has a color box in it.
// Hovering over it causes a Zoom mouse cursor to appear.  (The window is not
// zoomed however.)
using UnityEngine;
using UnityEditor;

public class AddCursorRectExample : EditorWindow
{
    //[MenuItem("MaestroTest/AddCursorRect Example")]
    static void addCursorRectExample()
    {
        AddCursorRectExample window =
            EditorWindow.GetWindowWithRect<AddCursorRectExample>(new Rect(0, 0, 180, 80));
        window.Show();
    }

    void OnGUI()
    {
        EditorGUI.DrawRect(new Rect(10, 10, 160, 60), new Color(0.5f, 0.5f, 0.85f));
        EditorGUI.DrawRect(new Rect(20, 20, 140, 40), new Color(0.9f, 0.9f, 0.9f));
        EditorGUIUtility.AddCursorRect(new Rect(20, 20, 140, 40), MouseCursor.Zoom);
    }
}
#endif