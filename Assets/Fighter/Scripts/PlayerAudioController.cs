using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerAudioController : MonoBehaviour
{
    [SerializeField] PlayerAudio[] playerAudios;

    void Start()
    {
        // Multi Game.
        if (BattleInfo.isMulti)
        {
            bool isOwner = GetComponent<FighterCondition>().IsOwner;
            if (isOwner)
            {
                foreach (PlayerAudio playerAudio in playerAudios)
                {
                    AudioSource audio = playerAudio.audio;
                    audio.spatialBlend = 0;
                    audio.volume = playerAudio.volume;
                }
            }

        }

        // Solo Game.
        else
        {
            foreach (PlayerAudio playerAudio in playerAudios)
            {
                AudioSource audio = playerAudio.audio;
                audio.spatialBlend = 0;
                audio.volume = playerAudio.volume;
            }

        }
    }
}

[System.Serializable]
public class PlayerAudio
{
    public AudioSource audio;
    public float volume;
}