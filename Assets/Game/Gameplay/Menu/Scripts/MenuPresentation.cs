using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuPresentation : MonoBehaviour
{
    private static readonly Color GoldText = new Color(0.85f, 0.72f, 0.38f, 1f);
    private static readonly Color SteelText = new Color(0.78f, 0.8f, 0.86f, 1f);

    [SerializeField] private RectTransform menuCanvas;
    [SerializeField] private Transform atmosphereRoot;

    private void Start()
    {
        ApplyAtmosphere();
        SetupCharacterLighting();
        StyleMenuText();
        AddScreenVignette();
    }

    private void ApplyAtmosphere()
    {
        RenderSettings.fog = false;

        RenderSettings.ambientSkyColor = new Color(0.22f, 0.24f, 0.3f);
        RenderSettings.ambientEquatorColor = new Color(0.14f, 0.15f, 0.19f);
        RenderSettings.ambientGroundColor = new Color(0.08f, 0.08f, 0.1f);
        RenderSettings.ambientIntensity = 1.15f;

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light.type != LightType.Directional) continue;

            light.color = new Color(0.82f, 0.7f, 0.45f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
        }
    }

    private void SetupCharacterLighting()
    {
        Transform root = atmosphereRoot != null ? atmosphereRoot : transform;
        Transform player = FindChildRecursive(root, "jugador");
        Vector3 focus = player != null ? player.position : new Vector3(-250f, 120f, -100f);

        CreatePointLight(root, "MenuCharacterKey", focus + new Vector3(-180f, 220f, 120f),
            new Color(0.9f, 0.78f, 0.5f), 3.5f, 1400f);

        CreatePointLight(root, "MenuCharacterFill", focus + new Vector3(220f, 160f, 80f),
            new Color(0.55f, 0.62f, 0.78f), 2f, 1200f);

        CreatePointLight(root, "MenuCharacterRim", focus + new Vector3(60f, 140f, -220f),
            new Color(0.75f, 0.55f, 0.35f), 1.6f, 1000f);
    }

    private static void CreatePointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, true);
        lightObject.transform.position = position;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private void AddScreenVignette()
    {
        if (menuCanvas == null) return;

        GameObject vignetteObject = new GameObject("MenuVignette");
        vignetteObject.transform.SetParent(menuCanvas, false);
        vignetteObject.transform.SetAsFirstSibling();

        RectTransform vignetteRect = vignetteObject.AddComponent<RectTransform>();
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = Vector2.zero;

        Image vignetteImage = vignetteObject.AddComponent<Image>();
        vignetteImage.color = new Color(0.04f, 0.05f, 0.09f, 0.18f);
        vignetteImage.raycastTarget = false;
    }

    private void StyleMenuText()
    {
        if (menuCanvas == null) return;

        TextMeshProUGUI[] labels = menuCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            switch (label.text)
            {
                case "Jugar":
                case "Play":
                    label.color = GoldText;
                    label.fontStyle = FontStyles.Bold;
                    break;
                case "Salir":
                    label.color = new Color(0.72f, 0.38f, 0.32f, 1f);
                    break;
                default:
                    label.color = SteelText;
                    break;
            }
        }
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }
}
