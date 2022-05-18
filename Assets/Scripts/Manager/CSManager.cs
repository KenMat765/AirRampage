using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
public class CSManager : Singleton<CSManager>
{
    protected override bool dont_destroy_on_load { get; set; } = true;
    public static bool swipeUp; public static bool swipeDown; public static bool swipeRight; public static bool swipeLeft;  // LeftStickとBlast以外でのスワイプのみ検知
    static int detectableNum = 4;
    public static TouchExtension[] currentTouches = new TouchExtension[detectableNum];
    public static TouchExtension[] swipes = new TouchExtension[detectableNum];
    public static Func<TouchExtension, bool> swipe_condition = (TouchExtension touch) => { return true; };

    void Update()
    {
        if(Input.touchCount > 0)
        {
            for(int i = 0; i < Mathf.Min(Input.touchCount, detectableNum); i ++)
            {
                Touch touch = Input.GetTouch(i);
                int Id = touch.fingerId;
                if(Id < detectableNum) { currentTouches[Id].touch = touch; }
            }

            for(int k = 0; k < detectableNum; k++)
            {
                int Id = Array.IndexOf(currentTouches, currentTouches[k]);
                switch (currentTouches[k].touch.phase)
                {
                    case TouchPhase.Began:
                    currentTouches[k].start_pos = currentTouches[k].touch.position;
                    break;

                    case TouchPhase.Ended:
                    currentTouches[k].start_pos = default(Vector2);
                    break;
                }
            }
        }

        SwipeChecker();
    }

    void SwipeChecker()
    {
        float swipeThresh = 1200;
        if(currentTouches.FindElement(s => s.drag_speed > swipeThresh, out TouchExtension[] swipes))
        {
            foreach(TouchExtension swipe in swipes)
            {
                if(!swipe_condition(swipe)) { return; }

                if(Vector2.SignedAngle(Vector2.up, swipe.delta_pos) >= -45 && Vector2.SignedAngle(Vector2.up, swipe.delta_pos) < 45)
                {
                    swipeUp = true;
                    swipes.Append(swipe);
                }
                else if(Vector2.SignedAngle(Vector2.up, swipe.delta_pos) >= 45 && Vector2.SignedAngle(Vector2.up, swipe.delta_pos) < 135)
                {
                    swipeLeft = true;
                    swipes.Append(swipe);
                }
                else if(Vector2.SignedAngle(Vector2.up, swipe.delta_pos) >= -135 && Vector2.SignedAngle(Vector2.up, swipe.delta_pos) < -45)
                {
                    swipeRight = true;
                    swipes.Append(swipe);
                }
                else
                {
                    swipeDown = true;
                    swipes.Append(swipe);
                }
            }
        }
        else
        {
            swipeUp = false;
            swipeDown = false;
            swipeRight = false;
            swipeLeft = false;
            swipes = default;
        }
    }
}

public struct TouchExtension
{
    public Touch touch {get; set;}
    public Vector2 start_pos {get; set;}
    public Vector2 current_pos
    {
        get { return (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) ? touch.position : default; }
    }
    public Vector2 delta_pos
    {
        get { return touch.deltaPosition; }
    }
    public float drag_speed
    {
        get { return touch.phase == TouchPhase.Moved ? delta_pos.magnitude / touch.deltaTime : 0; }
    }
}