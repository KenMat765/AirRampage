using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Has to be called before Participant Manager.
// Because, Participant Manager refers to spawnPoints.
[DefaultExecutionOrder(-1)]

// Handles objects which are dependent on game rule.
public class RuleSwich : MonoBehaviour
{
    [SerializeField] GameObject[] royalOnlyObjects;
    [SerializeField] GameObject[] terminalOnlyObjects;

    public static SpawnPointManager spawnPoints;

    void Awake()
    {
        switch (BattleInfo.rule)
        {
            case Rule.BATTLEROYAL:
                gameObject.AddComponent<BattleRoyal>();
                foreach (GameObject obj in royalOnlyObjects)
                {
                    obj.SetActive(true);
                    if (spawnPoints == null) obj.TryGetComponent<SpawnPointManager>(out spawnPoints);
                }
                foreach (GameObject obj in terminalOnlyObjects) Destroy(obj);
                break;

            case Rule.TERMINAL:
                gameObject.AddComponent<TerminalConquest>();
                foreach (GameObject obj in royalOnlyObjects) Destroy(obj);
                foreach (GameObject obj in terminalOnlyObjects)
                {
                    obj.SetActive(true);
                    if (spawnPoints == null) obj.TryGetComponent<SpawnPointManager>(out spawnPoints);
                }
                break;
        }
    }
}
