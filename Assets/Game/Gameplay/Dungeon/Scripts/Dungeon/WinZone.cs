using System;
using System.Collections;

using UnityEngine;

using Cinemachine;

public class WinZone : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayer = default;
    [SerializeField] private float focusRoomTransitionTime = 0f;
    [SerializeField] private float winAnimationExtraTime = 0f;
    [SerializeField] private CinemachineVirtualCamera virtualCamera = null;
    [SerializeField] private Vector3 offset = Vector3.zero;

    [SerializeField] private float radius = 0f;
    [SerializeField] private int spawnCount = 0;
    [SerializeField] private float spawnDelay = 0f;
    [SerializeField] private GameObject[] chibiPrefabs = null;

    private Action onFinishGame = null;
    private Action onWinAnimationEnd = null;
    private bool hasTriggered = false;
    private bool isUnlocked = false;
    private Collider zoneCollider = null;

    public void Init(Action onFinishGame, Action onWinAnimationEnd)
    {
        this.onFinishGame = onFinishGame;
        this.onWinAnimationEnd = onWinAnimationEnd;
        zoneCollider = GetComponent<Collider>();
    }

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
        if (zoneCollider != null)
            zoneCollider.enabled = unlocked;

        if (unlocked)
            ShowExitMarker();
    }

    private void ShowExitMarker()
    {
        if (transform.Find("PurifiedThreshold") != null)
            return;

        var markerRoot = new GameObject("PurifiedThreshold");
        markerRoot.transform.SetParent(transform, false);
        markerRoot.transform.localPosition = Vector3.zero;
        ThresholdVisual.Apply(markerRoot.transform);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !isUnlocked)
            return;

        if (!Utils.CheckLayerInMask(playerLayer, other.gameObject.layer))
            return;

        var dungeonRun = FindFirstObjectByType<DungeonRunController>();
        if (dungeonRun != null && !dungeonRun.CanWin)
            return;

        hasTriggered = true;
        onFinishGame?.Invoke();
        PlayWinAnimation();
    }

    public void PlayWinAnimation()
    {
        virtualCamera.Follow = null;
        virtualCamera.LookAt = null;

        StartCoroutine(FocusRuneTransitionCoroutine());
        IEnumerator FocusRuneTransitionCoroutine()
        {
            float timer = 0f;
            Vector3 startPosition = virtualCamera.transform.position;
            Vector3 targetPosition = new Vector3(transform.position.x, startPosition.y, transform.position.z) + offset;

            while (timer < focusRoomTransitionTime)
            {
                timer += Time.deltaTime;

                virtualCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, timer / focusRoomTransitionTime);

                yield return new WaitForEndOfFrame();
            }

            for (int i = 0; i < spawnCount; i++)
            {
                float angle = i * Mathf.PI * 2 / spawnCount;

                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                Vector3 spawnPosition = new Vector3(transform.position.x + x, transform.position.y, transform.position.z + z);

                int randomIndex = UnityEngine.Random.Range(0, chibiPrefabs.Length);
                GameObject chibiGO = Instantiate(chibiPrefabs[randomIndex], transform);
                chibiGO.transform.position = spawnPosition;
                chibiGO.transform.forward = Vector3.back;

                yield return new WaitForSeconds(spawnDelay);
            }

            yield return new WaitForSeconds(winAnimationExtraTime);

            onWinAnimationEnd?.Invoke();
        }
    }
}
