#if UNITY_EDITOR
//#define DEBUG_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MidiPlayerTK
{

    public partial class MidiEditorWindow : EditorWindow
    {
        #region const
        // https://en.wikipedia.org/wiki/List_of_musical_symbols
        // CONST
        const int WIDTH_BUTTON_PLAYER = 100;
        const float WIDTH_KEYBOARD = 100f;
        const float AREA_BORDER_X = 5;
        const float X_CORR_SECTION = AREA_BORDER_X + WIDTH_KEYBOARD;

        const float AREA_BORDER_Y = 5;//7;
        const int HEIGHT_HEADER = 30;//60; // Title
        const int HEIGHT_PLAYER_CMD = 60 + 20; // MIDI Player
        const float HEIGHT_TIMELINE = 38;//28;//28; // MIDI time
        const float HEIGHT_CHANNEL_BANNER = 20;//20; // Chnnel Header
        const float Y_CORR_SECTION = AREA_BORDER_Y + HEIGHT_HEADER + HEIGHT_PLAYER_CMD + HEIGHT_TIMELINE;
        const float WIDTH_PAD_CHANNEL = 100f;
        const float WIDTH_PAD_MIDI_EVENT = 300f;
        #endregion


        #region popup
        static MPTKGui.PopupList PopupSelectPreset;
        static MPTKGui.PopupList PopupSelectDisplayTime;
        static MPTKGui.PopupList PopupSelectQuantization;
        static MPTKGui.PopupList PopupSelectMidiCommand;
        static MPTKGui.PopupList PopupSelectMidiChannel;
        static MPTKGui.PopupList PopupSelectLoadMenu;
        static MPTKGui.PopupList PopupSelectSaveMenu;

        static List<MPTKGui.StyleItem> PopupItemsPreset;
        static List<MPTKGui.StyleItem> PopupItemsDisplayTime;
        static List<MPTKGui.StyleItem> PopupItemsQuantization;
        static List<MPTKGui.StyleItem> PopupItemsMidiCommand;
        static List<MPTKGui.StyleItem> PopupItemsMidiChannel;
        static List<MPTKGui.StyleItem> PopupItemsLoadMenu;
        static List<MPTKGui.StyleItem> PopupItemsSaveMenu;
        static public PopupInfoSynth winPopupSynth;
        #endregion


        #region ui
        int borderSize1 = 1; // Border size in pixels
        int borderSize2 = 2; // Border size in pixels
        int borderSize3 = 3; // Border size in pixels

        static RectOffset SepBorder0;
        static RectOffset SepBorder1;
        static RectOffset SepBorder2;
        static RectOffset SepBorder3;
        static Texture SepChannelTexture;
        static Texture SepNoteTexture;
        static Texture SepPresetTexture;
        static Texture SepBarText;
        static Texture SepQuarterText;
        static Texture SepDragEventText;
        static Texture SepDragMouseText;
        static Texture SepPlayingPositionTexture;
        static Texture SepSelectedPositionTexture;
        static Texture SepLoopTexture;
        static Rect separatorDragVerticalBegin;

        static Rect rectPositionSequencer = new Rect(0, 0, 2, 0);
        static Rect rectPositionLoopStart = new Rect(0, 0, 2, 0);
        static Rect rectPositionLoopEnd = new Rect(0, 0, 2, 0);

        static Rect rectSectionBtAllSections = new Rect(2, 5, 18, 18);
        static Rect rectSectionBtBanner = new Rect(0, 0, 16, 16);

        static GUIStyle TimelineStyle;
        static GUIStyle MeasurelineStyle;
        static GUIStyle PresetButtonStyle;
        static GUIStyle MetaLabelStyle;
        static Texture MidiNoteTexture;
        static Texture MidiSelectedTexture;
        static Texture MidiPresetTexture;
        static Texture MidiSelected;
        static GUIStyle ChannelBannerStyle;
        static GUIStyle BackgroundMidiEvents;
        static GUIStyle BackgroundMidiEvents1;
        static GUIStyle WhiteKeyLabelStyle;
        static GUIStyle BlackKeyLabelStyle;
        static Texture WhiteKeyDrawTexture;
        static Texture BlackKeyDrawTexture;

        float HeightScrollHori;
        float WidthScrollVert;

        #endregion

        #region variable

        static AreaUI MainArea;
        private Rect mouseCursorRect;

        /// <summary>
        ///  Contains 16 sections for each channel + 1 section for META + 1 section for System Timinf (not yet
        /// </summary>
        static SectionAll sectionAll;

        static MPTKEvent LastMidiEvent;
        static MPTKEvent LastNoteOnEvent;

        static MPTKWriter MidiFileWriter;
        static ContextEditor Context;
        static List<MPTKEvent> MidiEvents;

        static long TickQuantization;

        static public Vector2 ScrollerMidiEvents;
        static private MidiEditorLib MidiPlayerSequencer;

        static public MidiEventEditor MidiEventEdit=new MidiEventEditor();

        static long lastTickForUpdate = -1;
        static DateTime lastTimeForUpdate;

        //static string InvalidFileChars = new string(Path.GetInvalidFileNameChars());
        static char[] InvalidFileChars = Path.GetInvalidFileNameChars();

        static float startXEventsList;
        static float startYEventsList;
        static float widthVisibleEventsList; // with of the area displayed on the screen
        static float heightVisibleEventsList; // height of the area displayed on the screen

        static MPTKEvent LastEventPlayed = null;
        /// <summary>
        /// Current position only when not playing. If playing the value is taken from Player.midiLoaded.MPTK_TickPlayer
        /// </summary>
        static long CurrentTickPosition = 0;
        static int PositionSequencerPix = 0;
        static long InitialTick = 0;
        static int InitialDurationTick = 0;
        static int InitialValue = 0;

        static MPTKEvent KeyPlaying;
        static Event LastMouseMove;
        static MPTKEvent SelectedEvent = null;
        static MPTKEvent NewDragEvent = null;
        static Vector2 DragPosition;
        enAction MouseAction = enAction.None;
        enum enAction { None, MoveNote, LengthLeftNote, LengthRightNote, CreateNote, DeleteNote }

        static Vector2 LastMousePosition;

        static MouseCursor CurrentMouseCursor = MouseCursor.Arrow;

        static bool DebugDisplayCell;


        static GUIContent LabelPlay = new GUIContent(MPTKGui.LoadIcon("MidiEditor/playMidi"), "Play MIDI");
        static GUIContent LabelPlaying = new GUIContent(MPTKGui.LoadIcon("MidiEditor/playingMidi"), "Stop Playing");
        static GUIContent LabelPause = new GUIContent(MPTKGui.LoadIcon("MidiEditor/playPause"), "Pause MIDI");
        static GUIContent LabelPauseSet = new GUIContent(MPTKGui.LoadIcon("MidiEditor/playPauseSet"), "MIDI Paused");
        
        // not used .. for now
        //static GUIContent LabelStop = new GUIContent(MPTKGui.LoadIcon("MidiEditor/playStop"), "Stop MIDI");

        static GUIContent LabelLoop = new GUIContent(MPTKGui.LoadIcon("MidiEditor/loopRepeat"), "Activate Looping");
        static GUIContent LabelLoopSet = new GUIContent(MPTKGui.LoadIcon("MidiEditor/loopRepeating"), "Disable Looping");

        static GUIContent LabelLoopStart = new GUIContent(MPTKGui.LoadIcon("MidiEditor/loopStart"), "Set loop start from the value of the selected event or from the 'Tick' value");
        //static GUIContent LabelLoopStartSet = new GUIContent(MPTKGui.LoadIcon("MidiEditor/loopStartSet"), "");

        static GUIContent LabelLoopStop = new GUIContent(MPTKGui.LoadIcon("MidiEditor/loopStop"), "Set loop end from the value of the selected event + duration or from the 'Tick' value");
        //static GUIContent LabelLoopStopSet = new GUIContent(MPTKGui.LoadIcon("MidiEditor/loopStopSet"), "");

        static GUIContent LabelLoopReset = new GUIContent(MPTKGui.LoadIcon("MidiEditor/loopReset"), "Reset loop start and stop position");
        static GUIContent LabelLoopMode = new GUIContent(MPTKGui.LoadIcon("MidiEditor/loopMode"), "Change Loop Mode");

        static GUIContent LabelGoToBegin = new GUIContent(MPTKGui.LoadIcon("MidiEditor/gotoBegin"), "Go to begin");
        static GUIContent LabelGoToPrevBar = new GUIContent(MPTKGui.LoadIcon("MidiEditor/gotoPrevBar"), "Go to previous bar");
        static GUIContent LabelGoToCurrent = new GUIContent(MPTKGui.LoadIcon("MidiEditor/gotoCurrent"), "Go to current Tick value defined in the MIDI pad");
        static GUIContent LabelGoToNextBar = new GUIContent(MPTKGui.LoadIcon("MidiEditor/gotoNextBar"), "Go to next bar");
        static GUIContent LabelGoToEnd = new GUIContent(MPTKGui.LoadIcon("MidiEditor/gotoEnd"), "Go to end");

        static GUIContent LabelSectionOpen = new GUIContent(MPTKGui.LoadIcon("MidiEditor/sectionOpen"), "Open Section");
        static GUIContent LabelSectionClose = new GUIContent(MPTKGui.LoadIcon("MidiEditor/sectionClose"), "Close Section");

        static GUIContent LabelSectionMute = new GUIContent(MPTKGui.LoadIcon("MidiEditor/sectionMute"), "Muted Section");
        static GUIContent LabelSectionUnmute = new GUIContent(MPTKGui.LoadIcon("MidiEditor/sectionUnmute"), "Section Playing");
        static GUIContent LabelSectionSolo = new GUIContent(MPTKGui.LoadIcon("MidiEditor/sectionSolo"), "Solo Section");
        
        static GUIContent LabelLogEnabled = new GUIContent(MPTKGui.LoadIcon("MidiEditor/logEnabled"), "Log MIDI events when playing");
        static GUIContent LabelLogDisabled = new GUIContent(MPTKGui.LoadIcon("MidiEditor/logDisabled"), "No log MIDI events when playing");

        static GUIContent LabelTipEnabled = new GUIContent(MPTKGui.LoadIcon("MidiEditor/tipEnabled"), "Tip MIDI enabled");
        static GUIContent LabelTipDisabled = new GUIContent(MPTKGui.LoadIcon("MidiEditor/tipDisabled"), "No tip MIDI");

        static float heightFirstRowCmd = 22;

        private MidiFileEditorPlayer Player;
        private Rect separatorQuarterRect = new Rect();
        private GUIContent dynContent = new GUIContent();
        //static private System.Diagnostics.Stopwatch watchPerf = new System.Diagnostics.Stopwatch();
        #endregion
    }
}
#endif