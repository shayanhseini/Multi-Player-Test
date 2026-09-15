using UnityEngine;

public class PresentationUI : MonoBehaviour
{
    [SerializeField] private GameObject panel1;
    [SerializeField] private GameObject panel2;

    [SerializeField] private PresentationState presentationState;

    private void Start()
    {
        presentationState.currentPage.OnValueChanged += OnPageChanged;

        UpdatePage(presentationState.currentPage.Value);
    }

    private void OnPageChanged(int oldPage, int newPage)
    {
        UpdatePage(newPage);
    }

    private void UpdatePage(int page)
    {
        panel1.SetActive(page == 0);
        panel2.SetActive(page == 1);
    }

    private void OnDestroy()
    {
        if (presentationState != null)
        {
            presentationState.currentPage.OnValueChanged -= OnPageChanged;
        }
    }
}