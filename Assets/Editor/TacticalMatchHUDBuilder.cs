#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TacticalMatchHUDBuilder
{
    private const string ScenePath = "Assets/Scenes/Conference Room Scene.unity";
    private const string FontPath = "Assets/Font/Orbitron/Orbitron-Regular SDF.asset";
    private const string AudioPath = "Assets/Audio/CC0/Tactical/";
    private const string RootName = "Tactical Match HUD";

    static TacticalMatchHUDBuilder() => EditorApplication.delayCall += BuildIfNeeded;

    [MenuItem("Tools/LAN Shooter/Build Tactical Match HUD")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath) return;
        VRRigReferences rig = Object.FindFirstObjectByType<VRRigReferences>(FindObjectsInactive.Include);
        if (rig == null || rig.head == null || rig.head.Find(RootName) != null) return;

        GameObject root = new(RootName, typeof(RectTransform), typeof(Canvas));
        root.layer = 5;
        root.transform.SetParent(rig.head, false);
        root.transform.localPosition = new Vector3(0f, 0f, 1.8f);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * 0.0012f;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 25;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(1200f, 700f);

        GameObject top = Image("Top Match Bar", root.transform,
            new Color(0.025f, 0.055f, 0.09f, 0.88f), new Vector2(0f, 285f), new Vector2(800f, 72f));
        TextMeshProUGUI timer = Text("Round Timer", top.transform, "R01   03:00", 24,
            Color.white, TextAlignmentOptions.Left, new Vector2(-245f, 0f), new Vector2(250f, 48f));
        TextMeshProUGUI score = Text("Team Score", top.transform,
            "ATTACKERS  0   :   0  DEFENDERS", 20, Color.white,
            TextAlignmentOptions.Right, new Vector2(125f, 0f), new Vector2(500f, 48f));

        TextMeshProUGUI announcement = Text("Announcement", root.transform, string.Empty, 32,
            new Color(1f, 0.78f, 0.22f), TextAlignmentOptions.Center,
            new Vector2(0f, 190f), new Vector2(900f, 62f));
        TextMeshProUGUI objective = Text("Objective", root.transform, string.Empty, 15,
            new Color(0.38f, 0.82f, 0.95f), TextAlignmentOptions.Center,
            new Vector2(0f, 145f), new Vector2(800f, 36f));
        TextMeshProUGUI feedback = Text("Combat Feedback", root.transform, string.Empty, 26,
            Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(420f, 52f));
        TextMeshProUGUI killFeed = Text("Kill Feed", root.transform, string.Empty, 16,
            Color.white, TextAlignmentOptions.TopRight,
            new Vector2(420f, 90f), new Vector2(330f, 220f));
        TextMeshProUGUI ammo = Text("Ammo", root.transform, "AMMO  12 / 12", 19,
            Color.white, TextAlignmentOptions.BottomRight,
            new Vector2(420f, -255f), new Vector2(320f, 44f));
        TextMeshProUGUI spectator = Text("Spectator Status", root.transform, string.Empty, 18,
            new Color(1f, 0.78f, 0.22f), TextAlignmentOptions.Bottom,
            new Vector2(0f, -275f), new Vector2(700f, 44f));

        TacticalMatchHUD hud = root.AddComponent<TacticalMatchHUD>();
        root.AddComponent<PositionOnlyHudFollower>();
        TacticalAudioFeedback audio = root.AddComponent<TacticalAudioFeedback>();
        ConfigureAudio(audio);
        SerializedObject so = new(hud);
        so.FindProperty("hudRoot").objectReferenceValue = root;
        so.FindProperty("roundTimerText").objectReferenceValue = timer;
        so.FindProperty("teamScoreText").objectReferenceValue = score;
        so.FindProperty("objectiveText").objectReferenceValue = objective;
        so.FindProperty("ammoText").objectReferenceValue = ammo;
        so.FindProperty("announcementText").objectReferenceValue = announcement;
        so.FindProperty("killFeedText").objectReferenceValue = killFeed;
        so.FindProperty("spectatorText").objectReferenceValue = spectator;
        so.FindProperty("feedbackText").objectReferenceValue = feedback;
        so.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(true);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[LAN UI] Tactical VR match HUD was created under the local headset.");
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating ||
            SceneManager.GetActiveScene().path != ScenePath) return;
        VRRigReferences rig = Object.FindFirstObjectByType<VRRigReferences>(FindObjectsInactive.Include);
        if (rig == null || rig.head == null) return;
        Transform existing = rig.head.Find(RootName);
        if (existing == null)
            Build();
        else if (!existing.gameObject.activeSelf)
        {
            existing.gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
        if (existing != null && existing.GetComponent<PositionOnlyHudFollower>() == null)
        {
            existing.gameObject.AddComponent<PositionOnlyHudFollower>();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
        if (existing != null)
        {
            TacticalAudioFeedback audio = existing.GetComponent<TacticalAudioFeedback>();
            if (audio == null) audio = existing.gameObject.AddComponent<TacticalAudioFeedback>();
            ConfigureAudio(audio);
            EditorUtility.SetDirty(audio);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
    }

    private static void ConfigureAudio(TacticalAudioFeedback audio)
    {
        SerializedObject so = new(audio);
        SerializedProperty shots = so.FindProperty("gunshots");
        shots.arraySize = 3;
        for (int i = 0; i < 3; i++)
            shots.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioPath}Gunshot_0{i + 1}.wav");
        so.FindProperty("hitConfirm").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + "Hit_Confirm.wav");
        so.FindProperty("damageTaken").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + "Damage_Taken.wav");
        so.FindProperty("countdownTick").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + "UI_Select.wav");
        so.FindProperty("roundStart").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + "Round_Start.wav");
        so.FindProperty("roundWin").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + "Round_Win.wav");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject Image(string name, Transform parent, Color color,
        Vector2 position, Vector2 size)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        UnityEngine.UI.Image image = item.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return item;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string value,
        float size, Color color, TextAlignmentOptions alignment,
        Vector2 position, Vector2 bounds)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
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
}
#endif
