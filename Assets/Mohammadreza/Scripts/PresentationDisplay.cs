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

    [Header("Settings")]
    [SerializeField] private bool loop = false;

    private Material displayMaterial;
    private int currentIndex = 0;

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
                $"Material Index اشتباه است. این Renderer فقط {materials.Length} متریال دارد."
            );
            return;
        }

        displayMaterial = materials[materialIndex];

        Debug.Log(
            $"Presentation Display initialized. " +
            $"Material: {displayMaterial.name} | " +
            $"Property: {textureProperty}"
        );

        if (!displayMaterial.HasProperty(textureProperty))
        {
            Debug.LogError(
                $"Texture Property '{textureProperty}' در متریال " +
                $"'{displayMaterial.name}' پیدا نشد."
            );

            return;
        }

        if (images != null && images.Length > 0)
        {
            ShowImage(0);
        }
        else
        {
            Debug.LogWarning("هیچ عکسی در Images قرار داده نشده است.");
        }
    }

    public void NextImage()
    {
        if (images == null || images.Length == 0)
            return;

        if (currentIndex < images.Length - 1)
        {
            currentIndex++;
            ShowImage(currentIndex);
        }
        else if (loop)
        {
            currentIndex = 0;
            ShowImage(currentIndex);
        }
    }

    public void PreviousImage()
    {
        if (images == null || images.Length == 0)
            return;

        if (currentIndex > 0)
        {
            currentIndex--;
            ShowImage(currentIndex);
        }
        else if (loop)
        {
            currentIndex = images.Length - 1;
            ShowImage(currentIndex);
        }
    }

    private void ShowImage(int index)
    {
        if (displayMaterial == null)
            return;

        if (index < 0 || index >= images.Length)
            return;

        Texture texture = images[index];

        if (texture == null)
        {
            Debug.LogWarning($"Image {index + 1} خالی است.");
            return;
        }

        displayMaterial.SetTexture(textureProperty, texture);

        Debug.Log($"Showing Image {index + 1}: {texture.name}");
    }

    public int GetCurrentImageIndex()
    {
        return currentIndex;
    }
}