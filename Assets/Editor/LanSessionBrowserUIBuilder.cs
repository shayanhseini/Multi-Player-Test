#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class LanSessionBrowserUIBuilder
{
    private const string ScenePath = "Assets/Scenes/Conference Room Scene.unity";
    private const string RootName = "Professional LAN Lobby";
    private const string PageName = "LAN Session Browser";
    private const string FontPath = "Assets/Font/Orbitron/Orbitron-Regular SDF.asset";

    static LanSessionBrowserUIBuilder() => EditorApplication.delayCall += BuildIfNeeded;

    [MenuItem("Tools/LAN Shooter/Build LAN Session Browser")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath) return;
        GameObject root = GameObject.Find(RootName);
        UIManager ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        ConnectionManager connection = Object.FindFirstObjectByType<ConnectionManager>(FindObjectsInactive.Include);
        if (root == null || ui == null || connection == null || root.transform.Find(PageName) != null)
            return;

        GameObject page = CreateUI(PageName, root.transform);
        Stretch(page.GetComponent<RectTransform>());
        GameObject card = CreateImage("Browser Card", page.transform,
            Hex("0E1B2B"), new Vector2(0f, -20f), new Vector2(1120f, 650f));
        TextMeshProUGUI title = CreateText("Title", card.transform,
            "AVAILABLE LAN ROOMS", 30f, Color.white, TextAlignmentOptions.Left,
            new Vector2(-285f, 270f), new Vector2(500f, 45f));
        title.fontStyle = FontStyles.Bold;
        TextMeshProUGUI status = CreateText("Scan Status", card.transform,
            "SCANNING LOCAL NETWORK...", 14f, Hex("67DDB2"),
            TextAlignmentOptions.Right, new Vector2(290f, 270f), new Vector2(470f, 36f));

        Button[] buttons = new Button[6];
        TextMeshProUGUI[] labels = new TextMeshProUGUI[6];
        for (int i = 0; i < buttons.Length; i++)
        {
            float y = 190f - i * 78f;
            GameObject row = CreateImage($"Session Row {i + 1}", card.transform,
                Hex("17283D"), new Vector2(0f, y), new Vector2(1000f, 66f));
            buttons[i] = row.AddComponent<Button>();
            buttons[i].targetGraphic = row.GetComponent<UnityEngine.UI.Image>();
            labels[i] = CreateText("Room Information", row.transform,
                "LAN COMBAT ROOM     0/12\n<color=#7E94AA>192.168.1.10:7777</color>",
                15f, Color.white, TextAlignmentOptions.Left,
                new Vector2(-100f, 0f), new Vector2(760f, 54f));
            TextMeshProUGUI join = CreateText("Join Label", row.transform,
                "JOIN  ›", 16f, Hex("35C8F5"), TextAlignmentOptions.Right,
                new Vector2(390f, 0f), new Vector2(150f, 42f));
            join.fontStyle = FontStyles.Bold;
        }

        Button back = CreateButton(card.transform, "Back Button", "BACK",
            Hex("26384C"), new Vector2(-400f, -270f), new Vector2(190f, 52f));
        Button refresh = CreateButton(card.transform, "Refresh Button", "REFRESH LAN",
            Hex("2474E5"), new Vector2(370f, -270f), new Vector2(250f, 52f));

        LanSessionBrowserUI browser = page.AddComponent<LanSessionBrowserUI>();
        SerializedObject browserSO = new(browser);
        browserSO.FindProperty("connectionManager").objectReferenceValue = connection;
        browserSO.FindProperty("uiManager").objectReferenceValue = ui;
        browserSO.FindProperty("statusText").objectReferenceValue = status;
        browserSO.FindProperty("refreshButton").objectReferenceValue = refresh;
        browserSO.FindProperty("backButton").objectReferenceValue = back;
        AssignArray(browserSO.FindProperty("sessionButtons"), buttons);
        AssignArray(browserSO.FindProperty("sessionLabels"), labels);
        browserSO.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject uiSO = new(ui);
        uiSO.FindProperty("sessionBrowser").objectReferenceValue = page;
        uiSO.FindProperty("sessionBrowserController").objectReferenceValue = browser;
        uiSO.ApplyModifiedPropertiesWithoutUndo();
        page.SetActive(false);
        EditorUtility.SetDirty(ui);
        EditorUtility.SetDirty(browser);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[LAN UI] Multi-session LAN browser was created and assigned.");
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating ||
            SceneManager.GetActiveScene().path != ScenePath) return;
        GameObject root = GameObject.Find(RootName);
        if (root != null && root.transform.Find(PageName) == null) Build();
    }

    private static void AssignArray<T>(SerializedProperty property, T[] values)
        where T : Object
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static GameObject CreateUI(string name, Transform parent)
    {
        GameObject item = new(name, typeof(RectTransform));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        return item;
    }

    private static GameObject CreateImage(string name, Transform parent,
        Color color, Vector2 position, Vector2 size)
    {
        GameObject item = CreateUI(name, parent);
        UnityEngine.UI.Image image = item.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.type = UnityEngine.UI.Image.Type.Sliced;
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return item;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent,
        string value, float size, Color color, TextAlignmentOptions alignment,
        Vector2 position, Vector2 bounds)
    {
        GameObject item = CreateUI(name, parent);
        TextMeshProUGUI text = item.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ?? TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.sizeDelta = bounds;
        rect.anchoredPosition = position;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label,
        Color color, Vector2 position, Vector2 size)
    {
        GameObject item = CreateImage(name, parent, color, position, size);
        Button button = item.AddComponent<Button>();
        button.targetGraphic = item.GetComponent<UnityEngine.UI.Image>();
        TextMeshProUGUI text = CreateText("Label", item.transform, label, 15f,
            Color.white, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(16f, 8f));
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        return color;
    }
}
#endif
