using UnityEngine;

[DisallowMultipleComponent]
public class BulletPatternSystem : MonoBehaviour
{
    [SerializeField] private ObjectPool objectPool;
    [SerializeField] private string enemyBulletPoolKey = "EnemyBullet";
    [SerializeField] private float bulletSpeedMultiplier = 1f;
    [SerializeField] private float bulletCountMultiplier = 1f;

    public float BulletSpeedMultiplier
    {
        get => bulletSpeedMultiplier;
        set => bulletSpeedMultiplier = Mathf.Max(0.1f, value);
    }

    public float BulletCountMultiplier
    {
        get => bulletCountMultiplier;
        set => bulletCountMultiplier = Mathf.Max(0.3f, value);
    }

    private void Awake()
    {
        if (objectPool == null)
        {
            objectPool = FindAnyObjectByType<ObjectPool>();
        }
    }

    public void ConfigureDifficulty(float speedMultiplier, float countMultiplier)
    {
        BulletSpeedMultiplier = speedMultiplier;
        BulletCountMultiplier = countMultiplier;
    }

    public void FireStraight(Vector2 origin, Vector2 direction, float speed, float lifetime = 6f)
    {
        SpawnBullet(origin, direction, speed, lifetime);
    }

    public void FireAimedAtTarget(Vector2 origin, Transform target, float speed, float lifetime = 6f)
    {
        Vector2 direction = Vector2.down;
        if (target != null)
        {
            direction = ((Vector2)target.position - origin).normalized;
        }

        SpawnBullet(origin, direction, speed, lifetime);
    }

    public void FireSpread(
        Vector2 origin,
        Vector2 baseDirection,
        int baseCount,
        float spreadAngle,
        float speed,
        float lifetime = 6f)
    {
        int count = ScaleCount(baseCount);
        if (count <= 1)
        {
            SpawnBullet(origin, baseDirection, speed, lifetime);
            return;
        }

        float start = -spreadAngle * 0.5f;
        float step = spreadAngle / Mathf.Max(1f, count - 1f);
        for (int i = 0; i < count; i++)
        {
            float angle = start + (step * i);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * baseDirection.normalized;
            SpawnBullet(origin, direction, speed, lifetime);
        }
    }

    public void FireRadial(Vector2 origin, int baseCount, float speed, float lifetime = 6f, float angleOffset = 0f)
    {
        int count = Mathf.Max(1, ScaleCount(baseCount));
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = angleOffset + (step * i);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
            SpawnBullet(origin, direction, speed, lifetime);
        }
    }

    public void FireSpiralStep(
        Vector2 origin,
        ref float currentAngle,
        float stepDegrees,
        int bulletsPerStep,
        float speed,
        float lifetime = 6f)
    {
        int count = Mathf.Max(1, ScaleCount(bulletsPerStep));
        float slice = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = currentAngle + (slice * i);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
            SpawnBullet(origin, direction, speed, lifetime);
        }

        currentAngle += stepDegrees;
    }

    private void SpawnBullet(Vector2 origin, Vector2 direction, float speed, float lifetime)
    {
        if (objectPool == null)
        {
            objectPool = FindAnyObjectByType<ObjectPool>();
            if (objectPool == null)
            {
                return;
            }
        }

        GameObject bullet = objectPool.Spawn(enemyBulletPoolKey, origin, Quaternion.identity);
        if (bullet == null)
        {
            return;
        }

        if (!bullet.TryGetComponent<LevelBullet>(out LevelBullet levelBullet))
        {
            levelBullet = bullet.AddComponent<LevelBullet>();
        }

        float finalSpeed = Mathf.Max(0.1f, speed * bulletSpeedMultiplier);
        levelBullet.Initialize(direction, finalSpeed, lifetime, enemyOwned: true);
    }

    private int ScaleCount(int baseCount)
    {
        int safeBaseCount = Mathf.Max(1, baseCount);
        return Mathf.Max(1, Mathf.RoundToInt(safeBaseCount * BulletCountMultiplier));
    }
}
