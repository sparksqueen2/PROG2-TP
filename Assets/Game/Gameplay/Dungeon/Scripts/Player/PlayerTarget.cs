using UnityEngine;

public static class PlayerTarget
{
    private static Transform cachedTransform;

    public static Transform Transform
    {
        get
        {
            if (cachedTransform != null)
                return cachedTransform;

            var playerController = Object.FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                cachedTransform = playerController.transform;
                return cachedTransform;
            }

            try
            {
                var tagged = GameObject.FindWithTag("Player");
                if (tagged != null)
                {
                    cachedTransform = tagged.transform;
                    return cachedTransform;
                }
            }
            catch (UnityException)
            {
                // Tag "Player" no definido en el proyecto.
            }

            return null;
        }
    }

    public static void ClearCache() => cachedTransform = null;

    public static void Register(Transform playerTransform) => cachedTransform = playerTransform;
}
