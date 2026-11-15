#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class WristHealthUIBuilder
{
    private const string ScenePath = "Assets/Scenes/Conference Room Scene.unity";
    private const string FontPath = "Assets/Font/Orbitron/Orbitron-Regular SDF.asset";
    private const string ObjectName = "Wrist Health UI";

    static WristHealthUIBuilder() => EditorApplication.delayCall += BuildIfNeeded;

    [MenuItem("Tools/LAN Shooter/Build Wrist Health UI")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;

        VRRigReferences rig = Object.FindFirstObjectByType<VRRigReferences>(
            FindObjectsInactive.Include);
        if (rig == null || rig.leftHand == null)
        {
            Debug.LogWarning("[LAN UI] Local XR left-hand reference was not found.");
            return;
        }

        Transform existing = rig.leftHand.Find(ObjectName);
        if (existing != null)
            return;

        GameObject root = new(ObjectName, typeof(RectTransform), typeof(Canvas));
        root.layer = 5;
        root.transform.SetParent(rig.leftHand, false);
        root.transform.localPosition = new Vector3(0f, 0.075f, 0.025f);
        root.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
        root.transform.localScale = Vector3.one * 0.00075f;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 30;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(360f, 112f);

        GameObject background = CreateImage("Background", root.transform,
            new Color(0.025f, 0.055f, 0.09f, 0.96f), new Vector2(360f, 112f));
        GameObject track = CreateImage("Health Track", background.transform,
            new Color(0.12f, 0.17f, 0.23f, 1f), new Vector2(310f, 24f));
        track.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -23f);

        GameObject fillObject = CreateImage("Health Fill", track.transform,
            new Color(0.19f, 0.86f, 0.58f, 1f), new Vector2(310f, 24f));
        UnityEngine.UI.Image fill = fillObject.GetComponent<UnityEngine.UI.Image>();
        fill.type = UnityEngine.UI.Image.Type.Filled;
        fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;

        TextMeshProUGUI label = CreateText("Health Label", background.transform,
            "VITALS", 17f, new Color(0.35f, 0.78f, 0.95f, 1f),
            TextAlignmentOptions.Left, new Vector2(-96f, 25f), new Vector2(120f, 30f));
        label.fontStyle = FontStyles.Bold;
        TextMeshProUGUI value = CreateText("Health Value", background.transform,
            "100", 25f, Color.white, TextAlignmentOptions.Right,
            new Vector2(90f, 23f), new Vector2(130f, 36f));
        value.fontStyle = FontStyles.Bold;

        PlayerWristHealthUI display = root.AddComponent<PlayerWristHealthUI>();
        SerializedObject serialized = new(display);
        serialized.FindProperty("healthFill").objectReferenceValue = fill;
        serialized.FindProperty("healthText").objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(display);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[LAN UI] Editable wrist health display was created on the local left hand.");
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;
        VRRigReferences rig = Object.FindFirstObjectByType<VRRigReferences>(
            FindObjectsInactive.Include);
        if (rig != null && rig.leftHand != null && rig.leftHand.Find(ObjectName) == null)
            Build();
    }

    private static GameObject CreateImage(
        string name, Transform parent, Color color, Vector2 size)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        UnityEngine.UI.Image image = item.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return item;
    }

    private static TextMeshProUGUI CreateText(
        string name, Transform parent, string content, float size, Color color,
        TextAlignmentOptions alignment, Vector2 position, Vector2 bounds)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ??
                    TMP_Settings.defaultFontAsset;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = bounds;
        rect.anchoredPosition = position;
        return text;
    }
}
#endif
