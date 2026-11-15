#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ObjectiveMarkerBuilder
{
    private const string ScenePath = "Assets/Scenes/Conference Room Scene.unity";
    private const string FontPath = "Assets/Font/Orbitron/Orbitron-Regular SDF.asset";
    static ObjectiveMarkerBuilder() => EditorApplication.delayCall += Build;

    [MenuItem("Tools/LAN Shooter/Build Objective Markers")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath) return;
        GameObject flag = GameObject.Find("Flag") ?? GameObject.Find("Network Flag");
        BuildOne(flag, "Flag Objective Marker",
            ObjectiveWorldMarker.MarkerKind.Flag, new Color(1f, 0.7f, 0.15f));
        BuildOne(GameObject.Find("Escape Zone"), "Extraction Objective Marker",
            ObjectiveWorldMarker.MarkerKind.Extraction, new Color(0.2f, 0.9f, 0.65f));
        BuildOne(GameObject.Find("Flag Home"), "Flag Home Objective Marker",
            ObjectiveWorldMarker.MarkerKind.FlagHome, new Color(0.25f, 0.55f, 1f));
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    private static void BuildOne(GameObject target, string name,
        ObjectiveWorldMarker.MarkerKind kind, Color color)
    {
        if (target == null || target.transform.Find(name) != null) return;
        GameObject root = new(name, typeof(RectTransform), typeof(Canvas));
        root.layer = 5;
        root.transform.SetParent(target.transform, false);
        root.transform.localPosition = Vector3.up * 1.5f;
        root.transform.localScale = Vector3.one * 0.002f;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 70f);
        GameObject textObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = 5;
        textObject.transform.SetParent(root.transform, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320f, 70f);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ?? TMP_Settings.defaultFontAsset;
        text.text = kind == ObjectiveWorldMarker.MarkerKind.Flag ? "FLAG" : "EXTRACTION";
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        ObjectiveWorldMarker marker = root.AddComponent<ObjectiveWorldMarker>();
        SerializedObject so = new(marker);
        so.FindProperty("markerKind").enumValueIndex = (int)kind;
        so.FindProperty("label").objectReferenceValue = text;
        so.FindProperty("visualRoot").objectReferenceValue = textObject;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
