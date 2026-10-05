using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRColorPicker : MonoBehaviour
{
    [Header("Spectrum")]
    [SerializeField] private Image spectrum;
    [SerializeField] private RectTransform spectrumRect;

    [Header("Puck")]
    [SerializeField] private RectTransform puck;

    [Header("Selected Color")]
    [SerializeField] private Image selectedColor;

    [Header("XR Ray")]
    [SerializeField] private XRRayInteractor rayInteractor;

    [Header("Marker")]
    [SerializeField] private MarkerColorController colorController;

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    private Texture2D spectrumTexture;

    private void Start()
    {
        Debug.Log("========== VR COLOR PICKER DEBUG START ==========");

        // Ray
        if (rayInteractor == null)
        {
            Debug.LogError("[VRColorPicker] ❌ XR Ray Interactor = NULL");
        }
        else
        {
            Debug.Log(
                "[VRColorPicker] ✅ XR Ray = " +
                rayInteractor.gameObject.name
            );

            Debug.Log(
                "[VRColorPicker] UI Interaction = " +
                rayInteractor.enableUIInteraction
            );
        }

        // Spectrum
        if (spectrum == null)
        {
            Debug.LogError("[VRColorPicker] ❌ Spectrum = NULL");
        }
        else
        {
            Debug.Log(
                "[VRColorPicker] ✅ Spectrum = " +
                spectrum.gameObject.name
            );

            if (spectrum.sprite == null)
            {
                Debug.LogError(
                    "[VRColorPicker] ❌ Spectrum Sprite = NULL"
                );
            }
            else
            {
                spectrumTexture = spectrum.sprite.texture;

                Debug.Log(
                    "[VRColorPicker] ✅ Spectrum Texture = " +
                    spectrumTexture.name
                );

                Debug.Log(
                    "[VRColorPicker] Texture Readable = " +
                    spectrumTexture.isReadable
                );
            }
        }

        // Controller
        if (colorController == null)
        {
            Debug.LogError(
                "[VRColorPicker] ❌ MarkerColorController = NULL"
            );
        }
        else
        {
            Debug.Log(
                "[VRColorPicker] ✅ MarkerColorController = " +
                colorController.gameObject.name
            );
        }

        Debug.Log("========== DEBUG READY ==========");
    }


    private void Update()
    {
        if (rayInteractor == null)
            return;

        // ------------------------------------------
        // Get UI Model
        // ------------------------------------------

        if (!rayInteractor.TryGetUIModel(out var uiModel))
            return;

        // ------------------------------------------
        // Get current UI hit
        // ------------------------------------------

        GameObject hitObject =
            uiModel.currentRaycast.gameObject;

        if (hitObject == null)
            return;

        // ------------------------------------------
        // Check Spectrum
        // ------------------------------------------

        bool spectrumHit =
            hitObject == spectrum.gameObject ||
            hitObject.transform.IsChildOf(spectrum.transform);

        if (!spectrumHit)
            return;

        // ------------------------------------------
        // DEBUG
        // ------------------------------------------

        Debug.Log(
            "[VRColorPicker] 🎯 SPECTRUM HIT | " +
            "isSelectActive = " +
            rayInteractor.isSelectActive +
            " | uiModel.select = " +
            uiModel.select
        );

        // ------------------------------------------
        // IMPORTANT:
        // Use uiModel.select for UI interaction
        // ------------------------------------------

        if (uiModel.select)
        {
            Debug.Log(
                "[VRColorPicker] 🟢 UI SELECT DETECTED!"
            );

            PickColor(
                uiModel.currentRaycast.worldPosition
            );
        }
    }


    private void PickColor(Vector3 worldPosition)
    {
        Debug.Log(
            "[VRColorPicker] ============================="
        );

        Debug.Log(
            "[VRColorPicker] 🎨 PickColor() CALLED"
        );

        if (spectrumTexture == null)
        {
            Debug.LogError(
                "[VRColorPicker] ❌ Texture NULL"
            );

            return;
        }

        // ------------------------------------------
        // World -> Local
        // ------------------------------------------

        Vector2 localPoint =
            spectrumRect.InverseTransformPoint(
                worldPosition
            );

        Debug.Log(
            "[VRColorPicker] Local Point = " +
            localPoint
        );

        // ------------------------------------------
        // Normalize
        // ------------------------------------------

        Rect rect = spectrumRect.rect;

        float x = Mathf.InverseLerp(
            rect.xMin,
            rect.xMax,
            localPoint.x
        );

        float y = Mathf.InverseLerp(
            rect.yMin,
            rect.yMax,
            localPoint.y
        );

        x = Mathf.Clamp01(x);
        y = Mathf.Clamp01(y);

        Debug.Log(
            "[VRColorPicker] UV = " +
            x +
            " , " +
            y
        );

        // ------------------------------------------
        // Get Color
        // ------------------------------------------

        Color color =
            spectrumTexture.GetPixelBilinear(x, y);

        color.a = 1f;

        Debug.Log(
            "[VRColorPicker] 🎨 COLOR = " +
            color
        );

        // ------------------------------------------
        // Puck
        // ------------------------------------------

        if (puck != null)
        {
            float puckX = Mathf.Lerp(
                rect.xMin,
                rect.xMax,
                x
            );

            float puckY = Mathf.Lerp(
                rect.yMin,
                rect.yMax,
                y
            );

            puck.localPosition =
                new Vector3(
                    puckX,
                    puckY,
                    puck.localPosition.z
                );

            Debug.Log(
                "[VRColorPicker] ✅ Puck moved"
            );
        }

        // ------------------------------------------
        // Selected Color
        // ------------------------------------------

        if (selectedColor != null)
        {
            selectedColor.color = color;

            Debug.Log(
                "[VRColorPicker] ✅ SelectedColor changed"
            );
        }

        // ------------------------------------------
        // Marker
        // ------------------------------------------

        if (colorController != null)
        {
            Debug.Log(
                "[VRColorPicker] 🚀 Sending color to Marker"
            );

            colorController.SetColor(color);

            Debug.Log(
                "[VRColorPicker] ✅ MarkerColorController.SetColor DONE"
            );
        }

        Debug.Log(
            "[VRColorPicker] ============================="
        );
    }
}