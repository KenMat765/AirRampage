using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Text.RegularExpressions;

public class InfoCanvas : Singleton<InfoCanvas>
{
    protected override bool dont_destroy_on_load { get; set; } = true;
    [SerializeField, Header("UI Components")] RectTransform frameRect;
    [SerializeField] Button closeButton;
    [SerializeField] TextMeshProUGUI textBox, closeButtonText;
    [SerializeField] Image guardPanel;
    [SerializeField, Header("Constant Floats")] float openDuration = 0.5f;
    [SerializeField] float closeDuration = 0.3f, typeInterval = 0.1f;
    [SerializeField, Header("Button Text Colors")] Color buttonTextEnabledColor;
    [SerializeField] Color buttonTextDisabledColor;
    public bool isFrameOpen { get; private set; }
    public enum EnterMode { inMoment, typing }
    float frameScaleX;

    protected override void Awake()
    {
        base.Awake();
        frameScaleX = frameRect.localScale.x;
        frameRect.DOScaleX(0, 0);
        isFrameOpen = false;
        textBox.text = "";
        closeButton.onClick.AddListener(() => CloseFrame());
        CloseButtonInteract(true);
        GuardActivate(false);
    }

    ///<Summary> Open the Info Canvas frame. </Summary>
    public Tween OpenFrame()
    {
        if (isFrameOpen) return null;
        isFrameOpen = true;
        GuardActivate(true);
        return frameRect.DOScaleX(frameScaleX, openDuration);
    }

    ///<Summary> Close the Info Canvas frame. </Summary>
    public Tween CloseFrame()
    {
        if (!isFrameOpen) return null;
        isFrameOpen = false;
        GuardActivate(false);
        return frameRect.DOScaleX(0, closeDuration);
    }

    ///<Summary> Enter text on text box. </Summary>
    ///<param name="text"> Text to enter. </param>
    ///<param name="enterMode"> Determines how to enter the text. </param>
    ///<param name="clearPrevious"> Whether to clear previous text. </param>
    public void EnterText(string text, EnterMode enterMode = EnterMode.inMoment, bool clearPrevious = true)
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

    ///<Summary> Set close button interactable or not. </Summary>
    public void CloseButtonInteract(bool interactable)
    {
        closeButton.interactable = interactable;
        if (interactable) closeButtonText.color = buttonTextEnabledColor;
        else closeButtonText.color = buttonTextDisabledColor;

    }

    ///<Summary> Open the Info Canvas frame, and Enter text. </Summary>
    public void OpenFrameAndEnterText(string text, EnterMode enterMode = EnterMode.inMoment, bool clearPrevious = true)
    {
        if (isFrameOpen)
        {
            // If the frame is already opened, just enter the text.
            EnterText(text, enterMode, clearPrevious);
        }
        else
        {
            OpenFrame()
            .OnStart(() =>
            {
                // Clear previous text BEFORE the frame opens.
                if (clearPrevious) EnterText("", EnterMode.inMoment);
            })
            // No need to clear previous text, because it will be cleared before the frame opens.
            .OnComplete(() => EnterText(text, enterMode, false));
        }
    }

    ///<Summary> Activate screen guard to disable input to other canvas. </Summary>
    public void GuardActivate(bool activate) => guardPanel.raycastTarget = activate;
}
