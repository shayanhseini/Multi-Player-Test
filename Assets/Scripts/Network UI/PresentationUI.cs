using System.Collections.Generic;
using UnityEngine;

public class PresentationUI : MonoBehaviour
{
    [Header("Presentation Panels")]
    [SerializeField] private List<GameObject> panels;

    [Header("Network State")]
    [SerializeField] private PresentationState presentationState;

    private void Start()
    {
        if (presentationState == null)
        {
            Debug.LogError(
                "Presentation State تنظیم نشده است."
            );

            return;
        }

        if (panels == null || panels.Count == 0)
        {
            Debug.LogWarning(
                "هیچ Panel ای در Presentation UI قرار داده نشده است."
            );

            return;
        }

        // نمایش صفحه فعلی
        presentationState.currentPage.OnValueChanged += OnPageChanged;

        UpdatePage(presentationState.currentPage.Value);
    }

    private void OnPageChanged(int oldPage, int newPage)
    {
        UpdatePage(newPage);
    }

    private void UpdatePage(int page)
    {
        if (panels == null || panels.Count == 0)
            return;

        if (page < 0 || page >= panels.Count)
        {
            Debug.LogWarning(
                $"Page {page} برای Panels معتبر نیست. " +
                $"تعداد Panels: {panels.Count}"
            );

            return;
        }

        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(i == page);
        }
    }

    private void OnDestroy()
    {
        if (presentationState != null)
        {
            presentationState.currentPage.OnValueChanged -= OnPageChanged;
        }
    }
}