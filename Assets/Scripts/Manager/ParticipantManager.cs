using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


// バトルシーン開始時に全参加者を取得 ＋ Id振り分け(参加者は 4機 × 2チーム = 8機で固定)
public class ParticipantManager : NetworkBehaviour
{
    static ParticipantManager instance;
    public static ParticipantManager I
    {
        get
        {
            if(instance == null)
            {
                instance = FindObjectOfType<ParticipantManager>();
                if(instance == null) {instance = new GameObject(typeof(ParticipantManager).ToString()).AddComponent<ParticipantManager>();}
            }
            return instance;
        }
    }



    // 外部から参照可能なプロバティ
    // All fighters' information in scene (Player + AI + Zako)
    public FighterInfo[] fighterInfos {get; private set;}
    NetworkVariable<bool> allSpawnComplete = new NetworkVariable<bool>(false);
    public int myFighterNo {get; private set;}
    public bool infoSetComplete {get; private set;} = false;
    [SerializeField] GameObject redPlayerPrefab, bluePlayerPrefab, redAiPrefab, blueAiPrefab;



    void Awake()
    {
        if(this != I)
        {
            Destroy(this.gameObject);
            return;
        }
        StartCoroutine(OnAwakeProcess());
    }



    IEnumerator OnAwakeProcess()
    {
        // Define necessary variables.
        // allFighters include Zakos.
        fighterInfos = new FighterInfo[GameInfo.max_player_count + (SpawnPoints.zakoCountAll)];    // ← refer based on fighterNo
        GameObject[] allFighters = new GameObject[GameInfo.max_player_count + (SpawnPoints.zakoCountAll)];
        GameObject myPlayer = null;


        // Multi Players ///////////////////////////////////////////////////////////////////////////////
        if(BattleInfo.isMulti)
        {
            if (IsHost)
            {
                // Generate Players and AIs.
                SpawnAllFighters();
                // Create Zakos
                SpawnAllZakos();
            }

            // Wait until spawning is finished.
            yield return new WaitUntil(() => allSpawnComplete.Value);

            // Get your own fighter.
            myPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

            // Get generated fighters on each client.
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach(GameObject player in players)
            {
                int fighterNo = player.GetComponent<FighterCondition>().fighterNo.Value;
                allFighters[fighterNo] = player;
                if(player == myPlayer)
                {
                    myFighterNo = fighterNo;
                }
            }
            GameObject[] ais = GameObject.FindGameObjectsWithTag("AI");
            foreach(GameObject ai in ais)
            {
                int fighterNo = ai.GetComponent<FighterCondition>().fighterNo.Value;
                allFighters[fighterNo] = ai;
            }
            GameObject[] zakos = GameObject.FindGameObjectsWithTag("Zako");
            foreach(GameObject zako in zakos)
            {
                int fighterNo = zako.GetComponent<FighterCondition>().fighterNo.Value;
                allFighters[fighterNo] = zako;
            }
        }


        // Solo Players ////////////////////////////////////////////////////////////////////////////////
        else
        {
            // Generate your fighter
            myPlayer = Instantiate(redPlayerPrefab);
            allFighters[0] = myPlayer;
            myPlayer.GetComponent<FighterCondition>().fighterNo.Value = 0;
            myFighterNo = 0;

            // Generate AIs
            for(int no = 1; no < GameInfo.max_player_count; no ++)
            {
                GameObject aiFighter;
                if(no < GameInfo.max_player_count/2 - 1)
                {
                    aiFighter = Instantiate(redAiPrefab);
                }
                else
                {
                    aiFighter = Instantiate(blueAiPrefab);
                }
                allFighters[no] = aiFighter;
                aiFighter.GetComponent<FighterCondition>().fighterNo.Value = no;
            }

            // Generate Zakos
            for(int no = 0; no < SpawnPoints.zakoCountPerTeam; no ++)
            {
                GameObject redZako;
                redZako = Instantiate(redZakoPrefab);
                allFighters[GameInfo.max_player_count + no] = redZako;
                redZako.GetComponent<FighterCondition>().fighterNo.Value = GameInfo.max_player_count + no;

                GameObject blueZako;
                blueZako = Instantiate(blueZakoPrefab);
                allFighters[GameInfo.max_player_count + no + SpawnPoints.zakoCountPerTeam] = blueZako;
                blueZako.GetComponent<FighterCondition>().fighterNo.Value = GameInfo.max_player_count + no + SpawnPoints.zakoCountPerTeam;
            }
        }


        // Setup Player Camera ///////////////////////////////////////////////////////////////////////////
        GameObject cameraRoot = myPlayer.transform.Find("CameraRoot").gameObject;
        Camera playerCam = cameraRoot.GetComponentInChildren<Camera>();
        cameraRoot.SetActive(true);
        uGUIMannager.I.SetPlayerCamera(playerCam);
        CameraManager.SetupCameraInScene();


        // Process for all fighters /////////////////////////////////////////////////////////////////////////
        for(int no = 0; no < GameInfo.max_player_count + (SpawnPoints.zakoCountAll); no++)
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
            if(no < GameInfo.max_player_count)
            {
                // Get battle data at fighterNo.
                BattleInfo.ParticipantBattleData battleData = BattleInfo.battleDatas[no];

                // Name fighter
                fighter.name = battleData.name;

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
            // if(no == 3)
            // {
            //     fighter.transform.Find("Kari Camera").gameObject.SetActive(true);
            // }
        }


        // All set complete !!
        infoSetComplete = true;
    }



