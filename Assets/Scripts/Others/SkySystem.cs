using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SkySystem : MonoBehaviour
{
    public bool flyFighter = true;
    Material sky_material;
    [SerializeField] float angular_speed;
    GameObject fighter;
    float elapsed_time;
    float wait_time;
    [SerializeField] float wait_time_min;
    [SerializeField] float wait_time_max;
    [SerializeField] float duration_min;
    [SerializeField] float duration_max;

    void Start()
    {
        sky_material = RenderSettings.skybox;
        fighter = transform.Find("fighterbody").gameObject;
        wait_time = Random.Range(wait_time_min, wait_time_max);
    }

    void Update()
    {
        sky_material.SetFloat("_Rotation", Mathf.Repeat(sky_material.GetFloat("_Rotation") + angular_speed * Time.deltaTime, 360));

        if (flyFighter)
        {
            elapsed_time += Time.deltaTime;
            if (elapsed_time > wait_time)
            {
                elapsed_time = 0;
                wait_time = Random.Range(wait_time_min, wait_time_max);
                float duration = Random.Range(duration_min, duration_max);
                int direction = Random.Range(0, 2) == 0 ? -1 : 1;

                Vector3 start_pos = default(Vector3);
                Vector3 goal_pos = default(Vector3);
                int route_num = Random.Range(0, 8);
                switch (route_num)
                {
                    case 0:
                        start_pos = new Vector3(1000 * direction, 150, -100);
                        goal_pos = new Vector3(-400 * direction, -100, -1000);
                        break;

                    case 1:
                        start_pos = new Vector3(800 * direction, -100, -1000);
                        goal_pos = new Vector3(-1200 * direction, 200, 0);
                        break;

                    case 2:
                        start_pos = new Vector3(1000 * direction, 300, -1000);
                        goal_pos = new Vector3(-1200 * direction, -100, 0);
                        break;

                    case 3:
                        start_pos = new Vector3(30 * direction, -25, -750);
                        goal_pos = new Vector3(-1350 * direction, 55, 200);
                        break;

                    case 4:
                        start_pos = new Vector3(50 * direction, 55, -750);
                        goal_pos = new Vector3(-1350 * direction, -50, 200);
                        break;

                    case 5:
                        start_pos = new Vector3(-300 * direction, -35, -700);
                        goal_pos = new Vector3(300 * direction, -35, -700);
                        break;

                    case 6:
                        start_pos = new Vector3(-550 * direction, Random.Range(-35, 35), -500);
                        goal_pos = new Vector3(550 * direction, Random.Range(-35, 35), -500);
                        break;

                    case 7:
                        start_pos = new Vector3(-1000 * direction, Random.Range(-150, 150), Random.Range(-800, -300));
                        goal_pos = new Vector3(1000 * direction, Random.Range(-150, 150), Random.Range(-800, -300));
                        break;
                }
                fighter.transform.position = start_pos;
                fighter.transform.rotation = Quaternion.LookRotation(goal_pos - start_pos);
                fighter.transform.DOMove(goal_pos, duration);
            }
        }
    }
}
