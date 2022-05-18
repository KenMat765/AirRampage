using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class JetAudioController : MonoBehaviour
{
    AudioSource jetAudio;
    const float default_pitch = 0.5f;

    void Start()
    {
        jetAudio = GetComponent<AudioSource>();
        jetAudio.pitch = default_pitch;
    }

    public void ChangeJetPitch(float end_pitch, float duration = 0)
    {
        jetAudio.DOPitch(end_pitch, duration);
    }

    public void ResetJetPitch(float duration = 0)
    {
        jetAudio.DOPitch(default_pitch, duration);
    }
}
