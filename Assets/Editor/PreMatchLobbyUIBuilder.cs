#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PreMatchLobbyUIBuilder
{
    private const string ScenePath = "Assets/Scenes/Conference Room Scene.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/VR Network Player.prefab";
    private const string FontPath = "Assets/Font/Orbitron/Orbitron-Regular SDF.asset";
    private const string PanelName = "Pre-Match Lobby";

    private static readonly Color Backdrop = Hex("07101D", 0.995f);
    private static readonly Color Surface = Hex("0E1B2B", 1f);
    private static readonly Color SurfaceRaised = Hex("17283D", 1f);
    private static readonly Color Accent = Hex("35C8F5", 1f);
    private static readonly Color Attackers = Hex("D64550", 1f);
    private static readonly Color Defenders = Hex("2474E5", 1f);
    private static readonly Color Warning = Hex("D9A514", 1f);
    private static readonly Color Text = Hex("F3F7FC", 1f);
    private static readonly Color Muted = Hex("8FA4BA", 1f);

    static PreMatchLobbyUIBuilder() => EditorApplication.delayCall += BuildIfNeeded;

    [MenuItem("Tools/LAN Shooter/Rebuild Pre-Match Lobby UI")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;

        GameObject root = GameObject.Find("Professional LAN Lobby");
        MatchLobbyUI lobby = FindPrimaryLobbyController();
        if (root == null || lobby == null)
            return;

        Transform existing = root.transform.Find(PanelName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject panel = CreateImage(PanelName, root.transform, Backdrop,
            new Vector2(0f, -35f), new Vector2(1320f, 620f));

        TextMeshProUGUI title = CreateText("Title", panel.transform,
            "PRE-MATCH TEAM LOBBY", 31f, Text, TextAlignmentOptions.Left,
            new Vector2(-245f, 260f), new Vector2(760f, 44f));
        title.fontStyle = FontStyles.Bold;
        TextMeshProUGUI status = CreateText("Match Status", panel.transform,
            "TEAMS OPEN  •  WAITING FOR HOST TO START", 16f,
            Hex("6DE3B3", 1f), TextAlignmentOptions.Right,
            new Vector2(335f, 260f), new Vector2(500f, 34f));

        TextMeshProUGUI attackersRoster = BuildTeamCard(
            panel.transform, "Attackers Team", "ATTACKERS", "CAPTURE / EXTRACT",
            Attackers, new Vector2(-315f, 25f), out Button switchAttackers);
        TextMeshProUGUI defendersRoster = BuildTeamCard(
            panel.transform, "Defenders Team", "DEFENDERS", "PROTECT / HOLD",
            Defenders, new Vector2(315f, 25f), out Button switchDefenders);

        GameObject footer = CreateImage("Lobby Footer", panel.transform, Surface,
            new Vector2(0f, -255f), new Vector2(1260f, 82f));
        TextMeshProUGUI observer = CreateText("Observers", footer.transform,
            "OBSERVER  •  NONE", 15f, Muted, TextAlignmentOptions.Left,
            new Vector2(-245f, 0f), new Vector2(700f, 30f));
        Button leave = CreateButton(footer.transform, "Leave Button", "LEAVE",
            SurfaceRaised, new Vector2(320f, 0f), new Vector2(170f, 48f));
        Button start = CreateButton(footer.transform, "Start Match Button", "START MATCH",
            Warning, new Vector2(515f, 0f), new Vector2(200f, 48f));

        SerializedObject serialized = new(lobby);
        serialized.FindProperty("preMatchPanel").objectReferenceValue = panel;
        serialized.FindProperty("attackersRosterText").objectReferenceValue = attackersRoster;
        serialized.FindProperty("defendersRosterText").objectReferenceValue = defendersRoster;
        serialized.FindProperty("preMatchStatusText").objectReferenceValue = status;
        serialized.FindProperty("preMatchObserverText").objectReferenceValue = observer;
        serialized.FindProperty("switchAttackersButton").objectReferenceValue = switchAttackers;
        serialized.FindProperty("switchDefendersButton").objectReferenceValue = switchDefenders;
        serialized.FindProperty("startMatchButton").objectReferenceValue = start;
        serialized.FindProperty("preMatchLeaveButton").objectReferenceValue = leave;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        ApplyOrbitronFont();
        EditorUtility.SetDirty(lobby);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[LAN UI] Networked pre-match team lobby was created.");
    }

    private static TextMeshProUGUI BuildTeamCard(
        Transform parent, string name, string title, string subtitle,
        Color teamColor, Vector2 position, out Button switchButton)
    {
        GameObject card = CreateImage(name, parent, Surface,
            position, new Vector2(600f, 410f));
        GameObject header = CreateImage("Team Header", card.transform, teamColor,
            new Vector2(0f, 160f), new Vector2(600f, 90f));
        TextMeshProUGUI titleText = CreateText("Team Name", header.transform,
            title, 24f, Text, TextAlignmentOptions.Left,
            new Vector2(0f, 13f), new Vector2(520f, 34f));
        titleText.fontStyle = FontStyles.Bold;
        CreateText("Objective", header.transform, subtitle, 13f,
            Hex("FFFFFF", 0.82f), TextAlignmentOptions.Left,
            new Vector2(0f, -18f), new Vector2(520f, 22f));
        CreateText("Roster Label", card.transform, "CONNECTED PLAYERS", 13f,
            Muted, TextAlignmentOptions.Left,
            new Vector2(0f, 91f), new Vector2(520f, 24f));
        TextMeshProUGUI roster = CreateText("Roster", card.transform,
            "OPEN SLOT", 20f, Text, TextAlignmentOptions.TopLeft,
            new Vector2(0f, -18f), new Vector2(520f, 190f));
        roster.richText = true;
        roster.lineSpacing = 14f;
        switchButton = CreateButton(card.transform, "Switch Team Button",
            "SWITCH TO " + title, teamColor,
            new Vector2(0f, -160f), new Vector2(520f, 56f));
        return roster;
    }

    private static void ApplyOrbitronFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogWarning("[LAN UI] Orbitron-Regular SDF was not found.");
            return;
        }

        foreach (GameObject sceneRoot in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (TMP_Text text in sceneRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = font;
                EditorUtility.SetDirty(text);
            }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            foreach (TMP_Text text in prefabRoot.GetComponentsInChildren<TMP_Text>(true))
                text.font = font;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;
        GameObject root = GameObject.Find("Professional LAN Lobby");
        if (root == null)
            return;
        if (root.transform.Find(PanelName) == null)
        {
            Rebuild();
            return;
        }

        RepairExistingReferences(root);
    }

    [MenuItem("Tools/LAN Shooter/Repair Lobby UI References")]
    public static void RepairLobbyReferences()
    {
        GameObject root = GameObject.Find("Professional LAN Lobby");
        if (root != null)
            RepairExistingReferences(root);
    }

    private static MatchLobbyUI FindPrimaryLobbyController()
    {
        MatchLobbyUI fallback = null;
        foreach (MatchLobbyUI candidate in Object.FindObjectsByType<MatchLobbyUI>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            fallback ??= candidate;
            SerializedObject serialized = new(candidate);
            GameObject selection = serialized.FindProperty("selectionPanel")
                .objectReferenceValue as GameObject;
            if (selection != null && selection.name == "Player Setup")
                return candidate;
        }
        return fallback;
    }

    private static void RepairExistingReferences(GameObject root)
    {
        MatchLobbyUI primary = FindPrimaryLobbyController();
        Transform panel = root.transform.Find(PanelName);
        if (primary == null || panel == null)
            return;

        Transform attackers = panel.Find("Attackers Team");
        Transform defenders = panel.Find("Defenders Team");
        Transform footer = panel.Find("Lobby Footer");
        SerializedObject serialized = new(primary);
        serialized.FindProperty("preMatchPanel").objectReferenceValue = panel.gameObject;
        serialized.FindProperty("attackersRosterText").objectReferenceValue =
            attackers?.Find("Roster")?.GetComponent<TextMeshProUGUI>();
        serialized.FindProperty("defendersRosterText").objectReferenceValue =
            defenders?.Find("Roster")?.GetComponent<TextMeshProUGUI>();
        serialized.FindProperty("preMatchStatusText").objectReferenceValue =
            panel.Find("Match Status")?.GetComponent<TextMeshProUGUI>();
        serialized.FindProperty("preMatchObserverText").objectReferenceValue =
            footer?.Find("Observers")?.GetComponent<TextMeshProUGUI>();
        serialized.FindProperty("switchAttackersButton").objectReferenceValue =
            attackers?.Find("Switch Team Button")?.GetComponent<Button>();
        serialized.FindProperty("switchDefendersButton").objectReferenceValue =
            defenders?.Find("Switch Team Button")?.GetComponent<Button>();
        serialized.FindProperty("startMatchButton").objectReferenceValue =
            footer?.Find("Start Match Button")?.GetComponent<Button>();
        serialized.FindProperty("preMatchLeaveButton").objectReferenceValue =
            footer?.Find("Leave Button")?.GetComponent<Button>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        foreach (MatchLobbyUI candidate in Object.FindObjectsByType<MatchLobbyUI>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            candidate.enabled = candidate == primary;
            EditorUtility.SetDirty(candidate);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[LAN UI] Lobby references repaired; duplicate controller disabled.");
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
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.type = UnityEngine.UI.Image.Type.Sliced;
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

    private static Button CreateButton(
        Transform parent, string name, string label,
        Color color, Vector2 position, Vector2 size)
    {
        GameObject item = CreateImage(name, parent, color, position, size);
        Button button = item.AddComponent<Button>();
        button.targetGraphic = item.GetComponent<UnityEngine.UI.Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Hex("C8EFFF", 1f);
        colors.pressedColor = Accent;
        colors.selectedColor = Hex("A7E4F6", 1f);
        colors.disabledColor = Hex("465566", 0.55f);
        button.colors = colors;
        TextMeshProUGUI text = CreateText("Label", item.transform, label,
            14f, Text, TextAlignmentOptions.Center, Vector2.zero,
            size - new Vector2(20f, 10f));
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static Color Hex(string value, float alpha)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        color.a = alpha;
        return color;
    }
}
#endif
