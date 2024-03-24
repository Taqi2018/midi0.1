#if UNITY_EDITOR
namespace MidiPlayerTK
{
    public class TPatchUsed
    {
        public TBankUsed[] BankUsed;
        public int DefaultBankNumber = -1;
        public int DrumKitBankNumber = -1;

        public TPatchUsed()
        {
            BankUsed = new TBankUsed[130];
        }
    }

    public class TBankUsed
    {
        public TNoteUsed[] PatchUsed;
        public TBankUsed()
        {
            PatchUsed = new TNoteUsed[128];
        }
    }

    public class TNoteUsed
    {
        public int[] Note;
        public TNoteUsed()
        {
            Note = new int[128];
        }
    }

    /// <summary>@brief
    /// Scan midifiles and returns patchs used
    /// </summary>
    public class MidiOptim
    {
        /// <summary>@brief
        /// Scan midifiles and returns patchs used
        /// </summary>
        /// <param name="Info"></param>
        /// <returns></returns>
        static public TPatchUsed PatchUsed(BuilderInfo Info)
        {
            TPatchUsed filters = new TPatchUsed();
            try
            {
                filters.DefaultBankNumber = MidiPlayerGlobal.ImSFCurrent.DefaultBankNumber;
                filters.DrumKitBankNumber = MidiPlayerGlobal.ImSFCurrent.DrumKitBankNumber;

                if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles == null || MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count == 0)
                {
                    Info.Add("No MIDI files defined, can't optimize");
                    filters = null;
                }
                else

                    foreach (string midifilepath in MidiPlayerGlobal.CurrentMidiSet.MidiFiles)
                    {
                        Info.Add("   Scan " + midifilepath);

                        int[] currentPatch = new int[16];
                        MidiLoad midifile = new MidiLoad();
                        midifile.MPTK_KeepNoteOff = false;
                        midifile.MPTK_Load(midifilepath);
                        if (midifile != null)
                        {
                            foreach (MPTKEvent mptkEvent in midifile.MPTK_MidiEvents)
                            {

                                if (mptkEvent.Command== MPTKCommand.NoteOn)
                                {
                                    //if (((NoteOnEvent)trackEvent.Event).OffEvent != null)
                                    {
                                        //infoTrackMidi[e.IndexSection].Events.Add((NoteOnEvent)e);
                                        //NoteOnEvent noteon = (NoteOnEvent)trackEvent.Event;
                                        //if (noteon.OffEvent != null)
                                        {
                                            int banknumber = mptkEvent.Channel == 10 ? filters.DrumKitBankNumber : filters.DefaultBankNumber;
                                            int patchnumber = currentPatch[mptkEvent.Channel];
                                            if (banknumber >= 0)
                                            {
                                                if (filters.BankUsed[banknumber] == null)
                                                    filters.BankUsed[banknumber] = new TBankUsed();

                                                if (filters.BankUsed[banknumber].PatchUsed[patchnumber] == null)
                                                    filters.BankUsed[banknumber].PatchUsed[patchnumber] = new TNoteUsed();

                                                filters.BankUsed[banknumber].PatchUsed[patchnumber].Note[mptkEvent.Value]++;
                                            }
                                        }
                                    }
                                }
                                else if (mptkEvent.Command == MPTKCommand.PatchChange)
                                {
                                    //PatchChangeEvent change = (PatchChangeEvent)trackEvent.Event;
                                    // Always use patch 0 for drum kit
                                    currentPatch[mptkEvent.Channel] = mptkEvent.Channel == 10 ? 0 : mptkEvent.Value;
                                }
                            }
                        }
                        else
                        {
                        }
                    }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
            return filters;
        }
    }
}

#endif