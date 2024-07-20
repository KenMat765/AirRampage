using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AiCondition : FighterCondition
{
    protected override void Start()
    {
        base.Start();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
    }


    public override void HPDecreaser(float deltaHP)
    {
        if (!IsOwner) return;
        base.HPDecreaser(deltaHP);
        HpDecreaser_UIServerRPC(Hp);
    }

    [ServerRpc]
    public void HpDecreaser_UIServerRPC(float curHp)
    {
        float normHp = curHp.Normalize(0, defaultHp);
        HpDecreaser_UIClientRPC(normHp);
    }

    [ClientRpc]
    void HpDecreaser_UIClientRPC(float normHp)
    {
        uGUIMannager.I.HPDecreaser_UI(fighterNo.Value, normHp);
    }


    protected override void OnDeath(int killer_no, string cause_of_death)
    {
        base.OnDeath(killer_no, cause_of_death);

        // Report BattleConductor that you are killed. (Only Host)
        if (IsHost)
        {
            BattleConductor.I.OnFighterDestroyed(this, killer_no, cause_of_death);
        }

        // Send uGUIManger to report death of this fighter.
        string my_name = fighterName.Value.ToString();
        Team my_team = fighterTeam.Value;
        if (IsSpecificDeath(cause_of_death))  // Died from causes other than enemy attacks.
        {
            uGUIMannager.I.BookRepo(cause_of_death, my_name, my_team, cause_of_death);
        }
        else  // Died from enemy attacks.
        {
            string destroyer_name = ParticipantManager.I.fighterInfos[killer_no].fighterCondition.fighterName.Value.ToString();
            uGUIMannager.I.BookRepo(destroyer_name, my_name, my_team, cause_of_death);
        }
    }

    protected override void OnRevival()
    {
        base.OnRevival();
        uGUIMannager.I.ResetHP_UI(fighterNo.Value);
    }
}
