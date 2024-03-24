using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckRederable : MonoBehaviour
{
    // Start is called before the first frame update
    CanvasRenderer _renderer;
    void Start()
    {
        _renderer = this.GetComponent<CanvasRenderer>();
    }

    // Update is called once per frame
    float curtime = 0f;
    void Update()
    {
        if (Time.time - curtime > 1f)
        {
            if (_renderer != null && _renderer.GetAlpha() > 0)
                Debug.Log("Canvas renderable is shown!!!!" + _renderer.GetAlpha());
            else
                Debug.Log("Canvas renderable is shown!!!!");
            curtime = Time.time;
        }
    }
}
