using UnityEngine;
using UnityEngine.UI;

// Legacy compatibility: role restrictions were removed.
public class NetworkUIPermission : MonoBehaviour
{
    [SerializeField] private Selectable selectable;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (selectable != null)
            selectable.interactable = true;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
