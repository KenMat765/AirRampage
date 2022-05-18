using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Animator))]
public class Capsule : MonoBehaviour
{
    [SerializeField] Button openButton, returnButton;
    [SerializeField] RectTransform capsuleRect, textRect, returnRect;
    [SerializeField] Animator animator;

    Vector3 first_pos, first_scale;
    Vector3 text_first_pos, return_first_pos;

    bool selected = false;

    const float duration = 0.15f;

    public Action finish_open_action {get; set;}
    public Action start_close_action {get; set;}



    public void Selected()
    {
        selected = true;
        openButton.interactable = false;

        Vector2 textOffset = new Vector2(-250, -64);    // Textの右上からのOffset
        Vector3 text_rectTrans = new Vector3(CanvasManager.canvas_width/2 + textOffset.x, CanvasManager.canvas_height/2 + textOffset.y, 0);
        textRect.DOLocalMove(text_rectTrans, duration);

        Vector3 position = new Vector3(0, -50, 0);
        capsuleRect.DOLocalMove(position, duration);
        Vector3 scale = new Vector3(1.45f, 1.45f, 1);
        capsuleRect.DOScale(scale, duration);
        OpenCapsule();

        if(returnButton.interactable) returnButton.interactable = false;
        returnRect.DOAnchorPosX(100, duration).OnComplete(() => returnButton.interactable = true);
    }

    public void Exit(string diretion)
    {
        selected = false;
        openButton.interactable = false;

        // 外にどれくらい出すか
        const float outOffset_vertical = 150;
        const float outOffset_horizontal = 250;
        // TextのCapsuleからのOffset
        const float text_capsule_Offset = 150;
        switch(diretion)
        {
            case "up":
            float up_destination = CanvasManager.canvas_height/2 + outOffset_vertical;
            capsuleRect.DOLocalMoveY(up_destination, duration);
            textRect.DOLocalMoveY(up_destination + text_capsule_Offset, duration);
            break;

            case "down":
            float down_destination = -(CanvasManager.canvas_height/2 + outOffset_vertical + 50);    // Textの分だけ余計に(-50)下げる
            capsuleRect.DOLocalMoveY(down_destination, duration);
            textRect.DOLocalMoveY(down_destination + text_capsule_Offset, duration);
            break;

            case "left":
            float left_destination = -(CanvasManager.canvas_width/2 + outOffset_horizontal);
            capsuleRect.DOLocalMoveX(left_destination, duration);
            textRect.DOLocalMoveX(left_destination, duration);
            break;

            case "right":
            float right_destination = CanvasManager.canvas_width/2 + outOffset_horizontal;
            capsuleRect.DOLocalMoveX(right_destination, duration);
            textRect.DOLocalMoveX(right_destination, duration);
            break;

            default :
            break;
        }
    }

    public void Return()
    {
        if(selected)
        {
            selected = false;

            CloseCapsule();
            capsuleRect.DOLocalMove(first_pos, duration);
            capsuleRect.DOScale(first_scale, duration)
                .OnComplete(() => { textRect.DOMove(text_first_pos, duration)
                    .OnComplete(() => openButton.interactable = true); });

            returnButton.interactable = false;
            returnRect.DOMove(return_first_pos, duration);
        }
        else
        {
                capsuleRect.DOLocalMove(first_pos, duration).SetDelay(duration);
                textRect.DOMove(text_first_pos, duration).SetDelay(duration)
                    .OnComplete(() => openButton.interactable = true);
        }
    }

    public void OpenCapsule()
    {
        animator.SetInteger("capsule_state", 1);
        CapsuleAudios.I.open_capsule.Play();
    }

    public void CloseCapsule()
    {
        animator.SetInteger("capsule_state", -1);
        CapsuleAudios.I.close_capsule.Play();
    }

    public void OnFinishOpen() { if(finish_open_action != null) finish_open_action(); }
    public void OnStartClose() { if(start_close_action != null) start_close_action(); }



    void Start()
    {
        first_pos = capsuleRect.localPosition;
        first_scale = capsuleRect.localScale;
        text_first_pos = textRect.position;
        return_first_pos = returnRect.position;
    }
}