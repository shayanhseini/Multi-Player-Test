using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class KeyboardManager : MonoBehaviour
{
    public static KeyboardManager Instance { get; private set; }

    [Header("Keyboard")]
    [SerializeField] private GameObject keyboardRoot;

    [Header("Settings")]
    [SerializeField] private bool hideKeyboardOnEnter = true;
    [SerializeField] private bool useCapsLock = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onKeyboardShown;
    [SerializeField] private UnityEvent onKeyboardHidden;

    private TMP_InputField activeInputField;

    // =========================================================
    // Properties
    // =========================================================

    public TMP_InputField ActiveInputField => activeInputField;

    public bool IsKeyboardVisible =>
        keyboardRoot != null && keyboardRoot.activeSelf;

    public bool IsCapsLock => useCapsLock;


    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        HideKeyboard();
    }


    // =========================================================
    // Input Field
    // =========================================================

    public void SetInputField(TMP_InputField inputField)
    {
        if (inputField == null)
            return;

        activeInputField = inputField;

        ShowKeyboard();

        MoveCaretToEnd();

        inputField.ActivateInputField();
    }


    public void ClearActiveInputField()
    {
        activeInputField = null;
    }


    // =========================================================
    // Keyboard Visibility
    // =========================================================

    public void ShowKeyboard()
    {
        if (keyboardRoot == null)
        {
            Debug.LogWarning("Keyboard Root is not assigned.");
            return;
        }

        if (!keyboardRoot.activeSelf)
        {
            keyboardRoot.SetActive(true);
            onKeyboardShown?.Invoke();
        }
    }


    public void HideKeyboard()
    {
        if (keyboardRoot == null)
            return;

        if (keyboardRoot.activeSelf)
        {
            keyboardRoot.SetActive(false);
            onKeyboardHidden?.Invoke();
        }
    }


    // =========================================================
    // Character
    // =========================================================

    public void PressCharacter(string character)
    {
        if (!HasActiveInputField())
            return;

        if (string.IsNullOrEmpty(character))
            return;


        // -----------------------------------------------------
        // Caps Lock
        // -----------------------------------------------------

        if (useCapsLock)
            character = character.ToUpper();
        else
            character = character.ToLower();


        TMP_InputField inputField = activeInputField;


        // -----------------------------------------------------
        // Selection
        // -----------------------------------------------------

        int selectionStart = inputField.selectionAnchorPosition;
        int selectionEnd = inputField.selectionFocusPosition;


        // -----------------------------------------------------
        // اگر بخشی از متن انتخاب شده باشد
        // آن قسمت را حذف می‌کنیم
        // -----------------------------------------------------

        if (selectionStart != selectionEnd)
        {
            int start = Mathf.Min(selectionStart, selectionEnd);

            int length = Mathf.Abs(selectionEnd - selectionStart);

            inputField.text = inputField.text.Remove(start, length);

            inputField.caretPosition = start;

            inputField.selectionAnchorPosition = start;
            inputField.selectionFocusPosition = start;
        }


        // -----------------------------------------------------
        // Caret Position
        // -----------------------------------------------------

        int caretPosition = inputField.caretPosition;


        // -----------------------------------------------------
        // Insert Character
        // -----------------------------------------------------

        inputField.text = inputField.text.Insert(
            caretPosition,
            character
        );


        // -----------------------------------------------------
        // Move Caret
        // -----------------------------------------------------

        caretPosition += character.Length;

        inputField.caretPosition = caretPosition;

        inputField.selectionAnchorPosition = caretPosition;
        inputField.selectionFocusPosition = caretPosition;


        // -----------------------------------------------------
        // Keep Focus
        // -----------------------------------------------------

        inputField.ActivateInputField();
    }


    // =========================================================
    // Space
    // =========================================================

    public void PressSpace()
    {
        PressCharacter(" ");
    }


    // =========================================================
    // Backspace
    // =========================================================

    public void PressBackspace()
    {
        if (!HasActiveInputField())
            return;

        TMP_InputField inputField = activeInputField;


        // -----------------------------------------------------
        // Selection
        // -----------------------------------------------------

        int selectionStart = inputField.selectionAnchorPosition;
        int selectionEnd = inputField.selectionFocusPosition;


        // -----------------------------------------------------
        // اگر متن انتخاب شده باشد
        // کل Selection حذف می‌شود
        // -----------------------------------------------------

        if (selectionStart != selectionEnd)
        {
            int start = Mathf.Min(selectionStart, selectionEnd);

            int length = Mathf.Abs(selectionEnd - selectionStart);

            inputField.text = inputField.text.Remove(
                start,
                length
            );


            inputField.caretPosition = start;

            inputField.selectionAnchorPosition = start;
            inputField.selectionFocusPosition = start;

            inputField.ActivateInputField();

            return;
        }


        // -----------------------------------------------------
        // Normal Backspace
        // -----------------------------------------------------

        int caretPosition = inputField.caretPosition;


        // ابتدای متن هستیم
        if (caretPosition <= 0)
            return;


        inputField.text = inputField.text.Remove(
            caretPosition - 1,
            1
        );


        // -----------------------------------------------------
        // Move Caret Back
        // -----------------------------------------------------

        caretPosition--;

        inputField.caretPosition = caretPosition;

        inputField.selectionAnchorPosition = caretPosition;
        inputField.selectionFocusPosition = caretPosition;


        // -----------------------------------------------------
        // Keep Focus
        // -----------------------------------------------------

        inputField.ActivateInputField();
    }


    // =========================================================
    // Enter
    // =========================================================

    public void PressEnter()
    {
        if (!HasActiveInputField())
            return;

        TMP_InputField inputField = activeInputField;


        // -----------------------------------------------------
        // Submit Event
        // -----------------------------------------------------

        inputField.onSubmit?.Invoke(inputField.text);


        // -----------------------------------------------------
        // Hide Keyboard
        // -----------------------------------------------------

        if (hideKeyboardOnEnter)
        {
            HideKeyboard();
        }
    }


    // =========================================================
    // Caps Lock
    // =========================================================

    public void ToggleCapsLock()
    {
        useCapsLock = !useCapsLock;
    }


    public void SetCapsLock(bool value)
    {
        useCapsLock = value;
    }


    // =========================================================
    // Clear Input
    // =========================================================

    public void ClearInput()
    {
        if (!HasActiveInputField())
            return;

        TMP_InputField inputField = activeInputField;

        inputField.text = string.Empty;

        MoveCaretToEnd();

        inputField.ActivateInputField();
    }


    // =========================================================
    // Caret
    // =========================================================

    private void MoveCaretToEnd()
    {
        if (!HasActiveInputField())
            return;

        TMP_InputField inputField = activeInputField;

        int position = inputField.text.Length;

        inputField.caretPosition = position;

        inputField.selectionAnchorPosition = position;
        inputField.selectionFocusPosition = position;
    }


    // =========================================================
    // Validation
    // =========================================================

    private bool HasActiveInputField()
    {
        if (activeInputField == null)
        {
            Debug.LogWarning(
                "No active TMP_InputField is assigned to KeyboardManager."
            );

            return false;
        }

        return true;
    }
}