    // Only the host calls this method.
    void SpawnAllFighters()
    {
        for(int no = 0; no < GameInfo.max_player_count; no ++)
        {
            BattleInfo.ParticipantBattleData? battleData_nullable = BattleInfo.GetBattleDataByFighterNo(no);
            if(!battleData_nullable.HasValue)
            {
                Debug.LogError($"BattleData not set at fighterNo : {no}");
                continue;
            }
            BattleInfo.ParticipantBattleData battleData = (BattleInfo.ParticipantBattleData)battleData_nullable;


            // If Player ////////////////////////////////////////////////////////////////////////
            if(battleData.isPlayer)
            {
                // Get client id.
                ulong? clientId_nullable = battleData.clientId;
                if(!clientId_nullable.HasValue)
                {
                    Debug.LogError($"ClientId not set at fighterNo : {no}");
                    continue;
                }
                ulong clientId = (ulong)clientId_nullable;

                // Create fighter.
                GameObject fighter;
                SpawnPoint point = SpawnPoints.GetSpawnPoint(no);
                if(battleData.team == Team.Red)
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
                SpawnPoint point = SpawnPoints.GetSpawnPoint(no);
                if(battleData.team == Team.Red)
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


    // zakoCount : zako count per team.
    [SerializeField] GameObject redZakoPrefab, blueZakoPrefab;
    // Only the host calls this method.
    void SpawnAllZakos()
    {
        int red_pointNo = 0, blue_pointNo = 0;
        SpawnPoint redPoint = SpawnPoints.GetSpawnPointZako(Team.Red, red_pointNo);
        SpawnPoint bluePoint = SpawnPoints.GetSpawnPointZako(Team.Blue, blue_pointNo);;
        int red_current_spawned = 0, blue_current_spawned = 0;

        for(int k = 0; k < SpawnPoints.zakoCountPerTeam; k ++)
        {
            if(red_current_spawned == redPoint.zakoCount)
            {
                red_pointNo ++;
                redPoint = SpawnPoints.GetSpawnPointZako(Team.Red, red_pointNo);
                red_current_spawned = 0;
            }

            red_current_spawned ++;

            // Create fighter.
            GameObject red;
            red = Instantiate(redZakoPrefab, redPoint.transform.position, redPoint.transform.rotation);

            // Set fighterNo & fighterName at fighter condition.
            FighterCondition redCondition = red.GetComponent<FighterCondition>();
            redCondition.fighterNo.Value = GameInfo.max_player_count + k;
            redCondition.fighterName.Value = "ZakoRed" + (k + 1);
            redCondition.fighterTeam.Value = Team.Red;

            // Spawn fighter.
            NetworkObject redNet = red.GetComponent<NetworkObject>();
            redNet.Spawn();


            if(blue_current_spawned == bluePoint.zakoCount)
            {
                blue_pointNo ++;
                bluePoint = SpawnPoints.GetSpawnPointZako(Team.Blue, blue_pointNo);
                blue_current_spawned = 0;
            }

            blue_current_spawned ++;

            // Create fighter.
            GameObject blue;
            blue = Instantiate(blueZakoPrefab, bluePoint.transform.position, bluePoint.transform.rotation);

            // Set fighterNo & fighterName & team at fighter condition.
            FighterCondition blueCondition = blue.GetComponent<FighterCondition>();
            blueCondition.fighterNo.Value = GameInfo.max_player_count + k + SpawnPoints.zakoCountPerTeam;
            blueCondition.fighterName.Value = "ZakoBlue" + (k + 1);
            blueCondition.fighterTeam.Value = Team.Blue;


            // Spawn fighter.
            NetworkObject blueNet = blue.GetComponent<NetworkObject>();
            blueNet.Spawn();
        }
        // Zakos are spawned after players and AIs.
        // Therefore, set allSpawnComplete true, after you spawned Zakos.
        allSpawnComplete.Value = true;
    }


    // Activate (or unactivate) all fighters.
    public void AllFightersActivationHandler(bool activate)
    {
        foreach(FighterInfo info in fighterInfos)
        {
            info.fighterCondition.enabled = activate;
            info.movement.enabled = activate;
            info.attack.enabled = activate;
            info.receiver.enabled = activate;
        }
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

    public GameObject fighter {get; private set;}
    public GameObject body {get; private set;}
    public BodyManager bodyManager {get; private set;}
    public FighterCondition fighterCondition {get; private set;}
    public Movement movement {get; private set;}
    public Attack attack {get; private set;}
    public Receiver receiver {get; private set;}
}
