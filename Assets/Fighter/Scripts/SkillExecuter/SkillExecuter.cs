using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SkillExecuter : NetworkBehaviour
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

    // Activation RPCs are declared here because Skill component cannnot call RPCs. (they are attached AFTER fighters are spawned)

    [ServerRpc]
    /// <Param name="targetNos">Used for attack & disturb skills to send target fighter numbers.</Param>
    public void SkillActivatorServerRpc(ulong senderId, int skillNo, int[] targetNos = null)
    {
        SkillActivatorClientRpc(senderId, skillNo, targetNos);
    }

    [ClientRpc]
    public void SkillActivatorClientRpc(ulong senderId, int skillNo, int[] targetNos = null)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        skills[skillNo].Activator(targetNos);
    }

    [ServerRpc]
    public void SkillEndProccessServerRpc(ulong senderId, int skillNo)
    {
        SkillEndProccessClientRpc(senderId, skillNo);
    }

    [ClientRpc]
    public void SkillEndProccessClientRpc(ulong senderId, int skillNo)
    {
        if (NetworkManager.Singleton.LocalClientId == senderId) return;
        skills[skillNo].EndProccess();
    }

    protected virtual void OnDeath(int killer_no, string cause_of_death)
    {
        TerminateAllSkills();
    }
}
