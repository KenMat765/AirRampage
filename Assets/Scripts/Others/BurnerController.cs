using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BurnerController : MonoBehaviour
{
    ParticleSystem.MainModule burner_particle_left, burner_particle_right;
    ParticleSystem burner_impact_left, burner_impact_right;
    float default_size;
    FighterCondition fighterCondition;  // Lookup speed from this.
    float prev_speed;

    // Audio
    [SerializeField] AudioSource jetAudio, burstAudio;
    float default_pitch;
    public void PlayBurstAudio()
    {
        burstAudio.Play();
    }

    void Start()
    {
        Transform left_trans = transform.Find("AfterBurnerLeft");
        Transform right_trans = transform.Find("AfterBurnerRight");
        burner_particle_left = left_trans.GetComponent<ParticleSystem>().main;
        burner_particle_right = right_trans.GetComponent<ParticleSystem>().main;
        default_size = burner_particle_left.startSize.constant;

        Transform impact_left_trans = transform.Find("BurnerImpactLeft");
        Transform impact_right_trans = transform.Find("BurnerImpactRight");
        burner_impact_left = impact_left_trans.GetComponent<ParticleSystem>();
        burner_impact_right = impact_right_trans.GetComponent<ParticleSystem>();

        fighterCondition = transform.root.GetComponent<FighterCondition>();

        default_pitch = jetAudio.pitch;
    }

    void FixedUpdate()
    {
        float cur_speed = fighterCondition.speed;
        if (cur_speed != prev_speed)
        {
            prev_speed = cur_speed;

            float particle_size = default_size * (cur_speed / fighterCondition.defaultSpeed);
            burner_particle_left.startSizeMultiplier = particle_size;
            burner_particle_right.startSizeMultiplier = particle_size;

            float pitch = default_pitch * (cur_speed / fighterCondition.defaultSpeed);
            jetAudio.pitch = pitch;
        }
    }

    public void PlayImpact(Direction direction = 0)
    {
        float end_size = default_size * 3;
        float inc_duration = 0.0f;
        float dec_duration = 0.2f;
        float interval = 0.2f;

        Sequence left_seq = DOTween.Sequence();
        left_seq.Append(DOTween.To(() => burner_particle_left.startSizeMultiplier, (v) => burner_particle_left.startSizeMultiplier = v, end_size, inc_duration));
        left_seq.AppendInterval(interval);
        left_seq.Append(DOTween.To(() => burner_particle_left.startSizeMultiplier, (v) => burner_particle_left.startSizeMultiplier = v, default_size, dec_duration));

        Sequence right_seq = DOTween.Sequence();
        right_seq.Append(DOTween.To(() => burner_particle_right.startSizeMultiplier, (v) => burner_particle_right.startSizeMultiplier = v, end_size, inc_duration));
        right_seq.AppendInterval(interval);
        right_seq.Append(DOTween.To(() => burner_particle_right.startSizeMultiplier, (v) => burner_particle_right.startSizeMultiplier = v, default_size, dec_duration));

        switch (direction)
        {
            case Direction.left:
                left_seq.Play();
                burner_impact_left.Play();
                break;

            case Direction.right:
                right_seq.Play();
                burner_impact_right.Play();
                break;

            default:
                left_seq.Play();
                right_seq.Play();
                burner_impact_left.Play();
                burner_impact_right.Play();
                break;
        }

        left_seq.Kill();
        right_seq.Kill();
    }
}
