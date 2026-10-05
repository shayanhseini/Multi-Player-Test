using UnityEngine;
using UnityEngine.UI;

public class MarkerColorButton : MonoBehaviour
{
    [SerializeField] private MarkerColorController colorController;
    [SerializeField] private Image colorImage;

    public void SelectColor()
    {
        if (colorController == null)
        {
            Debug.LogWarning(
                "[MarkerColorButton] Color Controller is not assigned!"
            );
            return;
        }

        if (colorImage == null)
        {
            Debug.LogWarning(
                "[MarkerColorButton] Color Image is not assigned!"
            );
            return;
        }

        colorController.SetColor(colorImage.color);
    }
}