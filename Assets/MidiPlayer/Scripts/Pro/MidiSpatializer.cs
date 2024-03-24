
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    /// For playing Spatialized MIDI by channels or by tracks from the MidiDB. This class must be used with the prefab MidiSpatializer.
    /// 
    /// @attention MidiSpatializer inherits of classes MidiFilePlayer and MidiSynth. For clarity, only MidiSpatializer attibutes are provided here.
    /// Look at the classes MidiFilePlayer and MidiSynth to discover all attributes available.
    /// 
    /// @version 
    ///     Maestro Pro 
    /// 
    /// There is no specific API for this prefab. Scripting is necessary to defined position of channel or instrument in your 3D env. See below.\n\n
    /// This class inherits from MidiFilePlayer and MidiSynth, so all properties, event, methods from MidiFilePlayer and MidiSynth are available in this class.\n\n
    /// See "Midi File Setup" in the Unity menu MPTK for adding MIDI in MidiDB.\n
    /// ///! @snippet TestSpatializerFly.cs ExampleArrangeByChannel
    /// See full example in TestSpatializerFly.cs
    /// More information here https://paxstellar.fr/midi-external-player-v2/
    /// </summary>
    //  [HelpURL("https://paxstellar.fr/midi-external-player-v2/")]
    public class MidiSpatializer : MidiFilePlayer
    {
        protected new void Awake()
        {
            //Debug.Log("Awake MidiSpatializer:" + MPTK_IsPlaying + " " + MPTK_PlayOnStart + " " + MPTK_IsPaused);
            // Set this midisynth as the master : read midi events and send to the slave midisynth
            MPTK_Spatialize = true;
            if (!MPTK_CorePlayer)
            {
                Debug.LogWarning($"MidiSpatializer works only in Core player mode. Change properties in inspector");
                return;
            }

            if (MPTK_MaxDistance <= 0f)
                Debug.LogWarning($"Max Distance is set to 0, any sound will be played.");

            base.AwakeMidiFilePlayer();
        }

        public new void Start()
        {
            if (!MPTK_CorePlayer)
                return;

            base.StartMidiFilePlayer();

            // Will be called for each intance of MidiSpatializer. 
            OnEventStartPlayMidi.AddListener((string midiName) =>
            {
                //Debug.Log($"Start playing {midiName} Channel/Track:{MPTK_SpatialSynthIndex} MPTK_TrackCount:{MPTK_TrackCount}");
                instrumentPlayed = "";
                MPTK_InstrumentNum = 0;
                trackName = "";
                if (MPTK_SpatialSynthIndex < 0)
                    MPTK_RefreshedUsedSynth();
            });
        }

        /// <summary>@brief
        /// Enable or disable Synths depending on the mode Channel or Track. If mode = Channel, 16 Synth are enabled, if mode = Track, TrackCount Synths are enabled.  
        /// </summary>
        public void MPTK_RefreshedUsedSynth()
        {
            foreach (MidiFilePlayer mfp in MidiFilePlayer.SpatialSynths)
            {
                // Channel mode: max 16 synth
                if (MPTK_ModeSpatializer == ModeSpatializer.Channel)
                    if (mfp.MPTK_SpatialSynthIndex >= 16)
                        mfp.MPTK_SpatialSynthEnabled = false;
                    else
                        // No more than 16 channels are possible with MIDI
                        mfp.MPTK_SpatialSynthEnabled = true;

                // Track mode: max track count
                else if (mfp.MPTK_SpatialSynthIndex >= MPTK_TrackCount)
                    mfp.MPTK_SpatialSynthEnabled = false;
                else
                    mfp.MPTK_SpatialSynthEnabled = true;
            }
        }

        // This method is deprecated and replaced by MPTK_RefreshedUsedSynth
        public void MPTK_DisableUnsedSynth(int countOfUsefulSynth)
        {
        }
    }
}

