using JetBrains.Annotations;

namespace DAW.CommonClasses
{
    public enum MeasureLineType { 
        MEASURE,
        QUARTER,
        AUXILARY
    }
    public enum SideType
    {
        NONE = 0,
        LEFT,
        RIGHT
    }

    public enum EditState
    {
        TRACK = 0,
        CHORD,
        MELODY
    }

    public enum AreaType
    {
        Channels = 0,
        WhiteKeys = 1,
        BlackKeys = 2,
        Cannel
    }

    public class KeyInfo
    {
        public AreaType areaType = AreaType.WhiteKeys;
        public string name = "C";
        public int midiNote = 60;
        public int octave = 4;

        public KeyInfo()
        {
            areaType = AreaType.WhiteKeys;
            name = "C";
            midiNote = 60;
            octave = 4;
        }

        public KeyInfo(int n){
            midiNote = n;
            (name, octave) = Global.GetKeyNameFromNote(n);
            areaType = name.Contains("#")? AreaType.BlackKeys: AreaType.WhiteKeys;
        }
    }

    public class NoteCellInfo
    {
        public int index;
        public int command;
        public long tick;
        public int midiNote = 60;
        public int length;
        public int velocity = 100;
        public int preset;
        public int tempoBMP = 120; // it is similar to speed of playing music
        public string text = "";
        public AreaType areaType = AreaType.WhiteKeys;

        public NoteCellInfo()
        {
            index = 0;
            command = 0;
            tick = 0;
            midiNote = 60;
            length = 240;
            velocity = 128;
            preset = 1;
            tempoBMP = 120; // it is similar to speed of playing music
            text = "";
            areaType = AreaType.WhiteKeys;
        }

        public NoteCellInfo(int note, int idx){
            
            index = idx;
            if (idx < 0)
                index = ++Global._maxCellIndex;
            tick = 0;
            midiNote = note;
            length = 240;
            velocity = 128;
            preset = 1;
            tempoBMP = 120; // it is similar to speed of playing music
            (string name, int octave) = Global.GetKeyNameFromNote(note);
            areaType = name.Contains("#")? AreaType.BlackKeys: AreaType.WhiteKeys;
        }
    }

    public class TrackInfo
    {
        public int midiNote;
        public int octave;
        public string keyName;
        public AreaType areaType = AreaType.WhiteKeys;

        public TrackInfo(){
            midiNote = 0;
            areaType = AreaType.WhiteKeys;
            octave = -1;
            keyName = "C";
        }

        public TrackInfo(int n){
            midiNote = n;
            (keyName, octave) = Global.GetKeyNameFromNote(n);
            areaType = keyName.Contains("#")? AreaType.BlackKeys: AreaType.WhiteKeys;
        }
    }
}
