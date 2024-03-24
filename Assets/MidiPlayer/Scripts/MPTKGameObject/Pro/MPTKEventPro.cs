using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidiPlayerTK
{

    public partial class MPTKEvent : ICloneable
    {

        /// <summary>@brief
        /// List of generators modifier associated to the voices being played. Null if any modifier defined.
        /// @version Maestro Pro 
        /// </summary>
        public GenModifier[] GensModifier;

        /// <summary>@brief
        /// Apply modification on default SoundFont generator value. Can be applied independently for each notes,\n
        /// can be applied before the note is played and in <b>real time</b> when the note is playing.\n
        /// Each generator has a specific range of value, <a href=" https://paxstellar.fr/wp-content/uploads/2021/01/GeneratorModulation.png"><b>see generators list here.</b></a>\n
        /// Also the value used by this method is normalized (value between 0 and 1).
        /// <a href="https://paxstellar.fr/class-mptkevent#Generator-List"><b>See here  for more details.</b></a>\n
        /// @version Maestro Pro 
        /// @code
        /// // Create a midi event for a C5 note (60) with default value: infinite duration, channel = 0, and velocity = 127 (max)
        /// mptkEvent = new MPTKEvent() { Value = 60 };
        /// 
        /// // Fine tuning (pitch)
        /// mptkEvent.ModifySynthParameter(fluid_gen_type.GEN_FINETUNE, 0.52f, MPTKModeGeneratorChange.Override);
        /// 
        /// // Change low pass filter frequency
        /// mptkEvent.ModifySynthParameter(fluid_gen_type.GEN_FILTERFC, 0.6f, MPTKModeGeneratorChange.Override);
        /// 
        /// midiStream.MPTK_PlayDirectEvent(mptkEvent);
        /// @endcode
        /// </summary>
        /// <param name="genType">Type of generator to modify. Not all generators are authorized to real time modification.\n
        /// @li  GEN_MODLFOTOPITCH	Modulation LFO to pitch
        /// @li  GEN_VIBLFOTOPITCH	Vibrato LFO to pitch
        /// @li  GEN_MODENVTOPITCH	Modulation envelope to pitch
        /// @li  GEN_FILTERFC	Filter cutoff
        /// @li  GEN_FILTERQ	Filter Q
        /// @li  GEN_MODLFOTOFILTERFC	Modulation LFO to filter cutoff
        /// @li  GEN_MODENVTOFILTERFC	Modulation envelope to filter cutoff
        /// @li  GEN_MODLFOTOVOL	Modulation LFO to volume
        /// @li  GEN_CHORUSSEND	Chorus send amount
        /// @li  GEN_REVERBSEND	Reverb send amount
        /// @li  GEN_PAN	Stereo panning
        /// @li  GEN_MODLFODELAY	Modulation LFO delay
        /// @li  GEN_MODLFOFREQ	Modulation LFO frequency
        /// @li  GEN_VIBLFODELAY	Vibrato LFO delay
        /// @li  GEN_VIBLFOFREQ	Vibrato LFO frequency
        /// @li  GEN_MODENVDELAY	Modulation envelope delay
        /// @li  GEN_MODENVATTACK	Modulation envelope attack
        /// @li  GEN_MODENVHOLD	Modulation envelope hold
        /// @li  GEN_MODENVDECAY	Modulation envelope decay
        /// @li  GEN_MODENVSUSTAIN	Modulation envelope sustain
        /// @li  GEN_MODENVRELEASE	Modulation envelope release
        /// @li  GEN_VOLENVDELAY	Volume envelope delay
        /// @li  GEN_VOLENVATTACK	Volume envelope attack
        /// @li  GEN_VOLENVHOLD	Volume envelope hold
        /// @li  GEN_VOLENVDECAY	Volume envelope decay
        /// @li  GEN_VOLENVSUSTAIN	Volume envelope sustain
        /// @li  GEN_VOLENVRELEASE	Volume envelope release
        /// @li  GEN_ATTENUATION	Volume attenuation
        /// @li  GEN_COARSETUNE	Coarse tuning
        /// @li  GEN_FINETUNE	Fine tuning
        /// </param>
        /// <param name="value">Normalized value for the generator between 0 and 1.\n
        ///  <a href="https://paxstellar.fr/class-mptkevent#Generator-List"><b>See here real value for each parameters.</b></a>\n
        /// @li  0 set the minimum value for the generator. For example, with an envelope parameter, 0 will set -12000 (min for this type of parameters.
        /// @li  1 set the maximum value for the generator. For example, with an envelope parameter, 1 will set 12000 (max for this type of parameters.
        /// </param>
        /// <param name="mode">Define how to apply the value\n
        /// @li  Override: the SoundFont value is overridden.
        /// @li  Reinforce: the value is added to the default value.
        /// @li  Restaure: the default SoundFont value is used.
        /// </param>
        /// <returns>true if change has been done</returns>
        public bool ModifySynthParameter(fluid_gen_type genType, float value, MPTKModeGeneratorChange mode)
        {
            bool result = false;
            int genId = ConvertIdToIndex(genType);
            if (genId >= 0)
            {
                try
                {
                    // If a list of modifier is already associated to this event ?
                    if (GensModifier == null)
                    {
                        GenModifier.InitListGenerator();
                        GensModifier = new GenModifier[Enum.GetNames(typeof(fluid_gen_type)).Length];
                    }
                    if (GensModifier[genId] == null) GensModifier[genId] = new GenModifier();
                    GensModifier[genId].Mode = mode;
                    GensModifier[genId].NormalizedVal = value;
                    GensModifier[genId].SoundFontVal = Mathf.Lerp(fluid_gen_info.FluidGenInfo[genId].min, fluid_gen_info.FluidGenInfo[genId].max, value);

                    // If event is already playing (voices are defined) applied change in real time
                    if (Voices != null)
                        foreach (fluid_voice voice in Voices)
                            voice.fluid_voice_update_param(genId);
                    result = true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"ModifySynthParameter - {ex.Message}");
                }
            }
            return result;
        }

        /// <summary>@brief 
        /// If the MPTKEvent is a note-on then send a not-off event. Equivalent to send a note-off to the synth, the sound enters in the release phase.\n
        /// Has an effect only on Note On event.
        ///  v2.9.0 Pro. 
        /// </summary>
        public void StopEvent()
        {
            if (Command == MPTKCommand.NoteOn)
            {
                if (Voices != null && Voices.Count > 0)
                    if (Voices[0].synth != null)
                        Voices[0].synth.MPTK_PlayDirectEvent(new MPTKEvent() { Command = MPTKCommand.NoteOff, Channel = Channel, Value = Value });

            }
        }

        private static int ConvertIdToIndex(fluid_gen_type genType)
        {
            int genId = (int)genType;
            if (genId < 0 || genId >= Enum.GetNames(typeof(fluid_gen_type)).Length || !fluid_gen_info.FluidGenInfo[genId].RealTimeChange)
            {
                Debug.LogWarning($"ConvertIdToIndex - fluid_gen_type {genType} {genId} - cannot be modified or outside range");
                return -1;
            }
            return genId;
        }

        /// <summary>@brief
        /// Get the default soundfont value for the generator.\n
        /// Each generator has a specific range of value, <a href=" https://paxstellar.fr/wp-content/uploads/2021/01/GeneratorModulation.png"><b>see generators list here.</b></a>\n
        /// Also the value returned by this method is normalized (value between 0 and 1).
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="genType">see #ModifySynthParameter</param>
        /// <returns>Return the normalized value of the parameter</returns>
        public float GetSynthParameterDefaultValue(fluid_gen_type genType)
        {
            float result = 0f;
            int genId = ConvertIdToIndex(genType);
            if (genId >= 0)
            {
                try
                {
                    result = Mathf.InverseLerp(fluid_gen_info.FluidGenInfo[genId].min, fluid_gen_info.FluidGenInfo[genId].max, fluid_gen_info.FluidGenInfo[genId].def);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"GetSynthParameterDefaultValue - {ex.Message}");
                }
            }
            return result;
        }

        /// <summary>@brief
        /// Get the current value for the generator.\n
        /// Each generator has a specific range of value, <a href=" https://paxstellar.fr/wp-content/uploads/2021/01/GeneratorModulation.png"><b>see generators list here.</b></a>\n
        /// Also the value returned by this method is normalized (value between 0 and 1).
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="genType">see #ModifySynthParameter</param>
        /// <returns>Return the normalized value of the parameter</returns>
        public float GetSynthParameterCurrentValue(fluid_gen_type genType)
        {
            float result = 0f;
            int genId = ConvertIdToIndex(genType);
            if (genId >= 0)
            {
                try
                {
                    if (GensModifier[genId] == null)
                        // No generator modifier exists for this MIDI event
                        result = -1;
                    else
                        result = Mathf.InverseLerp(fluid_gen_info.FluidGenInfo[genId].min, fluid_gen_info.FluidGenInfo[genId].max, 
                            GensModifier[genId].SoundFontVal);
                }
                catch (Exception)
                {
                    result = -1;
                    //Debug.LogWarning($"GetSynthParameterCurrentValue - {ex.Message}");
                }
            }
            return result;
        }

        /// <summary>@brief
        /// Get the label for the generator. 
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="genType">see #ModifySynthParameter</param>
        /// <returns>Return the label of the generator</returns>
        public static string GetSynthParameterLabel(fluid_gen_type genType)
        {
            int genId = ConvertIdToIndex(genType);
            if (genId >= 0)
            {
                try
                {
                    GenModifier.InitListGenerator();
                    foreach (MPTKListItem item in GenModifier.RealTimeGenerator)
                        if (item.Index == genId)
                        {
                            return item.Label;// GenModifier.RealTimeGenerator
                        }

                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"GetSynthParameterLabel - {ex.Message}");
                }
            }
            return "not found";
        }

        /// <summary>@brief
        /// Get the list of modifiable generators.\n 
        /// It's a list of MPTKListItem where the properties are:\n
        /// @li  MPTKListItem#Index : generator Id
        /// @li  MPTKListItem#Label : texte associated to the generator
        /// @li  MPTKListItem#Position : position in the list from 0
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="genType">see #ModifySynthParameter</param>
        /// <returns>Return a list of modifiable generator (don't modify this list!)</returns>
        public static List<MPTKListItem> GetSynthParameterListGenerator()
        {
            GenModifier.InitListGenerator();
            return GenModifier.RealTimeGenerator;
        }

        /// <summary>@brief
        /// Reset synth parameter to default soundfont value.
        /// @version Maestro Pro 
        /// </summary>
        public void ClearSynthParameter()
        {
            GensModifier = null;
        }
    }
}
