using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ScoreManager : NetworkSingleton<ScoreManager>
{
    protected override bool dont_destroy_on_load { get; set; } = false;

    protected override void Awake()
    {
        base.Awake();

        individualScores = new NetworkList<int>();
    }


    public void Setup()
    {
        // Only the host can modify NetworkList. [Player count (= 8) + Zako (red & blue)]
        if (NetworkManager.Singleton.IsHost)
        {
            for (int k = 0; k < GameInfo.MAX_PLAYER_COUNT + 2; k++)
            {
                individualScores.Add(0);
            }
        }
    }


    NetworkVariable<float> redScore = new NetworkVariable<float>();
    NetworkVariable<float> blueScore = new NetworkVariable<float>();

    public float GetScore(Team team)
    {
        switch (team)
        {
            case Team.RED: return redScore.Value;
            case Team.BLUE: return blueScore.Value;
            default:
                Debug.LogError("Can not get score of team other than Red or Blue!");
                return 0;
        }
    }

    public void SetScore(float score, Team team)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        switch (team)
        {
            case Team.RED:
                redScore.Value = score;
                break;

            case Team.BLUE:
                blueScore.Value = score;
                break;

            default:
                Debug.LogError("Can not set score to team other than Red or Blue!");
                break;
        }
    }

    public void AddScore(float delta_score, Team team)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        switch (team)
        {
            case Team.RED:
                redScore.Value += delta_score;
                break;

            case Team.BLUE:
                blueScore.Value += delta_score;
                break;

            default:
                Debug.LogError("Can not add score to team other than Red or Blue!");
                break;
        }
    }

    public void SetScoreOpponent(float score, Team ally_team)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        switch (ally_team)
        {
            case Team.RED:
                blueScore.Value = score;
                break;

            case Team.BLUE:
                redScore.Value = score;
                break;

            default:
                Debug.LogError("Can not set score to team other than Red or Blue!");
                break;
        }
    }

    public void AddScoreOpponent(float delta_score, Team ally_team)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        switch (ally_team)
        {
            case Team.RED:
                blueScore.Value += delta_score;
                break;

            case Team.BLUE:
                redScore.Value += delta_score;
                break;

            default:
                Debug.LogError("Can not add score to team other than Red or Blue!");
                break;
        }
    }


    // Scores of each fighers & zakos of each team.
    NetworkList<int> individualScores;

    public int GetIndividualScore(int idx) => individualScores[idx];

    public void SetIndividualScore(int idx, int value)
    {
        if (!NetworkManager.Singleton.IsHost) return;
        individualScores[idx] = value;
    }

    public void AddIndividualScore(int idx, int delta)
    {
        if (!NetworkManager.Singleton.IsHost) return;
        individualScores[idx] += delta;
    }
}
