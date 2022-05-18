using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSceneConstructor : MonoBehaviour
{
    void Awake()
    {
        StartCoroutine(OnAwakeProcess());
    }

    IEnumerator OnAwakeProcess()
    {
        // ParticipantManagerがFighterInfosをセットするのを待つ
        yield return new WaitUntil(() => ParticipantManager.I.infoSetComplete);

        // uGUIManagerをアクティブ化
        uGUIMannager.I.enabled = true;

        // 任意のタイミングでゲームを開始する
        yield return new WaitForSeconds(1);

        // ゲーム開始
        ParticipantManager.I.AllFightersActivationHandler(true);
        Destroy(this.gameObject);
    }
}
