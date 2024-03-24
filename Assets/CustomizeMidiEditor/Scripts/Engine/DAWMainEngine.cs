using System;
using UnityEngine;
using MidiPlayerTK;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;
using System.Xml.Serialization;
using SimpleFileBrowser;
/*using SFB;*/

// namespace UnityEditor;
namespace DAW
{


    public partial class DAWMainEngine : MonoBehaviour
    {
        [Header("Main components")]
        public DAWUIMgr uiMgr;
        public DAWEventMgr eventMgr;
        public MidiStreamPlayer midiStreamPlayer;
        [Header("Variables")]
        [Range(0, 127)]
        public int CurrentNote;

        [Range(0, 16)]
        public int StreamChannel = 0;

        [Range(0, 16)]
        public int DrumChannel = 9; // by convention the channel 10 is used for playing drum (value = 9 because channel start from channel 0 in Maestro)

        [Range(-10f, 100f)]
        public float CurrentDuration;

        [Range(0, 127)]
        public int CurrentVelocity = 100;

        [Range(0f, 10f)]
        public float CurrentDelay = 0;

        [Range(0, 127)]
        public int CurrentPreset;

        [Range(0, 127)]
        public int CurrentBank;

        [Range(0, 127)]
        public int CurrentPatchDrum;

        private MPTKEvent NotePlaying;

        private PopupListItem PopPatchInstrument;
        private PopupListItem PopBankInstrument;
        private PopupListItem PopPatchDrum;
        private PopupListItem PopBankDrum;

        // Popup to select a realtime generator
        private PopupListItem[] PopGenerator;
        private const int nbrGenerator = 4;
        private string[] labelGenerator;
        private int[] indexGenerator;
        private float[] valueGenerator;

        [Header("Realtime voice parameters mode")]
        public bool RealtimeRelatif;

        [Header("Test MPTK_ChannelPresetChange for changing preset")]
        public bool Test_MPTK_ChannelPresetChange = false;
        private List<int> keysToNote;
        static private List<MPTKEvent> MidiEvents;
        static MPTKEvent KeyPlaying;
        private MidiFileEditorPlayer Player;

        static ContextEditor Context;
        static private MidiEditorLib MidiPlayerSequencer;
        static MPTKWriter MidiFileWriter;
        static long TickQuantization;
        static MPTKEvent LastMidiEvent;
        static MPTKEvent LastNoteOnEvent;

        static SectionAll sectionAll;
        static public MidiEventEditor MidiEventEdit = new MidiEventEditor();

        static long lastTickForUpdate = -1;
        static DateTime lastTimeForUpdate;

        static long CurrentTickPosition = 0;
        static int PositionSequencerPix = 0;

        static MPTKEvent LastEventPlayed = null;

        static public Vector2 ScrollerMidiEvents;
        static MPTKEvent SelectedEvent = null;
        static MPTKEvent NewDragEvent = null;

        static long InitialTick = 0;
        static int InitialDurationTick = 0;
        static int InitialValue = 0;

        bool isStart = true;
        bool isPlay = false;
        /*static List<MPTKEvent> MidiEvents;*/

        // Start is called before the first frame update
        void Start()
        {
            FileBrowser.SetFilters(true, new FileBrowser.Filter("Midi Files", ".mid"));
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar");
            string currentDirectory = Directory.GetCurrentDirectory();
            FileBrowser.AddQuickLink("currentDirectory", currentDirectory, null);

            uiMgr = GetComponent<DAWUIMgr>();
            eventMgr = GetComponent<DAWEventMgr>();
            
            InitVariable();
        }

        void InitVariable() {
            keysToNote = new List<int>();
            indexGenerator = new int[nbrGenerator];
            labelGenerator = new string[nbrGenerator];
            valueGenerator = new float[nbrGenerator];
            PopGenerator = new PopupListItem[nbrGenerator];
            MidiPlayerGlobal.OnEventPresetLoaded.AddListener(EndLoadingSF);

            // Define popup to display to select preset and bank
            PopBankInstrument = new PopupListItem() { Title = "Select A Bank", OnSelect = PopupBankPatchChanged, Tag = "BANK_INST", ColCount = 5, ColWidth = 150, };
            PopPatchInstrument = new PopupListItem() { Title = "Select A Patch", OnSelect = PopupBankPatchChanged, Tag = "PATCH_INST", ColCount = 5, ColWidth = 150, };
            PopBankDrum = new PopupListItem() { Title = "Select A Bank", OnSelect = PopupBankPatchChanged, Tag = "BANK_DRUM", ColCount = 5, ColWidth = 150, };
            PopPatchDrum = new PopupListItem() { Title = "Select A Patch", OnSelect = PopupBankPatchChanged, Tag = "PATCH_DRUM", ColCount = 5, ColWidth = 150, };

            for (int i = 0; i < nbrGenerator; i++)
            {
                indexGenerator[i] = GenModifier.RealTimeGenerator[0].Index;
                labelGenerator[i] = GenModifier.RealTimeGenerator[0].Label;
                if (indexGenerator[i] >= 0)
                    valueGenerator[i] = RealtimeRelatif ? 0f : GenModifier.DefaultNormalizedVal((fluid_gen_type)indexGenerator[i]) * 100f;
                PopGenerator[i] = new PopupListItem() { Title = "Select A Generator", OnSelect = PopupGeneratorChanged, Tag = i, ColCount = 3, ColWidth = 250, };
            }

            DrawKeyboard();
            ShowSequencer();
            SetDetailText();
        }

        private void Awake()
        {
            EditorDeserialize();
        }

        void OnEnable()
        {
            // EditorApplication.playModeStateChanged += ChangePlayModeState;
            // CompilationPipeline.compilationStarted += CompileStarted;
            InitPlayer();
            isStart = true;
        }

        private void OnDisable()
        {
            //Debug.Log($"OnDisable");
             EditorSerialize();
        }

        // Update is called once per frame
        void Update()
        {
            // make the sounds when pressing keyboard
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OpenSaveDialog();
            }

            if (isStart)
            {
                /*DrawKeyboard();
                ShowSequencer();*/
                StartCoroutine(DrawMidiEventsRoutine());
                isStart = false;
            }

            if (!Player.MPTK_IsPlaying && isPlay)
            {
                uiMgr.m_TrackEditPanel.SendMessage("StopPlayLine");
                isPlay = false;
            }

            if (Player.MPTK_IsPlaying && !Player.MPTK_IsPaused)
            {
                // Get the real-time tick value of the MIDI Player
                long tick = Player.MPTK_MidiLoaded.MPTK_TickPlayer;
                if (lastTickForUpdate == tick)
                {
                    if (Player.MPTK_PulseLenght > 0)
                        // No new MIDI event since the rectClear update, extrapolate the current tick from the ellapse time
                        tick = lastTickForUpdate + Convert.ToInt64((DateTime.Now - lastTimeForUpdate).TotalMilliseconds / Player.MPTK_PulseLenght);
                    //Debug.Log($"extrapolate Time.deltaTime:{Time.deltaTime} MPTK_TickPlayer:{Player.midiLoaded.MPTK_TickPlayer} MPTK_Pulse:{Player.MPTK_Pulse:F2} {(Time.deltaTime * 1000d) / Player.MPTK_Pulse:F2} lastTick:{lastTickForUpdate} tick:{tick}");
                }
                else
                {
                    lastTimeForUpdate = DateTime.Now;
                    lastTickForUpdate = tick;
                    //Debug.Log($"real tick Time.deltaTime:{Time.deltaTime} MPTK_TickPlayer:{Player.midiLoaded.MPTK_TickPlayer} MPTK_Pulse:{Player.MPTK_Pulse:F2} lastTick:{lastTickForUpdate} tick:{tick}");
                }

                // Prefer MPTK_PlayTimeTick rather MPTK_TickCurrent to have a smooth display
                //float position = ((float)tick / (float)Player.MPTK_DeltaTicksPerQuarterNote) * Context.QuarterWidth;
                float position = sectionAll.ConvertTickToPosition(tick);

                // Avoid repaint for value bellow 1 pixel
                if ((int)position != PositionSequencerPix)
                {
                    PositionSequencerPix = (int)position;
                    uiMgr.m_TrackEditPanel.SendMessage("DrawPlayLine", position, SendMessageOptions.DontRequireReceiver);
                    //Debug.Log($"Time.deltaTime:{Time.deltaTime} MPTK_TickCurrent:{Player.MPTK_TickCurrent} MPTK_Pulse:{Player.MPTK_Pulse} position:{position}");
                }
            }
        }

        public void OpenLoadDialog() {
            StartCoroutine(ShowLoadDialogCoroutine());
        }

