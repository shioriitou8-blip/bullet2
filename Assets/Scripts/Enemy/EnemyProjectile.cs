using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 4.5f;
    [SerializeField] private float lifetime = 6f;

    [Header("Visual")]
    [SerializeField] private Color projectileColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private float fallbackVisualScale = 0.11f;

    private static Sprite fallbackSprite;
    private Vector2 direction = Vector2.down;
    private float despawnTime;

    private void Awake()
    {
        EnsurePhysicsSetup();
        EnsureVisual();

        if (gameObject.tag != "EnemyBullet")
        {
            gameObject.tag = "EnemyBullet";
        }

        despawnTime = Time.time + lifetime;
    }

    public void Initialize(Vector2 shotDirection, float shotSpeed, float shotLifetime)
    {
        direction = shotDirection.sqrMagnitude > 0f ? shotDirection.normalized : Vector2.down;
        speed = shotSpeed;
        lifetime = Mathf.Max(0.1f, shotLifetime);
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
        if (!other.TryGetComponent<PlayerController>(out PlayerController playerController))
        {
            return;
        }

        playerController.ReceiveEnemyHit();
        Destroy(gameObject);
    }

    private void EnsurePhysicsSetup()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (col is CircleCollider2D circle && circle.radius <= 0f)
        {
            circle.radius = 0.06f;
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
        spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 90);

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
        float radius = size * 0.37f;

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
        texture.name = "EnemyProjectileRuntime";

        fallbackSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);

        return fallbackSprite;
    }
}
