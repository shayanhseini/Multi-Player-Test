using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class KeyboardInputField : MonoBehaviour, ISelectHandler
{
    [SerializeField] private TMP_InputField inputField;

    private void Awake()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (KeyboardManager.Instance == null)
        {
            Debug.LogWarning("KeyboardManager instance not found.");
            return;
        }

        KeyboardManager.Instance.SetInputField(inputField);
    }
}
