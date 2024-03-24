using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_ANDROID && UNITY_OBOE
using Oboe.Stream;
#endif

namespace MidiPlayerTK
{
    /// <summary>@brief
    /// class extension pro
    /// @version Maestro Pro 
    /// </summary>
    public partial class MidiSynth : MonoBehaviour
    {
        /// <summary>@brief
        /// Delegate for the event OnAudioFrameStartHandler. see #OnAudioFrameStart.
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="synthTime"></param>
        public delegate void OnAudioFrameStartHandler(double synthTime);

        /// <summary>@brief
        /// this event is triggered at each start of a new audio frame from the audio engine.<br>
        /// @details
        /// The parameter (double) is the current synth time in milliseconds. See example of use.\n
        /// The callbach function will not run on the Unity thread, so you can't call Unity API except Debug.Log.
        /// @version Maestro Pro
        /// @par
        /// @code
        /// // See Assets\MidiPlayer\Demo\ProDemos\Script\EuclideSeq\TestEuclideanRhythme.cs for the full code.
        /// public void Play()
        /// {
        ///     if (IsPlaying)
        ///         midiStream.OnAudioFrameStart += PlayHits;
        ///     else
        ///         midiStream.OnAudioFrameStart -= PlayHits;
        /// }
        /// private void PlayHits(double synthTimeMS)
        /// {
        ///     if (lastSynthTime <= 0d)
        ///         // First call, init the last time
        ///         lastSynthTime = synthTimeMS;
        ///     // Calculate time in millisecond since the last loop
        ///     double deltaTime = synthTimeMS - lastSynthTime;
        ///     lastSynthTime = synthTimeMS;
        ///     timeMidiFromStartPlay += deltaTime;
        /// 
        ///     // Calculate time since last beat played
        ///     timeSinceLastBeat += deltaTime;
        /// 
        ///     // Slider SldTempo in BPM.
        ///     //  60 BPM means 60 beats in each minute, 1 beat per second, 1000 ms between beat.
        ///     // 120 BPM would be twice as fast: 120 beats in each minute, 2 per second, 500 ms between beat.
        ///     // Calculate the delay between two quarter notes in millisecond
        ///     CurrentTempo = (60d / SldTempo.Value) * 1000d;
        /// 
        ///     // Is it time to play a hit ?
        ///     if (IsPlaying && timeSinceLastBeat > CurrentTempo)
        ///     {
        ///         timeSinceLastBeat = 0d;
        ///         CurrentBeat++;
        ///     }
        /// }
        /// @endcode
        /// </summary>
        public event OnAudioFrameStartHandler OnAudioFrameStart;

        /// <summary>@brief
        /// This function is called by the MIDI sequencer before sending the MIDI message to the MIDI synthesizer.\n
        /// From version 2.10.0 the callback must return a boolean (see example). true to keep the event, false to skip it.
        /// @details
        /// It can be used like a MIDI events preprocessor: it's possible to change the value of the MIDI events and therefore change the playback of the song.\n
        /// The callback function receives a MPTKEvent object by reference (normal, it's a C# class).\n
        /// Look at https://mptkapi.paxstellar.com/d9/d50/class_midi_player_t_k_1_1_m_p_t_k_event.html \n
        /// Many changes are possible on the MIDI event: change note, velocity, channel, skip ..., even changing the MIDI type of the message!!! \n
        /// See below some examples of run-time changes.\n
        /// @version Maestro Pro 
        /// @note
        /// @li The callback is running on a system thread not on the Unity thread. Unity API call is not possible except for the Debug.Log (to be gently used, it consumes CPU)\n
        /// @li Avoid heavy processing or waiting inside the callback otherwise MIDI playing accuracy will be bad.\n
        /// @li The midiEvent is passed by reference to the callback, so re-instanciate object (midiEvent = new MPTKEvent()) or set to null, has no effect!\n
        /// @li MIDI position attributs (Tick and RealTime) can be used in your algo but changing their values has no effect, it's too late!\n
        /// @li Changing SetTempo event is too late for the MIDI Sequencer (already taken into account). But you can use midiFilePlayer.CurrentTempo to change the tempo\n
        /// @par
        /// Some examples:
        /// @code
        /// // See TestMidiFilePlayerScripting.cs for the demo.
        /// void Start()
        /// {
        ///     MidiFilePlayer midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
        ///     midiFilePlayer.OnMidiEvent = PreProcessMidi;
        /// }
        /// 
        /// // Some example 
        /// bool PreProcessMidi(MPTKEvent midiEvent)
        /// {
        ///     bool playEvent=true;
        /// 
        ///     switch (midiEvent.Command)
        ///     {
        ///         case MPTKCommand.NoteOn:
        ///             if (midiEvent.Channel != 9)
        ///                 // transpose 2 octaves
        ///                 midiEvent.Value += 24;
        ///             else
        ///                 // Drums are muted
        ///                 playEvent= false;
        ///         break;
        ///         case MPTKCommand.PatchChange:
        ///             // Remove all patch change: all channels will played the default preset 0!!!
        ///             midiEvent.Command = MPTKCommand.MetaEvent;
        ///             midiEvent.Meta = MPTKMeta.TextEvent;
        ///             midiEvent.Info = "Patch Change removed";
        ///             break;
        ///        case MPTKCommand.MetaEvent:
        ///             if (midiEvent.Meta == MPTKMeta.SetTempo)
        ///                // Tempo forced to 100
        ///                midiFilePlayer.CurrentTempo = 100
        ///             break;
        ///     }
        ///     
        ///     // true: plays this event, false to skip
        ///     return playEvent;
        /// }
        /// 
        /// @endcode
        /// </summary>
        public Func<MPTKEvent, bool> OnMidiEvent;

