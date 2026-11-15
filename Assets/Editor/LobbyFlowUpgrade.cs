#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class LobbyFlowUpgrade
{
    private const string ScenePath = "Assets/Scenes/Conference Room Scene.unity";
    private const string MarkerName = "Lobby Flow V2";

    static LobbyFlowUpgrade() => EditorApplication.delayCall += UpgradeIfNeeded;

    [MenuItem("Tools/LAN Shooter/Upgrade Lobby Gameplay Flow")]
    public static void Upgrade()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;

        GameObject root = GameObject.Find("Professional LAN Lobby");
        MatchLobbyUI lobby = Object.FindFirstObjectByType<MatchLobbyUI>(
            FindObjectsInactive.Include);
        if (root == null || lobby == null)
            return;

        Transform previousMarker = root.transform.Find(MarkerName);
        if (previousMarker != null)
            Object.DestroyImmediate(previousMarker.gameObject);
        GameObject marker = new(MarkerName);
        marker.transform.SetParent(root.transform, false);

        SerializedObject serialized = new(lobby);
        UnityEngine.UI.Button observer = serialized.FindProperty("observerButton")
            .objectReferenceValue as UnityEngine.UI.Button;
        if (observer != null)
        {
            observer.gameObject.name = "Observer Button";
            Transform title = observer.transform.Find("Title");
            Transform subtitle = observer.transform.Find("Subtitle");
            if (title != null && title.TryGetComponent(out TextMeshProUGUI titleText))
                titleText.text = "VR OBSERVER";
            if (subtitle != null && subtitle.TryGetComponent(out TextMeshProUGUI subtitleText))
                subtitleText.text = "Host-only free-flight spectator";
        }

        Transform header = root.transform.Find("Header");
        if (header != null && header.Find("Gameplay Hint") == null)
        {
            GameObject hintObject = new("Gameplay Hint", typeof(RectTransform),
                typeof(TextMeshProUGUI));
            hintObject.layer = 5;
            hintObject.transform.SetParent(header, false);
            RectTransform rect = hintObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 28f);
            rect.anchoredPosition = new Vector2(300f, -43f);
            TextMeshProUGUI text = hintObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = "A  SCOREBOARD    •    B  SESSION MENU";
            text.fontSize = 13f;
            text.color = new Color(0.56f, 0.64f, 0.73f, 1f);
            text.alignment = TextAlignmentOptions.Right;
            text.raycastTarget = false;
        }

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[LAN UI] Observer and in-game session flow were upgraded.");
    }

    private static void UpgradeIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;
        GameObject root = GameObject.Find("Professional LAN Lobby");
        if (root != null && root.transform.Find(MarkerName) == null)
            Upgrade();
    }
}
#endif
