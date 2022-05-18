using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BurnerController : MonoBehaviour
{
    ParticleSystem.MainModule burner_particle_left, burner_particle_right;
    TrailRenderer trail_left, trail_right;
    float default_size;
    

    
    void Start()
    {
        Transform left_trans = transform.Find("AfterBurnerLeft");
        Transform right_trans = transform.Find("AfterBurnerRight");

        burner_particle_left = left_trans.GetComponent<ParticleSystem>().main;
        burner_particle_right = right_trans.GetComponent<ParticleSystem>().main;
        default_size = burner_particle_left.startSize.constant;

        trail_left = left_trans.GetComponent<TrailRenderer>();
        trail_right = right_trans.GetComponent<TrailRenderer>();
        trail_left.emitting = false;
        trail_right.emitting = false;
    }



    public void Boost(float end_size, float duration = 0, bool emit_trail = false)
    {
        DOTween.To(() => burner_particle_left.startSizeMultiplier, (v) => burner_particle_left.startSizeMultiplier = v, end_size, duration);
        DOTween.To(() => burner_particle_right.startSizeMultiplier, (v) => burner_particle_right.startSizeMultiplier = v, end_size, duration);

        if(emit_trail)
        {
            trail_left.emitting = true;
            trail_right.emitting = true;
        }
    }

    public void ResetBoost(float duration = 0)
    {
        DOTween.To(() => burner_particle_left.startSizeMultiplier, (v) => burner_particle_left.startSizeMultiplier = v, default_size, duration);
        DOTween.To(() => burner_particle_right.startSizeMultiplier, (v) => burner_particle_right.startSizeMultiplier = v, default_size, duration);
        
        if(trail_left.emitting)
        {
            trail_left.emitting = false;
            trail_right.emitting = false;
        }
    }
}
