using UnityEngine;

public class PurifiedThresholdPulse : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 1.1f;
    [SerializeField] private float lightIntensityMin = 1.6f;
    [SerializeField] private float lightIntensityMax = 2.8f;

    private Light thresholdLight;
    private Transform innerGlow;
    private Vector3 innerBaseScale;

    private void Awake()
    {
        thresholdLight = GetComponentInChildren<Light>();
        innerGlow = transform.Find("InnerGlow");
        if (innerGlow != null)
            innerBaseScale = innerGlow.localScale;
    }

    private void Update()
    {
        var pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        if (thresholdLight != null)
            thresholdLight.intensity = Mathf.Lerp(lightIntensityMin, lightIntensityMax, pulse);

        if (innerGlow != null)
        {
            var scale = Mathf.Lerp(0.92f, 1.08f, pulse);
            innerGlow.localScale = innerBaseScale * scale;
        }
    }
}
