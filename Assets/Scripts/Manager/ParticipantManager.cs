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
        fighterInfos = new FighterInfo[GameInfo.max_player_count + (zakoCount * 2)];    // ← refer based on fighterNo
        GameObject[] allFighters = new GameObject[GameInfo.max_player_count + (zakoCount * 2)];
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
            for(int no = 0; no < zakoCount; no ++)
            {
                GameObject redZako;
                redZako = Instantiate(redZakoPrefab);
                allFighters[GameInfo.max_player_count + no] = redZako;
                redZako.GetComponent<FighterCondition>().fighterNo.Value = GameInfo.max_player_count + no;

                GameObject blueZako;
                blueZako = Instantiate(blueZakoPrefab);
                allFighters[GameInfo.max_player_count + no + zakoCount] = blueZako;
                blueZako.GetComponent<FighterCondition>().fighterNo.Value = GameInfo.max_player_count + no + zakoCount;
            }
        }


        // Setup Player Camera ///////////////////////////////////////////////////////////////////////////
        GameObject cameraRoot = myPlayer.transform.Find("CameraRoot").gameObject;
        Camera playerCam = cameraRoot.GetComponentInChildren<Camera>();
        cameraRoot.SetActive(true);
        uGUIMannager.I.SetPlayerCamera(playerCam);
        CameraManager.SetupCameraInScene();


        // Process for all fighters /////////////////////////////////////////////////////////////////////////
        for(int no = 0; no < GameInfo.max_player_count + (zakoCount * 2); no++)
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

                // Setup fighter condition.
                fighterCondition.team = battleData.team;
                // attack.fighterCondition = fighterCondition;    // SkillのGeneratorで使用するが、AttackのStartが遅いため、ParticipantManagerから各Attackに配布する

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

            // Only for Zakos
            else
            {
                attack.fighterCondition = fighterCondition;
                if(no < GameInfo.max_player_count + zakoCount)
                {
                    fighterCondition.team = Team.Red;
                    fighter.name = "ZakoRed" + no.ToString();
                }
                else
                {
                    fighterCondition.team = Team.Blue;
                    fighter.name = "ZakoBlue" + no.ToString();
                }
            }

            // Set body name to fighterNo for easier reference to fighterNo.
            body.name = no.ToString();



            // 
            // 
            // 
            if(no == 3)
            {
                fighter.transform.Find("Kari Camera").gameObject.SetActive(true);
            }
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
                if(battleData.team == Team.Red)
                {
                    fighter = Instantiate(redPlayerPrefab);
                }
                else
                {
                    fighter = Instantiate(bluePlayerPrefab);
                }

                // Set fighterNo at fighter condition.
                fighter.GetComponent<FighterCondition>().fighterNo.Value = battleData.fighterNo;

                // Spawn fighter.
                NetworkObject networkObject = fighter.GetComponent<NetworkObject>();
                networkObject.SpawnAsPlayerObject(clientId);
            }


            // If AI ////////////////////////////////////////////////////////////////////////////
            else
            {
                // Create fighter.
                GameObject fighter;
                if(battleData.team == Team.Red)
                {
                    fighter = Instantiate(redAiPrefab);
                }
                else
                {
                    fighter = Instantiate(blueAiPrefab);
                }

                // Set fighterNo at fighter condition.
                fighter.GetComponent<FighterCondition>().fighterNo.Value = battleData.fighterNo;

                // Spawn fighter.
                NetworkObject networkObject = fighter.GetComponent<NetworkObject>();
                networkObject.Spawn();
            }
        }
    }


    // zakoCount : zako count per team.
    [Header("Zako"), SerializeField] int zakoCount;
    [SerializeField] GameObject redZakoPrefab, blueZakoPrefab;
    // Only the host calls this method.
    void SpawnAllZakos()
    {
        for(int k = 0; k < zakoCount; k ++)
        {
            // Create fighter.
            GameObject red;
            red = Instantiate(redZakoPrefab);
            // Set fighterNo at fighter condition.
            red.GetComponent<FighterCondition>().fighterNo.Value = GameInfo.max_player_count + k;
            // Spawn fighter.
            NetworkObject redNet = red.GetComponent<NetworkObject>();
            redNet.Spawn();

            // Create fighter.
            GameObject blue;
            blue = Instantiate(blueZakoPrefab);
            // Set fighterNo at fighter condition.
            blue.GetComponent<FighterCondition>().fighterNo.Value = GameInfo.max_player_count + k + zakoCount;
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
