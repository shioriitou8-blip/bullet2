using UnityEngine;

[DisallowMultipleComponent]
public class SampleSceneDialogueNpc : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 1.7f;
    [SerializeField] private Transform playerOverride;

    [Header("Dialogue")]
    [SerializeField] private string speakerName = "Operadora";
    [TextArea(2, 5)]
    [SerializeField] private string line1 = "Llegaste justo a tiempo.";
    [TextArea(2, 5)]
    [SerializeField] private string line2 = "Mantente en movimiento y usa foco para esquivar.";
    [TextArea(2, 5)]
    [SerializeField] private string line3 = "Cuando estes listo, avanza al jefe.";

    [Header("Visual")]
    [SerializeField] private bool autoCreateVisual = true;
    [SerializeField] private Color npcColor = new Color(0.65f, 0.95f, 1f, 1f);

    private static Sprite fallbackSprite;
    private Transform cachedPlayer;
    private DialogueSequence runtimeSequence;

    private void Awake()
    {
        EnsureVisual();
    }

    private void Reset()
    {
        EnsureVisual();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        interactionDistance = Mathf.Max(0.05f, interactionDistance);

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        if (autoCreateVisual && renderer.sprite == null)
        {
            renderer.sprite = GetOrCreateFallbackSprite();
        }

        renderer.color = npcColor;
        renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 55);
    }

    private void Update()
    {
        if (DialogueManager.IsDialogueActive)
        {
            return;
        }

        if (!InputBridge.WasKeyPressedThisFrame(interactKey))
        {
            return;
        }

        if (!IsPlayerInRange())
        {
            return;
        }

        DialogueManager.Play(GetOrCreateRuntimeSequence());
    }

    private bool IsPlayerInRange()
    {
        Transform player = ResolvePlayer();
        if (player == null)
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        return distance <= Mathf.Max(0.05f, interactionDistance);
    }

    private Transform ResolvePlayer()
    {
        if (playerOverride != null)
        {
            return playerOverride;
        }

        if (cachedPlayer != null)
        {
            return cachedPlayer;
        }

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            cachedPlayer = player.transform;
        }

        return cachedPlayer;
    }

    private DialogueSequence GetOrCreateRuntimeSequence()
    {
        if (runtimeSequence != null)
        {
            return runtimeSequence;
        }

        runtimeSequence = DialogueSequence.CreateRuntime(
            DialogueSequence.MakeLine(speakerName, line1),
            DialogueSequence.MakeLine(speakerName, line2),
            DialogueSequence.MakeLine(speakerName, line3));

        return runtimeSequence;
    }

    private void EnsureVisual()
    {
        if (!autoCreateVisual)
        {
            return;
        }

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (renderer.sprite == null)
        {
            renderer.sprite = GetOrCreateFallbackSprite();
        }

        renderer.color = npcColor;
        renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 55);
    }

    private static Sprite GetOrCreateFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        const int size = 24;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        int center = size / 2;
        int radius = Mathf.Max(1, center - 2);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - center);
                int dy = Mathf.Abs(y - center);
                bool inside = (dx + dy) <= radius;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        fallbackSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);

        return fallbackSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.05f, interactionDistance));
    }
}