        public void OpenSaveDialog() {
            StartCoroutine(ShowSaveDialogCoroutine());
        }

        public void OpenNew() {
            CreateMidi();
            AddSectionChannel(0);
            LoadContext();
            InitVariable();
        }

        IEnumerator ShowLoadDialogCoroutine()
        {
            // Show a load file dialog and wait for a response from user
            // Load file/folder: file, Allow multiple selection: true
            // Initial path: default (Documents), Initial filename: empty
            // Title: "Load File", Submit button text: "Load"
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Select Files", "Load");

            // Dialog is closed
            // Print whether the user has selected some files or cancelled the operation (FileBrowser.Success)
            Debug.Log(FileBrowser.Success);

            if (FileBrowser.Success)
                OnFilesSelected(FileBrowser.Result); // FileBrowser.Result is null, if FileBrowser.Success is false
        }

        IEnumerator ShowSaveDialogCoroutine()
        {
            // Show a load file dialog and wait for a response from user
            // Load file/folder: file, Allow multiple selection: true
            // Initial path: default (Documents), Initial filename: empty
            // Title: "Load File", Submit button text: "Load"
            yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, null, null, "Select Folder", "Save");

            // Dialog is closed
            // Print whether the user has selected some files or cancelled the operation (FileBrowser.Success)
            Debug.Log(FileBrowser.Success);

