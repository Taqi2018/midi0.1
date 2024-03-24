#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>@brief
    /// Inspector for the midi global player component
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MidiListPlayer))]
    public class MidiListPlayerEditor : Editor
    {
        private SerializedProperty CustomEventStartPlayMidi;
        private SerializedProperty CustomEventEndPlayMidi;

        private static MidiListPlayer instance;
        private MidiCommonEditor commonEditor;

        private static bool showEvents = false;
        private Rect dragMidiZone;

        private Vector2 scrollPlayList;
        private static GUILayoutOption miniButtonWidth = GUILayout.Width(20f);
        private Texture buttonIconUpArrow;
        private Texture buttonIconDnArrow;
        private Texture buttonIconDelete;
        private Texture buttonIconAdd;
        //private Texture buttonIconSelect;
        //private Texture buttonIconUnSelect;
        private MessagesEditor messages;
        //private SelectMidiWindow winSelectMidi;

        // Manage skin
        public CustomStyle myStyle;

        void OnEnable()
        {
            try
            {
                messages = new MessagesEditor();

                //Load a Texture (Assets/Resources/Textures/texture01.png)
                buttonIconUpArrow = Resources.Load<Texture2D>("Textures/008-up-arrow");
                buttonIconDnArrow = Resources.Load<Texture2D>("Textures/037-down-arrow");
                buttonIconDelete = Resources.Load<Texture2D>("Textures/Delete_32x32");
                buttonIconAdd = Resources.Load<Texture2D>("Textures/Plus");
                //buttonIconSelect = Resources.Load<Texture2D>("Textures/040-confirm");
                //buttonIconUnSelect = Resources.Load<Texture2D>("Textures/038-delete");

                //Debug.Log("OnEnable MidiFilePlayerEditor");
                CustomEventStartPlayMidi = serializedObject.FindProperty("OnEventStartPlayMidi");
                CustomEventEndPlayMidi = serializedObject.FindProperty("OnEventEndPlayMidi");

                instance = (MidiListPlayer)target;
                // Load description of available soundfont
                if (MidiPlayerGlobal.CurrentMidiSet == null || MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo == null)
                {
                    MidiPlayerGlobal.InitPath();
                    ToolsEditor.LoadMidiSet();
                    ToolsEditor.CheckMidiSet();
                }

                if (SelectMidiWindow.winSelectMidi != null)
                {
                    //Debug.Log("OnEnable winSelectMidi " + winSelectMidi.Title);
                    SelectMidiWindow.winSelectMidi.SelectedIndexMidi = indexMidiSelected;
                    SelectMidiWindow.winSelectMidi.Repaint();
                    SelectMidiWindow.winSelectMidi.Focus();
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private void OnDisable()
        {
            try
            {
                if (SelectMidiWindow.winSelectMidi != null)
                {
                    //Debug.Log("OnDisable winSelectMidi " + winSelectMidi.Title);
                    SelectMidiWindow.winSelectMidi.Close();
                }
            }
            catch (Exception)
            {
            }
        }

        //string labFormatMidiTime;
        string[] optionsFormatDuration = { "hh:mm:ss:mmm", "seconds", "MIDI ticks" };
        int indexEditItem;
        int indexMidiSelected;
        string nameMidiSelected;
        MidiListPlayer.MPTK_MidiPlayItem itemSelected;

        public void InitWinSelectMidi(int selected, Action<object, int> select)
        {
            // Get existing open window or if none, make a new one:
            try
            {
                SelectMidiWindow.winSelectMidi = EditorWindow.GetWindow<SelectMidiWindow>(true, "Select a MIDI File");
                SelectMidiWindow.winSelectMidi.OnSelect = select;
                SelectMidiWindow.winSelectMidi.SelectedIndexMidi = selected;
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private void MidiChanged(object tag, int midiindex)
        {
            //Debug.Log("MidiChanged " + midiindex + " for " + tag);
            //if (instance.midiFilter != null)
            //    instance.midiFilter.MidiLoad(midiindex);
            indexMidiSelected = midiindex;
            nameMidiSelected = instance.MPTK_PlayList[indexEditItem].MidiName = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[midiindex];
            instance.MPTK_ChangeMidi(MidiPlayerGlobal.CurrentMidiSet.MidiFiles[midiindex], indexEditItem);
            nameMidiSelected = instance.MPTK_PlayList[indexEditItem].MidiName;
            instance.MPTK_RePlay();
            MidiCommonEditor.SetSceneChangedIfNeed(instance, true);
        }

        public override void OnInspectorGUI()
        {
            // Set custom Style. Good for background color 3E619800
            if (myStyle == null) myStyle = new CustomStyle();

            try
            {
                GUI.changed = false;
                GUI.color = Color.white;
                if (commonEditor == null) commonEditor = ScriptableObject.CreateInstance<MidiCommonEditor>();

                commonEditor.DrawCaption("Midi List Player - Play from a list of MIDI.", "https://paxstellar.fr/midi-list-player-v2/", "dd/d3d/class_midi_player_t_k_1_1_midi_list_player.html#details");

                if (instance.MPTK_IsPlaying)
                    indexEditItem = instance.MPTK_PlayIndex;

                // Defined drop zone and List
                //EditorGUILayout.LabelField("To create or update the play list, drag Midi files from <YourProject>/MidiPlayer/Resources/MidiDB to the zone just below.",                       myStyle.DragZone, GUILayout.Height(40));
                //Event e = Event.current;
                GlobalProperties();
                EditorGUILayout.Separator();

                scrollPlayList = EditorGUILayout.BeginScrollView(scrollPlayList, false, false,
                        myStyle.HScroll, myStyle.VScroll, myStyle.BackgMidiList, GUILayout.MaxHeight(200));
                //Debug.Log(scrollPlayList);
                EditorGUILayout.BeginHorizontal(myStyle.InfoInspectorBackground);
                EditorGUILayout.LabelField(new GUIContent("S", "Select to play"), GUILayout.Width(17));
                EditorGUILayout.LabelField("Title", GUILayout.Width(251));
                EditorGUILayout.LabelField("Start", GUILayout.Width(101));
                EditorGUILayout.LabelField("End", GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                int hCell = 20;
                if (instance.MPTK_PlayList != null)
                    for (int i = 0; i < instance.MPTK_PlayList.Count; i++)
                    {
                        MidiListPlayer.MPTK_MidiPlayItem playing = instance.MPTK_PlayList[i];
                        if (i == indexEditItem)// instance.MPTK_PlayIndex)
                        {
                            EditorGUILayout.BeginHorizontal(myStyle.ItemSelected);
                            nameMidiSelected = playing.MidiName;
                            indexMidiSelected = MidiPlayerGlobal.MPTK_FindMidi(playing.MidiName);
                            //indexEditItem = i;
                        }
                        else
                            EditorGUILayout.BeginHorizontal(myStyle.ItemNotSelected);
                        playing.Selected = EditorGUILayout.Toggle(playing.Selected, GUILayout.Width(20), GUILayout.Height(hCell));

                        if (GUILayout.Button(playing.MidiName, myStyle.BtTransparent, GUILayout.Width(250), GUILayout.Height(hCell)))//, GUILayout.Height(20f)))
                        {
                            indexEditItem = i;
                            //if (instance.MPTK_IsPlaying)
                            //    instance.MPTK_PlayIndex = i;
                            //Debug.Log("Midi selectedInFilterList " + indexEditItem + " " + playing.MidiName + " " + indexMidiSelected);
                        }

                        //EditorGUILayout.LabelField(playing.MidiName, GUILayout.Width(250));
                        EditorGUILayout.LabelField(
                            FormatingDuration(playing.StartFrom, playing.TickLengthMs),
                            myStyle.ItemNotSelected, GUILayout.Width(100), GUILayout.Height(hCell));

                        EditorGUILayout.LabelField(
                            FormatingDuration(playing.EndFrom, playing.TickLengthMs),
                            myStyle.ItemNotSelected, GUILayout.Width(100), GUILayout.Height(hCell));

                        EditorGUILayout.EndHorizontal();
                    }

                EditorGUILayout.EndScrollView();
                GUILayout.Space(15);

                // Action button on the Midi list
                ActionButtons();

                // defined Scroll view as drag zone
                //if (e.type == EventType.Repaint) dragMidiZone = GUILayoutUtility.GetLastRect();

                if (indexEditItem >= 0 && indexEditItem < instance.MPTK_PlayList.Count)
                {
                    string tooltip;
                    GUIStyle styleBlockTime = myStyle.BackgMidiList;    
                    itemSelected = instance.MPTK_PlayList[indexEditItem];

                    //
                    // Block start position
                    // --------------------
                    EditorGUILayout.BeginVertical(styleBlockTime);
                    tooltip = "Position to start the midi";
                    EditorGUILayout.BeginHorizontal();
                    string sStartFrom = FormatingDuration(itemSelected.StartFrom, itemSelected.TickLengthMs);
                    EditorGUILayout.PrefixLabel(new GUIContent("Start Playing At", tooltip));
                    EditorGUILayout.LabelField(new GUIContent(sStartFrom, "")/*, myStyle.ItemSelected*/);
                    EditorGUILayout.EndHorizontal();

                    if (instance.indexlabFormatMidiTime == 0 || instance.indexlabFormatMidiTime == 1)
                    {
                        float currentPosition = itemSelected.StartFrom;
                        float newPosition = (float)Math.Round(EditorGUILayout.Slider(new GUIContent(" ", tooltip), currentPosition, 0f, itemSelected.RealDurationMs));
                        if (currentPosition != newPosition && Event.current.type == EventType.Used)
                            // Avoid event as layout triggered when duration is changed
                            itemSelected.StartFrom = newPosition;
                    }
                    else
                    {
                        // midi ticks position
                        itemSelected.StartFrom = (float)(EditorGUILayout.IntSlider(new GUIContent(" ", tooltip), (int)(itemSelected.StartFrom * itemSelected.TickLengthMs), 0,
                            (int)(itemSelected.RealDurationMs * itemSelected.TickLengthMs)) / itemSelected.TickLengthMs);
                    }
                    EditorGUILayout.EndVertical();

                    //
                    // Block end position
                    // --------------------
                    EditorGUILayout.BeginVertical(styleBlockTime);
                    tooltip = "Position to end playing the midi";
                    EditorGUILayout.BeginHorizontal();
                    string sEndFrom = FormatingDuration(itemSelected.EndFrom, itemSelected.TickLengthMs);
                    EditorGUILayout.PrefixLabel(new GUIContent("Stop Playing At", tooltip));
                    EditorGUILayout.LabelField(new GUIContent(sEndFrom, "")/*, myStyle.ItemSelected*/);
                    EditorGUILayout.EndHorizontal();

                    if (instance.indexlabFormatMidiTime == 0 || instance.indexlabFormatMidiTime == 1)
                    {
                        float currentPosition = itemSelected.EndFrom;
                        float newPosition = (float)Math.Round(EditorGUILayout.Slider(new GUIContent(" ", tooltip), currentPosition, 0f, itemSelected.RealDurationMs));
                        if (currentPosition != newPosition && Event.current.type == EventType.Used)
                            // Avoid event as layout triggered when duration is changed
                            itemSelected.EndFrom = newPosition;
                    }
                    else
                    {
                        // midi ticks position
                        itemSelected.EndFrom = (float)(EditorGUILayout.IntSlider(new GUIContent(" ", tooltip), (int)(itemSelected.EndFrom * itemSelected.TickLengthMs), 0,
                            (int)(itemSelected.RealDurationMs * itemSelected.TickLengthMs)) / itemSelected.TickLengthMs);
                    }
                    EditorGUILayout.EndVertical();

                    //GUILayout.Space(15);

                    //
                    // Block current position
                    // ----------------------
                    EditorGUILayout.BeginVertical(styleBlockTime);
                    tooltip = "Real time from start and total duration regarding the current tempo";

                    EditorGUILayout.BeginHorizontal();

                    string sPosition = FormatingDuration(instance.MPTK_Position, itemSelected.TickLengthMs); ;
                    string sDuration = FormatingDuration(itemSelected.EndFrom, itemSelected.TickLengthMs);

                    EditorGUILayout.PrefixLabel(new GUIContent("Time Position", tooltip));
                    EditorGUILayout.LabelField(new GUIContent(sPosition + " / " + sDuration, tooltip));
                    EditorGUILayout.EndHorizontal();

                    if (instance.indexlabFormatMidiTime == 0 || instance.indexlabFormatMidiTime == 1)
                    {
                        float currentPosition = (float)Math.Round(instance.MPTK_Position);
                        float newPosition = (float)Math.Round(EditorGUILayout.Slider(new GUIContent(" ", tooltip), currentPosition, 0f, itemSelected.RealDurationMs));
                        if (currentPosition != newPosition && Event.current.type == EventType.Used) // Avoid event as layout triggered when duration is changed
                            instance.MPTK_Position = newPosition;
                    }
                    else
                    {
                        // midi ticks position
                        long ticks = EditorGUILayout.IntSlider(new GUIContent(" ", tooltip), (int)instance.MPTK_TickCurrent, 0, (int)itemSelected.LastTick);
                        if ((int)instance.MPTK_TickCurrent != ticks) instance.MPTK_TickCurrent = ticks;
                    }
                    EditorGUILayout.EndVertical();

                }

                if (EditorApplication.isPlaying)
                {
                    EditorUtility.SetDirty(instance);
                    EditorGUILayout.Separator();
                    EditorGUILayout.BeginHorizontal();

                    if (instance.MPTK_IsPlaying)
                        GUI.color = MPTKGui.ButtonColor;
                    if (GUILayout.Button(new GUIContent("Play", "")))
                    {
                        if (instance.MPTK_PlayList.Count == 0)
                            messages.Add("Playing list is empty. Add MIDI file.");
                        else
                            instance.MPTK_Play();
                    }
                    GUI.color = Color.white;

                    if (instance.MPTK_IsPaused)
                        GUI.color = MPTKGui.ButtonColor;
                    if (GUILayout.Button(new GUIContent("Pause", "")))
                        if (instance.MPTK_IsPaused)
                            instance.MPTK_UnPause();
                        else
                            instance.MPTK_Pause();
                    GUI.color = Color.white;

                    if (GUILayout.Button(new GUIContent("Stop", "")))
                        instance.MPTK_Stop();

                    if (GUILayout.Button(new GUIContent("Restart", "")))
                        instance.MPTK_RePlay();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent("Previous", "")))
                        instance.MPTK_Previous();
                    if (GUILayout.Button(new GUIContent("Next", "")))
                        instance.MPTK_Next();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Separator();

                    //EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.Separator();

                showEvents = EditorGUILayout.Foldout(showEvents, "Show Unity Events");
                if (showEvents)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(CustomEventStartPlayMidi);
                    EditorGUILayout.PropertyField(CustomEventEndPlayMidi);
                    serializedObject.ApplyModifiedProperties();
                    EditorGUI.indentLevel--;
                }

                //if (DragAndDropMidiFiles()) scrollPlayList = new Vector2(0, 10000);

                messages.Display();

                instance.showDefault = EditorGUILayout.Foldout(instance.showDefault, "Show default editor");
                if (instance.showDefault)
                {
                    EditorGUI.indentLevel++;
                    DrawDefaultInspector();
                    EditorGUI.indentLevel--;
                }
                MidiCommonEditor.SetSceneChangedIfNeed(instance, GUI.changed);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private string FormatingDuration(double tsDuration, double tickLengthMs)
        {
            if (instance.indexlabFormatMidiTime == 0)
            {
                TimeSpan duration = TimeSpan.FromMilliseconds(tsDuration);
                return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", duration.Hours, duration.Minutes, duration.Seconds, duration.Milliseconds);
            }
            else if (instance.indexlabFormatMidiTime == 1)
                return string.Format("{0:F3}", tsDuration / 1000d);
            else
                return string.Format("{0}", (int)(tsDuration * tickLengthMs));
        }

        private void GlobalProperties()
        {
            float volume = EditorGUILayout.Slider(new GUIContent("Volume", "Set global volume"), instance.MPTK_Volume, 0f, 1f);
            if (instance.MPTK_Volume != volume)
                instance.MPTK_Volume = volume;


            float overlap = EditorGUILayout.Slider(new GUIContent("Overlap (ms)", "Overlap in milliseconds between two midi"), instance.MPTK_OverlayTimeMS, 0f, 10000f);
            if (instance.MPTK_OverlayTimeMS != overlap)
                instance.MPTK_OverlayTimeMS = overlap;

            instance.MPTK_PlayOnStart = EditorGUILayout.Toggle(new GUIContent("Play On Start", "Start playing midi when component starts"), instance.MPTK_PlayOnStart);
            instance.MPTK_MidiLoop = EditorGUILayout.Toggle(new GUIContent("Loop on the list", "Enable loop on midi play list"), instance.MPTK_MidiLoop);


            //SerializedProperty indexlabFormatMidiTimeProperties = serializedObject.FindProperty("indexlabFormatMidiTime");
            //EditorGUILayout.PropertyField(indexlabFormatMidiTimeProperties);
            //indexlabFormatMidiTimeProperties.intValue = EditorGUILayout.Popup("Duration Format", indexlabFormatMidiTimeProperties.intValue, optionsFormatDuration);
            //serializedObject.ApplyModifiedProperties();

            instance.indexlabFormatMidiTime = EditorGUILayout.Popup("Duration Format", instance.indexlabFormatMidiTime, optionsFormatDuration);

            //if (GUILayout.Button(new GUIContent("Time: " + labFormatMidiTime, "Select time format"), /*GUILayout.Width(200), */GUILayout.Height(15)))
            //    if (++idxlabFormatMidiTime > 2) idxlabFormatMidiTime = 0;

        }

        private void ActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // Move midi down in the list
            if (GUILayout.Button(new GUIContent(buttonIconDnArrow, "Move down selected MIDI"), EditorStyles.miniButtonRight, miniButtonWidth, GUILayout.Height(20f)))
            {
                if (instance.MPTK_PlayList != null && indexEditItem >= 0 && indexEditItem < instance.MPTK_PlayList.Count)
                {
                    int newindex = indexEditItem >= instance.MPTK_PlayList.Count - 1 ? 0 : indexEditItem + 1;
                    MidiListPlayer.MPTK_MidiPlayItem item = instance.MPTK_PlayList[newindex];
                    instance.MPTK_PlayList[newindex] = instance.MPTK_PlayList[indexEditItem];
                    instance.MPTK_PlayList[indexEditItem] = item;
                    instance.MPTK_PlayIndex = indexEditItem = newindex;
                    instance.MPTK_ReIndexMidi();
                }
            }

            // Move midi up in the list
            if (GUILayout.Button(new GUIContent(buttonIconUpArrow, "Move up selected MIDI"), EditorStyles.miniButtonRight, miniButtonWidth, GUILayout.Height(20f)))
            {
                if (instance.MPTK_PlayList != null && indexEditItem >= 0 && indexEditItem < instance.MPTK_PlayList.Count)
                {
                    int newindex = indexEditItem == 0 ? instance.MPTK_PlayList.Count - 1 : indexEditItem - 1;
                    MidiListPlayer.MPTK_MidiPlayItem item = instance.MPTK_PlayList[newindex];
                    instance.MPTK_PlayList[newindex] = instance.MPTK_PlayList[indexEditItem];
                    instance.MPTK_PlayList[indexEditItem] = item;
                    instance.MPTK_PlayIndex = indexEditItem = newindex;
                    instance.MPTK_ReIndexMidi();
                }
                instance.MPTK_ReIndexMidi();
            }

            GUILayout.Space(20);

            if (GUILayout.Button(new GUIContent(buttonIconAdd, "Add an entry to the playing list"), EditorStyles.miniButtonRight, miniButtonWidth, GUILayout.Height(20f)))
            {
                if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0 && indexMidiSelected < MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count - 1)
                {
                    instance.MPTK_AddMidi(MidiPlayerGlobal.CurrentMidiSet.MidiFiles[indexMidiSelected]);

                    //instance.MPTK_PlayList.Add(new MidiListPlayer.MPTK_MidiPlayItem()
                    //{
                    //    Selected = true,
                    //    Type = instance.MPTK_PlayList.Count,
                    //    MidiName = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[indexMidiSelected]
                    //});
                    indexEditItem = instance.MPTK_PlayIndex = instance.MPTK_PlayList.Count - 1;
                    //GetMidiInfo(indexMidiSelected, instance.MPTK_PlayList.Count - 1);
                    scrollPlayList.y = 10000;
                }
                else
                    messages.Add(MidiPlayerGlobal.ErrorNoMidiFile, MessageType.Warning);
            }

            //GUILayout.Space(20);

            // Define popup list with Midi playlist
            if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
            {
                if (GUILayout.Button(new GUIContent(/*indexMidiSelected + " - " +*/ nameMidiSelected, "Selected MIDI File to play")/*, GUILayout.Height(30)*/))
                    InitWinSelectMidi(indexMidiSelected, MidiChanged);
            }
            else
                messages.Add(MidiPlayerGlobal.ErrorNoMidiFile, MessageType.Warning);

            //GUILayout.Space(20);

            // remove selectedInFilterList midi from the list
            if (GUILayout.Button(new GUIContent(buttonIconDelete, "Remove selected MIDI from the list"), EditorStyles.miniButtonRight, miniButtonWidth, GUILayout.Height(20f)))
            {
                if (instance.MPTK_PlayList != null && indexEditItem >= 0 && indexEditItem < instance.MPTK_PlayList.Count)
                {
                    instance.MPTK_PlayList.RemoveAt(indexEditItem);
                    instance.MPTK_ReIndexMidi();

                    if (instance.MPTK_PlayList.Count == 0)
                        messages.Add("No MIDI in the playing list", MessageType.Warning);
                    else
                    {
                        //messages.Add("Midi removed from the playing list");
                        if (instance.MPTK_PlayIndex >= instance.MPTK_PlayList.Count)
                            instance.MPTK_PlayIndex = instance.MPTK_PlayList.Count - 1;
                    }
                }
            }

            if (indexEditItem >= instance.MPTK_PlayList.Count) indexEditItem = instance.MPTK_PlayList.Count - 1;
            EditorGUILayout.EndHorizontal();
        }

        private bool DragAndDropMidiFiles()
        {
            bool filesDropped = false;
            Event e = Event.current;

            // If mouse is dragging, we have a focused task, and we are not already dragging a task
            // Seems not useful ...
            //if ((e.type == EventType.MouseDrag))
            //{
            //    // Clear out drag data (doesn't seem to do much)
            //    DragAndDrop.PrepareStartDrag();
            //    DragAndDrop.paths = null;
            //    DragAndDrop.objectReferences = new UnityEngine.Object[0];
            //    // Start the actual drag (don't know what the name is for yet)
            //    DragAndDrop.StartDrag("Copy Task");
            //    // Use the event, else the drag won't start
            //    e.Use();
            //}

            if (e.type == EventType.DragUpdated)
            {
                //Debug.Log("" + e.type + " " + e.mousePosition);
                if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
                {
                    if (dragMidiZone.x != 0 && dragMidiZone.y != 0)
                    {
                        if (dragMidiZone.Contains(e.mousePosition) && DragAndDrop.objectReferences[0].ToString() == "MThd")
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        else
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    }
                }
            }

            //if (e.type == EventType.DragExited)
            //{
            //    Debug.Log("" + e.type + " " + e.mousePosition);
            //}

            if (e.type == EventType.DragPerform)
            {
                //Debug.Log("" + e.type + " " + e.mousePosition);
                if (instance.MPTK_PlayList == null)
                {
                    Debug.Log("new List<MPTK_MidiListPlayer.MPTK_MidiPlayItem>();");
                    instance.MPTK_PlayList = new List<MidiListPlayer.MPTK_MidiPlayItem>();
                }
                if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
                {
                    if (dragMidiZone.x != 0 && dragMidiZone.y != 0)
                    {
                        if (dragMidiZone.Contains(e.mousePosition))
                        {
                            foreach (UnityEngine.Object o in DragAndDrop.objectReferences)
                                if (o.ToString() == "MThd")
                                {
                                    instance.MPTK_AddMidi(o.name);
                                    GUI.changed = true;
                                    filesDropped = true;
                                }
                        }
                    }
                }
            }
            return filesDropped;
        }
    }
}
#endif