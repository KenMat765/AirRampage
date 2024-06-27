using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;


// Owner of terminal is always host.
public abstract class Terminal : NetworkBehaviour
{
    [Header("Identity")]
    public int No;

    [Header("Point Per Second")]
    [SerializeField] int point_per_sec;

    [Header("Default Status")]
    [SerializeField] float defaultHp;
    [SerializeField] Team defaultTeam;


    [Header("Current Status")]
    public float Hp;

    // Default value should only be set from inspector.
    [SerializeField] float skillProtection;   // 0 ~ 1.

    public float SkillProtection
    {
        get { return skillProtection; }
        set
        {
            skillProtection = value;
            skillProtection = Mathf.Clamp01(skillProtection);
        }
    }


    [Header("Material Colors")]
    [SerializeField, ColorUsage(false, true)] Color defaultColor;
    [SerializeField, ColorUsage(false, true)] Color redColor;
    [SerializeField, ColorUsage(false, true)] Color blueColor;


    // Which team this terminal belongs to now.
    public Team team { get; private set; } = Team.NONE;

    // Owner fighter of this terminal.
    public int ownerFighterNo;

    int lastShooterNo = -1;

    // Terminal LayerIndexes. (Index of the layers. Ex: terrain=6, structure=22, ...)
    public static int defaultLayer { get; private set; } = 19;
    public static int redLayer { get; private set; } = 20;
    public static int blueLayer { get; private set; } = 21;

    // Terminal LayerMasks. (Mask of the layers. Equals to 1 << layerIndex)
    public static LayerMask defaultMask { get; private set; } = 1 << defaultLayer;
    public static LayerMask redMask { get; private set; } = 1 << redLayer;
    public static LayerMask blueMask { get; private set; } = 1 << blueLayer;
    public static LayerMask allMask { get; private set; } = defaultMask + redMask + blueMask;

    // Terminal Material.
    Material material;

    // Circle Impact Effect.
    ParticleSystem circleImpact;

    public bool acceptDamage { get; set; }



    public virtual void SetupTerminal()
    {
        // // Initialize current HP.
        // Hp = defaultHp;

        // // Get the second material (= Emit Material) of renderer.
        // material = GetComponent<Renderer>().materials[1];

        // // Get circle impact effect.
        // circleImpact = GetComponentInChildren<ParticleSystem>();

        // // Initialize owner fighter No.
        // if (defaultTeam == Team.NONE) ownerFighterNo = -1;
        // else ownerFighterNo = GameInfo.GetNoFromTeam(defaultTeam, 0);

        // // Change to default team.
        // ChangeTerminalTeam(defaultTeam);
    }



    // If argument is null, terminal will belong to no team.
    protected virtual void ChangeTerminalTeam(Team new_team)
    {
        Team old_team = team;
        team = new_team;
        ParticleSystem.MainModule mainModule = circleImpact.main;

        switch (old_team)
        {
            case Team.RED: TerminalManager.redPoint_per_second -= point_per_sec; break;
            case Team.BLUE: TerminalManager.bluePoint_per_second -= point_per_sec; break;
        }
        switch (new_team)
        {
            case Team.RED:
                gameObject.layer = redLayer;
                material.SetColor("_EmissionColor", redColor);
                mainModule.startColor = new Color(redColor.r, redColor.g, redColor.b, 1);
                circleImpact.Play();
                TerminalManager.redPoint_per_second += point_per_sec;
                break;
            case Team.BLUE:
                gameObject.layer = blueLayer;
                material.SetColor("_EmissionColor", blueColor);
                mainModule.startColor = new Color(blueColor.r, blueColor.g, blueColor.b, 1);
                circleImpact.Play();
                TerminalManager.bluePoint_per_second += point_per_sec;
                break;
            case Team.NONE:
                gameObject.layer = defaultLayer;
                material.SetColor("_EmissionColor", defaultColor);
                break;
        }
    }

    [ClientRpc] void ChangeTerminalTeamClientRpc(Team new_team) => ChangeTerminalTeam(new_team);


    // HPDown is always called from weapon.
    // Weapon tells terminal weather you are skill or not, because terminal can not distinguish by themself.
    public void Damage(float power, bool is_skill, int fighterNo)
    {
        // if (!acceptDamage) return;

        // // Decrease HP.
        // float damage = is_skill ? power * (1 - skillProtection) : power;
        // Hp -= damage;

        // // Set fighter as last shooter when not zako.
        // int[] enemy_nos;
        // switch (team)
        // {
        //     case Team.RED: enemy_nos = GameInfo.GetNosFromTeam(Team.BLUE); break;
        //     case Team.BLUE: enemy_nos = GameInfo.GetNosFromTeam(Team.RED); break;
        //     default: enemy_nos = GameInfo.GetAllNos(); break;
        // }
        // if (enemy_nos.Contains(fighterNo)) lastShooterNo = fighterNo;

        // // Terminal automaticaly Falls when Hp is 0.
        // if (Hp > 0) return;

        // // Fall terminal when HP is less than 0.
        // Fall(fighterNo);
    }

    // Damage is always called at server.
    [ServerRpc(RequireOwnership = false)]
    public void DamageServerRpc(float power, bool is_skill, int fighterNo) => Damage(power, is_skill, fighterNo);

    void Fall(int faller_no)
    {
        // // Reset HP.
        // Hp = defaultHp;

        // // Reset Skill Protection.
        // skillProtection = 0.75f;

        // // Get new team.
        // Team old_team = team;
        // Team new_team = ParticipantManager.I.fighterInfos[faller_no].fighterCondition.fighterTeam.Value;

        // // Change team to new team.
        // if (BattleInfo.isMulti) ChangeTerminalTeamClientRpc(new_team);
        // else ChangeTerminalTeam(new_team);

        // // Set the new owner of this terminal.
        // // If terminal was fell only by zakos (this seldom happens), set the first fighter of new team as the owner.
        // if (lastShooterNo == -1)
        // {
        //     ownerFighterNo = GameInfo.GetNoFromTeam(new_team, 0);
        // }
        // else
        // {
        //     Team lastShooter_team = ParticipantManager.I.fighterInfos[lastShooterNo].fighterCondition.fighterTeam.Value;
        //     if (lastShooter_team == new_team) ownerFighterNo = lastShooterNo;
        //     else ownerFighterNo = GameInfo.GetNoFromTeam(new_team, 0);
        // }

        // // Reset last shooter.
        // lastShooterNo = -1;

        // // Invoke event in TerminalManager.
        // TerminalManager.I.OnTerminalFallEvent(old_team, new_team);
    }


    ///<summary> Returns random subtarget position around this terminal. </summary>
    public Vector3 GetRandomSubTargetPositionAround()
    {
        return subtargetsAround.RandomChoice().position;
    }



    // For preset.
    [Header("Sub Targets Around")]
    [SerializeField] Transform[] subtargetsAround;
    [SerializeField] float detectDistance;

    [NaughtyAttributes.Button("GetSubtargetsAround")]
    void GetSubtargetsAround()
    {
        Collider[] subtarget_cols = Physics.OverlapSphere(transform.position, detectDistance, 1 << 7);
        subtargetsAround = subtarget_cols.Select(c => c.transform).ToArray();
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectDistance);
    }
}
