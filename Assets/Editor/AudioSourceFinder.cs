using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AudioSourceFinder : EditorWindow
{
    public AudioMixerGroup seMixerGroup; // SE用のAudioMixerGroup
    private List<string> foundObjects = new List<string>();

    [MenuItem("Tools/Find AudioSources")]
    public static void ShowWindow()
    {
        GetWindow<AudioSourceFinder>("AudioSource Finder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Find and Set AudioSource Output", EditorStyles.boldLabel);

        // AudioMixerGroupを選択するフィールド
        seMixerGroup = (AudioMixerGroup)EditorGUILayout.ObjectField("SE Mixer Group", seMixerGroup, typeof(AudioMixerGroup), false);

        if (GUILayout.Button("Find and Set All AudioSources"))
        {
            if (seMixerGroup == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an AudioMixerGroup for SE.", "OK");
                return;
            }

            FindAndSetAudioSources();
        }

        GUILayout.Label("AudioSource Found in:");

        foreach (string path in foundObjects)
        {
            if (GUILayout.Button(path))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(path);
            }
        }
    }

    private void FindAndSetAudioSources()
    {
        foundObjects.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab t:Scene");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (asset is GameObject gameObject)
            {
                // プレハブの場合
                UpdateAudioSourcesInGameObject(gameObject, path);
            }
            else if (path.EndsWith(".unity"))
            {
                /*
                // シーンの場合
                string sceneName = path;
                EditorSceneManager.OpenScene(sceneName, OpenSceneMode.Additive);

                foreach (GameObject rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    UpdateAudioSourcesInGameObject(rootObject, sceneName);
                }

                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                EditorSceneManager.CloseScene(SceneManager.GetActiveScene(), true);
                */
            }
        }
    }

    private void UpdateAudioSourcesInGameObject(GameObject gameObject, string assetPath)
    {
        bool found = false;
        AudioSource[] audioSources = gameObject.GetComponentsInChildren<AudioSource>(true);

        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource.outputAudioMixerGroup != seMixerGroup)
            {
                Undo.RecordObject(audioSource, "Set AudioMixerGroup");
                audioSource.outputAudioMixerGroup = seMixerGroup;
                EditorUtility.SetDirty(audioSource);
                found = true;
            }
        }

        if (found)
        {
            foundObjects.Add(assetPath);
        }
    }
}
