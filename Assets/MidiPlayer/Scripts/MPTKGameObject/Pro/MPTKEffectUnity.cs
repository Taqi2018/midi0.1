using System;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    /// Unlike SoundFont effects, they applied to the whole player. On the other hand, the Unity effects parameters are rich and, obviously based on Uniy algo!\n
    /// https://docs.unity3d.com/Manual/class-AudioEffectMixer.html\n
    /// Only most important effect are integrated in Maestro: Reverb and Chorus. On need, others effects could be added. 
    /// @note
    ///     - Unity effects integration modules are exclusively available with the Maestro MPTK Pro version. 
    ///     - By default, these effects are disabled in Maestro. 
    ///     - To enable them, you’ll need to adjust the settings from the prefab inspector: Synth Parameters / Unity Effect.
    ///     - Each settings are available by script.
    /// @code
    /// @endcode
    /// </summary>
    public class MPTKEffectUnity : ScriptableObject
    {

        // ------------------------
        // Apply effect from Unity
        // ------------------------

        private MidiSynth synth;

        [HideInInspector, NonSerialized] // defined at startup by script
        public AudioReverbFilter ReverbFilter;

        [HideInInspector, NonSerialized] // defined at startup by script
        public AudioChorusFilter ChorusFilter;

        public void Init(MidiSynth psynth)
        {
            synth = psynth;
            if (Application.isPlaying)
            {
                // Enable or disable Unity effects related to your setting
                ReverbFilter = synth.CoreAudioSource.GetComponent<AudioReverbFilter>();
                if (ReverbFilter!=null) 
                    ReverbFilter.enabled = EnableReverb;

                ChorusFilter = synth.CoreAudioSource.GetComponent<AudioChorusFilter>();
                if (ChorusFilter != null)
                    ChorusFilter.enabled = EnableChorus;
            }
        }

        /// <summary>
        /// Set all Unity effects to default value as defined with Unity.
        /// </summary>
        public void DefaultAll()
        {
            DefaultReverb();
            DefaultChorus();
        }


        // -------
        // Reverb
        // -------

        /// <summary>@brief
        /// Set Reverb Unity default value as defined with Unity.
        /// @version Maestro Pro 
        /// </summary>
        public void DefaultReverb()
        {
            ReverbDryLevel = Mathf.InverseLerp(-10000f, 0f, 0f);
            ReverbRoom = Mathf.InverseLerp(-10000f, 0f, -1000f);
            ReverbRoomHF = Mathf.InverseLerp(-10000f, 0f, -100f);
            ReverbRoomLF = Mathf.InverseLerp(-10000f, 0f, 0f);
            ReverbDecayTime = 1.49f;
            ReverbDecayHFRatio = 0.83f;
            ReverbReflectionLevel = Mathf.InverseLerp(-10000f, 1000f, -2602f);
            ReverbReflectionDelay = Mathf.InverseLerp(-10000f, 1000f, -10000f);
            ReverbLevel = Mathf.InverseLerp(-10000f, 2000f, 200f);
            ReverbDelay = 0.011f;
            ReverbHFReference = 5000f;
            ReverbLFReference = 250f;
            ReverbDiffusion = Mathf.InverseLerp(0f, 100f, 100f);
            ReverbDensity = Mathf.InverseLerp(0f, 100f, 100f);
        }

        /// <summary>@brief
        /// Apply Reverb Unity effect to the AudioSource. The effect is applied to all voices.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public bool EnableReverb
        {
            get { return applyReverb; }
            set { if (ReverbFilter != null) ReverbFilter.enabled = value; applyReverb = value; }
        }
        [HideInInspector, SerializeField]
        private bool applyReverb;

        [HideInInspector, SerializeField]
        private float reverbRoom, reverbRoomHF, reverbRoomLF, reverbReflectionLevel, reverbReflectionDelay, reverbDryLevel;

        [HideInInspector, SerializeField]
        private float reverbDecayTime, reverbDecayHFRatio, reverbLevel, reverbDelay, reverbHfReference, reverbLfReference, reverbDiffusion, reverbDensity;

        /// <summary>@brief
        /// Mix level of dry signal in output.\n
        /// Ranges from 0 to 1. 
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbDryLevel
        {
            get { return reverbDryLevel; }
            set { reverbDryLevel = value; if (ReverbFilter != null) ReverbFilter.dryLevel = Mathf.Lerp(-10000f, 0f, reverbDryLevel); }
        }

        /// <summary>@brief
        /// Room effect level at low frequencies.\n
        /// Ranges from 0 to 1.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbRoom
        {
            get { return reverbRoom; }
            set { reverbRoom = value; if (ReverbFilter != null) ReverbFilter.room = Mathf.Lerp(-10000f, 0f, reverbRoom); }
        }

        /// <summary>@brief
        /// Room effect high-frequency level.\n
        /// Ranges from 0 to 1.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbRoomHF
        {
            get { return reverbRoomHF; }
            set { reverbRoomHF = value; if (ReverbFilter != null) ReverbFilter.roomHF = Mathf.Lerp(-10000f, 0f, reverbRoomHF); }
        }

        /// <summary>@brief
        /// Room effect low-frequency level.\n
        /// Ranges from 0 to 1.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbRoomLF
        {
            get { return reverbRoomLF; }
            set { reverbRoomLF = value; if (ReverbFilter != null) ReverbFilter.roomLF = Mathf.Lerp(-10000f, 0f, reverbRoomLF); }
        }

        /// <summary>@brief
        /// Reverberation decay time at low-frequencies in seconds.\n
        /// Ranges from 0.1 to 20. Default is 1.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbDecayTime
        {
            get { return reverbDecayTime; }
            set { reverbDecayTime = value; if (ReverbFilter != null) ReverbFilter.decayTime = reverbDecayTime; }
        }


        /// <summary>@brief
        /// Decay HF Ratio : High-frequency to low-frequency decay time ratio.\n
        /// Ranges from 0.1 to 2.0.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbDecayHFRatio
        {
            get { return reverbDecayHFRatio; }
            set { reverbDecayHFRatio = value; if (ReverbFilter != null) ReverbFilter.decayHFRatio = reverbDecayHFRatio; }
        }

        /// <summary>@brief
        /// Early reflections level relative to room effect.\n
        /// Ranges from 0 to 1.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbReflectionLevel
        {
            get { return reverbReflectionLevel; }
            set { reverbReflectionLevel = value; if (ReverbFilter != null) ReverbFilter.reflectionsLevel = Mathf.Lerp(-10000f, 1000f, reverbReflectionLevel); }
        }

        /// <summary>@brief
        /// Late reverberation level relative to room effect.\n
        /// Ranges from -10000.0 to 2000.0. Default is 0.0.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbReflectionDelay
        {
            get { return reverbReflectionDelay; }
            set { reverbReflectionDelay = value; if (ReverbFilter != null) ReverbFilter.reflectionsDelay = Mathf.Lerp(-10000f, 1000f, reverbReflectionDelay); }
        }

        /// <summary>@brief
        /// Late reverberation level relative to room effect.\n
        /// Ranges from 0 to 1. 
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbLevel
        {
            get { return reverbLevel; }
            set { reverbLevel = value; if (ReverbFilter != null) ReverbFilter.reverbLevel = Mathf.Lerp(-10000f, 2000f, reverbLevel); }
        }

        /// <summary>@brief
        /// Late reverberation delay time relative to first reflection in seconds.\n
        /// Ranges from 0 to 0.1. Default is 0.04
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbDelay
        {
            get { return reverbDelay; }
            set { reverbDelay = value; if (ReverbFilter != null) ReverbFilter.reverbDelay = reverbDelay; }
        }

        /// <summary>@brief
        /// Reference high frequency in Hz.\n
        /// Ranges from 1000 to 20000. Default is 5000
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbHFReference
        {
            get { return reverbHfReference; }
            set { reverbHfReference = value; if (ReverbFilter != null) ReverbFilter.hfReference = reverbHfReference; }
        }

        /// <summary>@brief
        /// Reference low-frequency in Hz.\n
        /// Ranges from 20 to 1000. Default is 250
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbLFReference
        {
            get { return reverbLfReference; }
            set { reverbLfReference = value; if (ReverbFilter != null) ReverbFilter.lfReference = reverbLfReference; }
        }

        /// <summary>@brief
        /// Reverberation diffusion (echo density) in percent.\n
        /// Ranges from 0 to 1. Default is 1.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbDiffusion
        {
            get { return reverbDiffusion; }
            set { reverbDiffusion = value; if (ReverbFilter != null) ReverbFilter.diffusion = Mathf.Lerp(0f, 100f, reverbDiffusion); }
        }

        /// <summary>@brief
        /// Reverberation density (modal density) in percent.\n
        /// Ranges from 0 to 1.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ReverbDensity
        {
            get { return reverbDensity; }
            set { reverbDensity = value; if (ReverbFilter != null) ReverbFilter.density = Mathf.Lerp(0f, 100f, reverbDensity); }
        }

        // -------
        // Chorus
        // -------

        /// <summary>@brief
        /// Set Chorus Unity default value as defined with Unity.
        /// @version Maestro Pro 
        /// </summary>
        public void DefaultChorus()
        {
            ChorusDryMix = 0.5f;
            ChorusWetMix1 = 0.5f;
            ChorusWetMix2 = 0.5f;
            ChorusWetMix3 = 0.5f;
            ChorusDelay = 40f;
            ChorusRate = 0.8f;
            ChorusDepth = 0.03f;
        }

        [HideInInspector, SerializeField]
        private bool applyChorus;

        [HideInInspector, SerializeField]
        private float chorusDryMix, chorusWetMix1, chorusWetMix2, chorusWetMix3, chorusDelay, chorusRate, chorusDepth;
        /// <summary>@brief
        /// Apply Chorus Unity effect to the AudioSource. The effect is applied to all voices.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public bool EnableChorus
        {
            get { return applyChorus; }
            set { if (ChorusFilter != null) ChorusFilter.enabled = value; applyChorus = value; }
        }

        /// <summary>@brief
        /// Volume of original signal to pass to output.\n
        /// Range from 0 to 1. Default = 0.5.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ChorusDryMix
        {
            get { return chorusDryMix; }
            set { chorusDryMix = value; if (ChorusFilter != null) ChorusFilter.dryMix = chorusDryMix; }
        }

        /// <summary>@brief
        /// Volume of 1st chorus tap.\n
        /// Range from  0 to 1. Default = 0.5.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ChorusWetMix1
        {
            get { return chorusWetMix1; }
            set { chorusWetMix1 = value; if (ChorusFilter != null) ChorusFilter.wetMix1 = chorusWetMix1; }
        }

        /// <summary>@brief
        /// Volume of 2nd chorus tap. This tap is 90 degrees out of phase of the first tap.\n
        /// Range from  0 to 1. Default = 0.5.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ChorusWetMix2
        {
            get { return chorusWetMix2; }
            set { chorusWetMix2 = value; if (ChorusFilter != null) ChorusFilter.wetMix2 = chorusWetMix2; }
        }

        /// <summary>@brief
        /// Volume of 3rd chorus tap. This tap is 90 degrees out of phase of the second tap.\n
        /// Range from 0 to 1. Default = 0.5.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ChorusWetMix3
        {
            get { return chorusWetMix3; }
            set { chorusWetMix3 = value; if (ChorusFilter != null) ChorusFilter.wetMix3 = chorusWetMix3; }
        }

        /// <summary>@brief
        /// Chorus delay in ms.\n
        /// Range from 0.1 to 100. Default = 40 ms.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ChorusDelay
        {
            get { return chorusDelay; }
            set { chorusDelay = value; if (ChorusFilter != null) ChorusFilter.delay = chorusDelay; }
        }

        /// <summary>@brief
        /// Chorus modulation rate in hz.\n
        /// Range from 0 to 20. Default = 0.8 hz.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ChorusRate
        {
            get { return chorusRate; }
            set { chorusRate = value; if (ChorusFilter != null) ChorusFilter.rate = chorusRate; }
        }

        /// <summary>@brief
        /// Chorus modulation depth.\n
        /// Range from 0 to 1. Default = 0.03.
        /// @version Maestro Pro 
        /// </summary>
        [HideInInspector]
        public float ChorusDepth
        {
            get { return chorusDepth; }
            set { chorusDepth = value; if (ChorusFilter != null) ChorusFilter.depth = chorusDepth; }
        }

    }
}
