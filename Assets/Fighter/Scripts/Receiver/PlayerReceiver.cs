using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReceiver : Receiver
{
    // Must be called on every clients.
    public override void OnDeath(int destroyerNo, string destroyerSkillName)
    {
        base.OnDeath(destroyerNo, destroyerSkillName);

        string my_name = fighterCondition.fighterName.Value.ToString();
        Team my_team = fighterCondition.fighterTeam.Value;

        if (destroyerSkillName == "Crystal")
        {
            uGUIMannager.I.BookRepo("Crystal", my_name, my_team, null);
            return;
        }

        string destroyer_name = ParticipantManager.I.fighterInfos[destroyerNo].fighterCondition.fighterName.Value.ToString();
        Sprite skill_sprite;
        if (destroyerSkillName == "NormalBlast") skill_sprite = null;
        else skill_sprite = SkillDatabase.I.SearchSkillByName(destroyerSkillName.ToString()).GetSprite();

        uGUIMannager.I.BookRepo(destroyer_name, my_name, my_team, skill_sprite);
    }

    public override void OnWeaponHit(int fighterNo)
    {
        // base.OnWeaponHit(fighterNo);
        uGUIMannager.I.ScreenColorSetter(new Color(1, 0, 0, 0.2f));
    }
}
