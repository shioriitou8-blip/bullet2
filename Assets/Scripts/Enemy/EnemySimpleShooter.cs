using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class EnemySimpleShooter : MonoBehaviour, IDamageable
{
    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float initialDelay = 0.75f;
    [SerializeField] private float fireInterval = 1.15f;
    [SerializeField] private int projectileCount = 3;
    [SerializeField] private float spreadAngle = 30f;
    [SerializeField] private float projectileSpeed = 4.5f;
    [SerializeField] private float projectileLifetime = 6f;
    [SerializeField] private float spawnOffsetUnits = 0.2f;

    [Header("Visual")]
    [SerializeField] private bool autoCreateVisual = true;
    [SerializeField] private Color enemyColor = new Color(1f, 0.65f, 0.65f, 1f);

    [Header("Health")]
    [SerializeField] private float maxHealth = 12f;
    [SerializeField] private int killScore = 1200;

    [Header("Boss Trigger")]
    [SerializeField] private BossThreePhaseController bossToTrigger;
    [SerializeField] private float bossEncounterDelay = 0.75f;

    private static Sprite fallbackEnemySprite;
    private float nextFireTime;
    private float currentHealth;
    private bool isDead;

    private void Reset()
    {
        EnsureVisual();
        EnsureCollider();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        maxHealth = Mathf.Max(1f, maxHealth);
        bossEncounterDelay = Mathf.Max(0f, bossEncounterDelay);
        currentHealth = maxHealth;
        EnsureVisual();
        EnsureCollider();
    }

    private void OnEnable()
    {
        EnsureVisual();
        EnsureCollider();

        if (bossToTrigger == null)
        {
            bossToTrigger = FindAnyObjectByType<BossThreePhaseController>();
        }

        if (Application.isPlaying)
        {
            currentHealth = Mathf.Max(1f, maxHealth);
            isDead = false;
            nextFireTime = Time.time + Mathf.Max(0f, initialDelay);
        }
    }

    private void Awake()
    {
        if (gameObject.tag != "Enemy")
        {
            gameObject.tag = "Enemy";
        }

        EnsureVisual();
        EnsureCollider();

        if (bossToTrigger == null)
        {
            bossToTrigger = FindAnyObjectByType<BossThreePhaseController>();
        }

        if (Application.isPlaying)
        {
            currentHealth = Mathf.Max(1f, maxHealth);
            isDead = false;
            nextFireTime = Time.time + Mathf.Max(0f, initialDelay);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (isDead)
        {
            return;
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + Mathf.Max(0.05f, fireInterval);
        FireSpread();
    }

    private void FireSpread()
    {
        int count = Mathf.Max(1, projectileCount);
        if (count == 1)
        {
            SpawnProjectileAtAngle(0f);
            return;
        }

        float startAngle = -spreadAngle * 0.5f;
        float step = spreadAngle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + (step * i);
            SpawnProjectileAtAngle(angle);
        }
    }

    private void SpawnProjectileAtAngle(float angleOffset)
    {
        Vector2 direction = Quaternion.Euler(0f, 0f, angleOffset) * Vector2.down;
        Vector3 spawnPosition = transform.position + (Vector3)(direction * spawnOffsetUnits);
        Transform bulletGroup = RuntimeSpawnGroups.GetEnemyBulletsGroup();

        GameObject projectile = projectilePrefab != null
            ? Instantiate(projectilePrefab, spawnPosition, Quaternion.identity, bulletGroup)
            : CreateFallbackProjectile(spawnPosition, bulletGroup);

        RuntimeSpawnGroups.MoveToEnemyBullets(projectile.transform);

        EnemyProjectile projectileLogic = projectile.GetComponent<EnemyProjectile>();
        if (projectileLogic == null)
        {
            projectileLogic = projectile.AddComponent<EnemyProjectile>();
        }

        projectileLogic.Initialize(direction, projectileSpeed, projectileLifetime);
    }

    public void TakeDamage(float amount)
    {
        if (!Application.isPlaying || isDead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth > 0f)
        {
            return;
        }

        Die();
    }

    private void TryTriggerBossEncounter()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (bossToTrigger == null)
        {
            bossToTrigger = FindAnyObjectByType<BossThreePhaseController>();
        }

        if (bossToTrigger != null)
        {
            bossToTrigger.QueueEncounterStart(bossEncounterDelay);
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        TryTriggerBossEncounter();

        if (ScoreSystem.Instance != null && killScore > 0)
        {
            ScoreSystem.Instance.AddScore(killScore);
        }

        Destroy(gameObject);
    }

    private GameObject CreateFallbackProjectile(Vector3 position, Transform parent)
    {
        GameObject bullet = new GameObject("EnemyProjectile");
        if (parent != null)
        {
            bullet.transform.SetParent(parent, false);
        }

        bullet.transform.position = position;

        CircleCollider2D collider = bullet.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.06f;

        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        return bullet;
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
            renderer.sprite = GetOrCreateEnemySprite();
        }

        renderer.enabled = true;
        renderer.color = enemyColor;
        renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 60);
    }

    private void EnsureCollider()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.18f;
            return;
        }

        col.isTrigger = true;
    }

    private static Sprite GetOrCreateEnemySprite()
    {
        if (fallbackEnemySprite != null)
        {
            return fallbackEnemySprite;
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
        texture.name = "EnemyRuntime";

        fallbackEnemySprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);

        return fallbackEnemySprite;
    }
}
