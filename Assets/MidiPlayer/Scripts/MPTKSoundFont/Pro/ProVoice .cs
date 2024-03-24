//#define DEBUGPERF
//#define DEBUGTIME

namespace MidiPlayerTK
{
    //! @cond NODOC
    public partial class fluid_voice
    {
        public float q_dB; // from GEN_FILTERQ, Q factor in centibels
        public float fres; // from GEN_FILTERFC

        /* reverb */
        public float reverb_send;
        float amp_reverb;

        /* chorus */
        public float chorus_send;
        float amp_chorus;

        public fluid_iir_filter resonant_filter;
        //fluid_iir_filter resonant_custom_filter; /* optional custom/general-purpose IIR resonant filter */

        private void InitFilter()
        {
            resonant_filter = new fluid_iir_filter(synth.FLUID_BUFSIZE);
            // High pass filter useless: resonant_filter.fluid_iir_filter_init(fluid_iir_filter_type.FLUID_IIR_HIGHPASS, fluid_iir_filter_flags.FLUID_IIR_NOFLAGS);
            resonant_filter.fluid_iir_filter_init(fluid_iir_filter_type.FLUID_IIR_LOWPASS, fluid_iir_filter_flags.FLUID_IIR_NOFLAGS);
            //resonant_custom_filter.fluid_iir_filter_init(fluid_iir_filter_type.FLUID_IIR_DISABLED, fluid_iir_filter_flags.FLUID_IIR_NOFLAGS);
        }

        private void CalcAndApplyFilter(int count)
        {
            /*************** resonant filter ******************/
            if (synth.MPTK_EffectSoundFont.EnableFilter)
            {
                resonant_filter.fluid_iir_filter_calc(output_rate, modlfo_val * modlfo_to_fc + modenv_val * modenv_to_fc, synth.MPTK_EffectSoundFont.FilterFreqOffset);
                resonant_filter.fluid_iir_filter_apply(dsp_buf, count);
            }

            /* additional custom filter - only uses the fixed modulator, no lfos... */
            //        resonant_custom_filter. fluid_iir_filter_calc(output_rate, 0);
            //        resonant_custom_filter. fluid_iir_filter_apply(dsp_buf, count);
        }

        private void ApplyEffect(int count, float[] dsp_reverb_buf, float[] dsp_chorus_buf)
        {
            int dsp_i;
            /* reverb send. Buffer may be NULL. */
            float levelReverb = amp_reverb + synth.MPTK_EffectSoundFont.ReverbAmplify;
            if (levelReverb > 1f)
                levelReverb = 1f;

            if (dsp_reverb_buf != null && levelReverb > 0f)
            {
                for (dsp_i = 0; dsp_i < count; dsp_i++)
                    dsp_reverb_buf[dsp_i] += levelReverb * dsp_buf[dsp_i];
            }

            /* chorus send. Buffer may be NULL. */
            float levelChorus = amp_chorus + synth.MPTK_EffectSoundFont.ChorusAmplify;
            if (levelChorus > 1f)
                levelChorus = 1f;

            //Debug.Log("amp_chorus:" + amp_chorus + " MPTK_ChorusAmplify:" + synth.MPTK_ChorusAmplify + " --> " + levelChorus));

            if (dsp_chorus_buf != null && levelChorus > 0f)
            {
                for (dsp_i = 0; dsp_i < count; dsp_i++)
                    dsp_chorus_buf[dsp_i] += levelChorus * dsp_buf[dsp_i];
            }
        }
    }
    //! @endcond
}
