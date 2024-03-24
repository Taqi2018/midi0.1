using MEC;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MidiPlayerTK
{
    public partial class MidiFilePlayer : MidiSynth
    {

        /// <summary>@brief
        /// Defined looping condition inside the MIDI Sequencer [Pro].
        /// Instance is automatically created when the MidiFilePlayer prefab is loaded/n
        /// See class #MPTKInnerLoop.
        /// @snippet TestInnerLoop.cs ExampleMidiInnerLoop
        /// @version 2.10.0
        /// </summary>
        public MPTKInnerLoop MPTK_InnerLoop;


        /// <summary>@brief
        /// Find a Midi in the Unity resources folder MidiDB which contains searchPartOfName in it's name (case sensitive).\n
        /// Set MPTK_MidiIndex and MPTK_MidiName if the MIDI has been found
        /// @note
        ///     - Add Midi files to your project with the Unity menu Maestro.
        /// @version Maestro Pro 
        /// @code
        /// // Find the first Midi file name in MidiDB which contains "Adagio in it's name"
        /// if (midiFilePlayer.MPTK_SearchMidiToPlay("Adagio"))
        /// {
        ///     Debug.Log($"MPTK_SearchMidiToPlay: {MPTK_MidiIndex} {MPTK_MidiName}");
        ///     // And play it
        ///     midiFilePlayer.MPTK_Play();
        /// }
        /// @endcode
        /// </summary>
        /// <param name="searchPartOfName">Part of MIDI name to search in MIDI list (case sensitive)</param>
        /// <returns>true if found else false</returns>
        public bool MPTK_SearchMidiToPlay(string searchPartOfName)
        {
            int index = -1;
            try
            {
                if (!string.IsNullOrEmpty(searchPartOfName))
                {
                    if (MidiPlayerGlobal.CurrentMidiSet != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null)
                    {
                        index = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.FindIndex(s => s.Contains(searchPartOfName));
                        if (index >= 0)
                        {
                            MPTK_MidiIndex = index;
                            //Debug.Log($"MPTK_SearchMidiToPlay: {MPTK_MidiIndex} - '{MPTK_MidiName}'");
                            return true;
                        }
                        else
                            Debug.LogWarningFormat("No MIDI file found with '{0}' in name", searchPartOfName);
                    }
                    else
                        Debug.LogWarning(MidiPlayerGlobal.ErrorNoMidiFile);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return false;
        }

        /// <summary>@brief
        /// Load a MIDI file from a local desktop file. Example of path:
        ///     - Mac: "/Users/xxx/Desktop/WellTempered.mid"\n
        ///     - Windows: "C:\Users\xxx\Desktop\BIM\Sound\Midi\DreamOn.mid"\n
        /// @note
        ///     - Logs are displayed in case of error.
        ///     - Look at MPTK_StatusLastMidiLoaded for load status information.\n
        ///     - Look at MPTK_MidiLoaded for detailed information about the MIDI loaded.\n
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="uri">uri or path to the midi file</param>
        /// <returns>MidiLoad to access all the properties of the midi loaded or null in case of error</returns>
        public MidiLoad MPTK_Load(string uri)
        {
            try
            {
                MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NotYetDefined;
                if (string.IsNullOrEmpty(uri))
                {
                    MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.MidiNameInvalid;
                    Debug.LogWarning($"MPTK_Load: file path not defined");
                    return null;
                }
                else if (!File.Exists(uri))
                {
                    MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NotFound;
                    Debug.LogWarning($"MPTK_Load: {uri} not found");
                    return null;
                }
                else
                {
                    try
                    {
                        using (Stream fsMidi = new FileStream(uri, FileMode.Open, FileAccess.Read))
                        {
                            byte[] data = new byte[fsMidi.Length];
                            fsMidi.Read(data, 0, (int)fsMidi.Length);

                            if (data.Length < 4)
                            {
                                MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.TooShortSize;
                                //Debug.LogWarning($"Error Loading Midi:{pathmidiNameToPlay} - Not a midi file, too short size");
                            }
                            else if (System.Text.Encoding.Default.GetString(data, 0, 4) != "MThd")
                            {
                                MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NoMThdSignature;
                            }
                            else
                            {
                                midiLoaded = new MidiLoad();
                                midiLoaded.MPTK_KeepNoteOff = MPTK_KeepNoteOff;
                                midiLoaded.MPTK_KeepEndTrack = MPTK_KeepEndTrack;
                                midiLoaded.MPTK_LogLoadEvents = MPTK_LogLoadEvents;
                                midiLoaded.MPTK_EnableChangeTempo = MPTK_EnableChangeTempo;
                                midiLoaded.MPTK_Load(data);
                                MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.Success;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.MidiFileInvalid;
                        MidiPlayerGlobal.ErrorDetail(ex);
                        return null;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
                return null;
            }
            return midiLoaded;
        }

        /// <summary>@brief
        /// MIDI list events must be sorted by ticks before playing. It's mandatory if the list is modified. 
        /// @version 2.0.0 - Maestro Pro 
        /// @snippet TestMidiFilePlayerScripting.cs Example_GUI_PreloadAndAlterMIDI
        /// </summary>
        public void MPTK_SortEvents()
        {
            if (MPTK_MidiEvents != null)
                MPTK_MidiEvents.Sort((e1, e2) =>
                {
                    return e1.Tick.CompareTo(e2.Tick);
                });
            else
                Debug.LogWarning("MPTK_SortEvents: No MIDI loaded");
        }

        /// <summary>@brief
        /// Play next or previous Midi from the MidiDB list.
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="offset">Forward or backward count in the list. 1:the next, -1:the previous</param>
        public void MPTK_PlayNextOrPrevious(int offset)
        {
            try
            {
                if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                {
                    int selectedMidi = MPTK_MidiIndex + offset;
                    if (selectedMidi < 0)
                        selectedMidi = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count - 1;
                    else if (selectedMidi >= MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count)
                        selectedMidi = 0;
                    MPTK_MidiIndex = selectedMidi;
                    if (offset < 0)
                        prevMidi = true;
                    else
                        nextMidi = true;
                    MPTK_RePlay();
                }
                else
                    Debug.LogWarning(MidiPlayerGlobal.ErrorNoMidiFile);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Switch playing between two MIDIs with ramp-up.\n
        /// This method is useful for an integration with Bolt: main MIDI parameters are defined in one call.
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="index">Index of the MIDI to play. Index is used only if name parameter is null or empty.</param>
        /// <param name="name">Name of the MIDI to play. Can be part of the MIDI Name. If set, this parameter has the priority over index parameter.</param>
        /// <param name="volume">Volume of the MIDI. -1 to not change the default volume</param>
        /// <param name="delayToStopMillisecond">Delay to stop the current MIDI playing (with volume decrease) or delay before playing the MIDI if no MIDI is playing</param>
        /// <param name="delayToStartMillisecond">Delay to get the MIDI at full volume (ramp-up volume)</param>
        public void MPTK_SwitchMidiWithDelay(int index, string name, float volume, float delayToStopMillisecond, float delayToStartMillisecond)
        {
            if (volume >= 0f)
                MPTK_Volume = volume;
            //Debug.Log($"Search for {name}");
            if (delayToStopMillisecond < 0f) delayToStopMillisecond = 0f;
            if (delayToStartMillisecond < 0f) delayToStartMillisecond = 0f;
            MPTK_Stop(delayToStopMillisecond);

            if (!string.IsNullOrWhiteSpace(name))
            {
                MPTK_SearchMidiToPlay(name);
            }
            else
                MPTK_MidiIndex = index;

            Routine.RunCoroutine(TheadPlayWithDelay(delayToStopMillisecond, delayToStartMillisecond), Segment.RealtimeUpdate);
        }

        /// <summary>@brief
        /// Play the midi file defined with MPTK_MidiName or MPTK_MidiIndex with ramp-up to the volume defined with MPTK_Volume.\n
        /// The time to get a MIDI playing at full MPTK_Volume is delayRampUp + startDelay.\n
        /// A delayed start can also be set.
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="delayRampUp">ramp-up delay in milliseconds to get the default volume</param>
        /// <param name="startDelay">delayed start in milliseconds V2.89.1</param>
        public virtual void MPTK_Play(float delayRampUp, float startDelay = 0)
        {
            Routine.CallDelayed(startDelay / 1000f, () =>
            {
                //Debug.Log("Delayed play");
                needDelayToStop = false;
                delayRampUpSecond = delayRampUp / 1000f;
                timeRampUpSecond = Time.realtimeSinceStartup + delayRampUpSecond;
                needDelayToStart = true;
                MPTK_Play();
            });
        }
        /// <summary>@brief
        /// Play the midi file from a byte array.\n
        /// Look at MPTK_StatusLastMidiLoaded to get status.
        /// @version Maestro Pro 
        /// @code
        ///   // Example of using with Windows or MACOs
        ///   using (Stream fsMidi = new FileStream(filepath, FileMode.Open, FileAccess.Read))
        ///   {
        ///         byte[] data = new byte[fsMidi.Length];
        ///         fsMidi.Read(data, 0, (int) fsMidi.Length);
        ///         midiFilePlayer.MPTK_Play(data);
        ///   }
        /// @endcode
        /// </summary>
        public void MPTK_Play(byte[] data)
        {
            try
            {
                MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NotYetDefined;

                if (MidiPlayerGlobal.MPTK_SoundFontLoaded)
                {
                    playPause = false;

                    if (!MPTK_IsPlaying)
                    {
                        if (data == null)
                        {
                            //Debug.LogWarning("MPTK_Play: set MPTK_MidiName or Midi Url/path in inspector before playing");
                            MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.MidiNameNotDefined;
                        }
                        else if (data.Length < 4)
                        {
                            MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.TooShortSize;
                            //Debug.LogWarning($"Error Loading Midi:{pathmidiNameToPlay} - Not a midi file, too short size");
                        }
                        else if (System.Text.Encoding.Default.GetString(data, 0, 4) != "MThd")
                        {
                            MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NoMThdSignature;
                            //Debug.LogWarning($"Error Loading Midi:{pathmidiNameToPlay} - Not a midi file, signature MThd not found");
                        }
                    }
                    else
                    {
                        //Debug.LogWarning("Already playing - " + pathmidiNameToPlay);
                        MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.AlreadyPlaying;
                    }
                }
                else
                {
                    //Debug.LogWarning("SoundFont not loaded");
                    MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.SoundFontNotLoaded;
                }

                // If no error, load and play the midi in background
                if (MPTK_StatusLastMidiLoaded == LoadingStatusMidiEnum.NotYetDefined)
                {
                    MPTK_InitSynth();
                    MPTK_StartSequencerMidi();

                    // Start playing
                    if (MPTK_CorePlayer)
                        Routine.RunCoroutine(ThreadCorePlay(data).CancelWith(gameObject), Segment.RealtimeUpdate);
                    else
                        Routine.RunCoroutine(ThreadLegacyPlay(data).CancelWith(gameObject), Segment.RealtimeUpdate);
                }
                else
                {
                    try
                    {
                        if (OnEventEndPlayMidi != null)
                            OnEventEndPlayMidi.Invoke(MPTK_MidiName, EventEndMidiEnum.MidiErr);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("OnEventEndPlayMidi: exception detected. Check the callback code");
                        Debug.LogException(ex);
                    }
                }
            }

            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Play the midi from a MidiFileWriter2 object
        /// @version Maestro Pro 
        /// @snippet TestMidiGenerator.cs ExampleMIDIPlayFromWriter
        /// </summary>
        /// <param name="mfw2">aMidiFileWriter2 object</param>
        /// <param name="delayRampUp"></param>
        public void MPTK_Play(MPTKWriter mfw2, float delayRampUp = 0f, float fromPosition = 0, float toPosition = 0, long fromTick = 0, long toTick = 0, bool timePosition = true)
        {
            try
            {
                // There is no duration on noteon when created from MidiFileWriter, we need noteoff to stop the note
                playNoteOff = true;
                MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.NotYetDefined;

                if (delayRampUp > 0f)
                {
                    delayRampUpSecond = delayRampUp / 1000f;
                    timeRampUpSecond = Time.realtimeSinceStartup + delayRampUpSecond;
                    needDelayToStart = true;
                }
                else
                    needDelayToStart = false;

                if (MidiPlayerGlobal.MPTK_SoundFontLoaded)
                {
                    playPause = false;

                    if (!MPTK_IsPlaying)
                    {
                        if (mfw2 == null)
                        {
                            //Debug.LogWarning("MPTK_Play: set MPTK_MidiName or Midi Url/path in inspector before playing");
                            MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.MidiNameNotDefined;
                        }
                        //else if (mfw2.Length < 4)
                        //{
                        //    MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.TooShortSize;
                        //    //Debug.LogWarning($"Error Loading Midi:{pathmidiNameToPlay} - Not a midi file, too short size");
                        //}
                    }
                    else
                    {
                        //Debug.LogWarning("Already playing - " + pathmidiNameToPlay);
                        MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.AlreadyPlaying;
                    }
                }
                else
                {
                    //Debug.LogWarning("SoundFont not loaded");
                    MPTK_StatusLastMidiLoaded = LoadingStatusMidiEnum.SoundFontNotLoaded;
                }

                // If no error, load and play the midi in background (v2.9.0 play also when already playing)
                if (MPTK_StatusLastMidiLoaded == LoadingStatusMidiEnum.NotYetDefined || MPTK_StatusLastMidiLoaded == LoadingStatusMidiEnum.AlreadyPlaying)
                {
                    MPTK_InitSynth();
                    MPTK_StartSequencerMidi();
                    midiNameToPlay = string.IsNullOrEmpty(mfw2.MidiName) ? "(no name)" : mfw2.MidiName;
                    // Start playing
                    if (Application.isPlaying)
                        Routine.RunCoroutine(ThreadMFWPlay(mfw2, fromPosition, toPosition, fromTick, toTick, timePosition).CancelWith(gameObject).CancelWith(gameObject), Segment.RealtimeUpdate);
                    else
                        Routine.RunCoroutine(ThreadMFWPlay(mfw2, fromPosition, toPosition, fromTick, toTick, timePosition), Segment.EditorUpdate);
                }
                else
                {
                    try
                    {
                        if (OnEventEndPlayMidi != null)
                            OnEventEndPlayMidi.Invoke(mfw2.MidiName ?? "(no name)", EventEndMidiEnum.MidiErr);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("OnEventEndPlayMidi: exception detected. Check the callback code");
                        Debug.LogException(ex);
                    }
                }
            }

            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Stop playing within a delay. After the stop delay (0 by default), the volume decrease until the playing is stopped.\n
        /// The time to get a real MIDI stop is delayRampDown + stopDelay.
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="delayRampDown">decrease time in millisconds</param>
        /// <param name="stopDelay">delayed stop in milliseconds V2.89.1</param>
        public virtual void MPTK_Stop(float delayRampDown, float stopDelay = 0)
        {
            Routine.CallDelayed(stopDelay / 1000f, () =>
            {
                if (midiLoaded != null && midiIsPlaying)
                {
                    needDelayToStart = false;
                    delayRampDnSecond = delayRampDown / 1000f;
                    timeRampDnSecond = Time.realtimeSinceStartup + delayRampDnSecond;
                    needDelayToStop = true;
                }
            });
        }

        //! @cond NODOC
        public void StopAndPlayMidi(int index, string name)
        {
            MPTK_Stop();
            if (!string.IsNullOrWhiteSpace(name))
                MPTK_SearchMidiToPlay(name);
            else
                MPTK_MidiIndex = index;
            MPTK_Play();
        }

        protected IEnumerator<float> TheadPlayWithDelay(float delayToStopMillisecond, float delayToStartMillisecond)
        {
            //Debug.Log($"TheadPlayWithDelay for {delayToStopMillisecond}");
            yield return Routine.WaitForSeconds((delayToStopMillisecond + 100f) / 1000f);
            //Debug.Log($"TheadPlayWithDelay play {delayToStartMillisecond}");
            MPTK_Play(delayToStartMillisecond);
        }

        public void PlayAndPauseMidi(int index, string name, int pauseMillisecond = -1)
        {
            MPTK_Stop();
            if (!string.IsNullOrWhiteSpace(name))
                MPTK_SearchMidiToPlay(name);
            else
                MPTK_MidiIndex = index;
            MPTK_Play();
            MPTK_Pause(pauseMillisecond);
        }

        protected IEnumerator<float> ThreadMFWPlay(MPTKWriter mfw2, float fromPosition = 0, float toPosition = 0, long fromTick = 0, long toTick = 0, bool timePosition = true)
        {
            StartPlaying();
            string currentMidiName = midiNameToPlay;
            //Debug.Log("Start play " + fromPosition + " " + toPosition);
            try
            {

                midiLoaded = new MidiLoad();
                midiLoaded.MPTK_KeepNoteOff = MPTK_KeepNoteOff;
                midiLoaded.MPTK_KeepEndTrack = MPTK_KeepEndTrack;
                midiLoaded.MPTK_LogLoadEvents = MPTK_LogLoadEvents;
                midiLoaded.MPTK_EnableChangeTempo = MPTK_EnableChangeTempo;
                if (!midiLoaded.MPTK_Load(mfw2))
                    midiLoaded = null;
#if DEBUG_START_MIDI
                Debug.Log("After load midi " + (double)watchStartMidi.ElapsedTicks / ((double)System.Diagnostics.Stopwatch.Frequency / 1000d));
#endif
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            if (midiLoaded != null)
            {
                if (!timePosition)
                {
                    midiLoaded.MPTK_TickStart = fromTick;
                    midiLoaded.MPTK_TickEnd = toTick;
                }
                if (Application.isPlaying)
                    Routine.RunCoroutine(ThreadInternalMidiPlaying(currentMidiName, fromPosition, toPosition).CancelWith(gameObject), Segment.RealtimeUpdate);
                else
                    Routine.RunCoroutine(ThreadInternalMidiPlaying(currentMidiName, fromPosition, toPosition), Segment.EditorUpdate);
            }
            yield return 0;
        }
        //! @endcond


    }
}

