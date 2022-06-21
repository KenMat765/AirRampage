using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager2 : Singleton<SceneManager2>
{
    protected override bool dont_destroy_on_load { get; set; } = true;
    protected override void Awake()
    {
        base.Awake();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene loadedScene, LoadSceneMode sceneMode)
    {
        // If entered sortie lobby scene, frame of info canvas might be open.
        if (loadedScene.name == "SortieLobby")
        {
            if (InfoCanvas.I.isFrameOpen)
            {
                InfoCanvas.I.CloseFrame();
                InfoCanvas.I.CloseButtonInteract(false);
            }
        }

        if (loadedScene.name != "Offline")
        {
            // In battle scenes, camera setup is called in ParticipantManager.
            CameraManager.SetupCameraInScene();

            // In battle scenes, fade in to the scene in BattleSceneConstructor.
            FadeCanvas.I.StopBlink();
            float fadein_duration = FadeCanvas.I.FadeIn(FadeType.left);
        }
    }

    public void LoadScene2(GameScenes gameScene)
    {
        switch (gameScene)
        {
            case GameScenes.menu:
                SceneManager.LoadScene("Menu");
                CSManager.swipe_condition = (TouchExtension touch) => { return true; };
                break;

            case GameScenes.skillport:
                SceneManager.LoadScene("SkillPort");
                break;

            case GameScenes.offline:
                SceneManager.LoadScene("Offline");
                break;

            case GameScenes.onlinelobby:
                SceneManager.LoadScene("OnlineLobby");
                break;

            case GameScenes.sortielobby:
                SceneManager.LoadScene("SortieLobby");
                break;
        }
    }

    public void LoadSceneAsync2(GameScenes gameScene, FadeType fadeOutType)
    {
        if (beforeSceneUnload != null) { beforeSceneUnload(); }
        StartCoroutine(SceneLoader(gameScene, fadeOutType));
    }
    IEnumerator SceneLoader(GameScenes gameScene, FadeType fadeOutType)
    {
        float fadeout_duration = FadeCanvas.I.FadeOut(fadeOutType);

        yield return new WaitForSeconds(fadeout_duration);

        AsyncOperation async;
        switch (gameScene)
        {
            case GameScenes.menu:
                async = SceneManager.LoadSceneAsync("Menu");
                CSManager.swipe_condition = (TouchExtension touch) => { return true; };
                break;

            case GameScenes.skillport:
                async = SceneManager.LoadSceneAsync("SkillPort");
                break;

            case GameScenes.offline:
                async = SceneManager.LoadSceneAsync("Offline");
                break;

            case GameScenes.onlinelobby:
                async = SceneManager.LoadSceneAsync("OnlineLobby");
                break;

            case GameScenes.sortielobby:
                async = SceneManager.LoadSceneAsync("SortieLobby");
                break;

            default:
                yield break;
        }
        async.allowSceneActivation = false;

        yield return new WaitUntil(() => async.progress >= 0.9f);

        FadeCanvas.I.StartBlink();

        yield return new WaitForSeconds(1.5f);    // 3.5秒間だけ"Now Loading ..."をわざと表示

        async.allowSceneActivation = true;
    }

    static Action beforeSceneUnload;
    public static void BeforeSceneUnload(params Action[] actions)
    {
        beforeSceneUnload = default;
        foreach (Action action in actions) { beforeSceneUnload += action; }
    }
}

public enum GameScenes
{
    menu,
    skillport,
    offline,
    onlinelobby,
    sortielobby,
}