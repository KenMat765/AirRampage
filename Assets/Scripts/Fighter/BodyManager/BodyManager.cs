using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyManager : MonoBehaviour
{
    public FighterCondition fighterCondition {get; set;}

    // Must be called on every clients.
    public void OnDeath()
    {
        gameObject.SetActive(false);
    }
    public void OnRevival()
    {
        transform.localPosition = Vector3.zero;
        gameObject.SetActive(true);
    }



    // ParticipantManagerから参照できるようにするため、基底クラスで定義 (× AiBodyManager)
    public bool visible {get; set;}
    void OnBecameVisible() { visible = true; }
    void OnBecameInvisible() { visible = false; }
}
