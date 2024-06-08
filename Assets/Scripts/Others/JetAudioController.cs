using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


// Updates audio of AfterBurners according to fighters speed.
public class JetAudioController : MonoBehaviour
{
    AudioSource jetAudio;
    float default_pitch;
    FighterCondition fighterCondition;  // Lookup speed from this.
    float prev_speed;

    void Start()
    {
        jetAudio = GetComponent<AudioSource>();
        default_pitch = jetAudio.pitch;
        fighterCondition = transform.root.GetComponent<FighterCondition>();
    }

    void FixedUpdate()
    {
        float cur_speed = fighterCondition.speed;
        if (cur_speed != prev_speed)
        {
            prev_speed = cur_speed;
            float pitch = default_pitch * (cur_speed / fighterCondition.defaultSpeed);
            ChangeJetPitch(pitch);
        }
    }

    public void ChangeJetPitch(float end_pitch, float duration = 0)
    {
        jetAudio.DOPitch(end_pitch, duration);
    }
}
