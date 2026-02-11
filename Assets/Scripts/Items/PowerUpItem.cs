using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PowerUpItem : MonoBehaviour
{
    [Header("Power")]
    [SerializeField] private int powerAmount = 1;

    [Header("Movement")]
    [SerializeField] private float fallSpeed = 1.5f;
    [SerializeField] private float lifeTime = 15f;

    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 0.35f;
    [SerializeField] private float magnetRadius = 1.5f;
    [SerializeField] private float magnetSpeed = 7f;

    [Header("Visual")]
    [SerializeField] private bool autoCreateVisual = true;
    [SerializeField] private Color itemColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private float visualScale = 0.2f;

    private static Sprite fallbackSprite;
    private float despawnTime;
    private Rigidbody2D rb2D;
    private PlayerController cachedPlayer;

    private void Awake()
    {
        EnsureTriggerCollider();
        EnsurePhysicsSetup();

        if (autoCreateVisual)
        {
            EnsureVisual();
        }
    }

    private void OnEnable()
    {
        despawnTime = Time.time + lifeTime;
    }

    private void Update()
    {
        if (TryAutoCollect())
        {
            return;
        }

        Vector2 move = Vector2.down * fallSpeed * Time.deltaTime;

        if (cachedPlayer == null || !cachedPlayer.gameObject.activeInHierarchy)
        {
            cachedPlayer = FindAnyObjectByType<PlayerController>();
        }

        if (cachedPlayer != null)
        {
            Vector2 toPlayer = (Vector2)(cachedPlayer.transform.position - transform.position);
            float distance = toPlayer.magnitude;
            if (distance <= magnetRadius && distance > pickupRadius)
            {
                move += toPlayer.normalized * magnetSpeed * Time.deltaTime;
            }
        }

        if (rb2D != null && rb2D.simulated)
        {
            rb2D.MovePosition(rb2D.position + move);
        }
        else
        {
            transform.position += (Vector3)move;
        }

        if (Time.time >= despawnTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private bool TryAutoCollect()
    {
        if (cachedPlayer == null || !cachedPlayer.gameObject.activeInHierarchy)
        {
            cachedPlayer = FindAnyObjectByType<PlayerController>();
            if (cachedPlayer == null)
            {
                return false;
            }
        }

        float distance = Vector2.Distance(transform.position, cachedPlayer.transform.position);
        if (distance > pickupRadius)
        {
            return false;
        }

        Collect(cachedPlayer);
        return true;
    }

private bool TryCollect(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerController>();
        }

        if (player == null)
        {
            return false;
        }

        Collect(player);
        return true;
    }

    private void Collect(PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        player.CollectPowerItem(powerAmount);
        ScoreSystem.Instance?.AddScore(500, applyMultiplier: false);
        Destroy(gameObject);
    }

    private void EnsureTriggerCollider()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        collider2D.isTrigger = true;

        if (collider2D is CircleCollider2D circle && circle.radius <= 0f)
        {
            circle.radius = 0.16f;
        }
    }

    private void EnsurePhysicsSetup()
    {
        rb2D = GetComponent<Rigidbody2D>();
        if (rb2D == null)
        {
            rb2D = gameObject.AddComponent<Rigidbody2D>();
        }

        rb2D.bodyType = RigidbodyType2D.Kinematic;
        rb2D.gravityScale = 0f;
        rb2D.simulated = true;
    }

    private void EnsureVisual()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = GetOrCreateFallbackSprite();
        }

        spriteRenderer.color = itemColor;
        spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 130);
        transform.localScale = new Vector3(visualScale, visualScale, 1f);
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
                bool insideDiamond = (dx + dy) <= radius;
                texture.SetPixel(x, y, insideDiamond ? Color.white : Color.clear);
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
}

