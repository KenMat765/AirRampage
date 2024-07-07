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

        int touch_count = Input.touchCount;
        if (touch_count > 0 && currentTouches.Count <= detectableNum)
        {
            for (int i = 0; i < Mathf.Min(touch_count, detectableNum); i++)
            {
                Touch touch = Input.GetTouch(i);
                int Id = touch.fingerId;
                switch (touch.phase)
                {
                    // New touch detected.
                    case TouchPhase.Began:
                        if (!currentTouches.ContainsKey(Id))
                        {
                            TouchExtension touch_began = new TouchExtension(touch, touch.position, 3);
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
                            TouchExtension touch_exist = currentTouches[Id];
                            touch_exist.UpdateTouch(touch);
                            currentTouches[Id] = touch_exist;
                        }
                        break;
                }
            }
        }
        else
        {
            currentTouches.Clear();
        }
    }

    bool SwipeCheck(TouchExtension touch)
    {
        if (!swipe_condition(touch))
        {
            return false;
        }

        if (touch.prev_drag_speed.Count == 0)
        {
            return false;
        }

        float max_drag_speed = touch.prev_drag_speed.Max();
        if (max_drag_speed > swipeThresh)
        {
            float swipe_angle = Vector2.SignedAngle(Vector2.up, touch.delta_pos);
            if (swipe_angle >= -45 && swipe_angle < 45)
            {
                swipeUp = true;
            }
            else if (swipe_angle >= 45 && swipe_angle < 135)
            {
                swipeLeft = true;
            }
            else if (swipe_angle >= -135 && swipe_angle < -45)
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
    public FixedSizeQueue<float> prev_drag_speed { get; set; }
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
        get { return delta_pos.magnitude / touch.deltaTime; }
    }
    public TouchExtension(Touch touch, Vector2 start_pos, int record_frames)
    {
        this.touch = touch;
        this.start_pos = start_pos;
        this.prev_drag_speed = new FixedSizeQueue<float>(record_frames);
    }
    public void UpdateTouch(Touch new_touch)
    {
        touch = new_touch;
        prev_drag_speed.Enqueue(drag_speed);
    }
}