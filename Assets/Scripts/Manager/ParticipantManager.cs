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


    public void FighterSetup(SpawnPointManager spawnPointManager) => StartCoroutine(fighterSetup(spawnPointManager));

    IEnumerator fighterSetup(SpawnPointManager spawnPointManager)
    {
        zakoCountAll = spawnPointManager.zakoCountAll;

        fighterInfos = new FighterInfo[GameInfo.max_player_count + (zakoCountAll)];
        GameObject[] allFighters = new GameObject[GameInfo.max_player_count + (zakoCountAll)];
        GameObject myPlayer = null;


        // Multi Players ///////////////////////////////////////////////////////////////////////////////
        if (BattleInfo.isMulti)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                // Generate Players and AIs.
                SpawnAllFighters(spawnPointManager);
                // Generate Zakos
                SpawnAllZakos(spawnPointManager);
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
        }


        // Solo Players ////////////////////////////////////////////////////////////////////////////////
        else
        {
            // Generate your fighter
            SpawnPointFighter myPoint = spawnPointManager.GetSpawnPointFighter(0);
            myPlayer = Instantiate(redPlayerPrefab, myPoint.transform.position, myPoint.transform.rotation);
            allFighters[0] = myPlayer;
            myPlayer.GetComponent<FighterCondition>().fighterNo.Value = 0;
            myFighterNo = 0;

            // Generate AIs
            for (int no = 1; no < GameInfo.max_player_count; no++)
            {
                // GetTeamFromNo(int) only returns Team.RED or Team.BLUE.
                Team team = GameInfo.GetTeamFromNo(no);
                SpawnPointFighter point = spawnPointManager.GetSpawnPointFighter(no);
                GameObject aiFighter;
                if (team == Team.RED)
                {
                    aiFighter = Instantiate(redAiPrefab, point.transform.position, point.transform.rotation);
                }
                else
                {
                    aiFighter = Instantiate(blueAiPrefab, point.transform.position, point.transform.rotation);
                }
                allFighters[no] = aiFighter;
                aiFighter.GetComponent<FighterCondition>().fighterNo.Value = no;
            }

            // Generate Zakos
            int point_no = 0;
            SpawnPointZako zako_point = spawnPointManager.GetSpawnPointZako(point_no);
            int current_spawned = 0;

            // First zako No is equal to max player count.
            zako_point.from_inclusive.Value = GameInfo.max_player_count;

            for (int no = 0; no < zakoCountAll; no++)
            {
                int zakoNo = GameInfo.max_player_count + no;

                // If spawned requested zakos, go to next spawn point.
                if (current_spawned == zako_point.zakoCount)
                {
                    point_no++;
                    zako_point = spawnPointManager.GetSpawnPointZako(point_no);
                    current_spawned = 0;
                    zako_point.from_inclusive.Value = zakoNo;
                }

                current_spawned++;

                GameObject zako;
                zako = Instantiate(zakoPrefab, zako_point.transform.position, zako_point.transform.rotation);

                allFighters[zakoNo] = zako;

                // Set fighterNo & fighterName at fighter condition.
                ZakoCondition zako_condition = zako.GetComponent<ZakoCondition>();
                zako_condition.fighterNo.Value = zakoNo;
                zako_condition.fighterName.Value = "Zako";
                // Team of zako is determined at each spawn points.
                zako_condition.fighterTeam.Value = Team.NONE;
                zako_condition.spawnPoint = zako_point;

                // Add zako to Zako Spawn Points standbys.
                zako_point.standbys.Add(zakoNo);
            }
        }


        // Setup Player Camera ///////////////////////////////////////////////////////////////////////////
        GameObject cameraRoot = myPlayer.transform.Find("CameraRoot").gameObject;
        Camera playerCam = cameraRoot.GetComponentInChildren<Camera>();
        cameraRoot.SetActive(true);
        uGUIMannager.I.SetPlayerCamera(playerCam);
        CameraManager.SetupCameraInScene();
        GameObject cameraRadar = myPlayer.transform.Find("CameraRadar").gameObject;
        cameraRadar.SetActive(true);


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

                if (!BattleInfo.isMulti)
                {
                    // Setup fighterCondition by battle data.
                    fighterCondition.fighterNo.Value = battleData.fighterNo;
                    fighterCondition.fighterName.Value = battleData.name;
                    fighterCondition.fighterTeam.Value = battleData.team;
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



            // 
            // 
            // 
            // For AI Debug.
            // if (no == 3)
            // {
            //     fighter.transform.Find("Kari Camera").gameObject.SetActive(true);
            // }
        }


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

                // Set fighterNo & fighterName at fighter condition.
                FighterCondition fighterCondition = fighter.GetComponent<FighterCondition>();
                fighterCondition.fighterNo.Value = battleData.fighterNo;
                fighterCondition.fighterName.Value = battleData.name;
                fighterCondition.fighterTeam.Value = battleData.team;

                // Spawn fighter.
                NetworkObject networkObject = fighter.GetComponent<NetworkObject>();
                networkObject.SpawnAsPlayerObject(clientId);
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

                // Set fighterNo at fighter & team condition.
                FighterCondition fighterCondition = fighter.GetComponent<FighterCondition>();
                fighterCondition.fighterNo.Value = battleData.fighterNo;
                fighterCondition.fighterName.Value = battleData.name;
                fighterCondition.fighterTeam.Value = battleData.team;

                // Spawn fighter.
                NetworkObject networkObject = fighter.GetComponent<NetworkObject>();
                networkObject.Spawn();
            }
        }
    }


    // Only the host calls this method.
    void SpawnAllZakos(SpawnPointManager spawnPointManager)
    {
        int point_no = 0;
        SpawnPointZako zako_point = spawnPointManager.GetSpawnPointZako(point_no);
        int current_spawned = 0;

        // First zako No is equal to max player count.
        zako_point.from_inclusive.Value = GameInfo.max_player_count;

        for (int k = 0; k < spawnPointManager.zakoCountAll; k++)
        {
            int zakoNo = GameInfo.max_player_count + k;

            // If spawned requested zakos, go to next spawn point.
            if (current_spawned == zako_point.zakoCount)
            {
                point_no++;
                zako_point = spawnPointManager.GetSpawnPointZako(point_no);
                current_spawned = 0;
                zako_point.from_inclusive.Value = zakoNo;
            }

            current_spawned++;

            // Create fighter.
            GameObject zako;
            zako = Instantiate(zakoPrefab, zako_point.transform.position, zako_point.transform.rotation);

            // Set fighterNo & fighterName at fighter condition.
            ZakoCondition zako_condition = zako.GetComponent<ZakoCondition>();
            zako_condition.fighterNo.Value = zakoNo;
            zako_condition.fighterName.Value = "Zako";
            // Team of zako is determined at each spawn points.
            zako_condition.fighterTeam.Value = Team.NONE;
            zako_condition.spawnPoint = zako_point;

            // Spawn fighter.
            NetworkObject zako_net = zako.GetComponent<NetworkObject>();
            zako_net.Spawn();

            // Add zako to Zako Spawn Points standbys.
            zako_point.standbys.Add(zakoNo);
        }

        // Zakos are spawned after players and AIs.
        // Therefore, set allSpawnComplete true, after you spawned Zakos.
        allSpawnComplete.Value = true;
    }


    // Activate (or unactivate) all fighters.
    public void AllFightersActivationHandler(bool activate)
    {
        foreach (FighterInfo info in fighterInfos)
        {
            info.fighterCondition.enabled = activate;
            info.movement.enabled = activate;
            info.attack.enabled = activate;
            info.receiver.enabled = activate;
        }
    }

    // Activate (or unactivate) fighters except zakos.
    public void FightersActivationHandler(bool activate)
    {
        for (int id = 0; id < GameInfo.max_player_count; id++)
        {
            FighterActivationHandler(id, activate);
        }
    }

    // Activate (or unactivate) zakos.
    public void ZakosActivationHandler(bool activate)
    {
        for (int id = GameInfo.max_player_count; id < GameInfo.max_player_count + zakoCountAll; id++)
        {
            FighterActivationHandler(id, activate);
        }
    }

    // Activate (or unactivate) fighter by id.
    public void FighterActivationHandler(int id, bool activate)
    {
        FighterInfo info = fighterInfos[id];
        info.fighterCondition.enabled = activate;
        info.movement.enabled = activate;
        info.attack.enabled = activate;
        info.receiver.enabled = activate;
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
