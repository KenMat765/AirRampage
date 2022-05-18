using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReceiver : Receiver
{
    public override void OnDeath()
    {
        string destroyer_name = lastShooter.transform.parent.name;
        string my_name = gameObject.transform.parent.name;

        Color arrowColor;
        if(fighterCondition.team == Team.Red) arrowColor = Color.blue;
        else arrowColor = Color.red;

        Sprite skill_sprite;
        if(lastSkillName == "NormalBlast") skill_sprite = null;
        else skill_sprite = SkillDatabase.I.SearchSkillByName(lastSkillName).GetSprite();

        uGUIMannager.I.BookRepo(destroyer_name, my_name, arrowColor, skill_sprite);
    }



    GameObject lastShooter;
    string lastSkillName;
    public override void Damage(Weapon weapon)
    {
        base.Damage(weapon);
        lastShooter = weapon.owner;
        lastSkillName = weapon.skill_name;
        uGUIMannager.I.ScreenColorSetter(new Color(1,0,0,0.1f));
    }
}
