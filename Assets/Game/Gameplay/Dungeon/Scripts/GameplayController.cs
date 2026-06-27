using UnityEngine;

public class GameplayController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController = null;
    [SerializeField] private GameplayUI gameplayUI = null;
    [SerializeField] private WinZone winZone = null;
    [SerializeField] private AudioEvent musicEvent = null;
    [SerializeField] private AudioEvent winEvent = null;
    [SerializeField] private AudioEvent loseEvent = null;

    private DungeonRunController dungeonRunController = null;

    private void Awake()
    {
        dungeonRunController = GetComponent<DungeonRunController>();
    }

    private void Start()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (gameplayUI == null)
            gameplayUI = FindFirstObjectByType<GameplayUI>();

        if (winZone == null)
            winZone = FindFirstObjectByType<WinZone>();

        if (playerController == null)
        {
            Debug.LogError("No hay PlayerController en la escena Gameplay.");
            return;
        }

        PlacePlayerAtStart(playerController);
        PlayerTarget.Register(playerController.transform);
        GameplayAtmosphere.Apply();

        playerController.Init(ToggleOnPause, gameplayUI != null ? gameplayUI.UpdatePlayerHealth : null, LoseGame);

        if (gameplayUI != null)
            gameplayUI.Init(ToggleTimeScale, ToggleOffPause);
        winZone?.Init(VictoryPlayer, WinGame);

        if (dungeonRunController == null)
        {
            Debug.LogError("Falta DungeonRunController en GameplayController.");
            return;
        }

        dungeonRunController.Init(winZone, gameplayUI, playerController.transform);
        dungeonRunController.BeginDungeon();

        if (GameManager.Instance?.AudioManager != null)
            GameManager.Instance.AudioManager.PlayAudio(musicEvent);
    }

    private static void PlacePlayerAtStart(PlayerController player)
    {
        var playerRoot = player.transform.parent != null ? player.transform.parent : player.transform;
        playerRoot.gameObject.SetActive(true);

        var spawn = new Vector3(7f, 3f, -27f);
        var spawnPoint = Object.FindFirstObjectByType<PlayerSpawnPoint>(FindObjectsInactive.Include);
        if (spawnPoint != null)
        {
            spawn = spawnPoint.transform.position;
        }
        else
        {
            foreach (var room in Object.FindObjectsByType<RoomBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (room.gameObject.name != "Room S")
                    continue;

                var roomPosition = room.transform.position;
                spawn = new Vector3(roomPosition.x + 3f, 3f, roomPosition.z + 3f);
                break;
            }
        }

        var character = player.GetComponent<CharacterController>();
        if (character != null)
            character.enabled = false;

        DungeonGroundSnap.TrySnapToGround(ref spawn, spawn.y);
        playerRoot.position = spawn;

        if (!playerRoot.CompareTag("Player"))
            playerRoot.tag = "Player";

        if (!player.gameObject.CompareTag("Player"))
            player.gameObject.tag = "Player";

        PlayerTarget.Register(player.transform);

        if (character != null)
            character.enabled = true;

        Debug.Log($"[Gameplay] Player spawn en {spawn}");
    }

    private void ToggleOnPause()
    {
        gameplayUI?.TogglePause(true);
        ToggleTimeScale(false);
    }

    private void ToggleOffPause()
    {
        ToggleTimeScale(true);
        playerController.TogglePause(false);
    }

    private void VictoryPlayer()
    {
        playerController.DisableInput();
        playerController.PlayVictoryAnimation();

        EnemyManager.Instance.OnPlayerVictory();
        GameManager.Instance.AudioManager.StopCurrentMusic(
            onSuccess: () =>
            {
                GameManager.Instance.AudioManager.PlayAudio(winEvent);
            });
    }

    private void LoseGame()
    {
        gameplayUI.OpenLosePanel();
        EnemyManager.Instance.OnPlayerDefeated();

        GameManager.Instance.AudioManager.StopCurrentMusic(
            onSuccess: () =>
            {
                GameManager.Instance.AudioManager.PlayAudio(loseEvent);
            });
    }

    private void WinGame()
    {
        var winMessage = dungeonRunController != null
            ? dungeonRunController.GetWinMessage()
            : null;
        gameplayUI.OpenWinPanel(winMessage);
    }

    private void ToggleTimeScale(bool status)
    {
        Time.timeScale = status ? 1f : 0f;
    }
}
