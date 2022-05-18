using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponTrans : Weapon
{
    protected override void OnMoving()
    {
        base.OnMoving();
        
        // 移動
        Vector3 myPos = transform.position;
        float deltaPos = speed*Time.deltaTime;
        transform.position = Vector3.MoveTowards(myPos, myPos + transform.forward*deltaPos, deltaPos);
    }
}