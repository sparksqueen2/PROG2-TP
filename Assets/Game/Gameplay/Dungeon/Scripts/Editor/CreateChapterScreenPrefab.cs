#if UNITY_EDITOR

using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CreateChapterScreenPrefab
{
    private const string PrefabPath = "Assets/Game/Gameplay/Dungeon/Prefabs/UI/ChapterScreen.prefab";

    [MenuItem("Game/UI/Crear Prefab ChapterScreen")]
    public static void Create()
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Art/ChapterScreen.png");

        var root = new GameObject("ChapterScreen", typeof(RectTransform), typeof(CanvasGroup), typeof(ChapterScreenView));
        var rootRect = root.GetComponent<RectTransform>();
        SetStretch(rootRect);

        var art = new GameObject("Art", typeof(RectTransform), typeof(RawImage));
        art.transform.SetParent(root.transform, false);
        SetStretch(art.GetComponent<RectTransform>());
        var rawImage = art.GetComponent<RawImage>();
        rawImage.texture = texture;
        rawImage.raycastTarget = false;

        var title = CreateText("TitleText", root.transform, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.52f), 30f,
            FontStyles.Bold, "CAPITULO I\nEl Umbral Corrupto");
        var body = CreateText("BodyText", root.transform, new Vector2(0.12f, 0.27f), new Vector2(0.88f, 0.4f), 18f,
            FontStyles.Normal, "Texto del capitulo...");

        var buttonGo = new GameObject("BeginButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(root.transform, false);
        var buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.38f, 0.19f);
        buttonRect.anchorMax = new Vector2(0.62f, 0.25f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        buttonGo.GetComponent<Image>().color = Color.clear;

        var buttonLabel = CreateText("Label", buttonGo.transform, Vector2.zero, Vector2.one, 24f, FontStyles.Bold,
            "COMENZAR");
        SetStretch(buttonLabel.rectTransform);

        var view = root.GetComponent<ChapterScreenView>();
        var serializedView = new SerializedObject(view);
        serializedView.FindProperty("titleText").objectReferenceValue = title;
        serializedView.FindProperty("bodyText").objectReferenceValue = body;
        serializedView.FindProperty("buttonLabel").objectReferenceValue = buttonLabel;
        serializedView.FindProperty("beginButton").objectReferenceValue = buttonGo.GetComponent<Button>();
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        var directory = Path.GetDirectoryName(PrefabPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null)
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        else
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Debug.Log($"Prefab de capítulo creado en {PrefabPath}. Editá ahí el layout.");
    }

    [InitializeOnLoadMethod]
    private static void EnsurePrefabExists()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(PrefabPath))
                Create();
        };
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        float fontSize, FontStyles style, string text)
    {
        var textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.92f, 0.88f, 0.82f, 1f);
        label.enableWordWrapping = true;
        return label;
    }

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

#endif
