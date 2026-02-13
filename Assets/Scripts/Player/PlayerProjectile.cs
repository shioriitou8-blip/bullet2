using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 16f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float lifetime = 3f;
    
    [Header("Visual")]
    [SerializeField] private Color projectileColor = new Color(0.6f, 0.9f, 1f, 1f);
    [SerializeField] private float fallbackVisualScale = 0.1f;

    private static Sprite fallbackSprite;
[SerializeField] private int hitScore = 150;

    private Vector2 direction = Vector2.up;
    private LayerMask enemyMask;
    private Transform ownerTransform;
    private float pointBlankDistance = 1.25f;
    private float despawnTime;

private void Awake()
    {
        EnsurePhysicsSetup();
        EnsureVisual();
        despawnTime = Time.time + lifetime;
    }

    private void OnEnable()
    {
        RuntimeSpawnGroups.MoveToPlayerBullets(transform);
    }

public void Initialize(
        Vector2 shotDirection,
        float shotSpeed,
        float shotDamage,
        LayerMask targetMask,
        Transform owner,
        float shotLifetime = 3f,
        float pointBlankMaxDistance = 1.25f)
    {
        direction = shotDirection.sqrMagnitude > 0f ? shotDirection.normalized : Vector2.up;
        speed = shotSpeed;
        damage = shotDamage;
        enemyMask = targetMask;
        ownerTransform = owner;
        lifetime = shotLifetime;
        pointBlankDistance = pointBlankMaxDistance;
        despawnTime = Time.time + lifetime;
        EnsurePhysicsSetup();
        EnsureVisual();
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Time.time >= despawnTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsTarget(other))
        {
            return;
        }

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
        }
        else
        {
            other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        if (ScoreSystem.Instance != null)
        {
            ScoreSystem.Instance.AddScore(hitScore);
            if (ownerTransform != null)
            {
                float distance = Vector2.Distance(ownerTransform.position, transform.position);
                ScoreSystem.Instance.AddPointBlankBonus(distance, pointBlankDistance);
            }
        }

        Destroy(gameObject);
    }

    private bool IsTarget(Collider2D other)
    {
        bool inMask = (enemyMask.value & (1 << other.gameObject.layer)) != 0;
        return inMask || other.CompareTag("Enemy");
    }

    private void EnsureVisual()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        bool createdRenderer = false;

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            createdRenderer = true;
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = GetOrCreateFallbackSprite();
            createdRenderer = true;
        }

        spriteRenderer.color = projectileColor;
        spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 120);

        if (createdRenderer)
        {
            transform.localScale = new Vector3(fallbackVisualScale, fallbackVisualScale, 1f);
        }
    }

    private static Sprite GetOrCreateFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        float center = (size - 1) * 0.5f;
        float radius = size * 0.38f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                bool inside = (dx * dx) + (dy * dy) <= radius * radius;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        texture.name = "PlayerProjectileRuntime";

        fallbackSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);

        return fallbackSprite;
    }

private void EnsurePhysicsSetup()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (col is CircleCollider2D circle && circle.radius <= 0f)
        {
            circle.radius = 0.05f;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
}
