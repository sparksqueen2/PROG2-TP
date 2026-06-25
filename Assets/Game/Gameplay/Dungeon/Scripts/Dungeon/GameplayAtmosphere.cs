using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class GameplayAtmosphere
{
    public static void Apply()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.009f;
        RenderSettings.fogColor = new Color(0.14f, 0.12f, 0.18f, 1f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f, 0.2f, 0.28f);
        RenderSettings.ambientEquatorColor = new Color(0.16f, 0.18f, 0.14f);
        RenderSettings.ambientGroundColor = new Color(0.1f, 0.12f, 0.09f);
        RenderSettings.ambientIntensity = 0.85f;
        RenderSettings.reflectionIntensity = 0.45f;

        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional)
                continue;

            light.color = new Color(0.78f, 0.74f, 0.88f);
            light.intensity = 0.92f;
            light.colorTemperature = 5200f;
            light.useColorTemperature = true;
            RenderSettings.sun = light;
            break;
        }

        var volume = Object.FindFirstObjectByType<Volume>();
        if (volume == null || volume.profile == null)
            return;

        if (volume.profile.TryGet(out Vignette vignette))
            vignette.intensity.value = 0.22f;

        if (volume.profile.TryGet(out ColorAdjustments colorAdjustments))
            colorAdjustments.saturation.value = -12f;
    }
}
