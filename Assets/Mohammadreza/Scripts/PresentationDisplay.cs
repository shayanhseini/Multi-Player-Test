using UnityEngine;

public class PresentationDisplay : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private Renderer displayRenderer;

    [Tooltip("شماره متریالی که صفحه نمایش روی آن قرار دارد")]
    [SerializeField] private int materialIndex = 1;

    [Tooltip("نام Property مربوط به Texture در Shader")]
    [SerializeField] private string textureProperty = "_MainTex";

    [Header("Presentation Images")]
    [SerializeField] private Texture[] images;

    [Header("Network State")]
    [SerializeField] private PresentationState presentationState;

    private Material displayMaterial;

    private void Awake()
    {
        if (displayRenderer == null)
        {
            Debug.LogError("Display Renderer تنظیم نشده است.");
            return;
        }

        Material[] materials = displayRenderer.materials;

        if (materialIndex < 0 || materialIndex >= materials.Length)
        {
            Debug.LogError(
                $"Material Index اشتباه است. " +
                $"این Renderer فقط {materials.Length} متریال دارد."
            );

            return;
        }

        displayMaterial = materials[materialIndex];

        if (!displayMaterial.HasProperty(textureProperty))
        {
            Debug.LogError(
                $"Texture Property '{textureProperty}' " +
                $"در متریال '{displayMaterial.name}' پیدا نشد."
            );

            return;
        }

        if (images == null || images.Length == 0)
        {
            Debug.LogWarning(
                "هیچ عکسی در Presentation Images قرار داده نشده است."
            );
        }
    }

    private void Start()
    {
        if (presentationState == null)
        {
            Debug.LogError(
                "Presentation State تنظیم نشده است."
            );

            return;
        }

        // تعداد صفحات برابر تعداد تصاویر است
        presentationState.SetPageCount(images.Length);

        // گوش دادن به تغییر صفحه
        presentationState.currentPage.OnValueChanged += OnPageChanged;

        // نمایش صفحه فعلی
        UpdateDisplay(presentationState.currentPage.Value);
    }

    private void OnPageChanged(int oldPage, int newPage)
    {
        UpdateDisplay(newPage);
    }

    private void UpdateDisplay(int page)
    {
        if (displayMaterial == null)
            return;

        if (images == null || images.Length == 0)
            return;

        if (page < 0 || page >= images.Length)
        {
            Debug.LogWarning(
                $"Page {page} برای Images معتبر نیست. " +
                $"تعداد تصاویر: {images.Length}"
            );

            return;
        }

        Texture texture = images[page];

        if (texture == null)
        {
            Debug.LogWarning(
                $"Image {page + 1} خالی است."
            );

            return;
        }

        displayMaterial.SetTexture(
            textureProperty,
            texture
        );

        Debug.Log(
            $"Presentation Display → " +
            $"Page {page + 1}: {texture.name}"
        );
    }

    private void OnDestroy()
    {
        if (presentationState != null)
        {
            presentationState.currentPage.OnValueChanged -= OnPageChanged;
        }
    }
}