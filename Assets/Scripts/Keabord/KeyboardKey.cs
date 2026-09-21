using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class KeyboardKey : MonoBehaviour
{
    public enum KeyType
    {
        Character,
        Space,
        Backspace,
        Enter,
        CapsLock,
        Clear
    }

    [Header("Key")]
    [SerializeField] private KeyType keyType;

    [SerializeField] private string keyValue;

    private Button button;


    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        button = GetComponent<Button>();

        button.onClick.AddListener(Press);
    }


    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(Press);
    }


    // =========================================================
    // Press
    // =========================================================

    private void Press()
    {
        if (KeyboardManager.Instance == null)
        {
            Debug.LogWarning("KeyboardManager instance not found.");
            return;
        }

        switch (keyType)
        {
            case KeyType.Character:
                KeyboardManager.Instance.PressCharacter(keyValue);
                break;


            case KeyType.Space:
                KeyboardManager.Instance.PressSpace();
                break;


            case KeyType.Backspace:
                KeyboardManager.Instance.PressBackspace();
                break;


            case KeyType.Enter:
                KeyboardManager.Instance.PressEnter();
                break;


            case KeyType.CapsLock:
                KeyboardManager.Instance.ToggleCapsLock();
                break;


            case KeyType.Clear:
                KeyboardManager.Instance.ClearInput();
                break;
        }
    }
}