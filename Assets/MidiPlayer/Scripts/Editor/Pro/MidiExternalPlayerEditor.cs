#if UNITY_EDITOR
#define SHOWDEFAULT
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>@brief
    /// Inspector for the midi global player component
    /// </summary>
    [CustomEditor(typeof(MidiExternalPlayer))]
    public class MidiExternalPlayerEditor : Editor
    {
        private SerializedProperty CustomEventListNotesEvent;
        private SerializedProperty CustomEventStartPlayMidi;
        private SerializedProperty CustomEventEndPlayMidi;

        private static MidiExternalPlayer instance;
        private MidiCommonEditor commonEditor;
        private static string lastDirectory = "";

        private static GUILayoutOption miniButtonWidth = GUILayout.Width(20f);

        // Manage skin
        public CustomStyle myStyle;

        void OnEnable()
        {
            try
            {
                //Debug.Log("OnEnable MidiFilePlayerEditor");
                CustomEventStartPlayMidi = serializedObject.FindProperty("OnEventStartPlayMidi");
                CustomEventListNotesEvent = serializedObject.FindProperty("OnEventNotesMidi");
                CustomEventEndPlayMidi = serializedObject.FindProperty("OnEventEndPlayMidi");

                instance = (MidiExternalPlayer)target;
                // Load description of available soundfont
                if (MidiPlayerGlobal.CurrentMidiSet == null || MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo == null)
                {
                    MidiPlayerGlobal.InitPath();
                    ToolsEditor.LoadMidiSet();
                    ToolsEditor.CheckMidiSet();
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        public override void OnInspectorGUI()
        {
            try
            {
                GUI.changed = false;
                GUI.color = Color.white;
                if (commonEditor == null) commonEditor = ScriptableObject.CreateInstance<MidiCommonEditor>();
                // Set custom Style. Good for background color 3E619800
                if (myStyle == null) myStyle = new CustomStyle();

                Event e = Event.current;

                commonEditor.DrawCaption("Midi External Player - Play from MIDI outside the MIDI DB.", "https://paxstellar.fr/midi-external-player-v2/", "d8/dc5/class_midi_player_t_k_1_1_midi_external_player.html#details");

                string helpuri = "Define an url(prefix with http:// or https://) or a full path to a local file (prefix with file://).";
                EditorGUILayout.LabelField(new GUIContent("MIDI URL or file path:", helpuri), GUILayout.Width(150));

                bool setByBrowser = false;
                string newmidi = EditorGUILayout.TextField(instance.MPTK_MidiName, myStyle.TextFieldMultiLine, GUILayout.Height(40f));
                if (newmidi != instance.MPTK_MidiName || setByBrowser)
                {
                    instance.MPTK_MidiName = newmidi;
                }


                // Select a midi from the desktop
                EditorGUILayout.BeginHorizontal(GUILayout.Width(22));
                if (GUILayout.Button(new GUIContent(MPTKGui.IconFolders, "Browse"), GUILayout.Width(32f), GUILayout.Height(32f)))
                {
                    instance.MPTK_Pause();
                    string path = EditorUtility.OpenFilePanel("Select a MIDI file", lastDirectory, "mid");
                    if (!string.IsNullOrEmpty(path))
                    {
                        lastDirectory = Path.GetDirectoryName(path);
                        instance.MPTK_MidiName = "file://" + path;
                        setByBrowser = true;
                    }
                }
                if (GUILayout.Button(new GUIContent(MPTKGui.IconClear, "Clear"), GUILayout.Width(32f), GUILayout.Height(32f)))
                {
                    instance.MPTK_MidiName = "";
                }
                EditorGUILayout.EndHorizontal();

                if (setByBrowser)
                {
                    instance.MPTK_Stop();
                    instance.MPTK_Play();
                }

                commonEditor.AllPrefab(instance);
                commonEditor.MidiFileParameters(instance);
                instance.showEvents = MidiCommonEditor.DrawFoldoutAndHelp(instance.showEvents, "Show Unity Events", "https://paxstellar.fr/midi-file-player-detailed-view-2/#Foldout-Events");
                if (instance.showEvents)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(CustomEventStartPlayMidi);
                    EditorGUILayout.PropertyField(CustomEventListNotesEvent);
                    EditorGUILayout.PropertyField(CustomEventEndPlayMidi);
                    serializedObject.ApplyModifiedProperties();
                    EditorGUI.indentLevel--;
                }
                commonEditor.MidiFileInfo(instance);
                commonEditor.SynthParameters(instance, serializedObject);

                instance.showDefault = EditorGUILayout.Foldout(instance.showDefault, "Show default editor");
                if (instance.showDefault)
                {
                    EditorGUI.indentLevel++;
                    commonEditor.DrawAlertOnDefault();
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
    }
}
#endif