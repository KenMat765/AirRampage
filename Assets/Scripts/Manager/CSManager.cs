using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

// !! Touches & Swipes must be checked BEFORE player movement !!
[DefaultExecutionOrder(-100)]
public class CSManager : Singleton<CSManager>
{
    protected override bool dont_destroy_on_load { get; set; } = true;
    public static bool swipeUp; public static bool swipeDown; public static bool swipeRight; public static bool swipeLeft;  // LeftStickとBlast以外でのスワイプのみ検知
    const int detectableNum = 4;
    public static Dictionary<int, TouchExtension> currentTouches = new Dictionary<int, TouchExtension>(detectableNum);
    public static Func<TouchExtension, bool> swipe_condition = (TouchExtension touch) => { return true; };
    float swipeThresh = 1200;

    void Update()
    {
        // Set all swipes false.
        swipeUp = false;
        swipeLeft = false;
        swipeRight = false;
        swipeDown = false;

        if (Input.touchCount > 0 && currentTouches.Count < detectableNum)
        {
            for (int i = 0; i < Mathf.Min(Input.touchCount, detectableNum); i++)
            {
                Touch touch = Input.GetTouch(i);
                int Id = touch.fingerId;
                switch (touch.phase)
                {
                    // New touch detected.
                    case TouchPhase.Began:
                        if (!currentTouches.ContainsKey(Id))
                        {
                            TouchExtension touch_began = new TouchExtension(touch, touch.position, 0);
                            currentTouches.Add(Id, touch_began);
                        }
                        break;

                    // Touch ended.
                    case TouchPhase.Ended:
                        // Check if ended touch was swiped before removing it.
                        if (currentTouches.ContainsKey(Id))
                        {
                            TouchExtension touch_ended = currentTouches[Id];
                            SwipeCheck(touch_ended);
                            currentTouches.Remove(Id);
                        }
                        break;

                    default:
                        // Update touch.
                        if (currentTouches.ContainsKey(Id))
                        {
                            Vector2 start_pos = currentTouches[Id].start_pos;
                            float prev_drag_speed = currentTouches[Id].drag_speed;
                            TouchExtension touch_exist = new TouchExtension(touch, start_pos, prev_drag_speed);
                            currentTouches[Id] = touch_exist;
                        }
                        break;
                }
            }
        }
    }

    bool SwipeCheck(TouchExtension swipe)
    {
        if (!swipe_condition(swipe))
        {
            return false;
        }

        if (swipe.prev_drag_speed > swipeThresh)
        {
            if (Vector2.SignedAngle(Vector2.up, swipe.delta_pos) >= -45 && Vector2.SignedAngle(Vector2.up, swipe.delta_pos) < 45)
            {
                swipeUp = true;
            }
            else if (Vector2.SignedAngle(Vector2.up, swipe.delta_pos) >= 45 && Vector2.SignedAngle(Vector2.up, swipe.delta_pos) < 135)
            {
                swipeLeft = true;
            }
            else if (Vector2.SignedAngle(Vector2.up, swipe.delta_pos) >= -135 && Vector2.SignedAngle(Vector2.up, swipe.delta_pos) < -45)
            {
                swipeRight = true;
            }
            else
            {
                swipeDown = true;
            }
            return true;
        }
        else
        {
            return false;
        }
    }
}

public struct TouchExtension
{
    public Touch touch { get; set; }
    public Vector2 start_pos { get; set; }
    public float prev_drag_speed { get; set; }
    public Vector2 current_pos
    {
        get { return touch.position; }
    }
    public Vector2 delta_pos
    {
        get { return touch.deltaPosition; }
    }
    public float drag_speed
    {
        // get { return touch.phase == TouchPhase.Moved ? delta_pos.magnitude / touch.deltaTime : 0; }
        get { return delta_pos.magnitude / touch.deltaTime; }
    }
    public TouchExtension(Touch touch, Vector2 start_pos, float prev_drag_speed)
    {
        this.touch = touch;
        this.start_pos = start_pos;
        this.prev_drag_speed = prev_drag_speed;
    }
}