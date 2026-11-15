#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ProfessionalKeyboardUIBuilder
{
    private const string ScenePath = "Assets/Scenes/Conference Room Scene.unity";
    private const string MarkerName = "Keyboard UI V2";

    static ProfessionalKeyboardUIBuilder() => EditorApplication.delayCall += BuildIfNeeded;

    [MenuItem("Tools/LAN Shooter/Rebuild Professional Keyboard UI")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;

        KeyboardManager manager = Object.FindFirstObjectByType<KeyboardManager>(
            FindObjectsInactive.Include);
        if (manager == null)
            return;
        SerializedObject managerSerialized = new(manager);
        GameObject root = managerSerialized.FindProperty("keyboardRoot")
            .objectReferenceValue as GameObject;
        if (root == null)
            return;

        Transform oldMarker = root.transform.Find(MarkerName);
        if (oldMarker != null)
            Object.DestroyImmediate(oldMarker.gameObject);
        Transform oldHeader = root.transform.Find("Keyboard Header");
        if (oldHeader != null)
            Object.DestroyImmediate(oldHeader.gameObject);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1180f, 620f);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.sortingOrder = 60;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        Transform panel = root.transform.Find("Panel");
        if (panel == null && root.transform.childCount > 0)
            panel = root.transform.GetChild(0);
        if (panel != null)
        {
            UnityEngine.UI.Image background = panel.GetComponent<UnityEngine.UI.Image>();
            if (background != null)
            {
                background.color = Hex("07101D", 0.985f);
                background.sprite = BuiltInSprite();
                background.type = UnityEngine.UI.Image.Type.Sliced;
            }
            VerticalLayoutGroup vertical = panel.GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
            {
                vertical.padding = new RectOffset(28, 28, 105, 28);
                vertical.spacing = 10f;
                vertical.childAlignment = TextAnchor.MiddleCenter;
            }
            foreach (HorizontalLayoutGroup row in
                     panel.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                row.spacing = 10f;
                row.padding = new RectOffset(5, 5, 2, 2);
                row.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            UnityEngine.UI.Image image = button.GetComponent<UnityEngine.UI.Image>();
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            string value = label != null ? label.text.Trim() : string.Empty;
            bool primary = value.Equals("Space", System.StringComparison.OrdinalIgnoreCase) ||
                           value.Equals("Enter", System.StringComparison.OrdinalIgnoreCase);
            if (image != null)
            {
                image.color = primary ? Hex("2474E5", 1f) : Hex("17283D", 1f);
                image.sprite = BuiltInSprite();
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }
            if (label != null)
            {
                label.color = Hex("F3F7FC", 1f);
                label.fontSize = primary ? 24f : 22f;
                label.fontStyle = FontStyles.Bold;
            }
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Hex("C8EFFF", 1f);
            colors.pressedColor = Hex("35C8F5", 1f);
            colors.selectedColor = Hex("A7E4F6", 1f);
            colors.disabledColor = Hex("556273", 0.55f);
            button.colors = colors;
        }

        GameObject marker = CreateUI(MarkerName, root.transform);
        marker.SetActive(true);
        GameObject header = CreateImage("Keyboard Header", root.transform,
            Hex("0E1B2B", 1f), new Vector2(0f, 267f), new Vector2(1180f, 86f));
        CreateImage("Accent", header.transform, Hex("35C8F5", 1f),
            new Vector2(-550f, 0f), new Vector2(6f, 48f));
        TextMeshProUGUI title = CreateText("Title", header.transform,
            "ENTER CALLSIGN", 25f, Hex("F3F7FC", 1f),
            TextAlignmentOptions.Left, new Vector2(-85f, 12f),
            new Vector2(860f, 34f));
        title.fontStyle = FontStyles.Bold;
        CreateText("Hint", header.transform,
            "Select letters with the VR pointer  •  ENTER confirms input",
            14f, Hex("8FA4BA", 1f), TextAlignmentOptions.Left,
            new Vector2(-85f, -18f), new Vector2(860f, 24f));
        GameObject badge = CreateImage("Input Badge", header.transform,
            Hex("123A3B", 1f), new Vector2(485f, 0f), new Vector2(150f, 42f));
        CreateText("Label", badge.transform, "VR INPUT", 14f,
            Hex("6DE3B3", 1f), TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(130f, 24f));

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[LAN UI] Professional VR keyboard was created.");
    }

    private static void BuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
            return;
        KeyboardManager manager = Object.FindFirstObjectByType<KeyboardManager>(
            FindObjectsInactive.Include);
        if (manager == null)
            return;
        SerializedObject serialized = new(manager);
        GameObject root = serialized.FindProperty("keyboardRoot").objectReferenceValue as GameObject;
        if (root != null && root.transform.Find(MarkerName) == null)
            Rebuild();
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
        text.font = TMP_Settings.defaultFontAsset;
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
