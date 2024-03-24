//#define DEBUGPERF
using MEC;
using UnityEngine;

namespace MidiPlayerTK
{
    public partial class MidiStreamPlayer : MidiSynth
    {
        public bool MPTK_LogChord;

        private MPTKScaleName currentScaleIndex;
        private MPTKScaleLib scaleLib;

        /// <summary>@brief
        /// [Play a Midi pitch change event for all notes on the channel.
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="pitchWheel">Normalized Pitch Wheel Value. Range 0 to 1. V2.88.2 range normalized from 0 to 1.
        /// @li  0      minimum (equivalent to value 0 for midi standard event) 
        /// @li  0.5    centered (equivalent to value 8192 for midi standard event) 
        /// @li  1      maximum (equivalent to value 16383 for midi standard event)
        /// </param> 
        public void MPTK_PlayPitchWheelChange(int channel, float pitchWheel)
        {
            int pitch = (int)Mathf.Lerp(0f, 16383f, pitchWheel);
            MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.PitchWheelChange, Value = pitch, Channel = channel });
        }

        /// <summary>@brief
        /// Play a midi pitch sensitivity change for all notes on the channel.
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="channel">Channel must be in the range 0-15</param>
        /// <param name="sensitivity">Pitch change sensitivity from 0 to 24 semitones up and down. Default value 2.\
        /// Example: 4, means semitones range is from -4 to 4 when MPTK_PlayPitchWheelChange change from 0 to 1.
        /// </param>
        public void MPTK_PlayPitchWheelSensitivity(int channel, int sensitivity)
        {
            sensitivity = Mathf.Clamp(sensitivity, 0, 24);
            // Select the registered parameter number to pitch bend range change
            MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.RPN_MSB, Value = 0, Channel = channel });
            MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.RPN_LSB, Value = (int)midi_rpn_event.RPN_PITCH_BEND_RANGE, Channel = channel });
            // Set the new value
            MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.DATA_ENTRY_MSB, Value = sensitivity, Channel = channel });
            MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.DATA_ENTRY_LSB, Value = 0, Channel = channel });
        }

        /// <summary>@brief
        /// Scale Name selected (musical scale).
        /// @version Maestro Pro 
        /// </summary>
        public string MPTK_ScaleName
        {
            get
            {
                return scaleLib != null ? scaleLib.Name : "Not set";
            }
        }

        /// <summary>@brief
        /// Current selected scale
        /// @version Maestro Pro 
        /// </summary>
        public MPTKScaleName MPTK_ScaleSelected
        {
            get { return currentScaleIndex; }
            set
            {
                if (currentScaleIndex != value || scaleLib == null)
                {
                    currentScaleIndex = value;
                    scaleLib = MPTKScaleLib.CreateScale(currentScaleIndex, MPTK_LogChord);
                }
            }
        }

        /// <summary>@brief
        /// Play a chord from the current selected scale (MPTK_ScaleSelected), Tonic and Degree are defined in parameter MPTKChord chord.\n
        /// Major scale is selected if no scale is defined.\n
        /// See file GammeDefinition.csv in folder Resources/GeneratorTemplate
        /// @version Maestro Pro 
        /// @code
        /// using MidiPlayerTK; // Add a reference to the MPTK namespace at the top of your script
        /// using UnityEngine;
        ///
        /// public class YourClass : MonoBehaviour
        /// {

        ///     // Need a reference to the prefab MidiStreamPlayer you have added in your scene hierarchy.
        ///     public MidiStreamPlayer midiStreamPlayer;
        /// 
        ///     // This object will be pass to the MPTK_PlayEvent for playing an event
        ///     MPTKEvent mptkEvent;

        ///     void Start()
        ///     {
        ///         // Find the MidiStreamPlayer. Could be also set directly from the inspector.
        ///         midiStreamPlayer = FindObjectOfType<MidiStreamPlayer>();
        ///     }
        ///     private void PlayOneChordFromLib()
        ///     {
        ///         // Start playing a new chord
        ///         MPTKChordBuilder ChordLibPlaying = new MPTKChordBuilder(true)
        ///         {
        ///             // Parameters to build the chord
        ///             Tonic = 60,
        ///             FromLib = 2,
        /// 
        ///             // Midi Parameters how to play the chord
        ///             Channel = 0,
        ///             // delay in milliseconds between each notes of the chord
        ///             Arpeggio = 100,
        ///             // millisecond, -1 to play indefinitely
        ///             Duration = 500,
        ///             // Sound can vary depending on the velocity
        ///             Velocity = 100,
        ///             Delay = 0,
        ///         };
        ///     midiStreamPlayer.MPTK_PlayChordFromLib(ChordLibPlaying);
        ///     }
        /// }
        /// 
        /// @endcode
        /// </summary>
        /// <param name="chord">required: Tonic and Degree on top of the classical Midi parameters</param>
        /// <returns></returns>
        public MPTKChordBuilder MPTK_PlayChordFromScale(MPTKChordBuilder chord)
        {
            try
            {
                if (MidiPlayerGlobal.MPTK_SoundFontLoaded)
                {
                    chord.Channel = Mathf.Clamp(chord.Channel, 0, MPTK_Channels.Length - 1);

                    // Set a default range
                    if (MPTK_ScaleSelected < 0)
                        // Load scale index 0 (instanciate scaleLib)
                        MPTK_ScaleSelected = 0;

                    chord.MPTK_BuildFromRange(scaleLib);

                    if (!MPTK_CorePlayer)
                        Routine.RunCoroutine(TheadPlay(chord.Events), Segment.RealtimeUpdate);
                    else
                    {
                        lock (this) // V2.83
                        {
                            foreach (MPTKEvent evnt in chord.Events)
                                QueueSynthCommand.Enqueue(new SynthCommand() { Command = SynthCommand.enCmd.StartEvent, MidiEvent = evnt });
                        }
                    }
                }
                else
                    Debug.LogWarningFormat("SoundFont not yet loaded, Chord cannot be processed Tonic:{0} Degree:{1}", chord.Tonic, chord.Degree);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return chord;
        }

        /// <summary>@brief
        /// Play a chord from the chord library. See file ChordLib.csv in folder Resources/GeneratorTemplate.\n
        /// The Tonic is used to buid the chord
        /// @version Maestro Pro 
        /// @code
        /// private void PlayOneChordFromLib()
        /// {
        ///    // Start playing a new chord
        ///    ChordLibPlaying = new MPTKChordBuilder(true)
        ///    {
        ///        // Parameters to build the chord
        ///        Tonic = CurrentNote,
        ///        FromLib = CurrentChord,
        ///
        ///        // Midi Parameters how to play the chord
        ///        Channel = StreamChannel,
        ///        // delay in milliseconds between each notes of the chord
        ///        Arpeggio = ArpeggioPlayChord, 
        ///        // millisecond, -1 to play indefinitely
        ///        Duration = Convert.ToInt64(NoteDuration * 1000f), 
        ///        // Sound can vary depending on the velocity
        ///        Velocity = Velocity, 
        ///        Delay = Convert.ToInt64(NoteDelay * 1000f),
        ///    };
        ///    midiStreamPlayer.MPTK_PlayChordFromLib(ChordLibPlaying);
        /// }
        /// @endcode
        /// </summary>
        /// <param name="chord">required: Tonic and FromLib on top of the classical Midi parameters</param>
        /// <returns></returns>
        public MPTKChordBuilder MPTK_PlayChordFromLib(MPTKChordBuilder chord)
        {
            try
            {
                if (MidiPlayerGlobal.MPTK_SoundFontLoaded)
                {
                    chord.Channel = Mathf.Clamp(chord.Channel, 0, MPTK_Channels.Length - 1);
                    chord.MPTK_BuildFromLib(chord.FromLib);

                    if (!MPTK_CorePlayer)
                        Routine.RunCoroutine(TheadPlay(chord.Events), Segment.RealtimeUpdate);
                    else
                    {
                        lock (this) // V2.83
                        {
                            foreach (MPTKEvent evnt in chord.Events)
                                QueueSynthCommand.Enqueue(new SynthCommand() { Command = SynthCommand.enCmd.StartEvent, MidiEvent = evnt });
                        }
                    }
                }
                else
                    Debug.LogWarningFormat("SoundFont not yet loaded, Chord cannot be processed Tonic:{0} Degree:{1}", chord.Tonic, chord.Degree);
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return chord;
        }

        /// <summary>@brief
        /// Stop playing the chord. All samples associated to the chord are stopped by sending a noteoff.
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="chord"></param>
        public void MPTK_StopChord(MPTKChordBuilder chord)
        {
            if (chord.Events != null)
            {
                foreach (MPTKEvent evt in chord.Events)
                {
                    if (!MPTK_CorePlayer)
                        StopEvent(evt);
                    else
                        lock (this) // V2.83
                        {
                            QueueSynthCommand.Enqueue(new SynthCommand() { Command = SynthCommand.enCmd.StopEvent, MidiEvent = evt });
                        }
                }
            }
        }
    }
}

