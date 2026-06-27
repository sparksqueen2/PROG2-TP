using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroCinematicPlayer : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private TextMeshProUGUI skipLabel;

    private Action onComplete;
    private Coroutine playRoutine;

    private void Awake()
    {
        if (videoImage == null)
            videoImage = GetComponentInChildren<RawImage>(true);
    }

    public void Play(Action complete)
    {
        if (videoPlayer == null || videoPlayer.clip == null)
        {
            complete?.Invoke();
            return;
        }

        onComplete = complete;

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        if (videoImage != null)
            videoImage.color = Color.white;

        gameObject.SetActive(true);
        playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        videoPlayer.Prepare();

        var timeout = 8f;
        while (!videoPlayer.isPrepared && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogWarning("[IntroCinematic] No se pudo preparar el video.");
            Finish();
            yield break;
        }

        videoPlayer.Play();

        while (videoPlayer.isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Escape) ||
                Input.GetMouseButtonDown(0))
            {
                break;
            }

            yield return null;
        }

        Finish();
    }

    private void Finish()
    {
        videoPlayer.Stop();
        gameObject.SetActive(false);
        playRoutine = null;

        var callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }
}
