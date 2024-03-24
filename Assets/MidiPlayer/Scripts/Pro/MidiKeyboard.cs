using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    /// Base class to send and receive Midi Message from a Midi keyboard connected to the desktop\n
    /// @version Maestro Pro 
    /// 
    /// More information here https://paxstellar.fr/class-midikeyboard/
    /// </summary>
    public class MidiKeyboard
    {
        /// <summary>@brief
        /// General error return values 
        /// </summary>
        public enum PluginError
        {
            OK = 0,  // no error 
            UNSPECIFIED = 1,  // unspecified error 
            BADDEVICEID = 2,  // device ID out of range 
            DRIVERNOTENABLED = 3,  // driver failed enable 
            DEVICEALLOCATED = 4,  // device already allocated 
            INVALHANDLE = 5,  // device handle is invalid 
            NODRIVER = 6,  // no device driver present 
            NOMEM = 7,  // memory allocation error 
            NOTSUPPORTED = 8,  // function isn't supported 
            BADERRNUM = 9,  // error value out of range 
            INVALFLAG = 10, // invalid flag passed 
            INVALPARAM = 11, // invalid parameter passed 
            HANDLEBUSY = 12, // handle being used simultaneously on another thread (eg callback, 
            INVALIDALIAS = 13, // specified alias not found 
            BADDB = 14, // bad registry database 
            KEYNOTFOUND = 15, // registry key not found 
            READERROR = 16, // registry read error 
            WRITEERROR = 17, // registry write error 
            DELETEERROR = 18, // registry delete error 
            VALNOTFOUND = 19, // registry value not found 
            NODRIVERCB = 20, // driver does not call DriverCallback 
            MOREDATA = 21, // more data to be returned 
            LASTERROR = 21, // last error in range 
        }

        /// <summary>@brief
        /// Event triggered when a Midi message is available.
        /// @code
        /// if (enableRealTimeRead)
        /// {
        ///     MidiKeyboard.OnActionInputMidi += ProcessEvent;
        ///     MidiKeyboard.MPTK_SetRealTimeRead();
        /// }
        /// else
        /// {
        ///     MidiKeyboard.OnActionInputMidi -= ProcessEvent;
        ///     MidiKeyboard.MPTK_UnsetRealTimeRead();
        /// }
        /// @endcode
        /// </summary>
        [HideInInspector]
        public static event Action<MPTKEvent> OnActionInputMidi;// = (impactPoint) => { };

        //public static EventMidiClass OnEventInputMidi;

        private static string msgPluginsNotFound = "MidiKeyboard Plugin not found, please see here to setup https://paxstellar.fr/class-midikeyboard/";

        /// <summary>@brief
        /// Empty the read queue
        /// </summary>
        /// <returns></returns>
        [DllImport("MidiKeyboard", EntryPoint = "MPTKClearReadQueue")]
        static public extern int MPTK_ClearReadQueue();

        /// <summary>@brief
        /// Read a Midi message from all devices input connected 
        /// </summary>
        /// <returns></returns>
        static public MPTKEvent MPTK_Read()
        {
            // Pop from the queue.
            ulong data = 0;
            try
            {
                data = _mptkRead();
            }
            catch (Exception)
            {
                Debug.LogWarning(msgPluginsNotFound);
                return null;
            }
            if (data == 0)
                // No more midi message, go out
                return null;
            else
                // Parse the message.
                return new MPTKEvent(data);
        }
        [DllImport("MidiKeyboard", EntryPoint = "MPTKRead")]
        static private extern ulong _mptkRead();

        /// <summary>@brief
        /// Count of midi message waiting in the read queue
        /// </summary>
        /// <returns></returns>
        [DllImport("MidiKeyboard", EntryPoint = "MPTKSizeReadQueue")]
        static public extern int MPTK_SizeReadQueue();

        /// <summary>@brief
        /// Count of output device detected
        /// </summary>
        /// <returns></returns>
        [DllImport("MidiKeyboard", EntryPoint = "MPTKCountInp")]
        static public extern int MPTK_CountInp();

        /// <summary>@brief
        /// Count of input device detected
        /// </summary>
        /// <returns></returns>
        [DllImport("MidiKeyboard", EntryPoint = "MPTKCountOut")]
        static public extern int MPTK_CountOut();

        /// <summary>@brief
        /// Name of the device
        /// </summary>
        /// <param name="index">Id of the device</param>
        /// <returns></returns>
        static public string MPTK_GetInpName(int index)
        {
            return Marshal.PtrToStringAnsi(_mptkGetInpName(index));
        }
        [DllImport("MidiKeyboard", EntryPoint = "MPTKGetInpName")]
        static private extern System.IntPtr _mptkGetInpName(int index);

        /// <summary>@brief
        /// Name of the device
        /// </summary>
        /// <param name="index">Id of the device</param>
        /// <returns></returns>
        static public string MPTK_GetOutName(int index)
        {
            return Marshal.PtrToStringAnsi(_mptkGetOutName(index));
        }
        [DllImport("MidiKeyboard", EntryPoint = "MPTKGetOutName")]
        static private extern System.IntPtr _mptkGetOutName(int index);

        //
        // Write to a dedicated midi
        // -------------------------

        /// <summary>@brief
        /// Open device for output
        /// </summary>
        /// <param name="index"></param>
        [DllImport("MidiKeyboard", EntryPoint = "MPTKOpenOut")]
        static public extern void MPTK_OpenOut(int index);

        /// <summary>@brief
        /// Close device for output
        /// </summary>
        /// <param name="index"></param>
        [DllImport("MidiKeyboard", EntryPoint = "MPTKCloseOut")]
        static public extern void MPTK_CloseOut(int index);

        /// <summary>@brief
        /// Play one midi event on the device with a thread so the call return immediately.
        /// </summary>
        /// <param name="evnt">Midi event</param>
        /// <param name="device">index of the device</param>
        static public void MPTK_PlayEvent(MPTKEvent evnt, int device)
        {
            ulong data = evnt.ToData();
            //Debug.Log($"Send {data:X}");
            if (evnt.Delay <= 0)
            {
                // for testing 0x00403C90
                _mptkWrite(device, data);
            }
            else
            {
                Thread thread = new Thread(() => delayedPlayThread(device, data, evnt.Delay));
                thread.Start();
            }
        }

        static private void delayedPlayThread(int device, ulong data, float delayMS)
        {
            TimeSpan time = TimeSpan.FromMilliseconds((double)delayMS);
            Thread.Sleep(time);
            //Debug.Log($"Delayed send {data:X}");
            _mptkWrite((int)device, (ulong)data);
        }
        // exemple 0x00403C90
        [DllImport("MidiKeyboard", EntryPoint = "MPTKWrite")]
        static private extern void _mptkWrite(int index, ulong data);

        //
        // Read from a dedicated midi - excluded from this version, rather use MPTKOpenAllInp
        //

        //[DllImport("MidiKeyboard", EntryPoint = "MPTKOpenInp")]
        //static public extern void MPTK_OpenInp(int index);

        //[DllImport("MidiKeyboard", EntryPoint = "MPTKCloseInp")]
        //static public extern void MPTK_CloseInp(int index);

        /// <summary>@brief
        /// Open or refresh all input device for receiving Midi message
        /// </summary>
        [DllImport("MidiKeyboard", EntryPoint = "MPTKOpenAllInp")]
        static public extern void MPTK_OpenAllInp();

        /// <summary>@brief
        /// Close all input device for receiving Midi message
        /// </summary>
        [DllImport("MidiKeyboard", EntryPoint = "MPTKCloseAllInp")]
        static public extern void MPTK_CloseAllInp();

        /// <summary>@brief
        /// Exclude system message
        /// </summary>
        /// <param name="exclude">If true exclude all messages with status/command >= 0xF0. Default: true</param>
        static public void MPTK_ExcludeSystemMessage(bool exclude)
        {
            mptkExcludeSystemMessage(exclude);
        }
        [DllImport("MidiKeyboard", EntryPoint = "MPTKExcludeSystemMessage")]
        static private extern void mptkExcludeSystemMessage(bool exclude);

        /// <summary>@brief
        /// Get current version of the plugins
        /// </summary>
        /// <returns></returns>
        static public string MPTK_Version()
        {
            string version = msgPluginsNotFound;
            try
            {
                version = Marshal.PtrToStringAnsi(_mptkVersion());
            }
            catch (Exception)
            {
                version = msgPluginsNotFound;
            }
            return version;
        }
        [DllImport("MidiKeyboard", EntryPoint = "MPTKVersion")]
        static private extern System.IntPtr _mptkVersion();


        [DllImport("MidiKeyboard", EntryPoint = "MPTKIVersion")]
        static private extern int MPTK_iVersion();


        /// <summary>@brief
        /// Enable read midi event from a callback. The event OnActionInputMidi is triggred when a Midi event is available.
        /// @code
        /// if (enableRealTimeRead)
        /// {
        ///     MidiKeyboard.OnActionInputMidi += ProcessEvent;
        ///     MidiKeyboard.MPTK_SetRealTimeRead();
        /// }
        /// else
        /// {
        ///     MidiKeyboard.OnActionInputMidi -= ProcessEvent;
        ///     MidiKeyboard.MPTK_UnsetRealTimeRead();
        /// }
        /// @endcode
        /// </summary>
        public static void MPTK_SetRealTimeRead()
        {
            MPTKSetMidiMsgCB(new MidiMsgDelegate(MidiMsgCB));
        }
        // Set a CB to return Midi event
        public delegate void MidiMsgDelegate(ulong data);
        [DllImport("MidiKeyboard")]
        private static extern void MPTKSetMidiMsgCB(MidiMsgDelegate fp);

        private static void MidiMsgCB(ulong data)
        {
            MPTKEvent midievent = null;
            try
            {
                midievent = new MPTKEvent(data);
            }
            catch (Exception)
            {
                Debug.LogWarning(msgPluginsNotFound);
            }
            if (midievent != null)
            {
                //Debug.Log("MidiMsgCB " + midievent.ToString());
                // Call event with these midi events
                try
                {
                    if (OnActionInputMidi != null)
                        OnActionInputMidi.Invoke(midievent);
                }
                catch (Exception ex)
                {
                    Debug.LogError("OnActionInputMidi: exception detected. Check the callback code");
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>@brief
        /// Disable read midi event from a callback. Mandatory before exiting the application, moreover with inside Unity editor to avoid crash.
        /// </summary>
        public static void MPTK_UnsetRealTimeRead()
        {
            try
            {
                //Debug.Log($"MPTK_UnsetRealTimeRead");
                MPTKUnsetMidiMsgCB();
            }
            catch (Exception)
            {
                // Remove exception when plugin not found 
                //Debug.LogWarning($"MPTK_UnsetRealTimeRead {ex.Message}");
            }
        }
        [DllImport("MidiKeyboard")]
        private static extern void MPTKUnsetMidiMsgCB();


        private static void DebugCallBack(System.IntPtr n, int m)
        {
            Debug.Log($"DebugCallBack {Marshal.PtrToStringAnsi(n)} {m}");
        }
        // Set a CB to display information
        public delegate void DebugDelegate(System.IntPtr p1, int p2);
        [DllImport("MidiKeyboard")]
        private static extern void SetDebugCB(DebugDelegate fp);

        [DllImport("MidiKeyboard")]
        private static extern void UnsetDebugCB();

        /// <summary>@brief
        /// Plugins Init. Mandatory before executing any other functions of the plugins
        /// </summary>
        static public bool MPTK_Init()
        {
            try
            {
                if (MPTK_iVersion() >= 12)
                {
                    _mptkInit(159789);
                    return true;
                }
            }
            catch (Exception)
            {
            }
            Debug.LogWarning($"MPTK MidiKeyboard PlugIns version is not correct");
            Debug.LogWarning("Look here to get the last version https://paxstellar.fr/class-midikeyboard");
            return false;
        }
        [DllImport("MidiKeyboard", EntryPoint = "MPTKInit")]
        static private extern void _mptkInit(int sig);

        /// <summary>@brief
        /// Last status, value reset to OK after the call
        /// </summary>
        static public PluginError MPTK_LastStatus
        {
            get
            {
                PluginError error;
                try
                {
                    error = (PluginError)mptkLastStatus();
                }
                catch
                {
                    error = PluginError.UNSPECIFIED;
                }
                return error;
            }
        }
        [DllImport("MidiKeyboard", EntryPoint = "MPTKLastStatus")]
        static private extern int mptkLastStatus();
    }
}

