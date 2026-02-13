using System;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelEnemyController : MonoBehaviour, IDamageable
{
    public enum EnemyType
    {
        TypeA,
        TypeB,
        TypeC,
        TypeD
    }

    [Serializable]
    public struct SpawnConfig
    {
        public EnemyType type;
        public float moveSpeed;
        public float maxHealth;
        public float fireInterval;
        public float bulletSpeed;
        public float bulletLifetime;
        public int scoreOnKill;
        public bool forceRadialOnly;
    }

    [Header("Runtime")]
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private float moveSpeed = 1.3f;
    [SerializeField] private float maxHealth = 8f;
    [SerializeField] private float fireInterval = 1.6f;
    [SerializeField] private float bulletSpeed = 3f;
    [SerializeField] private float bulletLifetime = 7f;
    [SerializeField] private int scoreOnKill = 600;
    [SerializeField] private bool forceRadialOnly;
    [SerializeField] private float despawnLeftX = -9.5f;

    private BulletPatternSystem bulletPatternSystem;
    private Action<LevelEnemyController> onReturnedToPool;
    private float currentHealth;
    private float nextShotTime;
    private int shotCounter;
    private bool initialized;
    private bool despawning;

    private void Awake()
    {
        EnsurePhysicsAndVisual();
    }

    private void OnEnable()
    {
        currentHealth = Mathf.Max(1f, maxHealth);
        nextShotTime = Time.time + UnityEngine.Random.Range(0.2f, 0.8f);
        shotCounter = 0;
        despawning = false;
        EnsurePhysicsAndVisual();
    }

    public void Configure(SpawnConfig config, BulletPatternSystem patternSystem, Action<LevelEnemyController> onDespawn)
    {
        enemyType = config.type;
        moveSpeed = Mathf.Max(0.05f, config.moveSpeed);
        maxHealth = Mathf.Max(1f, config.maxHealth);
        fireInterval = Mathf.Max(0.08f, config.fireInterval);
        bulletSpeed = Mathf.Max(0.2f, config.bulletSpeed);
        bulletLifetime = Mathf.Max(0.2f, config.bulletLifetime);
        scoreOnKill = Mathf.Max(0, config.scoreOnKill);
        forceRadialOnly = config.forceRadialOnly;

        bulletPatternSystem = patternSystem;
        onReturnedToPool = onDespawn;
        currentHealth = maxHealth;
        nextShotTime = Time.time + UnityEngine.Random.Range(0.2f, 0.8f);
        shotCounter = 0;
        despawning = false;
        initialized = true;

        EnsurePhysicsAndVisual();
    }

    private void Update()
    {
        if (!initialized || despawning)
        {
            return;
        }

        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
        if (transform.position.x < despawnLeftX)
        {
            ReturnToPool();
            return;
        }

        if (bulletPatternSystem == null || Time.time < nextShotTime)
        {
            return;
        }

        nextShotTime = Time.time + fireInterval;
        FireByType();
        shotCounter++;
    }

    public void TakeDamage(float amount)
    {
        if (!initialized || despawning || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth > 0f)
        {
            return;
        }

        if (ScoreSystem.Instance != null && scoreOnKill > 0)
        {
            ScoreSystem.Instance.AddScore(scoreOnKill);
        }

        ReturnToPool();
    }

    private void FireByType()
    {
        Vector2 origin = transform.position;
        Vector2 forward = Vector2.left;
        switch (enemyType)
        {
            case EnemyType.TypeA:
                bulletPatternSystem.FireStraight(origin, forward, bulletSpeed, bulletLifetime);
                break;

            case EnemyType.TypeB:
                bulletPatternSystem.FireSpread(origin, forward, 3, 34f, bulletSpeed, bulletLifetime);
                break;

            case EnemyType.TypeC:
                bulletPatternSystem.FireRadial(origin, 8, bulletSpeed, bulletLifetime, shotCounter * 7f);
                break;

            case EnemyType.TypeD:
                bool useRadial = forceRadialOnly || (shotCounter % 2 == 1);
                if (useRadial)
                {
                    bulletPatternSystem.FireRadial(origin, 12, bulletSpeed, bulletLifetime, shotCounter * 11f);
                }
                else
                {
                    bulletPatternSystem.FireSpread(origin, forward, 5, 52f, bulletSpeed, bulletLifetime);
                }
                break;
        }
    }

    private void ReturnToPool()
    {
        if (despawning)
        {
            return;
        }

        despawning = true;
        onReturnedToPool?.Invoke(this);

        if (TryGetComponent<PooledObject>(out PooledObject pooledObject))
        {
            pooledObject.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void EnsurePhysicsAndVisual()
    {
        if (gameObject.tag != "Enemy")
        {
            gameObject.tag = "Enemy";
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
        }

        col.isTrigger = true;
        if (col is CircleCollider2D circle)
        {
            circle.radius = 0.2f;
        }

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (renderer.sprite == null)
        {
            try
            {
                renderer.sprite = RuntimeSpriteLibrary.GetDiamondSprite();
            }
            catch
            {
                renderer.sprite = null;
            }
        }

        renderer.sortingOrder = Mathf.Max(70, renderer.sortingOrder);
        renderer.color = enemyType switch
        {
            EnemyType.TypeA => new Color(1f, 0.75f, 0.75f, 1f),
            EnemyType.TypeB => new Color(1f, 0.55f, 0.55f, 1f),
            EnemyType.TypeC => new Color(1f, 0.35f, 0.35f, 1f),
            EnemyType.TypeD => new Color(0.95f, 0.2f, 0.2f, 1f),
            _ => Color.white
        };
    }
}
