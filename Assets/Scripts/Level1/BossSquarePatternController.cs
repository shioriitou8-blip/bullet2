using UnityEngine;

[DisallowMultipleComponent]
public class BossSquarePatternController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Vector2 centerPosition = Vector2.zero;
    [SerializeField, Min(0.05f)] private float moveSpeed = 2.8f;
    [SerializeField, Min(0f)] private float centerArrivalThreshold = 0.06f;

    [Header("Shooting")]
    [SerializeField, Min(0.05f)] private float launchInterval = 1.2f;
    [SerializeField, Min(0.01f)] private float oppositeRotationDelay = 0.2f;
    [SerializeField, Min(3)] private int bulletsPerCircle = 16;
    [SerializeField, Min(0.1f)] private float bulletSpeed = 3.25f;
    [SerializeField, Min(0.1f)] private float bulletLifetime = 7f;
    [SerializeField] private float angleStepPerLaunch = 10f;
    [SerializeField] private float clockwiseCircleTurnRate = 36f;
    [SerializeField] private float counterClockwiseCircleTurnRate = -36f;

    [Header("Cross Lasers")]
    [SerializeField] private bool enableCrossLasers = true;
    [SerializeField] private Transform laserOriginPoint;
    [SerializeField, Min(0.1f)] private float laserInterval = 2.4f;
    [SerializeField, Min(1f)] private float laserSpinDegreesPerSecond = 160f;
    [SerializeField, Range(0.1f, 1f)] private float laserSpinSpeedMultiplier = 0.82f;
    [SerializeField, Min(0.05f)] private float laserGrowDuration = 2.8f;
    [SerializeField, Min(0f)] private float laserHoldDuration = 0.45f;
    [SerializeField, Min(0.05f)] private float laserShrinkDuration = 0.9f;
    [SerializeField, Min(0.5f)] private float laserLength = 10.5f;
    [SerializeField, Min(0.03f)] private float laserWidth = 0.24f;
    [SerializeField, Min(0.02f)] private float laserDamageInterval = 0.25f;
    [SerializeField] private Color laserColor = new Color(1f, 0.24f, 0.24f, 0.72f);
    [SerializeField] private int laserSortingOrder = 115;
    [SerializeField, Min(1f)] private float laserMaxRotationDegrees = 90f;

    [Header("Pool")]
    [SerializeField] private ObjectPool objectPool;
    [SerializeField] private string enemyBulletPoolKey = "EnemyBullet";
    [SerializeField, Min(0)] private int preloadBullets = 120;

    private sealed class LaserRuntime
    {
        public GameObject gameObject;
        public SpriteRenderer renderer;
        public BoxCollider2D collider;
        public Rigidbody2D rigidbody;
        public BossLaserBeam damage;
    }

    private enum LaserPhase
    {
        Inactive,
        Growing,
        Rotating,
        Holding,
        Shrinking
    }

    private static Sprite laserSprite;

    private float nextLaunchTime;
    private float nextLaserTime;
    private float laserRotatedDegrees;
    private float laserPhaseStartTime;
    private float laserCurrentLength;
    private float clockwiseOffset;
    private float counterClockwiseOffset;
    private bool reachedCenter;
    private bool waitingForOppositeBurst;
    private bool lasersActive;
    private LaserPhase laserPhase;

    private Transform laserRoot;
    private LaserRuntime[] laserRuntimes;

    private void Awake()
    {
        EnsurePoolReady();
        EnsureLaserObjects();
    }

    private void OnEnable()
    {
        reachedCenter = false;
        waitingForOppositeBurst = false;
        lasersActive = false;
        laserPhase = LaserPhase.Inactive;
        laserRotatedDegrees = 0f;
        laserCurrentLength = 0f;
        nextLaunchTime = Time.time + Mathf.Max(0.05f, launchInterval);
        nextLaserTime = Time.time + Mathf.Max(0.1f, laserInterval);
        UpdateLaserOrigin();
        ResetLaserRootRotation();
        ApplyLaserTransforms(0f);
        SetLasersActive(false);
    }

    private void OnDisable()
    {
        SetLasersActive(false);
        ResetLaserRootRotation();
    }

    private void Update()
    {
        if (!reachedCenter)
        {
            MoveToCenter();
            return;
        }

        HandleLaserSchedule();

        if (Time.time >= nextLaunchTime)
        {
            FireNextBurst();
        }
    }

    private void MoveToCenter()
    {
        Vector3 target = new Vector3(centerPosition.x, centerPosition.y, transform.position.z);
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            Mathf.Max(0.05f, moveSpeed) * Time.deltaTime);

        float sqrDistance = (transform.position - target).sqrMagnitude;
        float threshold = Mathf.Max(0f, centerArrivalThreshold);
        if (sqrDistance <= threshold * threshold)
        {
            transform.position = target;
            reachedCenter = true;
            waitingForOppositeBurst = false;
            nextLaunchTime = Time.time + Mathf.Max(0.05f, launchInterval);
            nextLaserTime = Time.time + Mathf.Max(0.1f, laserInterval);
        }
    }

    private void HandleLaserSchedule()
    {
        if (!enableCrossLasers)
        {
            if (lasersActive)
            {
                SetLasersActive(false);
                laserPhase = LaserPhase.Inactive;
                laserCurrentLength = 0f;
                ApplyLaserTransforms(0f);
            }

            ResetLaserRootRotation();
            return;
        }

        if (lasersActive)
        {
            UpdateActiveLasers();
            return;
        }

        if (Time.time >= nextLaserTime)
        {
            FireLaserCross();
        }
    }

    private void FireLaserCross()
    {
        if (!EnsureLaserObjects())
        {
            return;
        }

        UpdateLaserOrigin();
        ResetLaserRootRotation();
        laserPhase = LaserPhase.Growing;
        laserPhaseStartTime = Time.time;
        laserRotatedDegrees = 0f;
        laserCurrentLength = 0f;
        ApplyLaserTransforms(0f);
        SetLasersActive(true);
    }

    private void UpdateActiveLasers()
    {
        if (laserRoot == null)
        {
            return;
        }

        UpdateLaserOrigin();
        float maxLength = Mathf.Max(0.5f, laserLength);
        float nextLength = laserCurrentLength;

        if (laserPhase == LaserPhase.Growing)
        {
            float growDuration = Mathf.Max(0.05f, laserGrowDuration);
            float growT = Mathf.Clamp01((Time.time - laserPhaseStartTime) / growDuration);
            nextLength = maxLength * growT;
            if (growT >= 1f)
            {
                laserPhase = LaserPhase.Rotating;
                laserPhaseStartTime = Time.time;
            }
        }
        else if (laserPhase == LaserPhase.Rotating)
        {
            float safeDeltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            float spinSpeed = Mathf.Max(1f, Mathf.Abs(laserSpinDegreesPerSecond)) * Mathf.Clamp(laserSpinSpeedMultiplier, 0.1f, 1f);
            float step = spinSpeed * safeDeltaTime;
            laserRoot.Rotate(0f, 0f, step, Space.Self);
            laserRotatedDegrees += step;

            if (laserRotatedDegrees >= Mathf.Max(1f, laserMaxRotationDegrees))
            {
                laserPhase = LaserPhase.Holding;
                laserPhaseStartTime = Time.time;
            }
        }
        else if (laserPhase == LaserPhase.Holding)
        {
            if (Time.time - laserPhaseStartTime >= Mathf.Max(0f, laserHoldDuration))
            {
                laserPhase = LaserPhase.Shrinking;
                laserPhaseStartTime = Time.time;
            }
        }
        else if (laserPhase == LaserPhase.Shrinking)
        {
            float shrinkDuration = Mathf.Max(0.05f, laserShrinkDuration);
            float shrinkT = Mathf.Clamp01((Time.time - laserPhaseStartTime) / shrinkDuration);
            nextLength = maxLength * (1f - shrinkT);
            if (shrinkT >= 1f)
            {
                SetLasersActive(false);
                laserPhase = LaserPhase.Inactive;
                ResetLaserRootRotation();
                laserCurrentLength = 0f;
                ApplyLaserTransforms(0f);
                nextLaserTime = Time.time + Mathf.Max(0.1f, laserInterval);
                return;
            }
        }

        if (Mathf.Approximately(nextLength, laserCurrentLength))
        {
            return;
        }

        laserCurrentLength = nextLength;
        ApplyLaserTransforms(laserCurrentLength);
    }

    private void FireNextBurst()
    {
        if (!EnsurePoolReady() || string.IsNullOrWhiteSpace(enemyBulletPoolKey))
        {
            return;
        }

        int count = Mathf.Max(3, bulletsPerCircle);
        float speed = Mathf.Max(0.1f, bulletSpeed);
        float lifetime = Mathf.Max(0.1f, bulletLifetime);
        Vector2 origin = transform.position;

        if (!waitingForOppositeBurst)
        {
            FireCircle(origin, count, clockwiseOffset, speed, lifetime, clockwiseCircleTurnRate);
            clockwiseOffset += angleStepPerLaunch;
            waitingForOppositeBurst = true;
            nextLaunchTime = Time.time + Mathf.Max(0.01f, oppositeRotationDelay);
            return;
        }

        FireCircle(origin, count, counterClockwiseOffset, speed, lifetime, counterClockwiseCircleTurnRate);
        counterClockwiseOffset -= angleStepPerLaunch;
        waitingForOppositeBurst = false;
        nextLaunchTime = Time.time + Mathf.Max(0.05f, launchInterval);
    }

    private void FireCircle(
        Vector2 origin,
        int count,
        float angleOffset,
        float speed,
        float lifetime,
        float turnRate)
    {
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = angleOffset + (step * i);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
            SpawnBullet(origin, direction, speed, lifetime, turnRate);
        }
    }

    private void SpawnBullet(
        Vector2 origin,
        Vector2 direction,
        float speed,
        float lifetime,
        float turnRate)
    {
        GameObject bullet = objectPool.Spawn(
            enemyBulletPoolKey,
            origin,
            Quaternion.identity,
            RuntimeSpawnGroups.GetEnemyBulletsGroup());

        if (bullet == null)
        {
            return;
        }

        if (!bullet.TryGetComponent<LevelBullet>(out LevelBullet levelBullet))
        {
            levelBullet = bullet.AddComponent<LevelBullet>();
        }

        levelBullet.Initialize(
            direction,
            speed,
            lifetime,
            enemyOwned: true,
            turnRateDegrees: turnRate);
    }

    private bool EnsureLaserObjects()
    {
        if (laserRoot == null)
        {
            Transform existing = transform.Find("BossLasers");
            if (existing != null)
            {
                laserRoot = existing;
            }
            else
            {
                GameObject root = new GameObject("BossLasers");
                laserRoot = root.transform;
                laserRoot.SetParent(transform, false);
            }
        }

        if (laserRuntimes == null || laserRuntimes.Length != 4)
        {
            laserRuntimes = new LaserRuntime[4];
        }

        string[] names = { "LaserRight", "LaserLeft", "LaserUp", "LaserDown" };
        for (int i = 0; i < 4; i++)
        {
            if (laserRuntimes[i] != null && laserRuntimes[i].gameObject != null)
            {
                continue;
            }

            Transform existing = laserRoot.Find(names[i]);
            if (existing != null)
            {
                laserRuntimes[i] = BindLaserRuntime(existing.gameObject);
            }
            else
            {
                laserRuntimes[i] = CreateLaserRuntime(names[i]);
            }
        }

        ApplyLaserTransforms(laserCurrentLength);
        return true;
    }

    private LaserRuntime BindLaserRuntime(GameObject go)
    {
        LaserRuntime runtime = new LaserRuntime
        {
            gameObject = go,
            renderer = go.GetComponent<SpriteRenderer>(),
            collider = go.GetComponent<BoxCollider2D>(),
            rigidbody = go.GetComponent<Rigidbody2D>(),
            damage = go.GetComponent<BossLaserBeam>()
        };

        if (runtime.renderer == null)
        {
            runtime.renderer = go.AddComponent<SpriteRenderer>();
        }

        if (runtime.renderer.sprite == null)
        {
            runtime.renderer.sprite = GetOrCreateLaserSprite();
        }

        if (runtime.collider == null)
        {
            runtime.collider = go.AddComponent<BoxCollider2D>();
        }

        runtime.collider.isTrigger = true;
        runtime.collider.size = Vector2.one;

        if (runtime.rigidbody == null)
        {
            runtime.rigidbody = go.AddComponent<Rigidbody2D>();
        }

        runtime.rigidbody.bodyType = RigidbodyType2D.Kinematic;
        runtime.rigidbody.gravityScale = 0f;
        runtime.rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        runtime.rigidbody.simulated = true;

        if (runtime.damage == null)
        {
            runtime.damage = go.AddComponent<BossLaserBeam>();
        }

        runtime.damage.SetDamageInterval(laserDamageInterval);
        go.SetActive(false);
        return runtime;
    }

    private LaserRuntime CreateLaserRuntime(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(laserRoot, false);
        return BindLaserRuntime(go);
    }

    private void ApplyLaserTransforms(float lengthOverride = -1f)
    {
        if (laserRuntimes == null || laserRuntimes.Length != 4)
        {
            return;
        }

        float maxLength = Mathf.Max(0.5f, laserLength);
        float length = lengthOverride >= 0f ? Mathf.Clamp(lengthOverride, 0f, maxLength) : maxLength;
        float renderLength = Mathf.Max(0.001f, length);
        float halfLength = length * 0.5f;
        float width = Mathf.Max(0.03f, laserWidth);

        Vector2[] directions = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
        float[] zAngles = { 0f, 180f, 90f, -90f };

        for (int i = 0; i < 4; i++)
        {
            LaserRuntime runtime = laserRuntimes[i];
            if (runtime == null || runtime.gameObject == null)
            {
                continue;
            }

            Transform t = runtime.gameObject.transform;
            t.localPosition = (Vector3)(directions[i] * halfLength);
            t.localRotation = Quaternion.Euler(0f, 0f, zAngles[i]);
            t.localScale = new Vector3(renderLength, width, 1f);

            if (runtime.renderer != null)
            {
                runtime.renderer.color = laserColor;
                runtime.renderer.sortingOrder = laserSortingOrder;
                if (runtime.renderer.sprite == null)
                {
                    runtime.renderer.sprite = GetOrCreateLaserSprite();
                }
            }

            if (runtime.damage != null)
            {
                runtime.damage.SetDamageInterval(laserDamageInterval);
            }

            if (runtime.collider != null)
            {
                runtime.collider.enabled = length > 0.02f;
            }
        }
    }

    private void SetLasersActive(bool active)
    {
        lasersActive = active && enableCrossLasers;
        if (laserRuntimes == null)
        {
            return;
        }

        for (int i = 0; i < laserRuntimes.Length; i++)
        {
            LaserRuntime runtime = laserRuntimes[i];
            if (runtime == null || runtime.gameObject == null)
            {
                continue;
            }

            runtime.gameObject.SetActive(lasersActive);
        }
    }

    private void ResetLaserRootRotation()
    {
        if (laserRoot == null)
        {
            return;
        }

        laserRoot.localRotation = Quaternion.identity;
    }

    private void UpdateLaserOrigin()
    {
        if (laserRoot == null)
        {
            return;
        }

        if (laserOriginPoint != null)
        {
            laserRoot.position = laserOriginPoint.position;
            return;
        }

        laserRoot.localPosition = Vector3.zero;
    }

    private bool EnsurePoolReady()
    {
        if (objectPool == null)
        {
            objectPool = FindAnyObjectByType<ObjectPool>();
        }

        if (objectPool == null)
        {
            GameObject poolObject = new GameObject("BossObjectPool");
            objectPool = poolObject.AddComponent<ObjectPool>();
        }

        if (string.IsNullOrWhiteSpace(enemyBulletPoolKey))
        {
            enemyBulletPoolKey = "EnemyBullet";
        }

        if (objectPool.HasPool(enemyBulletPoolKey))
        {
            return true;
        }

        GameObject prefab = CreateBulletPrefab();
        if (prefab == null)
        {
            return false;
        }

        objectPool.RegisterRuntimePool(enemyBulletPoolKey, prefab, Mathf.Max(0, preloadBullets), true);
        return objectPool.HasPool(enemyBulletPoolKey);
    }

    private static GameObject CreateBulletPrefab()
    {
        GameObject prefab = new GameObject("Pooled_BossBullet");
        prefab.SetActive(false);

        CircleCollider2D circle = prefab.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.06f;

        Rigidbody2D rb = prefab.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        prefab.AddComponent<SpriteRenderer>();
        prefab.AddComponent<LevelBullet>();
        return prefab;
    }

    private static Sprite GetOrCreateLaserSprite()
    {
        if (laserSprite != null)
        {
            return laserSprite;
        }

        const int size = 4;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        texture.name = "BossLaserRuntime";
        laserSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);

        return laserSprite;
    }
}
