using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;
using static MidiPlayerTK.MPTKInnerLoop;

namespace MidiPlayerTK
{
    /// <summary>
    /// Setting for MIDI inner loop [Pro].
    /// Look at MidiFilePlayer.MPTK_InnerLoop  
    /// Example:
    /// @snippet TestInnerLoop.cs ExampleMidiInnerLoop
    /// </summary>
    public class MPTKInnerLoop
    {
        /// <summary>@brief
        /// Enable logging message
        /// </summary>
        public bool Log;

        /// <summary>@brief
        /// Loop phase action sent to #OnEventInnerLoop
        /// </summary>
        public enum InnerLoopPhase
        {
            /// <summary>
            /// Start the loop
            /// </summary>
            Start,

            /// <summary>
            /// Resume the loop
            /// </summary>
            Resume,

            /// <summary>
            /// Exit the loop
            /// </summary>
            Exit
        }

        /// <summary>@brief
        /// Unity event triggered when a loop occurs. 
        /// parameters: 
        ///     - InnerLoopPhase  current loop phase
        ///     - long            current tick player (MPTK_TickPlayer)
        ///     - long            tick target (End)
        ///     - long            loop count (Count).
        /// return: 
        ///     - boolean         true continue looping, false exit loop.
        /// @note 
        ///     - this action is done from the MIDI thread, not from the Unity thread. 
        ///     - It's not possible to call Unity API (only Debug.Log).
        ///     - it's a managed thread, so all variables from your script are visible.
        /// </summary>
        public Func<InnerLoopPhase, long, long, int, bool> OnEventInnerLoop;

        /// <summary>@brief
        /// Enable or disable the loop. Default is false.
        /// </summary>
        public bool Enabled;

        /// <summary>@brief
        /// Become true when looping is over or OnEventInnerLoop return false. Set to false at start.
        /// </summary>
        public bool Finished;

        /// <summary>@brief
        /// Tick position where the loop begin when the MIDI start. The MIDI sequencer go immediately to this position.\n
        /// if #Start > #Resume the loop will begin at #Resume position. Default is 0.
        /// </summary> 
        public long Start;

        /// <summary>@brief
        /// Tick position to resume the loop when MidiLoad.MPTK_TickPlayer >= to #End. Default is 0.
        /// </summary>
        public long Resume;

        /// <summary>@brief
        /// Tick position to trigger the loop restart to the #Resume position (when MidiLoad.MPTK_TickPlayer >= to #End). Default is 0.
        /// </summary>
        public long End;

        /// <summary>@brief
        /// Maximum iteration for the loop including the first. When #Count >= #Max the MIDI sequencer continue to the next MIDI events AFTER TICK #eND.\n
        /// Set to 0 for infinite loop. Default is 0.
        /// </summary>
        public int Max;

        /// <summary>@brief
        /// Current loop count. Default is 0.
        /// </summary>
        public int Count;

        [Preserve]
        public MPTKInnerLoop()
        {
        }

        public void Clear()
        {
            //Debug.Log("MPTKInnerLoop Clear");
            Enabled = false;
            Finished = false;
            Start = 0;
            Resume = 0;
            End = 0;
            End = 0;
            Count = 0;
        }

        public override string ToString()
        {
            return $"MPTKInnerLoop Enabled:{Enabled} Finished:{Finished} Start:{Start} Resume:{Resume} End:{End} Count:{Count}/{Max}";
        }
    }
}
