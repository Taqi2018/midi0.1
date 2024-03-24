using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidiPlayerTK
{
    // Singleton class to manage all global features of MPTK.
    public partial class MidiPlayerGlobal : MonoBehaviour
    {
        /// <summary>@brief
        /// Full path to SoundFont file (.sf2) or URL to load. 
        /// Defined in the MidiPlayerGlobal editor inspector. 
        /// Must start with file:// or http:// or https://.
        /// @version Maestro Pro 
        /// </summary>
        public string MPTK_LiveSoundFont;

        /// <summary>@brief 
        /// Status of the last SoundFonrloaded. The status is updated in a coroutine, so the status can change at each frame.
        /// @version 2.11.2
        /// </summary>
        [HideInInspector]
        public static LoadingStatusSoundFontEnum MPTK_StatusLastSoundFontLoaded;


        /// <summary>@brief
        /// Change the current Soundfont on fly. If MidiFilePlayer are running, they are stopped and optionally restarted.
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="name">SoundFont name</param>
        /// <param name="restartPlayer">if a MIDI is playing, restart the current playing midi</param>
        public static void MPTK_SelectSoundFont(string name, bool restartPlayer = true)
        {
            if (Application.isPlaying)
                Routine.RunCoroutine(SelectSoundFontThread(name, restartPlayer), Segment.RealtimeUpdate);
            else
                SelectSoundFont(name);
        }

        /// <summary>@brief
        /// Set default soundfont
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="restartPlayer"></param>
        /// <returns></returns>
        private static IEnumerator<float> SelectSoundFontThread(string name, bool restartPlayer = true)
        {
            if (!string.IsNullOrEmpty(name))
            {
                int index = CurrentMidiSet.SoundFonts.FindIndex(s => s.Name == name);
                if (index >= 0)
                {
                    MidiPlayerGlobal.CurrentMidiSet.SetActiveSoundFont(index);
                    MidiPlayerGlobal.CurrentMidiSet.Save();
                }
                else
                {
                    Debug.LogWarning("SoundFont not found: " + name);
                    yield return 0;
                }
            }
            // Load selected soundfont
            yield return Routine.WaitUntilDone(Routine.RunCoroutine(LoadSoundFontThread(restartPlayer), Segment.RealtimeUpdate));
        }

        /// <summary>@brief
        /// Select and load a SF when editor
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="name"></param>
        private static void SelectSoundFont(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                int index = CurrentMidiSet.SoundFonts.FindIndex(s => s.Name == name);
                if (index >= 0)
                {
                    MidiPlayerGlobal.CurrentMidiSet.SetActiveSoundFont(index);
                    MidiPlayerGlobal.CurrentMidiSet.Save();
                    // Load selected soundfont
                    LoadSoundFont();
                }
                else
                {
                    Debug.LogWarning("SoundFont not found " + name);
                }
            }
        }

        /// <summary>@brief
        ///  Load a SoundFont on the fly when application is running. SoundFont is loaded from a local file or from the web.
        ///  If some Midis are playing they are restarted.
        ///  Loading is done in background (coroutine), so method return immediately
        /// @version Maestro Pro 
        /// </summary>
        /// <param name="pPathSF">Full path to SoudFont file. Must start with file:// for local desktop loading or with or http:// or https:// for loading from web resource. if null, use MPTK_LiveSoundFont</param>
        /// <param name="defaultBank">default bank to use for instrument, default or -1 to select the first bank</param>
        /// <param name="drumBank">bank to use for drum kit, default or -1 to select the last bank</param>
        /// <param name="restartPlayer">Restart midi player if need, default is true</param>
        /// <param name="useCache">Reuse already downloaded SF if exist, default is true</param>
        /// <param name="log">Display log, default is false</param>
        /// <returns>
        ///     - true if loading is in progress, use OnEventPresetLoaded to get information when loading is over, for example MPTK_StatusLastSoundFontLoaded
        ///     - false if an error is detected in parameters. The callback OnEventPresetLoaded is not call uf return is false.
        /// </returns>
        static public bool MPTK_LoadLiveSF(string pPathSF = null, int defaultBank = -1, int drumBank = -1, bool restartPlayer = true, bool useCache = true, bool log = false)
        {
            MPTK_StatusLastSoundFontLoaded = LoadingStatusSoundFontEnum.InProgress;
            timeToDownloadSoundFont = TimeSpan.Zero;
            timeToLoadSoundFont = TimeSpan.Zero;
            timeToLoadWave = TimeSpan.Zero;
            MPTK_CountWaveLoaded = 0;

            string pathSF = string.IsNullOrEmpty(pPathSF) ? instance.MPTK_LiveSoundFont : pPathSF;

            if (string.IsNullOrEmpty(pathSF))
            {
                MPTK_StatusLastSoundFontLoaded = LoadingStatusSoundFontEnum.InvalidURL;
                Debug.LogWarning("MPTK_LoadLiveSF: SoundFont path not defined");
            }
            else if (!pathSF.ToLower().StartsWith("file://") &&
                     !pathSF.ToLower().StartsWith("http://") &&
                     !pathSF.ToLower().StartsWith("https://"))
            {
                MPTK_StatusLastSoundFontLoaded = LoadingStatusSoundFontEnum.InvalidURL;
                Debug.LogWarning("MPTK_LoadLiveSF: path to SoundFont must start with file:// or http:// or https:// - found: '" + pathSF + "'");
            }
            else
            {
                MidiSynth[] synths = FindObjectsOfType<MidiSynth>();
                if (Application.isPlaying)
                    Routine.RunCoroutine(ImSoundFont.LoadLiveSF(pathSF, defaultBank, drumBank, synths, restartPlayer, useCache, log), Segment.RealtimeUpdate);
                else
                    Routine.RunCoroutine(ImSoundFont.LoadLiveSF(pathSF, defaultBank, drumBank, synths, restartPlayer, useCache, log), Segment.EditorUpdate);
                return true;
            }
            return false;
        }

        // not yet available ... perhaps never!
        static public bool MPTK_MergeLiveSF(string pPathSF)
        {
            string pathSF = string.IsNullOrEmpty(pPathSF) ? instance.MPTK_LiveSoundFont : pPathSF;

            if (string.IsNullOrEmpty(pathSF))
                Debug.LogWarning("MPTK_MergeLiveSF: SoundFont path not defined");
            else if (!pathSF.ToLower().StartsWith("file://") &&
                     !pathSF.ToLower().StartsWith("http://") &&
                     !pathSF.ToLower().StartsWith("https://"))
                Debug.LogWarning("MPTK_MergeLiveSF: path to SoundFont must start with file:// or http:// or https:// - found: '" + pathSF + "'");
            else
            {
                //           Routine.RunCoroutine(ImSoundFont.MergeLiveSF(pathSF), Segment.RealtimeUpdate);
                return true;
            }
            return false;
        }
    }
}
