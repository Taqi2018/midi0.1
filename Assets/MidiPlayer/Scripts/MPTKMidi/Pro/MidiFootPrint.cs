using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    /// Experimental - Search a MIDI from a sequence of notes and calculate a score tempo.\n
    /// 
    /// These capabilities and API could evolve in time or ... disappear!\n
    /// 
    /// A footprint is calculated for each MIDI added in #MPTK_MidiLib with #MPTK_AddOne or #MPTK_AddMultiple.\n
    /// Then #MPTK_Search can be used to find a list of MIDI with the same fooprint (or partial footprint) from a sequence of notes (List of MPTKEvent).\n
    /// If notes in the sequence contains duration then a tempo score is given for each MIDI found.\n
    /// Limitation: max of 8 first notes are used for calculating the footprint\n
    /// @version Maestro Pro 
    /// @code
    /// 
    ///      private MPTKFootPrint libFootPrint;
    ///      
    ///      void Awake()
    ///      {
    ///          libFootPrint = gameObject.AddComponent<MPTKFootPrint>();
    ///      }
    ///      private void Start()
    ///      {
    ///          libFootPrint.Verbose = true;
    ///          libFootPrint.MPTK_AddMultiple("BACH");
    ///          libFootPrint.MPTK_AddOne("_simple-48");
    ///          libFootPrint.MPTK_AddOne("_simple-49");
    ///          libFootPrint.MPTK_AddOne("_simple-50");
    ///      }
    ///      
    ///      private void YourMethod()
    ///      {
    ///         // .... build a sequence of notes ...
    ///         SequenceNotes.Add(new MPTKEvent() {Command = MPTKCommand.NoteOn, Value = value, Duration = duration) });
    ///         
    ///         // .... Find MIDI with this sequence ...
    ///        List<MPTKFootPrint.FootPrint> listMidi = libFootPrint.MPTK_Search(SequenceNotes);
    ///        
    ///        if (listMidi.Count > 0)
    ///        {
    ///           foreach (MPTKFootPrint.FootPrint midiFP in listMidi)
    ///              Debug.Log(midiFP.ToString());
    ///         }
    ///         else { Debug.Log("No MIDI found"); }
    ///      }
    /// @endcode
    /// </summary>
    public class MPTKFootPrint : MonoBehaviour
    {
        /// <summary>@brief
        /// List of MIDI to search for a sequence of notes. MIDI can be added with #MPTK_AddOne or #MPTK_AddMultiple.
        /// </summary>
        public List<FootPrint> MPTK_MidiLib;

        /// <summary>@brief
        /// First note of the footprint range
        /// </summary>
        public int SettingFirstNote { get => firstNote; set { firstNote = Mathf.Clamp(value, 0, 127); Configuration(); } }

        /// <summary>@brief
        /// Last note of the footprint range
        /// </summary>
        public int SettingLastNote { get => lastNote; set { lastNote = Mathf.Clamp(value, 0, 127); } }

        /// <summary>@brief
        /// Shift bit for the footprint builder. Default value = 8. Other value has not been tested.
        /// </summary>
        public uint SettingShiftLeft { get => shiftLeft; set { shiftLeft = (uint)Mathf.Clamp(value, 1, 8); Configuration(); } }


        /// <summary>@brief
        /// Number of notes used to calculated the footprint. Default value = 8.
        /// </summary>
        public int SettingCountNote { get => countNote; set { countNote = value; Configuration(); } }

        /// <summary>@brief
        /// For debugging goal ...
        /// </summary>
        public bool Verbose;

        private int firstNote;
        private int lastNote;
        private uint shiftLeft;
        private ulong masque; // Calculated bit mask for the footprint builder.
        private int countNote;

        // Example: ulong = 64 bits, 64 / 5 = 12 notes max can be used to build the footprint
        uint maxNote;

        public void Configuration()
        {
            if (SettingShiftLeft != 0)
            {
                maxNote = 64 / SettingShiftLeft;
                masque = (ulong)Mathf.Pow(2, SettingShiftLeft) - 1;
                if (Verbose) Debug.Log($"Configuration {maxNote} {masque:X16}");
                if (SettingCountNote > maxNote)
                    Debug.LogWarning($"MPTKFootPrint length [{SettingCountNote}] too high, possible overriding. Max note: {maxNote}");
            }
        }

        private MidiFilePlayer midiFileLoader;

        private void Awake()
        {
            //Debug.Log("Start MidiFootPrint");

            // MidiPlayerGlobal is a singleton: only one instance can be created. 
            if (MidiPlayerGlobal.Instance == null)
                gameObject.AddComponent<MidiPlayerGlobal>();

            midiFileLoader = gameObject.AddComponent<MidiFilePlayer>();
            MPTK_Clear();

            // FootPrint are calculated from a range of note and a number of note. A default setting is used: Get all notes for a max of 8.
            SettingFirstNote = 0; // other example: 60=C5
            SettingLastNote = 127; // other example:  80=C7
            SettingShiftLeft = 8; // Range of 24 notes --> need 5 bits (code for 0 to 31 decimal), but keep it simple for this version: 8 bits for each notes coding for 0 to 255, useless but simpler !
            SettingCountNote = 8;
            Verbose = false;
            Configuration();
        }

        /// <summary>@brief
        /// Clear MPTK_MidiLib.
        /// </summary>
        public void MPTK_Clear()
        {
            MPTK_MidiLib = new List<FootPrint>();
        }

        /// <summary>@brief
        /// Add a MIDI to MPTK_MidiLib. Use the exact name defined in Unity resources folder MidiDB without any path or extension.
        /// </summary>
        /// <param name="name"></param>
        public void MPTK_AddOne(string name)
        {
            midiFileLoader.MPTK_MidiName = name;
            if (midiFileLoader.MPTK_Load() !=null)
            {
                FootPrint footprint = MPTK_Encode(midiFileLoader.MPTK_MidiEvents);
                footprint.Name = name;
                MPTK_MidiLib.Add(footprint);
                if (Verbose)
                    Debug.Log($"Add MIDI {footprint.ToString()}");
            }
            else
                Debug.Log($"MPTKFootPrint Add - Error when loading {name}");
        }

        /// <summary>@brief
        /// Add multiple MIDI to MPTK_MidiLib. 
        /// </summary>
        /// <param name="filter">Only add MIDI when name contains filter in parameter. No case sensitive. Add all MIDI DB if null or missing</param>
        public void MPTK_AddMultiple(string filter = null)
        {
            if (MidiPlayerGlobal.CurrentMidiSet != null)
            {
                string filterLower = filter.ToLower();
                foreach (string name in MidiPlayerGlobal.CurrentMidiSet.MidiFiles)
                {
                    if (filter == null || name.ToLower().Contains(filterLower))
                        MPTK_AddOne(name);
                }
            }
            MPTK_MidiLib = MPTK_MidiLib.OrderBy(o => o.Ident).ToList();
        }

        /// <summary>@brief
        /// Search in MPTK_MidiLib from the footprint (or partial footprint) from a sequence of notes.\n
        /// If notes in the sequence contains duration then a tempo score is given for each MIDI found.\n
        /// </summary>
        /// <param name="sequence">Sequence of MPTKEvent to search. Only the 8 first notes are used</param>
        /// <param name="countnote">Number of notes to search. The count of notes in sequence must be equal to the count of note of the searched MIDI.\n
        /// Default= -1, for searching partial match. </param>
        /// <param name="tempoScore">Only MIDI found with a score equal or greater are keep in the resulting list. Default = -1, keep all MIDI regardless the score.</param>
        /// <returns>List of MIDI found. Each FootPrint in list contains the MIDI name and the tempo score between 0 and 1</returns>
        public List<FootPrint> MPTK_Search(List<MPTKEvent> sequence, int countnote = -1, float tempoScore = -1f)
        {
            FootPrint footprint = MPTK_Encode(sequence);

            // by default, find with the exact count of notes (whole fingerprint)
            ulong masque = ulong.MaxValue;

            if (countnote == -1)
            {
                // Calculate a partial footprint 
                uint delta = (uint)(SettingCountNote - sequence.Count);
                int shift = (int)SettingShiftLeft * (int)delta;
                masque = ~((ulong)Mathf.Pow(2, shift) - 1);
            }

            List<FootPrint> found = new List<FootPrint>();

            foreach (FootPrint midiLib in MPTK_MidiLib)
            {
                //Debug.Log($" masque:{masque:X16} file.FootPrint:{file.FootPrint:X16} file.FootPrint & masque:{(file.FootPrint & masque):X16} footprint:{footprint:X16}");
                if ((midiLib.Ident & masque) == footprint.Ident)
                {
                    // Check also duration accuracy
                    float averageRatio = 0f;
                    for (int i = 0; i < footprint.Notes.Count; i++)
                    {
                        float ratio = (float)footprint.Notes[i].Duration / (float)midiLib.Notes[i].Duration;
                        if (ratio > 1f) ratio = 1f / ratio;
                        averageRatio += ratio;
                    }
                    averageRatio /= footprint.Notes.Count;
                    midiLib.ScoreTempo = averageRatio;
                    // Do we have to store this result ? 
                    if (tempoScore <= 0f || tempoScore >= averageRatio)
                        found.Add(midiLib);
                }
            }

            return found;
        }

        // decode a footprint, useful for debug reason
        private List<int> Decode(ulong footprint, int countNote)
        {
            List<int> sequence = new List<int>(countNote);

            for (int i = 0; i < countNote; i++)
            {
                int note = (int)(footprint & (ulong)masque) + SettingFirstNote;
                Debug.Log($"Decode {i} {note} {footprint}");
                sequence.Add(note);
            }
            //Debug.Log($"----- FootPrint {footprint} -----");
            //sequence.Reverse();
            return sequence;
        }

        /// <summary>@brief
        /// Calculate the footprint from the sequence of MPTKEvent. Only noteon in the range defined in the Setting are used.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns>footprint</returns>
        public FootPrint MPTK_Encode(List<MPTKEvent> sequence)
        {
            FootPrint footprint = new FootPrint();

            if (sequence != null)
            {
                foreach (MPTKEvent midiEvent in sequence)
                {
                    if (midiEvent.Command == MPTKCommand.NoteOn)
                    {
                        if (midiEvent.Value >= SettingFirstNote && midiEvent.Value <= SettingLastNote)
                        {
                            footprint.Ident <<= (int)SettingShiftLeft;
                            footprint.Ident |= (ulong)midiEvent.Value - (ulong)SettingFirstNote;
                            //Debug.Log($"Encode {NoteCount} {midiEvent.Value} {FootPrint}");
                            //footprint.NoteCount++;
                            footprint.Notes.Add(midiEvent);
                            if (footprint.Notes.Count >= SettingCountNote)
                            {
                                // encoding is over
                                if (Verbose)
                                    Debug.Log($"FootPrint is calculated with a maximum of {SettingCountNote} notes");
                                break;
                            }
                        }
                        else
                        {
                            if (Verbose)
                                Debug.Log($"FootPrint note must be in the range [{SettingFirstNote} , {SettingLastNote}]");
                        }
                    }
                }

                if (footprint.Notes.Count < SettingCountNote)
                    footprint.Ident <<= (int)SettingShiftLeft * (SettingCountNote - footprint.Notes.Count);

                //while (footprint.NoteCount < MPTK_Setting.CountNote)
                //{
                //    footprint.Ident <<= (int)MPTK_Setting.ShiftLeft;
                //    footprint.NoteCount++;
                //}

                if (Verbose)
                    Debug.Log(ToString());

            }
            return footprint;
        }

        /// <summary>
        /// Contains detailed information about a MIDI footprint.
        /// </summary>
        public class FootPrint
        {
            /// <summary>@brief
            /// Number of notes used to build the footprint. Can be inferior than the number of notes used to build the footprint if some notes are ouside the filter range (see Setting)
            /// </summary>
            public int NoteCount { get { return Notes != null ? Notes.Count : 0; } }

            /// <summary>@brief
            /// MIDI name as found in the MPTK DB
            /// </summary>
            public string Name;

            /// <summary>@brief
            /// Calculated footprint
            /// </summary>
            public ulong Ident;

            /// <summary>@brief
            /// List of notes used to build the footprint
            /// </summary>
            public List<MPTKEvent> Notes;

            /// <summary>@brief
            /// Score of the tempo calculated with MPTK_Search
            /// </summary>
            public float ScoreTempo;

            public FootPrint()
            {
                Name = "";
                Ident = 0L;
                Notes = new List<MPTKEvent>();
            }

            override public string ToString()
            {
                string name;
                if (Name != null)
                    if (Name.Length > 15)
                        name = Name.Substring(0, 15);
                    else
                        name = Name;
                else
                    name = "";

                string info = $"{name.PadRight(15, ' ')} FootPrint:{Ident:X16} Score:{ScoreTempo:F2}";
                foreach (MPTKEvent note in Notes) info += $" {note.Value}/{note.Duration}";
                return info;
            }

        }
    }
}