using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using DG.Tweening;



////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public abstract class Utilities : MonoBehaviour
{
    public static void DelayCall(MonoBehaviour mono, float time, params Action[] actions) { mono.StartCoroutine(Delay(time, actions)); }
    static IEnumerator Delay(float time, params Action[] actions)
    {
        yield return new WaitForSeconds(time);
        foreach (Action action in actions) { action(); }
    }

    public enum FunctionType
    {
        linear,
        convex_down
    }
    public static float R2R(float norm_x, float min_y, float max_y, FunctionType type, float convex_down_sharpness = 2)
    {
        float y;
        switch (type)
        {
            case FunctionType.linear:
                y = (max_y - min_y) * norm_x + min_y;
                break;

            case FunctionType.convex_down:
                y = ((max_y - min_y) / (convex_down_sharpness - 1)) * (Mathf.Pow(convex_down_sharpness, Mathf.Abs(norm_x)) - 1) * Mathf.Sign(norm_x) + min_y;
                break;

            default:
                y = default(float);
                break;
        }
        return y;
    }

    // nullが返ることはない
    // 0が除外されるのを防ぐためのnull許容型
    public static int[] RandomMultiSelect(int min, int max_exclusive, int quantity)
    {
        int count = 0;
        int?[] result_nullable = new int?[quantity];
        while (count < quantity)
        {
            int rand = UnityEngine.Random.Range(min, max_exclusive);
            if (!result_nullable.Contains(rand))
            {
                result_nullable[count] = rand;
                count++;
            }
        }
        int[] result = result_nullable.Select(r => (int)r).ToArray();
        return result;
    }

    // いらない予定
    protected Type String2Type(string class_name)
    {
        var assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
        Type type = assembly.GetType("SkillCollections." + class_name);
        return type;
    }
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////





////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static class MethodExtension
{
    public static T RandomChoice<T>(this IEnumerable<T> ienumerable) { return ienumerable.Any() ? ienumerable.ElementAt(UnityEngine.Random.Range(0, ienumerable.Count())) : default(T); }
    public static float Normalize(this float value, float min_value, float max_value) { return (value - min_value) / (max_value - min_value); }
    public static Vector2 XY(this Vector3 vector)
    {
        float x = vector.x;
        float y = vector.y;
        return new Vector2(x, y);
    }
    public static Vector2 Screen2Canvas(this Vector2 screenPos, AnchorPosition anchorPosition = AnchorPosition.Center)
    {
        float ratioX = screenPos.x / Screen.width;
        float ratioY = screenPos.y / Screen.height;
        Vector2 canvasPos = Vector2.zero;
        switch (anchorPosition)
        {
            case AnchorPosition.Center:
                canvasPos = new Vector2((ratioX - 0.5f) * CanvasManager.canvas_width, (ratioY - 0.5f) * CanvasManager.canvas_height);
                break;

            case AnchorPosition.LeftUp:
                canvasPos = new Vector2(ratioX * CanvasManager.canvas_width, (ratioY - 1f) * CanvasManager.canvas_height);
                break;

            case AnchorPosition.LeftDown:
                canvasPos = new Vector2(ratioX * CanvasManager.canvas_width, ratioY * CanvasManager.canvas_height);
                break;

            case AnchorPosition.RightUp:
                canvasPos = new Vector2((ratioX - 1f) * CanvasManager.canvas_width, (ratioY - 1f) * CanvasManager.canvas_height);
                break;

            case AnchorPosition.RightDown:
                canvasPos = new Vector2((ratioX - 1f) * CanvasManager.canvas_width, ratioY * CanvasManager.canvas_height);
                break;
        }
        return canvasPos;
    }
    public static Vector2 ScaleCanvasPos(this Vector2 canvasPos)
    {
        float x_scale = CanvasManager.canvas_scale.x;
        float y_scale = CanvasManager.canvas_scale.y;
        Vector2 norm_canvasPos = new Vector2(canvasPos.x * x_scale, canvasPos.y * y_scale);
        return norm_canvasPos;
    }
    public static Vector2 UnScaleCanvasPos(this Vector2 scaled_canvasPos)
    {
        float x_scale = CanvasManager.canvas_scale.x;
        float y_scale = CanvasManager.canvas_scale.y;
        Vector2 canvasPos = new Vector2(scaled_canvasPos.x / x_scale, scaled_canvasPos.y / y_scale);
        return canvasPos;
    }
    public static GameObject[] GetAllChildren(this GameObject parent)
    {
        GameObject[] children = new GameObject[parent.transform.childCount];
        for (int k = 0; k < children.Length; k++) { children[k] = parent.transform.GetChild(k).gameObject; }
        return children;
    }
    public static bool FindElement<T>(this IEnumerable<T> ienumerable, Func<T, bool> condition, out T[] element)
    {
        if (ienumerable.Any(s => condition(s)))
        {
            element = ienumerable.Where(t => condition(t)).ToArray();
            return true;
        }
        else
        {
            element = default(T[]);
            return false;
        }
    }
    public static T[] GetComponentsInChildrenWithoutSelf<T>(this GameObject self) where T : Component
    { return self.GetComponentsInChildren<T>().Where(c => c.gameObject != self).ToArray(); }
    public static bool ContainLayer(this int layerMask, int layer)
    { return ((1 << layer) & layerMask) != 0; }

    public static void LetOutRect(this RectTransform rect, Direction direction, float offset, float duration)
    {
        switch (direction)
        {
            case Direction.up:
                rect.DOLocalMoveY(CanvasManager.canvas_height / 2 + offset, duration);
                break;

            case Direction.down:
                rect.DOLocalMoveY(-(CanvasManager.canvas_height / 2 + offset), duration);
                break;

            case Direction.left:
                rect.DOLocalMoveX(-(CanvasManager.canvas_width / 2 + offset), duration);
                break;

            case Direction.right:
                rect.DOLocalMoveX(CanvasManager.canvas_width / 2 + offset, duration);
                break;
        }
    }
}

public enum Direction { up, down, left, right }
public enum AnchorPosition { Center, LeftUp, LeftDown, RightUp, RightDown }
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////