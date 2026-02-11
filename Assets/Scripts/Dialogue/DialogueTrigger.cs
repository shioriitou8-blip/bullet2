using UnityEngine;

[DisallowMultipleComponent]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueSequence sequence;
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool oneShot = true;

    [Header("Interaction")]
    [SerializeField] private bool allowInteraction = true;
    [SerializeField] private bool requirePlayerInRange = true;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 1.5f;
    [SerializeField] private Transform playerOverride;

    private Transform cachedPlayer;
    private bool consumed;

    private void Start()
    {
        if (playOnStart)
        {
            TryPlayDialogue();
        }
    }

    private void Update()
    {
        if (!allowInteraction)
        {
            return;
        }

        if (oneShot && consumed)
        {
            return;
        }

        if (DialogueManager.IsDialogueActive)
        {
            return;
        }

        if (!InputBridge.WasKeyPressedThisFrame(interactKey))
        {
            return;
        }

        if (requirePlayerInRange && !IsPlayerInRange())
        {
            return;
        }

        TryPlayDialogue();
    }

    public bool TriggerNow()
    {
        return TryPlayDialogue();
    }

    private bool TryPlayDialogue()
    {
        if (sequence == null)
        {
            return false;
        }

        if (oneShot && consumed)
        {
            return false;
        }

        bool started = DialogueManager.Play(sequence);
        if (started && oneShot)
        {
            consumed = true;
        }

        return started;
    }

    private bool IsPlayerInRange()
    {
        Transform player = ResolvePlayer();
        if (player == null)
        {
            return false;
        }

        float maxDistance = Mathf.Max(0.01f, interactionDistance);
        return Vector2.Distance(player.position, transform.position) <= maxDistance;
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

    private void OnDrawGizmosSelected()
    {
        if (!requirePlayerInRange)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, interactionDistance));
    }
}
