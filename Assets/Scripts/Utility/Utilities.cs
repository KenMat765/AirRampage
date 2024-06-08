using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine.UI;



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
    public static IEnumerable<T> RandomizeOrder<T>(this IEnumerable<T> original)
    {
        IEnumerable<T> copy = original.ToList();
        copy = copy.OrderBy(a => Guid.NewGuid()).ToList();
        return copy;
    }
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

    public static T String2Enum<T>(this string str) where T : Enum
    {
        T e = (T)Enum.Parse(typeof(T), str);
        return e;
    }

    public static bool String2Bool(this string str)
    {
        bool b = Convert.ToBoolean(str);
        return b;
    }

    public static int String2Int(this string str)
    {
        int i = int.Parse(str);
        return i;
    }

    // {1,10,...} -> "1/10/..."
    public static string Ints2String(this List<int> ints)
    {
        string str = "";
        foreach (int i in ints)
        {
            str += i.ToString() + "/";
        }
        return str;
    }

    // "1/10/..." -> {1,10,...}
    public static List<int> String2Ints(this string str)
    {
        List<int> ints = new List<int>();
        string str_cashe = "";
        for (int s = 0; s < str.Length; s++)
        {
            char code = str[s];
            if (code == '/')
            {
                int i = int.Parse(str_cashe);
                ints.Append(i);
                str_cashe = "";
            }
            else
            {
                str_cashe += code;
            }
        }
        return ints;
    }

    // {false, true, true, ...} -> "ftt..."
    public static string Bools2String(this IEnumerable<bool> bools)
    {
        string str = "";
        foreach (bool b in bools)
        {
            if (b) str += "t";
            else str += "f";
        }
        return str;
    }

    // "ftt..." -> {false, true, true, ...}
    public static List<bool> String2Bools(this string str)
    {
        List<bool> bools = new List<bool>();
        for (int s = 0; s < str.Length; s++)
        {
            char code = str[s];
            if (code == 't') bools.Add(true);
            else bools.Add(false);
        }
        return bools;
    }

    public static void FadeColor(this Graphic graphic, float alpha)
    {
        Color new_color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, alpha);
        graphic.color = new_color;
    }
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public enum Direction { up, down, left, right }
public enum AnchorPosition { Center, LeftUp, LeftDown, RightUp, RightDown }
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////