using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoShooter : MonoBehaviour
{
    [SerializeField] GameObject originalNormalBullet;
    List<GameObject> normalBullets;
    List<Weapon> normalWeapons;

    float power, speed, lifespan;

    public void SetupAutoShooter(float power, float speed, float lifespan)
    {
        this.power = power;
        this.speed = speed;
        this.lifespan = lifespan;
        PoolNormalBullets(10);
    }

    void PoolNormalBullets(int quantity)
    {
        Vector3 bullet_position = originalNormalBullet.transform.position;
        Quaternion bullet_rotation = originalNormalBullet.transform.rotation;

        normalBullets = new List<GameObject>();
        normalWeapons = new List<Weapon>();

        for (int k = 0; k < quantity; k++)
        {
            GameObject bullet = Instantiate(originalNormalBullet, bullet_position, bullet_rotation, transform);
            normalBullets.Add(bullet);
            Weapon weapon = bullet.GetComponent<Weapon>();
            normalWeapons.Add(weapon);
            // weapon.WeaponSetter(gameObject, this, false, "NormalBlast");
            // weapon.WeaponParameterSetter(power, speed, lifespan, homingType);
        }
    }

    int GetNormalBulletIndex()
    {
        Vector3 bullet_position = originalNormalBullet.transform.position;
        Quaternion bullet_rotation = originalNormalBullet.transform.rotation;
        foreach (GameObject normalBullet in normalBullets) { if (normalBullet.activeSelf == false) { return normalBullets.IndexOf(normalBullet); } }
        GameObject newBullet = Instantiate(originalNormalBullet, bullet_position, bullet_rotation, transform);   //全て使用中だったら新たに作成
        normalBullets.Add(newBullet);
        Weapon weapon = newBullet.GetComponent<Weapon>();
        normalWeapons.Add(weapon);
        // weapon.WeaponSetter(gameObject, this, false, "NormalBlast");
        // weapon.WeaponParameterSetter(power, speed, lifespan, homingType);
        return normalBullets.IndexOf(newBullet);
    }

    ///<param name="target"> Put null when there are no targets. </param>
    protected void NormalBlast(GameObject target)
    {
        Weapon bullet = normalWeapons[GetNormalBulletIndex()];
        bullet.Activate(target);
    }
}
