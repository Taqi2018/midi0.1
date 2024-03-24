using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MidiPlayerTK
{
    public partial class MidiLoad
    {
        /// <summary>@brief
        /// Current measure of the current event played [Pro].
        /// </summary>
        public int MPTK_CurrentMeasure;

        /// <summary>@brief
        /// Current beat of the current event played [Pro].
        /// </summary>
        public int MPTK_CurrentBeat;


        public bool calculateBeatPlayer()
        {
            bool beatChange = false;
            try
            {
                int measure = MPTK_CurrentSignMap.TickToMeasure(MPTK_TickPlayer);
                int beat = MPTK_CurrentSignMap.CalculateBeat(MPTK_TickPlayer, measure);
                if (MPTK_CurrentMeasure != measure || MPTK_CurrentBeat != beat)
                {
                    MPTK_CurrentMeasure = measure;
                    MPTK_CurrentBeat = beat;
                    beatChange = true;
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return beatChange;
        }

        public bool CheckInnerLoop(MPTKInnerLoop MPTK_InnerLoop)
        {
            try
            {
                //if (MPTK_InnerLoop != null &&  !MPTK_InnerLoop.Enabled) Debug.Log("disabled");
                // MPTK_Loop is allocated when MidiLoad is created
                if (MPTK_InnerLoop != null && TickSeek <= 0 && MPTK_InnerLoop.Enabled && !MPTK_InnerLoop.Finished)
                {
                    // Need goto start position? (start position must be lower than resume position)
                    if (MPTK_TickPlayer < MPTK_InnerLoop.Start && MPTK_InnerLoop.Start <= MPTK_InnerLoop.Resume)
                    {
                        TickSeek = MPTK_InnerLoop.Start;
                        InnerLoopLogAndCallback(MPTK_InnerLoop, MPTKInnerLoop.InnerLoopPhase.Start);
                        return true; // continue looping
                    }

                    // Need goto resume position? (resume position must be lower than end position)
                    if (MPTK_TickPlayer >= MPTK_InnerLoop.End && MPTK_InnerLoop.End > MPTK_InnerLoop.Resume)
                    {
                        MPTK_InnerLoop.Count++;
                        // Count limit is reach?
                        if (MPTK_InnerLoop.Max <= 0 || MPTK_InnerLoop.Count < MPTK_InnerLoop.Max)
                        {
                            TickSeek = MPTK_InnerLoop.Resume;
                            if (!InnerLoopLogAndCallback(MPTK_InnerLoop, MPTKInnerLoop.InnerLoopPhase.Resume))
                            {
                                MPTK_InnerLoop.Finished = true;
                                InnerLoopLogAndCallback(MPTK_InnerLoop, MPTKInnerLoop.InnerLoopPhase.Exit);
                            }
                        }
                        else
                        {
                            // Looping over, disable looping (return false)
                            MPTK_InnerLoop.Finished = true;
                            InnerLoopLogAndCallback(MPTK_InnerLoop, MPTKInnerLoop.InnerLoopPhase.Exit);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            MPTK_InnerLoop.Finished = true;
            return false; // disable looping
        }

        private bool InnerLoopLogAndCallback(MPTKInnerLoop innerLoop, MPTKInnerLoop.InnerLoopPhase phase)
        {
            if (innerLoop.Log)
                Debug.Log($"Inner Loop {phase} - MPTK_TickPlayer:{MPTK_TickPlayer} --> TickSeek:{TickSeek} Count:{innerLoop.Count}/{innerLoop.Max}");
            if (innerLoop.OnEventInnerLoop != null)
                try
                {
                    // Loop endded if call back return false
                    return innerLoop.OnEventInnerLoop.Invoke(phase, MPTK_TickPlayer, TickSeek, innerLoop.Count);
                }
                catch (Exception ex)
                {
                    Debug.LogError("An exception has beed detected in the callback method for OnEventInnerLoop.");
                    Debug.LogException(ex);
                }
            return true; // continue looping
        }

        /// <summary>@brief
        /// Search for a MIDI event from a tick position. v2.9.0\n
        /// </summary>
        /// <param name="tickSearched">tick position</param>
        /// <returns>MPTKEvent or null</returns>
        public static int MPTK_SearchEventFromTick(List<MPTKEvent> midiEvents, long tickSearched)
        {
            int index = -1;
            if (midiEvents == null)
            {
                Debug.LogWarning($"MPTK_SearchEventFromTick - MIDI events list is null");
            }
            else if (midiEvents.Count == 0)
            {
                index = 0;
            }
            else if (tickSearched <= 0)
                index = 0;
            else if (tickSearched >= midiEvents.Last().Tick)
                index = midiEvents.Count - 1;
            else
            {
                int lowIndex = 0;
                int highIndex = midiEvents.Count - 1;
                int middleIndex;
                long middleTicks;
                while (index < 0)
                {
                    middleIndex = (lowIndex + highIndex) / 2;
                    middleTicks = midiEvents[middleIndex].Tick;
                    if (tickSearched < middleTicks)
                        // before
                        highIndex = middleIndex;
                    else if (tickSearched > middleTicks)
                        // After
                        lowIndex = middleIndex;
                    else // tickSearched = middleTicks
                    {
                        index = middleIndex;
                        break;
                    }

                    if (lowIndex == highIndex)
                        // Found exact event with this tick or index adjacent
                        index = lowIndex;
                    else if (lowIndex + 1 == highIndex)
                        // index delta = 1, not divisible by 2
                        index = highIndex;
                }
                // Find event before with same tick 
                while (index > 0)
                {
                    if (midiEvents[index - 1].Tick != midiEvents[index].Tick)
                        break;
                    index--;
                }
            }
            //Debug.Log($"Insert at position {position} for tick {tick}");
            return index;
        }
        /// <summary>@brief
        /// Load MIDI file from a local file (Moved to PRO since version 2.89.5)
        /// </summary>
        /// <param name="filename">Midi path and filename to load (OS dependant)</param>
        /// <param name="strict">if true the MIDI must strictely respect the midi norm</param>
        /// <returns></returns>
        public bool MPTK_LoadFile(string filename, bool strict = false)
        {
            bool ok = true;
            try
            {
                using (Stream sfFile = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    byte[] data = new byte[sfFile.Length];
                    sfFile.Read(data, 0, (int)sfFile.Length);
                    ok = MPTK_Load(data, strict);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
                ok = false;
            }
            return ok;
        }
        /// <summary>@brief
        /// Load Midi from a MidiFileWriter2 object
        /// </summary>
        /// <param name="mfw2">MidiFileWriter2 object</param>
        /// <returns>true if loaded</returns>
        public bool MPTK_Load(MPTKWriter mfw2)
        {
            InitMidiLoadAttributes();
            bool ok = true;
            try
            {
                timeStartLoad = DateTime.Now;
                MPTK_MidiEvents = mfw2.MPTK_MidiEvents;
                MPTK_TempoMap = mfw2.MPTK_TempoMap;
                MPTK_SignMap = mfw2.MPTK_SignMap;
                MPTK_DeltaTicksPerQuarterNote = mfw2.DeltaTicksPerQuarterNote;
                if (MPTK_TempoMap != null && MPTK_TempoMap.Count > 0)
                {
                    MPTK_MicrosecondsPerQuarterNote = MPTK_TempoMap[0].MicrosecondsPerQuarterNote;
                }
                else
                {
                    MPTK_MicrosecondsPerQuarterNote = MPTKEvent.BeatPerMinute2QuarterPerMicroSecond(120);
                    Debug.LogWarning("No tempo map detected");
                }

                AnalyseTrackMidiEvent();
                if (MPTK_LogLoadEvents) MPTK_DisplayMidiAttributes();
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
                ok = false;
            }
            return ok;
        }
    }
}

