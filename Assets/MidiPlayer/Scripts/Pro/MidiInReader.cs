//#define DEBUGPERF
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    /// Read MIDI events from a MIDI keyboard connected to your Windows or Mac desktop. This class must be used with the prefab MidiInReader.\n
    /// There is no need to writing a script. For a simple usage, all the job can be done in the prefab inspector.
    /// More information here https://paxstellar.fr/prefab-midiinreader/\n
    /// @attention MidiInReader inherits of class MidiSynth. For clarity, only MidiInReader attibutes are provided here.
    /// Look at the class MidiSynth to discover all attributes available.
    /// @version 
    ///     Maestro Pro 
    /// 
    /// Example of script. See TestMidiInputScripting.cs for a more detailed usage.\n
    /// Display each MIDI events from a MIDI device connected
    /// Need for a reference to the Prefab (can also be set from the hierarchy)
    /// @code 
    /// MidiInReader midiIn = FindObjectOfType<MidiInReader>();
    /// 
    /// if (midiIn == null) 
    ///     Debug.LogWarning("Can't find a MidiInReader Prefab in the current Scene Hierarchy. Add it with the MPTK menu.");
    ///     
    /// // There is two methods to trigger event: in inpector from the Unity editor or by script
    /// midiIn.OnEventInputMidi.AddListener((MPTKEvent evt) => 
    /// {
    ///     // your processing here
    ///     Debug.Log(evt.ToString());
    /// });
    /// @endcode
    /// </summary>
    [HelpURL("https://paxstellar.fr/prefab-midiinreader/")]
    [RequireComponent(typeof(AudioSource))]
    public class MidiInReader : MidiSynth
    {
        public MPTKChannels MPTK_Channels
        {
            get { return Channels; }
        }

        /// <summary>@brief
        /// Read Midi input
        /// </summary>
        public bool MPTK_ReadMidiInput;

        public bool MPTK_RealTimeRead
        {
            get { return realTimeRead; }
            set
            {
                //Debug.Log($"MPTK_RealTimeRead {realTimeRead} --> {value}");
                realTimeRead = value;
                if (realTimeRead)
                {
                    MidiKeyboard.OnActionInputMidi += ProcessEvent;
                    MidiKeyboard.MPTK_SetRealTimeRead();
                }
                else
                {
                    MidiKeyboard.OnActionInputMidi -= ProcessEvent;
                    MidiKeyboard.MPTK_UnsetRealTimeRead();
                }
            }
        }

        [SerializeField]
        private bool realTimeRead;

        ///// <summary>@brief
        ///// Log midi events
        ///// </summary>
        //public bool MPTK_LogEvents;

        public float MPTK_DelayToRefreshDeviceMilliSeconds = 500f;

        float timeTorefresh;

        public int MPTK_CountEndpoints
        {
            get
            {
                //Debug.Log("MPTK_CountEndpoints:" + CountEndpoints().ToString());
                return MidiKeyboard.MPTK_CountInp();
            }
        }

        public string MPTK_GetEndpointDescription(int index)
        {
            return string.Format("id:{0} name:{1}", index, MidiKeyboard.MPTK_GetInpName(index));
        }

        /// <summary>@brief
        /// Define unity event to trigger when note available from the Midi file.
        /// @code
        /// MidiInReader midiFilePlayer = FindObjectOfType<MidiInReader>(); 
        ///         ...
        /// if (!midiFilePlayer.OnEventInputMidi.HasEvent())
        /// {
        ///    // No listener defined, set now by script. NotesToPlay will be called for each new notes read from Midi file
        ///    midiFilePlayer.OnEventInputMidi.AddListener(NotesToPlay);
        /// }
        ///         ...
        /// public void NotesToPlay(MPTKEvent notes)
        /// {
        ///    Debug.Log(notes.Value);
        ///    foreach (MPTKEvent midievent in notes)
        ///    {
        ///         ...
        ///    }
        /// }
        /// @endcode
        /// </summary>
        [HideInInspector]
        public EventMidiClass OnEventInputMidi;


        new void Awake()
        {
            base.Awake();
        }

        new void Start()
        {
            try
            {
                if (OnEventInputMidi == null) OnEventInputMidi = new EventMidiClass();

                MidiKeyboard.MPTK_Init();

                MidiInReader[] list = FindObjectsOfType<MidiInReader>();
                if (list.Length > 1)
                {
                    Debug.LogWarning("No more than one MidiInReader must be present in your hierarchy, we found " + list.Length + " MidiInReader.");
                }
                MPTK_InitSynth();
                base.Start();
                // With v2.10.1 the choice is available in the inspector)
                // MPTK_EnablePresetDrum = true;
                ThreadDestroyAllVoice();
                timeTorefresh = 0f;

                if (MPTK_RealTimeRead)
                {
                    MidiKeyboard.MPTK_SetRealTimeRead();
                }
                // Force set or unset CB realtime
                //MPTK_RealTimeRead = realTimeRead;

            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        public void OnApplicationQuit()
        {
            //Debug.Log("OnApplicationQuit MPTK_UnsetRealTimeRead");
            MidiKeyboard.MPTK_UnsetRealTimeRead();
        }

        public static void ErrorMidiPlugin()
        {
            Debug.LogWarning("MidiPlugin not found, please see here a setup description https://paxstellar.fr/prefab-midiinreader");
        }

        void Update()
        {
            int count = 0;
            try
            {
                if (Time.fixedUnscaledTime > timeTorefresh)
                {
                    timeTorefresh = Time.fixedUnscaledTime + MPTK_DelayToRefreshDeviceMilliSeconds / 1000f;
                    //Debug.Log(Time.fixedUnscaledTime);
                    // Open or refresh midi input 
                    MidiKeyboard.MPTK_OpenAllInp();
                    MidiKeyboard.PluginError status = MidiKeyboard.MPTK_LastStatus;
                    if (status != MidiKeyboard.PluginError.OK)
                        Debug.LogWarning($"MIDI Keyboard error, status: {status}");
                }
                if (!MPTK_RealTimeRead)
                {
                    // Process the message queue and avoid locking Unity
                    while (MPTK_ReadMidiInput && count < 100)
                    {
                        count++;

                        MPTKEvent midievent = MidiKeyboard.MPTK_Read();

                        // No more Midi message
                        if (midievent == null)
                            break;

                        // Call event with these midi events
                        ProcessEvent(midievent);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private void ProcessEvent(MPTKEvent midievent)
        {
            try
            {
                if (OnEventInputMidi != null)
                    OnEventInputMidi.Invoke(midievent);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("OnEventInputMidi: exception detected. Check the callback code");
                Debug.LogException(ex);
            }

            if (MPTK_DirectSendToPlayer)
                MPTK_PlayDirectEvent(midievent);

            if (MPTK_LogEvents)
                Debug.Log(midievent.ToString());
        }
    }
}

