#if UNITY_EDITOR
#define SHOWDEFAULT
using System;
using UnityEditor;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>@brief
    /// Inspector for the midi global player component
    /// </summary>
    [CustomEditor(typeof(MidiInReader))]
    public class MidiInReaderEditor : Editor
    {
        private SerializedProperty CustomEventOnEventInputMidi;

        private static MidiInReader instance;
        private MidiCommonEditor commonEditor;

#if SHOWDEFAULT
        private static bool showDefault;
#endif

        // Manage skin
        public CustomStyle myStyle;


        void OnEnable()
        {
            try
            {
                instance = (MidiInReader)target;
                CustomEventOnEventInputMidi = serializedObject.FindProperty("OnEventInputMidi");
                if (!Application.isPlaying)
                {
                    // Load description of available soundfont
                    if (MidiPlayerGlobal.CurrentMidiSet == null || MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo == null)
                    {
                        MidiPlayerGlobal.InitPath();
                        ToolsEditor.LoadMidiSet();
                        ToolsEditor.CheckMidiSet();
                    }
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
                // Set custom Style. 
                if (myStyle == null) myStyle = new CustomStyle();

                GUI.changed = false;
                GUI.color = Color.white;
                if (commonEditor == null) commonEditor = ScriptableObject.CreateInstance<MidiCommonEditor>();

                //mDebug.Log(Event.current.type);

                commonEditor.DrawCaption("MIDI In Reader - Read Midi events from your Midi keyboard.", "https://paxstellar.fr/prefab-midiinreader/", "d0/d5e/class_midi_player_t_k_1_1_midi_in_reader.html#details");

                // Endpoints
                int endpointCount = 0;
                try
                {
                    endpointCount = instance.MPTK_CountEndpoints;
                }
                catch (Exception)
                {
                    MidiInReader.ErrorMidiPlugin();
                    return;
                }

                var temp = "Detected MIDI devices:";
                for (var i = 0; i < endpointCount; i++)
                {
                    temp += "\n" + instance.MPTK_GetEndpointDescription(i);
                }
                EditorGUILayout.LabelField(temp, myStyle.BlueText, GUILayout.Height(40));
                EditorGUILayout.LabelField(string.Format($"Event in Queue: {MidiKeyboard.MPTK_SizeReadQueue()}"));
                //Debug.Log(temp);
                instance.MPTK_ReadMidiInput = EditorGUILayout.Toggle(new GUIContent("Read MIDI Events", ""), instance.MPTK_ReadMidiInput);

                bool realTime = EditorGUILayout.Toggle(new GUIContent("Real Time MIDI Read", ""), instance.MPTK_RealTimeRead);
                if (instance.MPTK_RealTimeRead != realTime)
                    instance.MPTK_RealTimeRead = realTime;
                if (instance.MPTK_RealTimeRead)
                    EditorGUILayout.LabelField("Warning: Real Time Read can cause crash with Unity Editor, save your project frequently!", myStyle.LabelAlert);

                instance.MPTK_LogEvents = EditorGUILayout.Toggle(new GUIContent("Log Midi Events", ""), instance.MPTK_LogEvents);
                instance.MPTK_DirectSendToPlayer = EditorGUILayout.Toggle(new GUIContent("Send To MPTK Synth", "MIDI events are send to the MIDI player directly"), instance.MPTK_DirectSendToPlayer);
                EditorGUILayout.PropertyField(CustomEventOnEventInputMidi);
                serializedObject.ApplyModifiedProperties();
                EditorGUILayout.Separator();
                commonEditor.AllPrefab(instance);
                commonEditor.SynthParameters(instance, serializedObject);
#if SHOWDEFAULT
                showDefault = EditorGUILayout.Foldout(showDefault, "Show default editor");
                if (showDefault)
                {
                    EditorGUI.indentLevel++;
                    commonEditor.DrawAlertOnDefault();
                    DrawDefaultInspector();
                    EditorGUI.indentLevel--;
                }
#endif
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