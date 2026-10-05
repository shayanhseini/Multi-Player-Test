using UnityEngine;
using UnityEngine.UI;

public class MarkerColorUI : MonoBehaviour
{
    [Header("Marker")]
    [SerializeField] private Renderer markerRenderer;
    [SerializeField] private string materialColorProperty = "_BaseColor";

    [Header("WhiteBoard")]
    [SerializeField] private WhiteBoard whiteBoard;

    [Header("Optional UI")]
    [SerializeField] private Image selectedColorIndicator;

    private Color currentColor;

    public void SetColor(Color newColor)
    {
        currentColor = newColor;

        // -------------------------
        // 1. تغییر رنگ متریال ماژیک
        // -------------------------
        if (markerRenderer != null)
        {
            Material mat = markerRenderer.material;

            if (mat.HasProperty(materialColorProperty))
            {
                mat.SetColor(materialColorProperty, newColor);
            }
        }

        // -------------------------
        // 2. تغییر رنگ Brush
        // -------------------------
        if (whiteBoard != null)
        {
            whiteBoard.SetBrushColor(newColor);
        }

        // -------------------------
        // 3. نمایش رنگ انتخاب شده
        // -------------------------
        if (selectedColorIndicator != null)
        {
            selectedColorIndicator.color = newColor;
        }
    }

    public void SetRed()
    {
        SetColor(Color.red);
    }

    public void SetOrange()
    {
        SetColor(new Color(1f, 0.45f, 0f));
    }

    public void SetYellow()
    {
        SetColor(Color.yellow);
    }

    public void SetGreen()
    {
        SetColor(Color.green);
    }

    public void SetBlue()
    {
        SetColor(Color.blue);
    }

    public void SetBlack()
    {
        SetColor(Color.black);
    }

    public void SetWhite()
    {
        SetColor(Color.white);
    }
}