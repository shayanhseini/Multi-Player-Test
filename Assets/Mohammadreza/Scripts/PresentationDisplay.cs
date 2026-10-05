using UnityEngine;

public class PresentationDisplay : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private Renderer displayRenderer;
    [SerializeField] private int materialIndex = 1;
    [SerializeField] private string textureProperty = "_MainTex";

    [Header("Presentation")]
    [SerializeField] private Texture[] images;
    [SerializeField] private PresentationState presentationState;

    private Material displayMaterial;

    private void Start()
    {
        if (displayRenderer == null)
        {
            Debug.LogError(
                "[PresentationDisplay] Display Renderer is missing."
            );
            return;
        }

        if (presentationState == null)
        {
            Debug.LogError(
                "[PresentationDisplay] PresentationState is missing."
            );
            return;
        }

        if (images == null || images.Length == 0)
        {
            Debug.LogError(
                "[PresentationDisplay] No presentation images assigned."
            );
            return;
        }

        Material[] materials = displayRenderer.materials;

        if (materialIndex < 0 || materialIndex >= materials.Length)
        {
            Debug.LogError(
                "[PresentationDisplay] Invalid material index."
            );
            return;
        }

        displayMaterial = materials[materialIndex];

        if (!displayMaterial.HasProperty(textureProperty))
        {
            Debug.LogError(
                $"[PresentationDisplay] Property {textureProperty} not found."
            );
            return;
        }

        // تعداد صفحات را به PresentationState اعلام می‌کند
        presentationState.SetPageCount(images.Length);

        // دریافت تغییر صفحه شبکه‌ای
        presentationState.currentPage.OnValueChanged += OnPageChanged;

        // نمایش مقدار فعلی
        ShowImage(presentationState.currentPage.Value);
    }

    private void OnPageChanged(int previousPage, int newPage)
    {
        ShowImage(newPage);
    }

    private void ShowImage(int index)
    {
        if (displayMaterial == null)
            return;

        if (index < 0 || index >= images.Length)
        {
            Debug.LogWarning(
                $"[PresentationDisplay] Page index {index} is invalid."
            );
            return;
        }

        displayMaterial.SetTexture(
            textureProperty,
            images[index]
        );

        Debug.Log(
            $"[PresentationDisplay] Showing page {index + 1}"
        );
    }

    public void NextImage()
    {
        presentationState.NextPage();
    }

    public void PreviousImage()
    {
        presentationState.PreviousPage();
    }

    private void OnDestroy()
    {
        if (presentationState != null)
        {
            presentationState.currentPage.OnValueChanged -=
                OnPageChanged;
        }
    }
}