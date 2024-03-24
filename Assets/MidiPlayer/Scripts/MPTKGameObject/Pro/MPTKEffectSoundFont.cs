using System;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    /// A SoundFont contains parameters to apply three kinds of effects: low-pass filter, reverb, chorus.\n
    /// These parameters can be specifics for each instruments and even each voices.\n
    /// Maestro MPTK effects are based on FluidSynth algo effects modules. 
    /// Furthermore, to get more liberty from SoundFont, Maestro can increase or decrease the impact of effects (from the inspector or by script).
    /// To summarize:
    ///     - Effects are applied individually to each voices, yet they are statically defined within the SoundFont.
    ///     - Maestro parameters can be adjusted to increase or decrease the default values set in the SoundFont.
    ///     - These adjustments will be applied across the entire prefab, but the effect will depend on the initial settings defined in the SoundFont preset.
    ///     - Please note that these effects require additional CPU resources.
    /// See more detailed information here https://paxstellar.fr/sound-effects/
    /// @note
    ///     - Effects modules are exclusively available with the Maestro MPTK Pro version. 
    ///     - By default, these effects are disabled in Maestro. 
    ///     - To enable them, you’ll need to adjust the settings from the prefab inspector (Synth Parameters / SoundFont Effect) or by script!
    ///     - For enhanced sound quality, it’s often beneficial to add a low-filter effect.
    /// </summary>
    public class MPTKEffectSoundFont : ScriptableObject
    {
        /// <summary>@brief
        /// Apply frequency low-pass filter as defined in the SoundFont.\n 
        /// This effect is processed with the fluidsynth algo independently on each voices but with a decrease of performace.
        /// @version Maestro Pro 
        /// </summary>
        public bool EnableFilter { get => applySFFilter; set => applySFFilter = value; }

        /// <summary>
        /// Apply reverberation effect as defined in the SoundFont.\n
        /// This effect is processed with the fluidsynth algo independently on each voices but with a decrease of performace. 
        /// @version Maestro Pro 
        /// </summary>
        public bool EnableReverb { get => applySFReverb; set => applySFReverb = value; }

        /// <summary>
        /// Apply chorus effect as defined in the SoundFont.\n
        /// This effect is processed with the fluidsynth algo independently on each voices but with a small decrease of performace. 
        /// @version Maestro Pro 
        /// </summary>
        public bool EnableChorus { get => applySFChorus; set => applySFChorus = value; }

        /// <summary>@brief
        /// Frequency cutoff is defined in the SoundFont for each notes.\n
        /// This parameter increase or decrease the default SoundFont value. Range: -2000 to 3000 Hz
        /// @version Maestro Pro 
        /// </summary>
        [Range(-2000f, 3000f)]
        [HideInInspector]
        public float FilterFreqOffset;

        /// <summary>@brief
        /// Quality Factor is defined in the SoundFont for each notes.\n
        /// This parameter increase or decrease the default SoundFont value. Range: -96 to 96.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float FilterQModOffset
        {
            get { return filterQModOffset; }
            set
            {
                if (filterQModOffset != value)
                {
                    filterQModOffset = Mathf.Clamp(value, -96f, 96f);
                    if (synth != null && synth.ActiveVoices != null)
                        foreach (fluid_voice voice in synth.ActiveVoices)
                            if (voice.resonant_filter != null)
                                voice.resonant_filter.fluid_iir_filter_set_q(voice.q_dB, filterQModOffset);
                }
            }
        }


        /// <summary>@brief
        /// Set all SoundFont effects to default value as defined in fluidsynth.\n
        /// @version Maestro Pro 
        /// </summary>
        public void DefaultAll()
        {
            DefaultFilter();
            DefaultReverb();
            DefaultChorus();
        }

        /// <summary>@brief
        /// Set Filter SoundFont default value as defined in fluidsynth.\n
        /// @version Maestro Pro 
        /// </summary>
        public void DefaultFilter()
        {
            FilterFreqOffset = 0f;
            FilterQModOffset = 0f;
        }

        [HideInInspector]
        /// <summary>@brief
        /// Reverberation level is defined in the SoundFont in the range [0, 1].\n
        /// This parameter is added to the the default SoundFont value.
        /// Range must be [-1, 1]
        /// @version Maestro Pro 
        /// </summary>
        [Range(-1f, 1f)]
        public float ReverbAmplify;

        [HideInInspector]
        /// <summary>@brief
        /// Chorus level is defined in the SoundFont in the range [0, 1].\n
        /// This parameter is added to the the default SoundFont value.\n
        /// Range must be [-1, 1]
        /// @version Maestro Pro 
        /// </summary>
        [Range(-1f, 1f)]
        public float ChorusAmplify;

        /// <summary>@brief
        /// Set the SoundFont reverb effect room size. Controls concave reverb time between 0 (0.7 s) and 1 (12.5 s)
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbRoomSize
        {
            get { return sfReverbRoomSize; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 1f);
                if (sfReverbRoomSize != newval)
                {
                    sfReverbRoomSize = newval;
                    SetParamSfReverb();
                }
            }
        }

        /// <summary>@brief
        /// Set the SoundFont reverb effect damp [0,1].\n
        /// Controls the reverb time frequency dependency. This controls the reverb time for the frequency sample rate/2\n
        /// When 0, the reverb time for high frequencies is the same as for DC frequency.\n
        /// When > 0, high frequencies have less reverb time than lower frequencies.\n
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbDamp
        {
            get { return sfReverbDamp; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 1f);
                if (sfReverbDamp != newval)
                {
                    sfReverbDamp = newval;
                    SetParamSfReverb();
                }
            }
        }

        /// <summary>@brief
        /// Set the SoundFont reverb effect width [0,100].\n
        ///  Controls the left/right output separation.\n
        ///  When 0, there are no separation and the signal on left and right output is the same.This sounds like a monophonic signal.\n
        ///  When 100, the separation between left and right is maximum.\n
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbWidth
        {
            get { return sfReverbWidth; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 100f);
                if (sfReverbWidth != newval)
                {
                    sfReverbWidth = newval;
                    SetParamSfReverb();
                }
            }
        }

        /// <summary>@brief
        /// Set the SoundFont reverb effect level.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbLevel
        {
            get { return sfReverbLevel; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 1f);
                if (sfReverbLevel != newval)
                {
                    sfReverbLevel = newval;
                    SetParamSfReverb();
                }
            }
        }

        /// <summary>@brief
        /// Set Reverb SoundFont default value as defined in fluidsynth.\n
        /// FLUID_REVERB_DEFAULT_ROOMSIZE 0.2f \n
        /// FLUID_REVERB_DEFAULT_DAMP 0.0f     \n
        /// FLUID_REVERB_DEFAULT_WIDTH 0.5f    \n
        /// FLUID_REVERB_DEFAULT_LEVEL 0.9f    \n
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public void DefaultReverb()
        {
            ReverbAmplify = 0f;
            ReverbRoomSize = 0.2f;
            ReverbDamp = 0f;
            ReverbWidth = 0.5f;
            ReverbLevel = 0.9f;
        }

        /// <summary>@brief
        /// Set the SoundFont chorus effect level [0, 10]\n
        /// Default value set to 0.9 (was 2f, thank John)
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ChorusLevel
        {
            get { return sfChorusLevel; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 10f);
                if (sfChorusLevel != newval)
                {
                    sfChorusLevel = newval;
                    SetParamSfChorus();
                }
            }
        }

        /// <summary>@brief
        /// Set the SoundFont chorus effect speed\n
        /// Chorus speed in Hz [0.1, 5]\n
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ChorusSpeed
        {
            get { return sfChorusSpeed; }
            set
            {
                float newval = Mathf.Clamp(value, 0.1f, 5f);
                if (sfChorusSpeed != newval)
                {
                    sfChorusSpeed = newval;
                    SetParamSfChorus();
                }
            }
        }

        /// <summary>@brief
        /// Set the SoundFont chorus effect depth\n
        /// Chorus depth [0, 256]\n
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ChorusDepth
        {
            get { return sfChorusDepth; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 256f);
                if (sfChorusDepth != newval)
                {
                    sfChorusDepth = newval;
                    SetParamSfChorus();
                }
            }
        }

        /// <summary>@brief
        /// Set the SoundFont chorus effect width\n
        /// The chorus unit process a monophonic input signal and produces stereo output controlled by WIDTH macro.\n
        /// Width allows to get a gradually stereo effect from minimum (monophonic) to maximum stereo effect. [0, 10]\n
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ChorusWidth
        {
            get { return sfChorusWidth; }
            set
            {
                float newval = Mathf.Clamp(value, 0f, 10f);
                if (sfChorusWidth != newval)
                {
                    sfChorusWidth = newval;
                    SetParamSfChorus();
                }
            }
        }

        /// <summary>@brief
        /// Set Chrous SoundFont default value as defined in fluidsynth.\n
        /// FLUID_CHORUS_DEFAULT_N 3        \n
        /// FLUID_CHORUS_DEFAULT_LEVEL 2.0 but set to 0.9 (thank John) \n
        /// FLUID_CHORUS_DEFAULT_SPEED 0.3 \n
        /// FLUID_CHORUS_DEFAULT_DEPTH 8.0 \n
        /// FLUID_CHORUS_DEFAULT_TYPE FLUID_CHORUS_MOD_SINE \n
        /// WIDTH 10
        /// @version Maestro Pro 
        /// </summary>
        public void DefaultChorus()
        {
            ChorusAmplify = 0f;
            ChorusLevel = 0.9f; // 2.0 in fluidsynthn set to 0.9 
            ChorusSpeed = 0.3f;
            ChorusDepth = 8f;
            ChorusWidth = 10f;
        }

        //! @cond NODOC

        private MidiSynth synth;

        public void Init(MidiSynth psynth)
        {
            synth = psynth;
            ///* Effects audio buffers */
            /* allocate the reverb module */
            fx_reverb = new float[psynth.FLUID_BUFSIZE];  // FLUID_MAX_BUFSIZE not supported, each frame must have the same length
            reverb = new fluid_revmodel(psynth.OutputRate, psynth.FLUID_BUFSIZE); // FLUID_MAX_BUFSIZE not supported, each frame must have the same length
            SetParamSfReverb();

            fx_chorus = new float[psynth.FLUID_BUFSIZE]; // FLUID_MAX_BUFSIZE not supported, each frame must have the same length
            /* allocate the chorus module */
            chorus = new fluid_chorus(psynth.OutputRate, psynth.FLUID_BUFSIZE); // FLUID_MAX_BUFSIZE not supported, each frame must have the same length
            SetParamSfChorus();
        }

        [HideInInspector, SerializeField]
        private float filterQModOffset;

        [HideInInspector, SerializeField]
        private bool applySFReverb, applySFChorus, applySFFilter;

        fluid_revmodel reverb;
        private float[] fx_reverb;
        fluid_chorus chorus;
        private float[] fx_chorus;


        [HideInInspector, SerializeField]
        private float sfReverbRoomSize = 0.2f;

        [HideInInspector, SerializeField]
        private float sfReverbDamp = 0f;

        [HideInInspector, SerializeField]
        private float sfReverbWidth = 0.5f;

        [HideInInspector, SerializeField]
        private float sfReverbLevel = 0.9f;

        /**< Default chorus voice count */
        const int FLUID_CHORUS_DEFAULT_N = 3;

        [HideInInspector, SerializeField]
        private float sfChorusLevel = 0.9f; // was 2.0 in fluidsynth  ... but too much

        [HideInInspector, SerializeField]
        private float sfChorusSpeed = 0.3f;

        [HideInInspector, SerializeField]
        private float sfChorusDepth = 8f;

        [HideInInspector, SerializeField]
        private float sfChorusWidth = 10f;

        const fluid_chorus.fluid_chorus_mod FLUID_CHORUS_DEFAULT_TYPE = fluid_chorus.fluid_chorus_mod.FLUID_CHORUS_MOD_SINE;  /**< Default chorus waveform type */

        private void SetParamSfReverb()
        {
            if (reverb != null)
                reverb.fluid_revmodel_set(/*(int)fluid_revmodel.fluid_revmodel_set_t.FLUID_REVMODEL_SET_ALL*/0xFF,
                    ReverbRoomSize, ReverbDamp, ReverbWidth, ReverbLevel);
        }

        private void SetParamSfChorus()
        {
            if (chorus != null)
                chorus.fluid_chorus_set((int)fluid_chorus.fluid_chorus_set_t.FLUID_CHORUS_SET_ALL,
                    FLUID_CHORUS_DEFAULT_N, ChorusLevel, ChorusSpeed, ChorusDepth, FLUID_CHORUS_DEFAULT_TYPE, ChorusWidth);
        }


        public void PrepareBufferEffect(out float[] reverb_buf, out float[] chorus_buf)
        {
            // Set up the reverb / chorus buffers only, when the effect is enabled on synth level.
            // Nonexisting buffers are detected in theDSP loop. 
            // Not sending the reverb / chorus signal saves some time in that case.
            if (EnableReverb)
            {
                Array.Clear(fx_reverb, 0, synth.FLUID_BUFSIZE);
                reverb_buf = fx_reverb;
            }
            else
                reverb_buf = null;

            if (EnableChorus)
            {
                Array.Clear(fx_chorus, 0, synth.FLUID_BUFSIZE);
                chorus_buf = fx_chorus;
            }
            else
                chorus_buf = null;
        }

        public void ProcessEffect(float[] reverb_buf, float[] chorus_buf, float[] left_buf, float[] right_buf)
        {
            /* send to reverb */
            if (EnableReverb && reverb_buf != null)
            {
                reverb.fluid_revmodel_processmix(reverb_buf, left_buf, right_buf);
            }

            /* send to chorus */
            if (EnableChorus && chorus_buf != null)
            {
                chorus.fluid_chorus_processmix(chorus_buf, left_buf, right_buf);
            }
        }

        //! @endcond

    }
}
