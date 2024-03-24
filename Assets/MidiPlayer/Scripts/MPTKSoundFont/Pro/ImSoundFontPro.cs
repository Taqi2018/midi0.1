using MEC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>@brief
    /// SoundFont adapted to Unity
    /// </summary>
    public partial class ImSoundFont
    {
        /// <summary>@brief
        /// Save an ImSoundFont 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        public void SaveMPTK(string path, string name, bool onlyXML)
        {
            try
            {
                if (!onlyXML)
                {
                    // Save SF binary data 
                    new SFSave(path + "/" + name + MidiPlayerGlobal.ExtensionSoundFileFileData, HiSf, SFFile.SfSource.MPTK);
                }

                // Build bank selected
                StrBankSelected = "";
                for (int b = 0; b < BankSelected.Length; b++)
                    if (BankSelected[b])
                        StrBankSelected += b + ",";

                var serializer = new XmlSerializer(typeof(ImSoundFont));
                using (var stream = new FileStream(path + "/" + name + MidiPlayerGlobal.ExtensionSoundFileDot, FileMode.Create))
                {
                    serializer.Serialize(stream, this);
                }
            }
            catch (System.Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        static public IEnumerator<float> LoadLiveSF(string pathSF, int defaultBank = -1, int drumBank = -1, MidiSynth[] synths = null, bool restartPlayer = true, bool useCache = true, bool log = false)
        {
            if (log) Debug.Log("LoadLiveSF " + pathSF);
            MidiPlayerGlobal.MPTK_SoundFontLoaded = false;
            List<MidiFilePlayer> playerToRestart = new List<MidiFilePlayer>();

            if (synths != null)
            {
                foreach (MidiSynth synth in synths)
                {
                    if (synth is MidiFilePlayer)
                    {
                        MidiFilePlayer player = (MidiFilePlayer)synth;
                        if (player.MPTK_IsPlaying)
                        {
                            playerToRestart.Add(player);
                            player.MPTK_Stop(); // stop and clear all sound
                        }
                    }
                    //synth.MPTK_ClearAllSound();
                    if (Application.isPlaying)
                        yield return Routine.WaitUntilDone(Routine.RunCoroutine(synth.ThreadWaitAllStop(), Segment.RealtimeUpdate), false);
                    else
                        yield return Routine.WaitUntilDone(Routine.RunCoroutine(synth.ThreadWaitAllStop(), Segment.EditorUpdate), false);
                    synth.MPTK_StopSynth();
                }
            }

            DicAudioClip.Init();
            DicAudioWave.Init();

            Uri uri = new Uri(pathSF);
            string sfName = uri.Segments.Last();
            string soundPath = MidiPlayerGlobal.MPTK_PathSoundFontCache;
            if (!Directory.Exists(soundPath)) Directory.CreateDirectory(soundPath);
            string soundFile = Path.Combine(soundPath, sfName);
            if (log) Debug.Log($"Start Loading SF {uri}, save to {soundFile}");
            System.Diagnostics.Stopwatch watchLoadSF = new System.Diagnostics.Stopwatch(); // High resolution time
            watchLoadSF.Start();

            if (useCache && File.Exists(soundFile))
            {
                if (log) Debug.Log($"Load SF {sfName} from cache");

                try
                {
                    MidiPlayerGlobal.timeToDownloadSoundFont = watchLoadSF.Elapsed;
                    watchLoadSF.Restart();
                    SFLoad sf = new SFLoad(soundFile, SFFile.SfSource.SF2);
                    CreateSoundFont(sf.SfData, defaultBank, drumBank, synths, restartPlayer, playerToRestart);
                }
                catch (System.Exception ex)
                {
                    MidiPlayerGlobal.MPTK_StatusLastSoundFontLoaded = LoadingStatusSoundFontEnum.SoundFontNotLoaded;
                    MidiPlayerGlobal.ErrorDetail(ex);
                }
            }
            else
            {
                using (UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get(uri))
                {
                    if (log) Debug.Log($"Download SF {sfName}");

                    yield return Routine.WaitUntilDone(req.SendWebRequest());
                    //Debug.Log($"result:{req.result} {pathSF}");
#if UNITY_2020_2_OR_NEWER
                    if (req.result == UnityEngine.Networking.UnityWebRequest.Result.InProgress ||
                        req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
#else
                if (!req.isNetworkError)
#endif
                    {
                        try
                        {
                            MidiPlayerGlobal.timeToDownloadSoundFont = watchLoadSF.Elapsed;
                            watchLoadSF.Restart();
                            byte[] data = req.downloadHandler.data;
                            if (data == null)
                            {
                                MidiPlayerGlobal.MPTK_StatusLastSoundFontLoaded = LoadingStatusSoundFontEnum.SoundFontEmpty;
                                Debug.LogWarning("SoundFont not find or not a SoundFont - " + pathSF);

                            }
                            else if (data.Length < 4 || System.Text.Encoding.Default.GetString(data, 0, 4) != "RIFF")
                            {
                                MidiPlayerGlobal.MPTK_StatusLastSoundFontLoaded = LoadingStatusSoundFontEnum.NoRIFFSignature;
                                Debug.LogWarning("SoundFont not find or not a SoundFont - " + pathSF);
                            }
                            else
                            {
                                //Debug.Log("Load with header " + System.Text.Encoding.Default.GetString(data, 0, 8));
                                SFLoad sf = new SFLoad(data, SFFile.SfSource.SF2);
                                CreateSoundFont(sf.SfData, defaultBank, drumBank, synths, restartPlayer, playerToRestart);
                                File.WriteAllBytes(soundFile, data);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            MidiPlayerGlobal.MPTK_StatusLastSoundFontLoaded = LoadingStatusSoundFontEnum.SoundFontNotLoaded;
                            MidiPlayerGlobal.ErrorDetail(ex);
                        }
                    }
                    else
                    {
                        MidiPlayerGlobal.MPTK_StatusLastSoundFontLoaded = LoadingStatusSoundFontEnum.NetworkError;
                        Debug.LogWarning("Network error - " + pathSF);
                    }
                }
            }

            if (MidiPlayerGlobal.ImSFCurrent != null)
                MidiPlayerGlobal.ImSFCurrent.SoundFontName = sfName;
            MidiPlayerGlobal.timeToLoadSoundFont = watchLoadSF.Elapsed;

            if (MidiPlayerGlobal.MPTK_StatusLastSoundFontLoaded == LoadingStatusSoundFontEnum.InProgress)
                MidiPlayerGlobal.MPTK_StatusLastSoundFontLoaded = LoadingStatusSoundFontEnum.Success;

            try
            {
                if (MidiPlayerGlobal.OnEventPresetLoaded != null)
                    MidiPlayerGlobal.OnEventPresetLoaded.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError("OnEventPresetLoaded: exception detected. Check the callback code");
                Debug.LogException(ex);
            }

        }

        public static void CreateSoundFont(SFData sfData, int defaultBank, int drumBank, MidiSynth[] synths = null, bool restartPlayer = true, List<MidiFilePlayer> playerToRestart = null)
        {
            ImSoundFont imsf = new ImSoundFont();
            imsf.HiSf = sfData;
            //Debug.Log("   SampleData.Length:" + load.SfData.SampleData.Length);
            //Debug.Log("   preset.Length:" + load.SfData.preset.Length);
            //Debug.Log("   Samples.Length:" + load.SfData.Samples.Length);

            imsf.LiveSF = true;

            imsf.Banks = new ImBank[ImSoundFont.MAXBANKPRESET];
            foreach (HiPreset p in imsf.HiSf.preset)
            {
                imsf.BankSelected[p.Bank] = true;
                if (imsf.Banks[p.Bank] == null)
                {
                    // New bank, create it
                    imsf.Banks[p.Bank] = new ImBank()
                    {
                        BankNumber = p.Bank,
                        defpresets = new HiPreset[ImSoundFont.MAXBANKPRESET]
                    };
                }

                // Sort preset by number of patch
                imsf.Banks[p.Bank].defpresets[p.Num] = p;
            }

            //int lastBank = 0; // generally drum kit

            foreach (ImBank bank in imsf.Banks)
            {
                if (bank != null)
                {
                    bank.PatchCount = 0;
                    //lastBank = bank.BankNumber;
                    foreach (HiPreset preset in bank.defpresets)
                        if (preset != null)
                        {
                            // Bank count
                            bank.PatchCount++;
                        }
                    // sf.PatchCount += bank.PatchCount;
                }
            }

            imsf.DefaultBankNumber = defaultBank < 0 ? imsf.FirstBank() : defaultBank;
            imsf.DrumKitBankNumber = drumBank < 0 ? imsf.LastBank() : drumBank;
            //Debug.Log("DefaultBankNumber:" + imsf.DefaultBankNumber);
            //Debug.Log("DrumKitBankNumber:" + imsf.DrumKitBankNumber);
            if (MidiPlayerGlobal.ImSFCurrent != null)
            {
                //Debug.Log(">>> Collect " + DateTime.Now + " " + GC.GetTotalMemory(false));
                MidiPlayerGlobal.ImSFCurrent.SamplesData = null;
                GC.Collect();
                //Debug.Log("<<< Collect " + DateTime.Now + " " + GC.GetTotalMemory(false));
            }
            MidiPlayerGlobal.ImSFCurrent = imsf;
            MidiPlayerGlobal.BuildBankList();
            MidiPlayerGlobal.BuildPresetList(true);
            MidiPlayerGlobal.BuildPresetList(false);
            //MidiPlayerGlobal.MPTK_SoundFontLoaded = true;

            System.Diagnostics.Stopwatch watchLoadWave = new System.Diagnostics.Stopwatch(); // High resolution time
            watchLoadWave.Start();
            imsf.SamplesData = new float[imsf.HiSf.SampleData.Length / 2];
            int size = imsf.HiSf.SampleData.Length / 2 - 1;
            for (int i = 0, j = 0; i <= size; i++, j += 2)
                imsf.SamplesData[i] = ((short)((imsf.HiSf.SampleData[j + 1] << 8) | imsf.HiSf.SampleData[j])) / 32768.0F;
            MidiPlayerGlobal.timeToLoadWave = watchLoadWave.Elapsed;
            MidiPlayerGlobal.MPTK_SoundFontLoaded = true;

            if (synths != null)
            {
                foreach (MidiSynth synth in synths)
                {
                    synth.MPTK_InitSynth();
                    if (synth is MidiFilePlayer)
                        synth.MPTK_StartSequencerMidi();
                }
                if (restartPlayer && playerToRestart != null)
                    foreach (MidiFilePlayer player in playerToRestart)
                        player.MPTK_RePlay();
            }
        }

        //        static bool testSavingSF = false;
        //        static public IEnumerator<float> MergeLiveSF(string pathInitSF, string pathAddSF)
        //        {
        //            //Debug.Log("LoadLiveSF " + pathSF);
        //            MidiPlayerGlobal.MPTK_SoundFontLoaded = false;

        //            // Load SF to merge into
        //            // ---------------------
        //            Debug.Log($"Loading {Path.GetFileName(pathInitSF)}");
        //            SoundFontData = null;
        //            yield return Routine.WaitUntilDone(Routine.RunCoroutine(LoadSFForEdit(pathInitSF), Segment.RealtimeUpdate), false);
        //            if (SoundFontData == null) yield return 0;
        //            SFData dataInit = SoundFontData;

        //            if (testSavingSF)
        //            {
        //                Debug.Log($"");
        //                Debug.Log($"Saving saved.sf2");
        //                new SFSave(@"C:\Users\Thierry\Desktop\BIM\Sound\Merged\saved.sf2", dataInit, SFFile.SfSource.SF2);

        //                Debug.Log($"");
        //                Debug.Log($"Reload saved.sf2");
        //                SoundFontData = null;
        //                yield return Routine.WaitUntilDone(Routine.RunCoroutine(LoadSFForEdit(@"C:\Users\Thierry\Desktop\BIM\Sound\Merged\saved.sf2"), Segment.RealtimeUpdate), false);
        //                if (SoundFontData == null) yield return 0;
        //                dataInit = SoundFontData;
        //            }
        //            else
        //            {
        //                // Load SF to add to be merged
        //                // ---------------------------
        //                Debug.Log($"Loading {Path.GetFileName(pathAddSF)}");
        //                SoundFontData = null;
        //                yield return Routine.WaitUntilDone(Routine.RunCoroutine(LoadSFForEdit(pathAddSF), Segment.RealtimeUpdate), false);
        //                if (SoundFontData == null) yield return 0;
        //                SFData dataToAdd = SoundFontData;


        //                // Build samples data
        //                // ------------------

        //                byte[] dataNew = new byte[dataToAdd.SampleData.Length + dataInit.SampleData.Length];
        //                // Copy initial samples
        //                dataInit.SampleData.CopyTo(dataNew, 0);
        //                dataToAdd.SampleData.CopyTo(dataNew, dataInit.SampleData.Length);

        //                // Build Samples list
        //                // ---------------------

        //                // Create new list of samples
        //                HiSample[] smplNew = new HiSample[dataToAdd.Samples.Length + dataInit.Samples.Length];
        //                // Copy initial list 
        //                dataInit.Samples.CopyTo(smplNew, 0);
        //                // Add samples at the end of the list, shift index
        //                foreach (HiSample s in dataToAdd.Samples)
        //                {
        //                    s.ItemId += dataInit.Samples.Length;
        //                    s.Start += (uint)dataInit.SampleData.Length / 2;
        //                    s.End += (uint)dataInit.SampleData.Length / 2;
        //                    s.LoopStart += (uint)dataInit.SampleData.Length / 2;
        //                    s.LoopEnd += (uint)dataInit.SampleData.Length / 2;
        //                }
        //                dataToAdd.Samples.CopyTo(smplNew, dataInit.Samples.Length);

        //                // Build Instruments list
        //                // ---------------------

        //                // Create new list of instruments
        //                HiInstrument[] instNew = new HiInstrument[dataToAdd.inst.Length + dataInit.inst.Length];
        //                // Copy initial list 
        //                dataInit.inst.CopyTo(instNew, 0);
        //                // Add intrument at the end of the list, shift index
        //                foreach (HiInstrument i in dataToAdd.inst)
        //                {
        //                    i.ItemId += dataInit.inst.Length;
        //                    foreach (HiZone z in i.Zone)
        //                    {
        //                        if (z.gens != null)
        //                        {
        //                            foreach (HiGen g in z.gens)
        //                            {
        //                                if (g != null)
        //                                {
        //                                    switch (g.type)
        //                                    {
        //                                        case fluid_gen_type.GEN_SAMPLEID:
        //                                            z.Index += dataInit.Samples.Length; // shift for added samples
        //                                            g.Amount.Sword = (short)z.Index;
        //                                            if (z.Index < 0 || z.Index >= dataInit.Samples.Length + dataToAdd.Samples.Length)
        //                                                Debug.LogFormat("Instrument:{0} zone:{1} {2} {3} *** errror, wave not defined", i.Name, z.Index, g.type, g.Amount.Sword);
        //                                            break;
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //                dataToAdd.inst.CopyTo(instNew, dataInit.inst.Length);

        //                // Build Preset list
        //                // ---------------------

        //                // Create new list of presets
        //                HiPreset[] presNew = new HiPreset[dataToAdd.preset.Length + dataInit.preset.Length];
        //                // Copy initial list 
        //                dataInit.preset.CopyTo(presNew, 0);

        //                // Add new presets at the end of the list, shift index
        //                foreach (HiPreset p in dataToAdd.preset)
        //                {
        //                    p.ItemId += dataInit.preset.Length;
        //                    foreach (HiZone zone in p.Zone)
        //                    {
        //                        if (zone.gens != null)
        //                        {
        //                            foreach (HiGen gen in zone.gens)
        //                            {
        //                                if (gen != null)
        //                                {
        //                                    switch (gen.type)
        //                                    {
        //                                        case fluid_gen_type.GEN_INSTRUMENT:
        //                                            zone.Index += dataInit.inst.Length; // shift for added instrument
        //                                            gen.Amount.Sword = (short)zone.Index;
        //                                            if (zone.Index < 0 || zone.Index >= dataInit.inst.Length + dataToAdd.inst.Length)
        //                                                Debug.LogFormat("Preset:{0} zone:{1} {2} {3} *** errror, instrument not defined", p.Name, zone.Index, gen.type, gen.Amount.Sword);
        //                                            break;
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //                dataToAdd.preset.CopyTo(presNew, dataToAdd.preset.Length);

        //                // Set SF information to SF target
        //                // -------------------------------

        //                // New sample data
        //                dataInit.SampleData = dataNew;

        //                // New list of samples
        //                dataInit.Samples = smplNew;

        //                // New list of instruments
        //                dataInit.inst = instNew;

        //                // New list of presets
        //                dataInit.preset = presNew;

        //                foreach (SFInfo sFInfo in dataInit.info)
        //                {
        //                    //Debug.Log($"{sFInfo.id} {sFInfo.Text}");
        //                    switch (sFInfo.id)
        //                    {
        //                        case File_Chunk_ID.INAM_ID:
        //                            sFInfo.Text = "merged";
        //                            break;
        //                        case File_Chunk_ID.IPRD_ID:
        //                        case File_Chunk_ID.ISFT_ID:
        //                        case File_Chunk_ID.ICOP_ID:
        //                        case File_Chunk_ID.IENG_ID:
        //                            sFInfo.Text = "Maestro";
        //                            break;

        //                        case File_Chunk_ID.ICRD_ID:
        //                            sFInfo.Text = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss"); // https://www.c-sharpcorner.com/blogs/date-and-time-format-in-c-sharp-programming1
        //                            break;
        //                    }
        //                }

        //                new SFSave(@"C:\Users\Thierry\Desktop\BIM\Sound\Merged\merged.sf2", dataInit, SFFile.SfSource.SF2);
        //                Debug.Log($"Saved merged");

        //                //Debug.Log("Start Loading SF " + pathSF);

        //                //imsf.SampleData = new float[imsf.HiSf.SampleData.Length / 2];
        //                //int size = imsf.HiSf.SampleData.Length / 2 - 1;
        //                //for (int i = 0, j = 0; i <= size; i++, j += 2)
        //                //    imsf.SampleData[i] = ((short)((imsf.HiSf.SampleData[j + 1] << 8) | imsf.HiSf.SampleData[j])) / 32768.0F;
        //            }
        //        }

        //        static SFData SoundFontData;

        //        static public IEnumerator<float> LoadSFForEdit(string pathInitSF)
        //        {

        //            using (UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get(pathInitSF))
        //            {
        //                yield return Routine.WaitUntilDone(req.SendWebRequest());
        //                //Debug.Log($"result:{req.result} {pathSF}");
        //#if UNITY_2020_2_OR_NEWER
        //                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.InProgress ||
        //                    req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        //#else
        //                if (!req.isNetworkError)
        //#endif
        //                {
        //                    try
        //                    {
        //                        byte[] data = req.downloadHandler.data;
        //                        if (data != null && data.Length > 4 && System.Text.Encoding.Default.GetString(data, 0, 4) == "RIFF")
        //                        {
        //                            //Debug.Log("Load with header " + System.Text.Encoding.Default.GetString(data, 0, 8));

        //                            SFLoad load = new SFLoad(data, SFFile.SfSource.SF2);
        //                            SoundFontData = load.SfData;
        //                            //Debug.Log("   Preset count:" + SoundFontData.preset.Length);
        //                            //Debug.Log("   Instrument count:" + SoundFontData.inst.Length);
        //                            //Debug.Log("   Sample count:" + SoundFontData.Samples.Length);
        //                            //Debug.Log("   Data length:" + SoundFontData.SampleData.Length);
        //                        }
        //                        else
        //                            Debug.LogWarning("SoundFont not find or not a SoundFont - " + pathInitSF);

        //                    }
        //                    catch (System.Exception ex)
        //                    {
        //                        Debug.LogWarning("LoadSF Error " + ex.Message);
        //                    }
        //                }
        //                else
        //                    Debug.LogWarning("LoadSF network error - " + pathInitSF);
        //            }
        //        }
    }
}
