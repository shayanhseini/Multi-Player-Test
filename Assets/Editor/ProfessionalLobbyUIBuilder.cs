#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ProfessionalLobbyUIBuilder
{
    private const string ScenePath = "Assets/Scenes/Conference Room Scene.unity";
    private const string RootName = "Professional LAN Lobby";

    private static readonly Color Backdrop = Hex("07101D", 0.98f);
    private static readonly Color Surface = Hex("0E1B2B", 1f);
    private static readonly Color SurfaceRaised = Hex("17283D", 1f);
    private static readonly Color Accent = Hex("35C8F5", 1f);
    private static readonly Color Primary = Hex("2474E5", 1f);
    private static readonly Color Danger = Hex("D64550", 1f);
    private static readonly Color Warning = Hex("D9A514", 1f);
    private static readonly Color Text = Hex("F3F7FC", 1f);
    private static readonly Color Muted = Hex("8FA4BA", 1f);

    static ProfessionalLobbyUIBuilder() => EditorApplication.delayCall += BuildIfNeeded;

    [MenuItem("Tools/LAN Shooter/Rebuild Professional Lobby UI")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;

        UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("[LAN UI] UIManager was not found.");
            return;
        }

        Transform existingRoot = uiManager.transform.Find(RootName);
        if (existingRoot != null)
            Object.DestroyImmediate(existingRoot.gameObject);

        SerializedObject oldUI = new(uiManager);
        List<GameObject> oldPages = ReadOldPages(oldUI);
        MatchLobbyUI oldLobby = Object.FindFirstObjectByType<MatchLobbyUI>(
            FindObjectsInactive.Include);
        GameObject scoreboard = null;
        TextMeshProUGUI scoreboardText = null;
        if (oldLobby != null)
        {
            SerializedObject oldLobbySerialized = new(oldLobby);
            scoreboard = oldLobbySerialized.FindProperty("scoreboardPanel")
                .objectReferenceValue as GameObject;
            scoreboardText = oldLobbySerialized.FindProperty("scoreboardText")
                .objectReferenceValue as TextMeshProUGUI;
        }
        if (scoreboard == null)
        {
            GameObject worldScoreboard = GameObject.Find("World Scoreboard Canvas");
            Transform scoreboardTransform = worldScoreboard != null
                ? worldScoreboard.transform.Find("Scoreboard Panel")
                : null;
            scoreboard = scoreboardTransform != null
                ? scoreboardTransform.gameObject
                : null;
            if (scoreboard != null)
            {
                foreach (TextMeshProUGUI candidate in
                         scoreboard.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (candidate.name != "Close Hint")
                    {
                        scoreboardText = candidate;
                        break;
                    }
            }
        }

        GameObject root = CreateUIObject(RootName, uiManager.transform);
        Stretch(root.GetComponent<RectTransform>());
        CreateImage("Backdrop", root.transform, Backdrop, Vector2.zero,
            new Vector2(1400f, 820f));
        CreateHeader(root.transform);

        GameObject main = CreatePage("Main Menu", root.transform);
        BuildMainMenu(main.transform, out Button hostButton, out Button guestButton);

        GameObject hostLobby = CreatePage("Host Lobby", root.transform);
        BuildRoomPage(hostLobby.transform, true,
            out TextMeshProUGUI sessionCode, out Button hostDisconnect);

        GameObject guestLobby = CreatePage("Guest Lobby", root.transform);
        BuildRoomPage(guestLobby.transform, false,
            out _, out Button guestDisconnect);

        GameObject connecting = CreatePage("Connecting Overlay", root.transform);
        BuildConnecting(connecting.transform);

        GameObject teamPanel = BuildTeamSelection(root.transform,
            out TMP_InputField nameInput,
            out TextMeshProUGUI teamStatus,
            out Button attackers,
            out Button defenders,
            out Button observer);

        MatchLobbyUI lobbyController = root.AddComponent<MatchLobbyUI>();
        SerializedObject lobbySerialized = new(lobbyController);
        lobbySerialized.FindProperty("selectionPanel").objectReferenceValue = teamPanel;
        lobbySerialized.FindProperty("scoreboardPanel").objectReferenceValue = scoreboard;
        lobbySerialized.FindProperty("connectionUI").objectReferenceValue = uiManager;
        lobbySerialized.FindProperty("nameInput").objectReferenceValue = nameInput;
        lobbySerialized.FindProperty("teamStatus").objectReferenceValue = teamStatus;
        lobbySerialized.FindProperty("attackersButton").objectReferenceValue = attackers;
        lobbySerialized.FindProperty("defendersButton").objectReferenceValue = defenders;
        lobbySerialized.FindProperty("observerButton").objectReferenceValue = observer;
        lobbySerialized.FindProperty("scoreboardText").objectReferenceValue = scoreboardText;
        lobbySerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject uiSerialized = new(uiManager);
        uiSerialized.FindProperty("mainMenu").objectReferenceValue = main;
        uiSerialized.FindProperty("joinSession").objectReferenceValue = main;
        uiSerialized.FindProperty("sessionCreated").objectReferenceValue = hostLobby;
        uiSerialized.FindProperty("joinedSession").objectReferenceValue = guestLobby;
        uiSerialized.FindProperty("connecting").objectReferenceValue = connecting;
        uiSerialized.FindProperty("hostButton").objectReferenceValue = hostButton;
        uiSerialized.FindProperty("clientButton").objectReferenceValue = guestButton;
        uiSerialized.FindProperty("sessionCodeInput").objectReferenceValue = null;
        uiSerialized.FindProperty("joinButton").objectReferenceValue = null;
        uiSerialized.FindProperty("joinSessionBackButton").objectReferenceValue = null;
        uiSerialized.FindProperty("disconnectButton").objectReferenceValue = guestDisconnect;
        uiSerialized.FindProperty("hostDisconnectButton").objectReferenceValue = hostDisconnect;
        uiSerialized.FindProperty("sessionCreatedCodeText").objectReferenceValue = sessionCode;
        uiSerialized.ApplyModifiedPropertiesWithoutUndo();

        foreach (GameObject oldPage in oldPages)
            if (oldPage != null && !oldPage.transform.IsChildOf(root.transform))
                Object.DestroyImmediate(oldPage);
        if (oldLobby != null && oldLobby != lobbyController &&
            !oldLobby.transform.IsChildOf(root.transform))
            Object.DestroyImmediate(oldLobby.gameObject);

        RestyleKeyboard();
        main.SetActive(true);
        hostLobby.SetActive(false);
        guestLobby.SetActive(false);
        connecting.SetActive(false);
        teamPanel.SetActive(false);

        EditorUtility.SetDirty(uiManager);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[LAN UI] Professional LAN lobby dashboard was created.");
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;
        UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager != null && uiManager.transform.Find(RootName) == null)
            Rebuild();
    }

    private static List<GameObject> ReadOldPages(SerializedObject ui)
    {
        List<GameObject> pages = new();
        foreach (string property in new[]
                 { "mainMenu", "joinSession", "sessionCreated", "joinedSession", "connecting" })
        {
            GameObject page = ui.FindProperty(property).objectReferenceValue as GameObject;
            if (page != null && !pages.Contains(page))
                pages.Add(page);
        }
        return pages;
    }

    private static void CreateHeader(Transform parent)
    {
        GameObject header = CreateImage("Header", parent, Surface,
            new Vector2(0f, 340f), new Vector2(1400f, 140f));
        CreateImage("Accent Line", header.transform, Accent,
            new Vector2(-650f, 0f), new Vector2(6f, 72f));
        TextMeshProUGUI title = CreateText("Title", header.transform,
            "LAN // COMBAT PROTOCOL", 34f, Text, TextAlignmentOptions.Left,
            new Vector2(-80f, 18f), new Vector2(1120f, 48f));
        title.fontStyle = FontStyles.Bold;
        CreateText("Subtitle", header.transform,
            "LOCAL SESSION  •  6V6 TACTICAL MATCH", 17f, Muted,
            TextAlignmentOptions.Left, new Vector2(-80f, -25f),
            new Vector2(1120f, 30f));
        GameObject chip = CreateImage("Network Status", header.transform,
            Hex("123A3B", 1f), new Vector2(548f, 0f), new Vector2(210f, 54f));
        CreateText("Label", chip.transform, "●  LAN READY", 17f,
            Hex("6DE3B3", 1f), TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(190f, 38f));
    }

    private static void BuildMainMenu(
        Transform page, out Button hostButton, out Button guestButton)
    {
        GameObject card = CreateImage("Session Actions", page, Surface,
            new Vector2(0f, -35f), new Vector2(1040f, 540f));
        CreateText("Eyebrow", card.transform, "MULTIPLAYER / LOCAL NETWORK", 17f,
            Accent, TextAlignmentOptions.Center, new Vector2(0f, 190f),
            new Vector2(800f, 30f));
        TextMeshProUGUI heading = CreateText("Heading", card.transform,
            "ENTER THE BATTLESPACE", 42f, Text, TextAlignmentOptions.Center,
            new Vector2(0f, 125f), new Vector2(900f, 60f));
        heading.fontStyle = FontStyles.Bold;
        CreateText("Description", card.transform,
            "Create a room on this network or automatically find an active host.",
            20f, Muted, TextAlignmentOptions.Center,
            new Vector2(0f, 70f), new Vector2(850f, 44f));
        hostButton = CreateButton(card.transform, "Host Button",
            "HOST LAN MATCH", "Create a new 12-player room", Primary,
            new Vector2(0f, -25f), new Vector2(680f, 92f));
        guestButton = CreateButton(card.transform, "Guest Button",
            "FIND LAN MATCH", "Discover a host on the same network", SurfaceRaised,
            new Vector2(0f, -135f), new Vector2(680f, 92f));
        CreateText("Footer Hint", card.transform,
            "No internet connection required", 16f, Muted,
            TextAlignmentOptions.Center, new Vector2(0f, -220f),
            new Vector2(700f, 30f));
    }

    private static void BuildRoomPage(
        Transform page, bool isHost,
        out TextMeshProUGUI sessionCode, out Button disconnect)
    {
        GameObject card = CreateImage("Room Information", page, Surface,
            new Vector2(-330f, -35f), new Vector2(600f, 560f));
        CreateText("Eyebrow", card.transform,
            isHost ? "HOST SESSION" : "ACTIVE SESSION", 17f, Accent,
            TextAlignmentOptions.Left, new Vector2(0f, 210f),
            new Vector2(500f, 30f));
        TextMeshProUGUI heading = CreateText("Heading", card.transform,
            isHost ? "ROOM ONLINE" : "CONNECTION READY", 34f, Text,
            TextAlignmentOptions.Left, new Vector2(0f, 155f),
            new Vector2(500f, 55f));
        heading.fontStyle = FontStyles.Bold;
        CreateText("Description", card.transform,
            isHost
                ? "Share this address only when manual connection is needed."
                : "You are connected. Complete your profile to enter the match.",
            18f, Muted, TextAlignmentOptions.Left,
            new Vector2(0f, 100f), new Vector2(500f, 65f));
        GameObject addressCard = CreateImage("Session Address", card.transform,
            SurfaceRaised, new Vector2(0f, 15f), new Vector2(500f, 100f));
        CreateText("Caption", addressCard.transform,
            isHost ? "SESSION ADDRESS" : "NETWORK STATUS", 14f, Muted,
            TextAlignmentOptions.Left, new Vector2(0f, 22f),
            new Vector2(440f, 24f));
        sessionCode = CreateText("Value", addressCard.transform,
            isHost ? "Detecting local address…" : "●  CONNECTED TO LAN HOST",
            23f, isHost ? Text : Hex("6DE3B3", 1f),
            TextAlignmentOptions.Left, new Vector2(0f, -18f),
            new Vector2(440f, 36f));
        disconnect = CreateButton(card.transform, "Disconnect Button",
            "LEAVE SESSION", "Return to LAN menu", Danger,
            new Vector2(0f, -155f), new Vector2(500f, 82f));
        CreateText("Step", card.transform, "NEXT  →  SET NAME AND TEAM", 15f,
            Muted, TextAlignmentOptions.Left,
            new Vector2(0f, -225f), new Vector2(500f, 28f));
    }

    private static GameObject BuildTeamSelection(
        Transform parent,
        out TMP_InputField nameInput,
        out TextMeshProUGUI teamStatus,
        out Button attackers,
        out Button defenders,
        out Button observer)
    {
        GameObject card = CreateImage("Player Setup", parent, Surface,
            new Vector2(350f, -35f), new Vector2(700f, 560f));
        CreateText("Eyebrow", card.transform, "PLAYER PROFILE", 17f, Accent,
            TextAlignmentOptions.Left, new Vector2(0f, 215f),
            new Vector2(600f, 30f));
        TextMeshProUGUI heading = CreateText("Heading", card.transform,
            "CHOOSE YOUR SIDE", 32f, Text, TextAlignmentOptions.Left,
            new Vector2(0f, 168f), new Vector2(600f, 48f));
        heading.fontStyle = FontStyles.Bold;
        CreateText("Name Caption", card.transform, "DISPLAY NAME", 14f, Muted,
            TextAlignmentOptions.Left, new Vector2(0f, 112f),
            new Vector2(600f, 24f));
        nameInput = CreateInput(card.transform, new Vector2(0f, 67f),
            new Vector2(600f, 64f));
        attackers = CreateButton(card.transform, "Attackers Button",
            "ATTACKERS", "Capture the flag and reach extraction", Danger,
            new Vector2(-153f, -33f), new Vector2(294f, 88f));
        defenders = CreateButton(card.transform, "Defenders Button",
            "DEFENDERS", "Protect the objective", Primary,
            new Vector2(153f, -33f), new Vector2(294f, 88f));
        observer = CreateButton(card.transform, "Observer Button",
            "VR OBSERVER", "Host-only free-flight spectator", Warning,
            new Vector2(0f, -135f), new Vector2(600f, 78f));
        teamStatus = CreateText("Team Capacity", card.transform,
            "ATTACKERS  0/6        DEFENDERS  0/6", 17f, Muted,
            TextAlignmentOptions.Center, new Vector2(0f, -215f),
            new Vector2(600f, 32f));
        card.SetActive(false);
        return card;
    }

    private static void BuildConnecting(Transform page)
    {
        GameObject overlay = CreateImage("Overlay", page,
            Hex("050B13", 0.96f), new Vector2(0f, -35f), new Vector2(1400f, 680f));
        GameObject card = CreateImage("Status Card", overlay.transform, Surface,
            Vector2.zero, new Vector2(720f, 330f));
        CreateText("Signal", card.transform, "◌", 72f, Accent,
            TextAlignmentOptions.Center, new Vector2(0f, 72f),
            new Vector2(120f, 90f));
        TextMeshProUGUI heading = CreateText("Heading", card.transform,
            "SEARCHING LOCAL NETWORK", 30f, Text, TextAlignmentOptions.Center,
            new Vector2(0f, 5f), new Vector2(620f, 46f));
        heading.fontStyle = FontStyles.Bold;
        CreateText("Description", card.transform,
            "Keep this headset on the same Wi-Fi or LAN as the host.",
            18f, Muted, TextAlignmentOptions.Center,
            new Vector2(0f, -48f), new Vector2(620f, 42f));
        CreateText("Progress", card.transform, "●   ●   ●", 22f, Accent,
            TextAlignmentOptions.Center, new Vector2(0f, -105f),
            new Vector2(260f, 35f));
    }

    private static void RestyleKeyboard()
    {
        KeyboardManager manager = Object.FindFirstObjectByType<KeyboardManager>(
            FindObjectsInactive.Include);
        if (manager == null)
            return;
        SerializedObject serialized = new(manager);
        GameObject keyboardRoot = serialized.FindProperty("keyboardRoot")
            .objectReferenceValue as GameObject;
        if (keyboardRoot == null)
            return;
        UnityEngine.UI.Image background = keyboardRoot.GetComponent<UnityEngine.UI.Image>();
        if (background != null)
            background.color = Backdrop;
        foreach (Button button in keyboardRoot.GetComponentsInChildren<Button>(true))
        {
            UnityEngine.UI.Image image = button.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
                image.color = SurfaceRaised;
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = Text;
                label.fontStyle = FontStyles.Bold;
            }
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Hex("C4EFFF", 1f);
            colors.pressedColor = Accent;
            colors.selectedColor = Hex("9DDEF4", 1f);
            button.colors = colors;
        }
    }

    private static GameObject CreatePage(string name, Transform parent)
    {
        GameObject page = CreateUIObject(name, parent);
        Stretch(page.GetComponent<RectTransform>());
        return page;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject item = new(name, typeof(RectTransform));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        return item;
    }

    private static GameObject CreateImage(
        string name, Transform parent, Color color, Vector2 position, Vector2 size)
    {
        GameObject item = CreateUIObject(name, parent);
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
        string name, Transform parent, string value, float fontSize,
        Color color, TextAlignmentOptions alignment, Vector2 position, Vector2 size)
    {
        GameObject item = CreateUIObject(name, parent);
        TextMeshProUGUI text = item.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return text;
    }

    private static Button CreateButton(
        Transform parent, string name, string title, string subtitle,
        Color color, Vector2 position, Vector2 size)
    {
        GameObject item = CreateImage(name, parent, color, position, size);
        Button button = item.AddComponent<Button>();
        button.targetGraphic = item.GetComponent<UnityEngine.UI.Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Hex("C8EFFF", 1f);
        colors.pressedColor = Hex("8ECFE7", 1f);
        colors.selectedColor = Hex("B4E7F7", 1f);
        colors.disabledColor = Hex("556273", 0.55f);
        button.colors = colors;
        TextMeshProUGUI heading = CreateText("Title", item.transform, title,
            20f, Text, TextAlignmentOptions.Left,
            new Vector2(0f, subtitle.Length > 0 ? 14f : 0f),
            new Vector2(size.x - 60f, 32f));
        heading.fontStyle = FontStyles.Bold;
        if (subtitle.Length > 0)
            CreateText("Subtitle", item.transform, subtitle, 14f,
                Hex("D4DEEA", 0.88f), TextAlignmentOptions.Left,
                new Vector2(0f, -16f), new Vector2(size.x - 60f, 24f));
        return button;
    }

    private static TMP_InputField CreateInput(
        Transform parent, Vector2 position, Vector2 size)
    {
        GameObject root = CreateImage("Player Name Input", parent,
            SurfaceRaised, position, size);
        TMP_InputField input = root.AddComponent<TMP_InputField>();
        root.AddComponent<KeyboardInputField>();
        GameObject viewport = CreateUIObject("Text Area", root.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        viewportRect.offsetMin = new Vector2(22f, 5f);
        viewportRect.offsetMax = new Vector2(-22f, -5f);
        viewport.AddComponent<RectMask2D>();
        TextMeshProUGUI value = CreateText("Text", viewport.transform,
            string.Empty, 22f, Text, TextAlignmentOptions.MidlineLeft,
            Vector2.zero, size - new Vector2(44f, 10f));
        TextMeshProUGUI placeholder = CreateText("Placeholder", viewport.transform,
            "Enter callsign", 22f, Muted, TextAlignmentOptions.MidlineLeft,
            Vector2.zero, size - new Vector2(44f, 10f));
        input.textViewport = viewportRect;
        input.textComponent = value;
        input.placeholder = placeholder;
        input.text = "Player";
        input.characterLimit = 24;
        input.lineType = TMP_InputField.LineType.SingleLine;
        return input;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Color Hex(string value, float alpha)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        color.a = alpha;
        return color;
    }
}
#endif
