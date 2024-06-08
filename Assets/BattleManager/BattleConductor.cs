using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// Should be called at the very first in scene.
[DefaultExecutionOrder(-1)]
public class BattleConductor : NetworkSingleton<BattleConductor>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    public static bool gameInProgress { get; private set; }
    public NetworkVariable<float> timer { get; private set; } = new NetworkVariable<float>(BattleInfo.time_sec);


    // Check whether everyone is ready or not (Host Only.)
    int ready_player = 0;

    // Used to know that everyone is ready (All Clients.)
    bool everyone_ready = false;

    // Clients tells the host that he/she is ready by this RPC.
    [ServerRpc(RequireOwnership = false)]
    void ImReadyServerRpc()
    {
        ready_player++;
        if (ready_player == BattleInfo.playerCount)
        {
            EveryOneIsReadyClientRpc();
        }
    }

    // Host tells all clients that everyone is ready.
    [ClientRpc] void EveryOneIsReadyClientRpc() => everyone_ready = true;


    // Score.
    NetworkVariable<float> redScore = new NetworkVariable<float>();
    public float RedScore
    {
        get { return redScore.Value; }
        set { if (gameInProgress) redScore.Value = value; }
    }

    NetworkVariable<float> blueScore = new NetworkVariable<float>();
    public float BlueScore
    {
        get { return blueScore.Value; }
        set { if (gameInProgress) blueScore.Value = value; }
    }

    public float GetAllyTeamScore(Team my_team)
    {
        switch (my_team)
        {
            case Team.RED: return redScore.Value;
            case Team.BLUE: return blueScore.Value;
            default: return 0;
        }
    }

    public float GetOpponentTeamScore(Team my_team)
    {
        switch (my_team)
        {
            case Team.RED: return blueScore.Value;
            case Team.BLUE: return redScore.Value;
            default: return 0;
        }
    }

    public void GiveScoreToOpponentTeam(Team my_team, int score)
    {
        switch (my_team)
        {
            case Team.RED: BlueScore += score; break;
            case Team.BLUE: RedScore += score; break;
        }
    }

    public static int[] individualScores = new int[GameInfo.max_player_count + 2];


    // 
    // 
    // Leave scores constant between fighters for now.
    public const int score_fighter = 200;
    public const int score_zako = 50;


    [SerializeField] GameObject[] royalOnlyObjects;
    [SerializeField] GameObject[] terminalOnlyObjects;
    [SerializeField] GameObject[] crystalOnlyObjects;


    // As SpawnPointManager can not be Singleton, keep this in conductor for easier access.
    public static SpawnPointManager spawnPointManager;


    protected override void Awake()
    {
        base.Awake();
        StartCoroutine(GameSetup());
    }


    IEnumerator GameSetup()
    {
        // Push UI to sides before fading in to the scene.
        uGUIMannager.I.EnterUI(false, true);

        // Destroy unnecessary GameObjects & Select SpawnPointManager which corresponds to the game rule.
        switch (BattleInfo.rule)
        {
            case Rule.BATTLEROYAL:
                foreach (GameObject obj in royalOnlyObjects) if (obj.TryGetComponent<SpawnPointManager>(out spawnPointManager)) break;
                foreach (GameObject obj in terminalOnlyObjects) Destroy(obj);
                foreach (GameObject obj in crystalOnlyObjects) Destroy(obj);
                break;

            case Rule.TERMINALCONQUEST:
                foreach (GameObject obj in royalOnlyObjects) Destroy(obj);
                foreach (GameObject obj in terminalOnlyObjects) if (obj.TryGetComponent<SpawnPointManager>(out spawnPointManager)) break;
                foreach (GameObject obj in crystalOnlyObjects) Destroy(obj);
                break;

            case Rule.CRYSTALHUNTER:
                foreach (GameObject obj in royalOnlyObjects) Destroy(obj);
                foreach (GameObject obj in terminalOnlyObjects) Destroy(obj);
                foreach (GameObject obj in crystalOnlyObjects) if (obj.TryGetComponent<SpawnPointManager>(out spawnPointManager)) break;
                break;
        }

        // Start ParticipantManager setup fighter information.
        ParticipantManager.I.FightersSetup(spawnPointManager);

        // Wait until ParticipantManager sets fighter information.
        yield return new WaitUntil(() => ParticipantManager.I.infoSetComplete);

        // Wait until everyone is at same point.
        ImReadyServerRpc();
        yield return new WaitUntil(() => everyone_ready);

        // After everyone is ready, setup spawnpoints.
        spawnPointManager.SetupSpawnPoints();

        // After setup spawnpoints, setup terminals.
        if (BattleInfo.rule == Rule.TERMINALCONQUEST) TerminalManager.I.SetupTerminals();
        else if (BattleInfo.rule == Rule.CRYSTALHUNTER) CrystalManager.I.SetupCrystals();

        // Set all zakos deactivated.
        ParticipantManager.I.FightersActivationHandler(1, false);

        // Activate player fighters.
        ParticipantManager.I.FightersActivationHandler(0, true);

        // Start uGUIManager setup UI, and enable it.
        uGUIMannager.I.UISetup();
        uGUIMannager.I.enabled = true;

        // Wait for few seconds until UI setup is done
        yield return new WaitForSeconds(0.5f);

        // Fade in to battle scene.
        FadeCanvas.I.StopBlink();
        float fadein_duration = FadeCanvas.I.FadeIn(FadeType.left);

        // Wait until fade in is complete.
        yield return new WaitForSeconds(fadein_duration + 0.1f);

        // Start Call.
        float call_duration = uGUIMannager.I.CallStart();

        // Wait for few seconds before starting game.
        yield return new WaitForSeconds(call_duration + 0.2f);

        // Start game at any given time.
        ParticipantManager.I.FightersControllHandler(0, true);
        ParticipantManager.I.FightersAttackHandler(0, true);
        ParticipantManager.I.FightersAcceptDamageHandler(-1, true);
        if (BattleInfo.rule == Rule.TERMINALCONQUEST) TerminalManager.I.TerminalsAcceptDamageHandler(true);
        else if (BattleInfo.rule == Rule.CRYSTALHUNTER) CrystalManager.I.AcceptCrystalHandler(true);
        uGUIMannager.I.EnterUI(true, false);
        gameInProgress = true;
    }


    void FixedUpdate()
    {
        if (!IsHost) return;

        if (gameInProgress)
        {
            timer.Value -= Time.deltaTime;
            if (timer.Value <= 0)
            {
                FinishGameClientRpc();
            }
        }
    }


    public void FinishGame(Team occupation_team = Team.NONE) => StartCoroutine(finishGame(occupation_team));
    [ClientRpc] public void FinishGameClientRpc(Team occupation_team = Team.NONE) => FinishGame(occupation_team);
    IEnumerator finishGame(Team occupation_team = Team.NONE)
    {
        gameInProgress = false;

        // Stop fighters & terminals accepting damages.
        ParticipantManager.I.FightersControllHandler(-1, false);
        ParticipantManager.I.FightersAttackHandler(-1, false);
        ParticipantManager.I.FightersAcceptDamageHandler(-1, false);
        if (BattleInfo.rule == Rule.TERMINALCONQUEST) TerminalManager.I.TerminalsAcceptDamageHandler(false);
        else if (BattleInfo.rule == Rule.CRYSTALHUNTER) CrystalManager.I.AcceptCrystalHandler(false);

        // Push UI to sides.
        uGUIMannager.I.EnterUI(false, false);

        // Disable uGUIManager.
        uGUIMannager.I.enabled = false;

        // Invoke finish call.
        float call_duration = uGUIMannager.I.CallFinish();

        yield return new WaitForSeconds(call_duration);

        // Show result.
        if (occupation_team == Team.NONE) uGUIMannager.I.ShowResult();
        else uGUIMannager.I.ShowResultOccupation(occupation_team);
    }


    [ClientRpc]
    public void ReturnToMenuClientRpc()
    {
        IEnumerator returnToMenu()
        {
            float fadeout_duration = FadeCanvas.I.FadeOut(FadeType.gradually);
            yield return new WaitForSeconds(fadeout_duration);
            float blink_duration = FadeCanvas.I.StartBlink();
            yield return new WaitForSeconds(blink_duration);
            NetworkManager.SceneManager.LoadScene("Menu", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        StartCoroutine(returnToMenu());
    }


    public void OnFighterDestroyed(FighterCondition fighterCondition, int destroyerNo, string destroyerSkillName)
    {
        Team my_team = fighterCondition.fighterTeam.Value;
        int my_no = fighterCondition.fighterNo.Value;

        switch (BattleInfo.rule)
        {
            case Rule.BATTLEROYAL:
                // Give score to destroyer's team.
                int my_score = (gameObject.tag == "Zako") ? score_zako : score_fighter;
                GiveScoreToOpponentTeam(my_team, my_score);

                // If killer is Fighter.
                if (0 <= destroyerNo && destroyerNo < GameInfo.max_player_count)
                {
                    individualScores[destroyerNo] += my_score;
                }

                // If killer is Zako.
                else
                {
                    FighterCondition zako_condition = ParticipantManager.I.fighterInfos[destroyerNo].fighterCondition;
                    Team destroyer_team = zako_condition.fighterTeam.Value;
                    switch (destroyer_team)
                    {
                        case Team.RED:
                            individualScores[GameInfo.max_player_count] += my_score;
                            break;

                        case Team.BLUE:
                            individualScores[GameInfo.max_player_count + 1] += my_score;
                            break;

                        default:
                            Debug.LogError("Destroyer's team is NONE!!", zako_condition.gameObject);
                            return;
                    }
                }
                break;

            case Rule.TERMINALCONQUEST:
                float protection_decrease = 0.25f;
                List<Terminal> owner_terminals;
                if (TerminalManager.I.TryGetOwnerTerminals(my_no, out owner_terminals))
                {
                    foreach (Terminal terminal in owner_terminals)
                    {
                        terminal.SkillProtection -= protection_decrease;
                    }
                }
                break;

            case Rule.CRYSTALHUNTER:
                for (int crystal_id = 0; crystal_id < CrystalManager.crystal_count; crystal_id++)
                {
                    int carrier_no = CrystalManager.I.carrierNos[crystal_id];
                    if (carrier_no == my_no)
                    {
                        Crystal crystal = CrystalManager.I.crystals[crystal_id];
                        crystal.ReleaseCrystal();
                    }
                }
                break;
        }
    }
}
