using UnityEngine;
using UnityEngine.UI;

public class LogoPulse : MonoBehaviour
{
    [SerializeField] private float minAlpha = 0.92f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float speed = 0.5f;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void Update()
    {
        if (image == null) return;

        float wave = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
        Color color = image.color;
        color.a = Mathf.Lerp(minAlpha, maxAlpha, wave);
        image.color = color;
    }
}
