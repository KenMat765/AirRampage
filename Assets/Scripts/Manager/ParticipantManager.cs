using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


// Handles every fighter's information, including AIs and Zakos.
public class ParticipantManager : NetworkSingleton<ParticipantManager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    public FighterInfo[] fighterInfos { get; private set; }
    NetworkVariable<bool> allSpawnComplete = new NetworkVariable<bool>(false);
    public int myFighterNo { get; private set; }
    public bool infoSetComplete { get; private set; } = false;
    public int zakoCountAll { get; private set; }
    [SerializeField] GameObject redPlayerPrefab, bluePlayerPrefab, redAiPrefab, blueAiPrefab, zakoPrefab;

    public void FightersSetup() => StartCoroutine(fightersSetup());
    IEnumerator fightersSetup()
    {
        zakoCountAll = SpawnPointManager.I.zakoCountAll;

        fighterInfos = new FighterInfo[GameInfo.MAX_PLAYER_COUNT + zakoCountAll];
        GameObject[] allFighters = new GameObject[GameInfo.MAX_PLAYER_COUNT + zakoCountAll];
        GameObject myPlayer = null;

        yield return new WaitUntil(() => IsSpawned);

        if (NetworkManager.Singleton.IsHost)
        {
            // Generate Players and AIs.
            SpawnAllFighters();
            // Generate Zakos
            SpawnAllZakos();
            // Tell every clients that spawning is finished.
            allSpawnComplete.Value = true;
        }

        // Wait until spawning is finished.
        yield return new WaitUntil(() => allSpawnComplete.Value);

        // Get your own fighter.
        myPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

        // Get generated fighters on each client.
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            int fighterNo = player.GetComponent<FighterCondition>().fighterNo.Value;
            allFighters[fighterNo] = player;
            if (player == myPlayer)
            {
                myFighterNo = fighterNo;
            }
        }

        GameObject[] ais = GameObject.FindGameObjectsWithTag("AI");
        foreach (GameObject ai in ais)
        {
            int fighterNo = ai.GetComponent<FighterCondition>().fighterNo.Value;
            allFighters[fighterNo] = ai;
        }

        GameObject[] zakos = GameObject.FindGameObjectsWithTag("Zako");
        foreach (GameObject zako in zakos)
        {
            int fighterNo = zako.GetComponent<FighterCondition>().fighterNo.Value;
            allFighters[fighterNo] = zako;
        }

        // Process for all fighters /////////////////////////////////////////////////////////////////////////
        for (int no = 0; no < GameInfo.MAX_PLAYER_COUNT + zakoCountAll; no++)
        {
            // Get necessary components.
            GameObject fighter = allFighters[no];
            FighterCondition fighterCondition = fighter.GetComponent<FighterCondition>();
            Movement movement = fighter.GetComponent<Movement>();
            GameObject body = fighter.transform.Find("fighterbody").gameObject;
            Visibility visibility = body.GetComponent<Visibility>();
            Attack attack = body.GetComponent<Attack>();
            Receiver receiver = body.GetComponent<Receiver>();

            // Generate & Assign fighter info.
            FighterInfo info = new FighterInfo(fighter, body, visibility, fighterCondition, movement, attack, receiver);
            fighterInfos[no] = info;

            // Only for Players & AIs
            if (no < GameInfo.MAX_PLAYER_COUNT)
            {
                // Get battle data at fighterNo.
                BattleInfo.ParticipantBattleData battleData = BattleInfo.battleDatas[no];

                // Setup Skills
                SkillController skill_executer = body.GetComponent<SkillController>();
                for (int skillNo = 0; skillNo < GameInfo.MAX_SKILL_COUNT; skillNo++)
                {
                    int? skillId_nullable = battleData.skillIds[skillNo];
                    if (skillId_nullable.HasValue)
                    {
                        int skillId = (int)skillId_nullable;
                        int skillLevel = (int)battleData.skillLevels[skillNo];
                        SkillData skill_data = SkillDatabase.I.SearchSkillById(skillId);
                        Skill skill_script = skill_data.GetScript();
                        SkillLevelData skill_levelData = SkillLevelDatabase.I.SearchSkillById(skillId);
                        LevelData level_data = skill_levelData.GetLevelData(skillLevel);
                        skill_executer.skills[skillNo] = (Skill)body.AddComponent(skill_script.GetType());
                        skill_executer.skills[skillNo].LevelDataSetter(level_data);
                        skill_executer.skills[skillNo].Generator(skillNo, skill_data);
                    }
                    else
                    {
                        skill_executer.skills[skillNo] = null;
                    }
                }

                // Setup Abilities. (Do this AFTER setting up skills, because some abilities refers to attack.skills)
                foreach (int abilityID in battleData.abilities)
                {
                    Ability ability = AbilityDatabase.I.GetAbilityById(abilityID);
                    ability.Introducer(fighterCondition);
                }
            }

            // Initialize fighter status AFTER initializing abilities.
            fighterCondition.InitStatus();

            // Set fighter name to fighterNo for easier reference to fighterNo.
            fighter.name = no.ToString();
        }

        // Setup Player Camera ///////////////////////////////////////////////////////////////////////////
        CameraController.I.SetupPlayerCamera(myPlayer.transform, PlayerInfo.I.viewType);
        CameraManager.SetupCameraInScene();
        GameObject cameraRadar = myPlayer.transform.Find("CameraRadar").gameObject;
        cameraRadar.SetActive(true);

        // All set complete !!
        infoSetComplete = true;
    }



    // Only the host calls this method.
    void SpawnAllFighters()
    {
        for (int no = 0; no < GameInfo.MAX_PLAYER_COUNT; no++)
        {
            // Get battle data of this fighter.
            BattleInfo.ParticipantBattleData? battleData_nullable = BattleInfo.GetBattleDataByFighterNo(no);
            if (!battleData_nullable.HasValue)
            {
                Debug.LogError($"BattleData not set at fighterNo : {no}");
                continue;
            }
            BattleInfo.ParticipantBattleData battleData = (BattleInfo.ParticipantBattleData)battleData_nullable;

            // GameObject & SpawnPoint of spawning fighter.
            GameObject fighter;
            SpawnPointFighter point = SpawnPointManager.I.GetSpawnPointFighter(no);

            // If Player ////////////////////////////////////////////////////////////////////////
            if (battleData.isPlayer)
            {
                // Get client id.
                ulong? clientId_nullable = battleData.clientId;
                if (!clientId_nullable.HasValue)
                {
                    Debug.LogError($"ClientId not set at fighterNo : {no}");
                    continue;
                }
                ulong clientId = (ulong)clientId_nullable;

                // Create fighter.
                if (battleData.team == Team.RED)
                {
                    fighter = Instantiate(redPlayerPrefab, point.transform.position, point.transform.rotation);
                }
                else
                {
                    fighter = Instantiate(bluePlayerPrefab, point.transform.position, point.transform.rotation);
                }

                // Spawn fighter.
                NetworkObject networkObject = fighter.GetComponent<NetworkObject>();
                networkObject.SpawnAsPlayerObject(clientId, true);
            }

            // If AI ////////////////////////////////////////////////////////////////////////////
            else
            {
                // Create fighter.
                if (battleData.team == Team.RED)
                {
                    fighter = Instantiate(redAiPrefab, point.transform.position, point.transform.rotation);
                }
                else
                {
                    fighter = Instantiate(blueAiPrefab, point.transform.position, point.transform.rotation);
                }

                // Spawn fighter.
                NetworkObject networkObject = fighter.GetComponent<NetworkObject>();
                networkObject.Spawn(true);
            }

            // Set NetworkVariables in fighter condition. (Initialize NetworkVariables AFTER spawning)
            FighterCondition fighterCondition = fighter.GetComponent<FighterCondition>();
            fighterCondition.fighterNo.Value = battleData.fighterNo;
            fighterCondition.fighterName.Value = battleData.name;
            fighterCondition.fighterTeam.Value = battleData.team;
        }
    }


    // Only the host calls this method.
    void SpawnAllZakos()
    {
        for (int k = 0; k < SpawnPointManager.I.zakoCountAll; k++)
        {
            int zakoNo = GameInfo.MAX_PLAYER_COUNT + k;

            // Create fighter.
            GameObject zako;
            zako = Instantiate(zakoPrefab);

            // Spawn fighter.
            NetworkObject zako_net = zako.GetComponent<NetworkObject>();
            zako_net.Spawn(true);

            // Set fighterNo & fighterName at fighter condition. (Initialize NetworkVariables AFTER spawning)
            ZakoCondition zako_condition = zako.GetComponent<ZakoCondition>();
            zako_condition.fighterNo.Value = zakoNo;
            zako_condition.fighterName.Value = "Zako";
            zako_condition.fighterTeam.Value = Team.NONE;

            // Add zako to standbys of ZakoCentralManager.
            ZakoCentralManager.I.standbyZakoNos.Add(zakoNo);
        }
    }



    /// <summary> Enable (or unable) controll of multiple fighters. </summary>
    /// <param name="target"> 0:players, 1:zakos, else:all </param>
    public void FightersActivationHandler(int target, bool activate)
    {
        switch (target)
        {
            case 0:
                for (int no = 0; no < GameInfo.MAX_PLAYER_COUNT; no++) FighterActivationHandler(no, activate);
                break;
            case 1:
                for (int no = GameInfo.MAX_PLAYER_COUNT; no < fighterInfos.Length; no++) FighterActivationHandler(no, activate);
                break;
            default:
                for (int no = 0; no < fighterInfos.Length; no++) FighterActivationHandler(no, activate);
                break;
        }
    }

    [ClientRpc] public void FightersActivationHandlerClientRpc(int target, bool activate) => FightersActivationHandler(target, activate);


    /// <summary> Activate (or unactivate) single fighter. </summary>
    public void FighterActivationHandler(int no, bool activate)
    {
        FighterInfo info = fighterInfos[no];
        info.body.SetActive(activate);
        info.fighterCondition.enabled = activate;
        info.movement.enabled = activate;
        info.attack.enabled = activate;
        info.receiver.enabled = activate;
    }

    [ClientRpc] public void FighterActivationHandlerClientRpc(int no, bool activate) => FighterActivationHandler(no, activate);



    /// <summary> Enable (or unable) controll of multiple fighters. </summary>
    /// <param name="target"> 0:players, 1:zakos, else:all </param>
    public void FightersControllHandler(int target, bool controllable)
    {
        switch (target)
        {
            case 0:
                for (int no = 0; no < GameInfo.MAX_PLAYER_COUNT; no++) FighterControllHandler(no, controllable);
                break;
            case 1:
                for (int no = GameInfo.MAX_PLAYER_COUNT; no < fighterInfos.Length; no++) FighterControllHandler(no, controllable);
                break;
            default:
                for (int no = 0; no < fighterInfos.Length; no++) FighterControllHandler(no, controllable);
                break;
        }
    }

    /// <summary>Enable (or unable) controll of single fighter.</summary>
    public void FighterControllHandler(int no, bool controllable) => fighterInfos[no].movement.Controllable(controllable);

    [ClientRpc] public void FighterControllHandlerClientRpc(int no, bool controllable) => FighterControllHandler(no, controllable);



    /// <summary> Enable (or unable) attack of multiple fighters. </summary>
    /// <param name="target"> 0:players, 1:zakos, else:all </param>
    public void FightersAttackHandler(int target, bool controllable)
    {
        switch (target)
        {
            case 0:
                for (int no = 0; no < GameInfo.MAX_PLAYER_COUNT; no++) FighterAttackHandler(no, controllable);
                break;
            case 1:
                for (int no = GameInfo.MAX_PLAYER_COUNT; no < fighterInfos.Length; no++) FighterAttackHandler(no, controllable);
                break;
            default:
                for (int no = 0; no < fighterInfos.Length; no++) FighterAttackHandler(no, controllable);
                break;
        }
    }

    /// <summary>Enable (or unable) attack of single fighter.</summary>
    public void FighterAttackHandler(int no, bool attackable) => fighterInfos[no].attack.attackable = attackable;

    [ClientRpc] public void FighterAttackHandlerClientRpc(int no, bool attackable) => FighterAttackHandler(no, attackable);



    /// <summary>Enable (or unable) attacks and damages of multiple fighters</summary>
    /// <param name="target"> 0:players, 1:zakos, else:all </param>
    public void FightersAcceptDamageHandler(int target, bool accept)
    {
        switch (target)
        {
            case 0:
                for (int no = 0; no < GameInfo.MAX_PLAYER_COUNT; no++) FighterAcceptDamageHandler(no, accept);
                break;
            case 1:
                for (int no = GameInfo.MAX_PLAYER_COUNT; no < fighterInfos.Length; no++) FighterAcceptDamageHandler(no, accept);
                break;
            default:
                for (int no = 0; no < fighterInfos.Length; no++) FighterAcceptDamageHandler(no, accept);
                break;
        }
    }

    ///<summary>Enable (or unable) attacks and damages of single fighter</summary>
    public void FighterAcceptDamageHandler(int no, bool accept)
    {
        Receiver receiver = fighterInfos[no].receiver;
        receiver.acceptAttack = accept;
    }
}



public struct FighterInfo
{
    public FighterInfo(GameObject fighter, GameObject body, Visibility visibility, FighterCondition fighterCondition, Movement movement, Attack attack, Receiver receiver)
    {
        this.fighter = fighter;
        this.body = body;
        this.visibility = visibility;
        this.fighterCondition = fighterCondition;
        this.movement = movement;
        this.attack = attack;
        this.receiver = receiver;
    }

    public GameObject fighter { get; private set; }
    public GameObject body { get; private set; }
    public Visibility visibility { get; private set; }
    public FighterCondition fighterCondition { get; private set; }
    public Movement movement { get; private set; }
    public Attack attack { get; private set; }
    public Receiver receiver { get; private set; }
}
