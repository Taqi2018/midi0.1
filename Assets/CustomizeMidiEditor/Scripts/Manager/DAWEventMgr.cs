using System.Collections;
using UnityEngine;

namespace DAW
{
    public class DAWEventMgr : MonoBehaviour
    {
        public delegate void TouchEvent();
        public delegate void KeyboardEvent(int note);

        public event TouchEvent OnTouchEvent;
        public event KeyboardEvent OnKeyEvent;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            if (OnTouchEvent != null)
            {
                OnTouchEvent?.Invoke();
                OnTouchEvent = null;
            }
        }
    }
}
