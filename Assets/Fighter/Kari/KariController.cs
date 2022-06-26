using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KariController : MonoBehaviour
{
    [SerializeField] Image leftStick, leftStickBack;
    static Vector2 firstStickPos;
    public static bool onStick { get; private set; }
    public static Vector2 norm_diffPos { get; private set; }

    void Start()
    {
        firstStickPos = leftStick.rectTransform.anchoredPosition;
    }

    void Update()
    {
        LeftStickMannager();
    }

    void LeftStickMannager()
    {
        TouchExtension[] onStick_touches;
        float detect_radius = leftStickBack.rectTransform.rect.width / 2;
        if (CSManager.currentTouches.FindElement(s => (s.start_pos.Screen2Canvas(AnchorPosition.LeftDown) - firstStickPos).sqrMagnitude < Mathf.Pow(detect_radius, 2), out onStick_touches))
        {
            onStick = true;
        }
        else
        {
            onStick = false;
            leftStick.rectTransform.anchoredPosition = firstStickPos;
            norm_diffPos = Vector2.zero;
        }

        if (onStick)
        {
            float diffPosMaxMag = leftStickBack.rectTransform.rect.width / 2;
            Vector2 diffPos;
            TouchExtension onStick_touch = onStick_touches[0];  // 最初に Left Stick に触れたもののみを検知
            Vector2 onStick_touch_canvas_pos = onStick_touch.current_pos.Screen2Canvas(AnchorPosition.LeftDown);

            if ((onStick_touch_canvas_pos - firstStickPos).sqrMagnitude < Mathf.Pow(diffPosMaxMag, 2)) { diffPos = onStick_touch_canvas_pos - firstStickPos; }
            else { diffPos = (onStick_touch_canvas_pos - firstStickPos).normalized * diffPosMaxMag; }
            leftStick.rectTransform.anchoredPosition = firstStickPos + diffPos;
            norm_diffPos = diffPos / diffPosMaxMag;
        }
    }
}
