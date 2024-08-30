using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Originals.Utilities;

// Should be called at the very first in scene.
[DefaultExecutionOrder(-1)]
public class BattleConductor : NetworkSingleton<BattleConductor>
{
    protected override bool dont_destroy_on_load { get; set; } = false;


    public static RuleManager ruleManager { get; private set; }
    [SerializeField] SerializableDictionary<Rule, GameObject> ruleObjects;


    public static bool gameInProgress { get; private set; }
    public NetworkVariable<float> timer { get; private set; } = new NetworkVariable<float>(BattleInfo.time_sec);


    int ready_player = 0;
    bool everyone_ready = false;

    [ServerRpc(RequireOwnership = false)]
    void ImReadyServerRpc()
    {
        ready_player++;
        if (ready_player == BattleInfo.playerCount)
        {
            EveryOneIsReadyClientRpc();
        }
    }

    [ClientRpc] void EveryOneIsReadyClientRpc() => everyone_ready = true;


    protected override void Awake()
    {
        base.Awake();

        StartCoroutine(GameSetup());
    }


    IEnumerator GameSetup()
    {
        // ===== SpawnPoint Manager ===== //
        SpawnPointManager.I.SetupSpawnPoints();

        // ===== Participant Manager ===== //
        ParticipantManager.I.FightersSetup();
        yield return new WaitUntil(() => ParticipantManager.I.infoSetComplete);

        // Wait until everyone is at same point.
        ImReadyServerRpc();
        yield return new WaitUntil(() => everyone_ready);

        // ===== Rule Manager ===== //
        foreach (var ruleObject in ruleObjects)
        {
            if (ruleObject.Key == BattleInfo.rule)
            {
                ruleManager = ruleObject.Value.GetComponent<RuleManager>();
                continue;
            }
            Destroy(ruleObject.Value);
        }
        ruleManager.Setup();

        // ===== Score Manager ===== //
        ScoreManager.I.Setup();

        // ===== uGUIManager Setup ===== //
        uGUIMannager.I.EnterExitUI(false, true);
        uGUIMannager.I.UISetup();
        uGUIMannager.I.enabled = true;
        yield return new WaitForSeconds(0.5f);

        // Fighters must be activated AFTER uGUIManager is initialized, because fighters reffers to it in Start().
        ParticipantManager.I.FightersActivationHandler(1, false);
        ParticipantManager.I.FightersActivationHandler(0, true);

        FadeCanvas.I.StopBlink();
        float fadein_duration = FadeCanvas.I.FadeIn(FadeType.left);
        yield return new WaitForSeconds(fadein_duration + 0.1f);

        float call_duration = uGUIMannager.I.CallStart();
        yield return new WaitForSeconds(call_duration + 0.2f);

        // ===== Start game ===== //
        ParticipantManager.I.FightersControllHandler(0, true);
        ParticipantManager.I.FightersAttackHandler(0, true);
        ParticipantManager.I.FightersAcceptDamageHandler(-1, true);
        ruleManager.OnGameStart();
        uGUIMannager.I.EnterExitUI(true, false);
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


    public void FinishGame() => StartCoroutine(finishGame());
    [ClientRpc] public void FinishGameClientRpc() => FinishGame();
    IEnumerator finishGame()
    {
        gameInProgress = false;

        ParticipantManager.I.FightersControllHandler(-1, false);
        ParticipantManager.I.FightersAttackHandler(-1, false);
        ParticipantManager.I.FightersAcceptDamageHandler(-1, false);

        ruleManager.OnGameEnd();

        uGUIMannager.I.EnterExitUI(false, false);
        uGUIMannager.I.CleanUpScreen();
        uGUIMannager.I.enabled = false;

        float call_duration = uGUIMannager.I.CallFinish();
        yield return new WaitForSeconds(call_duration);

        uGUIMannager.I.ShowResult();
    }
}