            if (FileBrowser.Success)
                OnSaveFileSelected(FileBrowser.Result); // FileBrowser.Result is null, if FileBrowser.Success is false
        }

        void OnFilesSelected(string[] filePaths)
        {
            // Print paths of the selected files
            for (int i = 0; i < filePaths.Length; i++)
                Debug.Log(filePaths[i]);

            // Get the file path of the first selected file
            string filePath = filePaths[0];
            LoadExternalMidiFile(filePath);
            LoadContext();
            InitVariable();
        }

        void OnSaveFileSelected(string[] filePaths) {
            for (int i = 0; i < filePaths.Length; i++)
                Debug.Log(filePaths[i]);

            // Get the file path of the first selected file
            string filePath = filePaths[0];
            if (filePath.Length != 0)
            {
                if (!filePath.Contains(".mid") || !filePath.Contains(".MID")) filePath += ".mid";
                Context.PathOrigin = Path.GetFullPath(filePath);
                Context.MidiName = Path.GetFileNameWithoutExtension(filePath);
                Debug.Log("Write MIDI file:" + filePath);
                MidiFileWriter.WriteToFile(filePath);
                Context.Modified = false;
            }
        }

        private void InitPlayer()
        {
            //Debug.Log("InitPlayer");
            MidiPlayerSequencer = new MidiEditorLib("MidiSequencer", _logSoundFontLoaded: true, _logDebug: false);
            Player = MidiPlayerSequencer.MidiPlayer;
            Player.VerboseSynth = false;
            Player.MPTK_PlayOnStart = false;
            Player.MPTK_StartPlayAtFirstNote = false; // was true
            Player.MPTK_EnableChangeTempo = true;
            Player.MPTK_ApplyRealTimeModulator = true;
            Player.MPTK_ApplyModLfo = true;
            Player.MPTK_ApplyVibLfo = true;
            Player.MPTK_ReleaseSameNote = true;
            Player.MPTK_KillByExclusiveClass = true;
            Player.MPTK_EnablePanChange = true;
            Player.MPTK_KeepPlayingNonLooped = true;
            Player.MPTK_KeepEndTrack = false; // was true
            Player.MPTK_LogEvents = false;
            Player.MPTK_KeepNoteOff = false; // was true
            Player.MPTK_Volume = 0.5f;
            Player.MPTK_Speed = 1f;
            Player.MPTK_InitSynth();

            // not yet available 
            //Player.OnEventStartPlayMidi.AddListener(StartPlay);
            //Player.OnEventNotesMidi.AddListener(MidiReadEvents);
        }

        private void EditorDeserialize()
        {
            //Debug.Log("EditorDeserialize"); 
            try
            {
                string filename = Path.Combine(GetTempFolder(), "_temp_.mid");
                if (File.Exists(filename))
                {
                    Debug.Log("Load temp MIDI file:" + filename);
                    LoadExternalMidiFile(filename);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Can't load temp MIDI file {ex}");
            }

            LoadContext();
        }

        private static void EditorSerialize()
        {
            //Debug.Log("EditorSerialize");

            if (MidiFileWriter != null)
            {
                string filename = Path.Combine(GetTempFolder(), "_temp_.mid");
                //Debug.Log("Save temp MIDI file:" + filename);
                MidiFileWriter.WriteToFile(filename);

                string path = Path.Combine(GetTempFolder(), "context.xml");
                var serializer = new XmlSerializer(typeof(ContextEditor));
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    serializer.Serialize(stream, Context);
                }

                //string json = JsonUtility.ToJson(MidiName);
            }
        }

        private void LoadExternalMidiFile(string midifile)
        {
            // check with CheckMidiSaved already done
            MidiLoad midiLoader = new MidiLoad();
            try
            {
                midiLoader.MPTK_EnableChangeTempo = true;
                midiLoader.MPTK_KeepEndTrack = false;
                midiLoader.MPTK_KeepNoteOff = false;
                midiLoader.MPTK_LoadFile(midifile);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error when loading MIDI file {ex}");
            }
            if (midiLoader != null)
            {
                // Context can be null if loaded from awake
                if (Context == null)
                    Context = new ContextEditor();
                Context.MidiName = Path.GetFileNameWithoutExtension(midifile);
                Context.Modified = false;

                ImportToMidiFileWriter(midiLoader);
                Context.SetSectionOpen(true);
                Context.SetSectionMute(false);
                ResetCurrent();
                
            }
            try
            {
                // if (window != null)
                //     window.Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error when Repaint {ex}");
            }

        }

        private void ImportToMidiFileWriter(MidiLoad midiLoader)
        {
            //Debug.Log($"ImportToMidiFileWriter midiLoader count:{midiLoader.MPTK_MidiEvents.Count} windowID:{window?.GetInstanceID()} or null");
            try
            {
                MidiFileWriter = new MPTKWriter();
                //MidiFileWriter.MPTK_NumberBeatsMeasure = midiLoader.MPTK_NumberBeatsMeasure;
                //MidiFileWriter.MPTK_NumberQuarterBeat = midiLoader.MPTK_NumberQuarterBeat;
                MidiFileWriter.ImportFromEventsList(midiLoader.MPTK_MidiEvents, midiLoader.MPTK_DeltaTicksPerQuarterNote,
                    name: Context.MidiName, position: 0, logDebug: false, logPerf: false);
                MidiFileWriter.CreateTracksStat();
                MidiFileWriter.CalculateTiming();
                MidiEvents = MidiFileWriter.MPTK_MidiEvents;
                SelectedEvent = null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error when importing MIDI file {ex}");
            }
            //Debug.Log($"ImportToMidiFileWriter {Context.MidiName} {MidiEvents.Count}");

            if (MidiEvents != null)
            {
                CalculRatioQuantization();
                FindLastMidiEvents();
                sectionAll = new SectionAll(MidiFileWriter);
                sectionAll.InitSections();
                MidiEventEdit.Length = MidiFileWriter.DeltaTicksPerQuarterNote;
                InitPosition();
                if (MidiPlayerSequencer != null && Player != null && Player.MPTK_IsPlaying)
                    PlayMidiFileSelected();
                //if (MidiEvents != null && MidiEvents.Count > 0)
                //Player.MPTK_Play(mfw2: MidiFileWriter, fromTick: Context.LoopResume, toTick: Context.LoopEnd, timePosition: false);
            }
        }

        private void PlayMidiFileSelected()
        {
            if (MidiPlayerGlobal.MPTK_SoundFontIsReady && MidiEvents != null && MidiEvents.Count > 0)
            {
                try
                {
                    MidiPlayerSequencer.PlayAudioSource();
                    InitPosition();

                    Player.MPTK_EffectSoundFont.EnableChorus = false;
                    Player.MPTK_EffectSoundFont.EnableFilter = false;
                    Player.MPTK_EffectSoundFont.EnableReverb = false;
                    Player.MPTK_EffectUnity.EnableChorus = false;
                    Player.MPTK_EffectUnity.EnableReverb = false;

                    if (Player.OnEventStartPlayMidi == null) Player.OnEventStartPlayMidi = new EventStartMidiClass();
                    if (Player.OnEventNotesMidi == null) Player.OnEventNotesMidi = new EventNotesMidiClass();
                    if (Player.OnEventEndPlayMidi == null) Player.OnEventEndPlayMidi = new EventEndMidiClass();

                    Player.OnEventStartPlayMidi.RemoveAllListeners();
                    Player.OnEventNotesMidi.RemoveAllListeners();
                    Player.OnEventEndPlayMidi.RemoveAllListeners();

                    Player.OnEventStartPlayMidi.AddListener(StartPlay);
                    Player.OnEventNotesMidi.AddListener(MidiReadEvents);
                    Player.OnEventEndPlayMidi.AddListener(EndPlay);

                    Player.MPTK_Stop();

                    if (MidiEvents != null && MidiEvents.Count > 0)
                    {
                        SetInnerLoopFromContext();
                        Player.MPTK_Play(mfw2: MidiFileWriter, timePosition: false);
                    }
                    else
                        Debug.Log("Nothing to play ...");
                }
                catch (Exception ex)
                {
                    throw new MaestroException($"PlayMidiFileSelected error.{ex.Message}");
                }
            }
        }

        public void StartPlay(string midiname)
        {
            //Debug.Log($"StartPlay {midiname}  {MidiPlayerSequencer.MidiPlayer.MPTK_TickCurrent}");
            SetMuteChannel();

        }

        private void SetInnerLoopFromContext()
        {
            if (Player != null)
            {
                Player.MPTK_InnerLoop.Enabled = Context.LoopEnabled;
                Player.MPTK_InnerLoop.Start = Player.MPTK_InnerLoop.Resume = Context.LoopResume;
                Player.MPTK_InnerLoop.End = Context.LoopEnd;
                Player.MPTK_InnerLoop.Finished = false;
                Player.MPTK_InnerLoop.Max = 0;
                Player.MPTK_InnerLoop.Count = 0;
                Player.MPTK_InnerLoop.Log = false;
                if (Player.MPTK_InnerLoop.Enabled)
                {
                    Debug.Log($"Inner Loop Enabled - From: {Player.MPTK_InnerLoop.Resume} To: {Player.MPTK_InnerLoop.End} ");
                }
                else
                    Debug.Log("Inner Loop Disabled");
            }
        }

        public void MidiReadEvents(List<MPTKEvent> midiEvents)
        {
            try
            {
                if (Context.LogEvents)
                    midiEvents.ForEach(midiEvent => Debug.Log(midiEvent.ToString()));

                LastEventPlayed = midiEvents[midiEvents.Count - 1];
                //midiEvents.ForEach(midiEvent =>
                //{ 
                //    if (midiEvent.Command== MPTKCommand.MetaEvent && midiEvent.Meta == MPTKMeta.SetTempo) 
                //        Player.SetTimeMidiFromStartPlay = midiEvent.RealTime; 
                //});


                //if (FollowEvent)
                {

                    //Debug.Log($"{scrollerMidiPlayer}");
                    // window.Repaint();
                }
            }
            catch (Exception ex)
            {
                throw new MaestroException($"MidiReadEvents.{ex.Message}");
            }
        }

        private void SetMuteChannel()
        {
            for (int channel = 0; channel < 16; channel++)
                Player.MPTK_Channels[channel].Enable = !Context.SectionMute[channel];
        }

        static public void EndPlay(string midiname, EventEndMidiEnum reason)
        {
            //Debug.Log($"EndPlay {midiname} {reason} {MidiPlayerSequencer.MidiPlayer.MPTK_TickCurrent}");
        }


        private void PopupBankPatchChanged(object tag, int index, int indexList)
        {
            //Debug.Log($"Bank or Patch Change {tag} {index} {indexList}");

            switch ((string)tag)
            {
                case "BANK_INST":
                    CurrentBank = index;
                    // This method build the preset list for the selected bank.
                    // This call doesn't change the MIDI bank used to play an instrument.
                    MidiPlayerGlobal.MPTK_SelectBankInstrument(index);
                    if (Test_MPTK_ChannelPresetChange)
                    {
                        // Before v2.10.1
                        // Change the bank number but not the preset, we need to retrieve the current preset for this channel
                        // int currentPresetInst = midiStreamPlayer.MPTK_ChannelPresetGetIndex(StreamChannel);
                        // Change the bank but not the preset. Return false if the preset is not found.
                        // ret = midiStreamPlayer.MPTK_ChannelPresetChange(StreamChannel, currentPresetInst, index);

                        // From v2.10.1
                        midiStreamPlayer.MPTK_Channels[StreamChannel].BankNum = index;
                    }
                    else
                        // Change bank withe the standard MIDI message
                        midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.BankSelectMsb, Value = index, Channel = StreamChannel, });

                    Debug.Log($"Instrument Bank change - channel:{StreamChannel} bank:{midiStreamPlayer.MPTK_Channels[StreamChannel].BankNum} preset:{midiStreamPlayer.MPTK_Channels[StreamChannel].PresetNum}");
                    break;

                case "PATCH_INST":
                    CurrentPreset = index;
                    if (Test_MPTK_ChannelPresetChange)
                    {
                        // Before v2.10.1
                        // Change the preset number but not the bank. Return false if the preset is not found.
                        // ret = midiStreamPlayer.MPTK_ChannelPresetChange(StreamChannel, index);

                        // From v2.10.1
                        midiStreamPlayer.MPTK_Channels[StreamChannel].PresetNum = index;
                    }
                    else
                        midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.PatchChange, Value = index, Channel = StreamChannel, });

                    Debug.Log($"Instrument Preset change - channel:{StreamChannel} bank:{midiStreamPlayer.MPTK_Channels[StreamChannel].BankNum} preset:{midiStreamPlayer.MPTK_Channels[StreamChannel].PresetNum}");
                    break;

                case "BANK_DRUM":
                    // This method build the preset list for the selected bank.
                    // This call doesn't change the MIDI bank used to play an instrument.
                    MidiPlayerGlobal.MPTK_SelectBankDrum(index);
                    if (Test_MPTK_ChannelPresetChange)
                        // From v2.10.1
                        midiStreamPlayer.MPTK_Channels[DrumChannel].BankNum = index;
                    else
                        // Change bank with the standard MIDI message
                        midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.ControlChange, Controller = MPTKController.BankSelectMsb, Value = index, Channel = DrumChannel, });

                    Debug.Log($"Drum Bank change - channel:{DrumChannel} bank:{midiStreamPlayer.MPTK_Channels[DrumChannel].BankNum} preset:{midiStreamPlayer.MPTK_Channels[DrumChannel].PresetNum}");
                    break;

                case "PATCH_DRUM":
                    CurrentPatchDrum = index;
                    if (Test_MPTK_ChannelPresetChange)
                        // From v2.10.1
                        midiStreamPlayer.MPTK_Channels[DrumChannel].PresetNum = index;
                    else
                        midiStreamPlayer.MPTK_PlayEvent(new MPTKEvent() { Command = MPTKCommand.PatchChange, Value = index, Channel = DrumChannel });

                    Debug.Log($"Drum Preset change - channel:{DrumChannel} bank:{midiStreamPlayer.MPTK_Channels[DrumChannel].BankNum} preset:{midiStreamPlayer.MPTK_Channels[DrumChannel].PresetNum}");
                    break;
            }
        }
        private void PopupGeneratorChanged(object tag, int index, int indexList)
        {
            int iGenerator = Convert.ToInt32(tag);
            indexGenerator[iGenerator] = index;
            labelGenerator[iGenerator] = GenModifier.RealTimeGenerator[indexList].Label;
            valueGenerator[iGenerator] = RealtimeRelatif ? 0f : GenModifier.DefaultNormalizedVal((fluid_gen_type)indexGenerator[iGenerator]) * 100f;
            Debug.Log($"indexList:{indexList} indexGenerator:{indexGenerator[iGenerator]} valueGenerator:{valueGenerator[iGenerator]} {labelGenerator[iGenerator]}");
        }

        public void EndLoadingSF()
        {/*
            Debug.Log("End loading SoundFont. Statistics: ");

            //Debug.Log("List of presets available");
            //foreach (MPTKListItem preset in MidiPlayerGlobal.MPTK_ListPreset)
            //    Debug.Log($"   [{preset.Index,3:000}] - {preset.Label}");

            Debug.Log("   Time To Load SoundFont: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadSoundFont.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Time To Load Samples: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadWave.TotalSeconds, 3).ToString() + " second");
            Debug.Log("   Presets Loaded: " + MidiPlayerGlobal.MPTK_CountPresetLoaded);
            Debug.Log("   Samples Loaded: " + MidiPlayerGlobal.MPTK_CountWaveLoaded);
            */
        }

        public void MaestroPlayOneNote(int note)
        {
            //===Test
            CurrentNote = note;
            //===Test End
            //Debug.Log($"{StreamChannel} {midiStreamPlayer.MPTK_ChannelPresetGetName(StreamChannel)}");
            // Start playing a new note
            NotePlaying = new MPTKEvent()
            {
                Command = MPTKCommand.NoteOn,
                Value = CurrentNote, // note to played, ex 60=C5. Use the method from class HelperNoteLabel to convert to string
                Channel = StreamChannel,
                Duration = Convert.ToInt64(CurrentDuration * 1000f), // millisecond, -1 to play indefinitely
                Velocity = CurrentVelocity, // Sound can vary depending on the velocity
                Delay = Convert.ToInt64(CurrentDelay * 1000f),
            };

            // #if MPTK_PRO
            // Applied to the current note playing all the real time generators defined
            for (int i = 0; i < nbrGenerator; i++)
                if (indexGenerator[i] >= 0)
                    NotePlaying.ModifySynthParameter((fluid_gen_type)indexGenerator[i], valueGenerator[i] / 100f, MPTKModeGeneratorChange.Override);
            // #endif
            midiStreamPlayer.MPTK_PlayEvent(NotePlaying);
        }

        public void InsertDownKey(int note)
        {
            Debug.Log($"InsertDownKey note is {note}");
            keysToNote.Add(note);
            if (keysToNote.Count > 0)
            {
                for (int key = 0; key < keysToNote.Count; key++)
                {
                    // Create a new note and play
                    MidiEvents.Add(new MPTKEvent()
                    {
                        Command = MPTKCommand.NoteOn,
                        Channel = StreamChannel, // From 0 to 15
                        Duration = -1, // Infinite, note-off when key is released, see bellow.
                        Value = keysToNote[key], // blues en C minor: C,D#,F,F#,G,A# http://patrick.murris.com/musique/gammes_piano.htm?base=3&scale=0%2C3%2C5%2C6%2C7%2C10&octaves=1
                        Velocity = 100
                    });

                    // Send the note-on MIDI event to the MIDI synth
                    midiStreamPlayer.MPTK_PlayEvent(MidiEvents[key]);
                }
            }
            else
            {
                if (MidiEvents.Count > 0) MidiEvents = new List<MPTKEvent>();
            }
        }

        public void RemoveUpKey(int note)
        {
            Debug.Log($"RemoveUpKey note is {note}");
            if (keysToNote.Count == 0) return;
            for (int key = 0; key < keysToNote.Count; key++)
            {
                if (keysToNote[key] == note)
                {
                    midiStreamPlayer.MPTK_StopEvent(MidiEvents[key]);
                    MidiEvents.Remove(MidiEvents[key]);
                    keysToNote.Remove(note);
                    break;
                }
            }
        }

        public void DrawMeasureLine(bool isFirst = false)
        {
            TrackEditPanel trackEditUI = uiMgr.m_TrackEditPanel.GetComponent<TrackEditPanel>();
            if (!isFirst)
            { 
                trackEditUI.ClearMeasureLine();
                trackEditUI.ClearStickLine();
            }
            /*if (!trackEditUI) return;*/
            if (Context.QuarterWidth > 1f) // To avoid infinite loop
            {
                float xStart, xEnd = 0f;
                (xStart, xEnd) = Global.GetMeasureLineRect();
                int quarter = 0;
                int measure = 1;
                int quarterInBar = 1;
                int indexSign = 0;
                int auxilaryLineInBar = 0;

                int modeDisplayTime;
                if (Context.QuarterWidth > 30)
                    // Draw all time
                    modeDisplayTime = 0;
                else if (Context.QuarterWidth > 20)
                    // Draw only at measure
                    modeDisplayTime = 1;
                else if (Context.QuarterWidth > 10)
                    // Draw only at even measure
                    modeDisplayTime = 2;
                else
                    // disable draw 
                    modeDisplayTime = -1;
                float trackWidth = Global.GetTrackWidth();
                float fullWidthSections = SectionChannel.FullWidthSections < 1800f ? 1800 : SectionChannel.FullWidthSections;
                for (float xQuarter = 0; xQuarter <= fullWidthSections; xQuarter += Context.QuarterWidth)
                {
                    long tick = sectionAll.ConvertPositionToTick(xQuarter);
                    tick = (long)((tick / (float)MidiFileWriter.DeltaTicksPerQuarterNote) + 0.5f) * MidiFileWriter.DeltaTicksPerQuarterNote;
                    indexSign = MPTKSignature.FindSegment(MidiFileWriter.MPTK_SignMap, tick, fromIndex: indexSign);
                    MPTKSignature signMap = MidiFileWriter.MPTK_SignMap[indexSign];
                    int numberBeatsMeasure = signMap.NumberBeatsMeasure * 4 / signMap.NumberQuarterBeat;
                    float measureWidth = Context.QuarterWidth * numberBeatsMeasure;
                    //Debug.Log($"xQuarter:{xQuarter} measure:{measure} quarterInBar:{quarterInBar} quarter:{quarter}");

                    // Draw only on visible area, max width displayed + one measure to avoid cut too early
                    string tip = $"Tick:{tick}\nMeasure:{measure}\nQuarterInBar:{quarterInBar}\n";
                    tip += $"BeatsMeasure:{numberBeatsMeasure}\nStart:{signMap.FromMeasure}\nEnd:{signMap.ToMeasure}";
#if DEBUG_EDITOR
                                {
                                    tip += $"\nIndexTempo:{indexSign}\nQuarterWidth:{Context.QuarterWidth}\n";
                                    GUIContent content = new GUIContent("12.3", tip);
                                    content.tooltip += $"Size xx.x:{MeasurelineStyle.CalcSize(content).x}";
                                }
#endif
                    // Draw Measure & quarter label
                    // ----------------------------
                    if (xQuarter > xStart - Context.QuarterWidth && xQuarter < xEnd)
                    {
                        for (float xAuxilaryLine = xQuarter; xAuxilaryLine <= xQuarter + Context.QuarterWidth; xAuxilaryLine += Context.QuarterWidth / 4f)
                        {
                            if (auxilaryLineInBar > 0)
                            {
                                trackEditUI.DrawMeasureLines(xAuxilaryLine - xStart, CommonClasses.MeasureLineType.AUXILARY);
                            }
                            auxilaryLineInBar++;
                            if (auxilaryLineInBar == 4) { auxilaryLineInBar = 0; break; }
                        }
                        if (Context.QuarterWidth > 30f)
                        {
                            // each quarter in measure
                            /* Rect rect = new Rect(separatorQuarterRect.x, 0, Context.QuarterWidth, height / 2f);
                             GUIContent content = new GUIContent(measure.ToString() + "." + quarterInBar.ToString(), tip);
                             GUI.Label(rect, content, MeasurelineStyle);*/

                            if (quarterInBar == 1)
                            {
                                trackEditUI.DrawMeasureLines(xQuarter - xStart, CommonClasses.MeasureLineType.MEASURE);
                                string str = $"{measure}.{quarterInBar}\n{sectionAll.ConvertPositionToTick(xQuarter)}";
                                trackEditUI.DrawMeasureStick(xQuarter - xStart, CommonClasses.MeasureLineType.MEASURE, str);
                            }

                            else { trackEditUI.DrawMeasureLines(xQuarter - xStart, CommonClasses.MeasureLineType.QUARTER);
                                string str = $"{measure}.{quarterInBar}\n{sectionAll.ConvertPositionToTick(xQuarter)}";
                                trackEditUI.DrawMeasureStick(xQuarter - xStart, CommonClasses.MeasureLineType.QUARTER, str);
                            }
                        }
                        else if (Context.QuarterWidth > 2f)
                        {
                            if (quarterInBar == 1)
                            {
                                // each measure
                                /*Rect rect = new Rect(separatorQuarterRect.x, 0, Context.QuarterWidth * numberBeatsMeasure, height / 2f);
                                GUIContent content = new GUIContent(measure.ToString(), tip);
                                GUI.Label(rect, content, MeasurelineStyle);*/
                            }
                        }

                        // Draw timeline
                        // -------------

                        // Able to draw at each quarter or each modeDisplayTime measure ?
                        if (modeDisplayTime >= 0)
                            if (modeDisplayTime == 0 ||
                                (modeDisplayTime == 1 && quarterInBar == 1) ||
                                (modeDisplayTime == 2 && quarterInBar == 1 && measure % 2 == 0)
                                )
                            {
                                /*Rect timeQuarterRect = new Rect((int)(xQuarter - ScrollerMidiEvents.x - Context.QuarterWidth / 2f), height / 2f, Context.QuarterWidth, height / 2f);
                                GUIContent content = BuildTextTime(quarter);
                                if (content.text != null && timeQuarterRect.x > 0f)
                                    GUI.Label(timeQuarterRect, content, TimelineStyle);*/
                            }
                    }

                    if (quarterInBar >= numberBeatsMeasure)
                    {
                        measure++;
                        quarterInBar = 1;
                    }
                    else
                        quarterInBar++;
                    quarter++;
                }
            }
        }
        private void CreateMidi()
        {
            /*if (CheckMidiSaved())
            {*/
            if (MidiPlayerSequencer != null && Player != null && Player.MPTK_IsPlaying)
                Player.MPTK_Stop();
            //! [ExampleInitMidiFileWriter]
            MidiFileWriter = new MPTKWriter(deltaTicksPerQuarterNote: 500, bpm: 120);
            MidiFileWriter.AddTempoChange(0, 0, MPTKEvent.BeatPerMinute2QuarterPerMicroSecond(MidiFileWriter.CurrentTempo));
            MidiFileWriter.AddTimeSignature(0, 0, numerator: 4, denominator: 2, ticksInMetronomeClick: 24);
            MPTKTempo.CalculateMap(MidiFileWriter.DeltaTicksPerQuarterNote, MidiFileWriter.MPTK_MidiEvents, MidiFileWriter.MPTK_TempoMap);
            MPTKSignature.CalculateMap(MidiFileWriter.DeltaTicksPerQuarterNote, MidiFileWriter.MPTK_MidiEvents, MidiFileWriter.MPTK_SignMap);
            MPTKSignature.CalculateMeasureBoundaries(MidiFileWriter.MPTK_SignMap);
            //! [ExampleInitMidiFileWriter]
            MidiEvents = MidiFileWriter.MPTK_MidiEvents;
            Context.MidiName = "no name";
            Context.PathOrigin = "";
            CalculRatioQuantization();
            FindLastMidiEvents();
            sectionAll = new SectionAll(MidiFileWriter);
            sectionAll.InitSections();
            AddSectionMeta();
            MidiEventEdit.Length = MidiFileWriter.DeltaTicksPerQuarterNote;
            InitPosition();
            Context.Modified = false;
            Context.SetSectionOpen(true);
            Context.SetSectionMute(false);
            Context.LoopEnabled = false;
            Context.LoopResume = Context.LoopEnd = 0;
            ResetCurrent();
            /*}*/
        }

        private void ResetCurrent()
        {
            //CurrentTrack = 0;
            MidiEventEdit.Channel = 0;
            MidiEventEdit.Command = 0;
            MidiEventEdit.Tick = 0;
            MidiEventEdit.Note = 60;
            MidiEventEdit.Length = MidiFileWriter != null ? MidiFileWriter.DeltaTicksPerQuarterNote : 500;
            MidiEventEdit.Velocity = 100;
            MidiEventEdit.Preset = 0;
            MidiEventEdit.TempoBpm = 120;
            MidiEventEdit.Text = "";
            SelectedEvent = null;
            /*if (PopupSelectMidiChannel != null)
                PopupSelectMidiChannel.SelectedIndex = MidiEventEdit.Channel;
            if (PopupSelectMidiCommand != null)
                PopupSelectMidiCommand.SelectedIndex = MidiEventEdit.Command;*/
        }

        private bool CheckMidiSaved()
        {
            if (!Context.Modified)
                return true;
            else
            {
                if (uiMgr == null) return false;
                uiMgr.m_ConfirmPanel.GetComponent<ConfirmPanel>().InitPanel(null, "", "Would you save it now?");
                // if (EditorUtility.DisplayDialogComplex("MIDI not saved", "This MIDI sequence has not beed saved, if you continue change will be lost", "Close without saving", "Cancel", "") == 0)
                // return true;
            }
            return false;
        }

        private void CalculRatioQuantization()
        {
            if (MidiFileWriter != null)
            {
                if (Context.IndexQuantization == 0) // none
                    TickQuantization = 0;
                else if (Context.IndexQuantization == 1) // whole
                    TickQuantization = MidiFileWriter.DeltaTicksPerQuarterNote * 4;
                else if (Context.IndexQuantization == 2) // half
                    TickQuantization = MidiFileWriter.DeltaTicksPerQuarterNote * 2;
                else // quarter and bellow
                    TickQuantization = MidiFileWriter.DeltaTicksPerQuarterNote / (1 << (Context.IndexQuantization - 3)); // division par puissance de 2

                //Debug.Log($"IndexQuantization:{Context.IndexQuantization} DeltaTicksPerQuarterNote:{MidiFileWriter.MPTK_DeltaTicksPerQuarterNote} Quantization:{TickQuantization}");
            }
        }

        private void FindLastMidiEvents()
        {
            LastMidiEvent = null;
            LastNoteOnEvent = null;
            if (MidiEvents != null && MidiEvents.Count > 0)
            {
                LastMidiEvent = MidiEvents[MidiEvents.Count - 1];
                if (LastMidiEvent.Command == MPTKCommand.NoteOn)
                    LastNoteOnEvent = LastMidiEvent;
                else
                {
                    for (int i = MidiEvents.Count - 1; i >= 0; i--)
                        if (MidiEvents[i].Command == MPTKCommand.NoteOn)
                        {
                            LastNoteOnEvent = MidiEvents[i];
                            break;
                        }
                }
            }
        }

        private bool AddSectionMeta()
        {
            try
            {
                //Debug.Log($"Add iSection {iSection}");
                if (sectionAll.SectionExist(SectionAll.SECTION_META))
                    return false;

                // Add a iSection, with a default preset and lownote=60 and highnote=65
                sectionAll.AddSection(SectionAll.SECTION_META);
                Context.Modified = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AddChannelMeta {ex}");
            }
            return true;
        }

        void InitPosition(bool keepSequencerPosition = false)
        {
            lastTickForUpdate = -1;
            lastTimeForUpdate = DateTime.Now;
            if (!keepSequencerPosition)
            {
                CurrentTickPosition = 0;
                PositionSequencerPix = 0;
            }
            LastEventPlayed = null;
            ScrollerMidiEvents = Vector2.zero;
        }

        void LoadContext()
        {
            //Debug.Log($"LoadContext");
            try
            {
                string path = Path.Combine(GetTempFolder(), "context.xml");
                if (File.Exists(path))
                {
                    var serializer = new XmlSerializer(typeof(ContextEditor));
                    using (var stream = new FileStream(path, FileMode.Open))
                    {
                        Context = serializer.Deserialize(stream) as ContextEditor;
                        Global._xNote16th = Context.QuarterWidth / 4f;
                        if (Context.QuarterWidth < 2f)
                            Context.QuarterWidth = 50f;
                    }
                }
                else
                {
                    Context = new ContextEditor();
                }
                SelectPopupItemFromContext();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Can't load MIDI context " + e.ToString());
                Context = new ContextEditor();
            }
        }

        private void SelectPopupItemFromContext()
        {
            /*if (PopupItemsDisplayTime != null)
                PopupItemsDisplayTime.ForEach(item => { item.Selected = item.Value == Context.DisplayTime; });
            if (PopupSelectDisplayTime != null)
                PopupSelectDisplayTime.SelectedIndex = Context.DisplayTime;

            //Debug.Log($"SelectPopupItemFromContext IndexQuantization:{Context.IndexQuantization}");
            if (PopupItemsQuantization != null)
                PopupItemsQuantization.ForEach(item => { item.Selected = item.Value == Context.IndexQuantization; });
            if (PopupSelectQuantization != null)
                PopupSelectQuantization.SelectedIndex = Context.IndexQuantization;*/

            CalculRatioQuantization();
        }

        static string GetTempFolder()
        {
            string folder = Path.Combine(Application.persistentDataPath, "MaestroMidiEditorTemp");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

        private void ShowSequencer(/*float startx, float starty, float width, float height*/)
        {
            try // Begin area MIDI events list
            {
                //if (MidiEvents.Count == 0)
                //    // Draw background MIDI events
                //    GUI.Box(new Rect(startx, starty, width, heightFirstRowCmd), "", BackgroundMidiEvents); // MPTKGui.stylePanelGrayLight
                //

                Global._maxCellIndex = MidiEvents.Count - 1;
                if (MidiEvents.Count > 0)
                {
                    /*if (winPopupSynth != null)
                        winPopupSynth.Repaint();*/

                    //DisplayPerf(null, true);

                    sectionAll.CalculateSizeAllSections(LastMidiEvent, Context.QuarterWidth, Context.CellHeight);
                    uiMgr.m_TrackEditPanel.SendMessage("SetTrackWidth", SectionChannel.FullWidthSections);
                    StartCoroutine(DrawMidiEventsRoutine());
                }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
        }

        IEnumerator DrawMidiEventsRoutine() {
            yield return new WaitForSeconds(0.05f); // Delay for 2 seconds
            DrawMidiEvents();
        }

        private void DrawMidiEvents(/*int indexSection, float startXEventsList, float startYLinePosition, float startYEventsList, float widthVisibleEventsList, float heightVisibleEventsList*/)
        {
            // Foreach MIDI events on the current page
            // ---------------------------------------
            foreach (SectionCore section in sectionAll.Sections)
            {
                if (section != null /*&& (indexSection == -1 || indexSection == section.IndexSection)*/ && Context.SectionOpen[section.IndexSection])
                {
                    if (section.IndexSection == SectionAll.SECTION_META)
                    {
                        // For each MIDI event filter by section
                        foreach (MPTKEvent midiEvent in MidiEvents)
                        {
                            if (midiEvent.Command == MPTKCommand.MetaEvent)
                            {

                                if (!DrawOneMidiEvent(section, midiEvent))
                                {
                                    //Debug.Log($"Outside the visible area, iSection {iSection}");
                                    break;
                                }

                            }
                        }
                    }
                    else
                        // For each MIDI event filter by section
                        foreach (MPTKEvent midiEvent in MidiEvents)
                        {
                            if (midiEvent.Channel == section.IndexSection && midiEvent.Command <= MPTKCommand.PitchWheelChange)
                            {
                                // Channel command to be draw in this section

                                if (!DrawOneMidiEvent(section, midiEvent))
                                {
                                    //Debug.Log($"Outside the visible area, iSection {iSection}");
                                    break;
                                }

                            }
                        }
                }
            }
            //DisplayPerf("DrawMidiEvents");
        }

        private bool DrawOneMidiEvent(/*float startXEventsList, float startYEventsList, float widthVisibleEventsList, float heightVisibleEventsList,*/
            SectionCore channelMidi, MPTKEvent midiEvent/*, AreaUI channelSubZone*/)
        {
            int index = midiEvent.Index;
            string cellText = "";
            float cellX = sectionAll.ConvertTickToPosition(midiEvent.Tick);
            float cellY;
            float cellW;
            float cellH = Context.CellHeight - 4f;

            /*if (cellX > ScrollerMidiEvents.x + widthVisibleEventsList)
            {
                // After the visible area, stop drawing all next iSection's notes
                //Debug.Log($"After the visible area, stop drawing all iSection's notes. IndexSection {midiEvent.IndexSection} Tick:{midiEvent.Tick} ScrollerMidiEvents.x:{ScrollerMidiEvents.x} width:{widthVisibleEventsList} cellX:{cellX}");
                return false;
            }*/
            /*Texture eventTexture = MidiNoteTexture; // default style*/
            switch (midiEvent.Command)
            {
                case MPTKCommand.NoteOn:
                    cellW = sectionAll.ConvertTickToPosition(midiEvent.Length);
                    if (midiEvent.Value < channelMidi.Layouts[(int)Layout.EnumType.Note].Lower ||
                        midiEvent.Value > channelMidi.Layouts[(int)Layout.EnumType.Note].Higher) return true;
                    cellY = channelMidi.Layouts[(int)Layout.EnumType.Note].BegY + (channelMidi.Layouts[(int)Layout.EnumType.Note].Higher - midiEvent.Value) * Context.CellHeight;
                    if (cellH >= 6f && cellW >= 20f)
                    {
                        cellText = HelperNoteLabel.LabelC4FromMidi(midiEvent.Value);
                        if (cellW >= 40f)
                            cellText += " N:" + midiEvent.Value.ToString();
                        if (cellW >= 70f)
                            cellText += " V:" + midiEvent.Velocity.ToString();
                        else if (cellH >= 19f)
                            cellText += "\nV:" + midiEvent.Velocity.ToString();
                    }
                    break;
                case MPTKCommand.PatchChange:
                    cellW = Context.QuarterWidth / 4f;
                    // -1 for alignment with the button 
                    cellY = channelMidi.Layouts[(int)Layout.EnumType.Preset].BegY - 1f + ((SectionChannel)channelMidi).GetPresetLine(midiEvent.Value) * Context.CellHeight;
                    if (cellW >= 20f)
                        cellText = $"{midiEvent.Value}";
                    break;
                case MPTKCommand.NoteOff: return true;
                case MPTKCommand.ControlChange: return true;
                case MPTKCommand.MetaEvent:
                    cellW = Context.QuarterWidth / 4f;
                    int line = -1;
                    switch (midiEvent.Meta)
                    {
                        case MPTKMeta.SetTempo:
                            line = 0;
                            break;
                        case MPTKMeta.TextEvent:
                            line = 1;
                            break;
                    }
                    if (line < 0) return true;
                    cellY = channelMidi.Layouts[0].BegY - 1f + line * Context.CellHeight;
                    if (cellW >= 20f)
                        cellText = $"{midiEvent.Value}";
                    break; ;
                case MPTKCommand.ChannelAfterTouch: return true;
                case MPTKCommand.KeyAfterTouch: return true;
                default: return true;
            }

            /*if (midiEvent == SelectedEvent)*/

            //Debug.Log($"ScrollerMidiEvents:{ScrollerMidiEvents}  width:{widthVisibleEventsList} startYEventsList:{startYEventsList}  heightVisible:{heightVisibleEventsList} cellX:{cellX} cellY:{cellY} cellW:{cellW}");

            /*if (cellX + cellW < ScrollerMidiEvents.x)
            {
                // Before the visible area, go to next event
                //Debug.Log($"   Before the visible area, go to next event. IndexSection {midiEvent.IndexSection} Tick:{midiEvent.Tick} ScrollerMidiEvents.x:{ScrollerMidiEvents.x} width:{widthVisibleEventsList} cellX:{cellX}");
                return true;
            }*/

            /*if (cellY + Context.CellHeight < ScrollerMidiEvents.y)
            {
                // Above the visible area, go to next event
                //Debug.Log($"   Above the visible area, go to next event. IndexSection {midiEvent.IndexSection} cellY:{cellY} ScrollerMidiEvents.y:{ScrollerMidiEvents.y}  heightFirstRowCmd:{heightVisibleEventsList} CellHeight:{Sectx.CellHeight}");
                return true;
            }*/

            // Minimum width to be able to select a MIDI event
            if (cellW < 12f) cellW = 12f;
            uiMgr.m_TrackEditPanel.GetComponent<TrackEditPanel>().DrawOneMidiNoteCell(midiEvent.Index, midiEvent.Value, cellX, cellW);
            /*Rect cellRect = new Rect(cellX, cellY + 3f, cellW, cellH);*/
            //Debug.Log($"cellRect {cellRect} {eventTexture}");

            /*GUI.DrawTexture(cellRect, eventTexture);
            GUI.DrawTexture(cellRect, eventTexture, ScaleMode.StretchToFill, false, 0f, Color.gray, borderWidth: 1f, borderRadius: 0f);*/

            /*if (cellText.Length > 0)
                GUI.Label(cellRect, cellText, MPTKGui.LabelCenterSmall);*/

            /*AreaUI zoneCell = new AreaUI() { midiEvent = midiEvent, Position = cellRect, };*/
            // Shift cell position from scroller area to absolute position
            /*zoneCell.Position.x += startXEventsList - ScrollerMidiEvents.x;
            zoneCell.Position.y += startYEventsList - ScrollerMidiEvents.y;*/
            // Add cell with note to this area
            //Debug.Log("zoneCell " + zoneCell);
            /*channelSubZone.SubArea.Add(zoneCell);*/

            return true;
        }

        private void DrawKeyboard()
        {
            const float HEIGHT_LIMITE = 11f;
            SectionCore temp = null;

            //Color savedColor = GUI.color;
            try // try keyboard
            {
                Array.ForEach(sectionAll.Sections, section =>
                {
                    if (section != null)
                    {
                        if (section is SectionMeta)
                        {
                            /*SectionMeta sectionMeta = (SectionMeta)section;
                            //
                            // Draw meta area and section name
                            // -------------------------------
                            if (isSharp == 0) // only one time !
                            {
                                // Draw Section name
                                GUI.Label(new Rect(0, section.BegSection - ScrollerMidiEvents.y, width, HEIGHT_CHANNEL_BANNER), "Meta Event", ChannelBannerStyle);
                                if (Context.SectionOpen[section.IndexSection])
                                {
                                    // only one layout
                                    float yMeta = section.Layouts[0].BegY - ScrollerMidiEvents.y;
                                    foreach (SectionMeta.MetaSet meta in (section as SectionMeta).Metas)
                                    {
                                        Rect metaRect = new Rect(0, yMeta, width, Context.CellHeight);
                                        GUI.Label(metaRect, meta.Name, MetaLabelStyle);
                                        yMeta += Context.CellHeight;
                                    }
                                }
                            }*/
                            if(temp == null)
                                uiMgr.m_TrackEditPanel.GetComponent<TrackEditPanel>().InitKeyboardAndTrackContent(48, 64);
                        }
                        else if (section is SectionChannel)
                        {
                            if (section.IndexSection == 9)
                            {
                                //
                                // Draw Drum
                                // ---------
                                if (Context.SectionOpen[section.IndexSection])
                                {
                                    int highNote = section.Layouts[(int)Layout.EnumType.Note].Higher;
                                    int lowNote = section.Layouts[(int)Layout.EnumType.Note].Lower;
                                    int count = highNote - lowNote + 1;
                                    if (highNote - lowNote + 1 < 14)
                                    {
                                        highNote += count % 2 == 0 ? (14 - count) / 2 : (14 - count) / 2 + 1;
                                        lowNote -= (14 - count) / 2;
                                    }
                                    uiMgr.m_TrackEditPanel.GetComponent<TrackEditPanel>().InitKeyboardAndTrackContent(lowNote, highNote);
                                }
                            }
                            else
                            {
                                //
                                // Draw keys
                                // ---------
                                if (Context.SectionOpen[section.IndexSection])
                                {
                                    temp = section;
                                    int highNote = section.Layouts[(int)Layout.EnumType.Note].Higher;
                                    int lowNote = section.Layouts[(int)Layout.EnumType.Note].Lower;
                                    int count = highNote - lowNote + 1;
                                    if (highNote - lowNote + 1 < 14)
                                    {
                                        highNote += count % 2 == 0 ? (14 - count) / 2 : (14 - count) / 2 + 1;
                                        lowNote -= (14 - count) / 2;
                                    }
                                    uiMgr.m_TrackEditPanel.GetComponent<TrackEditPanel>().InitKeyboardAndTrackContent(lowNote, highNote);
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }

            //DisplayPerf("DrawKeyboard");
        }

        private void PlayStop()
        {
            if (MidiPlayerGlobal.MPTK_SoundFontIsReady && MidiEvents != null && MidiEvents.Count > 0)
            {
                InitPosition(keepSequencerPosition: true);
                Player.OnEventStartPlayMidi.RemoveAllListeners();
                Player.OnEventNotesMidi.RemoveAllListeners();
                Player.OnEventEndPlayMidi.RemoveAllListeners();
                Player.MPTK_Stop();
            }
        }

        public void PlayOrStopMidi()
        {
            if (!Player.MPTK_IsPlaying)
            { PlayMidiFileSelected(); isPlay = true; }
            else
            {
                PlayStop();
                uiMgr.m_TrackEditPanel.SendMessage("StopPlayLine");
                isPlay = false;
            }
        }

        public void PauseMidi()
        {
            if (Player.MPTK_IsPlaying)
            {
                if (Player.MPTK_IsPaused)
                {
                    Player.MPTK_UnPause();
                    uiMgr.m_TrackEditPanel.SendMessage("PausePlayLine", false);
                }
                else
                {
                    Player.MPTK_Pause();
                    uiMgr.m_TrackEditPanel.SendMessage("PausePlayLine", true);
                }
            }
        }

        public void EventCreateMidiEvent(int index, int note, float x, float width, int channel = 0/*Event currentEvent, AreaUI area*/)
        {
            SectionCore section = sectionAll.Sections[channel];
            int line = -1;
            /*float xMouseCorrected = currentEvent.mousePosition.x + ScrollerMidiEvents.x - X_CORR_SECTION;*/
            long tick = sectionAll.ConvertPositionToTick(x);
            long duration = sectionAll.ConvertPositionToTick(width);
            // position begin header section
            /*float yMouseCorrected = currentEvent.mousePosition.y + ScrollerMidiEvents.y - Y_CORR_SECTION;*/
            //Debug.Log($"Create MIDI  IndexSection:{area.IndexSection}  x:{xMouseCorrected} y:{yMouseCorrected} Tick:{tick} AllLayout:{section.AllLayout.BegY} NoteLayout:{section.NoteLayout.BegY} PresetLayout:{section.PresetLayout.BegY}");


            MPTKEvent newEvent = null;
            foreach (Layout layout in section.Layouts)
                Array.ForEach(section.Layouts, layout =>
                {
                /*if (yMouseCorrected >= layout.BegY && yMouseCorrected <= layout.EndY)
                {*/
                    if (layout.Type == Layout.EnumType.Preset)
                    {
                    // Above preset section, add a section change
                    /*Debug.Log($"Preset zone  y:{yMouseCorrected} Channel:{area.Channel} Line:{line} Preset:{((SectionChannel)section).Presets[line].Value} PresetLayout:{layout}");*/
                    /*newEvent = new MPTKEvent()
                    {
                        Track = 1,
                        Command = MPTKCommand.PatchChange,
                        Value = ((SectionChannel)section).Presets[line].Value,
                    };*/
                    }
                    else if (layout.Type == Layout.EnumType.Note)
                    {
                    // Above notes section, add a note  
                    /*Debug.Log($"Note zone  y:{yMouseCorrected} Channel:{area.Channel} line:{line} IndexSection:{section.IndexSection} ");*/
                        newEvent = new MPTKEvent()
                        {
                            Index = index,
                            Track = 1,
                            Command = MPTKCommand.NoteOn,
                            Value = note,
                        // Duration will be calculate with RefreshMidi() 
                        Length = (int)duration,
                            Velocity = MidiEventEdit.Velocity
                        };
                    }
                    else if (layout.Type == Layout.EnumType.Meta)
                    {
                    /*Debug.Log($"Meta zone  y:{yMouseCorrected} Channel:{area.Channel} line:{line} IndexSection:{section.IndexSection} ");*/
                        if (line == 0)
                            newEvent = new MPTKEvent()
                            {
                                Index = index,
                                Track = 0,
                                Command = MPTKCommand.MetaEvent,
                                Meta = MPTKMeta.SetTempo,
                            /// MPTKEvent#Value contains new Microseconds Per Beat Note\n
                            Value = MPTKEvent.BeatPerMinute2QuarterPerMicroSecond(MidiEventEdit.TempoBpm),
                            };
                        else if (line == 1)
                            newEvent = new MPTKEvent()
                            {
                                Index = index,
                                Track = 0,
                                Command = MPTKCommand.MetaEvent,
                                Meta = MPTKMeta.TextEvent,
                                Info = "",
                            };
                    }
                //}
            });

            if (newEvent != null)
            {
                // Common settings and add event
                newEvent.Tick = tick;
                newEvent.Channel = channel;
                InsertEventIntoMidiFileWriter(newEvent);
                RefreshPadCmdMidi(newEvent);
            }
        }

        private bool InsertEventIntoMidiFileWriter(MPTKEvent newEvent)
        {
            try
            {
                if (newEvent.Command == MPTKCommand.NoteOn && newEvent.Duration == 0)
                {
                    Debug.Log($"Can't store noteon with duration 0 {newEvent}");
                    return false;
                }

                newEvent = ApplyQuantization(newEvent, toLowerValue: true);

                int index = MidiLoad.MPTK_SearchEventFromTick(MidiEvents, newEvent.Tick);
                if (index < 0)
                    index = 0;
                if (CheckMidiEventExist(newEvent, index))
                    return false;
                MidiEvents.Insert(index, newEvent);
                Debug.Log($"Create MIDI event -  index:{index} MIDI Event:{newEvent} ");

                // Sort MIDI events, calculate tempo map. For each MIDI events, calculate realtime, duration, measure, beat, index
                RefreshMidi();
                SelectedEvent = newEvent;

            }
            catch (Exception ex)
            {
                Debug.LogWarning($"InsertEventIntoMidiFileWriter {newEvent} {ex}");
            }
            //Repaint();
            return true;
        }

        private void RefreshPadCmdMidi(MPTKEvent mptkEvent)
        {
            //Debug.Log($"ApplyMidiEventToCurrent {mptkEvent}");
            CurrentTickPosition = mptkEvent.Tick;

            if (mptkEvent.Command == MPTKCommand.NoteOn)
            {
                MidiEventEdit.Channel = mptkEvent.Channel;
                MidiEventEdit.Command = 0;
                MidiEventEdit.Note = mptkEvent.Value;
                MidiEventEdit.Velocity = mptkEvent.Velocity;
                MidiEventEdit.Length = (int)mptkEvent.Length;

            }
            else if (mptkEvent.Command == MPTKCommand.PatchChange)
            {
                MidiEventEdit.Channel = mptkEvent.Channel;
                MidiEventEdit.Command = 1;
                MidiEventEdit.Preset = mptkEvent.Value;
                /*PopupSelectPreset.SelectedIndex = MidiEventEdit.Preset;*/

            }
            else if (mptkEvent.Command == MPTKCommand.MetaEvent)
            {
                MidiEventEdit.Channel = SectionAll.SECTION_META;
                if (mptkEvent.Meta == MPTKMeta.SetTempo)
                {
                    MidiEventEdit.Command = 2;
                    MidiEventEdit.TempoBpm = (int)MPTKEvent.QuarterPerMicroSecond2BeatPerMinute(mptkEvent.Value);
                }
                else if (mptkEvent.Meta == MPTKMeta.TextEvent)
                {
                    MidiEventEdit.Command = 3;
                    MidiEventEdit.Text = mptkEvent.Info;
                }
            }

            /*PopupSelectMidiChannel.SelectedIndex = MidiEventEdit.Channel;
            PopupSelectMidiCommand.SelectedIndex = MidiEventEdit.Command;*/
            MidiEventEdit.Tick = mptkEvent.Tick;
        }

        private MPTKEvent ApplyQuantization(MPTKEvent mEvent, bool toLowerValue = false)
        {
            mEvent.Tick = CalculateQuantization(mEvent.Tick, toLowerValue);
            return mEvent;
        }

        private long CalculateQuantization(long tick, bool toLowerValue = false)
        {
            long result;
            if (TickQuantization != 0)
            {
                float round = toLowerValue ? 0f : 0.5f;
                result = (long)((tick / (float)TickQuantization) + round) * TickQuantization;
                //Debug.Log($"tick:{tick} TickQuantization:{TickQuantization} ratio:{tick / (float)TickQuantization} result:{result}");
            }
            else
                result = tick;
            return TickQuantization != 0 ? result : tick;
        }

        private bool CheckMidiEventExist(MPTKEvent newEvent, int index)
        {
            bool exist = false;
            while (index < MidiEvents.Count && MidiEvents[index].Tick == newEvent.Tick)
            {
                if (MidiEvents[index].Command == newEvent.Command &&
                    MidiEvents[index].Channel == newEvent.Channel &&
                    MidiEvents[index].Value == newEvent.Value)
                {
                    Debug.LogWarning($"MIDI event already exists - Action canceled -  index:{index} MIDI Event:{newEvent} ");
                    return true;
                }
                index++;
            }
            return exist;
        }

        private void RefreshMidi(bool logPerf = true)
        {
            System.Diagnostics.Stopwatch watch = null;
            if (logPerf)
            {
                watch = new System.Diagnostics.Stopwatch(); // High resolution time
                watch.Start();
            }
            MidiFileWriter.StableSortEvents(logPerf: true);
            MidiFileWriter.CalculateTiming(logPerf: true, logDebug: false);
            FindLastMidiEvents();
            sectionAll.InitSections();
            Context.Modified = true;
            if (logPerf)
            {
                Debug.Log($"RefreshMidi {watch.ElapsedMilliseconds} {watch.ElapsedTicks}");
                watch.Stop();
            }
        }

        public void SelectMidiEvent(int index)
        {
            foreach (MPTKEvent mevent in MidiEvents)
            {
                if (mevent.Index == index)
                {
                    SelectedEvent = mevent;
                    NewDragEvent = mevent;
                    /*Debug.Log("index: " + index);*/
                    break;
                }
            }
        }

        public void EventMoveMidiEvent(int note, float x)
        {
            if (NewDragEvent != null)
            {
                //Debug.Log($"******* New Event drag:  {NewDragEvent.Tick} {NewDragEvent.Value} ");
                /*LastMousePosition = currentEvent.mousePosition;*/
                /*DragPosition = Vector2.zero;*/
                SelectedEvent = NewDragEvent;
                InitialTick = SelectedEvent.Tick;
                InitialValue = SelectedEvent.Value;
                /*InitialDurationTick = SelectedEvent.Length;*/
                NewDragEvent = null;
            }
            if (SelectedEvent != null)
            {
                SectionCore section = sectionAll.Sections[SelectedEvent.Channel];
                if (section != null)
                {
                    // Vertical move only for note
                    // ---------------------------
                    if (SelectedEvent.Command == MPTKCommand.NoteOn)
                    {
                        //Debug.Log($"    Event param:  {currentEvent.mousePosition} {currentEvent.delta} lastDragYPosition:{DragPosition} CellWidth:{CellWidth}  Sectx.CellHeight:{Sectx.CellHeight} ");
                        //Debug.Log($"        Change MIDI event DragPosition:{DragPosition} CellHeight:{Sectx.CellHeight} CellQuarterWidth:{CellQuarterWidth} to {LastMousePosition} ");
                        SelectedEvent.Value = note;
                        section.Layouts[1].SetLowerHigherNote(SelectedEvent.Value);
                    }
                    // Horizontal
                    // ----------

                    SelectedEvent.Tick = sectionAll.ConvertPositionToTick(x);
                    Context.Modified = true;

                    if (SelectedEvent.Tick < 0)
                        SelectedEvent.Tick = 0;

                    if (SelectedEvent.Tick != InitialTick)
                        // Sort MIDI events, calculate tempo map. For each MIDI events, calculate realtime, duration, measure, beat, index
                        RefreshMidi();

                    if (SelectedEvent.Value != InitialValue || SelectedEvent.Tick != InitialTick)
                    {
                        RefreshPadCmdMidi(SelectedEvent);
                        /*Repaint();*/
                    }
                    Debug.Log("moveCell");
                }
            }
        }

        public void EventResizeMidiEvent(float x, float width)
        {
            if (NewDragEvent != null)
            {
                //Debug.Log($"******* New Event drag:  {NewDragEvent.Tick} {NewDragEvent.Value} ");
                /*LastMousePosition = currentEvent.mousePosition;*/
                /*DragPosition = Vector2.zero;*/
                SelectedEvent = NewDragEvent;
                InitialTick = SelectedEvent.Tick;
                InitialValue = SelectedEvent.Value;
                InitialDurationTick = SelectedEvent.Length;
                NewDragEvent = null;
            }
            if (SelectedEvent != null)
            {
                SelectedEvent.Tick = sectionAll.ConvertPositionToTick(x);
                SelectedEvent.Length = (int)sectionAll.ConvertPositionToTick(width);
                //Debug.Log($"    Event param:  DragPosition:{DragPosition} QuarterWidth:{Context.QuarterWidth}  durationTicks:{SelectedEvent.Length} deltaTick:{deltaTick}");
                // Sort MIDI events, calculate tempo map. For each MIDI events, calculate realtime, duration, measure, beat, index
                RefreshMidi();
                RefreshPadCmdMidi(SelectedEvent);
            }
        }

        public bool AddSectionChannel(int channel)
        {
            try
            {
                //Debug.Log($"Add iSection {iSection}");
                if (channel >= SectionAll.SECTION_META || sectionAll.SectionExist(channel))
                    return false;

                MidiFileWriter.AddChangePreset(1, 0, channel, 0);
                // Add a iSection, with a default preset and lownote=60 and highnote=65
                sectionAll.AddSection(channel);
                Context.Modified = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AddChannel {channel} {ex}");
            }
            return true;
        }
        public bool DeleteChannel(int channel)
        {
            try
            {
                Debug.Log($"Delete channel {channel}");
                if (channel > 15 || !sectionAll.SectionExist(channel))
                    return false;

                MidiFileWriter.DeleteChannel(channel);
                sectionAll.InitSections();
                Context.Modified = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AddChannel {channel} {ex}");
            }
            return true;
        }

        private void SetDetailText() {
            string label = "";
            if (MidiFileWriter != null)
            {
                label = $"DTPQN:{MidiFileWriter.DeltaTicksPerQuarterNote}  {MidiFileWriter.MPTK_SignMap[0].NumberBeatsMeasure}/{MidiFileWriter.MPTK_SignMap[0].NumberQuarterBeat}  Events:{MidiFileWriter.MPTK_MidiEvents.Count}   ";
                if (Player.MPTK_IsPlaying)
                {
                    label += $"BPM:{Player.MPTK_Tempo:F1}   ";
                    label += $"{Player.MPTK_PulseLenght * Player.MPTK_DeltaTicksPerQuarterNote / 1000f:F2} sec/quarter";
                }
                else
                {
                    label += $"BPM:{MidiFileWriter.CurrentTempo:F1}   ";
                    label += $"{MidiFileWriter.PulseLenght * MidiFileWriter.DeltaTicksPerQuarterNote / 1000f:F2} sec/quarter";
                }

            }
            else {
                label = "No data";
            }
            uiMgr.m_TrackEditPanel.SendMessage("WriteDetailText", label, SendMessageOptions.DontRequireReceiver);
        }
    }

}
