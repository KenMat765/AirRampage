using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class WindController : MonoBehaviour
{
    ParticleSystem.MainModule wind_particle_main;
    ParticleSystem.EmissionModule wind_particle_emission;
    const float default_speed = 1;
    const float default_emission = 0;

    void Start()
    {
        ParticleSystem wind = GetComponent<ParticleSystem>();
        wind_particle_main = wind.main;
        wind_particle_emission = wind.emission;

        ResetWind();
    }

    public void WindGenerator(float speed, float emission)
    {
        wind_particle_main.simulationSpeed = speed;
        wind_particle_emission.rateOverTime = emission;
    }

    public void ResetWind()
    {
        wind_particle_main.simulationSpeed = default_speed;
        wind_particle_emission.rateOverTime = default_emission;
    }
}
