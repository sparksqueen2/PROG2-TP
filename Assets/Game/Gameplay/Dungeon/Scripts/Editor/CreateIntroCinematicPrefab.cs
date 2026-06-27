#if UNITY_EDITOR

using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public static class CreateIntroCinematicPrefab
{
    private const string PrefabPath = "Assets/Game/Gameplay/Menu/Prefabs/IntroCinematic.prefab";
    private const string RenderTexturePath = "Assets/Game/Gameplay/Menu/Art/IntroVideo.renderTexture";
    private const string ClipPath = "Assets/Game/Art/Kael_expelled_from_order_202606242024.mp4";
    private const string MenuScenePath = "Assets/Game/Gameplay/_Scenes/Menu.unity";

    [InitializeOnLoadMethod]
    private static void EnsurePrefabExists()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(PrefabPath))
                Configure();
        };
    }

    [MenuItem("Game/UI/Configurar Intro Cinematic")]
    public static void Configure()
    {
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(ClipPath);
        var renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);

        if (clip == null)
        {
            Debug.LogError($"No se encontro el video en {ClipPath}");
            return;
        }

        if (renderTexture == null)
        {
            Debug.LogError($"No se encontro el RenderTexture en {RenderTexturePath}");
            return;
        }

        var root = new GameObject("IntroCinematic", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var player = root.AddComponent<IntroCinematicPlayer>();
        var videoPlayer = root.AddComponent<VideoPlayer>();
        var audioSource = root.AddComponent<AudioSource>();

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = false;
        videoPlayer.clip = clip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.aspectRatio = VideoAspectRatio.NoScaling;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        canvas.overrideSorting = true;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var rootRect = root.GetComponent<RectTransform>();
        SetStretch(rootRect);

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(root.transform, false);
        SetStretch(background.GetComponent<RectTransform>());
        var backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = Color.black;
        backgroundImage.raycastTarget = false;

        var videoHolder = new GameObject("VideoHolder", typeof(RectTransform), typeof(AspectRatioFitter));
        videoHolder.transform.SetParent(root.transform, false);
        SetStretch(videoHolder.GetComponent<RectTransform>());
        var fitter = videoHolder.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = (float)clip.width / Mathf.Max(1, clip.height);

        var videoImageGo = new GameObject("VideoImage", typeof(RectTransform), typeof(RawImage));
        videoImageGo.transform.SetParent(videoHolder.transform, false);
        SetStretch(videoImageGo.GetComponent<RectTransform>());
        var rawImage = videoImageGo.GetComponent<RawImage>();
        rawImage.texture = renderTexture;
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;

        var skipHint = CreateText("SkipHint", root.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(900f, 48f), 24f, "Enter / Escape para saltar");

        var serializedPlayer = new SerializedObject(player);
        serializedPlayer.FindProperty("videoPlayer").objectReferenceValue = videoPlayer;
        serializedPlayer.FindProperty("videoImage").objectReferenceValue = rawImage;
        serializedPlayer.FindProperty("skipLabel").objectReferenceValue = skipHint;
        serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

        var directory = Path.GetDirectoryName(PrefabPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        PlaceInMenuScene(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = prefab;
        Debug.Log($"Intro cinematic listo. Prefab: {PrefabPath}. Tambien quedo instanciado en {MenuScenePath}.");
    }

    private static void PlaceInMenuScene(GameObject prefab)
    {
        var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        var existing = GameObject.Find("IntroCinematic");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null)
            return;

        instance.transform.SetAsFirstSibling();
        instance.SetActive(false);

        var menuController = Object.FindFirstObjectByType<MenuController>();
        if (menuController != null)
        {
            var serializedMenu = new SerializedObject(menuController);
            serializedMenu.FindProperty("introCinematic").objectReferenceValue = instance.GetComponent<IntroCinematicPlayer>();
            serializedMenu.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menuController);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize, string text)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.85f, 0.78f, 0.62f, 0.9f);
        label.raycastTarget = false;
        return label;
    }
}

#endif
