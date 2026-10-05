using UnityEngine;

public class MarkerColorController : MonoBehaviour
{
    [Header("WhiteBoard")]
    [SerializeField] private WhiteBoard whiteBoard;

    [Header("Marker")]
    [SerializeField] private Renderer markerRenderer;

    [Header("Marker Material")]
    [SerializeField] private int materialIndex = 1;

    [SerializeField] private string colorProperty = "_BaseColor";

    [Header("Selected Color")]
    [SerializeField] private Color currentColor = Color.red;


    public void SetColor(Color newColor)
    {
        currentColor = newColor;

        // =====================================
        // 1. تغییر رنگ Brush روی WhiteBoard
        // =====================================

        if (whiteBoard != null)
        {
            whiteBoard.SetBrushColor(newColor);
        }


        // =====================================
        // 2. تغییر رنگ متریال PencilColor
        // =====================================

        if (markerRenderer != null)
        {
            Material[] materials = markerRenderer.materials;

            if (materialIndex >= 0 && materialIndex < materials.Length)
            {
                Material targetMaterial = materials[materialIndex];

                if (targetMaterial.HasProperty("_BaseColor"))
                {
                    targetMaterial.SetColor("_BaseColor", newColor);
                }
                else if (targetMaterial.HasProperty("_Color"))
                {
                    targetMaterial.SetColor("_Color", newColor);
                }
                else
                {
                    Debug.LogWarning(
                        "[MarkerColorController] Target material has no _BaseColor or _Color."
                    );
                }
            }
            else
            {
                Debug.LogError(
                    "[MarkerColorController] Material Index is out of range!"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[MarkerColorController] Marker Renderer is not assigned!"
            );
        }


        Debug.Log(
            $"[MarkerColorController] Color changed to {newColor}"
        );
    }
}