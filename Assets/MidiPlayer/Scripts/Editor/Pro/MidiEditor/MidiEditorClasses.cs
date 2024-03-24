#if UNITY_EDITOR
//#define DEBUG_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MidiPlayerTK
{
    public partial class MidiEditorWindow : EditorWindow
    {
        public class ContextEditor
        {
            public string MidiName;
            public string PathOrigin;
            public bool Modified;

            /// <summary>
            /// Multiplier screen X to quarter length
            /// </summary>

            public float QuarterWidth;
            /// <summary>
            /// Height of a cell note
            /// </summary>
            public float CellHeight;

            /// <summary>
            /// 0:none, 1:whole, 2:half, 3:quarter, 4:height, 5:Sixteenth (16 quarter in a bar)
            /// </summary>
            public int IndexQuantization;

            public int DisplayTime;
            public int MidiIndex;
            public bool FollowEvent;
            public bool LogEvents;
            [OptionalField] public bool LoopEnabled;
            [OptionalField] public long LoopResume;
            [OptionalField] public long LoopEnd;
            [OptionalField] public bool TipEnabled;
            [OptionalField] public bool[] SectionOpen;
            [OptionalField] public bool[] SectionMute;
            public void SetDefaultSize()
            {
                QuarterWidth = 50f;
                CellHeight = 20f;
            }

            public void SetSectionOpen(bool open)
            {
                for (int i = 0; i < SectionOpen.Length; i++) { SectionOpen[i] = open; }
            }

            public void SetSectionMute(bool open)
            {
                for (int i = 0; i < SectionMute.Length; i++) { SectionMute[i] = open; }
            }

            public ContextEditor()
            {
                MidiName = "name not defined";
                PathOrigin = "";
                Modified = false;
                IndexQuantization = 5;
                DisplayTime = 0;
                FollowEvent = false;
                LogEvents = false;
                LoopEnabled = false;
                LoopResume = LoopEnd = 0;
                SectionOpen = new bool[30]; // normally not more 17 but in case of new idea ...
                SectionMute = new bool[30]; // normally not more 17 but in case of new idea ...
                SetSectionOpen(true);
                SetSectionMute(false);
                SetDefaultSize();
            }
        }

        class AreaUI
        {
            public enum AreaType
            {
                /// <summary>
                /// All channels, link to MainZone.SubZone[0]
                /// </summary>
                Channels = 0,

                /// <summary>
                /// All white wey, link to MainZone.SubZone[1]
                /// </summary>
                WhiteKeys = 1,

                /// <summary>
                /// All black wey, link to MainZone.SubZone[2]
                /// </summary>
                BlackKeys = 2,

                /// <summary>
                /// One section, no link, multi section 
                /// </summary>
                Channel,
            }

            public Rect Position;
            public AreaType areaType;
            public int Channel;
            public int Value;
            public MPTKEvent midiEvent;
            public List<AreaUI> SubArea;
            public override string ToString()
            {
                return $"Type:{areaType} Channel:{Channel} Position:{Position} MidiEvent:{midiEvent ?? midiEvent} Value:{Value}";
            }
        }

        /// <summary>
        ///  Contains 
        ///  16 sections for each section + 1 section for META + 1 section for System Timinf (not yet)
        ///  querter width + cell height + MeasureWidth
        ///  
        /// </summary>
        class SectionAll
        {
            public const int SECTION_META = 16;

            public SectionCore[] Sections;
            private MPTKWriter midiFileWriter;
            private float quarterWidth;
            private float cellHeight;

            public SectionAll(MPTKWriter midiFileWriter)
            {
                // Type 0 to 15 for MIDI IndexSection, section index 16 for META events (Set tempo, ...)
                Sections = new SectionCore[17];
                this.midiFileWriter = midiFileWriter;
            }

            public void InitSections()
            {
                Array.ForEach(Sections, section =>
                {
                    if (section != null)
                    {
                        Array.ForEach(section.Layouts, layout => { if (layout != null) layout.Count = 0; });
                    }
                });

                midiFileWriter.MPTK_MidiEvents.ForEach(midiEvent => UpdateSections(midiEvent));

                // Clear section with no data
                for (int section = 0; section < Sections.Length; section++)
                    if (Sections[section] != null && Sections[section] is SectionChannel)
                    {
                        SectionChannel sectionChannel = (SectionChannel)Sections[section];
                        if (sectionChannel.Layouts[0].Count == 0 && sectionChannel.Layouts[1].Count == 0)
                            Sections[section] = null;
                    }

                UpdateLayout(midiFileWriter.MPTK_MidiEvents);
            }

            public bool SectionExist(int section)
            {
                return Sections[section] != null;
            }

            public bool AddSection(int section)
            {
                if (SectionExist(section))
                    return false;

                if (section < SectionAll.SECTION_META)
                {
                    Sections[section] = new SectionChannel(section);
                    // This section contains note
                    (Sections[section] as SectionChannel).Layouts[1].Lower = 60;
                    (Sections[section] as SectionChannel).Layouts[1].Higher = 65;
                    // A preset change has been added when a new section has been created
                    (Sections[section] as SectionChannel).Presets.Add(new SectionChannel.PresetSet() { Line = 0, Value = 0 });
                }
                else if (section == SectionAll.SECTION_META)
                {
                    Sections[section] = new SectionMeta();
                    // TBD
                }

                return true;
            }

            private void UpdateLayout(List<MPTKEvent> midiEventx)
            {
                foreach (SectionCore s in Sections)
                    if (s != null)
                    {
                        if (s is SectionChannel)
                        {
                            SectionChannel section = (SectionChannel)s;
                            section.Layouts[1].FindLowerHigherNotes(midiEventx, section.IndexSection);
                            section.Presets.Sort((p1, p2) => { return p1.Value.CompareTo(p2.Value); });
                            int line = 0;
                            section.Presets.ForEach(p => p.Line = line++);
                        }
                        else if (s is SectionMeta)
                        {
                            SectionMeta section = (SectionMeta)s;
                        }
                    }
            }
            public void UpdateSections(MPTKEvent midiEvent)
            {
                int channel = midiEvent.Channel;
                switch (midiEvent.Command)
                {
                    case MPTKCommand.NoteOn:
                        if (Sections[channel] == null) Sections[channel] = new SectionChannel(channel);
                        (Sections[channel] as SectionChannel).Layouts[1].Count++;
                        break;
                    case MPTKCommand.NoteOff:
                        break;
                    case MPTKCommand.PatchChange:
                        if (Sections[channel] == null) Sections[channel] = new SectionChannel(channel);
                        int line = (Sections[channel] as SectionChannel).GetPresetLine(midiEvent.Value);
                        if (line < 0)
                            (Sections[channel] as SectionChannel).Presets.Add(new SectionChannel.PresetSet() { Line = (Sections[channel] as SectionChannel).Presets.Count, Value = midiEvent.Value });
                        (Sections[channel] as SectionChannel).Layouts[0].Count++;

                        break;
                    case MPTKCommand.MetaEvent:
                        if (Sections[SECTION_META] == null) Sections[SECTION_META] = new SectionMeta();
                        SectionMeta meta = (SectionMeta)Sections[SECTION_META];
                        meta.Layouts[0].Count++; 
                        //if (midiEvent.Meta == MPTKMeta.SetTempo)
                        //    meta.LayoutMeta.Count++;
                        break;
                }
            }

            public void CalculateSizeAllSections(MPTKEvent last, float QuarterWidth, float CellHeight)
            {
                quarterWidth = QuarterWidth;
                cellHeight = CellHeight;

                // Calculate total width
                SectionCore.FullWidthSections = 0f;
                if (last != null)
                {
                    // Calculate position + with of the rectClear note --> full width of the bigger section
                    SectionCore.FullWidthSections = ConvertTickToPosition(last.Tick) + ConvertTickToPosition(last.Length);
                    //Debug.Log($"BeginArea draw MIDI cellX:{cellX} cellW:{cellW} ChannelMidi.FullWidthChannelZone:{ChannelMidi.FullWidthChannelZone}");
                }

                // The minimun ticks to display is 4 measures even if there is no MIDI event
                long miniTick = midiFileWriter.DeltaTicksPerQuarterNote * midiFileWriter.MPTK_SignMap[0].NumberBeatsMeasure * 4 / midiFileWriter.MPTK_SignMap[0].NumberQuarterBeat;
                float miniWidth = ConvertTickToPosition(miniTick);
                if (SectionCore.FullWidthSections < miniWidth) SectionCore.FullWidthSections = miniWidth;
                //Debug.Log($"{SectionCore.FullWidthSections} miniWidth:{miniWidth}");

                // Calculate total height
                float currentY = 0f;

                // TBD
                if (Sections[SECTION_META] != null)
                {
                    SectionMeta section = Sections[SECTION_META] as SectionMeta;
                    currentY += HEIGHT_CHANNEL_BANNER;

                    // Program Change row
                    section.Layouts[0].BegY = currentY;
                    if (Context.SectionOpen[SECTION_META])
                        section.Layouts[0].EndY = section.Layouts[0].BegY + cellHeight * section.Metas.Count;
                    else
                        section.Layouts[0].EndY = currentY;
                    currentY = section.Layouts[0].EndY;
                }

                for (int channel = 0; channel < 16; channel++)
                {
                    SectionChannel section = Sections[channel] as SectionChannel;
                    if (section != null)
                    {
                        currentY += HEIGHT_CHANNEL_BANNER;
                        if (Context.SectionOpen[channel])
                        {
                            // Program Change row
                            section.Layouts[0].BegY = currentY;// - 3f; //no, was not a good idea for  Better alignment for the start of the preset section
                            section.Layouts[0].EndY = section.Layouts[0].BegY + cellHeight * section.Presets.Count;
                            currentY = section.Layouts[0].EndY;

                            // Notes rows
                            if (section.Layouts[1].Higher != 0)
                            {
                                section.Layouts[1].BegY = currentY;
                                section.Layouts[1].EndY = section.Layouts[1].BegY + (section.Layouts[1].Higher - section.Layouts[1].Lower + 1) * cellHeight;
                                currentY = section.Layouts[1].EndY;
                            }
                        }
                        else
                        {
                            section.Layouts[0].BegY = currentY;
                            section.Layouts[0].EndY = currentY;
                            section.Layouts[1].BegY = currentY;
                            section.Layouts[1].EndY = currentY;
                        }
                    }
                }
                SectionCore.FullHeightSections = currentY;
            }

            public float ConvertTickToPosition(long tick)
            {
                return ((float)tick / (float)midiFileWriter.DeltaTicksPerQuarterNote) * quarterWidth;
            }

            public long ConvertPositionToTick(float x)
            {
                return (long)((x * midiFileWriter.DeltaTicksPerQuarterNote) / quarterWidth);
            }
        }

        class SectionCore
        {
            /// <summary>
            /// One section for each section used in the MIDI, -1 for other section like META
            /// </summary>
            public int IndexSection;

            /// <summary>
            /// Each section can have multiple layout
            /// </summary>
            public Layout[] Layouts;
            //public class cLayout<Layout> { };
            //public Arr<Layout> Layouts;
            public float BegSection
            {
                get
                {
                    if (Layouts != null && Layouts.Length > 0)
                        return Layouts[0].BegY - HEIGHT_CHANNEL_BANNER;
                    else return 0; ;
                }
            }
            public float Height
            {
                get
                {
                    if (Layouts != null && Layouts.Length > 0)
                        return Layouts.Last().EndY - Layouts[0].BegY + HEIGHT_CHANNEL_BANNER;
                    else
                        return 0;
                }
            }


            /// <summary>
            /// Contains section info + notes zone + preset zone
            /// </summary>
            //public Layout LayoutAll;

            /// <summary>
            /// Width of the full which display MIDI events (not only the visible area). Calculated at start of each OnGUI call.
            /// </summary>
            public static float FullWidthSections;

            /// <summary>
            /// Height of the full are to display MIDI events. Calculated at each OnGUI.
            /// </summary>
            public static float FullHeightSections;

            public SectionCore(int section)
            {
                IndexSection = section;
                //Layouts instanciated in children
            }

            public override string ToString()
            {
                string detail = $"Section:{IndexSection}   FullWidthSections:{FullWidthSections} FullHeightSections:{FullHeightSections}";
                //if (LayoutAll != null) detail += "\n\tAll layout=" + LayoutAll.ToString();
                return detail;
            }
        }

        /// <summary>
        /// Sections are defined in SectionAll Type 0 to 15 for MIDI IndexSection, section 16 for META events (Set tempo, ...)
        /// </summary>
        class SectionChannel : SectionCore
        {

            /// <summary>
            /// Contains section info + notes zone + preset zone
            /// </summary>
            //public Layout[] Layouts;
            //public Layout LayoutPreset;

            // For a IndexSection channel, section is the channel number (from 0)
            public SectionChannel(int section) : base(section)
            {
                Presets = new List<PresetSet>();
                Layouts = new Layout[2];
                Layouts[0] = new Layout() { Type = Layout.EnumType.Preset };
                Layouts[1] = new Layout() { Type = Layout.EnumType.Note };
                //Layouts = new Arr<Layout>();
                //Layouts.Add(new Layout());
                //Layouts.Add(new Layout());

            }

            // Count preset used for this section
            public class PresetSet
            {
                public int Value;
                public int Line;
                public MPTKGui.PopupList PopupPreset;
            }
            public List<PresetSet> Presets;

            /// <summary>
            /// Search the line of the preset to display
            /// </summary>
            /// <param name="value"></param>
            /// <returns>-1 if not found, 0 for the first line</returns>
            public int GetPresetLine(int value)
            {
                foreach (PresetSet p in Presets)
                    if (p.Value == value)
                        return p.Line;
                // Not found
                return -1;
            }


            public override string ToString()
            {
                string detail = $"Section Channel:{IndexSection} NoteCount:{Layouts[0].Count} PresetCount:{Layouts[0].Count} FullWidthSections:{FullWidthSections} FullHeightSections:{FullHeightSections}";
                if (Layouts[0] != null) detail += "\n\tPreset lay=" + Layouts[0].ToString();
                if (Layouts[1] != null) detail += "\n\tNote lay  =" + Layouts[1].ToString();
                return detail;
            }
        }

        /// <summary>
        /// Sections are defined in SectionAll Type 0 to 15 for MIDI IndexSection, section 16 for META events (Set tempo, ...)
        /// </summary>
        class SectionMeta : SectionCore
        {
            public List<MetaSet> Metas;

            public SectionMeta() : base(SectionAll.SECTION_META)
            {
                Metas = new List<MetaSet>
                {
                    new MetaSet() { Meta = MPTKMeta.SetTempo,Name="Tempo" },
                    new MetaSet() { Meta = MPTKMeta.TextEvent,Name="Text"}
                };
                try
                {
                    Layouts = new Layout[1];
                    Layouts[0] = new Layout() { Type = Layout.EnumType.Meta }; ;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"new SectionMeta  {ex}");
                }
            }

            // Count preset used for this section
            public class MetaSet
            {
                public MPTKMeta Meta;
                public int Line;
                public string Name;
            }

            public override string ToString()
            {
                string detail = $"Section Meta:{IndexSection} MetaCount:{Layouts[0].Count} FullWidthSections:{FullWidthSections} FullHeightSections:{FullHeightSections}";
                if (Layouts[0] != null) detail += "\n\tMeta layout  =" + Layouts[0].ToString();
                return detail;
            }
        }

        //public class Arr<T> 
        //{
        //    T[] obj = new T[5];
        //    int count = 0;

        //    // adding items mechanism into generic type
        //    public void Add(T item)
        //    {
        //        //checking length
        //        if (count + 1 < 6)
        //        {
        //            obj[count] = item;

        //        }
        //        count++;
        //    }
        //    //indexer for foreach statement iteration
        //    public T this[int index]
        //    {
        //        get { return obj[index]; }
        //        set { obj[index] = value; }
        //    }
        //}

        /// <summary>
        /// Layout is defined in SectionChannel for : LayoutAll, Layouts, LayoutPreset
        /// </summary>
        class Layout
        {
            public enum EnumType
            {
                Preset,
                Note,
                Meta,
            }
            public EnumType Type;
            public int Lower;
            public int Higher;
            public int Count;
            //public float x; always start at 0
            /// Y start position of this zone from the beginarea
            public float BegY;
            public float EndY;
            // public float width; always the full width
            public float Height { get { return EndY - BegY; } }

            public Layout()
            {
                Lower = 9999;
                Higher = 0;
                Count = 0;
            }
            public void FindLowerHigherNotes(List<MPTKEvent> midiEvents, int channel, bool shrink = false)
            {
                //DisplayPerf(restart: true);
                //if (midiEvents.Count == 0)
                //{
                //    Lower = 60;
                //    Higher = 65;
                //}
                //else
                {
                    int l = 9999;
                    int h = 0;
                    //Lower = 9999;
                    //Higher = 0;
                    midiEvents.ForEach(noteon =>
                    {
                        if (noteon.Channel == channel && noteon.Command == MPTKCommand.NoteOn)
                        {
                            if (noteon.Value < l) l = noteon.Value;
                            if (noteon.Value > h) h = noteon.Value;
                        }
                    });

                    //if (l != 9999 && l != Lower)
                    //    if (l < Lower || shrink)
                    //        Lower = l;
                    //if (h != 0 && h != Higher)
                    //    if (h > Higher || shrink)
                    //        Higher = h;
                    if (l == 9999 && h == 0)
                    {
                        l = 60;
                        h = 65;
                    }
                    if (l < Lower) Lower = l;
                    if (h > Higher) Higher = h;


                    //if (l == 9999)
                    //    Lower = h - 5;
                    //else
                    //    Lower = l;

                    //if (h == 0)
                    //    Higher = l + 5;
                    //else
                    //    Higher = h;

                    //int ambitus = Higher - Lower;
                    //if (ambitus < 3)
                    //{
                    //    Higher += 1;
                    //    Lower -= 1;
                    //}

                    //Debug.Log($"FindLowerHigherNotes section:{section}     l:{l} h:{h} Lower:{Lower} Higher:{Higher}");
                }
                //DisplayPerf("FindLowerHigherNotes");
            }
            //int ambitus = Higher - Lower;
            //if (ambitus < 3)
            //{
            //    Higher += 1;
            //    Lower -= 1;
            //}


            public void SetLowerHigherNote(int value)
            {
                if (value < Lower) Lower = value;
                if (value > Higher) Higher = value;
            }

            public override string ToString()
            {
                return $"  BegY:{BegY:0000}  EndY:{EndY:0000}  Height:{Height:0000}  LowerNote:{Lower} HigherNote:{Higher}";
            }
        }

        public class ToolTipMidiEditor
        {
            static string Text;
            static Rect Position;
            static GUIContent Content;
            static GUIStyle TextAreaMultiCourier;

            public static void Init()
            {
                // Sometime, the background is lost ... Unity bug?
                if (TextAreaMultiCourier == null || TextAreaMultiCourier.normal.background == null)
                {
                    Font myFont = (Font)Resources.Load("Courier", typeof(Font));
                    RectOffset SepBorder1 = new RectOffset(1, 1, 1, 1);
                    TextAreaMultiCourier = MPTKGui.BuildStyle(inheritedStyle: MPTKGui.TextArea, fontSize: 12, textAnchor: TextAnchor.UpperLeft);
                    TextAreaMultiCourier.wordWrap = false;
                    TextAreaMultiCourier.richText = false;
                    TextAreaMultiCourier.font = Resources.Load<Font>("Courier");
                    MPTKGui.ColorStyle
                        (
                            style: TextAreaMultiCourier,
                            fontColor: Color.green,
                            backColor: MPTKGui.MakeTex(10, 10, textureColor: new Color(0.2f, 0.2f, 0.2f, 0.6f), border: SepBorder1, bordercolor: new Color(0.1f, 0.1f, 0.1f, 1))
                        );
                }
            }
            public static void Set(Rect position, MPTKEvent midiEvent)
            {
                string text = "";
                if (midiEvent != null)
                {
                    text += $"Tick:     {midiEvent.Tick}\n";
                    text += $"Measure:  {midiEvent.Measure}.{midiEvent.Beat}\n";
                    text += $"Time:     {midiEvent.RealTime / 1000f:F3} sec.\n";
                    switch (midiEvent.Command)
                    {
                        case MPTKCommand.NoteOn:
                            text += $"Note:     {midiEvent.Value}   {HelperNoteLabel.LabelC4FromMidi(midiEvent.Value)}\n";
                            text += $"Duration: {midiEvent.Length} ticks   {midiEvent.Duration / 1000f:F3} sec.\n";
                            text += $"Velocity: {midiEvent.Velocity}";
                            break;
                        case MPTKCommand.PatchChange:
                            text += $"Preset:   {midiEvent.Value}   {MidiPlayerGlobal.MPTK_GetPatchName(0, midiEvent.Value)}";
                            break;
                        case MPTKCommand.MetaEvent:
                            switch (midiEvent.Meta)
                            {
                                case MPTKMeta.TextEvent:
                                    text += $"Text:     {midiEvent.Info}";
                                    break;
                                case MPTKMeta.SetTempo:
                                    text += $"BPM:      {60000000 / midiEvent.Value:F0}   {midiEvent.Value} µs/Quarter";
                                    break;
                            }
                            break;
                    }
                    Set(position, text);
                }
            }
            public static void Set(Rect position, string text)
            {
                Init();
                Position = position;
                Text = text;
                Content = new GUIContent(text);
                Vector2 size = TextAreaMultiCourier.CalcSize(Content);
                if (Position.width == 0) Position.width = size.x;
                // Count line, not useful, CalcSize takes care of lines count. int freq = Text.Count(f => (f == '\n'));
                if (Position.height == 0) Position.height = size.y;
            }
            public static void Clear()
            {
                Text = null;
            }

            public static void Display()
            {
                Init();

                if (!string.IsNullOrEmpty(Text))
                    GUI.TextArea(Position, Text, TextAreaMultiCourier);
            }
        }
        public class MidiEventEditor
        {
            public int Channel;
            //public int CurrentTrack;
            public int Command;
            public long Tick;
            public int Note = 60;
            public int Length;
            public int Velocity = 100;
            public int Preset;
            public int TempoBpm = 120;
            public string Text = "";

        }
    }
}
#endif