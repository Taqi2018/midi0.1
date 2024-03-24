using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

using DAW.CommonClasses;

namespace DAW
{
    public class KeyboardUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Other Components")]
        [SerializeField] TMP_Text keyText;
        [SerializeField] Image keyImage;
        public KeyInfo keyInfo;

        [Header("State Colors")]
        [SerializeField] Color[] boardColors = new Color[2];
        [SerializeField] Color[] fontColors = new Color[2];
        [SerializeField] Color blackKeyColor = new Color(51, 51, 51, 1f);
        [SerializeField] Color whiteKeyColor = Color.white;

        [Header("Keyboard State")]
        [SerializeField] bool isDown;

        // Start is called before the first frame update
        void Start()
        {
            FindComponents();
        }

        void FindComponents()
        {
            if (!keyText) keyText = GetComponentInChildren<TMP_Text>();
            if (!keyImage) keyImage = GetComponent<Image>();
        }

        void InitVariables(){
            isDown = false;
        }

        public void InitKeyboard(KeyInfo k)
        {
            FindComponents();
            InitVariables();
            keyInfo = k;
            keyText.text = keyInfo.name + keyInfo.octave.ToString();
            if (keyInfo.areaType == AreaType.BlackKeys)
            {
                keyImage.color = blackKeyColor;
                keyText.color = whiteKeyColor;
            }
            else
            {
                keyImage.color = whiteKeyColor;
                keyText.color = blackKeyColor;
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (keyInfo == null) return;
            if(isDown) return;
            isDown = true;
            Global._engine.InsertDownKey(keyInfo.midiNote);
        }

        public void OnPointerUp(PointerEventData data)
        {   
            if(!isDown) return;
            isDown = false;
            Global._engine.RemoveUpKey(keyInfo.midiNote);
        }

    }
}