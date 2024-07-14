using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReceiver : Receiver
{
    public override void OnWeaponHit(int fighterNo)
    {
        base.OnWeaponHit(fighterNo);

        if (!IsOwner) return;

        // Flash the screen red briefly when weapon hit.
        uGUIMannager.I.ScreenColorSetter(new Color(1, 0, 0, 0.2f));
    }

    protected override void OnDeath(int killer_no, string cause_of_death)
    {
        base.OnDeath(killer_no, cause_of_death);

        // Report BattleConductor that you are killed. (Only Host)
        if (IsHost)
        {
            BattleConductor.I.OnFighterDestroyed(fighterCondition, killer_no, cause_of_death);
        }

        // Send uGUIManger to report death of this fighter.
        string my_name = fighterCondition.fighterName.Value.ToString();
        Team my_team = fighterCondition.fighterTeam.Value;
        if (FighterCondition.IsSpecificDeath(cause_of_death))
        {
            uGUIMannager.I.BookRepo(cause_of_death, my_name, my_team, cause_of_death);
        }
        else
        {
            string destroyer_name = ParticipantManager.I.fighterInfos[attackerNo].fighterCondition.fighterName.Value.ToString();
            uGUIMannager.I.BookRepo(destroyer_name, my_name, my_team, cause_of_death);
        }
    }
}