        /// <summary>
        /// Action executed at each quarter with: 
        ///     - time: time in milliseconds since the start of the playing MIDI.
        ///     - tick: current tick.
        ///     - measure: current measure (start from 1).
        ///     - beat: current beat (start from 1).
        /// @note
        ///     - Action is executed even if there is there no MIDI event on the beat.
        ///     - Accuracy is garantee.
        ///     - Direct call to Unity API is not possible but Debug.Log and Maestro API (example, play a sound at each beat) are allowed.
        /// @version 2.10.0
        /// @code
        /// // See TestMidiFilePlayerScripting.cs for the demo.
        /// void Start()
        /// {
        ///     MidiFilePlayer midiFilePlayer = FindObjectOfType<MidiFilePlayer>();
        ///     midiFilePlayer.OnBeatEvent = OnBeatAction;
        /// }
        /// void OnBeatAction(int time, long tick, int measure, int beat)
        /// {
        ///     Debug.Log($"Beat - time:{time} tick:{tick} tempoMap:{midiFilePlayer.MPTK_MidiLoaded.MPTK_CurrentTempoMap.Index} measure:{measure} beat:{beat}");
        /// }
        /// @endcode
        /// </summary>
        public Action<int, long, int, int> OnBeatEvent;

        public bool CheckBeatEvent(int time)
        {
            try
            {
                bool beatChange = midiLoaded.calculateBeatPlayer();

                if (!midiLoaded.EndMidiEvent && beatChange && OnBeatEvent != null)
                {
                    try
                    {
                        OnBeatEvent(time, midiLoaded.MPTK_TickPlayer, midiLoaded.MPTK_CurrentMeasure, midiLoaded.MPTK_CurrentBeat);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("OnBeatEvent: exception detected. Check your callback code.");
                        Debug.LogException(ex);
                        return false;
                    }
                    // Status has changed in the action ?
                    if (!midiLoaded.ReadyToPlay)
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            return true;
        }


        [SerializeField]
        public MPTKEffectSoundFont MPTK_EffectSoundFont;


        [SerializeField]
        public MPTKEffectUnity MPTK_EffectUnity;

        private void InitEffect()
        {
            GenModifier.InitListGenerator();

            if (CoreAudioSource != null)
            {
#if UNITY_ANDROID && UNITY_OBOE
                EditorGUILayout.LabelField("Applying this effect with Oboe can cause weird sounds or even crash some devices", myStyle.LabelAlert);
#endif
                //Debug.Log($"Create instance for effect.");


                if (MPTK_EffectSoundFont == null)
                {
                    // Instance is deserialized when MidiSynth is loaded, but not at the first load
                    //Debug.Log($"Create instance for MPTK_EffectSoundFont. <b>Set default setting in {this.name} Inspector / Synth Parameters / SoundFont Effect Parameters</b>");
                    MPTK_EffectSoundFont = ScriptableObject.CreateInstance<MPTKEffectSoundFont>();
                    MPTK_EffectSoundFont.DefaultAll();
                }
                MPTK_EffectSoundFont.Init(this);

                if (MPTK_EffectUnity == null)
                {
                    //Debug.Log($"Create instance for MPTK_EffectUnity. <b>Set default setting in {this.name} Inspector / Synth Parameters / Unity Effect Parameters</b>");
                    MPTK_EffectUnity = ScriptableObject.CreateInstance<MPTKEffectUnity>();
                    MPTK_EffectUnity.DefaultAll();
                }
                MPTK_EffectUnity.Init(this);

            }
        }



        /// <summary>@brief
        /// Spatializer Mode for the prefab MidiSpatializer
        /// @version Maestro Pro 
        /// </summary>
        public enum ModeSpatializer
        {
            /// <summary>@brief
            /// Spatial Synth are enabled to dispatch note-on by channels.\n
            /// As a reminder, only one instrument at at time can be played by a MIDI channel\n
            /// Instrument (preset) are defined by channel with the MIDI message MPTKCommand.PatchChange
            /// </summary>
            Channel,

            /// <summary>@brief
            /// Spatial Synth are enabled to dispatch note-on by tracks defined in the MIDI.\n
            /// As a reminder, multiple channels can be played on a tracks, so multiple instruments can be played on a Synth.\n
            /// Track name are defined with the Meta MIDI message SequenceTrackName. This MIDI message is always defined in MIDI, so name can be missing.
            /// </summary>
            Track,
        }

        /// <summary>@brief
        /// True if this MidiSynth is the master synth responsible to read midi events and to dispatch to other MidiSynths
        /// @version Maestro Pro 
        /// </summary>
        public bool MPTK_IsSpatialSynthMaster { get { return isSpatialSynthMaster; } }
        protected bool isSpatialSynthMaster = true; // for internal use, true only for the master midisynth responsible to read events, false for slave midisynth responsible to play note

        [HideInInspector]
        public ModeSpatializer MPTK_ModeSpatializer;

        [HideInInspector]
        public int MPTK_MaxSpatialSynth;

        /// <summary>@brief
        /// In spatialization mode not all MidiSynths are enabled.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public bool MPTK_SpatialSynthEnabled;

        /// <summary>@brief
        /// If spatialization is track mode, contains the last instrument played on this track.
        /// @version Maestro Pro 
        /// </summary>
        public string MPTK_InstrumentPlayed { get { return string.IsNullOrEmpty(instrumentPlayed) ? "" : instrumentPlayed; } }
        protected string instrumentPlayed;

        /// <summary>@brief
        /// When spatialization is track mode, contains the last instrument (preset) played.
        /// </summary>
        public int MPTK_InstrumentNum;

        /// <summary>@brief
        /// If spatialization is track mode, contains the last name of the track .
        /// @version Maestro Pro 
        /// </summary>
        public string MPTK_TrackName { get { return string.IsNullOrEmpty(trackName) ? "" : trackName; } }



        protected string trackName;

        // Play each midi events from the Midi reader (master synth) by sending midi events to the dedicated synth
        private void PlaySpatialEvent(MPTKEvent midievent)
        {
            if (MPTK_ModeSpatializer == ModeSpatializer.Channel)
            {
                // Channel mode, list of synths are indexed by channel, also send only event to the dedicated synth by channel
                MidiFilePlayer spatialChannel = SpatialSynths[midievent.Channel];
                //Debug.Log($"{MPTK_SpatialSynthIndex} {distanceToListener}");
                spatialChannel.MPTK_PlayDirectEvent((MPTKEvent)midievent/*.Clone()*/, false);
            }
            else
            {
                // Track mode
                if (midievent.Track < MPTK_MaxSpatialSynth)
                {
                    // List of synths are indexed by tracks
                    MidiSpatializer spatialTrack = (MidiSpatializer)SpatialSynths[(int)midievent.Track];
                    if (spatialTrack.MPTK_SpatialSynthEnabled)
                    {
                        switch (midievent.Command)
                        {
                            case MPTKCommand.NoteOn:
                                // Find which instrument will be played on this track
                                spatialTrack.instrumentPlayed = spatialTrack.MPTK_Channels[midievent.Channel].PresetName;
                                spatialTrack.MPTK_InstrumentNum = spatialTrack.MPTK_Channels[midievent.Channel].PresetNum;

                                //Debug.Log($"{midievent.Track} {midievent.Channel} {spatializer.instrumentPlayed}");
                                spatialTrack.MPTK_PlayDirectEvent((MPTKEvent)midievent/*.Clone()*/, false);
                                break;

                            //case MPTKCommand.NoteOff: --- send noteoff to all tracks because note off can be set to another track than the note-on !!!
                            //    spatializer.MPTK_PlayDirectEvent((MPTKEvent)midievent/*.Clone()*/, false);
                            //    break;

                            case MPTKCommand.MetaEvent:
                                switch (midievent.Meta)
                                {
                                    case MPTKMeta.SequenceTrackName:
                                        spatialTrack.trackName = midievent.Info;
                                        break;
                                }

                                foreach (MidiFilePlayer mfp in SpatialSynths)
                                    mfp.MPTK_PlayDirectEvent((MPTKEvent)midievent/*.Clone()*/, false);
                                break;

                            default:
                                foreach (MidiFilePlayer mfp in SpatialSynths)
                                    mfp.MPTK_PlayDirectEvent((MPTKEvent)midievent/*.Clone()*/, false);
                                break;
                        }
                    }
                }
                else
                    Debug.LogWarning($"Not enough Spatial Synths available Track:{midievent.Track} Max:{MPTK_MaxSpatialSynth}");
            }
        }

        // Send midi events to the UI thru the OnEventNotesMidi event
        protected void SpatialSendEvents(List<MPTKEvent> midievents)
        {
            if (midievents.Count == 1)
            {
                int indexSynth = (int)(MPTK_ModeSpatializer == ModeSpatializer.Channel ? midievents[0].Channel : midievents[0].Track);
                try
                {
                    SpatialSynths[indexSynth].OnEventNotesMidi.Invoke(midievents);
                }
                catch (Exception ex)
                {
                    Debug.LogError("OnEventNotesMidi: exception detected. Check the callback code");
                    Debug.LogException(ex);
                }
            }
            else
            {
                // Send to the channel synth
                List<MPTKEvent> synthEvents = new List<MPTKEvent>();
                foreach (MPTKEvent midievent in midievents)
                {
                    int indexSynth = (int)(MPTK_ModeSpatializer == ModeSpatializer.Channel ? midievent.Channel : midievent.Track);
                    if (SpatialSynths[indexSynth].OnEventNotesMidi != null)
                    {
                        synthEvents.Clear();
                        synthEvents.Add(midievent);

                        try
                        {
                            SpatialSynths[indexSynth].OnEventNotesMidi.Invoke(synthEvents);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("OnEventNotesMidi: exception detected. Check the callback code");
                            Debug.LogException(ex);
                        }
                    }
                }
            }
        }

        private void BuildSpatialSynth()
        {
            // Only the main midi reader instanciate all the others synths
            if (this is MidiSpatializer && MPTK_SpatialSynthIndex < 0)
            {
                MPTK_MaxSpatialSynth = Mathf.Clamp(MPTK_MaxSpatialSynth, 16, 100);
                SpatialSynths = new List<MidiFilePlayer>();//  new MidiFilePlayer[16];
                for (int idSynth = 0; idSynth < MPTK_MaxSpatialSynth; idSynth++)
                {
                    // Bad parameters could exec infinite loop, bodyguard below
                    if (lastIdSynth > 100) break;
                    //Debug.Log($"Before Instantiate synth  IdSynth:{IdSynth} channel:{channel}");
                    MidiFilePlayer mfp = Instantiate<MidiFilePlayer>((MidiFilePlayer)this);
                    //Debug.Log($"After Instantiate synth mfp.IdSynth:{mfp.IdSynth}");
                    mfp.spatialSynthIndex = idSynth;
                    mfp.name = $"Synth Id{idSynth + 1}";
                    mfp.MPTK_PlayOnStart = false;
                    mfp.MPTK_InitSynth();
                    mfp.MPTK_Spatialize = true;
                    mfp.isSpatialSynthMaster = false;
                    mfp.trackName = "";
                    mfp.instrumentPlayed = "";
                    //mfp.hideFlags = HideFlags.DontSave;
                    SpatialSynths.Add(mfp);
                }
                // Avoid set parent in the previous loop because infinite loop are created. Why? I don't known!!!
                foreach (MidiFilePlayer mfp in SpatialSynths) mfp.transform.SetParent(this.transform);
            }
        }

        private void OnDestroy()
        {
            //Debug.Log($"OnDestroy {MPTK_SpatialSynthIndex}");
            RemoveSpatialSynth();
        }

        private void RemoveSpatialSynth()
        {
            // Only the main midi reader instanciate all the others synths
            if (this is MidiSpatializer && MPTK_SpatialSynthIndex < 0)
            {
                MidiSpatializer[] goMidiGlobal = FindObjectsOfType<MidiSpatializer>();
                if (goMidiGlobal != null)
                    foreach (MidiSpatializer go in goMidiGlobal)
                    {
                        Debug.Log($"Find {go.IdSynth} {go.name}");
                        UnityEngine.Object.Destroy(go);
                    }
            }
        }

        private void StartFrame()
        {
            try
            {
                if (OnAudioFrameStart != null)
                    OnAudioFrameStart.Invoke(SynthElapsedMilli);
            }
            catch (Exception ex)
            {
                Debug.LogError("OnAudioFrameStart: exception detected. Check the callback code");
                Debug.LogException(ex);
            }
        }

        private bool StartMidiEvent(MPTKEvent midi)
        {
            try
            {
                if (OnMidiEvent != null)
                    return OnMidiEvent(midi);
            }
            catch (Exception ex)
            {
                Debug.LogError("OnMidiEvent: exception detected. Check the callback code");
                Debug.LogException(ex);
            }
            return true;
        }

        private void AnalyseActionMeta(MPTKEvent midiEvent)
        {
            if (midiEvent.Info != null && midiEvent.Info.Length > 0 && midiEvent.Info[0] == '@' /* just a quick check for efficiency */)
            {
                try
                {
                    if (midiEvent.Info.StartsWith("@ACTION:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Search string between : and ( 
                        Match match = Regex.Match(midiEvent.Info, @":(.+)\(");
                        if (match.Success)
                        {
                            string action = match.Groups[1].Value.ToUpper().Trim();
                            //Debug.Log(action);

                            /* Si vous voulez extraire la chaîne entre ( et ), vous pouvez modifier l'expression régulière comme suit: `\((.+)\)`. 
                               Cette expression régulière correspond à n'importe quelle chaîne entre ( et ) et utilise le groupe de capture `(.+)` pour capturer 
                               la chaîne recherchée. Thanks ChatGPT ;-) 
                            */
                            // Search string between ( and )
                            match = Regex.Match(midiEvent.Info, @"\((.+)\)");
                            if (match.Success)
                            {
                                string[] param = match.Groups[1].Value.ToUpper().Trim().Split(',');
                                for (int i = 0; i < param.Length; i++)
                                {
                                    param[i] = param[i].Trim();
                                    //Debug.Log(param[i]);
                                }
                                switch (action)
                                {
                                    case "INNER_LOOP":
                                        if (param.Length < 3)
                                            throw new Exception($"Bad count of parameters for '{action}'. Must be tree integer: Resume, End, Max");
                                        MPTKInnerLoop innerLoop = ((MidiFilePlayer)this).MPTK_InnerLoop;
                                        try { innerLoop.Resume = Convert.ToInt64(param[0]); } catch { throw new Exception($"Action '{action}', parameter Resume incorrect."); }
                                        try { innerLoop.End = Convert.ToInt64(param[1]); } catch { throw new Exception($"Action '{action}', parameter End incorrect."); }
                                        try { innerLoop.Max = Convert.ToInt32(param[2]); } catch { throw new Exception($"Action '{action}', parameter Max incorrect."); }
                                        if (innerLoop.Resume < 0) innerLoop.Resume = 0;
                                        if (innerLoop.End < 0) innerLoop.End = 0;
                                        if (innerLoop.Max < 0) innerLoop.Max = 0;
                                        innerLoop.Finished = false;
                                        innerLoop.Enabled = true;
                                        innerLoop.Count = 0; // reset count
                                        //innerLoop.Log = true;
                                        Debug.Log("Inner loop command detected in META " + innerLoop.ToString());
                                        break;
                                    //case "EVENT":
                                    //    if (param.Length < 2)
                                    //        throw new Exception($"Bad count of parameters for '{action}'. Must be tree integer: Resume, End, Max.");
                                    //    break;
                                    default:
                                        throw new Exception($"Action '{action}' unknown.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Format error in '{midiEvent.Info}' at tick {midiEvent.Tick} - {ex}");
                }
            }
        }

#if UNITY_ANDROID && UNITY_OBOE
        public AudioStream oboeAudioStream;
        public void InitOboe()
        {
            if (Application.isPlaying)
            {
                //MPTK_StopSynth();
                if (oboeAudioStream != null)
                {
                    oboeAudioStream.Stop();
                    oboeAudioStream.Close();
                }

                OboeManager.Initialize();
                OboeMixer mixer = new OboeMixer();

                //if (VerboseSynth)
                //    Debug.Log($"Init Oboe {MPTK_SynthRate} {DspBufferSize}");

                using (AudioStreamBuilder audioStreamBuilder = new AudioStreamBuilder
                {
                    Format = AudioFormat.Float,
                    ChannelCount = 2,
                    SampleRate = MPTK_SynthRate, // 48000, 
                    DataCallback = mixer,
                    ErrorCallback = new DefaultErrorHandler(),
                    AudioApi = AudioApi.Unspecified,
                    PerformanceMode = PerformanceMode.LowLatency,
                    SharingMode = SharingMode.Exclusive,
                    BufferCapacityInFrames = DspBufferSize, //384,
                    IsFormatConversionAllowed = true,
                    Direction = Direction.Output
                })
                {
                    Result result = audioStreamBuilder.OpenStream(out oboeAudioStream);
                    if (result != Result.OK)
                        Debug.LogError($"Oboe Error - result:{result} ");
                    else
                    {
                        mixer.processors.Add(this);
                        oboeAudioStream.Start();
                        OutputRate = oboeAudioStream.SampleRate;
                        //MPTK_InitSynth(resetActiveVoices: true);

                        if (VerboseSynth)
                        {
                            Debug.Log("------Init Oboe------");
                            Debug.Log($"    MPTKRate:{MPTK_SynthRate} Oboe  SampleRate:{oboeAudioStream.SampleRate}");
                            Debug.Log($"    DspBufferSize:{DspBufferSize}");
                            Debug.Log($"    ChannelCount:{oboeAudioStream.ChannelCount}");
                            Debug.Log($"    BufferCapacityInFrames:{oboeAudioStream.BufferCapacityInFrames}");
                            Debug.Log($"    BufferSizeInFrames:{oboeAudioStream.BufferSizeInFrames}");
                            Debug.Log($"    FramesPerCallback:{oboeAudioStream.FramesPerCallback}");
                            Debug.Log($"    PerformanceMode:{oboeAudioStream.PerformanceMode}");
                            Debug.Log($"    SampleRateConversionQuality:{oboeAudioStream.SampleRateConversionQuality}");
                            Debug.Log($"    Audio Format:{oboeAudioStream.Format}");
                            Debug.Log($"    SessionId:{oboeAudioStream.SessionId}");
                            Debug.Log($"    Usage:{oboeAudioStream.Usage}");
                            Debug.Log($"    AudioApi:{oboeAudioStream.AudioApi}");
                            Debug.Log("---------------------");
                        }
                    }
                }
            }
        }

        unsafe private void WriteAndroidSamples(void* dataArray, long ticks, int numFrames)
        {
            FLUID_BUFSIZE = numFrames;
            if (++histoCurrent >= histoDspSize.Length) histoCurrent = 0;
            histoDspSize[histoCurrent] = FLUID_BUFSIZE;
            float* data = (float*)dataArray;
            if (ActiveVoices.Count > 0)
            {
                Array.Clear(left_buf, 0, FLUID_BUFSIZE);
                Array.Clear(right_buf, 0, FLUID_BUFSIZE);

                float[] reverb_buf = null;
                float[] chorus_buf = null;
                MPTK_EffectSoundFont.PrepareBufferEffect(out reverb_buf, out chorus_buf);
                WriteAllSamples(ticks, reverb_buf, chorus_buf);
                MPTK_EffectSoundFont.ProcessEffect(reverb_buf, chorus_buf, left_buf, right_buf);

                float vol = MPTK_Volume * volumeStartStop;

                for (int i = 0; i < FLUID_BUFSIZE; i++)
                {
                    int j = i << 1;
                    data[j] = left_buf[i] * vol;
                    data[j + 1] = right_buf[i] * vol;
                }
            }
        }
#endif
    }
#if UNITY_ANDROID && UNITY_OBOE

    public class OboeMixer : AudioStreamDataCallback
    {
        public List<IMixerProcessor> processors = new List<IMixerProcessor>();

        public unsafe override DataCallbackResult OnAudioData(AudioStream audioStream, void* dataArray, int numFrames)
        {
            float* data = (float*)dataArray;

            for (int i = 0; i < numFrames; ++i)
            {
                data[i * 2] = 0;
                data[i * 2 + 1] = 0;
            }
            //Debug.Log($"cp:{processors.Count} numFrames:{numFrames}");
            foreach (var cp in processors)
            {
                cp.OnAudioData(audioStream, dataArray, numFrames);
            }
            return DataCallbackResult.Continue;
        }
    }

    public interface IMixerProcessor
    {
        unsafe void OnAudioData(AudioStream audioStream, void* dataArray, int numFrames);
    }
#endif

}


