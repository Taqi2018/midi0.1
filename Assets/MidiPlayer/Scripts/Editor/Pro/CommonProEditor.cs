#if UNITY_EDITOR
#define MPTK_PRO
using UnityEngine;
using UnityEditor;

using System;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using static UnityEngine.GraphicsBuffer;

namespace MidiPlayerTK
{
    /// <summary>@brief
    /// Inspector for the midi global player component
    /// </summary>
    public class CommonProEditor : ScriptableObject
    {
        public static void EffectSoundFontParameters(MidiSynth instance, CustomStyle myStyle)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("These effects will be applied independently on each voices. Effects values are defined in the SoundFont, weird sound can occurs when changing these settings.", myStyle.LabelGreen);

            instance.MPTK_EffectSoundFont.EnableFilter = EditorGUILayout.Toggle(new GUIContent("Apply Low Pass Filter", "Low pass filter is defined in each preset of the SoudFont. Uncheck to gain some % CPU on weak device."), instance.MPTK_EffectSoundFont.EnableFilter);
            {
                EditorGUI.indentLevel++;
                GUI.enabled = instance.MPTK_EffectSoundFont.EnableFilter;
                instance.MPTK_EffectSoundFont.FilterFreqOffset = EditorGUILayout.Slider(new GUIContent("Offset Cutoff Frequence", "Offset to the cutoff frequency (Low Pass) defined in the SoundFont."), instance.MPTK_EffectSoundFont.FilterFreqOffset, -2000f, 3000f);
                instance.MPTK_EffectSoundFont.FilterQModOffset = EditorGUILayout.Slider(new GUIContent("Offset Quality ", "Offset on the SF resonance peak defined in the SoundFont."), instance.MPTK_EffectSoundFont.FilterQModOffset, -96f, 96f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("SoundFont Filter", GUILayout.Width(150), GUILayout.Height(15));
                if (GUILayout.Button(new GUIContent("Set Default", ""), GUILayout.Width(100), GUILayout.Height(15)))
                    instance.MPTK_EffectSoundFont.DefaultFilter();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }

            // USeless EditorUtility.SetDirty(instance.MPTK_EffectSoundFont);

            instance.MPTK_EffectSoundFont.EnableReverb = EditorGUILayout.Toggle(new GUIContent("Apply Reverb", ""), instance.MPTK_EffectSoundFont.EnableReverb);
            {
                EditorGUI.indentLevel++;
#if MPTK_PRO && UNITY_ANDROID && UNITY_OBOE
                EditorGUILayout.LabelField("Applying this effect with Oboe can cause weird sounds or even crash some devices", myStyle.LabelAlert);
#endif
                GUI.enabled = instance.MPTK_EffectSoundFont.EnableReverb;
                instance.MPTK_EffectSoundFont.ReverbAmplify = EditorGUILayout.Slider(new GUIContent("Amplify", ""), instance.MPTK_EffectSoundFont.ReverbAmplify, -1f, 1f);
                instance.MPTK_EffectSoundFont.ReverbLevel = EditorGUILayout.Slider(new GUIContent("Level", ""), instance.MPTK_EffectSoundFont.ReverbLevel, 0f, 1f);
                instance.MPTK_EffectSoundFont.ReverbRoomSize = EditorGUILayout.Slider(new GUIContent("Room Size", "Controls concave reverb time between 0 (0.7 second) and 1 (12.5 second)"), instance.MPTK_EffectSoundFont.ReverbRoomSize, 0f, 1f);
                instance.MPTK_EffectSoundFont.ReverbDamp = EditorGUILayout.Slider(new GUIContent("Damp", "Controls the reverb time frequency dependency."), instance.MPTK_EffectSoundFont.ReverbDamp, 0f, 1f);
                instance.MPTK_EffectSoundFont.ReverbWidth = EditorGUILayout.Slider(new GUIContent("Width", "Controls the left/right output separation."), instance.MPTK_EffectSoundFont.ReverbWidth, 0f, 100f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("SoundFont Reverb", GUILayout.Width(150), GUILayout.Height(15));
                if (GUILayout.Button(new GUIContent("Set Default", ""), GUILayout.Width(100), GUILayout.Height(15)))
                    instance.MPTK_EffectSoundFont.DefaultReverb();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }


            instance.MPTK_EffectSoundFont.EnableChorus = EditorGUILayout.Toggle(new GUIContent("Apply Chorus", ""), instance.MPTK_EffectSoundFont.EnableChorus);
            {
                EditorGUI.indentLevel++;
#if MPTK_PRO && UNITY_ANDROID && UNITY_OBOE
                EditorGUILayout.LabelField("Applying this effect with Oboe can cause weird sounds or even crash some devices", myStyle.LabelAlert);
#endif
                GUI.enabled = instance.MPTK_EffectSoundFont.EnableChorus;
                instance.MPTK_EffectSoundFont.ChorusAmplify = EditorGUILayout.Slider(new GUIContent("Amplify", ""), instance.MPTK_EffectSoundFont.ChorusAmplify, -1f, 1f);
                instance.MPTK_EffectSoundFont.ChorusLevel = EditorGUILayout.Slider(new GUIContent("Level", ""), instance.MPTK_EffectSoundFont.ChorusLevel, 0f, 10f);
                instance.MPTK_EffectSoundFont.ChorusSpeed = EditorGUILayout.Slider(new GUIContent("Speed", "Chorus speed in Hz"), instance.MPTK_EffectSoundFont.ChorusSpeed, 0.1f, 5f);
                instance.MPTK_EffectSoundFont.ChorusDepth = EditorGUILayout.Slider(new GUIContent("Depth", "Chorus Depth"), instance.MPTK_EffectSoundFont.ChorusDepth, 0f, 256f);
                instance.MPTK_EffectSoundFont.ChorusWidth = EditorGUILayout.Slider(new GUIContent("Width", "Allows to get a gradually stereo effect from minimum (monophonic) to maximum stereo effect"), instance.MPTK_EffectSoundFont.ChorusWidth, 0f, 10f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("SoundFont Chorus", GUILayout.Width(150), GUILayout.Height(15));
                if (GUILayout.Button(new GUIContent("Set Default", ""), GUILayout.Width(100), GUILayout.Height(15)))
                    instance.MPTK_EffectSoundFont.DefaultChorus();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        public static void EffectUnityParameters(MidiSynth instance, CustomStyle myStyle)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("These effects will be applied to all voices processed by the current MPTK gameObject. You can add multiple MPTK gameObjects to apply for different effects.", myStyle.LabelGreen);
#if MPTK_PRO && UNITY_ANDROID && UNITY_OBOE
            EditorGUILayout.LabelField("These effects are not available with Oboe", myStyle.LabelAlert);
            instance.MPTK_ApplyUnityReverb = false;
            instance.MPTK_ApplyUnityChorus = false;  
#endif
            instance.MPTK_EffectUnity.EnableReverb = EditorGUILayout.Toggle(new GUIContent("Apply Reverb", ""), instance.MPTK_EffectUnity.EnableReverb);
            {
                EditorGUI.indentLevel++;
                GUI.enabled = instance.MPTK_EffectUnity.EnableReverb;
                instance.MPTK_EffectUnity.ReverbDryLevel = EditorGUILayout.Slider(new GUIContent("Dry Level", "Mix level of dry signal in output"), instance.MPTK_EffectUnity.ReverbDryLevel, 0, 1f);
                instance.MPTK_EffectUnity.ReverbRoom = EditorGUILayout.Slider(new GUIContent("Room Size", "Room effect level at low frequencies"), instance.MPTK_EffectUnity.ReverbRoom, 0f, 1f);
                instance.MPTK_EffectUnity.ReverbRoomHF = EditorGUILayout.Slider(new GUIContent("Room Size HF", "Room effect high-frequency level"), instance.MPTK_EffectUnity.ReverbRoomHF, 0f, 1f);
                instance.MPTK_EffectUnity.ReverbRoomLF = EditorGUILayout.Slider(new GUIContent("Room Size LF", "Room effect low-frequency level"), instance.MPTK_EffectUnity.ReverbRoomLF, 0f, 1f);
                instance.MPTK_EffectUnity.ReverbDecayTime = EditorGUILayout.Slider(new GUIContent("Decay Time", "Reverberation decay time at low-frequencies in seconds"), instance.MPTK_EffectUnity.ReverbDecayTime, 0.1f, 20f);
                instance.MPTK_EffectUnity.ReverbDecayHFRatio = EditorGUILayout.Slider(new GUIContent("Decay Ratio", "Decay HF Ratio : High-frequency to low-frequency decay time ratio"), instance.MPTK_EffectUnity.ReverbDecayHFRatio, 0.1f, 2f);
                instance.MPTK_EffectUnity.ReverbReflectionLevel = EditorGUILayout.Slider(new GUIContent("Early Reflection", "Early reflections level relative to room effect"), instance.MPTK_EffectUnity.ReverbReflectionLevel, 0f, 1f);
                instance.MPTK_EffectUnity.ReverbReflectionDelay = EditorGUILayout.Slider(new GUIContent("Late Reflection", "Late reverberation level relative to room effect"), instance.MPTK_EffectUnity.ReverbReflectionDelay, 0f, 1f);
                instance.MPTK_EffectUnity.ReverbLevel = EditorGUILayout.Slider(new GUIContent("Reverb Level", "Late reverberation level relative to room effect"), instance.MPTK_EffectUnity.ReverbLevel, 0f, 1f);
                instance.MPTK_EffectUnity.ReverbDelay = EditorGUILayout.Slider(new GUIContent("Reverb Delay", "Late reverberation delay time relative to first reflection in seconds"), instance.MPTK_EffectUnity.ReverbDelay, 0f, 0.1f);
                instance.MPTK_EffectUnity.ReverbHFReference = EditorGUILayout.Slider(new GUIContent("HF Reference", "Reference high frequency in Hz"), instance.MPTK_EffectUnity.ReverbHFReference, 1000f, 20000f);
                instance.MPTK_EffectUnity.ReverbLFReference = EditorGUILayout.Slider(new GUIContent("LF Reference", "Reference low frequency in Hz"), instance.MPTK_EffectUnity.ReverbLFReference, 20f, 1000f);
                instance.MPTK_EffectUnity.ReverbDiffusion = EditorGUILayout.Slider(new GUIContent("Diffusion", "Reverberation diffusion (echo density) in percent"), instance.MPTK_EffectUnity.ReverbDiffusion, 0f, 1f);
                instance.MPTK_EffectUnity.ReverbDensity = EditorGUILayout.Slider(new GUIContent("Density", "Reverberation density (modal density) in percent"), instance.MPTK_EffectUnity.ReverbDensity, 0f, 1f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Generic Reverb", GUILayout.Width(150), GUILayout.Height(15));
                if (GUILayout.Button(new GUIContent("Set Default", ""), GUILayout.Width(100), GUILayout.Height(15)))
                    instance.MPTK_EffectUnity.DefaultReverb();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }

            instance.MPTK_EffectUnity.EnableChorus = EditorGUILayout.Toggle(new GUIContent("Apply Chorus", ""), instance.MPTK_EffectUnity.EnableChorus);
            {
                EditorGUI.indentLevel++;
                GUI.enabled = instance.MPTK_EffectUnity.EnableChorus;
                instance.MPTK_EffectUnity.ChorusDryMix = EditorGUILayout.Slider(new GUIContent("Dry Mix", ""), instance.MPTK_EffectUnity.ChorusDryMix, 0f, 1f);
                instance.MPTK_EffectUnity.ChorusWetMix1 = EditorGUILayout.Slider(new GUIContent("Wet Mix 1", ""), instance.MPTK_EffectUnity.ChorusWetMix1, 0f, 1f);
                instance.MPTK_EffectUnity.ChorusWetMix2 = EditorGUILayout.Slider(new GUIContent("Wet Mix 2", ""), instance.MPTK_EffectUnity.ChorusWetMix2, 0f, 1f);
                instance.MPTK_EffectUnity.ChorusWetMix3 = EditorGUILayout.Slider(new GUIContent("Wet Mix 3", ""), instance.MPTK_EffectUnity.ChorusWetMix3, 0f, 1f);
                instance.MPTK_EffectUnity.ChorusDelay = EditorGUILayout.Slider(new GUIContent("Delay in ms.", ""), instance.MPTK_EffectUnity.ChorusDelay, 0.1f, 100f);
                instance.MPTK_EffectUnity.ChorusRate = EditorGUILayout.Slider(new GUIContent("Rate in Hz.", ""), instance.MPTK_EffectUnity.ChorusRate, 0f, 20f);
                instance.MPTK_EffectUnity.ChorusDepth = EditorGUILayout.Slider(new GUIContent("Modulation Depth", ""), instance.MPTK_EffectUnity.ChorusDepth, 0f, 1f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Generic Chorus", GUILayout.Width(150), GUILayout.Height(15));
                if (GUILayout.Button(new GUIContent("Set Default", ""), GUILayout.Width(100), GUILayout.Height(15)))
                    instance.MPTK_EffectUnity.DefaultChorus();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }
    }
}
#endif
