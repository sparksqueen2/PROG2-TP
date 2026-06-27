using System;
using System.Collections;

using TMPro;

using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup = null;
    [SerializeField] private float lerpTime = 0.3f;
    [SerializeField] private TextMeshProUGUI loreText = null;

    [SerializeField] private string[] loreQuotes =
    {
        "Kael juro lealtad. El reino respondio con destierro.",
        "Nadie recuerda al heroe. Todos recuerdan al exiliado.",
        "Las ruinas aun susurran su nombre.",
        "Algunas heridas no sangran: arden."
    };

    private Coroutine fadeCoroutine = null;

    private void Awake()
    {
        GameManager.Instance.LoadingManager.SetLoadingUI(this);

        if (loreText != null)
        {
            loreText.color = new Color(0.77f, 0.66f, 0.35f, 1f);
        }
    }

    public void ToggleUI(bool status, Action onComplete = null)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (status)
        {
            ShowLoadingMessage();
        }
        else if (loreText != null)
        {
            loreText.text = string.Empty;
        }

        fadeCoroutine = StartCoroutine(LoadingCoroutine(status, onComplete));
    }

    private void ShowLoadingMessage()
    {
        if (loreText == null)
            return;

        string quote = string.Empty;
        if (loreQuotes != null && loreQuotes.Length > 0)
        {
            quote = loreQuotes[UnityEngine.Random.Range(0, loreQuotes.Length)];
        }

        loreText.text = string.IsNullOrEmpty(quote)
            ? "CARGANDO..."
            : $"CARGANDO...\n\n<size=120%>{quote}</size>";
    }

    private IEnumerator LoadingCoroutine(bool status, Action onComplete)
    {
        float timer = 0f;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = status ? 1f : 0f;
        float duration = Mathf.Max(lerpTime, 0.01f);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = status;
        fadeCoroutine = null;
        onComplete?.Invoke();
    }
}
