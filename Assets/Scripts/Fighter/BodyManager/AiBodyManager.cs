using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiBodyManager : BodyManager
{
    void OnBecameVisible() { visible = true; }
    void OnBecameInvisible() { visible = false; }
}
