using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    private class EnemyBaseStats
    {
        public LevelEnemyController.EnemyType type;
        public float maxHealth = 8f;
        public float moveSpeed = 1.5f;
        public float fireInterval = 1.4f;
        public float bulletSpeed = 3f;
        public float bulletLifetime = 7f;
        public int scoreOnKill = 500;
        public int preloadCount = 12;
    }

    [Header("References")]
    [SerializeField] private ObjectPool objectPool;
    [SerializeField] private BulletPatternSystem bulletPatternSystem;
    [SerializeField] private Transform playerTarget;

    [Header("Pool Keys")]
    [SerializeField] private string enemyBulletPoolKey = "EnemyBullet";
    [SerializeField] private string enemyTypeAPoolKey = "EnemyA";
    [SerializeField] private string enemyTypeBPoolKey = "EnemyB";
    [SerializeField] private string enemyTypeCPoolKey = "EnemyC";
    [SerializeField] private string enemyTypeDPoolKey = "EnemyD";
    [SerializeField] private string miniBossPoolKey = "MiniBoss";

    [Header("Base Stats")]
    [SerializeField] private EnemyBaseStats typeAStats = new EnemyBaseStats
    {
        type = LevelEnemyController.EnemyType.TypeA,
        maxHealth = 7f,
        moveSpeed = 1.75f,
        fireInterval = 1.9f,
        bulletSpeed = 2.8f,
        bulletLifetime = 7f,
        scoreOnKill = 450,
        preloadCount = 20
    };

    [SerializeField] private EnemyBaseStats typeBStats = new EnemyBaseStats
    {
        type = LevelEnemyController.EnemyType.TypeB,
        maxHealth = 11f,
        moveSpeed = 1.55f,
        fireInterval = 1.7f,
        bulletSpeed = 3.1f,
        bulletLifetime = 7f,
        scoreOnKill = 650,
        preloadCount = 16
    };

    [SerializeField] private EnemyBaseStats typeCStats = new EnemyBaseStats
    {
        type = LevelEnemyController.EnemyType.TypeC,
        maxHealth = 14f,
        moveSpeed = 1.35f,
        fireInterval = 1.55f,
        bulletSpeed = 2.75f,
        bulletLifetime = 8f,
        scoreOnKill = 900,
        preloadCount = 16
    };

    [SerializeField] private EnemyBaseStats typeDStats = new EnemyBaseStats
    {
        type = LevelEnemyController.EnemyType.TypeD,
        maxHealth = 18f,
        moveSpeed = 1.25f,
        fireInterval = 1.2f,
        bulletSpeed = 3.5f,
        bulletLifetime = 8f,
        scoreOnKill = 1300,
        preloadCount = 14
    };

    private readonly Dictionary<LevelEnemyController.EnemyType, EnemyBaseStats> baseStatsByType =
        new Dictionary<LevelEnemyController.EnemyType, EnemyBaseStats>();

    private readonly HashSet<LevelEnemyController> activeEnemies = new HashSet<LevelEnemyController>();
    private MiniBossController activeMiniBoss;

    private void Awake()
    {
        if (objectPool == null)
        {
            objectPool = FindAnyObjectByType<ObjectPool>();
        }

        if (bulletPatternSystem == null)
        {
            bulletPatternSystem = FindAnyObjectByType<BulletPatternSystem>();
        }

        if (playerTarget == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        CacheBaseStats();
        EnsurePoolRegistrations();
    }

    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
    }

    public LevelEnemyController SpawnEnemy(
        LevelEnemyController.EnemyType type,
        Vector2 position,
        float healthScale = 1f,
        float moveSpeedScale = 1f,
        float fireRateScale = 1f,
        float bulletSpeedScale = 1f,
        bool forceRadialOnly = false)
    {
        if (objectPool == null || bulletPatternSystem == null || !baseStatsByType.TryGetValue(type, out EnemyBaseStats baseStats))
        {
            return null;
        }

        string poolKey = GetPoolKey(type);
        GameObject enemyObject = objectPool.Spawn(poolKey, position, Quaternion.identity);
        if (enemyObject == null)
        {
            return null;
        }

        if (!enemyObject.TryGetComponent<LevelEnemyController>(out LevelEnemyController enemy))
        {
            enemy = enemyObject.AddComponent<LevelEnemyController>();
        }

        LevelEnemyController.SpawnConfig config = new LevelEnemyController.SpawnConfig
        {
            type = type,
            maxHealth = baseStats.maxHealth * Mathf.Max(0.1f, healthScale),
            moveSpeed = baseStats.moveSpeed * Mathf.Max(0.1f, moveSpeedScale),
            fireInterval = baseStats.fireInterval / Mathf.Max(0.1f, fireRateScale),
            bulletSpeed = baseStats.bulletSpeed * Mathf.Max(0.1f, bulletSpeedScale),
            bulletLifetime = baseStats.bulletLifetime,
            scoreOnKill = baseStats.scoreOnKill,
            forceRadialOnly = forceRadialOnly
        };

        enemy.Configure(config, bulletPatternSystem, HandleEnemyReturnedToPool);
        activeEnemies.Add(enemy);
        return enemy;
    }

    public MiniBossController SpawnMiniBoss(Vector2 position, Action<MiniBossController> onDefeated)
    {
        if (objectPool == null)
        {
            return null;
        }

        if (activeMiniBoss != null && activeMiniBoss.gameObject.activeInHierarchy)
        {
            return activeMiniBoss;
        }

        GameObject bossObject = objectPool.Spawn(miniBossPoolKey, position, Quaternion.identity);
        if (bossObject == null)
        {
            return null;
        }

        if (!bossObject.TryGetComponent<MiniBossController>(out MiniBossController miniBoss))
        {
            miniBoss = bossObject.AddComponent<MiniBossController>();
        }

        miniBoss.OnMiniBossDefeated -= HandleMiniBossDefeated;
        miniBoss.OnMiniBossDefeated += HandleMiniBossDefeated;
        miniBoss.Configure(bulletPatternSystem, playerTarget, _ => { });

        if (onDefeated != null)
        {
            miniBoss.OnMiniBossDefeated += onDefeated;
        }

        activeMiniBoss = miniBoss;
        return miniBoss;
    }

    public void DespawnAllActiveEnemies()
    {
        LevelEnemyController[] enemies = new LevelEnemyController[activeEnemies.Count];
        activeEnemies.CopyTo(enemies);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].gameObject.activeInHierarchy)
            {
                if (enemies[i].TryGetComponent<PooledObject>(out PooledObject pooled))
                {
                    pooled.ReturnToPool();
                }
                else
                {
                    enemies[i].gameObject.SetActive(false);
                }
            }
        }

        activeEnemies.Clear();
    }

    public void ClearAllPooledObjects()
    {
        DespawnAllActiveEnemies();
        if (activeMiniBoss != null && activeMiniBoss.gameObject.activeInHierarchy)
        {
            if (activeMiniBoss.TryGetComponent<PooledObject>(out PooledObject pooled))
            {
                pooled.ReturnToPool();
            }
            else
            {
                activeMiniBoss.gameObject.SetActive(false);
            }
        }

        activeMiniBoss = null;
        objectPool?.DespawnAll();
    }

    private void HandleEnemyReturnedToPool(LevelEnemyController enemy)
    {
        if (enemy != null)
        {
            activeEnemies.Remove(enemy);
        }
    }

    private void HandleMiniBossDefeated(MiniBossController boss)
    {
        if (activeMiniBoss == boss)
        {
            activeMiniBoss = null;
        }
    }

    private void CacheBaseStats()
    {
        baseStatsByType.Clear();
        baseStatsByType[LevelEnemyController.EnemyType.TypeA] = typeAStats;
        baseStatsByType[LevelEnemyController.EnemyType.TypeB] = typeBStats;
        baseStatsByType[LevelEnemyController.EnemyType.TypeC] = typeCStats;
        baseStatsByType[LevelEnemyController.EnemyType.TypeD] = typeDStats;
    }

    private void EnsurePoolRegistrations()
    {
        if (objectPool == null)
        {
            return;
        }

        RegisterPoolIfNeeded(enemyBulletPoolKey, CreateBulletPrefab(), 180, true);
        RegisterPoolIfNeeded(enemyTypeAPoolKey, CreateEnemyPrefab("Enemy_TypeA"), typeAStats.preloadCount, true);
        RegisterPoolIfNeeded(enemyTypeBPoolKey, CreateEnemyPrefab("Enemy_TypeB"), typeBStats.preloadCount, true);
        RegisterPoolIfNeeded(enemyTypeCPoolKey, CreateEnemyPrefab("Enemy_TypeC"), typeCStats.preloadCount, true);
        RegisterPoolIfNeeded(enemyTypeDPoolKey, CreateEnemyPrefab("Enemy_TypeD"), typeDStats.preloadCount, true);
        RegisterPoolIfNeeded(miniBossPoolKey, CreateMiniBossPrefab(), 1, false);
    }

    private void RegisterPoolIfNeeded(string key, GameObject prefab, int preloadCount, bool expandable)
    {
        if (prefab == null || string.IsNullOrWhiteSpace(key) || objectPool.HasPool(key))
        {
            return;
        }

        objectPool.RegisterRuntimePool(key, prefab, preloadCount, expandable);
    }

    private GameObject CreateBulletPrefab()
    {
        GameObject prefab = new GameObject("Pooled_EnemyBullet");
        prefab.SetActive(false);

        CircleCollider2D circle = prefab.GetComponent<CircleCollider2D>();
        if (circle == null)
        {
            circle = prefab.AddComponent<CircleCollider2D>();
        }

        circle.isTrigger = true;

        Rigidbody2D rb = prefab.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = prefab.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (prefab.GetComponent<LevelBullet>() == null)
        {
            prefab.AddComponent<LevelBullet>();
        }

        if (prefab.GetComponent<SpriteRenderer>() == null)
        {
            prefab.AddComponent<SpriteRenderer>();
        }

        return prefab;
    }

    private GameObject CreateEnemyPrefab(string prefabName)
    {
        GameObject prefab = new GameObject(prefabName);
        prefab.SetActive(false);

        CircleCollider2D circle = prefab.GetComponent<CircleCollider2D>();
        if (circle == null)
        {
            circle = prefab.AddComponent<CircleCollider2D>();
        }

        circle.isTrigger = true;

        if (prefab.GetComponent<SpriteRenderer>() == null)
        {
            prefab.AddComponent<SpriteRenderer>();
        }

        if (prefab.GetComponent<LevelEnemyController>() == null)
        {
            prefab.AddComponent<LevelEnemyController>();
        }

        return prefab;
    }

    private GameObject CreateMiniBossPrefab()
    {
        GameObject prefab = new GameObject("Pooled_MiniBoss");
        prefab.SetActive(false);

        CircleCollider2D collider = prefab.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = prefab.AddComponent<CircleCollider2D>();
        }

        collider.isTrigger = true;
        collider.radius = 0.48f;

        if (prefab.GetComponent<SpriteRenderer>() == null)
        {
            prefab.AddComponent<SpriteRenderer>();
        }

        if (prefab.GetComponent<MiniBossController>() == null)
        {
            prefab.AddComponent<MiniBossController>();
        }

        return prefab;
    }

    private string GetPoolKey(LevelEnemyController.EnemyType type)
    {
        return type switch
        {
            LevelEnemyController.EnemyType.TypeA => enemyTypeAPoolKey,
            LevelEnemyController.EnemyType.TypeB => enemyTypeBPoolKey,
            LevelEnemyController.EnemyType.TypeC => enemyTypeCPoolKey,
            LevelEnemyController.EnemyType.TypeD => enemyTypeDPoolKey,
            _ => enemyTypeAPoolKey
        };
    }
}
