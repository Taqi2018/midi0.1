#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MidiPlayerTK
{
    // ensure class initializer is called whenever scripts recompile
    //[ExecuteAlways, InitializeOnLoadAttribute]
    public class TestGUI : EditorWindow
    {
        private static TestGUI window;

        static int currentPage;
        static public bool followEvent;
        static int displayOther;

        // SerializeField is used to ensure the view state is written to the window 
        // layout file. This means that the state survives restarting Unity as long as the window
        // is not closed. If the attribute is omitted then the state is still serialized/deserialized.
        [SerializeField] TreeViewState m_TreeViewState;

        //The TreeView is not serializable, so it should be reconstructed from the tree data.
        //SimpleTreeView m_SimpleTreeView;

        MPTKGui.PopupList popupCommand1;
        MPTKGui.PopupList popupCommand2;
        MPTKGui.PopupList popupMulti;
        MPTKGui.PopupList popupMultiShort;
        List<MPTKGui.StyleItem> ItemsCommand;
        List<MPTKGui.StyleItem> ItemsHuge;
        List<MPTKGui.StyleItem> ItemShort;
        float slider = -90;
        float newx;
        AnimationCurve curveX = AnimationCurve.EaseInOut(0, -10, 1, 10);
        //bool isPlaying;

        Vector2 ScrollerMidiEvents = Vector2.zero;
        public float scrollPos = 0.5f;

  //      [MenuItem("MaestroTest/Editor GUI &T", false, 5)]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            window = CreateWindow<TestGUI>("UI Test");
            window.ShowUtility();
            //window.titleContent = new GUIContent("Test");
            window.minSize = new Vector2(10, 10);
        }

        private void OnFocus()
        {
            Debug.Log($"OnFocus");

            //if (window == null)
            //    window = GetWindow<TestGUI>();
        }

        private void OnEnable()
        {
            Debug.Log($"OnEnable");
        }

        //public IEnumerator<float> ThreadCorePlay()
        //{
        //    //while (isPlaying)
        //    //{
        //    //    Debug.Log($" {Routine.LocalTime} {curveX.Evaluate(Routine.LocalTime)} ");
        //    //    yield return 0;
        //    //}
        //}
        void OnGUI()
        {
            try
            {

                MPTKGui.LoadSkinAndStyle();
                if (ItemsCommand == null)
                {
                    ItemsCommand = new List<MPTKGui.StyleItem>
                    {
                        new MPTKGui.StyleItem("Note On",value:10, true),
                        new MPTKGui.StyleItem("Note Off",value:11, true),
                        new MPTKGui.StyleItem("Control Change",value:12, true),
                        new MPTKGui.StyleItem("Preset Change", value : 13, true),
                        new MPTKGui.StyleItem("Meta", value : 14, true),
                        new MPTKGui.StyleItem("Touch", value : 15, true),
                        new MPTKGui.StyleItem("Others", value : 16, true)
                    };
                    ItemsCommand[0].Selected = true;

                    ItemShort = new List<MPTKGui.StyleItem>
                    {
                        new MPTKGui.StyleItem("xxxxx", false),
                        new MPTKGui.StyleItem("yyyyyyyyyyyyyyy", true)
                    };

                    ItemsHuge = new List<MPTKGui.StyleItem>
                    {
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true),
                        new MPTKGui.StyleItem("xxxxx", true),
                        new MPTKGui.StyleItem("yyyyyyy", true),
                        new MPTKGui.StyleItem("zzz", true)
                    };
                }

                //---------------------------------------------
                GUILayout.BeginHorizontal();

                MPTKGui.ComboBox(ref popupCommand1, "{Index} Display Mono: {Label}", ItemsCommand, false,
                    delegate (int index)
                    {
                        Debug.Log($"Action popupCommand1 {index}");
                    }, null, widthPopup: 200, GUILayout.Width(200));

                int idx = (int)GUILayout.HorizontalSlider((float)popupCommand1.SelectedIndex, 0, (float)ItemsCommand.Count - 1, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb);
                if (idx != popupCommand1.SelectedIndex)
                {
                    popupCommand1.SelectedIndex = idx;
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(20);

                //---------------------------------------------
                GUILayout.BeginHorizontal();

                MPTKGui.ComboBox(ref popupCommand2, "{Index} Display Mono: {Label}", ItemsCommand, false,
                    delegate (int index)
                    {
                        Debug.Log($"Action popupCommand2 {index}");
                    }, null, widthPopup: 200, GUILayout.Width(200));

                idx = (int)GUILayout.HorizontalSlider((float)popupCommand2.SelectedIndex, 0, (float)ItemsCommand.Count - 1, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb);
                if (idx != popupCommand2.SelectedIndex)
                {
                    popupCommand2.SelectedIndex = idx;
                    //indexCommand = idx;
                    //ItemsCommand[indexCommand].Selected = true;
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(20);

                //---------------------------------------------
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("List Item", MPTKGui.Button, GUILayout.Width(100), GUILayout.Height(40)))
                {
                    foreach (MPTKGui.StyleItem item in ItemsCommand)
                        Debug.Log($"{item.Caption} {item.Visible} {item.Selected}");
                    Debug.Log($"popupCommand1 - index:{popupCommand1.SelectedIndex} value:{popupCommand1.SelectedValue} label:'{popupCommand1.SelectedLabel}' count:{popupCommand1.SelectedCount} ");
                    Debug.Log($"popupCommand2 - index:{popupCommand2.SelectedIndex} value:{popupCommand2.SelectedValue} label:'{popupCommand2.SelectedLabel}' count:{popupCommand2.SelectedCount} ");
                    Debug.Log("---------------------------------------------------------------");
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(20);


                //---------------------------------------------
                GUILayout.BeginHorizontal();

                float alignHorizontal = 24;
                followEvent = GUILayout.Toggle(followEvent, "Follow", GUILayout.Height(alignHorizontal));

                //Rect lastEventDraw = Rect.zero;
                //if (Event.current.type == EventType.Repaint)
                //    lastEventDraw = GUILayoutUtility.GetLastRect();

                MPTKGui.ComboBox(ref popupMulti, "Display Huge - {Count}", ItemsHuge, true,
                    delegate (int index)
                    {
                        Debug.Log($"Action {index}");
                        foreach (MPTKGui.StyleItem item in ItemsHuge)
                            Debug.Log($"{item.Caption} {item.Visible} {item.Selected}");
                    });

                MPTKGui.ComboBox(ref popupMultiShort, "Short {*}", ItemShort, true,
                    delegate (int index)
                    {
                        Debug.Log($"Action {index}");
                        foreach (MPTKGui.StyleItem item in ItemShort)
                            Debug.Log($"{item.Caption} {item.Visible} {item.Selected}");
                    },
                    null, 200f);
                if (GUILayout.Button("aaa")) currentPage = 0;
                if (GUILayout.Button("bbb")) currentPage = 0;
                if (GUILayout.Button(MPTKGui.IconFirst)) currentPage = 0;
                if (GUILayout.Button(MPTKGui.IconPrevious)) currentPage--;
                GUILayout.Label($"Page {currentPage} / ?", MPTKGui.styleLabelCenter, GUILayout.Height(alignHorizontal));
                if (GUILayout.Button(MPTKGui.IconNext)) currentPage++;
                if (GUILayout.Button(MPTKGui.IconLast)) currentPage = 100;

                GUILayout.EndHorizontal();


                //---------------------------------------------
                GUILayout.BeginHorizontal();


                slider = GUILayout.HorizontalSlider(slider, -360, 360, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb);
                if (GUILayout.Button("0"))
                    slider = 0;
                if (GUILayout.Button("-90"))
                    slider = -90;
                GUILayout.EndHorizontal();

                //---------------------------------------------
                GUILayout.BeginHorizontal();
                newx = GUILayout.HorizontalSlider(newx, -400, 400, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb);
                GUILayout.EndHorizontal();


                Rect midiEventsVisibleRect = new Rect(100, 300, 100, 100);
                GUI.Box(midiEventsVisibleRect, "", MPTKGui.stylePanelGrayLight);

                Rect midiEventsContentRect = new Rect(0, 0, 220, 200);

                //ScrollerMidiEvents = GUI.BeginScrollView(midiEventsVisibleRect, ScrollerMidiEvents, midiEventsContentRect, false, false);
                //for (int i = 0; i < 50; i++)
                //    GUILayout.Label($"line {i}", MidiCommonEditor.styleLabelLeft);
                //GUI.EndScrollView();


                // An absolute-positioned example: We make a scrollview that has a really large client
                // rect and put it in a small rect on the screen.
                ScrollerMidiEvents = GUI.BeginScrollView(midiEventsVisibleRect, ScrollerMidiEvents, midiEventsContentRect, false, false,
                    new GUIStyle("horizontalscrollbar"), new GUIStyle("verticalscrollbar"));

                // Make four buttons - one in each corner. The coordinate system is defined
                // by the rectClear parameter to BeginScrollView.
                GUI.Button(new Rect(0, 0, 100, 20), "Top-left");
                GUI.Button(new Rect(120, 0, 100, 20), "Top-right");
                GUI.Button(new Rect(0, 180, 100, 20), "Bottom-left");
                GUI.Button(new Rect(120, 180, 100, 20), "Bottom-right");

                // End the scroll view that we began above.
                GUI.EndScrollView();

                // This will use the following style names to determine the size / placement of the buttons
                // MyScrollbarleftbutton    - Name of style used for the left button.
                // MyScrollbarrightbutton - Name of style used for the right button.
                // MyScrollbarthumb         - Name of style used for the draggable thumb.
                scrollPos = GUI.HorizontalScrollbar(new Rect(300, 300, 100, 20), scrollPos, 1.0f, 0.0f, 100.0f);
                scrollPos = GUI.VerticalScrollbar(new Rect(500, 300, 20, 100), scrollPos, 1.0f, 0.0f, 100.0f);

                // Matrix4x4 save = GUI.matrix;

                // //GUILayout.BeginArea(new Rect(100f, 100f,100, 1000), MidiCommonEditor.styleListTitle);

                // Rect rect;
                // //GUI.Label(rect, $"Area", MidiCommonEditor.styleListTitle);

                // float x = 10;
                // float y = 95;
                // float w = 25;
                // float h = 200;


                // rect = new Rect(x, y, w, h);
                //// GUI.Label(rect, $"No Rotate", MidiCommonEditor.styleListTitle);
                // /* Use Matrix4x4.TRS(t, r, s) to create your matrix, then provide:
                //     t: translation vector value, move along x and y, but not z
                //     r: quaternion rotation value, rotate on z, but not x and y
                //     s: scale vector value, scale on x and y, but not z */
                // GUIUtility.RotateAroundPivot(slider, new Vector2(x, y));
                // //GUI.matrix = Matrix4x4.TRS(new Vector3(-105,325,0), GUI.matrix.rotation, Vector3.one);

                // rect = new Rect(newx, y, h, w);
                // GUI.Label(rect, $"A Very Long Text To Be Rotated", MidiCommonEditor.styleListTitle);

                // //rect = new Rect(x, 200, 200f, 20);
                // //GUI.Label(rect, $"Another Very Long Text To Be Rotated", MidiCommonEditor.styleListTitle);

                // GUI.matrix = save;

                //GUILayout.EndArea();

                //{
                //    GUILayout.BeginHorizontal();
                //    // https://docs.unity3d.com/ScriptReference/EditorGUI.CurveField.html
                //    curveX = EditorGUI.CurveField(GUILayoutUtility.GetRect(300, 100), "Animation on X", curveX);
                //    curveX.postWrapMode = WrapMode.PingPong;

                //    if (GUILayout.Button("Generate Curve"))
                //        if (Selection.activeGameObject)
                //        {
                //            //    // Get GameObject selectedInFilterList in hierarchy
                //            //    FollowAnimationCurve comp = Selection.activeGameObject.GetComponent<FollowAnimationCurve>();
                //            //    comp.SetCurves(curveX);
                //        }
                //    bool changePlaying = GUILayout.Toggle(isPlaying, "Play");
                //    if (isPlaying != changePlaying)
                //    {
                //        isPlaying = changePlaying;
                //        if (isPlaying)
                //            Routine.RunCoroutine(ThreadCorePlay(), Application.isPlaying ? Segment.RealtimeUpdate : Segment.EditorUpdate);

                //    }
                //    GUILayout.EndHorizontal();
                //}

                //m_SimpleTreeView.OnGUI(new Rect(0, 50, position.width, position.height));
            }
            catch (ExitGUIException ex)
            {
                Debug.Log(ex.Message);

            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                //MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
    }

    // FollowAnimationCurve.cs
    // using UnityEngine; 
    // using System.Collections;

    // namespace MidiPlayerTK
    //    {

    //        public class FollowAnimationCurve : MonoBehaviour
    //        {
    //            public AnimationCurve curveX;

    //            public void SetCurves(AnimationCurve xC)
    //            {
    //                curveX = xC;
    //            }

    //            void Update()
    //            {
    //                transform.position = new Vector3(curveX.Evaluate(Time.time), transform.position.y, transform.position.z);
    //            }
    //        }
    //    }


    //    public MultiColumnTreeView(TreeViewState state,
    //                        MultiColumnHeader multicolumnHeader,
    //                        TreeModel<MyTreeElement> model)
    //                        : base(state, multicolumnHeader, model)
    //    {
    //        // Custom setup
    //        rowHeight = 20;
    //        columnIndexForTreeFoldouts = 2;
    //        showAlternatingRowBackgrounds = true;
    //        showBorder = true;
    //        customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f;
    //        extraSpaceBeforeIconAndLabel = kToggleWidth;
    //        multicolumnHeader.sortingChanged += OnSortingChanged;

    //        Reload();
    //    }

    //}
    //class SimpleTreeView : TreeView
    //{
    //    public SimpleTreeView(TreeViewState treeViewState)
    //        : base(treeViewState)
    //    {
    //        Reload();
    //    }

    //    protected override TreeViewItem BuildRoot()
    //    {
    //        // BuildRoot is called every time Reload is called to ensure that TreeViewItems 
    //        // are created from data. Here we create a fixed set of items. In a real world example,
    //        // a data model should be passed into the TreeView and the items created from the model.

    //        // This section illustrates that IDs should be unique. The root item is required to 
    //        // have a depth of -1, and the rest of the items increment from that.
    //        var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
    //        var allItems = new List<TreeViewItem>
    //    {
    //        new TreeViewItem {id = 1, depth = 0, displayName = "Animals"},
    //        new TreeViewItem {id = 2, depth = 1, displayName = "Mammals"},
    //        new TreeViewItem {id = 3, depth = 2, displayName = "Tiger"},
    //        new TreeViewItem {id = 4, depth = 2, displayName = "Elephant"},
    //        new TreeViewItem {id = 5, depth = 2, displayName = "Okapi"},
    //        new TreeViewItem {id = 6, depth = 2, displayName = "Armadillo"},
    //        new TreeViewItem {id = 7, depth = 1, displayName = "Reptiles"},
    //        new TreeViewItem {id = 8, depth = 2, displayName = "Crocodile"},
    //        new TreeViewItem {id = 9, depth = 2, displayName = "Lizard"},
    //    };

    //        // Utility method that initializes the TreeViewItem.children and .parent for all items.
    //        SetupParentsAndChildrenFromDepths(root, allItems);

    //        // Return root of the tree
    //        return root;
    //    }
    //}
    //[Serializable]
    ////The TreeElement data class is extended to hold extra data, which you can show and edit in the front-end TreeView.
    //internal class MyTreeElement : TreeElement
    //{
    //    public float floatValue1, floatValue2, floatValue3;
    //    public Material material;
    //    public string text = "";
    //    public bool enabled = true;

    //    public MyTreeElement(string name, int depth, int id) : base(name, depth, id)
    //    {
    //        floatValue1 = Random.value;
    //        floatValue2 = Random.value;
    //        floatValue3 = Random.value;
    //    }
    //}
    //[CreateAssetMenu(fileName = "TreeDataAsset", menuName = "Tree Asset", order = 1)]
    //public class MyTreeAsset : ScriptableObject
    //{
    //    [SerializeField] List<MyTreeElement> m_TreeElements = new List<MyTreeElement>();

    //    internal List<MyTreeElement> treeElements
    //    {
    //        get { return m_TreeElements; }
    //        set { m_TreeElements = value; }
    //    }
    //}

}
#endif