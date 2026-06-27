using System;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUI : MonoBehaviour
{
    [Header("HUD Settings")]
    [SerializeField] private Slider playerHealthSlider = null;

    [Header("Pause Settings")]
    [SerializeField] private GameObject pausePanel = null;
    [SerializeField] private Button resumeBtn = null;
    [SerializeField] private Button backToMenuBtn = null;

    [Header("Lose Settings")]
    [SerializeField] private GameObject losePanel = null;
    [SerializeField] private Button retryBtn = null;
    [SerializeField] private Button loseBackToMenuBtn = null;

    [Header("Win Settings")]
    [SerializeField] private GameObject winPanel = null;
    [SerializeField] private Button winBackToMenuBtn = null;

    private Action<bool> onToggleTimeScale = null;
    private Action onToggleOnPlayerInput = null;

    private void Start()
    {
        resumeBtn.onClick.AddListener(() => TogglePause(false));
        backToMenuBtn.onClick.AddListener(BackToMenu);

        retryBtn.onClick.AddListener(Retry);
        loseBackToMenuBtn.onClick.AddListener(BackToMenu);

        winBackToMenuBtn.onClick.AddListener(BackToMenu);
    }

    public void Init(Action<bool> onToggleTimeScale, Action onToggleOnPlayerInput)
    {
        this.onToggleTimeScale = onToggleTimeScale;
        this.onToggleOnPlayerInput = onToggleOnPlayerInput;
    }

    public void TogglePause(bool status)
    {
        pausePanel.SetActive(status);
        onToggleTimeScale?.Invoke(!status);

        if (!status)
        {
            onToggleOnPlayerInput?.Invoke();
        }
    }

    public void OpenLosePanel()
    {
        losePanel.SetActive(true);
    }

    public void OpenWinPanel(string message = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            var title = winPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (title != null)
                title.text = message;
        }

        winPanel.SetActive(true);
    }

    public void ReturnToMenu()
    {
        BackToMenu();
    }

    public void UpdatePlayerHealth(int currentLives, int maxLives)
    {
        if (playerHealthSlider == null || maxLives <= 0)
            return;

        playerHealthSlider.value = (float)currentLives / maxLives;
    }

    private void BackToMenu()
    {
        resumeBtn.interactable = false;
        backToMenuBtn.interactable = false;

        retryBtn.interactable = false;
        loseBackToMenuBtn.interactable = false;

        winBackToMenuBtn.interactable = false;

        GameManager.Instance.ChangeScene(SceneGame.Menu);
        GameManager.Instance.AudioManager.ToggleMusic(true);
        onToggleTimeScale?.Invoke(true);
    }

    private void Retry()
    {
        retryBtn.interactable = false;
        loseBackToMenuBtn.interactable = false;

        GameManager.Instance.ChangeScene(SceneGame.Gameplay);
        GameManager.Instance.AudioManager.ToggleMusic(true);
    }

    
}