using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadarIconController : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    static Color red = new Color((0xFF4044 >> 16 & 0xFF) / 255.0f, (0xFF4044 >> 8 & 0xFF) / 255.0f, (0xFF4044 & 0xFF) / 255.0f);
    static Color blue = new Color((0x8DAAFF >> 16 & 0xFF) / 255.0f, (0x8DAAFF >> 8 & 0xFF) / 255.0f, (0x8DAAFF & 0xFF) / 255.0f);

    void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

    public void Visualize(bool visualize) => spriteRenderer.enabled = visualize;

    public void ChangeRadarIconColor(Team team)
    {
        switch (team)
        {
            case Team.RED: spriteRenderer.color = red; break;
            case Team.BLUE: spriteRenderer.color = blue; break;
            case Team.NONE: spriteRenderer.color = Color.clear; break;
        }
    }
}