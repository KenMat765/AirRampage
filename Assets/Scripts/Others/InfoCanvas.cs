using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Text.RegularExpressions;

public class InfoCanvas : Singleton<InfoCanvas>
{
    protected override bool dont_destroy_on_load { get; set; } = true;
    [SerializeField, Header("UI Components")] RectTransform frameRect;
    [SerializeField] TextMeshProUGUI textBox;
    [SerializeField, Header("Constant Floats")] float openDuration = 0.5f;
    [SerializeField] float typeInterval = 0.1f;
    public enum EnterMode { inMoment, typing }
    float frameScaleX;

    void Start()
    {
        frameScaleX = frameRect.localScale.x;
        frameRect.DOScaleX(0, 0);
        textBox.text = "";
    }

    ///<Summary> Open the Info Canvas frame. </Summary>
    public Tween OpenFrame() => frameRect.DOScaleX(frameScaleX, openDuration);

    ///<Summary> Close the Info Canvas frame. </Summary>
    public Tween CloseFrame() => frameRect.DOScaleX(0, openDuration);

    ///<Summary> Enter text on text box. </Summary>
    ///<param name="text"> Text to enter. </param>
    ///<param name="enterMode"> Determines how to enter the text. </param>
    ///<param name="clearPrevious"> Whether to clear previous text. </param>
    public void EnterText(string text, EnterMode enterMode, bool clearPrevious = true)
    {
        if (clearPrevious) textBox.text = "";

        switch (enterMode)
        {
            case EnterMode.inMoment:
                textBox.text += text;
                break;

            case EnterMode.typing:
                StartCoroutine(TypeAnimation(text, typeInterval));
                break;
        }

        IEnumerator TypeAnimation(string text, float interval)
        {
            string[] chars = Regex.Split(text, @"¥*");
            textBox.text += chars[0];
            for (int k = 1; k < chars.Length; k++)
            {
                yield return new WaitForSeconds(interval);
                textBox.text += chars[k];
            }
        }
    }

    ///<Summary> Open the Info Canvas frame, and Enter text. </Summary>
    public void OpenFrameAndEnterText(string text, EnterMode enterMode, bool clearPrevious = true)
    {
        OpenFrame().OnComplete(() => EnterText(text, enterMode, clearPrevious));
    }
}
