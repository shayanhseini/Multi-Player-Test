#if UNITY_EDITOR
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[InitializeOnLoad]
public static class LanEditableSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Conference Room Scene.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/VR Network Player.prefab";
    private const int GunLayerMask = 1 << 1;
    private const int GrenadeLayerMask = 1 << 2;

    static LanEditableSceneSetup() => EditorApplication.delayCall += UpgradeIfNeeded;

    [MenuItem("Tools/LAN Shooter/Create Editable Scene Setup")]
    public static void BuildAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogWarning($"Open {ScenePath} before building the LAN setup.");
            return;
        }

        CleanupLegacyScene();
        ConfigureInteractionLayers();
        BuildWorldObjects();
        BuildLobbyUI();
        BuildPlayerNameplate();
        BuildPlayerHolsters();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[LAN Shooter] Editable scene setup is ready in the Hierarchy and Inspector.");
    }

    private static void BuildWorldObjects()
    {
        GameObject systems = GameObject.Find("LAN Shooter Systems") ??
                             GameObject.Find("LAN Game Systems") ??
                             new GameObject("LAN Shooter Systems");
        systems.name = "LAN Shooter Systems";

        LanVoiceChat voice = systems.GetComponent<LanVoiceChat>();
        if (voice == null)
            voice = systems.AddComponent<LanVoiceChat>();
        _ = voice;

        NetworkObject systemsNetworkObject = systems.GetComponent<NetworkObject>();
        if (systemsNetworkObject == null)
            systemsNetworkObject = systems.AddComponent<NetworkObject>();
        _ = systemsNetworkObject;

        Transform attackersStart = FindOrCreateRoot(
            "Attackers Match Start", new Vector3(-3f, 0f, 0f));
        Transform defendersStart = FindOrCreateRoot(
            "Defenders Match Start", new Vector3(3f, 0f, 0f));
        LanMatchController matchController = systems.GetComponent<LanMatchController>();
        if (matchController == null)
            matchController = systems.AddComponent<LanMatchController>();
        SerializedObject matchSerialized = new(matchController);
        matchSerialized.FindProperty("attackersStartPoint").objectReferenceValue = attackersStart;
        matchSerialized.FindProperty("defendersStartPoint").objectReferenceValue = defendersStart;
        matchSerialized.ApplyModifiedPropertiesWithoutUndo();

        Transform[] allTransforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        GameObject existingFlag = null;
        foreach (Transform candidate in allTransforms)
        {
            if (candidate.name != "Network Flag" && candidate.name != "Flag")
                continue;
            if (existingFlag == null || candidate.GetComponent<NetworkFlag>() != null)
                existingFlag = candidate.gameObject;
        }
        foreach (Transform candidate in allTransforms)
        {
            if ((candidate.name == "Network Flag" || candidate.name == "Flag") &&
                candidate.gameObject != existingFlag)
                Object.DestroyImmediate(candidate.gameObject);
        }
        Transform flagRoot = existingFlag != null
            ? existingFlag.transform
            : FindOrCreateRoot("Network Flag", new Vector3(0f, 1f, 0f));
        flagRoot.name = "Network Flag";
        if (flagRoot.childCount == 0)
            CreateFlagVisual(flagRoot);

        Transform home = systems.transform.Find("Flag Home");
        if (home == null)
        {
            home = new GameObject("Flag Home").transform;
            home.SetParent(systems.transform, false);
            home.SetPositionAndRotation(flagRoot.position, flagRoot.rotation);
        }

        Rigidbody body = flagRoot.GetComponent<Rigidbody>();
        if (body == null)
            body = flagRoot.gameObject.AddComponent<Rigidbody>();
        body.mass = 1f;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        CapsuleCollider collider = flagRoot.GetComponent<CapsuleCollider>();
        if (collider == null)
            collider = flagRoot.gameObject.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0.2f, 0.75f, 0f);
        collider.height = 1.6f;
        collider.radius = 0.18f;
        NetworkObject networkObject = flagRoot.GetComponent<NetworkObject>();
        if (networkObject == null)
            networkObject = flagRoot.gameObject.AddComponent<NetworkObject>();
        _ = networkObject;
        OwnerNetworkTransform networkTransform = flagRoot.GetComponent<OwnerNetworkTransform>();
        if (networkTransform == null)
            networkTransform = flagRoot.gameObject.AddComponent<OwnerNetworkTransform>();
        _ = networkTransform;
        XRGrabInteractable grab = flagRoot.GetComponent<XRGrabInteractable>();
        if (grab == null)
            grab = flagRoot.gameObject.AddComponent<XRGrabInteractable>();
        grab.interactionLayers = 1;
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.throwOnDetach = true;
        Transform attach = flagRoot.Find("Attach Point");
        if (attach == null)
        {
            attach = new GameObject("Attach Point").transform;
            attach.SetParent(flagRoot, false);
            attach.localPosition = new Vector3(0.15f, 0.75f, 0f);
        }
        grab.attachTransform = attach;
        NetworkFlag networkFlag = flagRoot.GetComponent<NetworkFlag>();
        if (networkFlag == null)
            networkFlag = flagRoot.gameObject.AddComponent<NetworkFlag>();
        SerializedObject flagSerialized = new(networkFlag);
        flagSerialized.FindProperty("grabInteractable").objectReferenceValue = grab;
        flagSerialized.FindProperty("flagBody").objectReferenceValue = body;
        flagSerialized.FindProperty("homePoint").objectReferenceValue = home;
        flagSerialized.ApplyModifiedPropertiesWithoutUndo();

        Transform escape = FindOrCreateRoot("Escape Zone", new Vector3(5f, 0.05f, 0f));
        if (escape.childCount == 0)
            CreateEscapeVisual(escape);

        LanMatchObjective objective = systems.GetComponent<LanMatchObjective>();
        if (objective == null)
            objective = systems.AddComponent<LanMatchObjective>();
        SerializedObject serialized = new(objective);
        serialized.FindProperty("flag").objectReferenceValue = networkFlag;
        serialized.FindProperty("escapeZone").objectReferenceValue = escape;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SceneGunPickup gun = Object.FindFirstObjectByType<SceneGunPickup>();
        if (gun != null)
        {
            gun.gameObject.name = "Shared Gun Pickup";
            XRGrabInteractable gunGrab = gun.GetComponentInChildren<XRGrabInteractable>(true);
            if (gunGrab != null)
                gunGrab.interactionLayers = 1 | GunLayerMask;
        }
        EditorUtility.SetDirty(systems);
    }

    private static void UpgradeIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;
        Transform[] transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool hasLegacyManager = System.Array.Exists(
            transforms, item => item.name == "Presentation Manager" || item.name == "WhiteBoard");
        GameObject environmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Env.prefab");
        bool environmentHasWhiteboard = environmentPrefab != null && System.Array.Exists(
            environmentPrefab.GetComponentsInChildren<Transform>(true),
            item => item.name == "WhiteBoard");
        NetworkFlag flag = Object.FindFirstObjectByType<NetworkFlag>(FindObjectsInactive.Include);
        if (flag == null || hasLegacyManager || environmentHasWhiteboard ||
            Object.FindFirstObjectByType<LanMatchController>(FindObjectsInactive.Include) == null ||
            GameObject.Find("World Scoreboard Canvas") == null)
            BuildAll();
    }

    private static void CleanupLegacyScene()
    {
        foreach (Transform item in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (item.name == "Presentation Manager")
                Object.DestroyImmediate(item.gameObject);
        }

        RemoveLegacyComponentsFromRoots(SceneManager.GetActiveScene().GetRootGameObjects());
        CleanupLegacyPrefab("Assets/Prefabs/Env.prefab");
        AssetDatabase.DeleteAsset("Assets/Prefabs/Board.prefab");
    }

    private static readonly string[] LegacyComponentNames =
    {
        "PresentationState", "PresentationUI", "NetworkUIPermission",
        "NetworkInteractionPermission", "InteractionPermission",
        "WhiteBoard", "MarkerColorUI", "MarkerColorController",
        "MarkerColorButton", "VRColorPicker", "PresentationDisplay"
    };

    private static void RemoveLegacyComponentsFromRoots(GameObject[] roots)
    {
        System.Collections.Generic.List<GameObject> legacyObjects = new();
        foreach (GameObject root in roots)
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            if (item != root.transform && item.name == "WhiteBoard")
                legacyObjects.Add(item.gameObject);
        }
        foreach (GameObject legacyObject in legacyObjects)
            Object.DestroyImmediate(legacyObject, true);

        foreach (GameObject root in roots)
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null && System.Array.IndexOf(
                    LegacyComponentNames, behaviour.GetType().Name) >= 0)
                Object.DestroyImmediate(behaviour, true);
        }
    }

    private static void CleanupLegacyPrefab(string path)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
        if (prefabRoot == null)
            return;
        try
        {
            RemoveLegacyComponentsFromRoots(new[] { prefabRoot });
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ConfigureInteractionLayers()
    {
        Object settings = AssetDatabase.LoadMainAssetAtPath(
            "Assets/XRI/Settings/Resources/InteractionLayerSettings.asset");
        if (settings == null)
            return;
        SerializedObject serialized = new(settings);
        SerializedProperty names = serialized.FindProperty("m_LayerNames");
        names.GetArrayElementAtIndex(1).stringValue = "Gun";
        names.GetArrayElementAtIndex(2).stringValue = "Grenade";
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
    }

    private static Transform FindOrCreateRoot(string name, Vector3 position)
    {
        GameObject found = GameObject.Find(name);
        if (found != null)
            return found.transform;
        GameObject created = new(name);
        created.transform.position = position;
        return created.transform;
    }

    private static void CreateFlagVisual(Transform root)
    {
        Material poleMaterial = GetOrCreateMaterial(
            "Assets/Materials/LAN Flag Pole.mat", new Color(0.12f, 0.12f, 0.14f));
        Material flagMaterial = GetOrCreateMaterial(
            "Assets/Materials/LAN Flag Cloth.mat", new Color(1f, 0.72f, 0.05f));

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.SetParent(root, false);
        pole.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        pole.transform.localScale = new Vector3(0.035f, 0.75f, 0.035f);
        Object.DestroyImmediate(pole.GetComponent<Collider>());
        pole.GetComponent<Renderer>().sharedMaterial = poleMaterial;

        GameObject cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cloth.name = "Flag Visual";
        cloth.transform.SetParent(root, false);
        cloth.transform.localPosition = new Vector3(0.3f, 1.25f, 0f);
        cloth.transform.localScale = new Vector3(0.6f, 0.35f, 0.03f);
        Object.DestroyImmediate(cloth.GetComponent<Collider>());
        cloth.GetComponent<Renderer>().sharedMaterial = flagMaterial;
    }

    private static void CreateEscapeVisual(Transform root)
    {
        Material material = GetOrCreateMaterial(
            "Assets/Materials/LAN Escape Zone.mat", new Color(0.1f, 0.9f, 0.25f, 0.5f));
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Escape Zone Visual";
        visual.transform.SetParent(root, false);
        visual.transform.localScale = new Vector3(4f, 0.025f, 4f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                        Shader.Find("Standard");
        material = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void BuildLobbyUI()
    {
        UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("[LAN Shooter] UIManager was not found in the active scene.");
            return;
        }

        Transform existing = uiManager.transform.Find("Match Lobby UI");
        if (existing != null)
        {
            UpgradeLobbyUI(uiManager, existing);
            return;
        }

        GameObject root = new("Match Lobby UI", typeof(RectTransform), typeof(MatchLobbyUI));
        root.transform.SetParent(uiManager.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject selection = CreatePanel(root.transform, "Team Selection Panel",
            new Vector2(850f, 480f), Vector2.zero);
        CreateLabel(selection.transform, "JOIN MATCH", 44f,
            new Vector2(0f, 180f), new Vector2(700f, 60f));
        CreateLabel(selection.transform, "Enter your name and choose a team", 25f,
            new Vector2(0f, 125f), new Vector2(700f, 45f));
        TMP_InputField input = CreateInput(selection.transform,
            new Vector2(0f, 65f), new Vector2(520f, 62f));
        input.text = "Player";
        Button attackers = CreateButton(selection.transform, "Attackers Button", "ATTACKERS",
            new Color(0.7f, 0.12f, 0.1f), new Vector2(-210f, -25f));
        Button defenders = CreateButton(selection.transform, "Defenders Button", "DEFENDERS",
            new Color(0.08f, 0.3f, 0.72f), new Vector2(210f, -25f));
        Button god = CreateButton(selection.transform, "Observer Button", "VR OBSERVER",
            new Color(0.65f, 0.5f, 0.05f), new Vector2(0f, -115f));
        TextMeshProUGUI teamStatus = CreateLabel(selection.transform, string.Empty, 24f,
            new Vector2(0f, -190f), new Vector2(760f, 45f));

        GameObject scoreboard = CreatePanel(root.transform, "Scoreboard Panel",
            new Vector2(570f, 470f), Vector2.zero);
        TextMeshProUGUI scoreboardText = CreateLabel(scoreboard.transform, string.Empty, 22f,
            Vector2.zero, new Vector2(520f, 420f));
        scoreboardText.alignment = TextAlignmentOptions.TopLeft;

        MatchLobbyUI controller = root.GetComponent<MatchLobbyUI>();
        SerializedObject serialized = new(controller);
        serialized.FindProperty("selectionPanel").objectReferenceValue = selection;
        serialized.FindProperty("scoreboardPanel").objectReferenceValue = scoreboard;
        serialized.FindProperty("nameInput").objectReferenceValue = input;
        serialized.FindProperty("teamStatus").objectReferenceValue = teamStatus;
        serialized.FindProperty("attackersButton").objectReferenceValue = attackers;
        serialized.FindProperty("defendersButton").objectReferenceValue = defenders;
        serialized.FindProperty("observerButton").objectReferenceValue = god;
        serialized.FindProperty("scoreboardText").objectReferenceValue = scoreboardText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        selection.SetActive(false);
        scoreboard.SetActive(false);
        UpgradeLobbyUI(uiManager, root.transform);
    }

    private static void UpgradeLobbyUI(UIManager uiManager, Transform lobbyRoot)
    {
        RectTransform lobbyRect = lobbyRoot.GetComponent<RectTransform>();
        lobbyRect.anchorMin = Vector2.zero;
        lobbyRect.anchorMax = Vector2.one;
        lobbyRect.offsetMin = Vector2.zero;
        lobbyRect.offsetMax = Vector2.zero;
        lobbyRoot.SetAsLastSibling();

        MatchLobbyUI controller = lobbyRoot.GetComponent<MatchLobbyUI>();
        if (controller == null)
            return;
        SerializedObject controllerSerialized = new(controller);
        GameObject selection = controllerSerialized.FindProperty("selectionPanel")
            .objectReferenceValue as GameObject;
        GameObject scoreboard = controllerSerialized.FindProperty("scoreboardPanel")
            .objectReferenceValue as GameObject;

        if (selection != null)
        {
            RectTransform selectionRect = selection.GetComponent<RectTransform>();
            selectionRect.anchorMin = selectionRect.anchorMax = new Vector2(0.5f, 0.5f);
            selectionRect.pivot = new Vector2(0.5f, 0.5f);
            selectionRect.sizeDelta = new Vector2(760f, 720f);
            selectionRect.anchoredPosition = new Vector2(420f, 0f);
            UnityEngine.UI.Image background = selection.GetComponent<UnityEngine.UI.Image>();
            if (background != null)
                background.color = new Color(0.025f, 0.035f, 0.065f, 0.97f);
        }

        ShiftSessionContent(uiManager.transform.Find("SessionCreated"));
        ShiftSessionContent(uiManager.transform.Find("JoinedSession"));

        GameObject worldCanvas = GameObject.Find("World Scoreboard Canvas");
        if (worldCanvas == null)
        {
            worldCanvas = new GameObject(
                "World Scoreboard Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster));
        }
        worldCanvas.layer = 5;
        Canvas canvas = worldCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 30;
        CanvasScaler scaler = worldCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 10f;
        RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1000f, 700f);
        canvasRect.localScale = Vector3.one * 0.0015f;

        if (scoreboard != null)
        {
            scoreboard.transform.SetParent(worldCanvas.transform, false);
            RectTransform scoreboardRect = scoreboard.GetComponent<RectTransform>();
            scoreboardRect.anchorMin = Vector2.zero;
            scoreboardRect.anchorMax = Vector2.one;
            scoreboardRect.offsetMin = Vector2.zero;
            scoreboardRect.offsetMax = Vector2.zero;
            scoreboard.name = "Scoreboard Panel";

            if (scoreboard.transform.Find("Close Hint") == null)
            {
                TextMeshProUGUI hint = CreateLabel(
                    scoreboard.transform, "A  •  CLOSE SCOREBOARD", 21f,
                    new Vector2(0f, -310f), new Vector2(500f, 35f));
                hint.name = "Close Hint";
                hint.color = new Color(1f, 1f, 1f, 0.65f);
            }
            scoreboard.SetActive(false);
        }

        controllerSerialized.FindProperty("connectionUI").objectReferenceValue = uiManager;
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void ShiftSessionContent(Transform sessionPanel)
    {
        if (sessionPanel == null)
            return;
        Transform content = sessionPanel.Find("Session Information");
        if (content == null)
        {
            content = new GameObject("Session Information", typeof(RectTransform)).transform;
            content.SetParent(sessionPanel, false);
            RectTransform contentRect = (RectTransform)content;
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            System.Collections.Generic.List<Transform> children = new();
            for (int i = 0; i < sessionPanel.childCount; i++)
            {
                Transform child = sessionPanel.GetChild(i);
                if (child != content)
                    children.Add(child);
            }
            foreach (Transform child in children)
                child.SetParent(content, true);
        }
        ((RectTransform)content).anchoredPosition = new Vector2(-420f, 0f);
    }

    private static GameObject CreatePanel(
        Transform parent, string name, Vector2 size, Vector2 position)
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        panel.GetComponent<UnityEngine.UI.Image>().color =
            new Color(0.025f, 0.035f, 0.065f, 0.96f);
        return panel;
    }

    private static TextMeshProUGUI CreateLabel(
        Transform parent, string value, float fontSize, Vector2 position, Vector2 size)
    {
        GameObject item = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        TextMeshProUGUI label = item.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(
        Transform parent, string name, string title, Color color, Vector2 position)
    {
        GameObject item = new(name, typeof(RectTransform),
            typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(310f, 70f);
        rect.anchoredPosition = position;
        item.GetComponent<UnityEngine.UI.Image>().color = color;
        CreateLabel(item.transform, title, 27f, Vector2.zero, rect.sizeDelta);
        return item.GetComponent<UnityEngine.UI.Button>();
    }

    private static TMP_InputField CreateInput(
        Transform parent, Vector2 position, Vector2 size)
    {
        GameObject root = new("Player Name Input", typeof(RectTransform),
            typeof(UnityEngine.UI.Image), typeof(TMP_InputField), typeof(KeyboardInputField));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = size;
        rootRect.anchoredPosition = position;
        root.GetComponent<UnityEngine.UI.Image>().color = new Color(1f, 1f, 1f, 0.14f);

        GameObject viewport = new("Text Area", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(root.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(18f, 5f);
        viewportRect.offsetMax = new Vector2(-18f, -5f);

        TextMeshProUGUI text = CreateLabel(viewport.transform, string.Empty, 28f,
            Vector2.zero, size - new Vector2(36f, 10f));
        text.alignment = TextAlignmentOptions.MidlineLeft;
        TextMeshProUGUI placeholder = CreateLabel(viewport.transform, "Player name", 28f,
            Vector2.zero, size - new Vector2(36f, 10f));
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        placeholder.color = new Color(1f, 1f, 1f, 0.45f);

        TMP_InputField input = root.GetComponent<TMP_InputField>();
        input.textViewport = viewportRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 24;
        input.lineType = TMP_InputField.LineType.SingleLine;
        return input;
    }

    private static void BuildPlayerNameplate()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            VRNetworkPlayer player = prefabRoot.GetComponent<VRNetworkPlayer>();
            if (player == null || player.head == null ||
                player.head.Find("Player Nameplate") != null)
                return;

            GameObject root = new("Player Nameplate", typeof(RectTransform),
                typeof(TextMeshPro), typeof(PlayerNameplate));
            root.transform.SetParent(player.head, false);
            root.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            TextMeshPro label = root.GetComponent<TextMeshPro>();
            label.font = TMP_Settings.defaultFontAsset;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 2.2f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 0.5f;
            label.fontSizeMax = 2.2f;
            label.rectTransform.sizeDelta = new Vector2(2.4f, 0.45f);

            SerializedObject serialized = new(root.GetComponent<PlayerNameplate>());
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void BuildPlayerHolsters()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            VRNetworkPlayer player = prefabRoot.GetComponent<VRNetworkPlayer>();
            if (player == null || player.head == null)
                return;

            Transform holsters = player.head.Find("Player Holsters");
            if (holsters == null)
            {
                holsters = new GameObject("Player Holsters").transform;
                holsters.SetParent(player.head, false);
                holsters.localPosition = new Vector3(0f, -0.55f, 0.05f);
                holsters.localRotation = Quaternion.identity;
            }

            XRSocketInteractor gunSocket = CreateSocket(
                holsters, "Gun Holster Socket", new Vector3(0.3f, 0f, 0f), GunLayerMask);
            XRSocketInteractor grenadeSocket = CreateSocket(
                holsters, "Grenade Holster Socket", new Vector3(-0.3f, 0f, 0f), GrenadeLayerMask);

            Transform grenadeTransform = grenadeSocket.transform.Find("Reusable Grenade");
            GameObject grenade;
            if (grenadeTransform == null)
            {
                grenade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                grenade.name = "Reusable Grenade";
                grenade.transform.SetParent(grenadeSocket.transform, false);
                grenade.transform.localScale = Vector3.one * 0.12f;
                grenade.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
                    "Assets/Materials/LAN Grenade.mat", new Color(0.12f, 0.25f, 0.1f));
            }
            else
            {
                grenade = grenadeTransform.gameObject;
            }

            Rigidbody grenadeBody = grenade.GetComponent<Rigidbody>();
            if (grenadeBody == null)
                grenadeBody = grenade.AddComponent<Rigidbody>();
            grenadeBody.mass = 0.35f;
            grenadeBody.useGravity = true;
            grenadeBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            XRGrabInteractable grenadeGrab = grenade.GetComponent<XRGrabInteractable>();
            if (grenadeGrab == null)
                grenadeGrab = grenade.AddComponent<XRGrabInteractable>();
            grenadeGrab.interactionLayers = 1 | GrenadeLayerMask;
            grenadeGrab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grenadeGrab.throwOnDetach = false;

            ReusableGrenadePickup reusable = grenade.GetComponent<ReusableGrenadePickup>();
            if (reusable == null)
                reusable = grenade.AddComponent<ReusableGrenadePickup>();
            SerializedObject reusableSerialized = new(reusable);
            reusableSerialized.FindProperty("grabInteractable").objectReferenceValue = grenadeGrab;
            reusableSerialized.FindProperty("holsterPoint").objectReferenceValue =
                grenadeSocket.attachTransform != null
                    ? grenadeSocket.attachTransform
                    : grenadeSocket.transform;
            reusableSerialized.ApplyModifiedPropertiesWithoutUndo();

            PlayerHolsterSockets controller = prefabRoot.GetComponent<PlayerHolsterSockets>();
            if (controller == null)
                controller = prefabRoot.AddComponent<PlayerHolsterSockets>();
            SerializedObject controllerSerialized = new(controller);
            controllerSerialized.FindProperty("gunSocket").objectReferenceValue = gunSocket;
            controllerSerialized.FindProperty("grenadeSocket").objectReferenceValue = grenadeSocket;
            controllerSerialized.FindProperty("reusableGrenade").objectReferenceValue = grenadeGrab;
            controllerSerialized.FindProperty("trackingReference").objectReferenceValue = player.head;
            controllerSerialized.FindProperty("holsterRoot").objectReferenceValue = holsters;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static XRSocketInteractor CreateSocket(
        Transform parent, string name, Vector3 localPosition, int interactionMask)
    {
        Transform existing = parent.Find(name);
        GameObject socketObject;
        if (existing == null)
        {
            socketObject = new GameObject(name);
            socketObject.transform.SetParent(parent, false);
        }
        else
        {
            socketObject = existing.gameObject;
        }

        socketObject.transform.localPosition = localPosition;
        socketObject.transform.localRotation = Quaternion.identity;
        XRSocketInteractor socket = socketObject.GetComponent<XRSocketInteractor>();
        if (socket == null)
            socket = socketObject.AddComponent<XRSocketInteractor>();
        socket.interactionLayers = interactionMask;
        socket.socketActive = true;
        socket.showInteractableHoverMeshes = true;
        return socket;
    }
}
#endif
