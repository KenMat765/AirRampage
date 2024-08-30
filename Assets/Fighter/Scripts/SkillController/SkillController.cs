using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SkillController : NetworkBehaviour
{
    // Set in ParticipantManager.Awake
    public Skill[] skills { get; set; } = new Skill[GameInfo.MAX_SKILL_COUNT];
    public FighterCondition fighterCondition { get; protected set; }
    public bool has_skillKeep { get; set; } = false;

    protected virtual void Awake()
    {
        fighterCondition = GetComponentInParent<FighterCondition>();
        fighterCondition.OnDeathCallback += OnDeath;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        fighterCondition.OnDeathCallback -= OnDeath;
    }

    protected virtual void OnDeath(int killed_no, int killer_no, Team killed_team, string cause_of_death)
    {
        TerminateAllSkills();
    }

    public void SkillActivator(int skill_no)
    {
        Skill skill = skills[skill_no];

        // Activate for Owner.
        int[] send_data = skill.Activator();

        // Send RPC to other clients.
        ulong owner_id = NetworkManager.Singleton.LocalClientId;
        if (IsHost)
            SkillActivatorClientRpc(owner_id, skill_no, send_data);
        else
            SkillActivatorServerRpc(owner_id, skill_no, send_data);
    }

    [ServerRpc]
    /// <Param name="targetNos">Used for attack & disturb skills to send target fighter numbers.</Param>
    void SkillActivatorServerRpc(ulong sender_id, int skill_no, int[] send_data = null)
    {
        SkillActivatorClientRpc(sender_id, skill_no, send_data);
    }

    [ClientRpc]
    void SkillActivatorClientRpc(ulong sender_id, int skill_no, int[] send_data = null)
    {
        if (NetworkManager.Singleton.LocalClientId == sender_id) return;
        Skill skill = skills[skill_no];
        skill.Activator(send_data);
    }

    // Called from Skill.EndProcess when the skill ends.
    [ServerRpc]
    public void SkillEndProcessServerRpc(ulong sender_id, int skill_no)
    {
        SkillEndProcessClientRpc(sender_id, skill_no);
    }

    [ClientRpc]
    public void SkillEndProcessClientRpc(ulong sender_id, int skill_no)
    {
        if (NetworkManager.Singleton.LocalClientId == sender_id) return;
        Skill skill = skills[skill_no];
        skill.EndProcess();
    }

    // Stop charging and disable the use of skills.
    public void LockAllSkills(bool lock_skill)
    {
        foreach (Skill skill in skills)
        {
            if (skill != null)
            {
                if (skill.isUsing)
                {
                    skill.ForceTermination(true);
                }
                skill.isLocked = lock_skill;
            }
        }
    }

    // Terminate all currently active skills.
    public void TerminateAllSkills()
    {
        bool maintain_charge = has_skillKeep;
        foreach (Skill skill in skills)
        {
            if (skill != null)
            {
                skill.ForceTermination(maintain_charge);
            }
        }
    }
}
