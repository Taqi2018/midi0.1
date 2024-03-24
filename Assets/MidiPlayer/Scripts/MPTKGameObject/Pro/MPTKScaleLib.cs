using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    /// Build Scale and Play with MidiStreamPlayer.\n
    /// @version Maestro Pro 
    /// 
    /// See example in TestMidiStream.cs and ExtStreamPlayerPro.cs
    /// @code
    ///
    ///     // Need a reference to the prefab MidiStreamPlayer you have added in your scene hierarchy.
    ///     public MidiStreamPlayer midiStreamPlayer;
    ///     
    ///     new void Start()
    ///     {
    ///         // Find the MidiStreamPlayer. Could be also set directly from the inspector.
    ///         midiStreamPlayer = FindObjectOfType<MidiStreamPlayer>();
    ///     }
    ///
    ///     private void PlayScale()
    ///     {
    ///         // get the current scale selected
    ///         MPTKRangeLib range = MPTKRangeLib.Range(CurrentScale, true);
    ///         for (int ecart = 0; ecart < range.Count; ecart++)
    ///         {
    ///             NotePlaying = new MPTKEvent()
    ///             {
    ///                 Command = MPTKCommand.NoteOn, // midi command
    ///                 Value = CurrentNote + range[ecart], // from 0 to 127, 48 for C3, 60 for C4, ...
    ///                 Channel = StreamChannel, // from 0 to 15, 9 reserved for drum
    ///                 Duration = DelayPlayScale, // note duration in millisecond, -1 to play indefinitely, MPTK_StopEvent to stop
    ///                 Velocity = Velocity, // from 0 to 127, sound can vary depending on the velocity
    ///                 Delay = ecart * DelayPlayScale, // delau in millisecond before playing the note
    ///             };
    ///             midiStreamPlayer.MPTK_PlayEvent(NotePlaying);
    ///         }
    ///     }
    /// @endcode
    /// </summary>
    public class MPTKScaleLib
    {
        /// <summary>@brief
        /// Position in the list (from the library)
        /// </summary>
        public int Index;

        /// <summary>@brief
        /// Long name of the scale
        /// </summary>
        public string Name;

        /// <summary>@brief
        /// Short name of the scale
        /// </summary>
        public string Short;

        /// <summary>@brief
        /// Some indicator when available.
        /// @li   M = major scale
        /// @li   m = minor scale
        /// @li   _ = undetermined
        /// </summary>
        public string Flag;

        /// <summary>@brief
        /// Common scale if true else exotic
        /// </summary>
        public bool Main;

        /// <summary>@brief
        /// Count of notes in the range
        /// </summary>
        public int Count;

        /// <summary>@brief
        /// Indexer on an instance if this classe\n
        /// This provides access to each count of Intervals in semitones from the tonic.\n
        /// From a Major Melodic, each index will return 0, 2, 4, 5, 7, 9, 11
        /// First position (index=0) always return 0 because it is the ibterval count from the tonic xD. 
        /// </summary>
        /// <param name="index">Index in the scale. If greater than interval count in the scale, the interval in semitones is taken from the next octave.</param>
        /// <returns>Intervals in semitones from 0</returns>
        /// @code
        /// // Create a scale from the first scale found in the library: "Major melodic"
        /// // Log enabled to display the content of the scale.
        /// mptkScale = MPTKRangeLib.Range(indexScale:0, log:true);
        /// Debug.Log(mptkScale[0]) // display 0
        /// Debug.Log(mptkScale[4]) // display 7
        /// @endcode
        public int this[int index]
        {
            get
            {
                if (Count == 0) return 0;
                if (octave == null) BuildOctave();
                int delta = 0;
                try
                {
                    delta = octave[index % Count] + ((index / Count) * 12);
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }
                return delta;
            }
        }

        private int[] octave;

        /// <summary>@brief
        /// A full scale is based on 12 semitones. This array contains semitones selected for the scale.
        /// </summary>
        private string[] position;

        private static List<MPTKScaleLib> scales;

        /// <summary>@brief
        /// Get a scale from an index. Scales are read from GammeDefinition.csv in folder Resources/GeneratorTemplate.csv.
        /// </summary>
        /// <param name="indexScale"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        //public static MPTKScaleLib Scale(int indexScale, bool log = false)
        //{
        //    if (scales == null) Init(log);
        //    if (indexScale < 0 && indexScale >= scales.Count) return null;
        //    scales[indexScale].BuildOctave(log);
        //    return scales[indexScale];
        //}

        /// <summary>@brief
        /// Get a scale from an index. Scales are read from GammeDefinition.csv in folder Resources/GeneratorTemplate.csv.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static MPTKScaleLib CreateScale(MPTKScaleName index, bool log = false)
        {
            if (scales == null) Init(log);
            scales[(int)index].BuildOctave(log);
            return scales[(int)index];
        }

        /// <summary>@brief
        /// Count of scales availables in the library GammeDefinition.csv in folder Resources/GeneratorTemplate.csv
        /// </summary>
        public static int RangeCount
        {
            get
            {
                if (scales == null) Init();
                return scales.Count;
            }
        }

        private static void Init(bool log = false)
        {
            if (scales == null)
            {
                scales = new List<MPTKScaleLib>();
                TextAsset mytxtData = Resources.Load<TextAsset>("GeneratorTemplate/GammeDefinition");
                string text = System.Text.Encoding.UTF8.GetString(mytxtData.bytes);
                string[] list1 = text.Split('\r');
                if (list1.Length >= 1)
                {
                    for (int i = 1; i < list1.Length; i++)
                    {
                        string[] c = list1[i].Split(';');
                        if (c.Length >= 15)
                        {
                            MPTKScaleLib scale = new MPTKScaleLib();
                            try
                            {
                                scale.Index = scales.Count;
                                scale.Name = c[0];
                                if (scale.Name[0] == '\n') scale.Name = scale.Name.Remove(0, 1);
                                scale.Short = c[1];
                                scale.Flag = c[2];
                                scale.Main = (c[3].ToUpper() == "X") ? true : false;
                                scale.Count = Convert.ToInt32(c[4]);
                                scale.position = new string[12];
                                for (int j = 5; j <= 16; j++)
                                {
                                    scale.position[j - 5] = c[j];
                                }
                            }
                            catch (System.Exception ex)
                            {
                                MidiPlayerGlobal.ErrorDetail(ex);
                            }
                            scales.Add(scale);
                        }
                    }

                }
                if (log)
                    Debug.Log("Ranges loaded: " + MPTKScaleLib.scales.Count);
            }
        }

        private void BuildOctave(bool log = false)
        {
            if (octave == null)
            {
                try
                {
                    octave = new int[Count];
                    int iEcart = 0;
                    int vEcart = 1;
                    octave[0] = 0;
                    iEcart++;
                    for (int i = 1; i < position.Length; i++)
                    {
                        if (position[i].Trim().Length == 0)
                        {
                            vEcart++;
                        }
                        else
                        {
                            octave[iEcart] = vEcart;
                            iEcart++;
                            vEcart += 1;
                        }
                    }
                    //octave[octave.Length - 1] = 12;
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }

                if (log)
                {
                    Debug.Log($"Scale: Flag:{Flag}  Name:'{Name}'  Count:{Count} intervals in the scale");
                    Debug.Log("Example with tonic on C4 (48)");
                    Debug.Log("   Index   semitones count  Note result");
                    int index = 0;
                    foreach (int e in octave)
                        Debug.Log($"     {index++}             {e,2}            {HelperNoteLabel.LabelFromMidi(48 + e)}");
                }
            }
        }
    }

    /// <summary>
    /// List of ranges available
    /// @version Maestro Pro 
    /// </summary>
    public enum MPTKScaleName
    {
        MajorMelodic = 0,
        MajorHarmonic = 1,
        MinorNatural = 2,
        MinorMelodic = 3,
        MinorHarmonic = 4,
        PentatonicMajor = 5,
        PentatonicMinor = 6,
        Chromatic = 7,
        Blues = 8,
        Enigmatic1 = 9,
        Enigmatic2 = 10,
        Gitane = 11,
        Oriental1 = 12,
        BebopMajor = 13,
        AeolienB5 = 14,
        Arabic = 15,
        Augmented = 16,
        Bahar = 17,
        Balinaise = 18,
        Bartock = 19,
        BebopDominante = 20,
        Aeolien = 21,
        BebopMinor = 22,
        BitonalMajorChromatic = 23,
        BitonalMinorChromatic = 24,
        BluesDecreased1 = 25,
        BluesDecreased2 = 26,
        MajorBlues1 = 27,
        MinorBlues1 = 28,
        MajorBlues2 = 29,
        MinorBlues2 = 30,
        Chinese1 = 31,
        Chinese2 = 32,
        DemiDecreased = 33,
        DemiTonNoSixte = 34,
        Diminish = 35,
        Dorien = 36,
        Spanish1 = 37,
        Spanish2 = 38,
        Spanish8 = 39,
        Gypsy = 40,
        Hexalydien = 41,
        HexaMelodic = 42,
        HexaPhrygien = 43,
        HexaTritoniqueBinary = 44,
        HexaTritoniqueDecreased1 = 45,
        HexaTritoniqueDecreased2 = 46,
        HexaTritoniqueDecreased3 = 47,
        Hindou = 48,
        Hirajoshi = 49,
        HongroiseGitane = 50,
        HongroiseMajor = 51,
        HongroiseMinor = 52,
        Indoustane = 53,
        Ionien = 54,
        Ionien5 = 55,
        Iwato = 56,
        Javanais = 57,
        KokinJoshi = 58,
        Kumoi = 59,
        Locrien = 60,
        Locrien6 = 61,
        Lydien1 = 62,
        Lydien2 = 63,
        Lydien3 = 64,
        Mixolydien = 65,
        NapolitanMajor = 66,
        NapolitanMinor = 67,
        Oriental2 = 68,
        Oriental3 = 69,
        PentatonicHarmonic = 70,
        PentatonicDominante = 71,
        PentatonicEgyptian = 72,
        PentatonicJapanese = 73,
        PentatonicLocrien1 = 74,
        PentatonicLocrien2 = 75,
        PentatonicMauritanian = 76,
        PentatonicPelog = 77,
        Persane1 = 78,
        Persane2 = 79,
        Phrygien = 80,
        Promethee = 81,
        RoumanMinor = 82,
        SuperlocrienBB7 = 83,
        SuperlocrienAltered = 84,
        TonByTon = 85,
    }
}
