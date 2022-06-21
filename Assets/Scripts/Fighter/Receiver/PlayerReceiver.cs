using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReceiver : Receiver
{
    public override void OnDeath(int destroyerNo, string destroyerSkillName)
    {
        base.OnDeath(destroyerNo, destroyerSkillName);

        string destroyer_name = ParticipantManager.I.fighterInfos[destroyerNo].fighterCondition.fighterName.Value.ToString();
        string my_name = fighterCondition.fighterName.Value.ToString();

        Color arrowColor;
        if (fighterCondition.fighterTeam.Value == Team.RED) arrowColor = Color.blue;
        else arrowColor = Color.red;

        Sprite skill_sprite;
        if (destroyerSkillName == "NormalBlast") skill_sprite = null;
        else skill_sprite = SkillDatabase.I.SearchSkillByName(destroyerSkillName).GetSprite();

        uGUIMannager.I.BookRepo(destroyer_name, my_name, arrowColor, skill_sprite);
    }

    public override void OnWeaponHit(int fighterNo)
    {
        uGUIMannager.I.ScreenColorSetter(new Color(1, 0, 0, 0.1f));
    }
}
