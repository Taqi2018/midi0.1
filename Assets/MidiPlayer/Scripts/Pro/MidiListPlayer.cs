
using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    /// Play a list of pre-selected MIDI files from the MidiDB. This class must be used with the prefab MidiListPlayer.\n
    /// @version Maestro Pro 
    /// 
    /// See "Midi File Setup" in the Unity menu MPTK for adding MIDI in MidiDB.\n
    /// Two Midi Players are defined in the MidiListPlayer. Only one is played at a given time.\n
    /// They are switched at the end of a midi with an overlap time between.    
    /// See Midi Player Setup (Unity menu MPTK) for adding MIDI in MidiDB.\n
    /// More information here https://paxstellar.fr/midi-list-player-v2/
    /// </summary>
    /// @snippet TestMidiListPlayer.cs ExampleLogMidiListPlayer
    [HelpURL("https://paxstellar.fr/midi-list-player-v2/")]
    public class MidiListPlayer : MonoBehaviour
    {
        /// <summary>
        /// Define a MIDI file from DBMidi to be added in the list
        /// </summary>
        [Serializable]
        public class MPTK_MidiPlayItem
        {
            /// <summary>@brief
            /// Midi Name. Use the exact name defined in Unity resources folder MidiDB without any path or extension.
            /// Tips: Add Midi files to your project with the Unity menu MPTK or add it directly in the ressource folder and open Midi File Setup to automatically integrate Midi in MPTK.
            /// </summary>
            public string MidiName;

            /// <summary>@brief
            /// Select or unselect this Midi in the Inspector to apply actions (reorder, delete, ...) NO MORE USED
            /// </summary>
            public bool UIAction;

            /// <summary>@brief
            /// Select or unselect this Midi to be played in the list ...)
            /// </summary>
            public bool Selected;

            /// <summary>@brief
            /// Position of the Midi in the list. Use method MPTK_ReIndexMidi() recalculate the index.
            /// </summary>
            public int Index;

            /// <summary>@brief
            /// Time (ms) position where to start playing the midi file
            /// </summary>
            public float StartFrom;

            /// <summary>@brief
            /// Time (ms) position where to end playing the midi file
            /// </summary>
            public float EndFrom;

            //! @cond NODOC
            /// <summary>@brief
            /// value set by MPTK, don't change anything
            /// </summary>
            public long LastTick;
            /// <summary>@brief
            /// value set by MPTK, don't change anything
            /// </summary>
            public float RealDurationMs;
            /// <summary>@brief
            /// value set by MPTK, don't change anything
            /// </summary>
            public double TickLengthMs;
            //! @endcond

            override public string ToString()
            {
                return string.Format("{0} Index:{1} {2} StartFrom:{3} EndFrom:{4} LastTick:{5} RealDurationMs:{6:F3} TickLengthMs:{7:F3} ", MidiName, Index, Selected, StartFrom, EndFrom, LastTick, RealDurationMs, TickLengthMs);
            }
        }

        /// <summary>@brief
        /// Status of the player
        /// </summary>
        public enum enStatusPlayer
        {
            /// <summary>@brief
            /// Midi Player is beginning to play during the ovelap period.
            /// </summary>
            Starting,

            /// <summary>@brief
            /// Midi Player is playing at the 100% of the volume.
            /// </summary>
            Playing,

            /// <summary>@brief
            /// Midi Player is stopping to play during the ovelap period.
            /// </summary>
            Ending,

            /// <summary>@brief
            /// Midi player do nothing.
            /// </summary>
            Stopped
        }

        /// <summary>
        /// Midi Player status for the two MIDI Players defined in the MidiListPlayer.\n
        /// 
        /// Only one is playing at a time and they are switched at the end of a midi.\n
        /// There is an overlap time between them when the two are playing.
        /// </summary>
        [Serializable]
        public class MidiListPlayerStatus
        {
            /// <summary>@brief
            /// Access all the API available with MidiFilePlayer. 
            /// </summary>
            public MidiFilePlayer MPTK_MidiFilePlayer;

            /// <summary>@brief
            /// Current status of the player.
            /// </summary>
            public enStatusPlayer StatusPlayer;

            /// <summary>@brief
            /// Time end of the Midi playing in millisecond.
            /// </summary>
            public float EndAt;

            /// <summary>@brief
            /// Global volume defined for all the Midi playing.
            /// </summary>
            public float Volume;

            /// <summary>@brief
            /// Percentage of volume to applied. Range [0, 1].\n
            /// Increase from 0 to 1 when starting during the overlap time.\n
            /// Decrease from 1 to 0 when ending during the overlap time.
            /// </summary>
            public float PctVolume;

            public MidiListPlayerStatus()
            {
                //PctVolume = 100f;
                StatusPlayer = enStatusPlayer.Stopped;
            }
            public void UpdateVolume()
            {
                //if (StatusPlayer != enStatusPlayer.Stopped)
                if (MPTK_MidiFilePlayer != null)
                    MPTK_MidiFilePlayer.MPTK_Volume = Volume * PctVolume;
            }
        }

        //! @cond NODOC

        [HideInInspector]
        public bool showDefault;

        //! @endcond

        /// <summary>@brief
        /// Volume of midi playing. 
        /// Must be >=0 and <= 1
        /// </summary>
        public float MPTK_Volume
        {
            get { return volume; }
            set
            {
                if (volume >= 0f && volume <= 1f)
                {
                    SetVolume(value);
                }
                else
                    Debug.LogWarning("MidiListPlayer - Set Volume value not valid : " + value);
            }
        }

        private void SetVolume(float value)
        {
            volume = value;
            MPTK_MidiFilePlayer_1.Volume = volume;
            MPTK_MidiFilePlayer_1.UpdateVolume();
            MPTK_MidiFilePlayer_2.Volume = volume;
            MPTK_MidiFilePlayer_2.UpdateVolume();
        }

        [SerializeField]
        [HideInInspector]
        private float volume = 0.5f;

        [HideInInspector]
        //[Savable]
        public int indexlabFormatMidiTime;

        /// <summary>@brief
        /// Play list
        /// </summary>
        public List<MPTK_MidiPlayItem> MPTK_PlayList;

        /// <summary>@brief
        /// Play a specific Midi in the list.
        /// </summary>
        public int MPTK_PlayIndex
        {
            get { return playIndex; }
            set
            {
                if (MPTK_MidiFilePlayer_1 != null && MPTK_MidiFilePlayer_2 != null)
                {
                    if (MPTK_PlayList == null || MPTK_PlayList.Count == 0)
                        Debug.LogWarning("No Play List defined");
                    else if (value < 0 || value >= MPTK_PlayList.Count)
                        Debug.LogWarning("Index to play " + value + " not correct");
                    else
                    {
                        playIndex = value;
                        //Debug.Log("PlayIndex: Index to play " + playIndex + " " + MPTK_PlayList[playIndex].MidiName);
                        MidiListPlayerStatus mps1 = GetFirstAvailable;
                        //Debug.Log("PlayIndex: Play on " + mps1.MPTK_MidiFilePlayer.name);

                        if (mps1 != null && mps1.MPTK_MidiFilePlayer != null)
                        {
                            mps1.PctVolume = 0f;
                            mps1.UpdateVolume();
                            mps1.MPTK_MidiFilePlayer.MPTK_MidiName = MPTK_PlayList[playIndex].MidiName;
                            if (MidiPlayerGlobal.MPTK_SoundFontLoaded)
                            {
                                mps1.MPTK_MidiFilePlayer.MPTK_UnPause();
                                mps1.MPTK_MidiFilePlayer.MPTK_Position = 0d;

                                if (!mps1.MPTK_MidiFilePlayer.MPTK_IsPlaying)
                                {
                                    // Load description of available soundfont
                                    if (MidiPlayerGlobal.ImSFCurrent != null && MidiPlayerGlobal.CurrentMidiSet != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                                    {
                                        mps1.MPTK_MidiFilePlayer.MPTK_InitSynth();
                                        mps1.MPTK_MidiFilePlayer.MPTK_StartSequencerMidi();
                                        //if (VerboseSynth)Debug.Log(MPTK_MidiName);
                                        if (string.IsNullOrEmpty(mps1.MPTK_MidiFilePlayer.MPTK_MidiName))
                                            mps1.MPTK_MidiFilePlayer.MPTK_MidiName = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[0];
                                        int selectedMidi = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.FindIndex(s => s == mps1.MPTK_MidiFilePlayer.MPTK_MidiName);
                                        if (selectedMidi < 0)
                                        {
                                            Debug.LogWarning("MidiFilePlayer - MidiFile " + mps1.MPTK_MidiFilePlayer.MPTK_MidiName + " not found. Try with the first in list.");
                                            selectedMidi = 0;
                                            mps1.MPTK_MidiFilePlayer.MPTK_MidiName = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[0];
                                        }

                                        float endAt = MPTK_PlayList[MPTK_PlayIndex].EndFrom;

                                        if (endAt <= 0)
                                            endAt = MPTK_PlayList[MPTK_PlayIndex].RealDurationMs;

                                        // If end before start, set end after with 2 * overlay (time to up and time to down)
                                        if (endAt < MPTK_PlayList[MPTK_PlayIndex].StartFrom + 2f * MPTK_OverlayTimeMS)
                                            endAt = MPTK_PlayList[MPTK_PlayIndex].StartFrom + 2f * MPTK_OverlayTimeMS;
                                        mps1.EndAt = endAt;
                                        //Debug.Log("start play " + mps1.MPTK_MidiFilePlayer.MPTK_MidiName + " from " + MPTK_PlayList[playIndex].StartFrom + " to " + endAt);

                                        if (mps1.MPTK_MidiFilePlayer.MPTK_CorePlayer)
                                            Routine.RunCoroutine(mps1.MPTK_MidiFilePlayer.ThreadCorePlay(null,
                                                MPTK_PlayList[playIndex].StartFrom, endAt).CancelWith(gameObject), Segment.RealtimeUpdate);
                                        else
                                            Routine.RunCoroutine(mps1.MPTK_MidiFilePlayer.ThreadLegacyPlay(null,
                                                MPTK_PlayList[playIndex].StartFrom, endAt).CancelWith(gameObject), Segment.RealtimeUpdate);
                                    }
                                    else
                                        Debug.LogWarning(MidiPlayerGlobal.ErrorNoMidiFile);
                                }
                            }

                            mps1.StatusPlayer = enStatusPlayer.Starting;

                            MidiListPlayerStatus mps2 = mps1 == MPTK_MidiFilePlayer_1 ? MPTK_MidiFilePlayer_2 : MPTK_MidiFilePlayer_1;
                            if (mps2.StatusPlayer != enStatusPlayer.Stopped)
                            {
                                // Set to ending phase
                                mps2.EndAt = (float)mps2.MPTK_MidiFilePlayer.MPTK_Position + MPTK_OverlayTimeMS;
                                mps2.StatusPlayer = enStatusPlayer.Ending;
                            }
                        }
                    }
                }
            }
        }

        private MidiListPlayerStatus GetFirstAvailable
        {
            get
            {
                if (MPTK_MidiFilePlayer_1.StatusPlayer == enStatusPlayer.Stopped)
                    return MPTK_MidiFilePlayer_1;
                if (MPTK_MidiFilePlayer_2.StatusPlayer == enStatusPlayer.Stopped)
                    return MPTK_MidiFilePlayer_2;
                if (MPTK_MidiFilePlayer_1.StatusPlayer == enStatusPlayer.Ending)
                    return MPTK_MidiFilePlayer_1;
                if (MPTK_MidiFilePlayer_2.StatusPlayer == enStatusPlayer.Ending)
                    return MPTK_MidiFilePlayer_2;
                return null;
            }
        }

        public MidiListPlayerStatus MPTK_GetPlaying
        {
            get
            {
                if (MPTK_MidiFilePlayer_1 != null && MPTK_MidiFilePlayer_1.StatusPlayer == enStatusPlayer.Playing)
                    return MPTK_MidiFilePlayer_1;
                if (MPTK_MidiFilePlayer_2 != null && MPTK_MidiFilePlayer_2.StatusPlayer == enStatusPlayer.Playing)
                    return MPTK_MidiFilePlayer_2;
                return null;
            }
        }

        public MidiListPlayerStatus MPTK_GetStarting
        {
            get
            {
                if (MPTK_MidiFilePlayer_1.StatusPlayer == enStatusPlayer.Starting)
                    return MPTK_MidiFilePlayer_1;
                if (MPTK_MidiFilePlayer_2.StatusPlayer == enStatusPlayer.Starting)
                    return MPTK_MidiFilePlayer_2;
                return null;
            }
        }

        public MidiListPlayerStatus MPTK_GetEnding
        {
            get
            {
                if (MPTK_MidiFilePlayer_1.StatusPlayer == enStatusPlayer.Ending)
                    return MPTK_MidiFilePlayer_1;
                if (MPTK_MidiFilePlayer_2.StatusPlayer == enStatusPlayer.Ending)
                    return MPTK_MidiFilePlayer_2;
                return null;
            }
        }

        public MidiListPlayerStatus MPTK_GetPausing
        {
            get
            {
                if (MPTK_MidiFilePlayer_1.MPTK_MidiFilePlayer.MPTK_IsPaused)
                    return MPTK_MidiFilePlayer_1;
                if (MPTK_MidiFilePlayer_2.MPTK_MidiFilePlayer.MPTK_IsPaused)
                    return MPTK_MidiFilePlayer_2;
                return null;
            }
        }
        /// <summary>@brief
        /// Should the Midi much restart at first of the list when playing list is over?
        /// </summary>
        public bool MPTK_PlayOnStart { get { return playOnStart; } set { playOnStart = value; } }

        /// <summary>@brief
        /// Should the playing be restarted at the beginning of the list when the playlist is finished?
        /// </summary>
        public bool MPTK_MidiLoop { get { return loop; } set { loop = value; } }

        /// <summary>@brief
        /// Set or Get midi position of the currrent midi playing. Position is a time in millisecond. 
        /// Be carefull when modifying position on fly from GUI, weird behavior can happen
        /// If the Midi contains tempo change, the position could not reflect the real time from the beginning.
        /// Use MPTK_TickCurrent to change the position in tick which is independent of the tempo and the speed. 
        /// There is no effect if the Midi is not playing.
        /// @code
        /// // Be carefull when modifying position on fly from GUI, weird behavior can happen
        /// // Below change is applied only above 2 decimals.
        /// double currentPosition = Math.Round(midiFilePlayer.MPTK_Position / 1000d, 2);
        /// double newPosition = Math.Round(GUILayout.HorizontalSlider((float)currentPosition, 0f, (float)midiFilePlayer.MPTK_RealDuration.TotalSeconds, GUILayout.Width(buttonWidth)), 2);
        /// if (newPosition != currentPosition)
        /// {
        ///    Debug.Log("New position " + currentPosition + " --> " + newPosition );
        ///    midiFilePlayer.MPTK_Position = newPosition * 1000d;
        ///  }
        /// @endcode
        /// </summary>
        public double MPTK_Position
        {
            get
            {
                if (MPTK_GetPlaying != null)
                    return MPTK_GetPlaying.MPTK_MidiFilePlayer.MPTK_Position;
                else
                    return 0d;
            }
            set
            {
                if (MPTK_GetPlaying != null)
                    MPTK_GetPlaying.MPTK_MidiFilePlayer.MPTK_Position = value;
            }
        }

        /// <summary>@brief
        /// Last tick position of the currrent midi playing: Value of the tick for the last midi event in sequence expressed in number of "ticks". MPTK_TickLast / MPTK_DeltaTicksPerQuarterNote equal the duration time of a quarter-note regardless the defined tempo.
        /// </summary>
        public long MPTK_TickLast
        {
            get
            {
                if (MPTK_GetPlaying != null)
                    return MPTK_GetPlaying.MPTK_MidiFilePlayer.MPTK_TickLast;
                else
                    return 0;
            }
        }

        /// <summary>@brief
        /// Set or get the current tick position in the Midi when playing. 
        /// Midi tick is an easy way to identify a position in a song independently of the time which could vary with tempo change. 
        /// The count of ticks for a quarter is constant all along a Midi: see properties MPTK_DeltaTicksPerQuarterNote. 
        /// Example: with a time signature of 4/4 the ticks length of a bar is 4 * MPTK_DeltaTicksPerQuarterNote.
        /// Warning: if you want to set the start position, set MPTK_TickCurrent inside the processing of the event OnEventStartPlayMidi 
        /// because MPTK_Play() reset the start position to 0.
        /// Other possibility to change the position in the Midi is to use the property MPTK_Position: set or get the position in milliseconds 
        /// but tempo change event will impact also this time.
        /// More info here https://paxstellar.fr/2020/09/11/midi-timing/
        /// </summary>
        public long MPTK_TickCurrent
        {
            get
            {
                if (MPTK_GetPlaying != null)
                    return MPTK_GetPlaying.MPTK_MidiFilePlayer.MPTK_TickCurrent;
                else
                    return 0;
            }
            set
            {
                if (MPTK_GetPlaying != null)
                    MPTK_GetPlaying.MPTK_MidiFilePlayer.MPTK_TickCurrent = value;
            }
        }

        /// <summary>@brief
        /// Duration of the currrent midi playing. This duration can change during the playing when Change Tempo Event are processed.
        /// </summary>
        public TimeSpan MPTK_Duration
        {
            get
            {
                if (MPTK_GetPlaying != null)
                    return MPTK_GetPlaying.MPTK_MidiFilePlayer.MPTK_Duration;
                else
                    return TimeSpan.Zero;
            }
        }

        /// <summary>@brief
        /// Is Midi is paused ?
        /// </summary>
        public bool MPTK_IsPaused
        {
            get
            {
                if (MPTK_GetPlaying != null)
                    return MPTK_GetPlaying.MPTK_MidiFilePlayer.MPTK_IsPaused;
                return false;
            }
        }

        /// <summary>@brief
        /// Is Midi is playing ?
        /// </summary>
        public bool MPTK_IsPlaying
        {
            get
            {
                return (MPTK_GetPlaying != null || MPTK_GetStarting != null || MPTK_GetEnding != null);
                //return (MPTK_MidiFilePlayer_1.MPTK_MidiFilePlayer.MPTK_IsPlaying || MPTK_MidiFilePlayer_2.MPTK_MidiFilePlayer.MPTK_IsPlaying);
            }
        }

        /// <summary>@brief
        /// Define unity event to trigger at the start playing of each Midi in the list
        /// </summary>
        [HideInInspector]
        public EventStartMidiClass OnEventStartPlayMidi;

        /// <summary>@brief
        /// Define unity event to trigger at the end playing of each Midi in the list
        /// </summary>
        [HideInInspector]
        public EventEndMidiClass OnEventEndPlayMidi;

        /// <summary>@brief
        /// First MidiFilePlayer to play the Midi
        /// </summary>
        /// 
        public MidiListPlayerStatus MPTK_MidiFilePlayer_1;

        /// <summary>@brief
        /// Second MidiFilePlayer to play the Midi
        /// </summary>
        public MidiListPlayerStatus MPTK_MidiFilePlayer_2;

        /// <summary>@brief
        /// Duration of overlay between playing two midi in milliseconds
        /// </summary>
        public float MPTK_OverlayTimeMS;

        [SerializeField]
        [HideInInspector]
        private bool playOnStart = false, loop = false;

        [SerializeField]
        [HideInInspector]
        private int playIndex;

        void Awake()
        {
            //Debug.Log("Awake midiIsPlaying:" + MPTK_IsPlaying);
        }

        void Start()
        {
            //Debug.Log("Start MPTK_PlayOnStart:" + MPTK_PlayOnStart);

            try
            {
                MidiFilePlayer[] mfps = GetComponentsInChildren<MidiFilePlayer>();
                if (mfps == null || mfps.Length != 2)
                    Debug.LogWarning("Two MidiFilePlayer components are needed for MidiListPlayer.");
                else
                {
                    MPTK_MidiFilePlayer_1 = new MidiListPlayerStatus() { MPTK_MidiFilePlayer = mfps[0] };
                    MPTK_MidiFilePlayer_1.MPTK_MidiFilePlayer.OnEventStartPlayMidi.AddListener(EventStartPlayMidi);
                    MPTK_MidiFilePlayer_1.MPTK_MidiFilePlayer.OnEventEndPlayMidi.AddListener(EventEndPlayMidi);

                    MPTK_MidiFilePlayer_2 = new MidiListPlayerStatus() { MPTK_MidiFilePlayer = mfps[1] };
                    MPTK_MidiFilePlayer_2.MPTK_MidiFilePlayer.OnEventStartPlayMidi.AddListener(EventStartPlayMidi);
                    MPTK_MidiFilePlayer_2.MPTK_MidiFilePlayer.OnEventEndPlayMidi.AddListener(EventEndPlayMidi);
                }

                // v2.11.0
                //if (OnEventStartPlayMidi == null) OnEventStartPlayMidi = new EventStartMidiClass();
                //if (OnEventEndPlayMidi == null) OnEventEndPlayMidi = new EventEndMidiClass();

                SetVolume(volume);
                if (MPTK_PlayOnStart)
                {
                    // Find first 
                    foreach (MPTK_MidiPlayItem item in MPTK_PlayList)
                    {
                        //Debug.Log(item.ToString());
                        if (item.Selected)
                        {
                            MPTK_PlayIndex = item.Index;
                            break;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
        public void EventStartPlayMidi(string midiname)
        {
            //Debug.LogFormat($"MLP: EventStartPlayMidi {midiname}");
            try
            {
                OnEventStartPlayMidi.Invoke(midiname);
            }
            catch (Exception ex)
            {
                Debug.LogError("OnEventStartPlayMidi: exception detected. Check the callback code");
                Debug.LogException(ex);
            }
        }

        public void EventEndPlayMidi(string midiname, EventEndMidiEnum reason)
        {
            //Debug.LogFormat($"MLP: EventEndPlayMidi {midiname} reason:{reason}");
            try
            {
                OnEventEndPlayMidi.Invoke(midiname, reason);
            }
            catch (Exception ex)
            {
                Debug.LogError("EventEndPlayMidi: exception detected. Check the callback code");
                Debug.LogException(ex);
            }
        }
        public void Update()
        {
            MidiListPlayerStatus mpsPlaying = MPTK_GetPlaying;
            if (mpsPlaying != null)
            {
                if (mpsPlaying.MPTK_MidiFilePlayer.MPTK_Position > mpsPlaying.EndAt - MPTK_OverlayTimeMS)
                {
                    //Debug.Log("Time to swap to the next midi");
                    // Time to swap to the next midi
                    if (MPTK_PlayIndex < MPTK_PlayList.Count - 1 || MPTK_MidiLoop)
                        MPTK_Next();
                }
            }

            MidiListPlayerStatus mpsStarting = MPTK_GetStarting;
            if (mpsStarting != null && mpsStarting.MPTK_MidiFilePlayer.MPTK_Position != 0)
            {
                float overlayTime = (float)mpsStarting.MPTK_MidiFilePlayer.MPTK_Position - MPTK_PlayList[MPTK_PlayIndex].StartFrom;
                //Debug.Log("Starting " + mpsStarting.MPTK_MidiFilePlayer.MPTK_MidiName + " overlayTime:" + overlayTime.ToString("F3") + " " + mpsStarting.MPTK_MidiFilePlayer.MPTK_Position.ToString("F3"));
                if (MPTK_OverlayTimeMS > 0f && overlayTime < MPTK_OverlayTimeMS)
                {
                    mpsStarting.PctVolume = overlayTime / MPTK_OverlayTimeMS;
                    mpsStarting.PctVolume = Mathf.Clamp(mpsStarting.PctVolume, 0f, 1f);
                }
                else
                {
                    mpsStarting.PctVolume = 1f;
                    mpsStarting.StatusPlayer = enStatusPlayer.Playing;
                }
            }

            MidiListPlayerStatus mpsEnding = MPTK_GetEnding;
            if (mpsEnding != null)
            {
                float overlayTime = mpsEnding.EndAt - (float)mpsEnding.MPTK_MidiFilePlayer.MPTK_Position;
                //Debug.Log("Ending " + mpsEnding.MPTK_MidiFilePlayer.MPTK_MidiName + " overlayTime:" + overlayTime.ToString("F3") + " MPTK_Position:" + mpsEnding.MPTK_MidiFilePlayer.MPTK_Position.ToString("F3") + " MPTK_IsPlaying:" + mpsEnding.MPTK_MidiFilePlayer.MPTK_IsPlaying);
                if (overlayTime > 0f && MPTK_OverlayTimeMS > 0f && mpsEnding.MPTK_MidiFilePlayer.MPTK_IsPlaying)
                {
                    mpsEnding.PctVolume = overlayTime / MPTK_OverlayTimeMS;
                    mpsEnding.PctVolume = Mathf.Clamp(mpsEnding.PctVolume, 0f, 1f);
                }
                else
                {
                    mpsEnding.PctVolume = 0f;
                    mpsEnding.StatusPlayer = enStatusPlayer.Stopped;
                    mpsEnding.MPTK_MidiFilePlayer.MPTK_Stop();
                }
            }

            MPTK_MidiFilePlayer_1.UpdateVolume();
            MPTK_MidiFilePlayer_2.UpdateVolume();
        }

        /// <summary>@brief
        /// Create an empty list
        /// </summary>
        public void MPTK_NewList()
        {
            MPTK_PlayList = new List<MPTK_MidiPlayItem>();
        }

        /// <summary>@brief
        /// Add a Midi name to the list. Use the exact name defined in Unity resources (folder MidiDB) without any path or extension.
        /// Tips: Add Midi files to your project with the Unity menu MPTK or add it directly in the ressource folder and open Midi File Setup to automatically integrate Midi in MPTK.
        /// @code
        /// midiListPlayer.MPTK_AddMidi("Albinoni - Adagio");
        /// midiListPlayer.MPTK_AddMidi("Conan The Barbarian", 10000, 20000);
        /// @endcode
        /// </summary>
        /// <param name="name">midi filename as defined in resources</param>
        /// <param name="start">starting time of playing (ms). Default: start of the midi</param>
        /// <param name="end">endding time of playing (ms). Default: end of midi</param>
        /// @snippet TestMidiListPlayer.cs ExampleCreateListMidiListPlayer
        public void MPTK_AddMidi(string name, float start = 0, float end = 0)
        {
            try
            {
                MidiLoad midifile = new MidiLoad();
                if (midifile.MPTK_Load(name))
                {
                    MPTK_PlayList.Add(new MPTK_MidiPlayItem()
                    {
                        MidiName = name,
                        Selected = true,
                        Index = MPTK_PlayList.Count,
                        LastTick = midifile.MPTK_TickLast,
                        RealDurationMs = (float)midifile.MPTK_DurationMS,
                        TickLengthMs = midifile.MPTK_Pulse,
                        StartFrom = start,
                        EndFrom = end <= 0f ? (float)midifile.MPTK_DurationMS : end,
                    });
                    //Debug.Log(MPTK_PlayList[MPTK_PlayList.Count - 1].ToString());
                }
                else
                    Debug.LogWarning($"MPTK_AddMidi - midi name not correct {name}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("MPTK_AddMidi: " + ex.Message);
            }
        }

        /// <summary>@brief
        /// Change a Midi in the list
        /// </summary>
        /// <param name="name"></param>
        /// <param name="indexList"></param>
        public void MPTK_ChangeMidi(string name, int indexList)
        {
            try
            {
                if (indexList < 0 || indexList >= MPTK_PlayList.Count)
                    Debug.LogWarning($"MPTK_ChangeMidi - index not correct {indexList}");
                else
                {
                    MidiLoad midifile = new MidiLoad();
                    if (midifile.MPTK_Load(name))
                    {
                        MPTK_PlayList[indexList].MidiName = name;
                        MPTK_PlayList[indexList].Selected = true;
                        MPTK_PlayList[indexList].LastTick = midifile.MPTK_TickLast;
                        MPTK_PlayList[indexList].RealDurationMs = (float)midifile.MPTK_DurationMS;
                        MPTK_PlayList[indexList].TickLengthMs = midifile.MPTK_Pulse;
                        MPTK_PlayList[indexList].StartFrom = 0;
                        MPTK_PlayList[indexList].EndFrom = (float)midifile.MPTK_DurationMS;

                        //Debug.Log(MPTK_PlayList[indexList].ToString());
                    }
                    else
                        Debug.LogWarning($"MPTK_ChangeMidi - midi name not correct {name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("MPTK_ChangeMidi: " + ex.Message);
            }
        }

        /// <summary>@brief
        /// Remove a Midi name from the list. Use the exact name defined in Unity resources folder MidiDB without any path or extension.
        /// @code
        /// midiListPlayer.MPTK_RemoveMidi("Albinoni - Adagio");
        /// @endcode
        /// </summary>
        public void MPTK_RemoveMidi(string name)
        {
            int index = MPTK_PlayList.FindIndex(s => s.MidiName == name);
            if (index >= 0)
                MPTK_PlayList.RemoveAt(index);
            MPTK_ReIndexMidi();
        }

        /// <summary>@brief
        /// Remove a Midi at position from the list..
        /// @code
        /// // Removes the second midi in list (start at 0)
        /// midiListPlayer.MPTK_RemoveMidiAt(1);
        /// @endcode
        /// </summary>
        public void MPTK_RemoveMidiAt(int index)
        {
            if (index >= 0 && index < MPTK_PlayList.Count)
                MPTK_PlayList.RemoveAt(index);
            MPTK_ReIndexMidi();
        }

        /// <summary>@brief
        /// Get description of a play item at position.
        /// @code
        /// // GEt the second midi in list (start at 0)
        /// midiListPlayer.MPTK_GetAt(1);
        /// @endcode
        /// </summary>
        public MPTK_MidiPlayItem MPTK_GetAt(int index)
        {
            if (index >= 0 && index < MPTK_PlayList.Count)
                return MPTK_PlayList[index];
            return null;
        }

        /// <summary>@brief
        /// Recalculate the index of the midi from the list.
        /// </summary>
        public void MPTK_ReIndexMidi()
        {
            int index = 0;
            foreach (MPTK_MidiPlayItem item in MPTK_PlayList)
                item.Index = index++;
        }

        /// <summary>@brief
        /// Play the midi in list at MPTK_PlayIndex position
        /// </summary>
        public void MPTK_Play()
        {
            try
            {
                if (MidiPlayerGlobal.MPTK_SoundFontLoaded)
                {
                    // Load description of available soundfont
                    if (MidiPlayerGlobal.ImSFCurrent != null && MidiPlayerGlobal.CurrentMidiSet != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                        // Force to start the current midi index
                        MPTK_PlayIndex = MPTK_PlayIndex;
                    else
                        Debug.LogWarning(MidiPlayerGlobal.ErrorNoMidiFile);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Stop playing
        /// </summary>
        public void MPTK_Stop()
        {
            try
            {
                if (MPTK_MidiFilePlayer_1 != null)
                {
                    MPTK_MidiFilePlayer_1.MPTK_MidiFilePlayer.MPTK_Stop();
                    MPTK_MidiFilePlayer_1.StatusPlayer = enStatusPlayer.Stopped;
                }
                if (MPTK_MidiFilePlayer_2 != null)
                {
                    MPTK_MidiFilePlayer_2.MPTK_MidiFilePlayer.MPTK_Stop();
                    MPTK_MidiFilePlayer_2.StatusPlayer = enStatusPlayer.Stopped;
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Restart playing the current midi file
        /// </summary>
        public void MPTK_RePlay()
        {
            try
            {
                // Force to play the same index
                MPTK_PlayIndex = MPTK_PlayIndex;

                //if (MPTK_MidiFilePlayer_1 != null && MPTK_MidiFilePlayer_1.StatusPlayer == enStatusPlayer.Stopped)
                //{
                //    MPTK_MidiFilePlayer_1.MPTK_MidiFilePlayer.MPTK_RePlay();
                //    return;
                //}
                //if (MPTK_MidiFilePlayer_2 != null && MPTK_MidiFilePlayer_1.StatusPlayer == enStatusPlayer.Stopped)
                //    MPTK_MidiFilePlayer_2.MPTK_MidiFilePlayer.MPTK_RePlay();
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }


        /// <summary>@brief
        /// Pause the current playing
        /// </summary>
        public void MPTK_Pause()
        {
            try
            {
                if (MPTK_MidiFilePlayer_1 != null && MPTK_MidiFilePlayer_2 != null)
                {
                    if (MPTK_MidiFilePlayer_1.StatusPlayer == enStatusPlayer.Playing)
                    {
                        MPTK_MidiFilePlayer_1.MPTK_MidiFilePlayer.MPTK_Pause();
                        return;
                    }
                    if (MPTK_MidiFilePlayer_2.StatusPlayer == enStatusPlayer.Playing)
                        MPTK_MidiFilePlayer_2.MPTK_MidiFilePlayer.MPTK_Pause();
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Pause the current playing
        /// </summary>
        public void MPTK_UnPause()
        {
            try
            {
                if (MPTK_MidiFilePlayer_1 != null && MPTK_MidiFilePlayer_2 != null)
                {
                    if (MPTK_MidiFilePlayer_1.StatusPlayer == enStatusPlayer.Playing && MPTK_MidiFilePlayer_1.MPTK_MidiFilePlayer.MPTK_IsPaused)
                    {
                        MPTK_MidiFilePlayer_1.MPTK_MidiFilePlayer.MPTK_UnPause();
                        return;
                    }
                    if (MPTK_MidiFilePlayer_2.StatusPlayer == enStatusPlayer.Playing && MPTK_MidiFilePlayer_2.MPTK_MidiFilePlayer.MPTK_IsPaused)
                        MPTK_MidiFilePlayer_2.MPTK_MidiFilePlayer.MPTK_UnPause();
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Play next Midi in list
        /// </summary>
        public void MPTK_Next()
        {
            try
            {
                if (MPTK_MidiFilePlayer_1 != null && MPTK_MidiFilePlayer_2 != null)
                {
                    if (!MPTK_IsPaused)
                    {
                        int count = 0;
                        int newIndex = MPTK_PlayIndex;
                        bool find = false;
                        while (!find && count < MPTK_PlayList.Count)
                        {

                            if (newIndex < MPTK_PlayList.Count - 1)
                                newIndex++;
                            else
                                newIndex = 0;
                            if (MPTK_PlayList[newIndex].Selected)
                                find = true;
                            count++;
                        }
                        if (find)
                        {
                            MPTK_PlayIndex = newIndex;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Play previous Midi in list
        /// </summary>
        public void MPTK_Previous()
        {
            try
            {
                if (MPTK_MidiFilePlayer_1 != null && MPTK_MidiFilePlayer_2 != null)
                {
                    if (!MPTK_IsPaused)
                    {
                        int count = 0;
                        int newIndex = MPTK_PlayIndex;
                        bool find = false;
                        while (!find && count < MPTK_PlayList.Count)
                        {

                            if (newIndex > 0)
                                newIndex--;
                            else
                                newIndex = MPTK_PlayList.Count - 1;
                            if (MPTK_PlayList[newIndex].Selected)
                                find = true;
                            count++;
                        }
                        if (find)
                        {
                            MPTK_PlayIndex = newIndex;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
    }
}

