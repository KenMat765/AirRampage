using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    static int topScore;
    public static int nowScore {get; set;}
    Text topScoreTex;
    GameObject newRecord;
    public static string winner {get; set;}
    public static Text winnerTex {get; set;}
    public static Color winnerColor {get; set;}
    Text winnerSubTex;

    void Start()
    {
        topScoreTex = transform.Find("TopScore").GetComponent<Text>();
        transform.Find("Score").GetComponent<Text>().text = nowScore.ToString();
        newRecord = transform.Find("NewRecord").gameObject;
        winnerTex = transform.Find("WinnerTex").GetComponent<Text>();
        winnerTex.color = winnerColor;
        winnerSubTex = transform.Find("WinnerSubTex").GetComponent<Text>();
        if(nowScore > topScore)
        {
            topScore = nowScore;
            topScoreTex.text = nowScore.ToString();
            newRecord.SetActive(true);
            PlayerPrefs.SetInt("TopScore", topScore);
        }
        else
        {
            topScoreTex.text = topScore.ToString();
            newRecord.SetActive(false);
        }
        if(winner != null)
        {
            winnerSubTex.text = "Winner";
            winnerTex.text = winner;
        }
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod]
    static void GetTopScore()
    {
        topScore = PlayerPrefs.GetInt("TopScore", 0);
    }

    public void GameStarter()
    {
        // SceneManager2.LoadScene2(GameScenes.offline);
    }
}
