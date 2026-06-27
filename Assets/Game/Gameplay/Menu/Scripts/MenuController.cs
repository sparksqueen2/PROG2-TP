using UnityEngine;

public class MenuController : MonoBehaviour
{
    [SerializeField] private AudioEvent menuMusicEvent = null;
    [SerializeField] private IntroCinematicPlayer introCinematic = null;
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject creditsCanvas;
    [SerializeField] private GameObject configCanvas;

    public void Start()
    {
        if (introCinematic == null)
            introCinematic = FindFirstObjectByType<IntroCinematicPlayer>(FindObjectsInactive.Include);

        GameManager.Instance.AudioManager.PlayAudio(menuMusicEvent);
    }

    public void OnStartGameButton()
    {
        menuCanvas.SetActive(false);
        creditsCanvas.SetActive(false);
        configCanvas.SetActive(false);

        GameManager.Instance.AudioManager.StopCurrentMusic();

        if (introCinematic == null)
            introCinematic = FindFirstObjectByType<IntroCinematicPlayer>(FindObjectsInactive.Include);

        if (introCinematic == null)
        {
            GameManager.Instance.ChangeScene(SceneGame.Gameplay);
            return;
        }

        introCinematic.Play(() => GameManager.Instance.ChangeScene(SceneGame.Gameplay));
    }

    public void ShowCredits()
    {
        menuCanvas.SetActive(false);
        creditsCanvas.SetActive(true);
    }

    public void ShowMenu()
    {
        creditsCanvas.SetActive(false);
        menuCanvas.SetActive(true);
        configCanvas.SetActive(false);
    }

    public void ConfigButton()
    {
        menuCanvas.SetActive(false);
        configCanvas.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Game is exiting");
    }
}
