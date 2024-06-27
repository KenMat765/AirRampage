using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

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

    string[] battleScenes = { "Space", "Canyon" };

    private void OnSceneLoaded(Scene loadedScene, LoadSceneMode sceneMode)
    {
        if (!battleScenes.Contains(loadedScene.name))
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
            case GameScenes.MENU:
                SceneManager.LoadScene("Menu");
                CSManager.swipe_condition = (TouchExtension touch) => { return true; };
                break;

            case GameScenes.SKILLPORT:
                SceneManager.LoadScene("SkillPort");
                break;

            case GameScenes.ONLINELOBBY:
                SceneManager.LoadScene("OnlineLobby");
                break;

            case GameScenes.SORTIELOBBY:
                SceneManager.LoadScene("SortieLobby");
                break;

            case GameScenes.CANYON:
                SceneManager.LoadScene("Canyon");
                break;

            case GameScenes.SPACE:
                SceneManager.LoadScene("Space");
                break;

            case GameScenes.SNOWPEAK:
                SceneManager.LoadScene("SnowPeak");
                break;
        }
    }

    public void LoadSceneAsync2(GameScenes gameScene, FadeType fadeOutType)
    {
        if (beforeSceneUnload != null) { beforeSceneUnload(); }
        StartCoroutine(SceneLoader(gameScene, fadeOutType));
    }
    public void LoadSceneAsync2(Stage stage, FadeType fadeOutType)
    {
        if (beforeSceneUnload != null) { beforeSceneUnload(); }
        GameScenes gameScene;
        switch (stage)
        {
            case Stage.CANYON:
                gameScene = GameScenes.CANYON;
                break;

            case Stage.SNOWPEAK:
                gameScene = GameScenes.SNOWPEAK;
                break;

            case Stage.SPACE:
                gameScene = GameScenes.SPACE;
                break;

            default:
                Debug.LogError("Could not associate stage with game scene!!", gameObject);
                return;
        }
        StartCoroutine(SceneLoader(gameScene, fadeOutType));
    }
    IEnumerator SceneLoader(GameScenes gameScene, FadeType fadeOutType)
    {
        float fadeout_duration = FadeCanvas.I.FadeOut(fadeOutType);

        yield return new WaitForSeconds(fadeout_duration + 0.2f);

        AsyncOperation async;
        switch (gameScene)
        {
            case GameScenes.MENU:
                async = SceneManager.LoadSceneAsync("Menu");
                CSManager.swipe_condition = (TouchExtension touch) => { return true; };
                break;

            case GameScenes.SKILLPORT:
                async = SceneManager.LoadSceneAsync("SkillPort");
                break;

            case GameScenes.ABILITYPORT:
                async = SceneManager.LoadSceneAsync("AbilityPort");
                break;

            case GameScenes.SKILLFACTORY:
                async = SceneManager.LoadSceneAsync("SkillFactory");
                break;

            case GameScenes.ABILITYFACTORY:
                async = SceneManager.LoadSceneAsync("AbilityFactory");
                break;

            case GameScenes.ONLINELOBBY:
                async = SceneManager.LoadSceneAsync("OnlineLobby");
                break;

            case GameScenes.SORTIELOBBY:
                async = SceneManager.LoadSceneAsync("SortieLobby");
                break;

            case GameScenes.CANYON:
                async = SceneManager.LoadSceneAsync("Canyon");
                break;

            case GameScenes.SPACE:
                async = SceneManager.LoadSceneAsync("Space");
                break;

            case GameScenes.SNOWPEAK:
                async = SceneManager.LoadSceneAsync("SnowPeak");
                break;

            default:
                yield break;
        }
        async.allowSceneActivation = false;

        yield return new WaitUntil(() => async.progress >= 0.9f);

        float blink_duration = FadeCanvas.I.StartBlink();

        yield return new WaitForSeconds(blink_duration);

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
    MENU,
    SKILLPORT,
    ABILITYPORT,
    SKILLFACTORY,
    ABILITYFACTORY,
    ONLINELOBBY,
    SORTIELOBBY,
    CANYON,
    SPACE,
    SNOWPEAK
}