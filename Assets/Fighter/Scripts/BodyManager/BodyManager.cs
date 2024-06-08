using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyManager : MonoBehaviour
{
    // ParticipantManagerから参照できるようにするため、基底クラスで定義 (× AiBodyManager)
    public bool visible { get; set; }
    void OnBecameVisible() { visible = true; }
    void OnBecameInvisible() { visible = false; }
}
