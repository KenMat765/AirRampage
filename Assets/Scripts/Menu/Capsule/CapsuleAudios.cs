using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapsuleAudios : Singleton<CapsuleAudios>
{
    protected override bool dont_destroy_on_load { get; set; } = false;
    public AudioSource open_capsule;
    public AudioSource close_capsule;
}