using MPTK.NAudio.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    /// Create, build, write, import, play MIDI by script. See full example with these scripts:
    /// @li  TestMidiGenerator.cs for MIDI creation examples. 
    /// @li  TinyMidiSequencer.cs for a light sequencer.
    /// @li  MidiEditorProWindow.cs.cs for a full Midi Editor.
    /// @note
    /// Method like MPTK_AddxxxxMilli will be soon deprecated. Explanation:\n
    /// Adding MIDI event is done with method like MPTK_Addxxxxxxx. There is two kind of method: adding with a tickFromTime position (AddNote) or with a time position (AddNoteMilli).
    /// For clarity, all MPTK_AddxxxxMilli will be deprecated in future version. The method #MPTK_ConvertTickToMilli will be used in place.
    /// More information here: https://paxstellar.fr/class-MPTKWriter/
    ///
    /// @version 
    ///     Maestro Pro 
    ///
    /// @snippet TestMidiGenerator.cs ExampleFullMidiFileWriter
    /// </summary>
    public class MPTKWriter
    {
        /// <summary>@brief
        /// Delta Ticks Per Beat Note (or DTPQN) represent the duration time in "ticks" which make up a quarter-note. \n
        /// For example, with 96 a duration of an eighth-note in the file would be 48.\n
        /// From a MIDI file, this value is found in the MIDI Header and remains constant for all the MIDI file.\n
        /// More info here https://paxstellar.fr/2020/09/11/midi-timing/\n
        /// </summary>
        public int DeltaTicksPerQuarterNote;

        //private int _bpm;
        // version 2.10.0 was an int, becomes a double
        private double _bpm;

        /// <summary>@brief
        /// Get current Microseconds Per Quater Note:  60 * 1000 * 1000 https://en.wikipedia.org/wiki/Tempo\n
        /// @details
        /// The tempo in a MIDI file is given in micro seconds per quarter beat. To convert this to BPM use method #QuarterPerMicroSecond2BeatPerMinute.\n
        /// This value can change during the generation when #AddTempoChange is called.\n
        /// See here for more information https://paxstellar.fr/2020/09/11/midi-timing/        
        /// </summary>
        public int MicrosecondsPerQuaterNote
        {
            get { return _bpm > 0 ? (int)(60 * 1000 * 1000 / _bpm) : 0; }
        }


        /// <summary>@brief
        /// V2.9.0 - Get the current tempo. Set with #AddTempoChange.\n
        /// https://en.wikipedia.org/wiki/Tempo
        /// </summary>
        public double CurrentTempo => _bpm;

        /// <summary>@brief
        /// List of tempo changes found in #MPTK_MidiEvents. Must be updated with MPTKTempo.MPTK_CalculateMap.\n
        /// See example:
        /// @snippet TestMidiGenerator.cs ExampleCalculateMaps
        /// @version 2.10.0
        /// </summary>
        public List<MPTKTempo> MPTK_TempoMap;

        /// <summary>@brief
        /// List of signature changes found in #MPTK_MidiEvents. Must be updated with MPTKSignature.MPTK_CalculateMap.\n
        /// See example:
        /// @snippet TestMidiGenerator.cs ExampleCalculateMaps
        /// @version 2.10.0
        /// </summary>
        public List<MPTKSignature> MPTK_SignMap;


        /// <summary>@brief
        /// Get current lenght in millisecond of a MIDI tickFromTime (related to the current tempo).\n
        /// @details
        /// Obviously depends on the current tempo (#CurrentTempo) and the #MPTK_DeltaTicksPerQuarterNote.\n
        /// PulseLenght = 60000 / #CurrentTempo / #MPTK_DeltaTicksPerQuarterNote
        /// </summary>
        public float PulseLenght { get { return _bpm > 0 && DeltaTicksPerQuarterNote > 0 ? (60000f / (float)_bpm) / (float)DeltaTicksPerQuarterNote : 0f; } }

        /// <summary>@brief
        /// Convert an absolute tickFromTime position to a time in millisecond.\n
        /// </summary>
        /// <param name="tick">Absolute tickFromTime position</param>
        /// <param name="indexTempo">Index in #MPTK_TempoMap for this position. Optional, if not defined or -1, #MPTK_TempoMap will be recalculated and the segment for this position will be searched</param>
        /// <returns>Duration in milliseconds</returns>
        public float ConvertTickToMilli(long tick, int indexTempo = -1)
        {
            if (indexTempo < 0)
            {
                MPTKTempo.CalculateMap(DeltaTicksPerQuarterNote, MPTK_MidiEvents, MPTK_TempoMap);
                indexTempo = MPTKTempo.FindSegment(MPTK_TempoMap, tick, fromIndex: 0);
            }
            return (float)MPTK_TempoMap[indexTempo].CalculateTime(tick);
        }

        /// <summary>@brief
        /// Convert a time position in millisecond to an absolute tickFromTime position.\n
        /// </summary>
        /// <param name="time">Time position in milliseconds</param>
        /// <param name="indexTempo">Index in #MPTK_TempoMap for this position. Optional, if not defined or -1, #MPTK_TempoMap will be recalculated and the segment for this position will be searched</param>
        /// <returns>Absolute tickFromTime position</returns>
        public long ConvertMilliToTick(float time, int indexTempo = -1)
        {
            if (indexTempo < 0)
            {
                MPTKTempo.CalculateMap(DeltaTicksPerQuarterNote, MPTK_MidiEvents, MPTK_TempoMap);
                indexTempo = MPTKTempo.FindSegment(MPTK_TempoMap, time, fromIndex: 0);
            }
            return MPTK_TempoMap[indexTempo].CalculatelTick(time);
        }

        /// <summary>@brief
        /// Convert the tickFromTime duration to a time duration in millisecond regarding the current tempo.\n
        /// @note Previous call to #AddBPMChange and #AddTempoChange have direct impact on this calculation.
        /// </summary>
        /// <param name="tick">duration in ticks</param>
        /// <returns>duration in milliseconds</returns>
        public long DurationTickToMilli(long tick)
        {
            return (long)(tick * PulseLenght + 0.5f);
        }

        /// <summary>@brief
        /// Convert the tickFromTime duration to a real time duration in millisecond regarding the current tempo.\n
        /// @note Previous call to #AddBPMChange and #AddTempoChange have direct impact on this calculation.
        /// </summary>
        /// <param name="tick">duration in ticks</param>
        /// <returns>duration in milliseconds</returns>
        public long DurationMilliToTick(float time)
        {
            return PulseLenght > 0d ? (long)(time / PulseLenght + 0.5f) : 0L;
        }

        /// <summary>@brief
        /// Get the count of track. The value is available only when CreateTracksStat() has been called.
        /// There no more limit of count of track with V2.9.0
        /// </summary>
        public int TrackCount { get { return MPTK_TrackStat?.Count ?? 0; } }

        /// <summary>@brief
        /// Get the MIDI file type of the loaded MIDI (0,1,2)
        /// </summary>
        public int MidiFileType;

        /// <summary>@brief
        /// Name of this MIDI.
        /// </summary>
        public string MidiName;

        /// <summary>@brief
        /// Get all the MIDI events created.
        /// @code
        /// midiFileWriter.MPTK_MidiEvents.ForEach(midiEvent =>
        /// {
        ///     midiEvent.Tick += shiftTick;
        ///     midiEvent.RealTime += shiftTime;
        /// });
        /// @endcode
        /// </summary>
        public List<MPTKEvent> MPTK_MidiEvents;

        /// <summary>@brief
        /// Last MIDI events created.
        /// </summary>
        public MPTKEvent MPTK_LastEvent => MPTK_MidiEvents == null || MPTK_MidiEvents.Count == 0 ? null : MPTK_MidiEvents[MPTK_MidiEvents.Count - 1];

        /// <summary>@brief
        /// Tick position of the last MIDI event found including the duration of this event.
        /// </summary>
        public long TickLast;

        // @cond NODOC
        // Not yet mature to be published.
        // Track information, built with CreateTracksStat. It's a dictionary with the track number as a key and the item holds some information about the track.
        public Dictionary<long, MPTKStat> MPTK_TrackStat;
        // @endcond

        /// <summary>@brief
        /// Count of MIDI events in the MPTK_Events
        /// </summary>
        public int CountEvent
        {
            get { return MPTK_MidiEvents == null ? 0 : MPTK_MidiEvents.Count; }
        }

        /// <summary>@brief
        /// Create an empty MPTKWriter with default or specific header midi value (for advanced use)\n
        /// Default:\n
        /// @li Delta Ticks Per Beat Note = 240 \n
        /// @li Midi file type = 1 \n
        /// @li Beats Per Minute = 120\n
        /// @snippet MidiEditorProWindow.cs.cs ExampleInitMidiFileWriter
        /// 
        /// </summary>
        /// <param name="deltaTicksPerQuarterNote">Delta Ticks Per Beat Note, default is 240. See #MPTK_DeltaTicksPerQuarterNote.</param>
        /// <param name="midiFileType">type of Midi format. Must be 0 or 1, default 1</param>
        /// <param name="bpm">Initial Beats Per Minute, default 120</param>
        public MPTKWriter(int deltaTicksPerQuarterNote = 240, int midiFileType = 1, int bpm = 120)
        {
            MPTK_MidiEvents = new List<MPTKEvent>();
            DeltaTicksPerQuarterNote = deltaTicksPerQuarterNote;
            //MPTK_NumberBeatsMeasure = 4;
            MidiFileType = midiFileType;
            TickLast = 0;
            MPTK_TempoMap = new List<MPTKTempo>();
            MPTK_SignMap = new List<MPTKSignature>();

            _bpm = bpm;
        }

        /// <summary>@brief
        /// Remove all MIDI events and restore default attributs:
        /// @li MPTK_DeltaTicksPerQuarterNote = 240
        /// @li MPTK_MidiFileType = 1
        /// @li Tempo = 120
        /// </summary>
        public void Clear()
        {
            if (MPTK_TrackStat != null)
                MPTK_TrackStat.Clear();
            //MPTK_DeltaTicksPerQuarterNote = 240;
            MidiFileType = 1;
            _bpm = 120;
            TickLast = 0;
            MPTK_MidiEvents.Clear();
            MPTK_TempoMap.Clear();
            MPTK_SignMap.Clear();
        }



        /// <summary>@brief
        /// New with version V2.9.0 Import a list of MptkEvent.\n
        /// @details
        /// Multiple imports can be done for joining MIDI events from different sources @emoji grin.\n
        /// @details
        /// @li The first import will be the reference for the DeltaTicksPerQuarterNote (MPTK_DeltaTicksPerQuarterNote is set with the value in parameter).
        /// @li The next imports will convert time and duration of the MIDI events with the ratio of DeltaTicksPerQuarterNote in parameter and the initial DeltaTicksPerQuarterNote.
        /// @li real time, measure and beat for each events are recalculated for the whole MIDI events at the end off the import with #CalculateTiming()

        /// Example from MIDI Generator
        /// @snippet TestMidiGenerator.cs ExampleMIDIImport
        /// \n
        /// Example from MIDI Join And Import
        /// @snippet MidiJoinAndImport.cs ExampleMIDIImportAndPlay
        /// \n
        /// </summary>
        /// <param name="midiEventsToInsert">List of MptkEvent to insert</param>
        /// <param name="deltaTicksPerQuarterNote">
        /// It's the DTPQN of the MIDI events to insert. \n
        /// @li If there is not yet MIDI events in #MPTK_MidiEvents, that will be the default #DeltaTicksPerQuarterNote of the MPTKWriter instance.\n
        /// @li If there is already MIDI events in #MPTK_MidiEvents, the timing of MIDI events in #midiEventsToInsert will be converted accordingly.\n
        /// </param>
        /// <param name="position">tickFromTime position to insert, -1 to append, 0 at beguinning</param>
        /// <param name="name">Name of the MIDI created (set MPTK_MidiName).</param>
        /// <param name="logDebug">Debug log.</param>
        /// <returns>true if no error</returns>
        public bool ImportFromEventsList(List<MPTKEvent> midiEventsToInsert, int deltaTicksPerQuarterNote, long position = -1, string name = null, bool logPerf = false, bool logDebug = false)
        {
            bool ok = false;
            try
            {

                if (!string.IsNullOrEmpty(name))
                    MidiName = name;

                if (logDebug) Debug.Log($"***** MPTK_ImportFromEventsList to {name}");

                if (deltaTicksPerQuarterNote <= 0)
                    throw new MaestroException($"deltaTicksPerQuarterNote cannot be < 0, found {deltaTicksPerQuarterNote}");

                System.Diagnostics.Stopwatch watch = null;
                if (logPerf)
                {
                    watch = new System.Diagnostics.Stopwatch(); // High resolution time
                    watch.Start();
                }

                // MPTK_Events is instancied with the instance of the class, so no worry, can't be null (check just in case)
                if (MPTK_MidiEvents.Count == 0)
                {
                    // No event, add at beginning
                    position = 0;
                    // when no event already exist, take the DTPQN in parameters
                    DeltaTicksPerQuarterNote = deltaTicksPerQuarterNote;
                    if (logDebug) Debug.Log($"Set MPTK_DeltaTicksPerQuarterNote from paremeter: {DeltaTicksPerQuarterNote}");
                }

                float ratioDTPQN = 1f;
                long shiftTick = 0;
                //float shiftTime = 0f;

                if (deltaTicksPerQuarterNote != DeltaTicksPerQuarterNote)
                    ratioDTPQN = (float)DeltaTicksPerQuarterNote / (float)deltaTicksPerQuarterNote;

                if (logDebug)
                {
                    Debug.Log($"Count events in source={MPTK_MidiEvents.Count} MPTK_DeltaTicksPerQuarterNote={DeltaTicksPerQuarterNote}");
                    Debug.Log($"Count events to import={midiEventsToInsert.Count} DTPQN={deltaTicksPerQuarterNote}");
                    Debug.Log($"ratio DTPQN = {ratioDTPQN}");
                }

                if (position == 0)
                {
                    // Insert at the beguining, get event information from the last MIDI event to insert
                    // ---------------------------------------------------------------------------------
                    if (logDebug) Debug.Log("Insert at the beguining");

                    if (ratioDTPQN != 1f)
                    {
                        if (logDebug) Debug.Log("Convert imported events to the DTPQN original");

                        // DTPQN conversion (real time will be recalculated at the end of the full MIDI events list)
                        midiEventsToInsert.ForEach(midiEvent =>
                            {
                                midiEvent.Tick = (long)(midiEvent.Tick * ratioDTPQN + 0.5f);
                                //midiEvent.RealTime = midiEvent.RealTime * ratioDTPQN;
                                midiEvent.Length = (int)(midiEvent.Length * ratioDTPQN + 0.5f);
                            }
                        );
                    }

                    if (MPTK_MidiEvents.Count != 0)
                    {
                        // Convert existing events (but no change of the DTPQN)
                        MPTKEvent insert = midiEventsToInsert.Last();
                        shiftTick = insert.Tick + insert.Length; // only noteon have a length, be careful with endtrack at the last position 
                        //shiftTime = insert.RealTime;
                        if (logDebug) Debug.Log($"Shift {MPTK_MidiEvents.Count} source events, shift tickFromTime={shiftTick}");

                        // time shift source event  (real time will be recalculated at the end of the full MIDI events list)
                        MPTK_MidiEvents.ForEach(midiEvent =>
                            {
                                midiEvent.Tick += shiftTick;
                                // No change on Length (tickFromTime duration) as the original DTPQN is not modified
                                //midiEvent.RealTime += shiftTime;
                            }
                        );
                    }
                    else if (logDebug) Debug.Log("No events in source, no time shifting");

                    // Insert at beguining
                    MPTK_MidiEvents.InsertRange(0, midiEventsToInsert);
                }
                else if (position < 0 || position >= MPTK_MidiEvents.Last().Tick)
                {
                    // Append at the end (or after !), get event information from the last MIDI event of the source
                    // -------------------------------------------------------------------------------
                    if (logDebug)
                        if (position < 0)
                            Debug.Log("Append at the end of the source");
                        else
                            Debug.Log("Append after the end of the source");

                    MPTKEvent insert = MPTK_MidiEvents.Last();
                    shiftTick = insert.Tick + insert.Length;  // only noteon have a length, be careful with endtrack at the last position 
                    if (position > 0)
                        shiftTick += (long)(position * ratioDTPQN + 0.5f);

                    //shiftTime = insert.RealTime;
                    if (logDebug) Debug.Log($"Shift {midiEventsToInsert.Count} imported events, shift tickFromTime={shiftTick}");
                    if (logDebug) AddText(0, insert.Tick, MPTKMeta.TextEvent, "Insert MIDI");

                    // shift event to append + DTPQN conversion  (real time will be recalculated at the end of the full MIDI events list)
                    midiEventsToInsert.ForEach(midiEvent =>
                        {
                            midiEvent.Tick = (long)(midiEvent.Tick * ratioDTPQN + 0.5f) + shiftTick;
                            //midiEvent.RealTime = midiEvent.RealTime * ratioDTPQN + shiftTime;
                            midiEvent.Length = (int)(midiEvent.Length * ratioDTPQN + 0.5f);
                        }
                    );


                    // Append, works also if there is no event in the source
                    MPTK_MidiEvents.AddRange(midiEventsToInsert);
                }
                else
                {
                    // Insert anywhere inside the source MIDI events!!!
                    // Get event information from the last MIDI event of the source
                    // ------------------------------------------------------------
                    if (logDebug) Debug.Log($"Insert at tickFromTime position {position}");

                    int indexToInsert = MidiLoad.MPTK_SearchEventFromTick(MPTK_MidiEvents, position);
                    if (logDebug) Debug.Log($"Insert at index position {indexToInsert}");
                    if (indexToInsert < 0)
                        Debug.Log($"Not possible to insert at position tickFromTime {position}");
                    else
                    {
                        //// insert after the group with the same tickFromTime in origine
                        //while (index < MPTK_MidiEvents.Count && MPTK_MidiEvents[index].Tick >= position)
                        //    index++;
                        //Debug.Log($"Insert corrected {index} for tickFromTime {position}");

                        // get  event information from the MIDI event where to insert
                        MPTKEvent sourceEvent = MPTK_MidiEvents[indexToInsert];
                        shiftTick = sourceEvent.Tick + sourceEvent.Length;
                        //shiftTime = sourceEvent.RealTime;

                        if (logDebug) Debug.Log($"Shift {midiEventsToInsert.Count} imported events, shift tickFromTime={shiftTick}");

                        // Time shift event to insert + DTPQN conversion  (real time will be recalculated at the end of the full MIDI events list)
                        midiEventsToInsert.ForEach(midiEvent =>
                            {
                                midiEvent.Tick = (long)(midiEvent.Tick * ratioDTPQN + 0.5f) + shiftTick;
                                //midiEvent.RealTime = midiEvent.RealTime * ratioDTPQN + shiftTime;
                                midiEvent.Length = (int)(midiEvent.Length * ratioDTPQN + 0.5f);
                            }
                        );

                        // Insert at the position found
                        MPTK_MidiEvents.InsertRange(indexToInsert, midiEventsToInsert);

                        // Correct events after (time shift of the source events), take tickFromTime of the last events inserted
                        MPTKEvent insert = midiEventsToInsert.Last();
                        shiftTick = insert.Tick;
                        //shiftTime = insert.RealTime;

                        if (logDebug) Debug.Log($"Shift {midiEventsToInsert.Count} source events from postiion {indexToInsert}, shift tickFromTime={shiftTick} ");

                        for (int index = indexToInsert + midiEventsToInsert.Count; index < MPTK_MidiEvents.Count; index++)
                        {
                            // No change on Length (tickFromTime duration) as the original DTPQN is not modified
                            MPTK_MidiEvents[index].Tick += shiftTick;
                            //MPTK_MidiEvents[index].RealTime += shiftTime;
                        }
                    }
                    //MPTK_MidiEvents.RemoveRange(index, MPTK_MidiEvents.Count - index);
                    ok = true;
                }

                // Calculate real time, measure and beat for each events
                // -----------------------------------------------------
                CalculateTiming(logPerf: logPerf);

                if (logPerf)
                {
                    Debug.Log($"MPTK_ImportFromEventsList {watch.ElapsedMilliseconds} ms {watch.ElapsedTicks} timer ticks");
                    watch.Stop();
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /*
        /// <summary>@brief
        /// 
        /// </summary>
        /// <param name="frequency">
        ///     With frequency \n
        ///         @li  >  0 insert Game Event each "frequency" bar (1, 2, 3, ...)
        ///         @li  =  0 insert nothing but removes each Game Event if remove=True
        ///         @li  = -1 insert Game Event each Half
        ///         @li  = -2 insert Game Event each Beat
        ///         @li  = -3 insert Game Event each Eighth
        /// </param>
        /// <param name="info">
        ///     Free text to identify your Game Event. The ful text received in the callback will be:\n
        ///     MPTK_[info]_[bar]_[half]_[quarter]_[eighth]
        ///     Split the string to retrieve each part string[] id = info.Split('_');
        ///     id[1] will contain your specific info
        ///     NON plutot faire des parametres specifiques dans le callback
        /// </param>
        /// <param name="remove"></param>
        /// <param name="fromTick"></param>
        /// <param name="toTick"></param>
        /// <param name="logDebug"></param>
        /// <returns></returns>
        public bool MPTK_InsertGameEvent(int frequency, string info = null, bool remove = false, long fromTick = 0, long toTick = -1, bool logDebug = false)
        {
            string data = "THExxQUICKxxBROWNxxFOX";

            data.Split(new string[] { "xx" }, StringSplitOptions.None);
            return true;
        }
        */

        /// <summary>@brief
        /// Load a Midi file from OS system file (could be dependant of the OS)
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>true if no error</returns>
        public bool LoadFromFile(string filename)
        {
            bool ok = false;
            try
            {
                MidiLoad midiLoad = new MidiLoad();
                //midiLoad.MPTK_KeepNoteOff = true;
                if (midiLoad.MPTK_LoadFile(filename)) // corrected in 2.89.5 MPTK_Load --> MPTK_LoadFile (pro)
                {
                    MPTK_MidiEvents = midiLoad.MPTK_MidiEvents;
                    // Added in 2.89.5
                    DeltaTicksPerQuarterNote = midiLoad.MPTK_DeltaTicksPerQuarterNote;
                    ok = true;
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>@brief
        /// Create a MPTKWriter from a Midi found in MPTK MidiDB. All existing MIDI events before the load will be lost.
        /// If you add some MIDI events after loading, don't forget to sort the MIDI events after.
        /// See example.
        /// @snippet TestMidiGenerator.cs ExampleMidiWileWriterLoadMidi
        /// </summary>
        /// <param name="indexMidiDb"></param>
        /// <returns>true if no error</returns>
        public bool LoadFromMidiDB(int indexMidiDb)
        {
            bool ok = false;
            try
            {
                if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null && MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                {
                    if (indexMidiDb >= 0 && indexMidiDb < MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count - 1)
                    {
                        string midiname = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[indexMidiDb];
                        TextAsset mididata = Resources.Load<TextAsset>(Path.Combine(MidiPlayerGlobal.MidiFilesDB, midiname));
                        MidiLoad midiLoad = new MidiLoad();
                        //midiLoad.MPTK_KeepNoteOff = true;
                        midiLoad.MPTK_Load(mididata.bytes);
                        // Corrected with version 2.10.1 - Delta ticks per quarter was lost when MIDI loaded in MidiFileWriter
                        DeltaTicksPerQuarterNote = midiLoad.MPTK_DeltaTicksPerQuarterNote;
                        MPTK_MidiEvents = midiLoad.MPTK_MidiEvents;
                        ok = true;
                    }
                    else
                        Debug.LogWarning("Index is out of the MidiDb list");
                }
                else
                    Debug.LogWarning("No MidiDb defined");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>
        /// Build the tracks information (MPTK_TrackStat) from the MIDI event found in MPTK_Events.
        /// MPTK_TrackStat is a dictionary with the track number as a key and the item holds some informatio about the track.
        /// </summary>
        /// <returns>Stat dictionary</returns>
        /// <exception cref="MaestroException"></exception>
        public Dictionary<long, MPTKStat> CreateTracksStat()
        {
            if (MPTK_MidiEvents == null)
                throw new MaestroException("MPTK_Events is null");
            foreach (MPTKEvent midiEvent in MPTK_MidiEvents)
                UpdateStatTrack(midiEvent);
            return MPTK_TrackStat;
        }

        private void UpdateStatTrack(MPTKEvent midiEvent)
        {
            if (MPTK_TrackStat == null) MPTK_TrackStat = new Dictionary<long, MPTKStat>();
            if (!MPTK_TrackStat.ContainsKey(midiEvent.Track)) MPTK_TrackStat[midiEvent.Track] = new MPTKStat();
            MPTK_TrackStat[midiEvent.Track].CountAll++;
            if (midiEvent.Command == MPTKCommand.NoteOn) MPTK_TrackStat[midiEvent.Track].CountNote++;
            if (midiEvent.Command == MPTKCommand.PatchChange) MPTK_TrackStat[midiEvent.Track].CountPreset++;
        }

        /// <summary>@brief
        /// Add a MPTK Midi event from a MptkEvent instance. Useful to add a raw MIDI event.\n
        /// @details
        /// These attributs must be defined in the MptkEvent instance:
        /// @li MptkEvent.Track
        /// @li MptkEvent.Channel
        /// @li MptkEvent.Command
        /// @li MptkEvent.Tick
        /// @note
        /// Others attributs must be defined depending on the value of MptkEvent.Command, see class MidiPlayerTK.MPTKCommand.\n
        /// For example, MptkEvent.Length must be defined if MptkEvent.Command=MPTKCommand.NoteOn
        /// </summary>.
        /// <param name="mptkEvent"></param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddRawEvent(MPTKEvent mptkEvent)
        {
            try
            {
                if (mptkEvent == null) throw new MaestroException($"mptkEvent is null");
                if (mptkEvent.Channel < 0 || mptkEvent.Channel > 15) throw new MaestroException($"The channel must be >= 0 and <= 15, found {mptkEvent.Channel}");
                if (mptkEvent.Tick < 0) throw new MaestroException($"Position (tickFromTime or time) cannot be negative, found {mptkEvent.Tick}");
                if (mptkEvent.Track < 0) throw new MaestroException($"The number of the track ({mptkEvent.Track}) cannot be negative.");
                if (mptkEvent.Track == 0 && (
                        mptkEvent.Command == MPTKCommand.NoteOn ||
                        mptkEvent.Command == MPTKCommand.NoteOff ||
                        mptkEvent.Command == MPTKCommand.KeyAfterTouch ||
                        mptkEvent.Command == MPTKCommand.ControlChange ||
                        mptkEvent.Command == MPTKCommand.PatchChange ||
                        mptkEvent.Command == MPTKCommand.ChannelAfterTouch ||
                        mptkEvent.Command == MPTKCommand.PitchWheelChange)
                    )
                {
                    throw new MaestroException($"MIDI events based on channel (noteon, noteoff, patch change ...) cannot be defined on track 0.");
                }
                MPTK_MidiEvents.Add(mptkEvent);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return mptkEvent;
        }


        /// <summary>@brief
        /// Add a note on event at an absolute time (tickFromTime count). The corresponding Noteoff is automatically created if length > 0\n
        /// If an infinite note-on is added (length < 0), don't forget to add a note-off, it will never created automatically.
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="note">Note must be in the range 0-127</param>
        /// <param name="velocity">Velocity must be in the range 0-127.</param>
        /// <param name="length">Duration in tickFromTime. No automatic noteoff is added if duration is < 0, need to be added with MPTK_AddOff</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddNote(int track, long tick, int channel, int note, int velocity, int length)
        {
            MPTKEvent mptkEvent = null;
            try
            {
                if (velocity < 0 || velocity > 127)
                {
                    throw new MaestroException($"Velocity must be >= 0 and <= 127, found {velocity}.");
                }

                if (length < 0)
                    // duration not specifed, set a default of a quarter (not taken into account by the synth). A next note off event will whange this duration.
                    mptkEvent = AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.NoteOn, Value = note, Velocity = velocity, Duration = -1, Length = -1 });
                else
                {
                    long duration_ms = DurationTickToMilli(length);
                    mptkEvent = AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.NoteOn, Value = note, Velocity = velocity, Duration = duration_ms, Length = length });
                    // It's better to create note-off when saving the MIDI file
                    // MPTK don't use note-off but the duration of the event
                    // But they are mandatory for the MIDI file norm .
                    // AddRawEvent(new MptkEvent() { Track = track, Tick = tickFromTime + length, Channel = channel, Command = MPTKCommand.NoteOff, Value = note, Velocity = 0 });
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return mptkEvent;
        }


        /// <summary>@brief
        /// Add a silence.\n
        /// @note
        /// A silent note does not exist in the MIDI norm, we simulate it with a noteon and a very low velocity = 1.\n
        /// it's not possible to create a noteon with a velocity = 0, it's considered as a noteof by MIDI
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event.</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="length">Duration in tickFromTime.</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddSilence(int track, long tick, int channel, int length)
        {
            MPTKEvent mptkEvent = null;
            try
            {
                mptkEvent = new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.NoteOn, Value = 0, Velocity = 1, Length = length };
                AddRawEvent(mptkEvent);
                // It's better to create note-off when saving the MIDI file
                // MPTK don't use note-off but the duration of the event
                // But they are mandatory for the MIDI file norm .
                // AddRawEvent(new MptkEvent() { Track = track, Tick = tickFromTime + duration, Channel = channel, Command = MPTKCommand.NoteOff, Value = 0 });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return mptkEvent;

        }

        /// <summary>@brief
        /// Add a note off event.\n
        /// Must always succeed the corresponding NoteOn, obviously on the same channel!
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="note">Note must be in the range 0-127</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddOff(int track, long tick, int channel, int note)
        {
            MPTKEvent mptkEvent = null;
            try
            {
                bool found = false;
                for (int index = MPTK_MidiEvents.Count - 1; index >= 0; index--)
                {
                    mptkEvent = MPTK_MidiEvents[index];
                    if (mptkEvent.Channel == channel && mptkEvent.Command == MPTKCommand.NoteOn && mptkEvent.Value == note)
                    {
                        int length = Convert.ToInt32(tick - mptkEvent.Tick);
                        if (length > 0)
                        {
                            found = true;
                            mptkEvent.Length = length;
                            mptkEvent.Duration = DurationTickToMilli(length);
                            break;
                        }
                    }
                }
                if (!found)
                    Debug.LogWarning($"No NoteOn found corresponding to this NoteOff: track={track} channel={channel} tickFromTime={tick} note={note}");
                //AddRawEvent(new MptkEvent() { Track = track, Tick = tickFromTime, Channel = channel, Command = MPTKCommand.NoteOff, Value = note });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return mptkEvent;

        }

        private void CalculateLastTick(MPTKEvent midiEvent)
        {
            if (TickLast == 0)
                TickLast = midiEvent.Tick + midiEvent.Length;
        }

        /// <summary>@brief
        /// Add a chord from a range
        /// @snippet TestMidiGenerator.cs ExampleMidiWriterBuildChordFromRange
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick"></param>
        /// <param name="channel"></param>
        /// <param name="scale">See MPTKScaleLib</param>
        /// <param name="chord">See MPTKChordBuilder</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public void AddChordFromScale(int track, long tick, int channel, MPTKScaleLib scale, MPTKChordBuilder chord)
        {
            try
            {
                chord.MPTK_BuildFromRange(scale);
                foreach (MPTKEvent evnt in chord.Events)
                    AddNote(track, tick, channel, evnt.Value, evnt.Velocity, (int)ConvertMilliToTick(evnt.Duration));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a chord from a library of chord
        /// @snippet TestMidiGenerator.cs ExampleMidiWriterBuildChordFromLib
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick"></param>
        /// <param name="channel"></param>
        /// <param name="chordName">Name of the chord See #MPTKChordName</param>
        /// <param name="chord">See MPTKChordBuilder</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public void AddChordFromLib(int track, long tick, int channel, MPTKChordName chordName, MPTKChordBuilder chord)
        {
            try
            {
                chord.MPTK_BuildFromLib(chordName);
                foreach (MPTKEvent evnt in chord.Events)
                    AddNote(track, tick, channel, evnt.Value, evnt.Velocity, (int)ConvertMilliToTick(evnt.Duration));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>@brief
        /// Add a change preset
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="preset">Preset (program/patch) must be in the range 0-127</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddChangePreset(int track, long tick, int channel, int preset)
        {
            MPTKEvent mptkEvent = null;
            try
            {
                mptkEvent = new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.PatchChange, Value = preset };
                AddRawEvent(mptkEvent);
                //AddEvent(track, new PatchChangeEvent(absoluteTime, channel + 1, preset));
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return mptkEvent;
        }


        /// <summary>@brief
        /// Add a Channel After-Touch Event
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="afterTouchPressure">After-touch pressure from 0 to 127</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddChannelAfterTouch(int track, long tick, int channel, int afterTouchPressure)
        {
            MPTKEvent mptkEvent = null;
            try
            {
                mptkEvent = new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.ChannelAfterTouch, Value = afterTouchPressure };
                return AddRawEvent(mptkEvent);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return null;
        }


        /// <summary>@brief
        /// Creates a general control change event (CC)
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="controller">The MIDI Controller. See #MPTKController</param>
        /// <param name="controllerValue">Controller value</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddControlChange(int track, long tick, int channel, MPTKController controller, int controllerValue)
        {
            try
            {
                MPTKEvent mptkEvent = new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.ControlChange, Controller = controller, Value = controllerValue };
                return AddRawEvent(mptkEvent);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return null;
        }


        /// <summary>@brief
        /// Creates a control change event (CC) for the pitch (Pitch Wheel)\n
        /// pitchWheel=
        /// @li  0      minimum (0 also for midi standard event value) 
        /// @li  0.5    centered value (8192 for midi standard event value) 
        /// @li  1      maximum (16383 for midi standard event value)
        /// </summary>
        /// <param name="track">Track for this event (do not add to track 0)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="pitchWheel">Normalized Pitch Wheel Value. Range 0 to 1. V2.88.2 range normalized from 0 to 1.</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddPitchWheelChange(int track, long tick, int channel, float pitchWheel)
        {
            try
            {
                int pitch = (int)Mathf.Lerp(0f, 16383f, pitchWheel); // V2.88.2 range normalized from 0 to 1
                                                                     //Debug.Log($"{pitchWheel} --> {pitch}");
                return AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Channel = channel, Command = MPTKCommand.PitchWheelChange, Value = pitch });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return null;
        }

        /// <summary>@brief
        /// Add a tempo change to the midi stream. There is no channel in parameter because tempo change is applied to all tracks and channels.\n
        /// Next note-on with milliseconds defined after the tempo change will take into account the new value of the BPM.
        /// @note 
        /// TempoMap is not updated, call MPTK_CalculateTempoMap to recreate the full tempo map.
        /// </summary>
        /// <param name="track">Track for this event (it's a good practive to use track 0 for this event)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="bpm">quarter per minute</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddBPMChange(int track, long tick, int bpm)
        {
            if (bpm <= 0)
            {
                Debug.LogWarning("AddBPMChange: BPM must > 0");
                return null;
            }
            try
            {
                _bpm = bpm; // MPTK_MicrosecondsPerQuaterNote is calculated from the bpm
                            //Value contains new Microseconds Per Beat Note and Duration contains new tempo (quarter per minute).
                return AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Command = MPTKCommand.MetaEvent, Meta = MPTKMeta.SetTempo, Value = MicrosecondsPerQuaterNote/*, Duration = _bpm*/ });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return null;
        }

        /// <summary>@brief
        /// Add a tempo change to the midi stream in microseconds per quarter note. \n
        /// There is no channel in parameter because tempo change is applied to all tracks and channels.\n
        /// Next note-on with milliseconds defined after the tempo change will take into account the new value of the BPM.
        /// @note 
        /// MPTK_TempoMap is not updated. See example:
        /// @snippet TestMidiGenerator.cs ExampleCalculateMaps
        /// </summary>
        /// <param name="track">Track for this event (it's a good practive to use track 0 for this event)</param>
        /// <param name="tick">Tick time for this event</param>
        /// <param name="microsecondsPerQuarterNote">Microseconds per quarter note</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddTempoChange(int track, long tick, int microsecondsPerQuarterNote)
        {
            if (microsecondsPerQuarterNote <= 0)
            {
                Debug.LogWarning("AddBPMChange: Microseconds Per Quarter Note must > 0");
                return null;
            }
            try
            {
                _bpm = MPTKEvent.QuarterPerMicroSecond2BeatPerMinute(microsecondsPerQuarterNote);
                //Value contains new Microseconds Per Beat Note and Duration contains new tempo (quarter per minute).
                return AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Command = MPTKCommand.MetaEvent, Meta = MPTKMeta.SetTempo, Value = microsecondsPerQuarterNote,/* Duration = _bpm*/ });
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return null;
        }


        /// <summary>@brief
        /// Create a new TimeSignatureEvent. This event is optionnal. 
        /// Internal Midi sequencer assumes the default value is 4,2,24,32.  No track nor channel as teampo change applied to the whole midi.
        /// More info here https://paxstellar.fr/2020/09/11/midi-timing/
        /// @note 
        /// MPTK_SignMap is not updated. See example:
        /// @snippet TestMidiGenerator.cs ExampleCalculateMaps
        /// </summary>
        /// <param name="track">Track for this event (it's a good practive to use track 0 for this event)</param>
        /// <param name="tick">Time at which to create this event</param>
        /// <param name="numerator">Numerator, beats per measure. Will be MPTKSignature.NumberBeatsMeasure in #MPTK_SignMap</param>
        /// <param name="denominator">Denominator, beat unit: 1 means 2, 2 means 4 (crochet), 3 means 8 (quaver), 4 means 16, ...</param>
        /// <param name="ticksInMetronomeClick">Ticks in Metronome Click. Set to 24 for a standard value.</param>
        /// <param name="no32ndNotesInQuarterNote">No of 32nd Notes in Beat Click. Set to 32 for a standard value.</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddTimeSignature(int track, long tick, int numerator = 4, int denominator = 2, int ticksInMetronomeClick = 24, int no32ndNotesInQuarterNote = 32)
        {
            try
            {
                //MPTK_NumberBeatsMeasure = numerator;
                // if Meta = TimeSignature,
                //      Value contains the numerator (number of beats in a bar) and the denominator (Beat unit: 1 means 2, 2 means 4 (crochet), 3 means 8 (quaver), 4 means 16, ...)
                return AddRawEvent(new MPTKEvent()
                {
                    Track = track,
                    Tick = tick,
                    Command = MPTKCommand.MetaEvent,
                    Meta = MPTKMeta.TimeSignature,
                    Value = MPTKEvent.BuildIntFromBytes((byte)numerator, (byte)denominator, (byte)ticksInMetronomeClick, (byte)no32ndNotesInQuarterNote),
                    Length = ticksInMetronomeClick,
                });

            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return null;
        }


        /// <summary>@brief
        /// Create a MIDI Text Event.
        /// @snippet TestMidiGenerator.cs ExampleCreateMeta
        /// </summary>
        /// <param name="track">Track for this event (it's a good practice to use track 0 for this event)</param>
        /// <param name="tick">Absolute time of this event</param>
        /// <param name="typeMeta">MetaEvent type</param>
        /// <param name="text">The text associated to this MIDI event</param>
        /// <returns>Return the MIDI event created or null if error</returns>
        public MPTKEvent AddText(int track, long tick, MPTKMeta typeMeta, string text)
        {
            try
            {
                switch (typeMeta)
                {
                    case MPTKMeta.TextEvent:
                    case MPTKMeta.Copyright:
                    case MPTKMeta.DeviceName:
                    case MPTKMeta.Lyric:
                    case MPTKMeta.ProgramName:
                    case MPTKMeta.SequenceTrackName:
                    case MPTKMeta.Marker:
                    case MPTKMeta.TrackInstrumentName:
                        return AddRawEvent(new MPTKEvent() { Track = track, Tick = tick, Command = MPTKCommand.MetaEvent, Meta = typeMeta, Info = text });
                    default:
                        throw new Exception($"AddText need a meta event type for text. {typeMeta} is not correct.");
                }
            }

            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return null;
        }

        /// <summary>@brief
        /// Delete all MIDI events on this channel
        /// </summary>
        /// <param name="channel"></param>
        public void DeleteChannel(int channel)
        {
            if (MPTK_MidiEvents != null)
                for (int index = 0; index < MPTK_MidiEvents.Count;)
                {
                    if (MPTK_MidiEvents[index].Channel == channel)
                        MPTK_MidiEvents.RemoveAt(index);
                    else
                        index++;
                }
        }

        /// <summary>@brief
        /// Delete all MIDI events on this track
        /// </summary>
        /// <param name="track"></param>
        public void DeleteTrack(int track)
        {
            if (MPTK_MidiEvents != null)
                for (int index = 0; index < MPTK_MidiEvents.Count;)
                {
                    if (MPTK_MidiEvents[index].Track == track)
                        MPTK_MidiEvents.RemoveAt(index);
                    else
                        index++;
                }
        }

        /// <summary>@brief
        /// Sort in place events in MPTK_MidiEvents by ascending tickFromTime position.
        /// First priority is applied for 'preset change' and 'meta' event for a group of events with the same position (but 'end track' are set at end of the group. 
        /// @note
        /// @li No reallocation of the list is done, the events in the list are sorted in place.
        /// @li good performance for low disorder 
        /// @li not efficient for high disorder list. Typically when reading a MIDI file, list is sorted by tracks.
        /// @li in case of high disorder the use of MPTK_SortEvents is recommended at the price of a realocation of the list.
        /// </summary>
        /// <param name="logPerf"></param>
        public void StableSortEvents(bool logPerf = false)
        {
            if (MPTK_MidiEvents != null)
            {
                System.Diagnostics.Stopwatch watch = null;
                if (logPerf)
                {
                    watch = new System.Diagnostics.Stopwatch(); // High resolution time
                    watch.Start();
                }

                //// Quick sort - NO will realloc the list. 
                //MPTK_MidiEvents = MPTK_MidiEvents.OrderBy(o => o.Tick).ToList();
                //if (logPerf)
                //{
                //    Debug.Log($"Quick Sort time {watch.ElapsedMilliseconds} {watch.ElapsedTicks}");
                //    watch.Restart();
                //}

                // Then sort with priority on meta and preset change event (too long for a not pre-sorted list)
                MidiLoad.Sort(MPTK_MidiEvents, 0, MPTK_MidiEvents.Count - 1, new MidiLoad.MidiEventComparer());
                if (logPerf)
                {
                    Debug.Log($"Stable sort time {watch.ElapsedMilliseconds} {watch.ElapsedTicks}");
                    watch.Stop();
                }
            }
            else
                Debug.LogWarning("MPTKWriter - MPTK_SortEvents - MPTK_MidiEvents is null");
        }

        /// <summary>@brief
        /// Calculate real time, measure and quarter position
        /// @li Calculate #MPTK_TempoMap with #MPTKTempo.MPTK_CalculateMap
        /// @li Calculate #MPTK_SignMap with  #MPTKSignature.MPTK_CalculateMap
        /// @li Calculate time and duration of each events from the tickFromTime value and from the tempo map.
        /// @li Calculate measure and quarter position taking into account time signature.
        /// @version 2.10.0
        /// @snippet TestMidiGenerator.cs ExampleCalculateMaps
        /// </summary>
        /// <param name="logPerf"></param>
        /// <param name="logDebug"></param>
        public void CalculateTiming(bool logPerf = false, bool logDebug = false)
        {
            if (MPTK_MidiEvents != null)
            {
                System.Diagnostics.Stopwatch watch = null;
                if (logPerf)
                {
                    watch = new System.Diagnostics.Stopwatch(); // High resolution time
                    watch.Start();
                }

                // Default are defined in MPTK_CalculateMap
                //if (MPTK_TempoMap.Count == 0)
                //{
                //    MPTKTempo.DeltaTicksPerQuarterNote = MPTK_DeltaTicksPerQuarterNote;
                //    // Create in the map a default tempo, set to 120 by default (500 000 microseconds)
                //    MPTK_TempoMap.Add(new MPTKTempo(index: MPTK_TempoMap.Count, fromTick: 0, microsecondsPerQuarterNote: MptkEvent.BeatPerMinute2QuarterPerMicroSecond(120),
                //        pulse: (double)MptkEvent.BeatPerMinute2QuarterPerMicroSecond(120) / (double)MPTK_DeltaTicksPerQuarterNote / 1000d));
                //}
                //if (MPTK_SignMap.Count == 0)
                //{
                //    // Create in the map a default signature
                //    MPTK_SignMap.Add(new MPTKSignature(index: 0));
                //}

                // New with 2.10.0
                MPTKTempo.CalculateMap(DeltaTicksPerQuarterNote, MPTK_MidiEvents, MPTK_TempoMap);
                MPTKSignature.CalculateMap(DeltaTicksPerQuarterNote, MPTK_MidiEvents, MPTK_SignMap);
                MPTKSignature.CalculateMeasureBoundaries(MPTK_SignMap);

                int indexEvent = 0;
                int indexTempo = 0;
                int indexSign = 0;
                if (logDebug) Debug.Log($"CalculateTiming index:{indexTempo} {MPTK_TempoMap[indexTempo]} {MPTK_SignMap[indexSign]} (init)");
                foreach (MPTKEvent mptkEvent in MPTK_MidiEvents)
                {
                    mptkEvent.Index = indexEvent++;

                    indexTempo = MPTKTempo.FindSegment(MPTK_TempoMap, mptkEvent.Tick, fromIndex: indexTempo);
                    MPTKTempo tempoMap = MPTK_TempoMap[indexTempo];
                    mptkEvent.RealTime = (float)tempoMap.CalculateTime(mptkEvent.Tick);
                    if (mptkEvent.Command == MPTKCommand.NoteOn && mptkEvent.Duration > -1)
                        mptkEvent.Duration = (long)(mptkEvent.Length * tempoMap.Pulse);

                    indexSign = MPTKSignature.FindSegment(MPTK_SignMap, mptkEvent.Tick, fromIndex: indexSign);
                    MPTKSignature signMap = MPTK_SignMap[indexSign];
                    mptkEvent.Measure = signMap.TickToMeasure(mptkEvent.Tick);
                    mptkEvent.Beat = signMap.CalculateBeat(mptkEvent.Tick, mptkEvent.Measure);
                    if (logDebug) Debug.Log($"CalculateTiming index:{indexTempo} {MPTK_TempoMap[indexTempo]} {MPTK_SignMap[indexSign]}");
                }
                if (logPerf)
                {
                    Debug.Log($"CalculateTiming {watch.ElapsedMilliseconds} ms {watch.ElapsedTicks} timer ticks");
                    watch.Stop();
                }
            }
            else
                Debug.LogWarning("MPTKWriter - CalculateTiming - MPTK_MidiEvents is null");
        }

        /// <summary>@brief
        /// Write Midi file to an OS folder
        /// @snippet TestMidiGenerator.cs ExampleMIDIWriteAndPlay
        /// </summary>
        /// <param name="filename">filename of the midi file</param>
        /// <returns>true if ok</returns>
        public bool WriteToFile(string filename)
        {
            bool ok = false;
            try
            {
                if (MPTK_MidiEvents != null && MPTK_MidiEvents.Count > 0)
                {
                    MidiFile midiToSave = BuildNAudioMidi();
                    // NAudio don't create noteoff associated to noteon! they need to be added if they are missing
                    MidiFile.Export(filename, midiToSave.Events);
                    ok = true;
                }
                else
                    Debug.LogWarning("MPTKWriter - Write - MidiEvents is null or empty");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>@brief
        /// Write Midi file to MidiDB.\n
        /// To be used only in edit mode not in a standalone application.\n
        /// A call to AssetDatabase.Refresh() must be required after the file has been added to the resource.
        /// </summary>
        /// <param name="filename">filename of the midi file without any folder and any extension</param>
        /// <returns>true if ok</returns>
        public bool WriteToMidiDB(string filename)
        {
            bool ok = false;
            try
            {
                if (Application.isEditor)
                {
                    string filenameonly = Path.GetFileNameWithoutExtension(filename) + ".bytes";
                    // Build path to midi folder 
                    string pathMidiFile = Path.Combine(Application.dataPath, MidiPlayerGlobal.PathToMidiFile);
                    string filepath = Path.Combine(pathMidiFile, filenameonly);
                    //Debug.Log(filepath);
                    WriteToFile(filepath);
                    // To be review, can't access class in the editor project ...
                    //MidiPlayerTK.ToolsEditor.CheckMidiSet();
                    string filenoext = Path.GetFileNameWithoutExtension(filename);
                    if (!MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Contains(filenoext))
                    {
                        Debug.Log($"Add MIDI '{filenoext}' to MidiDB");
                        MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Add(filenoext);
                        MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Sort();
                        MidiPlayerGlobal.CurrentMidiSet.Save();
                    }

                    ok = true;
                }
                else
                    Debug.LogWarning("WriteToMidiDB can be call only in editor mode not in a standalone application");
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>@brief
        ///  Build a NAudio midi object from the midi events. WriteToMidiDB and WriteToFile call these methods just before writing the MIDI file.
        /// </summary>
        /// <returns>NAudio MidiFile</returns>
        public MidiFile BuildNAudioMidi()
        {
            MidiFile naudioMidi = new MidiFile(MidiFileType, DeltaTicksPerQuarterNote);

            if (MPTK_TrackStat == null)
                CreateTracksStat();

            foreach (int track in MPTK_TrackStat.Keys)
            {
                if (MPTK_TrackStat[track].CountAll == 0)
                    Debug.LogWarning($"BuildNAudioMidi - Track {track} is empty");
                else
                {
                    bool endTrack = false;
                    naudioMidi.Events.AddTrack();
                    long endLastEvent = 0;
                    long prevAbsEvent = 0;
                    //Debug.Log($"Build track {track}");
                    foreach (MPTKEvent mptkEvent in MPTK_MidiEvents)
                    {
                        if (mptkEvent.Track == track)
                        {
                            MidiEvent naudioEvent = null;
                            MidiEvent naudioNoteOff = null;
                            //MidiEvent naudioNoteOff = null;
                            try
                            {
                                switch (mptkEvent.Command)
                                {
                                    case MPTKCommand.NoteOn:
                                        if (mptkEvent.Length < 0)
                                            Debug.LogWarning($"BuildNAudioMidi - NoteOn with negative duration not processed. NoteOff Missing? {mptkEvent}");
                                        else
                                            naudioEvent = new NoteOnEvent(mptkEvent.Tick, mptkEvent.Channel + 1, mptkEvent.Value, mptkEvent.Velocity, (int)mptkEvent.Length);
                                        // noteoff are already created if event has been added with AddNote but not if loaded with MidiLoad and KeepNoteOff is false.
                                        // NAudio don't create noteoff associated to noteon! they need to be added if they are missing
                                        // Can be added now, the events will be sorted by NAudio (MergeSort)
                                        naudioNoteOff = new NoteEvent(mptkEvent.Tick + mptkEvent.Length, mptkEvent.Channel + 1, MidiCommandCode.NoteOff, mptkEvent.Value, 0);

                                        break;
                                    //case MPTKCommand.NoteOff:
                                    //    if (!addNoteOffAuto)
                                    //        // Noteoff are added only if automatic note off creation is off.
                                    //        naudioEvent = new NoteEvent(mptkEvent.Tick, mptkEvent.Channel + 1, MidiCommandCode.NoteOff, mptkEvent.Value, 0);
                                    //    break;
                                    case MPTKCommand.PatchChange:
                                        naudioEvent = new PatchChangeEvent(mptkEvent.Tick, mptkEvent.Channel + 1, mptkEvent.Value);
                                        break;
                                    case MPTKCommand.ControlChange:
                                        naudioEvent = new ControlChangeEvent(mptkEvent.Tick, mptkEvent.Channel + 1, (MidiController)mptkEvent.Controller, mptkEvent.Value);
                                        break;
                                    case MPTKCommand.ChannelAfterTouch:
                                        naudioEvent = new ChannelAfterTouchEvent(mptkEvent.Tick, mptkEvent.Channel + 1, mptkEvent.Value);
                                        break;
                                    case MPTKCommand.KeyAfterTouch:
                                        // Not processed by NAudio
                                        // naudioEvent = new KeyAfterTouchEvent(mptkEvent.Tick, mptkEvent.Channel + 1, mptkEvent.Value);
                                        break;
                                    case MPTKCommand.MetaEvent:
                                        switch (mptkEvent.Meta)
                                        {
                                            case MPTKMeta.SetTempo:
                                                // mptkEvent.Value = microsecondsPerQuarterNote 
                                                naudioEvent = new TempoEvent(mptkEvent.Value, mptkEvent.Tick);
                                                break;
                                            case MPTKMeta.TimeSignature:
                                                // - The fourth byte is the numerator of the time signature and has values between 0x00 and 0xFF (0 and 255).
                                                // - The fifth byte is the power to which the number 2 must be raised to obtain the time signature denominator.
                                                //   Thus, if the fifth byte is 0, the denominator is 20 = 1, denoting whole notes.If the fifth byte is 1, the denominator is 21 = 2 denoting half notes, and so on.
                                                // - The sixth byte of the message defines a metronome pulse in terms of the number of MIDI clock ticks per click.
                                                //   Assuming 24 MIDI clocks per quarter note, if the value of the sixth byte is 48, the metronome will click every two quarter notes, or in other words, every half-note.
                                                // - The seventh byte defines the number of 32nd notes per beat (no32ndNotesInQuarterNote).
                                                //   This byte is usually 8 as there is usually one quarter note per beat and one quarter note contains eight 32nd notes.
                                                //   It does not affect at what time events are sent (so a pure playback program will ignore this message), but how notes are displayed.
                                                //   For example, if the header says there are 100 ticks per beat, and the time signature has the default of 8 32th notes per beat,
                                                //   then a note-on / note - off pair with a distance of 100 ticks is displayed as a quarter note.
                                                //   If you change the time signature to 32 32th notes per beat, then a length of 100 ticks corresponds to a whole note.
                                                // https://www.recordingblogs.com/wiki/midi-time-signature-meta-message#:~:text=Assuming%2024%20MIDI%20clocks%20per%20quarter%20note%2C%20if,and%20one%20quarter%20note%20contains%20eight%2032nd%20notes.
                                                naudioEvent = new TimeSignatureEvent(mptkEvent.Tick,
                                                    MPTKEvent.ExtractFromInt((uint)mptkEvent.Value, 0),
                                                    MPTKEvent.ExtractFromInt((uint)mptkEvent.Value, 1),
                                                    MPTKEvent.ExtractFromInt((uint)mptkEvent.Value, 2),
                                                    MPTKEvent.ExtractFromInt((uint)mptkEvent.Value, 3));
                                                break;
                                            case MPTKMeta.KeySignature:
                                                naudioEvent = new KeySignatureEvent(
                                                    MPTKEvent.ExtractFromInt((uint)mptkEvent.Value, 0),
                                                    MPTKEvent.ExtractFromInt((uint)mptkEvent.Value, 1), mptkEvent.Tick);
                                                break;
                                            case MPTKMeta.EndTrack:
                                                // v2.9.0 - don't add endtrack, they are automatically processed by Maestro
                                                Debug.LogWarning($"Do not add endtrack, they are automatically processed by Maestro, track:{track}");
                                                // naudioMidi.Events.AddEvent(new MetaEvent(MetaEventType.EndTrack, 0, mptkEvent.Tick), track);
                                                // End track, no more event will be added after this event for this track
                                                endTrack = true;
                                                break;
                                            case MPTKMeta.Marker:
                                            case MPTKMeta.MidiChannel:
                                            case MPTKMeta.MidiPort:
                                            case MPTKMeta.SmpteOffset:
                                            case MPTKMeta.CuePoint:
                                                // Not processed by Maestro
                                                break;

                                            default:
                                                if (mptkEvent.Info != null)
                                                    naudioEvent = new TextEvent(mptkEvent.Info, (MetaEventType)mptkEvent.Meta, mptkEvent.Tick);
                                                else
                                                    Debug.LogWarning($"This Meta MIDI event is not processed by Maestro: {mptkEvent.Meta}");
                                                break;
                                        }
                                        break;
                                    case MPTKCommand.PitchWheelChange:
                                        naudioEvent = new PitchWheelChangeEvent(mptkEvent.Tick, mptkEvent.Channel + 1, mptkEvent.Value);
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"Can't build event {mptkEvent} {ex}");
                            }
                            try
                            {
                                if (naudioEvent != null)
                                {
                                    naudioEvent.DeltaTime = (int)(naudioEvent.AbsoluteTime - prevAbsEvent);
                                    prevAbsEvent = naudioEvent.AbsoluteTime;
                                    naudioMidi.Events.AddEvent(naudioEvent, track);
                                    //Debug.Log($"   Add event {naudioEvent}");

                                    if (endLastEvent < naudioEvent.AbsoluteTime)
                                    {
                                        endLastEvent = naudioEvent.AbsoluteTime;
                                        // v2.9.0 - there is always a noteoff with noteon
                                        //if (naudioEvent.CommandCode == MidiCommandCode.NoteOn)
                                        //    // A noteoff event will be created, so time of last event will be more later
                                        //    endLastEvent += naudioEvent.DeltaTime;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"Can't add event {mptkEvent} {ex}");
                            }
                            try
                            {
                                if (naudioNoteOff != null)
                                {
                                    naudioNoteOff.DeltaTime = (int)(naudioNoteOff.AbsoluteTime - prevAbsEvent);
                                    prevAbsEvent = naudioNoteOff.AbsoluteTime;
                                    naudioMidi.Events.AddEvent(naudioNoteOff, track);
                                    if (endLastEvent < naudioNoteOff.AbsoluteTime)
                                    {
                                        endLastEvent = naudioNoteOff.AbsoluteTime;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"Can't add noteoff event {mptkEvent} {ex}");
                            }
                        }

                        if (endTrack)
                            // exit loop on each events for this track
                            break;
                    } // foreach event

                    if (!endTrack)
                    {
                        try
                        {
                            //Debug.Log($"Close track {track} at {endLastEvent}");
                            naudioMidi.Events.AddEvent(new MetaEvent(MetaEventType.EndTrack, 0, endLastEvent), track);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Can't add end track event {ex}");
                        }
                    }
                }
            } // foreach track

            //naudioMidi.Events.MidiFileType = MPTK_MidiFileType;
            //naudioMidi.Events.PrepareForExport();

            return naudioMidi;
        }

        /// <summary>@brief
        /// Log information about the MIDI
        /// </summary>
        /// <returns></returns>
        public bool LogWriter()
        {
            bool ok = false;
            //
            // REWRITED with 2.9.0
            // 
            try
            {
                if (MPTK_MidiEvents != null && MPTK_MidiEvents.Count > 0)
                {
                    Debug.Log($"<b>---------------- MPTKWriter: LogWriter ----------------</b>");
                    Debug.Log($"<b>MPTK_DeltaTicksPerQuarterNote: {DeltaTicksPerQuarterNote}</b>");
                    Debug.Log($"<b>MPTK_TrackCount: {TrackCount}</b>");
                    LogTrackStat();

                    LogTempoMap();
                    LogSignMap();

                    Debug.Log($"<b>MIDI events: {MPTK_MidiEvents.Count}</b>");
                    if (MPTK_MidiEvents.Count < 10000)
                        foreach (MPTKEvent tmidi in MPTK_MidiEvents)
                            Debug.Log("   " + tmidi.ToString());
                    else
                    {
                        Debug.Log("<b>*** Log only the 10.000 last MIDI events ***</b>");
                        for (int i = MPTK_MidiEvents.Count - 10000; i < MPTK_MidiEvents.Count; i++)
                            Debug.Log("   " + MPTK_MidiEvents[i].ToString());
                    }
                    Debug.Log("--------------------------------------------------------------");
                }
                else
                    Debug.LogWarning("MPTKWriter - LogWriter - MidiEvents is null or empty");


            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        /// <summary>@brief
        /// Log information about the tempo map
        /// </summary>
        public void LogTempoMap()
        {
            if (MPTK_TempoMap != null)
            {
                Debug.Log($"<b>MPTK_TempoMap: {MPTK_TempoMap.Count}</b>");
                MPTK_TempoMap.ForEach(t =>
                {
                    Debug.Log("   " + t);
                });
            }
        }

        /// <summary>@brief
        /// Log information about the signature map
        /// </summary>
        public void LogSignMap()
        {
            if (MPTK_SignMap != null)
            {
                Debug.Log($"<b>MPTK_SignMap: {MPTK_SignMap.Count}</b>");
                MPTK_SignMap.ForEach(t =>
                {
                    Debug.Log("   " + t);
                });
            }
        }

        /// <summary>@brief
        /// Log information about the stat map
        /// </summary>
        public void LogTrackStat()
        {
            if (MPTK_TrackStat != null)
                foreach (int track in MPTK_TrackStat.Keys)
                {
                    if (MPTK_TrackStat[track].CountAll != 0)
                    {
                        Debug.Log($"   Track: {track,-2}\tCount event: {MPTK_TrackStat[track].CountAll,-3}\tPreset Change: {MPTK_TrackStat[track].CountPreset,-2}\tNote: {MPTK_TrackStat[track].CountNote}");
                    }
                }
            else
                Debug.Log($"   No track stat available, call CreateTracksStat() before.");
        }


        /// <summary>@brief
        /// Log information about the MIDI
        /// </summary>
        /// <returns></returns>
        public bool LogRaw()
        {
            bool ok = false;
            //
            // REWRITED with 2.9.0
            // 
            try
            {
                if (MPTK_MidiEvents != null && MPTK_MidiEvents.Count > 0)
                {
                    MidiFile midifile = BuildNAudioMidi();

                    Debug.Log($"---------------- MPTKWriter: LogRaw ----------------");
                    Debug.Log($"MidiFileType: {midifile.Events.MidiFileType}");
                    Debug.Log($"Tracks Count: {midifile.Tracks}");

                    if (midifile.Events.MidiFileType == 0 && midifile.Tracks > 1)
                    {
                        throw new ArgumentException("Can't export more than one track to a type 0 file");
                    }

                    for (int track = 0; track < midifile.Events.Tracks; track++)
                    {
                        IList<MidiEvent> eventList = midifile.Events[track];

                        long absoluteTime = midifile.Events.StartAbsoluteTime;

                        // use a stable sort to preserve ordering of MIDI events whose 
                        // absolute times are the same
                        //MergeSort.Sort(eventList, new MidiEventComparer());
                        if (eventList.Count > 0)
                        {
                            // TBN Change - error if no end track
                            Debug.Assert(MidiEvent.IsEndTrack(eventList[eventList.Count - 1]), "Exporting a track with a missing end track");
                        }
                        foreach (var midiEvent in eventList)
                        {
                            string info = $"   Track:{track} {midiEvent}";
                            if (midiEvent.CommandCode == MidiCommandCode.NoteOn)
                            {
                                NoteOnEvent ev = (NoteOnEvent)midiEvent;
                                if (ev.OffEvent != null)
                                    info += $" NoteOff at:  {ev.AbsoluteTime}";
                            }
                            Debug.Log(info);
                        }

                    }

                    //foreach (IList<MidiEvent> track in midifile.Events)
                    //{
                    //    foreach (MidiEvent nAudioMidievent in track)
                    //    {
                    //        string sEvent = nAudioMidievent.ToString();  //MidiScan.ConvertnAudioEventToString(nAudioMidievent, indexTrack);
                    //        if (sEvent != null)
                    //            Debug.Log("   " + sEvent);
                    //    }
                    //    indexTrack++;
                    //}

                    Debug.Log($"--------------------------------------------------------------");
                }
                else
                    Debug.LogWarning("MPTKWriter - LogRaw - MidiEvents is null or empty");


            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }

        private static bool Test(string source, string target)
        {
            bool ok = false;
            try
            {
                MidiFile midifile = new MidiFile(source);
                MidiFile.Export(target, midifile.Events);
                ok = true;
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return ok;
        }
    }
}
