#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MidiPlayerTK
{
    static public class ExtensionSoundFontSetupWindows
    {
        public static void SelectSf(this SoundFontSetupWindow sfsw, int i)
        {
            MidiPlayerGlobal.CurrentMidiSet.SetActiveSoundFont(i);
            string soundPath = Path.Combine(Application.dataPath + "/", MidiPlayerGlobal.PathToSoundfonts);
            soundPath = Path.Combine(soundPath + "/", MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.Name);

            MidiPlayerGlobal.LoadCurrentSF();

            MidiPlayerGlobal.CurrentMidiSet.Save();
            AssetDatabase.Refresh();
            if (MidiPlayerGlobal.ImSFCurrent != null)
            {
                //KeepAllPatchs = MidiPlayerGlobal.ImSFCurrent.KeepAllPatchs;
                //KeepAllZones = MidiPlayerGlobal.ImSFCurrent.KeepAllZones;
                //RemoveUnusedWaves = MidiPlayerGlobal.ImSFCurrent.RemoveUnusedWaves;
                if (Application.isPlaying)
                {
                    MidiPlayerGlobal.MPTK_SelectSoundFont(null);
                }
            }
        }

        public static bool DeleteSf(this SoundFontSetupWindow sfsw, int i)
        {
            bool ret = false;
            SoundFontInfo sf = MidiPlayerGlobal.CurrentMidiSet.SoundFonts[i];
            string soundFontPath = Path.Combine(Application.dataPath + "/", MidiPlayerGlobal.PathToSoundfonts);
            string path = Path.Combine(soundFontPath, sf.Name);
            if (!string.IsNullOrEmpty(path) &&
                EditorUtility.DisplayDialog(
                    $"Remove SoundFont {sf.Name}",
                    $"Remove SoundFont {sf.Name} ?\n\nIf you click ok, the content of this folder will be deleted:\n\n{path}",
                    "ok", "cancel"))
            {
                try
                {
                    if (Directory.Exists(path))
                        Directory.Delete(path, true);
                    if (File.Exists(path + ".meta"))
                        File.Delete(path + ".meta");
                    ret = true;
                }
                catch (Exception ex)
                {
                    Debug.Log("Remove SF " + ex.Message);
                }
                AssetDatabase.Refresh();
                ToolsEditor.CheckMidiSet();
            }
            return ret;
        }
    }
}
#endif