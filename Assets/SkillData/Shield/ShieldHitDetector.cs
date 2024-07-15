using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ShieldHitDetector : MonoBehaviour
{
    [SerializeField] Collider spr_collider;
    [SerializeField] GameObject impact_origin;
    List<GameObject> impacts;

    // Shieldクラスからセット
    float shield_durability;

    public void DecreaseDurability(float damage) => shield_durability -= damage;

    float exhaust_speed;

    Shield shield;
    Material mat;
    int property_Id;
    float activate_duration = 1f;
    int enemy_bullet_layer;

    Tweener activate_tweener;
    [SerializeField] AudioSource activateAudio;


    void Awake()
    {
        shield = GetComponentInParent<Shield>();

        mat = GetComponent<Renderer>().material;
        property_Id = Shader.PropertyToID("_Height");
        mat.SetFloat(property_Id, -1);

        // impactsを生成
        int default_count = 3;
        impacts = new List<GameObject>();
        for (int k = 0; k < default_count; k++)
        {
            impacts.Add(Instantiate(impact_origin, transform));
            impacts[k].SetActive(false);
        }

        // 敵の弾丸のlayer設定
        if (gameObject.layer == LayerMask.NameToLayer("RedShield")) enemy_bullet_layer = LayerMask.NameToLayer("BlueBullet");
        else if (gameObject.layer == LayerMask.NameToLayer("BlueShield")) enemy_bullet_layer = LayerMask.NameToLayer("RedBullet");
        else Debug.LogError("Layer is not set to Shield Prefab");

        // Set self to Receiver (Used in RPC)
        GetComponentInParent<Receiver>().hitDetector = this;
    }

    void Update()
    {
        shield_durability -= exhaust_speed * Time.deltaTime;

        // Skillのelapsed_timeをshield_durabilityに応じて更新
        shield.MeterDecreaserManual(Mathf.Clamp(shield_durability, 0, shield.shield_durability) * shield.charge_time / shield.shield_durability);

        if (shield_durability < 0)
        {
            shield_durability = 0;
            shield.EndProcess();
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == enemy_bullet_layer)
        {
            // 波紋の演出
            foreach (ContactPoint c_point in other.contacts)
            {
                GameObject impact = GetImpact();
                impact.transform.position = c_point.point;
                impact.transform.rotation = Quaternion.LookRotation(c_point.normal);
            }
        }
    }

    GameObject GetImpact()
    {
        foreach (GameObject impact in impacts)
        {
            if (!impact.activeSelf)
            {
                impact.SetActive(true);
                return impact;
            }
        }
        GameObject new_impact = Instantiate(impact_origin, transform);
        impacts.Add(new_impact);
        new_impact.SetActive(true);
        return new_impact;
    }

    // Shieldクラスから起動
    // Called on every clones.
    public void ShieldActivator(float shield_durability, float exhaust_speed)
    {
        this.shield_durability = shield_durability;
        this.exhaust_speed = exhaust_speed;
        spr_collider.enabled = true;
        gameObject.SetActive(true);

        // Play effect & sound.
        mat.SetFloat(property_Id, -1);    // 破壊された瞬間に起動した時
        activate_tweener = mat.DOFloat(2, property_Id, activate_duration);
        activateAudio.Play();
    }

    // Called from Shield.EndProccess().
    public void DestroyShield(bool immediate = false)
    {
        spr_collider.enabled = false;
        shield_durability = 0;
        if (activate_tweener.IsActive())
        {
            activate_tweener.Kill(true);
        }
        // Throws Null Reference Error of mat when called before activating shield.
        if (mat != null)
        {
            if (immediate)
                mat.SetFloat(property_Id, -1);
            else
                mat.DOFloat(-1, property_Id, activate_duration)
                    .OnComplete(() => gameObject.SetActive(false));
        }
    }
}