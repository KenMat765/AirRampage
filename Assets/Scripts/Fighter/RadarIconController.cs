using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadarIconController : MonoBehaviour
{
    FighterCondition fighterCondition;
    SpriteRenderer spriteRenderer;
    
    void Start()
    {
        fighterCondition = GetComponentInParent<FighterCondition>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if(fighterCondition.isDead)
        {
            if(spriteRenderer.enabled) spriteRenderer.enabled = false;
        }
        else
        {
            if(!spriteRenderer.enabled) spriteRenderer.enabled = true;
        }
    }
}
