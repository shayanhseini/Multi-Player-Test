#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ProfessionalScoreboardUIBuilder
{
    private const string ScenePath = "Assets/Scenes/Conference Room Scene.unity";
    private const string FontPath = "Assets/Font/Orbitron/Orbitron-Regular SDF.asset";
    private const string MarkerName = "Scoreboard UI V4";

    private static readonly Color Backdrop = Hex("07101D", 0.985f);
    private static readonly Color Surface = Hex("0E1B2B", 1f);
    private static readonly Color SurfaceRaised = Hex("17283D", 1f);
    private static readonly Color Accent = Hex("35C8F5", 1f);
    private static readonly Color Attackers = Hex("D64550", 1f);
    private static readonly Color Defenders = Hex("2474E5", 1f);
    private static readonly Color Text = Hex("F3F7FC", 1f);
    private static readonly Color Muted = Hex("8FA4BA", 1f);

    static ProfessionalScoreboardUIBuilder() => EditorApplication.delayCall += BuildIfNeeded;

    [MenuItem("Tools/LAN Shooter/Rebuild Professional Scoreboard UI")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;

        GameObject canvasObject = GameObject.Find("World Scoreboard Canvas");
        Transform panelTransform = canvasObject != null
            ? canvasObject.transform.Find("Scoreboard Panel")
            : null;
        MatchLobbyUI lobby = Object.FindFirstObjectByType<MatchLobbyUI>(
            FindObjectsInactive.Include);
        if (canvasObject == null || panelTransform == null || lobby == null)
        {
            Debug.LogError("[LAN UI] World Scoreboard Canvas, panel, or MatchLobbyUI was not found.");
            return;
        }

        GameObject panel = panelTransform.gameObject;
        while (panelTransform.childCount > 0)
            Object.DestroyImmediate(panelTransform.GetChild(0).gameObject);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1200f, 760f);
        canvasRect.localScale = Vector3.one * 0.0013f;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.sortingOrder = 40;

        UnityEngine.UI.Image panelImage = panel.GetComponent<UnityEngine.UI.Image>();
        panelImage.color = Backdrop;
        panelImage.sprite = BuiltInSprite();
        panelImage.type = UnityEngine.UI.Image.Type.Sliced;

        GameObject marker = CreateUI(MarkerName, panelTransform);
        marker.SetActive(true);
        BuildHeader(panelTransform);
        TextMeshProUGUI attackersText = BuildTeamCard(
            panelTransform, "Attackers Card", "ATTACKERS", "OBJECTIVE / EXTRACTION",
            Attackers, new Vector2(-288f, -25f));
        TextMeshProUGUI defendersText = BuildTeamCard(
            panelTransform, "Defenders Card", "DEFENDERS", "OBJECTIVE / PROTECTION",
            Defenders, new Vector2(288f, -25f));
        TextMeshProUGUI observersText = BuildFooter(panelTransform);

        SerializedObject serialized = new(lobby);
        serialized.FindProperty("scoreboardText").objectReferenceValue = null;
        serialized.FindProperty("attackersScoreboardText").objectReferenceValue = attackersText;
        serialized.FindProperty("defendersScoreboardText").objectReferenceValue = defendersText;
        serialized.FindProperty("observersScoreboardText").objectReferenceValue = observersText;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        EditorUtility.SetDirty(lobby);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[LAN UI] Professional world scoreboard was created.");
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;
        GameObject canvas = GameObject.Find("World Scoreboard Canvas");
        Transform panel = canvas != null ? canvas.transform.Find("Scoreboard Panel") : null;
        if (panel != null && panel.Find(MarkerName) == null)
            Rebuild();
    }

    private static void BuildHeader(Transform parent)
    {
        GameObject header = CreateImage("Header", parent, Surface,
            new Vector2(0f, 305f), new Vector2(1200f, 150f));
        CreateImage("Accent Line", header.transform, Accent,
            new Vector2(-555f, 0f), new Vector2(7f, 76f));
        TextMeshProUGUI title = CreateText("Title", header.transform,
            "MATCH SCOREBOARD", 36f, Text, TextAlignmentOptions.Left,
            new Vector2(-100f, 20f), new Vector2(900f, 52f));
        title.fontStyle = FontStyles.Bold;
        CreateText("Subtitle", header.transform,
            "LAN // 6V6 TACTICAL SESSION", 17f, Muted,
            TextAlignmentOptions.Left, new Vector2(-100f, -27f),
            new Vector2(900f, 30f));
        GameObject live = CreateImage("Live Status", header.transform,
            Hex("123A3B", 1f), new Vector2(475f, 0f), new Vector2(180f, 52f));
        CreateText("Label", live.transform, "●  LIVE", 17f,
            Hex("6DE3B3", 1f), TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(150f, 30f));
    }

    private static TextMeshProUGUI BuildTeamCard(
        Transform parent, string objectName, string title, string subtitle,
        Color teamColor, Vector2 position)
    {
        GameObject card = CreateImage(objectName, parent, Surface,
            position, new Vector2(550f, 465f));
        GameObject teamHeader = CreateImage("Team Header", card.transform,
            teamColor, new Vector2(0f, 190f), new Vector2(550f, 85f));
        TextMeshProUGUI titleText = CreateText("Team Name", teamHeader.transform,
            title, 25f, Text, TextAlignmentOptions.Left,
            new Vector2(0f, 12f), new Vector2(480f, 34f));
        titleText.fontStyle = FontStyles.Bold;
        CreateText("Objective", teamHeader.transform, subtitle, 13f,
            Hex("FFFFFF", 0.8f), TextAlignmentOptions.Left,
            new Vector2(0f, -17f), new Vector2(480f, 22f));

        CreateImage("Column Background", card.transform, SurfaceRaised,
            new Vector2(0f, 122f), new Vector2(500f, 44f));
        CreateText("Player Column", card.transform, "#    PLAYER", 14f, Muted,
            TextAlignmentOptions.Left, new Vector2(-93f, 122f),
            new Vector2(290f, 28f));
        TextMeshProUGUI statsHeader = CreateText("Stats Columns", card.transform,
            "<pos=345>K<pos=390>D<pos=430>STATUS", 12f, Muted,
            TextAlignmentOptions.Left, new Vector2(0f, 122f),
            new Vector2(500f, 28f));
        statsHeader.richText = true;

        TextMeshProUGUI rows = CreateText("Player Rows", card.transform,
            "NO PLAYERS ASSIGNED", 20f, Muted, TextAlignmentOptions.TopLeft,
            new Vector2(0f, -50f), new Vector2(500f, 285f));
        rows.richText = true;
        rows.lineSpacing = 18f;
        rows.textWrappingMode = TextWrappingModes.NoWrap;
        return rows;
    }

    private static TextMeshProUGUI BuildFooter(Transform parent)
    {
        GameObject footer = CreateImage("Footer", parent, Surface,
            new Vector2(0f, -335f), new Vector2(1200f, 70f));
        TextMeshProUGUI observer = CreateText("Observers", footer.transform,
            "OBSERVER  •  NONE", 16f, Muted, TextAlignmentOptions.Left,
            new Vector2(-180f, 0f), new Vector2(760f, 30f));
        GameObject hint = CreateImage("Close Hint", footer.transform,
            SurfaceRaised, new Vector2(475f, 0f), new Vector2(190f, 42f));
        CreateText("Label", hint.transform, "A  •  CLOSE", 15f, Text,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(165f, 26f));
        return observer;
    }

    private static GameObject CreateUI(string name, Transform parent)
    {
        GameObject item = new(name, typeof(RectTransform));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        return item;
    }

    private static GameObject CreateImage(
        string name, Transform parent, Color color, Vector2 position, Vector2 size)
    {
        GameObject item = CreateUI(name, parent);
        UnityEngine.UI.Image image = item.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.sprite = BuiltInSprite();
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.raycastTarget = false;
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return item;
    }

    private static TextMeshProUGUI CreateText(
        string name, Transform parent, string value, float size, Color color,
        TextAlignmentOptions alignment, Vector2 position, Vector2 bounds)
    {
        GameObject item = CreateUI(name, parent);
        TextMeshProUGUI text = item.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ??
                    TMP_Settings.defaultFontAsset;
        text.text = value;
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

    private static Sprite BuiltInSprite() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

    private static Color Hex(string value, float alpha)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        color.a = alpha;
        return color;
    }
}
#endif
