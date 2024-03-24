#if UNITY_EDITOR
//#define DEBUG_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MidiPlayerTK
{

    public partial class MidiEditorWindow : EditorWindow
    {
        private void Awake()
        {
            //Debug.Log($"Awake");

            // Main
            //      Channels  --> Zone.Type.IndexSection == 0
            //      WhiteKeys --> Zone.Type.IndexSection == 1
            //          SubArea = list of white keys
            //      BlackKeys --> Zone.Type.IndexSection == 2
            //          SubArea = list of black keys
            //Debug.Log("Create Area");
            MainArea = new AreaUI()
            {
                Position = new Rect(),
                SubArea = new List<AreaUI>()
                {
                   new AreaUI()
                   {
                      areaType=AreaUI.AreaType.Channels,
                      Position = new Rect(),
                      SubArea = new List<AreaUI>(),
                   },
                   new AreaUI()
                   {
                      areaType=AreaUI.AreaType.WhiteKeys,
                      Position = new Rect(),
                      SubArea = new List<AreaUI>(),
                   },
                   new AreaUI()
                   {
                      areaType=AreaUI.AreaType.BlackKeys,
                      Position = new Rect(),
                      SubArea = new List<AreaUI>(),
                   },
                },
            };

            EditorDeserialize();
        }

        private void OnEnable()
        {
            //Debug.Log($"OnEnable");
            EditorApplication.playModeStateChanged += ChangePlayModeState;
            CompilationPipeline.compilationStarted += CompileStarted;
            InitPlayer();
            //InitGUI();

            //if (winSelectMidi != null)
            //{
            //    //Debug.Log("OnEnable winSelectMidi " + winSelectMidi.Title);
            //    winSelectMidi.SelectedIndexMidi = MidiIndex;
            //    winSelectMidi.Repaint();
            //    winSelectMidi.Focus();
            //}
        }
        private void OnDisable()
        {
            //Debug.Log($"OnDisable");
            EditorSerialize();
        }

        void OnDestroy()
        {
            //Debug.Log($"OnDestroy");
            try
            {
                if (SelectMidiWindow.winSelectMidi != null)
                {
                    //Debug.Log("OnDestroy winSelectMidi " + SelectMidiWindow.winSelectMidi.ToString());
                    SelectMidiWindow.winSelectMidi.Close();
                    SelectMidiWindow.winSelectMidi = null;
                }
            }
            catch (Exception)
            {
            }

            if (winPopupSynth != null)
            {
                winPopupSynth.Close();
                winPopupSynth = null;
            }

            EditorApplication.playModeStateChanged -= ChangePlayModeState;
            CompilationPipeline.compilationStarted -= CompileStarted;
            if (Player.OnEventStartPlayMidi != null) Player.OnEventStartPlayMidi.RemoveAllListeners();
            if (Player.OnEventNotesMidi != null) Player.OnEventNotesMidi.RemoveAllListeners();
            if (Player.OnEventEndPlayMidi != null) Player.OnEventEndPlayMidi.RemoveAllListeners();

            if (MidiPlayerSequencer != null) //strangely, this property can be null when window is close
                MidiPlayerSequencer.DestroyMidiObject();
            //else
            //    Debug.LogWarning("MidiPlayerEditor is null");
        }

        private void OnFocus()
        {
            // Load description of available soundfont
            try
            {
                //Debug.Log("OnFocus");
                MidiPlayerGlobal.InitPath();
                ToolsEditor.LoadMidiSet();
                ToolsEditor.CheckMidiSet();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        void OnGUI()
        {
            try
            {
                // In some case, Unity Editor lost skin, or texture, or style. Thats a random behavior but also
                // systematic at first load of MIDI Editor after launch of Unity Editor or a when scene change is done.
                // These "hack" seems correct the issue.
                if (MPTKGui.MaestroSkin == null || SepDragEventText == null || ChannelBannerStyle == null || ChannelBannerStyle.normal.background == null)
                {
                    //Debug.Log($" ********************* ReInit GUISkin **************************");
                    InitGUI();
                    mouseCursorRect.x = 0;
                    mouseCursorRect.y = 0;
                }

                // Skin must defined at each OnGUI cycle (certainly a global GUI variable)
                GUI.skin = MPTKGui.MaestroSkin;
                GUI.skin.settings.cursorColor = Color.white;
                GUI.skin.settings.cursorFlashSpeed = 0f;

                float startx = AREA_BORDER_X;
                float starty = AREA_BORDER_Y;
                float nextAreaY = starty;

                EventManagement();
                float width = window.position.width - 2 * AREA_BORDER_X;

                //Debug.Log($"{Screen.safeArea} {CurrentMouseCursor} position:{this.position}");
                //EditorGUIUtility.AddCursorRect(SceneView.lastActiveSceneView.position, CurrentMouseCursor);
                mouseCursorRect.width = this.position.width;
                mouseCursorRect.height = this.position.height;
                // EditorGUIUtility.AddCursorRect(mouseCursorRect, CurrentMouseCursor);

                // Main menu at the top window 
                CmdNewLoadSaveTools(startx, starty, width, HEIGHT_HEADER);
                nextAreaY += HEIGHT_HEADER + 1;// + AREA_SPACE;

                if (MidiFileWriter != null)
                {

                    CmdSequencer(startx, nextAreaY, width, HEIGHT_PLAYER_CMD);
                    nextAreaY += HEIGHT_PLAYER_CMD - 1;

                    ShowSequencer(startx, nextAreaY, width, window.position.height - nextAreaY - 1);

                    ToolTipMidiEditor.Display();

                    if (DebugDisplayCell)
                        DebugAreaUI();
                }
            }
            //catch (ExitGUIException) { }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }


        private void InitGUI()
        {
            //Debug.Log($"InitGUI");
            if (Context == null)
                LoadContext();

            MPTKGui.LoadSkinAndStyle(loadSkin: true);

            HeightScrollHori = MPTKGui.HorizontalThumb.fixedHeight;
            WidthScrollVert = MPTKGui.VerticalThumb.fixedWidth;
            //Debug.Log($"HeightScrollHori:{HeightScrollHori} WidthScrollVert:{WidthScrollVert}");

            SepBorder0 = new RectOffset(0, 0, 0, 0);
            SepBorder1 = new RectOffset(borderSize1, borderSize1, borderSize1, borderSize1);
            SepBorder2 = new RectOffset(borderSize2, borderSize2, borderSize2, borderSize2);
            SepBorder3 = new RectOffset(borderSize3, borderSize3, borderSize3, borderSize3);
            SepNoteTexture = MPTKGui.MakeTex(Color.blue, SepBorder1);
            SepPresetTexture = MPTKGui.MakeTex(Color.green, SepBorder2);
            SepBarText = MPTKGui.MakeTex(0.3f, SepBorder1);
            SepQuarterText = MPTKGui.MakeTex(0.6f, SepBorder1);
            SepDragEventText = MPTKGui.MakeTex(new Color(0.9f, 0.9f, 0f, 1), SepBorder1);
            SepDragMouseText = MPTKGui.MakeTex(new Color(0.9f, 0.9f, 0.7f, 1), SepBorder1);
            SepPlayingPositionTexture = MPTKGui.MakeTex(new Color(0.9f, 0.5f, 0.5f, 1), SepBorder1);
            SepSelectedPositionTexture = MPTKGui.MakeTex(new Color(0.5f, 0.7f, 0.5f, 1), SepBorder1);
            SepLoopTexture = MPTKGui.MakeTex(new Color(0.5f, 0.9f, 0.5f, 1), SepBorder1);

            // Warning: GUI.skin.GetStyle("label") doesn't work with contentOffset!!!
            TimelineStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.LabelListNormal, fontSize: 11, textAnchor: TextAnchor.MiddleCenter);
            //MPTKGui.ColorStyle(style: TimelineStyle,
            //    fontColor: Color.black,
            //    backColor: MPTKGui.MakeTex(100, 20, textureColor: new Color(0.8f, 0.9f, 0.8f, 1f), border: SepBorder1, bordercolor: Color.gray));

            //TimelineStyle.normal.background.
            //TestLenStyle();

            MeasurelineStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.LabelListNormal, fontSize: 11, textAnchor: TextAnchor.MiddleCenter);
            //MPTKGui.ColorStyle(style: MeasurelineStyle,
            //    fontColor: Color.black,
            //    backColor: MPTKGui.MakeTex(100, 20, textureColor: new Color(0.8f, 0.9f, 0.8f, 1f), border: SepBorder1, bordercolor: Color.gray));

            PresetButtonStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.MaestroSkin.GetStyle("box"), fontSize: 10, textAnchor: TextAnchor.MiddleCenter);
            MPTKGui.ColorStyle(style: PresetButtonStyle,
                fontColor: Color.black,
                backColor: MPTKGui.MakeTex(10, 10, textureColor: new Color(0.7f, 0.9f, 0.7f, 1f), border: SepBorder1, bordercolor: Color.gray));

            MetaLabelStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.MaestroSkin.GetStyle("box"), fontSize: 10, textAnchor: TextAnchor.MiddleCenter);
            MPTKGui.ColorStyle(style: MetaLabelStyle,
                fontColor: Color.black,
                backColor: MPTKGui.MakeTex(10, 10, textureColor: new Color(0.7f, 0.7f, 0.7f, 1f), border: SepBorder1, bordercolor: Color.gray));

            MidiNoteTexture = MPTKGui.MakeTex(new Color(0.7f, 0.7f, 0.9f, 1f));
            MidiPresetTexture = MPTKGui.MakeTex(new Color(0.6f, 0.9f, 0.7f, 1f));
            MidiSelectedTexture = MPTKGui.MakeTex(new Color(0.8f, 0.4f, 0.4f, 1f));

            ChannelBannerStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.MaestroSkin.GetStyle("box"), fontSize: 11, textAnchor: TextAnchor.MiddleLeft);
            MPTKGui.ColorStyle(style: ChannelBannerStyle,
                fontColor: Color.white,
                backColor: MPTKGui.MakeTex(10, 10, textureColor: new Color(0.4f, 0.2f, 0.2f, 1f), border: SepBorder1, bordercolor: new Color(0.5f, 0.5f, 0.5f, 1)));

            BackgroundMidiEvents = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.MaestroSkin.GetStyle("box"), fontSize: 11, textAnchor: TextAnchor.MiddleLeft);
            MPTKGui.ColorStyle(style: BackgroundMidiEvents,
                fontColor: Color.white,
                backColor: MPTKGui.MakeTex(10, 10, textureColor: new Color(0.6f, 0.6f, 0.8f, 1f), border: SepBorder1, bordercolor: new Color(0.1f, 0.1f, 0.1f, 1)));

            BackgroundMidiEvents1 = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.MaestroSkin.GetStyle("box"), fontSize: 11, textAnchor: TextAnchor.MiddleLeft);
            MPTKGui.ColorStyle(style: BackgroundMidiEvents1,
                fontColor: Color.white,
                backColor: MPTKGui.MakeTex(10, 10, textureColor: new Color(0.5f, 0.5f, 0.7f, 1f), border: SepBorder1, bordercolor: new Color(0.1f, 0.1f, 0.1f, 1)));

            WhiteKeyLabelStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.Label, fontSize: 11, textAnchor: TextAnchor.MiddleRight);
            WhiteKeyLabelStyle.normal.textColor = Color.black;
            WhiteKeyLabelStyle.focused.textColor = Color.black;

            BlackKeyLabelStyle = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.Label, fontSize: 11, textAnchor: TextAnchor.MiddleRight);
            BlackKeyLabelStyle.normal.textColor = Color.white;
            BlackKeyLabelStyle.focused.textColor = Color.white;

            WhiteKeyDrawTexture = MPTKGui.MakeTex(new Color(0.9f, 0.9f, 0.9f, 1f));
            BlackKeyDrawTexture = MPTKGui.MakeTex(new Color(0.1f, 0.1f, 0.1f, 1f));

            if (PopupItemsPreset == null)
            {
                PopupItemsPreset = new List<MPTKGui.StyleItem>();
                MidiPlayerGlobal.MPTK_ListPreset.ForEach(preset =>
                    PopupItemsPreset.Add(new MPTKGui.StyleItem(preset.Label, value: preset.Index, visible: true, selected: false)));
                PopupItemsPreset[0].Selected = true;
                //Debug.Log($"InitListPreset MidiPlayerGlobal.MPTK_ListPreset count:{MidiPlayerGlobal.MPTK_ListPreset}");
            }

            if (PopupItemsMidiCommand == null)
            {
                PopupItemsMidiCommand = new List<MPTKGui.StyleItem>
                {
                    new MPTKGui.StyleItem("Note", true, true),
                    new MPTKGui.StyleItem("Preset", true,false),
                    new MPTKGui.StyleItem("Tempo", true,false),
                    new MPTKGui.StyleItem("Text", true, false)
                };
            }

            if (PopupItemsDisplayTime == null)
            {
                PopupItemsDisplayTime = new List<MPTKGui.StyleItem>
                {
                    // value = index of the item
                    new MPTKGui.StyleItem("Ticks", 0, true, false),
                    new MPTKGui.StyleItem("Seconds", 1, true, false),
                    new MPTKGui.StyleItem("Time", 2, true, false)
                };
            }

            if (PopupItemsQuantization == null)
            {
                PopupItemsQuantization = new List<MPTKGui.StyleItem>
                {
                    // value = index of the item
                    new MPTKGui.StyleItem("Off", 0, true, false),
                    new MPTKGui.StyleItem("Whole", 1, true, false), //  entire length of a measure 
                    new MPTKGui.StyleItem("Half", 2,true, false), //   1/2 of the duration of a whole note (2 quarter) 
                    new MPTKGui.StyleItem("Quarter",3, true, false), //  1/4 of the duration of a whole note.
                    new MPTKGui.StyleItem("1/8", 4,true, false), // Eighth duration of a whole note.
                    new MPTKGui.StyleItem("1/16", 5,true, false), // Sixteenth duration of a whole note.
                    new MPTKGui.StyleItem("1/32", 6,true, false), // Thirty-second of the duration of a whole note.
                    new MPTKGui.StyleItem("1/64", 7,true, false), // Sixty-fourth of the duration of a whole note.
                    new MPTKGui.StyleItem("1/128",8, true, false) // Hundred twenty-eighth of the duration of a whole note.
                };
            }

            if (PopupItemsMidiChannel == null)
            {
                PopupItemsMidiChannel = new List<MPTKGui.StyleItem>();
                for (int i = 0; i <= 15; i++)
                    PopupItemsMidiChannel.Add(new MPTKGui.StyleItem($"Channel {i}", true, i == 0 ? true : false));
                //PopupItemsMidiChannel.Add(new MPTKGui.StyleItem($"Meta", true));
            }

            if (PopupItemsLoadMenu == null)
            {
                PopupItemsLoadMenu = new List<MPTKGui.StyleItem>
                {
                    new MPTKGui.StyleItem("Load from Maestro MIDI Database", true, false,MPTKGui.Button),
                    new MPTKGui.StyleItem("Load from an external MIDI file", true, false,MPTKGui.Button),
                    new MPTKGui.StyleItem("Insert from Maestro MIDI Database", true, false,MPTKGui.Button),
                    new MPTKGui.StyleItem("Insert from an external MIDI file", true, false, MPTKGui.Button),
                    new MPTKGui.StyleItem("Open Temp Folder", true, false, MPTKGui.Button),
                };
            }

            if (PopupItemsSaveMenu == null)
            {
                PopupItemsSaveMenu = new List<MPTKGui.StyleItem>
                {
                    new MPTKGui.StyleItem("Save to Maestro MIDI Database", true, false,MPTKGui.Button),
                    new MPTKGui.StyleItem("Save to an external MIDI file", true, false,MPTKGui.Button),
                    new MPTKGui.StyleItem("Save As to an external MIDI file", true, false,MPTKGui.Button),
                };
            }

            SelectPopupItemFromContext();
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
        private void ChangePlayModeState(PlayModeStateChange state)
        {
            //Debug.Log(">>> LogPlayModeState MidiSequencerWindow" + state);
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Close(); // call OnDestroy
            }
            //Debug.Log("<<< LogPlayModeState MidiSequencerWindow" + state); 
        }
        private void CompileStarted(object obj)
        {
            //Debug.Log("Compilation, close editor");
            // Don't appreciate recompilation when window is open
            Close(); // call OnDestroy which call EditorSerialize

            MidiFileWriter = null;
            MidiEvents = null;
            sectionAll = null;
            MidiFileWriter = null;
            MainArea = null;
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

        private void CreateMidi()
        {
            if (CheckMidiSaved())
            {
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
            }
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
            if (PopupSelectMidiChannel != null)
                PopupSelectMidiChannel.SelectedIndex = MidiEventEdit.Channel;
            if (PopupSelectMidiCommand != null)
                PopupSelectMidiCommand.SelectedIndex = MidiEventEdit.Command;
        }



        private void LoadMidiFromDB(object tag, int midiindex)
        {
            if (CheckMidiSaved())
            {
                MidiLoad midiLoader = new MidiLoad();
                midiLoader.MPTK_EnableChangeTempo = true;
                midiLoader.MPTK_KeepEndTrack = false;
                midiLoader.MPTK_KeepNoteOff = false;
                midiLoader.MPTK_LogLoadEvents = Player.MPTK_LogLoadEvents;
                midiLoader.MPTK_Load(midiindex);
                Context.MidiIndex = midiindex;
                //Player.MPTK_MidiIndex = MidiIndex;
                //Player.MPTK_PreLoad();
                Context.MidiName = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[Context.MidiIndex];
                Context.PathOrigin = "";
                Context.Modified = false;

                ImportToMidiFileWriter(midiLoader);
                Context.SetSectionOpen(true);
                Context.SetSectionMute(false);
                ResetCurrent();
                window.Repaint();

            }
        }
        static string GetTempFolder()
        {
            string folder = Path.Combine(Application.persistentDataPath, "MaestroMidiEditorTemp");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
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
                if (window != null)
                    window.Repaint();
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
        public void StartPlay(string midiname)
        {
            //Debug.Log($"StartPlay {midiname}  {MidiPlayerSequencer.MidiPlayer.MPTK_TickCurrent}");
            SetMuteChannel();

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

        /// <summary>@brief
        /// Event fired by MidiFilePlayer when midi notes are available. 
        /// Set by Unity Editor in MidiFilePlayer Inspector or by script with OnEventNotesMidi.
        /// </summary>
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
                    window.Repaint();
                }
            }
            catch (Exception ex)
            {
                throw new MaestroException($"MidiReadEvents.{ex.Message}");
            }
        }


        private void SelectPopupItemFromContext()
        {
            if (PopupItemsDisplayTime != null)
                PopupItemsDisplayTime.ForEach(item => { item.Selected = item.Value == Context.DisplayTime; });
            if (PopupSelectDisplayTime != null)
                PopupSelectDisplayTime.SelectedIndex = Context.DisplayTime;

            //Debug.Log($"SelectPopupItemFromContext IndexQuantization:{Context.IndexQuantization}");
            if (PopupItemsQuantization != null)
                PopupItemsQuantization.ForEach(item => { item.Selected = item.Value == Context.IndexQuantization; });
            if (PopupSelectQuantization != null)
                PopupSelectQuantization.SelectedIndex = Context.IndexQuantization;

            CalculRatioQuantization();
        }
        void Update()
        {
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
                    //Debug.Log($"Time.deltaTime:{Time.deltaTime} MPTK_TickCurrent:{Player.MPTK_TickCurrent} MPTK_Pulse:{Player.MPTK_Pulse} position:{position}");
                    Repaint();
                }
            }
        }


        private void CmdNewLoadSaveTools(float startX, float starty, float width, float height)
        {
            try
            {
                // Begin area header
                // --------------------------
                GUILayout.BeginArea(new Rect(startX, starty, width, height), MPTKGui.stylePanelGrayLight);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent() { text = "New", tooltip = "Create a new MIDI" }, MPTKGui.Button, GUILayout.Width(60)))
                {
                    CreateMidi();
                }

                // Midi Load
                // --------- 
                MPTKGui.ComboBox(ref PopupSelectLoadMenu, "Load", PopupItemsLoadMenu, false,
                        delegate (int index)
                        {
                            if (index == 0)
                            {
                                SelectMidiWindow.winSelectMidi = EditorWindow.GetWindow<SelectMidiWindow>(true, "Select a MIDI File to load in the Editor");
                                SelectMidiWindow.winSelectMidi.OnSelect = LoadMidiFromDB;
                                SelectMidiWindow.winSelectMidi.SelectedIndexMidi = Context.MidiIndex;
                            }
                            else if (index == 1)
                            {
                                if (CheckMidiSaved())
                                {

                                    string selectedFile = EditorUtility.OpenFilePanelWithFilters("Open and import MIDI file", ToolsEditor.lastDirectoryMidi,
                                       new string[] { "MIDI files", "mid,midi", "Karoke files", "kar", "All", "*" });
                                    if (!string.IsNullOrEmpty(selectedFile))
                                    {
                                        // selectedFile contins also the folder 
                                        ToolsEditor.lastDirectoryMidi = Path.GetDirectoryName(selectedFile);
                                        LoadExternalMidiFile(selectedFile);
                                        Context.PathOrigin = ToolsEditor.lastDirectoryMidi;
                                        Context.Modified = false;
                                    }
                                }
                            }
                            else if (index == 4)
                            {
                                Application.OpenURL("file://" + GetTempFolder());
                            }
                            else
                                EditorUtility.DisplayDialog("Loading option", "Not yet implemented", "OK");

                        }, MPTKGui.Button, widthPopup: 300, GUILayout.Width(60));


                // Midi Save
                // ---------

                MPTKGui.ComboBox(ref PopupSelectSaveMenu, "Save " + (Context.Modified ? " *" : ""), PopupItemsSaveMenu, false,
                        delegate (int index)
                        {
                            if (index == 0)
                            {
                                if (!string.IsNullOrWhiteSpace(Context.MidiName))
                                {
                                    MidiFileWriter.WriteToMidiDB(Context.MidiName);
                                    AssetDatabase.Refresh();
                                    Context.Modified = false;
                                }
                                else
                                    EditorUtility.DisplayDialog("Save MIDI to MIDI DB", "Enter a filename", "Ok", "Cancel");
                            }
                            else if (index == 1)
                            {
                                if (!string.IsNullOrWhiteSpace(Context.MidiName))
                                {
                                    if (string.IsNullOrEmpty(Context.PathOrigin))
                                    {
                                        Context.PathOrigin = EditorUtility.OpenFolderPanel("Select a folder to save your MIDI file", ToolsEditor.lastDirectoryMidi, "");
                                    }
                                    if (!string.IsNullOrEmpty(Context.PathOrigin))
                                    {
                                        string filename = Path.Combine(Context.PathOrigin, Context.MidiName + ".mid");
                                        Debug.Log("Write MIDI file:" + filename);
                                        MidiFileWriter.WriteToFile(filename);
                                        Context.Modified = false;
                                    }
                                }
                                else
                                    EditorUtility.DisplayDialog("Save MIDI to MIDI DB", "Enter a filename", "Ok", "Cancel");
                            }
                            else if (index == 2)
                            {
                                string path = EditorUtility.SaveFilePanel("Save As your MIDI file", ToolsEditor.lastDirectoryMidi, Context.MidiName + ".mid", "mid"); ;

                                if (path.Length != 0)
                                {
                                    Context.PathOrigin = Path.GetFullPath(path);
                                    Context.MidiName = Path.GetFileNameWithoutExtension(path);
                                    Debug.Log("Write MIDI file:" + path);
                                    MidiFileWriter.WriteToFile(path);
                                    Context.Modified = false;
                                }
                            }
                            else
                                EditorUtility.DisplayDialog("Loading option", "Not yet implemented", "OK");

                        }, MPTKGui.Button, widthPopup: 300, GUILayout.Width(60));


                // MIDI Name
                // ---------
                //GUILayout.Label("Name:", MPTKGui.LabelLeft, GUILayout.Height(22), GUILayout.ExpandWidth(false));
                //MPTKGui.MaestroSkin.settings.cursorColor = Color.cyan;
                string newName = GUILayout.TextField(Context.MidiName, MPTKGui.TextField, GUILayout.Width(250));
                if (newName != Context.MidiName)
                {
                    for (int i = 0; i < InvalidFileChars.Length; i++)
                        newName = newName.Replace(InvalidFileChars[i], '_');
                    Context.MidiName = newName;
                }

                // MIDI Information 
                // ----------------
                if (MidiFileWriter != null)
                {
                    string label = $"DTPQN:{MidiFileWriter.DeltaTicksPerQuarterNote}  {MidiFileWriter.MPTK_SignMap[0].NumberBeatsMeasure}/{MidiFileWriter.MPTK_SignMap[0].NumberQuarterBeat}  Events:{MidiFileWriter.MPTK_MidiEvents.Count}   ";
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

                    GUILayout.Label(label, MPTKGui.LabelGray, GUILayout.Height(22)/*, GUILayout.ExpandWidth(false)*/);
                }
                else
                    GUILayout.Label($" no midi file writer", MPTKGui.LabelLeft, GUILayout.Height(22)/*, GUILayout.ExpandWidth(false)*/);

                GUILayout.FlexibleSpace();

                // Midi Tools
                // ----------

                if (GUILayout.Button("Synth", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    if (winPopupSynth != null)
                    {
                        winPopupSynth.Close();
                        winPopupSynth = null;
                    }
                    else
                    {
                        winPopupSynth = EditorWindow.GetWindow<PopupInfoSynth>(false, "Maestro MIDI Synth");
                        winPopupSynth.minSize = new Vector2(760, 73);
                        winPopupSynth.maxSize = new Vector2(760, 73);
                        winPopupSynth.MidiSynth = Player;
                        winPopupSynth.ShowUtility();
                    }
                }

                if (GUILayout.Button("Event", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    MidiFileWriter.CalculateTiming(logPerf: true);
                    MidiFileWriter.LogWriter();
                }

                if (GUILayout.Button(new GUIContent(MPTKGui.IconRefresh, "Restaure default value"), MPTKGui.Button, GUILayout.Width(30), GUILayout.Height(22)))
                {
                    //Debug.Log($"ReInitGUI  '{MPTKGui.MaestroSkin.name}' '{GUI.skin.name}' {MPTKGui.HorizontalThumb.fixedHeight} {MPTKGui.HorizontalThumb.fixedWidth} - {MPTKGui.VerticalThumb.fixedHeight} {MPTKGui.VerticalThumb.fixedWidth}");
                    InitGUI();
                    Context.SetDefaultSize();
                    Repaint();
                }

                if (GUILayout.Button(new GUIContent(MPTKGui.IconHelp, "Get some help on MPTK web site"), MPTKGui.Button, GUILayout.Width(30), GUILayout.Height(22)))
                    Application.OpenURL("https://paxstellar.fr/maestro-midi-editor/");
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { GUILayout.EndHorizontal(); GUILayout.EndArea(); }
        }

        private bool CheckMidiSaved()
        {
            if (!Context.Modified)
                return true;
            else
            {
                if (EditorUtility.DisplayDialogComplex("MIDI not saved", "This MIDI sequence has not beed saved, if you continue change will be lost", "Close without saving", "Cancel", "") == 0)
                    return true;
            }
            return false;
        }

        private void CmdSequencer(float startx, float starty, float width, float height)
        {
            Event currentEvent = Event.current;

            CmdPadChannel(startx, starty, height);
            CmdPadMidiEvent(startx, starty, height);

            try // Begin area MIDI player commands
            {
                GUILayout.BeginArea(new Rect(
                    startx + WIDTH_PAD_MIDI_EVENT + 1 + WIDTH_PAD_CHANNEL + 1, starty,
                    width - WIDTH_PAD_MIDI_EVENT - 1 - WIDTH_PAD_CHANNEL - 1, height),
                    MPTKGui.stylePanelGrayMiddle);

                try // Player command  --- line 1 ---
                {
                    GUILayout.BeginHorizontal();

                    CmdMidiPlay();
                    CmdMidiLoop();

                    GUILayout.FlexibleSpace();
                    GUILayout.Label("View:");

                    // Select display time format
                    MPTKGui.ComboBox(ref PopupSelectDisplayTime, "{Label}", PopupItemsDisplayTime, false,
                           delegate (int index) { Context.DisplayTime = index; }, null, widthPopup: 70, GUILayout.Width(70));

                    if (GUILayout.Button(Context.LogEvents ? LabelLogEnabled : LabelLogDisabled, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                        Context.LogEvents = !Context.LogEvents;

                    if (GUILayout.Button(Context.TipEnabled ? LabelTipEnabled : LabelTipDisabled, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                        Context.TipEnabled = !Context.TipEnabled;

                    //Context.LogEvents = GUILayout.Toggle(Context.LogEvents, "Log Events ", MPTKGui.styleToggle, GUILayout.Width(80));
                }
                catch (Exception ex) { Debug.LogException(ex); throw; }
                finally { GUILayout.EndHorizontal(); }

                try // Player command  --- line 2 ---
                {
                    GUILayout.BeginHorizontal();

                    Context.FollowEvent = GUILayout.Toggle(Context.FollowEvent, new GUIContent("Follow", "When enabled, horizontal scrollbar is disabled"), MPTKGui.styleToggle, GUILayout.Width(60));

                    float volume = Player.MPTK_Volume;
                    GUILayout.Label("Volume:" + volume.ToString("F2"), MPTKGui.LabelLeft, GUILayout.Width(80));
                    Player.MPTK_Volume = GUILayout.HorizontalSlider(volume, 0.0f, 1f, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb, GUILayout.MinWidth(100));

                    float speed = Player.MPTK_Speed;
                    // Button to restore speed to 1 with label style
                    if (GUILayout.Button("   Speed: " + speed.ToString("F2"), MPTKGui.LabelRight, GUILayout.ExpandWidth(false))) speed = 1f;
                    Player.MPTK_Speed = GUILayout.HorizontalSlider(speed, 0.01f, 10f, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb, GUILayout.MinWidth(100));
                }
                catch (Exception ex) { Debug.LogException(ex); throw; }
                finally { GUILayout.EndHorizontal(); }


                try // Player command  --- line 3 ---
                {
                    GUILayout.BeginHorizontal();
                    CmdGoto();
                    CmdTimeSlider();
                }
                catch (Exception ex) { Debug.LogException(ex); throw; }
                finally { GUILayout.EndHorizontal(); }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { GUILayout.EndArea(); }

        }

        private void CmdMidiPlay()
        {
            try // Player command  --- line 1 ---
            {
                GUILayout.Label("Play:", GUILayout.Width(30));
                // Play
                if (!Player.MPTK_IsPlaying)
                {
                    if (GUILayout.Button(LabelPlay, MPTKGui.Button, GUILayout.Width(36), GUILayout.Height(heightFirstRowCmd)))
                        PlayMidiFileSelected();
                }
                else
                {
                    // Stop
                    if (GUILayout.Button(LabelPlaying, MPTKGui.Button, GUILayout.Width(36), GUILayout.Height(heightFirstRowCmd)))
                        PlayStop();
                }

                // Pause
                if (GUILayout.Button(Player.MPTK_IsPaused ? LabelPauseSet : LabelPause, MPTKGui.Button, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                    if (Player.MPTK_IsPaused)
                        Player.MPTK_UnPause();
                    else
                        Player.MPTK_Pause();

                GUILayout.Space(15);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
            finally { }
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

        private void CmdMidiLoop()
        {
            try // Player command  --- line 1 ---
            {

                // Set looping
                // -----------
                GUILayout.Label("Loop:");
                if (GUILayout.Button(Context.LoopEnabled ? LabelLoopSet : LabelLoop, MPTKGui.Button, GUILayout.Width(36), GUILayout.Height(heightFirstRowCmd)))
                {
                    Context.LoopEnabled = !Context.LoopEnabled;
                    SetInnerLoopFromContext();
                }

                // Set looping start
                // -----------------
                //if (Context.LoopResume != 0)
                LabelLoopStart.tooltip = $"Loop from tick:{Context.LoopResume}\nSet loop start from the value of the selected event or from the 'Tick' value";
                //if (GUILayout.Button(Context.LoopResume == 0 ? LabelLoopStart : LabelLoopStartSet, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                if (GUILayout.Button(LabelLoopStart, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                {
                    Context.LoopResume = MidiEventEdit.Tick;
                    if (Context.LoopResume > Context.LoopEnd)
                    {
                        Context.LoopEnd = Context.LoopResume;
                        Player.MPTK_InnerLoop.End = Context.LoopEnd;
                    }
                    SetInnerLoopFromContext();
                }

                // Set looping end
                // ---------------
                //if (Context.LoopEnd != 0)
                LabelLoopStop.tooltip = $"Loop until tick:{Context.LoopEnd}\nSet loop end from the value of the selected event + duration or from the 'Tick' value";
                //if (GUILayout.Button(Context.LoopEnd == 0 ? LabelLoopStop : LabelLoopStopSet, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                if (GUILayout.Button(LabelLoopStop, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                {
                    Context.LoopEnd = MidiEventEdit.Tick + (SelectedEvent != null ? MidiEventEdit.Length : 0);
                    if (Context.LoopEnd < Context.LoopResume)
                        Context.LoopEnd = Context.LoopResume;
                    SetInnerLoopFromContext();
                }

                // Reset looping position
                // ----------------------
                LabelLoopReset.tooltip = $"Loop from tick {Context.LoopResume} to {Context.LoopEnd}\nClear current value. ";
                if (GUILayout.Button(LabelLoopReset, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                {
                    Context.LoopResume = Context.LoopEnd = 0;
                    SetInnerLoopFromContext();
                }

#if TO_BE_MOVED_TO_SETUP // example of popup with a label, issue with the height of tha label which can't be defined
                GUIContent LabelModeLoop = new GUIContent(Player.ModeStopPlayLabel[(int)Player.MPTK_ModeStopVoice], MPTKGui.IconComboBox);
                GUILayout.Label(LabelModeLoop, MPTKGui.ButtonCombo);
                if (Event.current.type == EventType.MouseDown)
                {
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    if (lastRect.Contains(Event.current.mousePosition))
                    {
                        var dropDownMenu = new GenericMenu();
                        foreach (ModeStopPlay mode in Enum.GetValues(typeof(ModeStopPlay)))
                            dropDownMenu.AddItem(
                                new GUIContent(Player.ModeStopPlayLabel[(int)mode], ""),
                                Player.MPTK_ModeStopVoice == mode, () => { Player.MPTK_ModeStopVoice = mode; });
                        dropDownMenu.ShowAsContext();
                    }
                } 
#endif
                // Set looping mode (not apply to inner loop)
                // ----------------
                //if (GUILayout.Button(LabelLoopMode, GUILayout.Width(heightFirstRowCmd), GUILayout.Height(heightFirstRowCmd)))
                //{
                //    var dropDownMenu = new GenericMenu();
                //    foreach (ModeStopPlay mode in Enum.GetValues(typeof(ModeStopPlay)))
                //        dropDownMenu.AddItem(new GUIContent(MidiFilePlayer.ModeStopPlayLabel[(int)mode], ""),
                //            Context.ModeLoop == mode, () => { Player.MPTK_ModeStopVoice = mode; Context.ModeLoop = mode; });
                //    dropDownMenu.ShowAsContext();
                //}
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
            finally { }
        }

        private void CmdGoto()
        {
            GUILayout.Label("Goto:", GUILayout.Width(30));

            long lastTick = LastMidiEvent != null ? LastMidiEvent.Tick : 0;
            long current = Player.MPTK_IsPlaying ? Player.midiLoaded.MPTK_TickPlayer : CurrentTickPosition;
            long currentSaved = current;

            if (GUILayout.Button(LabelGoToBegin, MPTKGui.Button, GUILayout.Width(22), GUILayout.Height(22)))
                current = 0;

            if (GUILayout.Button(LabelGoToPrevBar, MPTKGui.Button, GUILayout.Width(22), GUILayout.Height(22)))
            {
                int index = MPTKSignature.FindSegment(MidiFileWriter.MPTK_SignMap, current);
                int measure = MidiFileWriter.MPTK_SignMap[index].TickToMeasure(current);
                if (measure > 0)
                {
                    long tick = MPTKSignature.MeasureToTick(MidiFileWriter.MPTK_SignMap, measure - 1);
                    if (tick >= 0)
                        current = tick;
                }
            }


            if (GUILayout.Button(LabelGoToCurrent, MPTKGui.Button, GUILayout.Width(22), GUILayout.Height(22)))
                current = MidiEventEdit.Tick;

            //! [ExampleFindNextMeasure]
            // Go to next measure
            if (GUILayout.Button(LabelGoToNextBar, MPTKGui.Button, GUILayout.Width(22), GUILayout.Height(22)))
            {
                int index = MPTKSignature.FindSegment(MidiFileWriter.MPTK_SignMap, current);
                int measure = MidiFileWriter.MPTK_SignMap[index].TickToMeasure(current);
                if (measure > 0)
                {
                    long tick = MPTKSignature.MeasureToTick(MidiFileWriter.MPTK_SignMap, measure + 1);
                    if (tick >= 0)
                        current = tick;
                }
            }
            //! [ExampleFindNextMeasure]

            if (GUILayout.Button(LabelGoToEnd, MPTKGui.Button, GUILayout.Width(22), GUILayout.Height(22)))
                current = lastTick;

            if (current != currentSaved)
            {
                // Didn't works if (Player.midiLoaded != null) Player.midiLoaded.MPTK_TickCurrent = current;
                CurrentTickPosition = current;

                if (Player.MPTK_IsPlaying)
                {
                    Player.MPTK_TickCurrent = CurrentTickPosition;
                    Debug.Log($"MPTK_TickCurrent:{Player.MPTK_TickCurrent}");
                }
                float position = sectionAll.ConvertTickToPosition(CurrentTickPosition);

                // Avoid repaint for value bellow 1 pixel
                if ((int)position != PositionSequencerPix)
                {
                    PositionSequencerPix = (int)position;
                    SetScrollXPosition(PositionSequencerPix, widthVisibleEventsList);
                    Repaint();
                }
            }
        }
        private void CmdTimeSlider()
        {
            long lastTick = LastMidiEvent != null ? LastMidiEvent.Tick : 0;
            long current = Player.MPTK_IsPlaying ? Player.midiLoaded.MPTK_TickPlayer : CurrentTickPosition;

            if (Context.DisplayTime == 0)
            {
                GUILayout.Label($"{current:000000} / {lastTick:000000}", MPTKGui.LabelCenter, GUILayout.Width(120));
                long tick = (long)GUILayout.HorizontalSlider((float)current, 0f, lastTick, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb);
                if (tick != current)
                {
                    //MidiEventEdit.Tick = tick;
                    CurrentTickPosition = tick;
                    if (Player.MPTK_IsPlaying)
                    {
                        Player.MPTK_TickCurrent = CurrentTickPosition;
                        //Debug.Log($"MPTK_TickCurrent:{Player.MPTK_TickCurrent} MPTK_Pulse:{Player.MPTK_Pulse}");
                    }
                    float position = sectionAll.ConvertTickToPosition(CurrentTickPosition);

                    // Avoid repaint for value bellow 1 pixel
                    if ((int)position != PositionSequencerPix)
                    {
                        PositionSequencerPix = (int)position;
                        SetScrollXPosition(PositionSequencerPix, widthVisibleEventsList);
                        Repaint();
                    }
                }
            }
            else
            {
                int lastPositionMilli = 0;
                int currentPositionMilli = 0;
                int indexTempo = 0;
                if (MidiFileWriter.MPTK_TempoMap.Count > 0)
                {
                    try
                    {
                        if (LastMidiEvent != null)
                            lastPositionMilli = (int)(MidiFileWriter.MPTK_TempoMap.Last().CalculateTime(LastMidiEvent.Tick) / Player.MPTK_Speed);
                        indexTempo = MPTKTempo.FindSegment(MidiFileWriter.MPTK_TempoMap, current, fromIndex: 0);
                        currentPositionMilli = (int)(MidiFileWriter.MPTK_TempoMap[indexTempo].CalculateTime(current) / Player.MPTK_Speed);
                    }
                    catch
                    {
                        Debug.LogWarning($"Issue with real time calculation indexTempo={indexTempo} tick={current}");
                    }
                }
                if (Context.DisplayTime == 1)
                {
                    GUILayout.Label($"{currentPositionMilli / 1000f:F2} / {lastPositionMilli / 1000f:F2}", MPTKGui.LabelCenter, GUILayout.Width(100));
                }
                else if (Context.DisplayTime == 2)
                {
                    TimeSpan timePos = TimeSpan.FromMilliseconds(currentPositionMilli);
                    string playTime = string.Format("{0:00}:{1:00}:{2:00}:{3:000}", timePos.Hours, timePos.Minutes, timePos.Seconds, timePos.Milliseconds);
                    TimeSpan lastPos = TimeSpan.FromMilliseconds(lastPositionMilli);
                    string lastTime = string.Format("{0:00}:{1:00}:{2:00}:{3:000}", lastPos.Hours, lastPos.Minutes, lastPos.Seconds, lastPos.Milliseconds);
                    GUILayout.Label($"{playTime} / {lastTime}", MPTKGui.LabelCenter, GUILayout.Width(165));
                }

                // slider
                int newPositionMilli = (int)GUILayout.HorizontalSlider(currentPositionMilli, 0f, lastPositionMilli, MPTKGui.HorizontalSlider, MPTKGui.HorizontalThumb);

                if (newPositionMilli != currentPositionMilli)
                {
                    // Horible approximation which didn't take into account tempo change
                    CurrentTickPosition = Convert.ToInt64(((newPositionMilli / 1000f) / (lastPositionMilli / 1000f)) * lastTick);
                    //MidiEventEdit.Tick = CurrentTickPosition;

                    if (Player.MPTK_IsPlaying)
                    {
                        Player.MPTK_TickCurrent = CurrentTickPosition;
                        Debug.Log($"MPTK_TickCurrent:{Player.MPTK_TickCurrent} MPTK_Pulse:{Player.MPTK_PulseLenght}");
                    }
                    float position = sectionAll.ConvertTickToPosition(CurrentTickPosition);

                    // Avoid repaint for value bellow 1 pixel
                    if ((int)position != PositionSequencerPix)
                    {
                        PositionSequencerPix = (int)position;
                        SetScrollXPosition(PositionSequencerPix, widthVisibleEventsList);
                        Repaint();
                    }
                }
            }
        }

        private void CmdPadChannel(float startx, float starty, float height)
        {
            try
            {
                GUILayout.BeginArea(new Rect(startx, starty, WIDTH_PAD_CHANNEL, height), MPTKGui.stylePanelGrayMiddle);

                // Select IndexSection
                MPTKGui.ComboBox(ref PopupSelectMidiChannel, "{Label}", PopupItemsMidiChannel, false,
                    delegate (int index) { MidiEventEdit.Channel = index; }, null, widthPopup: 80, GUILayout.Width(92), GUILayout.Height(heightFirstRowCmd));

                GUILayout.BeginHorizontal();
                GUILayout.Label(" "); // let an empty line
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    // Add iSection
                    AddSectionChannel(MidiEventEdit.Channel);
                }

                if (GUILayout.Button("Del", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    // remove iSection 
                    if (EditorUtility.DisplayDialog($"Delete Channel {MidiEventEdit.Channel}", $"All MIDI events on the channel {MidiEventEdit.Channel} will be deleted.", $"Delete Channel {MidiEventEdit.Channel}", "Cancel"))
                        DeleteChannel(MidiEventEdit.Channel);
                }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally
            {
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }
        private void CmdPadMidiEvent(float startx, float starty, float height)
        {
            try
            {
                GUILayout.BeginArea(new Rect(startx + WIDTH_PAD_CHANNEL + 1, starty, WIDTH_PAD_MIDI_EVENT, height), MPTKGui.stylePanelGrayMiddle);

                // no track in this version
                // CurrentTrack = MPTKGui.IntField("Track:", CurrentTrack, min: 0, max: 999, maxLength: 3, width: 40);

                // ------ line 1 ----------
                GUILayout.BeginHorizontal();

                // Select MIDI command
                MPTKGui.ComboBox(ref PopupSelectMidiCommand, "{Label}", PopupItemsMidiCommand, false,
                    delegate (int index)
                    {
                        SelectedEvent = null;
                        MidiEventEdit.Command = index;
                        //Debug.Log($"MidiEventEdit.CurrentCommand:{MidiEventEdit.CurrentCommand}");
                    }, null, widthPopup: 60, GUILayout.Width(65), GUILayout.Height(heightFirstRowCmd));


                // Select quantization
                MPTKGui.ComboBox(ref PopupSelectQuantization, "Snap: {Label}", PopupItemsQuantization, false,
                       delegate (int index) { Context.IndexQuantization = index; CalculRatioQuantization(); }, null, widthPopup: 200, GUILayout.Width(105), GUILayout.Height(heightFirstRowCmd));

                long tick = MPTKGui.LongField("Tick:", MidiEventEdit.Tick, min: 0, max: 99999999999999999, maxLength: 7, widthLabel: 30, widthText: 60);
                if (tick != MidiEventEdit.Tick)
                {
                    CurrentTickPosition = MidiEventEdit.Tick = tick;
                }
                GUILayout.EndHorizontal();

                // ------ Line 2 -------
                GUILayout.BeginHorizontal();

                if (MidiEventEdit.Command == 0) //noteon
                {
                    MidiEventEdit.Note = MPTKGui.IntField("Note:", MidiEventEdit.Note, min: 0, max: 127, maxLength: 3, widthLabel: 30, widthText: 30);
                    MidiEventEdit.Velocity = MPTKGui.IntField("Velocity:", MidiEventEdit.Velocity, min: 0, max: 127, maxLength: 3, widthLabel: 50, widthText: 30);
                }
                else if (MidiEventEdit.Command == 1) //preset change
                {
                    MPTKGui.ComboBox(ref PopupSelectPreset, "{Label}", PopupItemsPreset, false,
                            action: delegate (int index)
                            {
                                MidiEventEdit.Preset = PopupItemsPreset[index].Value;
                                Repaint();
                            },
                            null, widthPopup: 180, option: GUILayout.MinWidth(100));
                }
                else if (MidiEventEdit.Command == 2) //tempo
                {
                    MidiEventEdit.TempoBpm = MPTKGui.IntField("BPM:", MidiEventEdit.TempoBpm, min: 1, max: 9999, maxLength: 4, widthLabel: 40, widthText: 60);
                }
                else if (MidiEventEdit.Command == 3) //text
                {
                    MidiEventEdit.Text = GUILayout.TextField(MidiEventEdit.Text, MPTKGui.TextField, GUILayout.MinWidth(100));
                }

                //GUILayout.FlexibleSpace();


                if (MidiEventEdit.Command == 0)
                    MidiEventEdit.Length = MPTKGui.IntField("Duration:", MidiEventEdit.Length, min: 0, max: 999999999, maxLength: 7, widthLabel: 50, widthText: 60);

                GUILayout.EndHorizontal();

                // ------  Line 3 --------
                GUILayout.BeginHorizontal();

                CmdAddOrApplyEvent();
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally
            {
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

        private void CmdAddOrApplyEvent()
        {
            if (SelectedEvent == null)
            {
                // Mode create
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Add", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    CreateEventFromPad();
                }
            }
            else
            {
                if (GUILayout.Button("Unselect", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                    SelectedEvent = null;

                GUILayout.FlexibleSpace();

                // Mode apply or delete on selectedInFilterList
                if (GUILayout.Button("Apply", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    ApplyMidiChangeFromPad();
                }

                if (GUILayout.Button("Del", MPTKGui.Button, GUILayout.ExpandWidth(false)))
                {
                    DeleteEventFromMidiFileWriter(SelectedEvent);
                    SelectedEvent = null;
                }
            }
        }

        private void CreateEventFromPad()
        {
            // Add iSection if needed
            AddSectionChannel(MidiEventEdit.Channel);
            MPTKEvent newEvent = null;

            MidiEventEdit.Tick = CalculateQuantization(MidiEventEdit.Tick);
            MidiEventEdit.Length = (int)CalculateQuantization((long)MidiEventEdit.Length);

            newEvent = new MPTKEvent()
            {
                Track = 1,
                Channel = MidiEventEdit.Channel,
                Tick = MidiEventEdit.Tick,
            };

            if (MidiEventEdit.Command == 0) //noteon
            {
                newEvent.Command = MPTKCommand.NoteOn;
                newEvent.Value = MidiEventEdit.Note;
                // newEvent.Duration will be calculate with RefreshMidi()
                newEvent.Length = MidiEventEdit.Length;
                newEvent.Velocity = MidiEventEdit.Velocity;
            }
            else if (MidiEventEdit.Command == 1) //preset change
            {
                newEvent.Command = MPTKCommand.PatchChange;
                newEvent.Value = PopupSelectPreset.SelectedValue;
            }
            else if (MidiEventEdit.Command == 2) //tempo change
            {
                if (MidiEventEdit.TempoBpm <= 0 || MidiEventEdit.TempoBpm >= 10000)
                {
                    Debug.LogWarning("BPM must be greater than 0 and lower than 10000");
                    newEvent = null;
                }
                else
                {
                    newEvent.Command = MPTKCommand.MetaEvent;
                    newEvent.Meta = MPTKMeta.SetTempo;
                    //newEvent.Duration = MidiEventEdit.CurrentBPM;
                    /// MPTKEvent#Value contains new Microseconds Per Beat Note\n
                    newEvent.Value = 60000000 / MidiEventEdit.TempoBpm;
                }
            }
            else if (MidiEventEdit.Command == 3) //text change
            {
                newEvent.Command = MPTKCommand.MetaEvent;
                newEvent.Meta = MPTKMeta.TextEvent;
                newEvent.Info = MidiEventEdit.Text;
            }
            else
            {
                EditorUtility.DisplayDialog("Loading option", "Not yet implemented", "OK");
                newEvent = null;
            }

            if (newEvent != null)
                InsertEventIntoMidiFileWriter(newEvent);
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
                PopupSelectPreset.SelectedIndex = MidiEventEdit.Preset;

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

            PopupSelectMidiChannel.SelectedIndex = MidiEventEdit.Channel;
            PopupSelectMidiCommand.SelectedIndex = MidiEventEdit.Command;
            MidiEventEdit.Tick = mptkEvent.Tick;
        }

        // Calls from button "Apply" from pad or from keyboard shortcut KeypadEnter / KeyCode.Return
        private void ApplyMidiChangeFromPad()
        {
            MidiEventEdit.Tick = CalculateQuantization(MidiEventEdit.Tick);
            MidiEventEdit.Length = (int)CalculateQuantization((long)MidiEventEdit.Length);

            SelectedEvent.Tick = MidiEventEdit.Tick;
            SelectedEvent.Channel = MidiEventEdit.Channel;

            if (MidiEventEdit.Command == 0) //noteon
            {
                SelectedEvent.Value = MidiEventEdit.Note;
                // SelectedEvent.Duration will be calculate with RefreshMidi() 
                SelectedEvent.Length = MidiEventEdit.Length;
                SelectedEvent.Velocity = MidiEventEdit.Velocity;
            }
            else if (MidiEventEdit.Command == 1) //preset change
            {
                SelectedEvent.Value = PopupSelectPreset.SelectedValue;
            }
            else if (MidiEventEdit.Command == 2) //tempo change
            {
                if (MidiEventEdit.TempoBpm <= 0 || MidiEventEdit.TempoBpm >= 10000)
                {
                    Debug.LogWarning("BPM must be greater than 0 and lower than 10000");
                    return;
                }
                else
                {
                    //SelectedEvent.Duration = MidiEventEdit.CurrentBPM;
                    // MPTKEvent#Value contains new Microseconds Per Beat Note\n
                    SelectedEvent.Value = 60000000 / MidiEventEdit.TempoBpm;
                }
            }
            else if (MidiEventEdit.Command == 3) //text change
            {
                SelectedEvent.Info = MidiEventEdit.Text;
            }
            else
            {
                Debug.LogWarning("Not yet implemented");
                return;
            }
            ApplyEventToMidiFileWriter(SelectedEvent);
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
        private bool AddSectionChannel(int channel)
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
        private bool DeleteChannel(int channel)
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

        private bool ApplyEventToMidiFileWriter(MPTKEvent modifiedEvent)
        {
            try
            {
                modifiedEvent = ApplyQuantization(modifiedEvent, toLowerValue: true);
                // Not used by the Midi synth, will be calculate if reloaded but we need it if position is displayed in second
                //modifiedEvent.RealTime = ConvertTickToDuration(modifiedEvent.Tick);

                Debug.Log($"ApplyEventInMidiFileWriter - MIDI Event:{modifiedEvent}");

                // Sort MIDI events, calculate tempo map. For each MIDI events, calculate realtime, duration, measure, beat, index
                RefreshMidi();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ApplyEventToMidiFileWriter {modifiedEvent} {ex}");
            }
            Repaint();
            return true;
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
            Repaint();
            return true;
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

        private bool DeleteEventFromMidiFileWriter(MPTKEvent delEvent)
        {
            bool ok = true;
            try
            {
                int index = MidiLoad.MPTK_SearchEventFromTick(MidiFileWriter.MPTK_MidiEvents, delEvent.Tick);
                if (index >= 0)
                {
                    MidiEvents.Remove(delEvent);
                    Debug.Log($"Delete MIDI event -  index:{index} MIDI Event:{delEvent} ");
                    // Sort MIDI events, calculate tempo map. For each MIDI events, calculate realtime, duration, measure, beat, index
                    RefreshMidi();

                    SelectedEvent = delEvent;
                    Repaint();
                }
                else
                    ok = false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DeleteEventFromMidiFileWriter {delEvent} {ex}");
            }
            return ok;
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



        /// <summary>
        /// 
        /// </summary>
        /// <param name="startx">AREA_BORDER</param>
        /// <param name="starty">HEIGHT_HEADER + HEIGHT_PLAYER_CMD</param>
        /// <param name="width">width of visible area : window.position.width - 2 * AREA_BORDER</param>
        /// <param name="height">heightFirstRowCmd of visible area : window.position.heightFirstRowCmd - starty</param>
        private void ShowSequencer(float startx, float starty, float width, float height)
        {
            try // Begin area MIDI events list
            {
                //if (MidiEvents.Count == 0)
                //    // Draw background MIDI events
                //    GUI.Box(new Rect(startx, starty, width, heightFirstRowCmd), "", BackgroundMidiEvents); // MPTKGui.stylePanelGrayLight
                //else
                if (MidiEvents.Count > 0)
                {
                    if (winPopupSynth != null)
                        winPopupSynth.Repaint();

                    //DisplayPerf(null, true);
                    sectionAll.CalculateSizeAllSections(LastMidiEvent, Context.QuarterWidth, Context.CellHeight);


                    if (Event.current.type != EventType.MouseMove)
                    {
                        startXEventsList = startx + WIDTH_KEYBOARD;
                        startYEventsList = starty + HEIGHT_TIMELINE;
                        widthVisibleEventsList = width - WIDTH_KEYBOARD /*- WidthScrollVert*/; // with of the area displayed on the screen
                        heightVisibleEventsList = height - HEIGHT_TIMELINE - HeightScrollHori; // heightFirstRowCmd of the area displayed on the screen

                        // Contains timeline, keyboard, events
                        MainArea.Position.x = startx;
                        MainArea.Position.y = starty;
                        MainArea.Position.width = width - WidthScrollVert;
                        MainArea.Position.height = height - HeightScrollHori;

                        //DrawMidiEvents(SectionAll.SECTION_META, startXEventsList, starty, startYEventsList, widthVisibleEventsList, heightVisibleEventsList);
                        //startYEventsList += 100;

                        // Area upon the keyboard area and at left of the measure line area
                        // ----------------------------------------------------------------
                        GUILayout.BeginArea(new Rect(startx, starty, WIDTH_KEYBOARD, HEIGHT_TIMELINE), MPTKGui.stylePanelGrayMiddle);

                        rectSectionBtAllSections.x = 2;
                        if (GUI.Button(rectSectionBtAllSections, LabelSectionOpen, MPTKGui.ButtonSmall)) Context.SetSectionOpen(true);

                        rectSectionBtAllSections.x += rectSectionBtAllSections.width;
                        if (GUI.Button(rectSectionBtAllSections, LabelSectionClose, MPTKGui.ButtonSmall)) Context.SetSectionOpen(false);

                        rectSectionBtAllSections.x += rectSectionBtAllSections.width;
                        if (GUI.Button(rectSectionBtAllSections, LabelSectionMute, MPTKGui.ButtonSmall))
                        {
                            Context.SetSectionMute(true);
                            SetMuteChannel();
                        }
                        rectSectionBtAllSections.x += rectSectionBtAllSections.width;
                        if (GUI.Button(rectSectionBtAllSections, LabelSectionUnmute, MPTKGui.ButtonSmall))
                        {
                            Context.SetSectionMute(false);
                            SetMuteChannel();
                        }

                        GUILayout.EndArea();

                        DrawMeasureLine(startXEventsList, starty, widthVisibleEventsList, HEIGHT_TIMELINE);

                        MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position.x = startx;
                        MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position.y = startYEventsList;
                        MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position.width = WIDTH_KEYBOARD - 1;
                        MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position.height = heightVisibleEventsList;
                        MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].Position = MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position;

                        DrawKeyboard(startx, startYEventsList, WIDTH_KEYBOARD - 1, heightVisibleEventsList);

                        // Draw background MIDI events
                        GUI.Box(new Rect(startXEventsList, startYEventsList, widthVisibleEventsList /*SectionChannel.FullWidthSections + 1*/, heightVisibleEventsList /*SectionChannel.FullHeightSections + 1*/), "", BackgroundMidiEvents1); // MPTKGui.stylePanelGrayLight

                        Rect midiEventsVisibleRect = new Rect(startXEventsList, startYEventsList, widthVisibleEventsList /*+ WidthScrollVert*/, heightVisibleEventsList + HeightScrollHori); // heightFirstRowCmd to integrate the scrollbar
                        Rect midiEventsContentRect = new Rect(0, 0, SectionChannel.FullWidthSections, SectionChannel.FullHeightSections);

                        MainArea.SubArea[(int)AreaUI.AreaType.Channels].Position = midiEventsVisibleRect;

                        Vector2 scroller = GUI.BeginScrollView(midiEventsVisibleRect, ScrollerMidiEvents, midiEventsContentRect, false, false);
                        if (scroller.x != ScrollerMidiEvents.x)
                            Context.FollowEvent = false;
                        ScrollerMidiEvents = scroller;

                        DrawGridAndBannerChannels(widthVisibleEventsList, heightVisibleEventsList);
                        DrawBorderDragCell(widthVisibleEventsList, heightVisibleEventsList);
                        DrawMousePosition(widthVisibleEventsList, heightVisibleEventsList);
                        DrawMidiEvents(-1, startXEventsList, starty, startYEventsList, widthVisibleEventsList, heightVisibleEventsList);
                        DrawPlayingPosition(startXEventsList, widthVisibleEventsList);
                    }
                }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { GUI.EndScrollView(); }
        }



        private static void SetScrollXPosition(int positionSequencerPix, float widthVisibleEventsList)
        {
            ScrollerMidiEvents.x = (int)(positionSequencerPix - widthVisibleEventsList / 2f);
            // Set min / max valeur of the scroll
            if (ScrollerMidiEvents.x > SectionChannel.FullWidthSections - widthVisibleEventsList)
                ScrollerMidiEvents.x = SectionChannel.FullWidthSections - widthVisibleEventsList;
            if (ScrollerMidiEvents.x < 0)
                ScrollerMidiEvents.x = 0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="startx"></param>
        /// <param name="starty"></param>
        /// <param name="width">visible area</param>
        /// <param name="height">visible area</param>
        private void DrawMeasureLine(float startx, float starty, float width, float height)
        {
            try // Begin area measure line
            {
                GUILayout.BeginArea(new Rect(startx, starty, width, height), MPTKGui.stylePanelGrayLight);
                // Draw quarter/measure separator. Start from tick=0 but first separator start after the first quarter
                if (Context.QuarterWidth > 1f) // To avoid infinite loop
                {
                    int quarter = 0;
                    int measure = 1;
                    int quarterInBar = 1;
                    int indexSign = 0;

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


                    separatorQuarterRect.y = 0;
                    separatorQuarterRect.width = 1;
                    separatorQuarterRect.height = height;

                    for (float xQuarter = 0; xQuarter <= SectionChannel.FullWidthSections; xQuarter += Context.QuarterWidth)
                    {
                        long tick = sectionAll.ConvertPositionToTick(xQuarter);
                        tick = (long)((tick / (float)MidiFileWriter.DeltaTicksPerQuarterNote) + 0.5f) * MidiFileWriter.DeltaTicksPerQuarterNote;
                        indexSign = MPTKSignature.FindSegment(MidiFileWriter.MPTK_SignMap, tick, fromIndex: indexSign);
                        MPTKSignature signMap = MidiFileWriter.MPTK_SignMap[indexSign];
                        int numberBeatsMeasure = signMap.NumberBeatsMeasure * 4 / signMap.NumberQuarterBeat;
                        float measureWidth = Context.QuarterWidth * numberBeatsMeasure;
                        //Debug.Log($"xQuarter:{xQuarter} measure:{measure} quarterInBar:{quarterInBar} quarter:{quarter}");
                        separatorQuarterRect.x = (int)(xQuarter - ScrollerMidiEvents.x);

                        // Draw only on visible area, max width displayed + one measure to avoid cut too early
                        if (separatorQuarterRect.x >= -measureWidth && separatorQuarterRect.x < width)
                        {
                            // Draw Measure & quarter line 
                            // ---------------------------
                            if (quarterInBar == 1)
                            {
                                // Draw measure separator
                                if (Context.QuarterWidth > 5f)
                                    // Draw measure separator
                                    GUI.DrawTexture(separatorQuarterRect, SepBarText);
                            }
                            else
                            {
                                // Draw quarter separator
                                if (Context.QuarterWidth > 17f)
                                    GUI.DrawTexture(separatorQuarterRect, SepQuarterText);
                            }

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
                            if (Context.QuarterWidth > 30f)
                            {
                                // each quarter in measure
                                Rect rect = new Rect(separatorQuarterRect.x, 0, Context.QuarterWidth, height / 2f);
                                GUIContent content = new GUIContent(measure.ToString() + "." + quarterInBar.ToString(), tip);
                                GUI.Label(rect, content, MeasurelineStyle);
                            }
                            else if (Context.QuarterWidth > 2f)
                            {
                                if (quarterInBar == 1)
                                {
                                    // each measure
                                    Rect rect = new Rect(separatorQuarterRect.x, 0, Context.QuarterWidth * numberBeatsMeasure, height / 2f);
                                    GUIContent content = new GUIContent(measure.ToString(), tip);
                                    GUI.Label(rect, content, MeasurelineStyle);
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
                                    Rect timeQuarterRect = new Rect((int)(xQuarter - ScrollerMidiEvents.x - Context.QuarterWidth / 2f), height / 2f, Context.QuarterWidth, height / 2f);
                                    GUIContent content = BuildTextTime(quarter);
                                    if (content.text != null/* && timeQuarterRect.x > 0f*/)
                                        GUI.Label(timeQuarterRect, content, TimelineStyle);
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
                //TimelineStyle.contentOffset = Vector2.zero;
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { GUILayout.EndArea(); }

            //DisplayPerf("DrawMeasureLine");

        }
        private void DrawGridAndBannerChannels(float width, float height)
        {

            // Draw vertical lines, quarter/measure separator
            // ----------------------------------------------
            try
            {
                if (Context.QuarterWidth > 1f) // To avoid infinite loop
                {
                    int quarter = 0;
                    int measure = 1;
                    int quarterInBar = 1;
                    int indexSign = 0;

                    separatorQuarterRect.y = HEIGHT_CHANNEL_BANNER;
                    separatorQuarterRect.width = 1;
                    separatorQuarterRect.height = SectionChannel.FullHeightSections - HEIGHT_CHANNEL_BANNER;


                    for (float xQuarter = /*Context.QuarterWidth*/0; xQuarter <= SectionChannel.FullWidthSections; xQuarter += Context.QuarterWidth)
                    {
                        long tick = sectionAll.ConvertPositionToTick(xQuarter);
                        tick = (long)((tick / (float)MidiFileWriter.DeltaTicksPerQuarterNote) + 0.5f) * MidiFileWriter.DeltaTicksPerQuarterNote;
                        indexSign = MPTKSignature.FindSegment(MidiFileWriter.MPTK_SignMap, tick, fromIndex: indexSign);
                        int numberBeatsMeasure = MidiFileWriter.MPTK_SignMap[indexSign].NumberBeatsMeasure * 4 / MidiFileWriter.MPTK_SignMap[indexSign].NumberQuarterBeat;
                        separatorQuarterRect.x = 0;
                        bool draw = false;
                        // Draw only visible
                        if (xQuarter >= ScrollerMidiEvents.x && xQuarter < ScrollerMidiEvents.x + width)
                        {
                            separatorQuarterRect.x = xQuarter;
                            //= new Rect(xQuarter, HEIGHT_CHANNEL_BANNER, 1, SectionChannel.FullHeightSections - HEIGHT_CHANNEL_BANNER);
                            draw = true;
                        }
                        //Debug.Log($"FullWidthSections:{SectionChannel.FullWidthSections} xQuarter:{xQuarter} {MidiFileWriter.MPTK_NumberBeatsMeasure} {xQuarter % MidiFileWriter.MPTK_NumberBeatsMeasure}");
                        if (quarterInBar == 1)
                        //if (quarter >= numberBeatsMeasure/*MidiFileWriter.MPTK_NumberBeatsMeasure*/)
                        {
                            // Draw only on visible area
                            if (Context.QuarterWidth > 5f && draw)
                                GUI.DrawTexture(separatorQuarterRect, SepBarText);
                            // quarter = 1;
                        }
                        else
                        {
                            // Draw only on visible area
                            if (Context.QuarterWidth > 17f && draw)
                                GUI.DrawTexture(separatorQuarterRect, SepQuarterText);
                            //quarter++;
                        }

                        if (quarterInBar >= numberBeatsMeasure/*MidiFileWriter.MPTK_NumberBeatsMeasure*/)
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
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { }

            // For each section, draw horizontal lines and section banner
            // ----------------------------------------------------------
            try
            {
                float widthBannerVisible = width;//< SectionChannel.FullWidthSections  ? SectionChannel.FullWidthSections + 1 : width;
                //Debug.Log($"FullWidthSections:{SectionChannel.FullWidthSections} width:{width} "); 

                for (int iSection = 0; iSection < sectionAll.Sections.Length; iSection++)
                {
                    if (sectionAll.Sections[iSection] != null)
                    {
                        SectionCore section = (SectionCore)sectionAll.Sections[iSection];

                        // Display section banner
                        // ----------------------
                        string infoSection = null;
                        if (section is SectionMeta)
                        {
                            infoSection = $"Meta count: {section.Layouts[0].Count}  ";
#if DEBUG_EDITOR
                            infoSection += $" PositionSequencerPix:{PositionSequencerPix} MidiEventEdit.CurrentTickPosition:{CurrentTickPosition} LastMidiEvent:{LastMidiEvent?.Tick} LastNoteOnEvent:{LastNoteOnEvent?.Tick} CellHeight:{Context.CellHeight} MouseAction:{MouseAction}";
#endif
                        }
                        else if (section is SectionChannel)
                        {
                            infoSection = $"Channel: {iSection}    Preset: {section.Layouts[(int)Layout.EnumType.Preset].Count}   Note: {section.Layouts[(int)Layout.EnumType.Note].Count}";
#if DEBUG_EDITOR
                            infoSection += $"  Layout Note:{section.Layouts[1].BegY} {section.Layouts[1].Height} QuarterWidth:{Context.QuarterWidth} CellHeight:{Context.CellHeight} MouseAction:{MouseAction}";
#endif
                        }
                        Vector2 size = ChannelBannerStyle.CalcSize(new GUIContent(infoSection));
                        Rect channelBannerRect = new Rect(ScrollerMidiEvents.x, section.BegSection, widthBannerVisible, HEIGHT_CHANNEL_BANNER);

                        // Display banner centered on the visible area
                        ChannelBannerStyle.contentOffset = new Vector2(((widthBannerVisible - size.x) / 2f), 0);
                        GUI.Box(channelBannerRect, infoSection, ChannelBannerStyle);
                        ChannelBannerStyle.contentOffset = Vector2.zero;

                        rectSectionBtBanner.y = section.BegSection + 2;
                        rectSectionBtBanner.x = ScrollerMidiEvents.x;
                        if (GUI.Button(rectSectionBtBanner, Context.SectionOpen[section.IndexSection] ? LabelSectionOpen : LabelSectionClose, MPTKGui.ButtonSmall))
                            Context.SectionOpen[section.IndexSection] = !Context.SectionOpen[section.IndexSection];

                        // Mute only on channel section
                        if (section is SectionChannel)
                        {
                            rectSectionBtBanner.x += rectSectionBtBanner.width;
                            if (GUI.Button(rectSectionBtBanner, Context.SectionMute[section.IndexSection] ? LabelSectionMute : LabelSectionUnmute, MPTKGui.ButtonSmall))
                            {
                                Context.SectionMute[section.IndexSection] = !Context.SectionMute[section.IndexSection];
                                SetMuteChannel();
                            }

                            rectSectionBtBanner.x += rectSectionBtBanner.width;
                            if (GUI.Button(rectSectionBtBanner, LabelSectionSolo, MPTKGui.ButtonSmall))
                            {
                                Context.SetSectionMute(true);
                                Context.SectionMute[section.IndexSection] = false;
                                SetMuteChannel();
                            }
                        }
                        // Draw separator between each preset line but not the first
                        // ---------------------------------------------------------
                        if (Context.SectionOpen[section.IndexSection] && Context.QuarterWidth > 5f)
                        {
                            foreach (Layout layout in section.Layouts)
                            {
                                for (float y = layout.BegY + Context.CellHeight; y <= layout.EndY; y += Context.CellHeight)
                                {
                                    Rect rect = new Rect(ScrollerMidiEvents.x, y, widthBannerVisible, 1);
                                    GUI.DrawTexture(rect, layout.Type == Layout.EnumType.Preset || layout.Type == Layout.EnumType.Meta ? SepPresetTexture : SepNoteTexture); // green or blue
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { }
            //DisplayPerf("DrawGrid");

        }

        /// <summary>
        /// build and return time
        /// Use a static to avoid realloc each time
        /// </summary>
        /// <param name="quarter"></param>
        /// <returns></returns>
        private GUIContent BuildTextTime(int quarter)
        {
            int tick = quarter * MidiFileWriter.DeltaTicksPerQuarterNote;
            int indexTempo = 0;
            double realTime = 0;

            try
            {
                //! [ExampleFindTempoMap]
                indexTempo = MPTKTempo.FindSegment(MidiFileWriter.MPTK_TempoMap, tick, fromIndex: 0);
                realTime = MidiFileWriter.MPTK_TempoMap[indexTempo].CalculateTime(tick) / 1000f / Player.MPTK_Speed;
                //! [ExampleFindTempoMap]
            }
            catch
            {
                Debug.LogWarning($"Issue with real time calculation indexTempo={indexTempo} tick={tick}");
            }
            dynContent.text = null;

            if (Context.DisplayTime == 0)
            {
                dynContent.text = tick.ToString();
            }
            else
            {
                if (Context.DisplayTime == 1)
                {
                    //dynContent.text = (MidiFileWriter.MPTK_Pulse * tick / 1000f).ToString("F2");
                    dynContent.text = realTime.ToString("F2");
                }
                else if (Context.DisplayTime == 2)
                {
                    //double timeSecond = MidiFileWriter.MPTK_Pulse * tick / 1000d;
                    TimeSpan timePos = TimeSpan.FromSeconds(realTime);
                    if (realTime < 60d)
                        dynContent.text = string.Format("{0}.{1:000}", timePos.Seconds, timePos.Milliseconds);
                    else if (realTime < 3600d)
                        dynContent.text = string.Format("{0}:{1}.{2:000}", timePos.Minutes, timePos.Seconds, timePos.Milliseconds);
                    else
                        dynContent.text = string.Format("{0}:{1}:{2}.{3:000}", timePos.Hours, timePos.Minutes, timePos.Seconds, timePos.Milliseconds);
                }
            }
            dynContent.tooltip = $"Quarter:{quarter}\n";
            dynContent.tooltip += $"Tick:{tick}\n";
            dynContent.tooltip += $"Time:{realTime.ToString("F2")} sec.";
            return dynContent;
        }

        private void DrawKeyboard(float startx, float starty, float width, float height)
        {
            const float HEIGHT_LIMITE = 11f;
            GUIStyle keyLabelStyle;
            Texture keyDrawTexture;

            //Color savedColor = GUI.color;
            try // try keyboard
            {
                GUILayout.BeginArea(new Rect(startx, starty, width, height));

                // Draw white keys in first then black keys to overlap white keys ... if section is Channel
                MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].SubArea.Clear();
                MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].SubArea.Clear();
                for (int isSharp = 0; isSharp <= 1; isSharp++)
                {
                    int subZone = isSharp == 0 ? (int)AreaUI.AreaType.WhiteKeys : (int)AreaUI.AreaType.BlackKeys;

                    Array.ForEach(sectionAll.Sections, section =>
                    {
                        if (section != null)
                        {
                            if (section is SectionMeta)
                            {
                                SectionMeta sectionMeta = (SectionMeta)section;
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
                                }
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
                                        if (isSharp == 0) // no black key for drum
                                        {
                                            int keyValue = section.Layouts[(int)Layout.EnumType.Note].Higher;
                                            for (float y = section.Layouts[(int)Layout.EnumType.Note].BegY; keyValue >= section.Layouts[(int)Layout.EnumType.Note].Lower; y += Context.CellHeight, keyValue--)
                                            {
                                                float yKey = y - ScrollerMidiEvents.y;
                                                float hKey = Context.CellHeight;
                                                float wKey = width;
                                                keyLabelStyle = WhiteKeyLabelStyle;
                                                keyDrawTexture = WhiteKeyDrawTexture;

                                                if (yKey + hKey <= 0 || yKey >= height)
                                                    // out of the zone
                                                    continue;

                                                // Build key rectPositionSequencer for the button
                                                Rect keyRect = new Rect(0, yKey, wKey, hKey);

                                                // Display all label if heightFirstRowCmd > 11 or only C note
                                                string label = Context.CellHeight >= HEIGHT_LIMITE ? /*keyValue.ToString() + " " +*/ HelperNoteLabel.LabelPercussion(keyValue) + " " : "";

                                                //Debug.Log($"isSharp:{isSharp} subZone:{subZone} keyValue:{keyValue} yKey:{yKey}");
                                                MainArea.SubArea[subZone].SubArea.Add(new AreaUI()
                                                {
                                                    Position = new Rect(keyRect.x + startx, keyRect.y + starty, keyRect.width, keyRect.height),
                                                    Channel = section.IndexSection,
                                                    Value = keyValue
                                                });

                                                GUI.DrawTexture(keyRect, keyDrawTexture);
                                                GUI.DrawTexture(keyRect, keyDrawTexture, ScaleMode.StretchToFill, false, 0f, Color.gray, borderWidth: 1f, borderRadius: 2f);

                                                keyLabelStyle.contentOffset = Vector2.zero;

                                                if (label.Length > 0)
                                                    GUI.Label(keyRect, label, keyLabelStyle);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //
                                    // Draw keys
                                    // ---------
                                    if (Context.SectionOpen[section.IndexSection])
                                    {
                                        int keyValue = section.Layouts[(int)Layout.EnumType.Note].Higher;
                                        for (float y = section.Layouts[(int)Layout.EnumType.Note].BegY; keyValue >= section.Layouts[(int)Layout.EnumType.Note].Lower; y += Context.CellHeight, keyValue--)
                                        {
                                            if (isSharp == 0 && !HelperNoteLabel.IsSharp(keyValue) || isSharp == 1 && HelperNoteLabel.IsSharp(keyValue))
                                            {
                                                float yKey = y - ScrollerMidiEvents.y;
                                                float hKey = Context.CellHeight - 1f;
                                                float wKey = width;
                                                if (HelperNoteLabel.IsSharp(keyValue))
                                                {
                                                    keyLabelStyle = BlackKeyLabelStyle;
                                                    keyDrawTexture = BlackKeyDrawTexture;
                                                }
                                                else
                                                {
                                                    keyLabelStyle = WhiteKeyLabelStyle;
                                                    keyDrawTexture = WhiteKeyDrawTexture;
                                                }
                                                if (isSharp == 0)
                                                {
                                                    // Make higher keys for keys with a sharp above: C, D, F, G, A
                                                    if (keyValue != section.Layouts[(int)Layout.EnumType.Note].Higher && HelperNoteLabel.IsSharp(keyValue + 1))
                                                    {
                                                        yKey -= Context.CellHeight / 2f;
                                                        hKey += Context.CellHeight / 2f;
                                                    }
                                                    // Make higher keys for keys with a sharp below: D, E, G, A, B
                                                    if (keyValue != section.Layouts[(int)Layout.EnumType.Note].Lower && HelperNoteLabel.IsSharp(keyValue - 1))
                                                    {
                                                        hKey += Context.CellHeight / 2f;
                                                    }
                                                    // Label offset Only for
                                                    //  white keys not at the begin and
                                                    //  not at the rectClear position in the keyboard
                                                    if (keyValue != section.Layouts[(int)Layout.EnumType.Note].Higher &&
                                                        keyValue != section.Layouts[(int)Layout.EnumType.Note].Lower && Context.CellHeight >= HEIGHT_LIMITE)
                                                        switch (HelperNoteLabel.NoteNumber(keyValue))
                                                        {
                                                            case 0: keyLabelStyle.contentOffset = new Vector2(0, 5); break;   // C
                                                            case 4: keyLabelStyle.contentOffset = new Vector2(0, -4); break;  // E
                                                            case 5: keyLabelStyle.contentOffset = new Vector2(0, 5); break;   // F
                                                            case 11: keyLabelStyle.contentOffset = new Vector2(0, -4); break; // B
                                                        }
                                                }
                                                else
                                                    // Black keys: smaller and label always v-centered
                                                    wKey = width * 0.66f;

                                                if (yKey + hKey <= 0 || yKey >= height)
                                                {
                                                    // out of the zone
                                                    //Debug.Log($"out isSharp:{isSharp} subZone:{subZone} keyValue:{keyValue} yKey:{yKey}");
                                                    continue;
                                                }

                                                // Build key rectPositionSequencer for the button
                                                Rect keyRect = new Rect(0, yKey, wKey, hKey);

                                                // Display all label if heightFirstRowCmd > 11 or only C note
                                                string label = Context.CellHeight >= HEIGHT_LIMITE || HelperNoteLabel.NoteNumber(keyValue) == 0 ? HelperNoteLabel.LabelC4FromMidi(keyValue) + " " : "";

                                                //Debug.Log($"isSharp:{isSharp} subZone:{subZone} keyValue:{keyValue} yKey:{yKey}");
                                                MainArea.SubArea[subZone].SubArea.Add(new AreaUI()
                                                {
                                                    Position = new Rect(keyRect.x + startx, keyRect.y + starty/* + ScrollerMidiEvents.y*/, keyRect.width, keyRect.height),
                                                    Channel = section.IndexSection,
                                                    Value = keyValue
                                                });


                                                GUI.DrawTexture(keyRect, keyDrawTexture);
                                                GUI.DrawTexture(keyRect, keyDrawTexture, ScaleMode.StretchToFill, false, 0f, Color.gray, borderWidth: 1f, borderRadius: 2f);

                                                if (label.Length > 0)
                                                    GUI.Label(keyRect, label, keyLabelStyle);

                                                keyLabelStyle.contentOffset = Vector2.zero;
                                            }
                                        }
                                    }
                                }
                                //
                                // Draw preset area and section name
                                // ---------------------------------
                                if (isSharp == 0) // only one time !
                                {
                                    // Section name
                                    GUI.Label(new Rect(0, section.BegSection - ScrollerMidiEvents.y, width, HEIGHT_CHANNEL_BANNER), $"Channel {section.IndexSection}", ChannelBannerStyle);
                                    float yPreset = section.Layouts[(int)Layout.EnumType.Preset].BegY - ScrollerMidiEvents.y;
                                    //if (section.Presets.Count > 2) Debug.Log("");
                                    if (Context.SectionOpen[section.IndexSection])

                                        foreach (SectionChannel.PresetSet sectionPreset in (section as SectionChannel).Presets)
                                        {
                                            Rect presetRect = new Rect(0, yPreset, width, Context.CellHeight);
                                            // Select display time format

                                            MPTKGui.ComboBox(presetRect, ref sectionPreset.PopupPreset, "{Label}", PopupItemsPreset, false,
                                               action: delegate (int index)
                                               {
                                                   int newPresetValue = PopupItemsPreset[index].Value;
                                                   //Debug.Log($"index:{index} {PopupItemsPreset[index].Caption} preset:{meta.Value} to:{newPresetValue}");
                                                   MidiEvents.ForEach(m =>
                                                   {
                                                       if (m.Channel == section.IndexSection && m.Command == MPTKCommand.PatchChange && m.Value == sectionPreset.Value)
                                                       {
                                                           m.Value = newPresetValue;/* Debug.Log(m);*/
                                                       }
                                                   });
                                                   sectionPreset.Value = newPresetValue;
                                                   Repaint();
                                               },
                                               style: PresetButtonStyle, widthPopup: 150, option: null);
                                            sectionPreset.PopupPreset.SelectedIndex = sectionPreset.Value;
                                            yPreset += Context.CellHeight;
                                        }
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
            finally { GUILayout.EndArea();/* GUI.color = savedColor;*/ }

            //DisplayPerf("DrawKeyboard");
        }

        private void DrawBorderDragCell(float width, float height)
        {
            if (SelectedEvent != null)
            {

                if (MouseAction == enAction.MoveNote)// || dragAction == MouseAction.LengthLeftNote)
                {
                    float cellX;
                    //float x = (float)(SelectedEvent.Tick + deltaTick) * CellWidth / (float)LoadedMidi.MPTK_DeltaTicksPerQuarterNote ;

                    // Draw vertical line at note-on
                    cellX = ((float)SelectedEvent.Tick / (float)MidiFileWriter.DeltaTicksPerQuarterNote) * Context.QuarterWidth;
                    //Debug.Log($"cellX:{cellX} ScrollerMidiEvents.x:{ScrollerMidiEvents.x}");
                    Rect rect = new Rect(cellX, ScrollerMidiEvents.y, 1, height);
                    GUI.DrawTexture(rect, SepDragEventText);

                    // Draw vertical line at note-off
                    cellX = ((float)(SelectedEvent.Tick + SelectedEvent.Length) / (float)MidiFileWriter.DeltaTicksPerQuarterNote) * Context.QuarterWidth;
                    rect = new Rect(cellX, ScrollerMidiEvents.y, 1, height);
                    GUI.DrawTexture(rect, SepDragEventText);
                }
            }

            //DisplayPerf("DrawDragNote");

            // Draw always left border
            //{
            //    float x = ScrollerMidiEvents.x;
            //    Rect rectTest = new Rect(startx + x, 0, 1, heightFirstRowCmd);
            //    GUI.DrawTexture(rectTest, separatorChannelTexture);
            //    // Darw always at end of first measure
            //    x = 4 * CellQuarterWidth + ScrollerMidiEvents.x;
            //    rectTest = new Rect(startx + x, 0, 1, heightFirstRowCmd);
            //    GUI.DrawTexture(rectTest, separatorChannelTexture);
            //}
        }
        private void DrawPlayingPosition(float startXEventsList, float widthVisibleEventsList)
        {
            // Draw current position playing
            if (/*PositionSequencerPix > 0 &&*/ Context.FollowEvent)// || !Player.MPTK_IsPlaying)
            {
                SetScrollXPosition(PositionSequencerPix, widthVisibleEventsList);
            }

            if (MouseAction != enAction.MoveNote)
            {
                if (Player.MPTK_IsPlaying)
                {
                    // draw position from the current playing position
                    rectPositionSequencer.x = PositionSequencerPix;
                    rectPositionSequencer.height = SectionChannel.FullHeightSections;
                    GUI.DrawTexture(rectPositionSequencer, SepPlayingPositionTexture);
                }
                else
                {
                    // draw position from the current tick
                    rectPositionSequencer.x = (int)sectionAll.ConvertTickToPosition(CurrentTickPosition);
                    rectPositionSequencer.height = SectionChannel.FullHeightSections;
                    GUI.DrawTexture(rectPositionSequencer, SepSelectedPositionTexture);
                }
            }
            // Draw loop position
            if (Context.LoopResume > 0)
            {
                rectPositionLoopStart.x = sectionAll.ConvertTickToPosition(Context.LoopResume); ;
                rectPositionLoopStart.height = SectionChannel.FullHeightSections;
                GUI.DrawTexture(rectPositionLoopStart, SepLoopTexture);
            }
            if (Context.LoopEnd > 0)
            {
                rectPositionLoopEnd.x = sectionAll.ConvertTickToPosition(Context.LoopEnd); ;
                rectPositionLoopEnd.height = SectionChannel.FullHeightSections;
                GUI.DrawTexture(rectPositionLoopEnd, SepLoopTexture);
            }
            //Debug.Log($"x:{ScrollerMidiEvents.x} FullWidthSections:{SectionChannel.FullWidthSections} startXEventsList:{startXEventsList} widthVisibleEventsList:{widthVisibleEventsList} PositionSequencerPix:{PositionSequencerPix}");
        }

        private void DrawMousePosition(float width, float height)
        {
            // Draw only when mouse is on MIDI events zone including time line
             if (Event.current.mousePosition.y + HEIGHT_TIMELINE > 0)
            {
                Rect rect;
                //Debug.Log(Event.current.mousePosition.y);
                // Draw mouse position vertical
                rect = new Rect(Event.current.mousePosition.x, ScrollerMidiEvents.y, 1, height);
                GUI.DrawTexture(rect, SepDragMouseText);

                // Draw mouse position horizontal
                rect = new Rect(ScrollerMidiEvents.x, Event.current.mousePosition.y, width, 1);
                GUI.DrawTexture(rect, SepDragMouseText);
            }
        }

        private void DrawMidiEvents(int indexSection, float startXEventsList, float startYLinePosition, float startYEventsList, float widthVisibleEventsList, float heightVisibleEventsList)
        {
            //GUILayout.BeginArea(new Rect(startXEventsList, startYLinePosition, widthVisibleEventsList, height), MPTKGui.stylePanelGrayLight);

            MainArea.SubArea[(int)AreaUI.AreaType.Channels].SubArea.Clear();
            // Foreach MIDI events on the current page
            // ---------------------------------------
            Array.ForEach(sectionAll.Sections, section =>
            {
                if (section != null && (indexSection == -1 || indexSection == section.IndexSection) && Context.SectionOpen[section.IndexSection])
                {
                    // Add an area for this section to hold cell with MIDI events
                    AreaUI channelArea = new AreaUI()
                    {
                        Position = new Rect(
                            startXEventsList,
                            startYEventsList + section.BegSection - ScrollerMidiEvents.y,
                            widthVisibleEventsList,
                            section.Height),
                        SubArea = new List<AreaUI>(),
                        areaType = AreaUI.AreaType.Channel,
                        Channel = section.IndexSection,
                    };
                    MainArea.SubArea[(int)AreaUI.AreaType.Channels].SubArea.Add(channelArea);

                    if (section.IndexSection == SectionAll.SECTION_META)
                    {
                        // For each MIDI event filter by section
                        foreach (MPTKEvent midiEvent in MidiEvents)
                        {
                            if (midiEvent.Command == MPTKCommand.MetaEvent)
                            {
                                try // display one row
                                {
                                    if (!DrawOneMidiEvent(startXEventsList, startYEventsList, widthVisibleEventsList, heightVisibleEventsList, section, midiEvent, channelArea))
                                    {
                                        //Debug.Log($"Outside the visible area, iSection {iSection}");
                                        break;
                                    }
                                }
                                catch (MaestroException ex)
                                {
                                    Debug.LogException(ex);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogException(ex);
                                    throw;
                                }
                                finally { }
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
                                try // display one row
                                {
                                    if (!DrawOneMidiEvent(startXEventsList, startYEventsList, widthVisibleEventsList, heightVisibleEventsList, section, midiEvent, channelArea))
                                    {
                                        //Debug.Log($"Outside the visible area, iSection {iSection}");
                                        break;
                                    }
                                }
                                catch (MaestroException ex)
                                {
                                    Debug.LogException(ex);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogException(ex);
                                    throw;
                                }
                                finally { }
                            }
                        }
                }
            });
            //DisplayPerf("DrawMidiEvents");
        }

        private bool DrawOneMidiEvent(float startXEventsList, float startYEventsList, float widthVisibleEventsList, float heightVisibleEventsList,
            SectionCore channelMidi, MPTKEvent midiEvent, AreaUI channelSubZone)
        {
            int index = midiEvent.Index;
            string cellText = "";
            float cellX = sectionAll.ConvertTickToPosition(midiEvent.Tick);
            float cellY;
            float cellW;
            float cellH = Context.CellHeight - 4f;

            if (cellX > ScrollerMidiEvents.x + widthVisibleEventsList)
            {
                // After the visible area, stop drawing all next iSection's notes
                //Debug.Log($"After the visible area, stop drawing all iSection's notes. IndexSection {midiEvent.IndexSection} Tick:{midiEvent.Tick} ScrollerMidiEvents.x:{ScrollerMidiEvents.x} width:{widthVisibleEventsList} cellX:{cellX}");
                return false;
            }
            Texture eventTexture = MidiNoteTexture; // default style
            switch (midiEvent.Command)
            {
                case MPTKCommand.NoteOn:
                    cellW = sectionAll.ConvertTickToPosition(midiEvent.Length);
                    if (midiEvent.Value < channelMidi.Layouts[(int)Layout.EnumType.Note].Lower ||
                        midiEvent.Value > channelMidi.Layouts[(int)Layout.EnumType.Note].Higher) return true;
                    cellY = channelMidi.Layouts[(int)Layout.EnumType.Note].BegY + (channelMidi.Layouts[(int)Layout.EnumType.Note].Higher - midiEvent.Value) * Context.CellHeight;
                    eventTexture = MidiNoteTexture;
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
                    eventTexture = MidiPresetTexture;
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
                    eventTexture = MidiPresetTexture;
                    if (cellW >= 20f)
                        cellText = $"{midiEvent.Value}";
                    break; ;
                case MPTKCommand.ChannelAfterTouch: return true;
                case MPTKCommand.KeyAfterTouch: return true;
                default: return true;
            }

            if (midiEvent == SelectedEvent)
                eventTexture = MidiSelectedTexture;

            //Debug.Log($"ScrollerMidiEvents:{ScrollerMidiEvents}  width:{widthVisibleEventsList} startYEventsList:{startYEventsList}  heightVisible:{heightVisibleEventsList} cellX:{cellX} cellY:{cellY} cellW:{cellW}");

            if (cellX + cellW < ScrollerMidiEvents.x)
            {
                // Before the visible area, go to next event
                //Debug.Log($"   Before the visible area, go to next event. IndexSection {midiEvent.IndexSection} Tick:{midiEvent.Tick} ScrollerMidiEvents.x:{ScrollerMidiEvents.x} width:{widthVisibleEventsList} cellX:{cellX}");
                return true;
            }

            if (cellY + Context.CellHeight < ScrollerMidiEvents.y)
            {
                // Above the visible area, go to next event
                //Debug.Log($"   Above the visible area, go to next event. IndexSection {midiEvent.IndexSection} cellY:{cellY} ScrollerMidiEvents.y:{ScrollerMidiEvents.y}  heightFirstRowCmd:{heightVisibleEventsList} CellHeight:{Sectx.CellHeight}");
                return true;
            }

            if (cellY > heightVisibleEventsList + ScrollerMidiEvents.y)
            {
                // Bellow visible area, go to next event
                //Debug.Log($"   Bellow the visible area, go to next event. IndexSection {midiEvent.IndexSection} cellY:{cellY} startYEventsList:{startYEventsList} ScrollerMidiEvents.y:{ScrollerMidiEvents.y} heightFirstRowCmd:{heightVisibleEventsList} Sectx.CellHeight:{Sectx.CellHeight}");
                return true;
            }

            // Minimum width to be able to select a MIDI event
            if (cellW < 12f) cellW = 12f;
            Rect cellRect = new Rect(cellX, cellY + 3f, cellW, cellH);
            //Debug.Log($"cellRect {cellRect} {eventTexture}");

            GUI.DrawTexture(cellRect, eventTexture);
            GUI.DrawTexture(cellRect, eventTexture, ScaleMode.StretchToFill, false, 0f, Color.gray, borderWidth: 1f, borderRadius: 0f);

            if (cellText.Length > 0)
                GUI.Label(cellRect, cellText, MPTKGui.LabelCenterSmall);

            AreaUI zoneCell = new AreaUI() { midiEvent = midiEvent, Position = cellRect, };
            // Shift cell position from scroller area to absolute position
            zoneCell.Position.x += startXEventsList - ScrollerMidiEvents.x;
            zoneCell.Position.y += startYEventsList - ScrollerMidiEvents.y;
            // Add cell with note to this area
            //Debug.Log("zoneCell " + zoneCell);
            channelSubZone.SubArea.Add(zoneCell);

            return true;
        }

        // Used for DebugAreaUI, no matter if alloc is not optimized
        void DrawRect(Rect rect, Color color, int innnerEdge)
        {
            Texture texture = MPTKGui.MakeTex(color, new RectOffset(innnerEdge, innnerEdge, innnerEdge, innnerEdge));
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, innnerEdge), texture); // top
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - innnerEdge, rect.width, innnerEdge), texture); // bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y + innnerEdge, innnerEdge, rect.height - 2 * innnerEdge), texture); // left
            GUI.DrawTexture(new Rect(rect.x + rect.width - innnerEdge, rect.y + innnerEdge, innnerEdge, rect.height - 2 * innnerEdge), texture); // right
        }

        private void EventManagement()
        {
            Event currentEvent = Event.current;
            //Debug.Log($"------- CurrentEvent  {currentEvent} --------");

            if (currentEvent.type == EventType.MouseDown)
                EventMouseDown(currentEvent);

            if (currentEvent.type == EventType.MouseUp)
                EventMouseUp();

            if (currentEvent.type == EventType.MouseDrag)
                EventMouseDrag(currentEvent);

            if (currentEvent.type == EventType.MouseMove)
                EventMouseMove(currentEvent);

            if (currentEvent.type == EventType.MouseUp & SelectedEvent != null)
            {
                NewDragEvent = null;
                //SelectedEvent = null;
                DragPosition = Vector2.zero;
                separatorDragVerticalBegin = Rect.zero;
                Repaint();
            }

            if (currentEvent.type == EventType.KeyDown)
            {
                EventKeyDown(currentEvent);

            }
            if (currentEvent.type == EventType.ScrollWheel)
            {
                ToolTipMidiEditor.Clear();

                if (currentEvent.control)
                    EventWheelZoom(currentEvent);
                else if (currentEvent.shift)
                    EventWheelScrollH(currentEvent);
            }
        }

        private void EventMouseDown(Event currentEvent)
        {
            //Debug.Log($"Mouse Down {currentEvent.mousePosition}");
            // Mouse down on the keyboard ?
            KeyPlaying = PlayKeyboard(currentEvent);
            if (KeyPlaying == null)
            {
                // Perhaps on a a MIDI event displayed ?
                MPTKEvent mptkEvent = FindCellMouse(currentEvent);
                if (mptkEvent != null)
                {
                    if (MouseAction == enAction.DeleteNote)
                    {
                        DeleteEventFromMidiFileWriter(mptkEvent);
                    }
                    else
                    {
                        // Event found under the mouse, display it
                        //Debug.Log($"EventMouseDown selectedInFilterList MIDIEvent  {currentEvent.mousePosition} --> {mptkEvent} {MouseAction}");
                        SelectedEvent = NewDragEvent = mptkEvent;
                        RefreshPadCmdMidi(NewDragEvent);
                    }
                    Repaint();
                }
                else if (MouseAction == enAction.CreateNote)
                {
                    // Create a MIDI event
                    AreaUI channelsArea = MainArea.SubArea[(int)AreaUI.AreaType.Channels];
                    if (channelsArea.Position.Contains(currentEvent.mousePosition))
                    {
                        foreach (AreaUI channelZone in channelsArea.SubArea)
                        {
                            if (channelZone.Position.Contains(currentEvent.mousePosition))
                            {
                                EventCreateMidiEvent(currentEvent, channelZone);
                                break;
                            }
                        }
                    }
                    Repaint();
                }

            }
        }


        private void EventCreateMidiEvent(Event currentEvent, AreaUI area)
        {
            SectionCore section = sectionAll.Sections[area.Channel];
            int line = -1;
            float xMouseCorrected = currentEvent.mousePosition.x + ScrollerMidiEvents.x - X_CORR_SECTION;
            long tick = sectionAll.ConvertPositionToTick(xMouseCorrected);
            // position begin header section
            float yMouseCorrected = currentEvent.mousePosition.y + ScrollerMidiEvents.y - Y_CORR_SECTION;
            //Debug.Log($"Create MIDI  IndexSection:{area.IndexSection}  x:{xMouseCorrected} y:{yMouseCorrected} Tick:{tick} AllLayout:{section.AllLayout.BegY} NoteLayout:{section.NoteLayout.BegY} PresetLayout:{section.PresetLayout.BegY}");

            if (yMouseCorrected >= section.BegSection && yMouseCorrected < section.Layouts[0].BegY)
            {
                // Above header section
                Debug.Log($"Header channel zone  y:{yMouseCorrected} Channel:{area.Channel} AllLayout:{section.Layouts[0]} ");
                SelectedEvent = null;
            }
            else
            {
                MPTKEvent newEvent = null;
                Array.ForEach(section.Layouts, layout =>
                {
                    if (yMouseCorrected >= layout.BegY && yMouseCorrected <= layout.EndY)
                    {
                        line = (int)((yMouseCorrected - layout.BegY) / Context.CellHeight);
                        if (layout.Type == Layout.EnumType.Preset)
                        {
                            // Above preset section, add a section change
                            Debug.Log($"Preset zone  y:{yMouseCorrected} Channel:{area.Channel} Line:{line} Preset:{((SectionChannel)section).Presets[line].Value} PresetLayout:{layout}");
                            newEvent = new MPTKEvent()
                            {
                                Track = 1,
                                Command = MPTKCommand.PatchChange,
                                Value = ((SectionChannel)section).Presets[line].Value,
                            };
                        }
                        else if (layout.Type == Layout.EnumType.Note)
                        {
                            // Above notes section, add a note  
                            Debug.Log($"Note zone  y:{yMouseCorrected} Channel:{area.Channel} line:{line} IndexSection:{section.IndexSection} ");
                            newEvent = new MPTKEvent()
                            {
                                Track = 1,
                                Command = MPTKCommand.NoteOn,
                                Value = layout.Higher - line,
                                // Duration will be calculate with RefreshMidi() 
                                Length = MidiEventEdit.Length,
                                Velocity = MidiEventEdit.Velocity
                            };
                        }
                        else if (layout.Type == Layout.EnumType.Meta)
                        {
                            Debug.Log($"Meta zone  y:{yMouseCorrected} Channel:{area.Channel} line:{line} IndexSection:{section.IndexSection} ");
                            if (line == 0)
                                newEvent = new MPTKEvent()
                                {
                                    Track = 0,
                                    Command = MPTKCommand.MetaEvent,
                                    Meta = MPTKMeta.SetTempo,
                                    /// MPTKEvent#Value contains new Microseconds Per Beat Note\n
                                    Value = MPTKEvent.BeatPerMinute2QuarterPerMicroSecond(MidiEventEdit.TempoBpm),
                                };
                            else if (line == 1)
                                newEvent = new MPTKEvent()
                                {
                                    Track = 0,
                                    Command = MPTKCommand.MetaEvent,
                                    Meta = MPTKMeta.TextEvent,
                                    Info = "",
                                };
                        }
                    }
                });

                if (newEvent != null)
                {
                    // Common settings and add event
                    newEvent.Tick = tick;
                    newEvent.Channel = area.Channel;
                    InsertEventIntoMidiFileWriter(newEvent);
                    RefreshPadCmdMidi(newEvent);
                }
            }
        }

        private void EventMouseUp()
        {
            if (KeyPlaying != null)
            {
                if (KeyPlaying.Voices != null && KeyPlaying.Voices.Count > 0)
                    if (KeyPlaying.Voices[0].synth != null)
                        KeyPlaying.Voices[0].synth.MPTK_PlayDirectEvent(new MPTKEvent() { Command = MPTKCommand.NoteOff, Channel = KeyPlaying.Channel, Value = KeyPlaying.Value });
                KeyPlaying = null;
            }
            MouseAction = enAction.None;
        }

        private void EventMouseDrag(Event currentEvent)
        {
            if (NewDragEvent != null)
            {
                //Debug.Log($"******* New Event drag:  {NewDragEvent.Tick} {NewDragEvent.Value} ");
                LastMousePosition = currentEvent.mousePosition;
                DragPosition = Vector2.zero;
                SelectedEvent = NewDragEvent;
                InitialTick = SelectedEvent.Tick;
                InitialValue = SelectedEvent.Value;
                InitialDurationTick = SelectedEvent.Length;
                NewDragEvent = null;
            }
            if (SelectedEvent != null)
            {
                SectionCore section = sectionAll.Sections[SelectedEvent.Channel];
                if (section != null)
                {
                    ToolTipMidiEditor.Clear();

                    DragPosition += currentEvent.mousePosition - LastMousePosition;
                    LastMousePosition = currentEvent.mousePosition;
                    long deltaTick;
                    switch (MouseAction)
                    {
                        case enAction.LengthLeftNote:
                            deltaTick = Convert.ToInt64(DragPosition.x / Context.QuarterWidth * MidiFileWriter.DeltaTicksPerQuarterNote);
                            deltaTick = CalculateQuantization(deltaTick);
                            SelectedEvent.Length = InitialDurationTick - (int)deltaTick; // sign - because the delta is negative and we need to increase the duration
                            SelectedEvent.Tick = InitialTick + deltaTick;
                            //Debug.Log($"    Event param:  DragPosition:{DragPosition} QuarterWidth:{Context.QuarterWidth}  durationTicks:{SelectedEvent.Length} deltaTick:{deltaTick}");
                            // Sort MIDI events, calculate tempo map. For each MIDI events, calculate realtime, duration, measure, beat, index
                            RefreshMidi();
                            RefreshPadCmdMidi(SelectedEvent);
                            Repaint();
                            break;

                        case enAction.LengthRightNote:
                            deltaTick = Convert.ToInt64(DragPosition.x / Context.QuarterWidth * MidiFileWriter.DeltaTicksPerQuarterNote);
                            deltaTick = CalculateQuantization(deltaTick);
                            int length = InitialDurationTick + (int)deltaTick;
                            SelectedEvent.Length = (int)CalculateQuantization((int)length);
                            //Debug.Log($"    Event param:  DragPosition:{DragPosition} QuarterWidth:{Context.QuarterWidth}  durationTicks:{SelectedEvent.Length} deltaTick:{deltaTick}");
                            // Sort MIDI events, calculate tempo map. For each MIDI events, calculate realtime, duration, measure, beat, index
                            RefreshMidi();
                            RefreshPadCmdMidi(SelectedEvent);
                            Repaint();
                            break;

                        case enAction.MoveNote:
                            // Vertical move only for note
                            // ---------------------------
                            if (SelectedEvent.Command == MPTKCommand.NoteOn)
                            {
                                //Debug.Log($"    Event param:  {currentEvent.mousePosition} {currentEvent.delta} lastDragYPosition:{DragPosition} CellWidth:{CellWidth}  Sectx.CellHeight:{Sectx.CellHeight} ");
                                //Debug.Log($"        Change MIDI event DragPosition:{DragPosition} CellHeight:{Sectx.CellHeight} CellQuarterWidth:{CellQuarterWidth} to {LastMousePosition} ");
                                SelectedEvent.Value = Mathf.Clamp(InitialValue - Convert.ToInt32(DragPosition.y / Context.CellHeight), 0, 127);
                                section.Layouts[1].SetLowerHigherNote(SelectedEvent.Value);
                            }
                            // Horizontal
                            // ----------
                            deltaTick = Convert.ToInt32(DragPosition.x / Context.QuarterWidth * MidiFileWriter.DeltaTicksPerQuarterNote);
                            SelectedEvent.Tick = CalculateQuantization(InitialTick + deltaTick);
                            Context.Modified = true;

                            if (SelectedEvent.Tick < 0)
                                SelectedEvent.Tick = 0;

                            if (SelectedEvent.Tick != InitialTick)
                                // Sort MIDI events, calculate tempo map. For each MIDI events, calculate realtime, duration, measure, beat, index
                                RefreshMidi();

                            if (SelectedEvent.Value != InitialValue || SelectedEvent.Tick != InitialTick)
                            {
                                RefreshPadCmdMidi(SelectedEvent);
                                Repaint();
                            }
                            break;
                    }
                }
            }
        }


        private void EventMouseMove(Event currentEvent)
        {
            ToolTipMidiEditor.Clear();
            CurrentMouseCursor = MouseCursor.Arrow;
            FindCellMouse(currentEvent);
            if (CurrentMouseCursor == MouseCursor.Arrow)
                MouseAction = enAction.None;

            // ------------------------ TU search
            //float xMouseCorrected = currentEvent.mousePosition.x + ScrollerMidiEvents.x - X_CORR_SECTION;
            //long tick_raw = sectionAll.ConvertPositionToTick(xMouseCorrected);
            //long tick_quantized;
            //tick_quantized = CalculateQuantization(tick_raw);

            // FOR TESTING
            //int index = MidiLoad.MPTK_SearchEventFromTick(MidiEvents, tick_quantized);
            //if (index >= 0)
            //    Debug.Log($"Find at position {index} for tick quantized {tick_quantized} raw:{tick_raw} {MidiEvents[index]}");
            //else
            //    Debug.Log($"Not Find at position {index} for tick quantized {tick_quantized} raw:{tick_raw} ");

            // -------------------------- 

            LastMouseMove = currentEvent;
            Repaint();
        }

        private void EventKeyDown(Event currentEvent)
        {
            //Debug.Log("Ev.KeyDown: " + e);
            if (currentEvent.keyCode == KeyCode.Space || currentEvent.keyCode == KeyCode.DownArrow || currentEvent.keyCode == KeyCode.UpArrow ||
                currentEvent.keyCode == KeyCode.End || currentEvent.keyCode == KeyCode.Home)
            {
                GUI.changed = true;
                Repaint();
            }
            if (currentEvent.keyCode == KeyCode.Delete)
            {
                if (SelectedEvent != null)
                {
                    DeleteEventFromMidiFileWriter(SelectedEvent);
                    SelectedEvent = null;
                    currentEvent.Use();
                }
            }

            if (currentEvent.keyCode == KeyCode.KeypadEnter || currentEvent.keyCode == KeyCode.Return)
            {
                if (SelectedEvent != null)
                {
                    ApplyMidiChangeFromPad();
                }
                else
                    CreateEventFromPad();
            }

            if (currentEvent.keyCode == KeyCode.Escape)
                SelectedEvent = null;

#if DEBUG_EDITOR
            if (currentEvent.modifiers == EventModifiers.Alt && currentEvent.keyCode == KeyCode.Z)
            {
                DebugDisplayCell = !DebugDisplayCell;
                Repaint();
                Debug.Log($"Debug MainZone {DebugDisplayCell}");
            }

            if (currentEvent.modifiers == EventModifiers.Alt && currentEvent.keyCode == KeyCode.T)
            {
                if (MidiFileWriter != null)
                {
                    MidiFileWriter.MPTK_LogTempoMap();
                    MidiFileWriter.MPTK_LogSignMap();
                }
            }

            if (currentEvent.modifiers == EventModifiers.Alt && currentEvent.keyCode == KeyCode.H)
            {
                Debug.Log("--------------------------------------");
                Debug.Log($"Loaded skin {GUI.skin.name}");
                Debug.Log($"QuarterWidth:{Context.QuarterWidth} CellHeight:{Context.CellHeight} ");
                Debug.Log($"startXEventsList:{startXEventsList} widthVisibleEventsList: {widthVisibleEventsList}");
                Debug.Log($"startYEventsList:{startYEventsList} heightVisibleEventsList:{heightVisibleEventsList}");
                Debug.Log($"FullWidthChannelZone:{SectionChannel.FullWidthSections} FullHeightChannelsZone:{SectionChannel.FullHeightSections}");
                if (LastMidiEvent != null) Debug.Log($"LastMidiEvent:\t\t{LastMidiEvent}");
                if (LastNoteOnEvent != null) Debug.Log($"LastNoteOnEvent:\t{LastNoteOnEvent}");
                if (LastMouseMove != null) Debug.Log($"LastMouseMove:\t{LastMouseMove}");
                if (SelectedEvent != null) Debug.Log($"SelectedEvent:\t{SelectedEvent}");
                if (NewDragEvent != null) Debug.Log($"NewDragEvent:\t{NewDragEvent}");
                DebugMainAreaAndSection();
            }
#endif
        }

        private void EventWheelZoom(Event currentEvent)
        {
            // change cell width only if no alt
            if (!currentEvent.alt)
            {
                Context.QuarterWidth -= currentEvent.delta.y / 2.5f;
                // Clamp and keep only one decimal
                Context.QuarterWidth = Mathf.Round(Mathf.Clamp(Context.QuarterWidth, 2f, 200f) * 10f) / 10f;
            }
            // change cell heightFirstRowCmd only if no shift
            if (!currentEvent.shift)
            {
                Context.CellHeight -= currentEvent.delta.y / 5f;
                // Clamp and keep only one decimal
                Context.CellHeight = Mathf.Round(Mathf.Clamp(Context.CellHeight, 5f, 40f) * 10f) / 10f;
            }
            currentEvent.Use();
            Repaint();
        }
        private void EventWheelScrollH(Event currentEvent)
        {
            //Debug.Log($"{currentEvent.delta.y / 3f}");

            CurrentTickPosition -= (long)((MidiFileWriter != null ? MidiFileWriter.DeltaTicksPerQuarterNote * 4f : 1000f) * currentEvent.delta.y / 3f);
            if (CurrentTickPosition < 0)
                CurrentTickPosition = 0;
            else
            {
                long lastTick = LastMidiEvent != null ? LastMidiEvent.Tick : 0;
                if (CurrentTickPosition > lastTick)
                    CurrentTickPosition = lastTick;
            }

            PositionSequencerPix = (int)sectionAll.ConvertTickToPosition(CurrentTickPosition);
            SetScrollXPosition(PositionSequencerPix, widthVisibleEventsList);
            currentEvent.Use();
            Repaint();
        }

        private MPTKEvent PlayKeyboard(Event currentEvent)
        {
            MPTKEvent playMPTK = null;
            if (MainArea.Position.Contains(currentEvent.mousePosition))
            {
                //Debug.Log($"MainZone");
                // Black keys are on top of white key, so check only white key
                if (MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position.Contains(currentEvent.mousePosition))
                {
                    //Debug.Log($"    KeyZone  {currentEvent.mousePosition} --> {MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position}");
                    AreaUI foundZone = null;
                    foreach (AreaUI zone in MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].SubArea)
                    {
                        if (zone.Position.Contains(currentEvent.mousePosition))
                        {
                            //Debug.Log($"    Black Key  {currentEvent.mousePosition} --> {zone.Position} {zone.Value}");
                            foundZone = zone;
                            break;
                        }
                    }
                    if (foundZone == null)
                        foreach (AreaUI zone in MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].SubArea)
                        {
                            if (zone.Position.Contains(currentEvent.mousePosition))
                            {
                                //Debug.Log($"    White Key  {currentEvent.mousePosition} --> {zone.Position} {zone.Value}");
                                foundZone = zone;
                                break;
                            }
                        }
                    if (foundZone != null)
                    {
                        // Play the not found on the keyboard
                        playMPTK = new MPTKEvent() { Command = MPTKCommand.NoteOn, Channel = foundZone.Channel, Value = foundZone.Value, Duration = 10000 };
                        Player.MPTK_PlayDirectEvent(playMPTK);
                    }
                }
            }
            return playMPTK;
        }


        // To avoid realloc at each iteration
        //Rect restrictZone = new Rect();
        private MPTKEvent FindCellMouse(Event currentEvent)
        {
            MPTKEvent foundMPTKEvent = null;
            if (MainArea.Position.Contains(currentEvent.mousePosition))
            {
                AreaUI channelsZone = MainArea.SubArea[(int)AreaUI.AreaType.Channels];
                if (channelsZone.Position.Contains(currentEvent.mousePosition))
                {
                    // foreach area as a full iSection (preset + note + ...)
                    foreach (AreaUI channelZone in channelsZone.SubArea)
                    {
                        // is the mouse over a section zone? 
                        if (channelZone.Position.Contains(currentEvent.mousePosition))
                        {
                            // Foreach subarea: note, preset, ... A SubArea contains all MIDI events visible for this section
                            foreach (AreaUI cellZone in channelZone.SubArea)
                            {
                                // is the mouse over a cell position?  
                                if (cellZone.Position.Contains(currentEvent.mousePosition))
                                {
                                    // Yes! there is an event under the mouse
                                    //Debug.Log($"Find MIDIEvent {currentEvent.mousePosition} --> {cellZone.Position} {cellZone.Position.x + cellZone.Position.width} {cellZone.midiEvent}");
                                    foundMPTKEvent = cellZone.midiEvent;
                                    if (currentEvent.alt)
                                    {
                                        MouseAction = enAction.DeleteNote;
                                        CurrentMouseCursor = MouseCursor.ArrowMinus;
                                    }
                                    // https://docs.unity3d.com/ScriptReference/MouseCursor.html
                                    // Only note on can be resized
                                    else
                                    {
                                        // We need 8px on each side to display the action change length and 8 px inside for the move action.
                                        // Also if size <=24 we will only be able to move the cell
                                        // unless the shift key is pressed, the action will be forced to sizing by right.
                                        if (cellZone.Position.width >= 24 && currentEvent.mousePosition.x < cellZone.Position.x + 8f && foundMPTKEvent.Command == MPTKCommand.NoteOn)
                                        {
                                            MouseAction = enAction.LengthLeftNote;
                                            CurrentMouseCursor = MouseCursor.ResizeHorizontal;
                                        }
                                        // Only note-on can be resized, force a right sizing if shift key
                                        else if (currentEvent.shift || cellZone.Position.width >= 24 && currentEvent.mousePosition.x >= cellZone.Position.x + cellZone.Position.width - 8f && foundMPTKEvent.Command == MPTKCommand.NoteOn)
                                        {
                                            MouseAction = enAction.LengthRightNote;
                                            CurrentMouseCursor = MouseCursor.ResizeHorizontal;
                                        }
                                        else
                                        {
                                            MouseAction = enAction.MoveNote;
                                            CurrentMouseCursor = MouseCursor.Pan;
                                            if (Context.TipEnabled)
                                                ToolTipMidiEditor.Set(new Rect(cellZone.Position.x, cellZone.Position.y + cellZone.Position.height, 0, 0), cellZone.midiEvent);
                                        }
                                    }
                                    // Found a MIDI event 
                                    //Debug.Log($"Find MIDIEvent - mousePosition:{currentEvent.mousePosition} --> cellZone:{cellZone.Position} {MouseAction} MidiEvent:{foundMPTKEvent ?? foundMPTKEvent}");
                                    break;
                                }
                            }
                            if (foundMPTKEvent == null && currentEvent.mousePosition.y > channelZone.Position.y + HEIGHT_CHANNEL_BANNER)
                            {
                                // Vary between 0 and CellHeight
                                float posRelativ = (currentEvent.mousePosition.y - (channelZone.Position.y + HEIGHT_CHANNEL_BANNER)) % Context.CellHeight;
                                //Debug.Log($"mousePosition.y:{currentEvent.mousePosition.y} posRelativ:{posRelativ}");
                                float trigger = Context.CellHeight / 4f;
                                if (posRelativ > trigger && posRelativ < Context.CellHeight - trigger)
                                {
                                    CurrentMouseCursor = MouseCursor.ArrowPlus;
                                    MouseAction = enAction.CreateNote;
                                }
                            }
                        }
                        if (foundMPTKEvent != null) break;
                    }
                }
            }
            return foundMPTKEvent;
        }



        // Level of quantization : 
        // 0 = None --> 0
        // 1 = whole  --> dtpqn * 4
        // 2 = half --> dtpqn * 2
        // 3 = Beat Note --> dtpqn * 1
        // 4 = Eighth Note --> dtpqn * 0.5
        // 5 = 16th Note
        // 6 = 32th Note
        // 7 = 64th Note
        // 8 = 128th Note

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
        //private void AddCell(MPTKEvent midiEvent, MPTKGui.StyleItem item, string text, GUIStyle styleRow = null)
        //{
        //    if (!item.Hidden)
        //    {
        //        GUIStyle style = styleRow == null ? item.Style : styleRow;
        //        if (item.Offset != 0) style.contentOffset = new Vector2(item.Offset, 0);
        //        GUILayout.Label(text, style, GUILayout.Width(item.Width));
        //        if (item.Offset != 0) style.contentOffset = Vector2.zero;

        //        // User select a line ?
        //        if (Event.current.type == EventType.MouseDown)
        //            if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        //            {
        //                //Debug.Log($"{midiEvent.Type} XXX {window.position.x + GUILayoutUtility.GetLastRect().x}");
        //                //SelectedEvent = midiEvent.Type;
        //                //MidiPlayerEditor.MidiPlayer.MPTK_TickCurrent = midiEvent.Tick;
        //                window.Repaint();
        //            }
        //    }
        //}

        private void TestLenStyle()
        {
            string test = "";
            for (int i = 1; i < 20; i++)
            {
                test += "0";
                float len = TimelineStyle.CalcSize(new GUIContent(test)).x;
                Debug.Log($"{i,-2} {len} {len / (float)i:F2} {test}");
            }
        }

        //private void DisplayPerf(string title = null, bool restart = true)
        //{
        //    //StackFrame sf = new System.Diagnostics.StackTrace().GetFrame(1);
        //    if (title != null)
        //        Debug.Log($"{title,-20} {((double)watchPerf.ElapsedTicks) / ((double)System.Diagnostics.Stopwatch.Frequency / 1000d):F2} ms ");
        //    if (restart)
        //        watchPerf.Restart();
        //}

        private void DebugAreaUI()
        {
            //Debug.Log($"White Keys:{MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].SubArea.Count} Black Keys:{MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].SubArea.Count}");

            GUI.Label(MainArea.Position, "Mainzone", TimelineStyle);
            DrawRect(MainArea.Position, Color.yellow, 4);
            DrawRect(MainArea.SubArea[(int)AreaUI.AreaType.Channels].Position, Color.blue, 3);
            MainArea.SubArea[(int)AreaUI.AreaType.Channels].SubArea.ForEach(zone =>
            {
                DrawRect(zone.Position, Color.red, 2);
                zone.SubArea.ForEach(zoneCell =>
                {
                    DrawRect(zoneCell.Position, Color.green, 1);
                });
            });

            DrawRect(MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].Position, Color.black, 3);
            //       White and black keys: same zone   DrawRect(MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].Position, Color.black, 3);
            MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys].SubArea.ForEach(zone => DrawRect(zone.Position, Color.green, 1));
            MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys].SubArea.ForEach(zone => DrawRect(zone.Position, Color.red, 1));
        }
        private void DebugMainAreaAndSection()
        {
            Debug.Log("--------------- MainArea -------------------");
            DebugSubArea(MainArea);
            DebugSubArea(MainArea.SubArea[(int)AreaUI.AreaType.WhiteKeys]);
            DebugSubArea(MainArea.SubArea[(int)AreaUI.AreaType.BlackKeys]);
            DebugSubArea(MainArea.SubArea[(int)AreaUI.AreaType.Channels]);

            Debug.Log("--------------- SectDim -------------------");
            for (int channel = 0; channel < sectionAll.Sections.Length; channel++)
            {
                // Ambitus + row for preset
                if (sectionAll.Sections[channel] != null)
                    Debug.Log(sectionAll.Sections[channel].ToString());
            }
        }

        private static void DebugSubArea(AreaUI area)
        {
            Debug.Log("\t" + area.ToString());
            if (area.SubArea != null)
                area.SubArea.ForEach(zone =>
                {
                    Debug.Log("\t\t" + zone.ToString());
                    if (zone.SubArea != null) zone.SubArea.ForEach(zoneCell => { Debug.Log("\t\t\t" + zoneCell.ToString()); });
                });
        }
        private void DebugListEvent()
        {
            MidiEvents.ForEach(e => { Debug.Log(e); });
        }
    }
}
#endif