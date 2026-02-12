using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class LevelBullet : MonoBehaviour
{
    [SerializeField] private bool fromEnemy = true;
    [SerializeField] private float speed = 4f;
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float collisionRadius = 0.06f;
    [SerializeField] private Color enemyColor = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color allyColor = new Color(0.6f, 0.9f, 1f, 1f);

    private static Sprite fallbackSprite;

    private Vector2 direction = Vector2.down;
    private float despawnTime;
    private PooledObject pooledObject;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
        EnsurePhysicsSetup();
        EnsureVisual();
    }

    private void OnEnable()
    {
        despawnTime = Time.time + Mathf.Max(0.05f, lifetime);
    }

    public void Initialize(Vector2 moveDirection, float moveSpeed, float bulletLifetime, bool enemyOwned)
    {
        direction = moveDirection.sqrMagnitude > 0f ? moveDirection.normalized : Vector2.down;
        speed = Mathf.Max(0.1f, moveSpeed);
        lifetime = Mathf.Max(0.05f, bulletLifetime);
        fromEnemy = enemyOwned;
        despawnTime = Time.time + lifetime;
        EnsureVisual();
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (Time.time >= despawnTime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (fromEnemy)
        {
            if (!other.TryGetComponent<PlayerController>(out PlayerController playerController))
            {
                return;
            }

            playerController.ReceiveEnemyHit();
            ReturnToPool();
            return;
        }

        if (!other.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            return;
        }

        damageable.TakeDamage(1f);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (pooledObject != null)
        {
            pooledObject.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void EnsurePhysicsSetup()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
        if (col is CircleCollider2D circle)
        {
            circle.radius = Mathf.Max(0.01f, collisionRadius);
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (gameObject.tag != "EnemyBullet")
        {
            gameObject.tag = "EnemyBullet";
        }
    }

    private void EnsureVisual()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (renderer.sprite == null)
        {
            renderer.sprite = GetOrCreateFallbackSprite();
        }

        renderer.color = fromEnemy ? enemyColor : allyColor;
        renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 110);

        float scale = fromEnemy ? 0.11f : 0.1f;
        transform.localScale = new Vector3(scale, scale, 1f);
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
        float radius = size * 0.36f;
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
        texture.name = "LevelBulletRuntime";
        fallbackSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);

        return fallbackSprite;
    }
}
