using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmPanel : MonoBehaviour
{
    [SerializeField] TMP_Text alertText;
    public GameObject sendObj;
    public string sendMsg;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitPanel(GameObject obj, string msg, string alert)
    {
        gameObject.SetActive(true);
        sendObj = obj;
        sendMsg = msg;
        alertText.text = alert;
    }

    public void OnClicked_1() {
        Debug.Log("Button1 clicked!!!");
        gameObject.SetActive(false);
    }

    public void OnClicked_2() {
        Debug.Log("Button2 clicked!!!");
        gameObject.SetActive(false);
    }
}
