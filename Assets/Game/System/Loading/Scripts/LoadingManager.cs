using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    private bool isLoading = false;
    private LoadingUI loadingUI = null;

    private Action onFinishTransition = null;

    private static readonly Dictionary<SceneGame, string> sceneNames = new Dictionary<SceneGame, string>()
    {
        { SceneGame.Menu, "Menu" },
        { SceneGame.Gameplay, "Gameplay" },
        { SceneGame.Loading, "Loading" }
    };

    public void SetLoadingUI(LoadingUI loadingUI)
    {
        this.loadingUI = loadingUI;
    }

    public void TransitionScene(SceneGame nextScene, Action onComplete = null)
    {
        isLoading = true;

        loadingUI.ToggleUI(true,
            onComplete: () =>
            {
                SceneGame previousScene = GetCurrentScene();

                UnloadScene(previousScene,
                    onSuccess: () =>
                    {
                        LoadingScene(nextScene,
                            onSuccess: () =>
                            {
                                SetActiveScene(nextScene);
                                loadingUI.ToggleUI(false,
                                    onComplete: () =>
                                    {
                                        onComplete?.Invoke();

                                        onFinishTransition?.Invoke();
                                        onFinishTransition = null;

                                        isLoading = false;
                                    });
                            });
                    });
            });
    }

    public void SetFinishTransitionCallback(Action callback)
    {
        if (isLoading)
        {
            onFinishTransition += callback;
        }
        else
        {
            callback.Invoke();
        }
    }

    public void LoadingScene(SceneGame scene, Action onSuccess = null)
    {
        if (!sceneNames.TryGetValue(scene, out string sceneName))
        {
            onSuccess?.Invoke();
            return;
        }

        if (IsSceneLoaded(sceneName))
        {
            onSuccess?.Invoke();
            return;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        op.completed += _ => onSuccess?.Invoke();
    }

    private void UnloadScene(SceneGame scene, Action onSuccess = null)
    {
        if (!sceneNames.TryGetValue(scene, out string sceneName))
        {
            onSuccess?.Invoke();
            return;
        }

        Scene sceneToUnload = SceneManager.GetSceneByName(sceneName);
        if (!sceneToUnload.IsValid() || !sceneToUnload.isLoaded)
        {
            onSuccess?.Invoke();
            return;
        }

        AsyncOperation op = SceneManager.UnloadSceneAsync(sceneToUnload);
        op.completed += _ => onSuccess?.Invoke();
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    private SceneGame GetCurrentScene()
    {
        string currSceneName = SceneManager.GetActiveScene().name;

        foreach (KeyValuePair<SceneGame, string> scene in sceneNames)
        {
            if (scene.Value == currSceneName)
            {
                return scene.Key;
            }
        }

        return default;
    }

    private void SetActiveScene(SceneGame scene)
    {
        if (sceneNames.TryGetValue(scene, out string sceneName))
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        }
    }
}
