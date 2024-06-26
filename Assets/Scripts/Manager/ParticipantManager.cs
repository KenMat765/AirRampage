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

    public void FightersSetup(SpawnPointManager spawnPointManager) => StartCoroutine(fightersSetup(spawnPointManager));
    IEnumerator fightersSetup(SpawnPointManager spawnPointManager)
    {
        zakoCountAll = spawnPointManager.zakoCountAll;

        fighterInfos = new FighterInfo[GameInfo.max_player_count + zakoCountAll];
        GameObject[] allFighters = new GameObject[GameInfo.max_player_count + zakoCountAll];
        GameObject myPlayer = null;

        yield return new WaitUntil(() => IsSpawned);

        if (NetworkManager.Singleton.IsHost)
        {
            // Generate Players and AIs.
            SpawnAllFighters(spawnPointManager);
            // Generate Zakos
            SpawnAllZakos(spawnPointManager);
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
        for (int no = 0; no < GameInfo.max_player_count + zakoCountAll; no++)
        {
            // Get necessary components.
            GameObject fighter = allFighters[no];
            FighterCondition fighterCondition = fighter.GetComponent<FighterCondition>();
            Movement movement = fighter.GetComponent<Movement>();
            GameObject body = fighter.transform.Find("fighterbody").gameObject;
            BodyManager bodyManager = body.GetComponent<BodyManager>();
            Attack attack = body.GetComponent<Attack>();
            Receiver receiver = body.GetComponent<Receiver>();

            // Generate & Assign fighter info.
            FighterInfo info = new FighterInfo(fighter, body, bodyManager, fighterCondition, movement, attack, receiver);
            fighterInfos[no] = info;

            // Only for Players & AIs
            if (no < GameInfo.max_player_count)
            {
                // Get battle data at fighterNo.
                BattleInfo.ParticipantBattleData battleData = BattleInfo.battleDatas[no];

                // Setup Abilities (do this before skill setup)
                foreach (int abilityID in battleData.abilities)
                {
                    Ability ability = AbilityDatabase.I.GetAbilityById(abilityID);
                    ability.Introducer(fighterCondition);
                }

                // Setup Skills
                for (int skillNo = 0; skillNo < GameInfo.max_skill_count; skillNo++)
                {
                    int? skillId_nullable = battleData.skillIds[skillNo];
                    if (skillId_nullable.HasValue)
                    {
                        int skillId = (int)skillId_nullable;
                        int skillLevel = (int)battleData.skillLevels[skillNo];
                        Skill skill_origin = SkillDatabase.I.SearchSkillById(skillId).GetScript();
                        LevelData level_data = SkillLevelDatabase.I.SearchSkillById(skillId).GetLevelData(skillLevel);
                        attack.skills[skillNo] = (Skill)body.AddComponent(skill_origin.GetType());
                        attack.skills[skillNo].LevelDataSetter(level_data);
                        attack.skills[skillNo].Generator();
                        attack.skills[skillNo].skillNo = skillNo;
                    }
                    else
                    {
                        attack.skills[skillNo] = null;
                    }
                }
            }

            // Set fighter name to fighterNo for easier reference to fighterNo.
            fighter.name = no.ToString();

            // For AI Debug.
            // if (no == 3)
            // {
            //     fighter.transform.Find("Kari Camera").gameObject.SetActive(true);
            // }
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
    void SpawnAllFighters(SpawnPointManager spawnPointManager)
    {
        for (int no = 0; no < GameInfo.max_player_count; no++)
        {
            BattleInfo.ParticipantBattleData? battleData_nullable = BattleInfo.GetBattleDataByFighterNo(no);
            if (!battleData_nullable.HasValue)
            {
                Debug.LogError($"BattleData not set at fighterNo : {no}");
                continue;
            }
            BattleInfo.ParticipantBattleData battleData = (BattleInfo.ParticipantBattleData)battleData_nullable;


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
                GameObject fighter;
                SpawnPointFighter point = spawnPointManager.GetSpawnPointFighter(no);
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

                // Set fighterNo & fighterName at fighter condition. (Initialize NetworkVariables AFTER spawning)
                FighterCondition fighterCondition = fighter.GetComponent<FighterCondition>();
                fighterCondition.fighterNo.Value = battleData.fighterNo;
                fighterCondition.fighterName.Value = battleData.name;
                fighterCondition.fighterTeam.Value = battleData.team;
            }


            // If AI ////////////////////////////////////////////////////////////////////////////
            else
            {
                // Create fighter.
                GameObject fighter;
                SpawnPointFighter point = spawnPointManager.GetSpawnPointFighter(no);
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

                // Set fighterNo at fighter & team condition.
                FighterCondition fighterCondition = fighter.GetComponent<FighterCondition>();
                fighterCondition.fighterNo.Value = battleData.fighterNo;
                fighterCondition.fighterName.Value = battleData.name;
                fighterCondition.fighterTeam.Value = battleData.team;
            }
        }
    }


    // Only the host calls this method.
    void SpawnAllZakos(SpawnPointManager spawnPointManager)
    {
        for (int k = 0; k < spawnPointManager.zakoCountAll; k++)
        {
            int zakoNo = GameInfo.max_player_count + k;

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
                for (int no = 0; no < GameInfo.max_player_count; no++) FighterActivationHandler(no, activate);
                break;
            case 1:
                for (int no = GameInfo.max_player_count; no < fighterInfos.Length; no++) FighterActivationHandler(no, activate);
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
        info.fighterCondition.radarIcon.Visualize(activate);
    }

    [ClientRpc] public void FighterActivationHandlerClientRpc(int no, bool activate) => FighterActivationHandler(no, activate);



    /// <summary> Enable (or unable) controll of multiple fighters. </summary>
    /// <param name="target"> 0:players, 1:zakos, else:all </param>
    public void FightersControllHandler(int target, bool controllable)
    {
        switch (target)
        {
            case 0:
                for (int no = 0; no < GameInfo.max_player_count; no++) FighterControllHandler(no, controllable);
                break;
            case 1:
                for (int no = GameInfo.max_player_count; no < fighterInfos.Length; no++) FighterControllHandler(no, controllable);
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
                for (int no = 0; no < GameInfo.max_player_count; no++) FighterAttackHandler(no, controllable);
                break;
            case 1:
                for (int no = GameInfo.max_player_count; no < fighterInfos.Length; no++) FighterAttackHandler(no, controllable);
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
                for (int no = 0; no < GameInfo.max_player_count; no++) FighterAcceptDamageHandler(no, accept);
                break;
            case 1:
                for (int no = GameInfo.max_player_count; no < fighterInfos.Length; no++) FighterAcceptDamageHandler(no, accept);
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
        receiver.acceptDamage = accept;
    }
}



public struct FighterInfo
{
    public FighterInfo(GameObject fighter, GameObject body, BodyManager bodyManager, FighterCondition fighterCondition, Movement movement, Attack attack, Receiver receiver)
    {
        this.fighter = fighter;
        this.body = body;
        this.bodyManager = bodyManager;
        this.fighterCondition = fighterCondition;
        this.movement = movement;
        this.attack = attack;
        this.receiver = receiver;
    }

    public GameObject fighter { get; private set; }
    public GameObject body { get; private set; }
    public BodyManager bodyManager { get; private set; }
    public FighterCondition fighterCondition { get; private set; }
    public Movement movement { get; private set; }
    public Attack attack { get; private set; }
    public Receiver receiver { get; private set; }
}
