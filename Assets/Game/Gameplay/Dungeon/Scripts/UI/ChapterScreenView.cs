using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterScreenView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText = null;
    [SerializeField] private TextMeshProUGUI bodyText = null;
    [SerializeField] private TextMeshProUGUI buttonLabel = null;
    [SerializeField] private Button beginButton = null;
    [SerializeField] private string defaultButtonText = "COMENZAR";

    private Action onBegin;

    private void Awake()
    {
        if (beginButton != null)
            beginButton.onClick.AddListener(HandleBeginClicked);

        gameObject.SetActive(false);
    }

    public void Show(string title, string body, Action beginCallback, string buttonText = null)
    {
        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;

        if (buttonLabel != null)
            buttonLabel.text = string.IsNullOrWhiteSpace(buttonText) ? defaultButtonText : buttonText;

        onBegin = beginCallback;
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    private void HandleBeginClicked()
    {
        gameObject.SetActive(false);

        var callback = onBegin;
        onBegin = null;
        callback?.Invoke();
    }
}
