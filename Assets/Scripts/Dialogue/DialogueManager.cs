using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool IsDialogueActive => Instance != null && Instance.isDialogueActive;

    [Header("UI References")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private Image panelImage;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text continueHintText;

    [Header("Behavior")]
    [SerializeField] private bool createUiIfMissing = true;
    [SerializeField] private bool pauseGameplayWhileActive = true;
    [SerializeField] private float charactersPerSecond = 55f;
    [SerializeField] private KeyCode advancePrimaryKey = KeyCode.Z;
    [SerializeField] private KeyCode advanceSecondaryKey = KeyCode.Space;
    [SerializeField] private KeyCode advanceTertiaryKey = KeyCode.E;
    [SerializeField] private string continueHint = "Z / SPACE / E";

    private DialogueSequence activeSequence;
    private Action onDialogueComplete;
    private string currentLineText = string.Empty;
    private int lineIndex;
    private int visibleCharacterCount;
    private float characterProgress;
    private bool isDialogueActive;
    private bool isTyping;
    private bool modifiedTimeScale;
    private float cachedTimeScale = 1f;

    public static bool Play(DialogueSequence sequence, Action onComplete = null)
    {
        if (sequence == null)
        {
            return false;
        }

        DialogueManager manager = Instance;
        if (manager == null)
        {
            GameObject managerObject = new GameObject("DialogueManager");
            manager = managerObject.AddComponent<DialogueManager>();
        }

        return manager.StartDialogue(sequence, onComplete);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUiReferences();
        SetDialogueVisible(false);
    }

    private void OnEnable()
    {
        EnsureUiReferences();
    }

    private void OnDisable()
    {
        RestoreTimeScaleIfNeeded();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        RestoreTimeScaleIfNeeded();
    }

    private void Update()
    {
        if (!isDialogueActive || activeSequence == null)
        {
            return;
        }

        TickTypewriter();

        if (!WasAdvancePressedThisFrame())
        {
            return;
        }

        if (isTyping)
        {
            RevealCurrentLineInstant();
            return;
        }

        AdvanceLine();
    }

    public bool StartDialogue(DialogueSequence sequence, Action onComplete = null)
    {
        if (sequence == null || sequence.LineCount == 0)
        {
            return false;
        }

        EnsureUiReferences();
        if (panelImage == null || bodyText == null)
        {
            Debug.LogWarning("[DialogueManager] Missing dialogue UI references.");
            return false;
        }

        if (isDialogueActive)
        {
            EndDialogue(false);
        }

        activeSequence = sequence;
        onDialogueComplete = onComplete;
        lineIndex = 0;
        visibleCharacterCount = 0;
        characterProgress = 0f;
        currentLineText = string.Empty;
        isDialogueActive = true;
        isTyping = false;

        SetDialogueVisible(true);
        ApplyPauseIfNeeded();
        PresentCurrentLine();
        return true;
    }

    public void EndDialogue(bool invokeCallback = true)
    {
        isDialogueActive = false;
        isTyping = false;
        activeSequence = null;
        lineIndex = 0;
        visibleCharacterCount = 0;
        characterProgress = 0f;
        currentLineText = string.Empty;
        bodyText.text = string.Empty;
        if (speakerText != null)
        {
            speakerText.text = string.Empty;
            speakerText.gameObject.SetActive(false);
        }

        SetDialogueVisible(false);
        RestoreTimeScaleIfNeeded();

        Action callback = onDialogueComplete;
        onDialogueComplete = null;
        if (invokeCallback)
        {
            callback?.Invoke();
        }
    }

    private void PresentCurrentLine()
    {
        if (activeSequence == null || !activeSequence.TryGetLine(lineIndex, out DialogueSequence.DialogueLine line))
        {
            EndDialogue(true);
            return;
        }

        string speaker = line.Speaker ?? string.Empty;
        currentLineText = line.Text ?? string.Empty;
        visibleCharacterCount = 0;
        characterProgress = 0f;
        isTyping = currentLineText.Length > 0;

        if (speakerText != null)
        {
            bool hasSpeaker = !string.IsNullOrWhiteSpace(speaker);
            speakerText.gameObject.SetActive(hasSpeaker);
            if (hasSpeaker)
            {
                speakerText.text = speaker;
            }
        }

        bodyText.text = string.Empty;
        UpdateContinueHint();
    }

    private void TickTypewriter()
    {
        if (!isTyping)
        {
            return;
        }

        float speed = Mathf.Max(1f, charactersPerSecond);
        characterProgress += Time.unscaledDeltaTime * speed;
        int targetVisible = Mathf.Clamp(Mathf.FloorToInt(characterProgress), 0, currentLineText.Length);
        if (targetVisible == visibleCharacterCount)
        {
            return;
        }

        visibleCharacterCount = targetVisible;
        bodyText.text = currentLineText.Substring(0, visibleCharacterCount);

        if (visibleCharacterCount >= currentLineText.Length)
        {
            isTyping = false;
            bodyText.text = currentLineText;
            UpdateContinueHint();
        }
    }

    private void RevealCurrentLineInstant()
    {
        isTyping = false;
        visibleCharacterCount = currentLineText.Length;
        characterProgress = visibleCharacterCount;
        bodyText.text = currentLineText;
        UpdateContinueHint();
    }

    private void AdvanceLine()
    {
        lineIndex++;
        if (activeSequence != null && lineIndex < activeSequence.LineCount)
        {
            PresentCurrentLine();
            return;
        }

        EndDialogue(true);
    }

    private bool WasAdvancePressedThisFrame()
    {
        return InputBridge.WasKeyPressedThisFrame(advancePrimaryKey) ||
               InputBridge.WasKeyPressedThisFrame(advanceSecondaryKey) ||
               InputBridge.WasKeyPressedThisFrame(advanceTertiaryKey);
    }

    private void UpdateContinueHint()
    {
        if (continueHintText == null)
        {
            return;
        }

        continueHintText.text = isTyping ? "AVANZAR: " + continueHint : "SIGUIENTE: " + continueHint;
    }

    private void ApplyPauseIfNeeded()
    {
        if (!pauseGameplayWhileActive || modifiedTimeScale)
        {
            return;
        }

        cachedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        modifiedTimeScale = true;
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!modifiedTimeScale)
        {
            return;
        }

        Time.timeScale = cachedTimeScale;
        modifiedTimeScale = false;
    }

    private void EnsureUiReferences()
    {
        if (dialogueCanvas == null)
        {
            dialogueCanvas = FindAnyObjectByType<Canvas>();
            if (dialogueCanvas == null && createUiIfMissing)
            {
                dialogueCanvas = CreateCanvas();
            }
        }

        if (dialogueCanvas == null)
        {
            return;
        }

        if (panelImage == null && createUiIfMissing)
        {
            panelImage = CreatePanel(dialogueCanvas.transform);
        }

        if (panelImage == null)
        {
            return;
        }

        if (speakerText == null && createUiIfMissing)
        {
            speakerText = CreateText("SpeakerText", panelImage.transform, TextAnchor.UpperLeft, 30, FontStyle.Bold);
            RectTransform rect = speakerText.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.62f);
            rect.anchorMax = new Vector2(1f, 0.95f);
            rect.offsetMin = new Vector2(28f, 0f);
            rect.offsetMax = new Vector2(-28f, 0f);
        }

        if (bodyText == null && createUiIfMissing)
        {
            bodyText = CreateText("BodyText", panelImage.transform, TextAnchor.UpperLeft, 26, FontStyle.Normal);
            RectTransform rect = bodyText.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.22f);
            rect.anchorMax = new Vector2(1f, 0.65f);
            rect.offsetMin = new Vector2(28f, 0f);
            rect.offsetMax = new Vector2(-28f, 0f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        if (continueHintText == null && createUiIfMissing)
        {
            continueHintText = CreateText("ContinueHintText", panelImage.transform, TextAnchor.LowerRight, 20, FontStyle.Italic);
            RectTransform rect = continueHintText.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0.2f);
            rect.offsetMin = new Vector2(28f, 0f);
            rect.offsetMax = new Vector2(-28f, 0f);
        }
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("DialogueCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private Image CreatePanel(Transform parent)
    {
        GameObject panelObject = new GameObject("DialoguePanel");
        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.8f);

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.07f, 0.03f);
        rect.anchorMax = new Vector2(0.93f, 0.27f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return image;
    }

    private Text CreateText(string objectName, Transform parent, TextAnchor alignment, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        text.alignment = alignment;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.text = string.Empty;
        text.supportRichText = true;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
    }

    private void SetDialogueVisible(bool isVisible)
    {
        if (panelImage != null)
        {
            panelImage.gameObject.SetActive(isVisible);
        }
    }
}
