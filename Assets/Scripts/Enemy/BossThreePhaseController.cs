using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class BossThreePhaseController : MonoBehaviour, IDamageable
{
    private enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3,
        Defeated
    }

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileLifetime = 8f;
    [SerializeField] private float spawnRadius = 0.25f;

    [Header("Health Per Phase")]
    [SerializeField] private float phase1MaxHealth = 75f;
    [SerializeField] private float phase2MaxHealth = 95f;
    [SerializeField] private float phase3MaxHealth = 140f;
    [SerializeField] private float phaseTransitionInvulnerability = 1.25f;
    [SerializeField] private int killScore = 30000;

    [Header("Phase 1 - Rotating + Downward")]
    [SerializeField] private float lineInterval = 0.65f;
    [SerializeField] private int lineCount = 5;
    [SerializeField] private float lineSpacing = 0.7f;
    [SerializeField] private float lineSpeed = 5.4f;
    [SerializeField] private float lineStartAngle = 180f;
    [SerializeField] private int lineVolleysBeforeRotate = 4;
    [SerializeField] private float lineRotatePerVolley = 8f;
    [SerializeField] private float downLineInterval = 0.48f;
    [SerializeField] private int downLineCount = 6;
    [SerializeField] private float downLineSpacing = 0.72f;
    [SerializeField] private float downLineSpeed = 5.3f;

    [Header("Phase 2 - Circles")]
    [SerializeField] private float circleInterval = 0.8f;
    [SerializeField] private int circleBulletCount = 24;
    [SerializeField] private float circleSpeed = 4.15f;
    [SerializeField] private float circleSpinPerShot = 11f;

    [Header("Phase 3 - Screen Flood")]
    [SerializeField] private float floodFanInterval = 0.28f;
    [SerializeField] private int floodFanCount = 18;
    [SerializeField] private float floodFanSpreadAngle = 160f;
    [SerializeField] private float floodFanBaseSpeed = 4.6f;
    [SerializeField] private float floodFanSpeedStep = 0.18f;
    [SerializeField] private float floodSpiralInterval = 0.55f;
    [SerializeField] private int floodSpiralCount = 30;
    [SerializeField] private float floodSpiralSpeed = 4.2f;
    [SerializeField] private float floodSpiralSpinPerShot = 20f;
    [SerializeField] private bool moveToCenterOnPhase2 = true;
    [SerializeField] private bool returnToHomeOnPhase3 = true;
    [SerializeField] private float phaseMoveSpeed = 3.75f;
    [SerializeField] private float phaseArrivalDistance = 0.04f;
    

    [Header("Encounter")]
    [SerializeField] private bool autoStartEncounter = false;
    [SerializeField] private bool hideUntilEncounterStarts = true;
    [SerializeField] private float encounterAutoStartDelay = 0f;
    [SerializeField] private Vector2 encounterEntryOffset = new Vector2(0f, 2.5f);
    [SerializeField] private float encounterEntryMoveSpeed = 4f;
    [SerializeField] private float phase1ChargeDuration = 1.75f;
[SerializeField] private Vector2 phase2CenterOffset = Vector2.zero;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] [Range(0f, 1f)] private float bossMusicVolume = 1f;
    [SerializeField] private bool playBossMusicOnSpawn = true;
    [SerializeField] private bool autoCreateMusicSource = true;
    [SerializeField] private bool restorePreviousMusicOnDefeat = false;

    [Header("Visual")]
    [SerializeField] private bool autoCreateVisual = true;
    [SerializeField] private Color bossColor = new Color(1f, 0.35f, 0.35f, 1f);

    private static Sprite fallbackBossSprite;

    private BossPhase currentPhase = BossPhase.Phase1;
    private float currentPhaseHealth;
    private float transitionTimer;
    private float nextLineTime;
    private float nextDownLineTime;
    private float nextCircleTime;
    private float nextFloodFanTime;
    private float nextFloodSpiralTime;
    private float phase1LineAngle;
    private int phase1VolleyCount;
    private float phaseAngle;
    private bool isDead;
    private bool phase2ReachedCenter;
    private bool phase3ReachedHome;
    private Camera cachedCamera;
    private Vector3 homePosition;
    private bool bossMusicStarted;
    private bool hasStoredMusicState;
    private AudioClip storedMusicClip;
    private float storedMusicVolume;
    private bool storedMusicLoop;
    
    private bool encounterRequested;
    private bool encounterActive;
    private bool phaseLoopEnabled;
    private float encounterStartDelayTimer;
private bool warnedMusicPlayFailed;

    public int CurrentPhaseNumber
    {
        get
        {
            return currentPhase switch
            {
                BossPhase.Phase1 => 1,
                BossPhase.Phase2 => 2,
                BossPhase.Phase3 => 3,
                _ => 0
            };
        }
    }

    public bool IsTransitioning => transitionTimer > 0f;
    
    public bool IsEncounterActive => encounterActive && !isDead && currentPhase != BossPhase.Defeated;
public bool IsDefeated => isDead || currentPhase == BossPhase.Defeated;
    public float PhaseHealth01 => Mathf.Clamp01(currentPhaseHealth / Mathf.Max(1f, GetPhaseMaxHealth(currentPhase)));
    public float TransitionProgress01 => Mathf.Clamp01(1f - (transitionTimer / Mathf.Max(0.0001f, phaseTransitionInvulnerability)));

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

        phase1MaxHealth = Mathf.Max(1f, phase1MaxHealth);
        phase2MaxHealth = Mathf.Max(1f, phase2MaxHealth);
        phase3MaxHealth = Mathf.Max(1f, phase3MaxHealth);
        lineCount = Mathf.Max(1, lineCount);
        lineSpacing = Mathf.Max(0.05f, lineSpacing);
        lineVolleysBeforeRotate = Mathf.Max(0, lineVolleysBeforeRotate);
        lineRotatePerVolley = Mathf.Max(0f, lineRotatePerVolley);
        downLineInterval = Mathf.Max(0.06f, downLineInterval);
        downLineCount = Mathf.Max(1, downLineCount);
        downLineSpacing = Mathf.Max(0.05f, downLineSpacing);
        downLineSpeed = Mathf.Max(0.2f, downLineSpeed);
        circleBulletCount = Mathf.Max(6, circleBulletCount);
        floodFanCount = Mathf.Max(5, floodFanCount);
        floodSpiralCount = Mathf.Max(8, floodSpiralCount);
        phaseMoveSpeed = Mathf.Max(0.05f, phaseMoveSpeed);
        phaseArrivalDistance = Mathf.Max(0.001f, phaseArrivalDistance);
        bossMusicVolume = Mathf.Clamp01(bossMusicVolume);
        encounterAutoStartDelay = Mathf.Max(0f, encounterAutoStartDelay);
        encounterEntryMoveSpeed = Mathf.Max(0.05f, encounterEntryMoveSpeed);
        phase1ChargeDuration = Mathf.Max(0f, phase1ChargeDuration);

        EnsureVisual();
        EnsureCollider();
    }

private void OnEnable()
    {
        InitializeRuntimeState();
    }

private void Awake()
    {
        InitializeRuntimeState();
    }

    public void QueueEncounterStart(float delaySeconds = 0f)
    {
        if (!Application.isPlaying || isDead || encounterActive || encounterRequested || phaseLoopEnabled || currentPhase == BossPhase.Defeated)
        {
            return;
        }

        encounterRequested = true;
        encounterActive = true;
        phaseLoopEnabled = false;
        transitionTimer = 0f;
        currentPhaseHealth = 0f;
        encounterStartDelayTimer = Mathf.Max(0f, delaySeconds);

        SetBossVisible(true);

        Vector3 entryStart = homePosition + (Vector3)encounterEntryOffset;
        transform.position = entryStart;
    }

    private void InitializeRuntimeState()
    {
        if (gameObject.tag != "Enemy")
        {
            gameObject.tag = "Enemy";
        }

        EnsureVisual();
        EnsureCollider();
        cachedCamera = Camera.main;
        homePosition = transform.position;

        if (!Application.isPlaying)
        {
            return;
        }

        isDead = false;
        currentPhase = BossPhase.Phase1;
        currentPhaseHealth = 0f;
        transitionTimer = 0f;
        phaseLoopEnabled = false;
        encounterRequested = false;
        encounterActive = false;
        encounterStartDelayTimer = 0f;

        if (hideUntilEncounterStarts)
        {
            SetBossVisible(false);
        }

        if (autoStartEncounter)
        {
            QueueEncounterStart(encounterAutoStartDelay);
        }
    }

    private void TryAdvanceEncounterIntro()
    {
        if (!encounterRequested)
        {
            return;
        }

        if (encounterStartDelayTimer > 0f)
        {
            encounterStartDelayTimer = Mathf.Max(0f, encounterStartDelayTimer - Time.deltaTime);
            return;
        }

        float arrivalDistance = Mathf.Max(0.001f, phaseArrivalDistance);
        if ((transform.position - homePosition).sqrMagnitude > (arrivalDistance * arrivalDistance))
        {
            float step = Mathf.Max(0.01f, encounterEntryMoveSpeed) * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, homePosition, step);
            return;
        }

        transform.position = homePosition;
        encounterRequested = false;
        StartPhase(BossPhase.Phase1);
        transitionTimer = Mathf.Max(0f, phase1ChargeDuration);
    }

    private void SetBossVisible(bool isVisible)
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = isVisible;
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = isVisible;
        }
    }

    
private void SyncBossMusicVolume()
    {
        if (!Application.isPlaying || !bossMusicStarted || musicSource == null || musicSource.clip != bossMusic)
        {
            return;
        }

        float targetVolume = Mathf.Clamp01(bossMusicVolume);
        if (!Mathf.Approximately(musicSource.volume, targetVolume))
        {
            musicSource.volume = targetVolume;
        }
    }

private void Update()
    {
        SyncBossMusicVolume();

        if (!Application.isPlaying || isDead)
        {
            return;
        }

        if (!phaseLoopEnabled)
        {
            TryAdvanceEncounterIntro();
            return;
        }

        if (transitionTimer > 0f)
        {
            transitionTimer = Mathf.Max(0f, transitionTimer - Time.deltaTime);
            return;
        }

        switch (currentPhase)
        {
            case BossPhase.Phase1:
                RunPhase1();
                break;

            case BossPhase.Phase2:
                RunPhase2();
                break;

            case BossPhase.Phase3:
                RunPhase3();
                break;
        }
    }

public void TakeDamage(float amount)
    {
        if (!Application.isPlaying || isDead || !phaseLoopEnabled || transitionTimer > 0f || amount <= 0f)
        {
            return;
        }

        currentPhaseHealth = Mathf.Max(0f, currentPhaseHealth - amount);
        if (currentPhaseHealth > 0f)
        {
            return;
        }

        AdvancePhase();
    }

private void StartPhase(BossPhase phase)
    {
        currentPhase = phase;
        isDead = false;
        encounterActive = true;
        phaseLoopEnabled = true;
        transitionTimer = 0f;
        phaseAngle = 0f;
        SetBossVisible(true);

        switch (phase)
        {
            case BossPhase.Phase1:
                currentPhaseHealth = Mathf.Max(1f, phase1MaxHealth);
                nextLineTime = Time.time + 0.45f;
                nextDownLineTime = Time.time + 0.35f;
                phase1LineAngle = lineStartAngle;
                phase1VolleyCount = 0;
                phase2ReachedCenter = false;
                phase3ReachedHome = false;
                TryPlayBossMusic();
                break;

            case BossPhase.Phase2:
                currentPhaseHealth = Mathf.Max(1f, phase2MaxHealth);
                nextCircleTime = Time.time + 0.7f;
                phase2ReachedCenter = !moveToCenterOnPhase2;
                phase3ReachedHome = false;
                break;

            case BossPhase.Phase3:
                currentPhaseHealth = Mathf.Max(1f, phase3MaxHealth);
                nextFloodFanTime = Time.time + 0.45f;
                nextFloodSpiralTime = Time.time + 0.65f;
                phase3ReachedHome = !returnToHomeOnPhase3;
                break;
        }
    }

    private void AdvancePhase()
    {
        ClearEnemyBullets();

        if (currentPhase == BossPhase.Phase1)
        {
            StartPhase(BossPhase.Phase2);
            transitionTimer = phaseTransitionInvulnerability;
            return;
        }

        if (currentPhase == BossPhase.Phase2)
        {
            StartPhase(BossPhase.Phase3);
            transitionTimer = phaseTransitionInvulnerability;
            return;
        }

        Die();
    }

private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        encounterActive = false;
        phaseLoopEnabled = false;
        currentPhase = BossPhase.Defeated;
        ClearEnemyBullets();

        if (ScoreSystem.Instance != null && killScore > 0)
        {
            ScoreSystem.Instance.AddScore(killScore);
        }

        RestorePreviousMusicIfNeeded();
        Destroy(gameObject);
    }

    private void TryPlayBossMusic()
    {
        if (!Application.isPlaying || !playBossMusicOnSpawn || bossMusicStarted || bossMusic == null)
        {
            return;
        }

        if (musicSource == null)
        {
            musicSource = FindBestMusicSource();
            if (musicSource == null && autoCreateMusicSource)
            {
                musicSource = GetComponent<AudioSource>();
                if (musicSource == null)
                {
                    musicSource = gameObject.AddComponent<AudioSource>();
                }

                ConfigureMusicSource(musicSource);
            }
        }

        ConfigureMusicSource(musicSource);

        if (musicSource == null)
        {
            return;
        }

        if (restorePreviousMusicOnDefeat && !hasStoredMusicState)
        {
            hasStoredMusicState = true;
            storedMusicClip = musicSource.clip;
            storedMusicVolume = musicSource.volume;
            storedMusicLoop = musicSource.loop;
        }

        musicSource.enabled = true;
        musicSource.clip = bossMusic;
        musicSource.volume = Mathf.Clamp01(bossMusicVolume);
        musicSource.loop = true;
        musicSource.Play();

        if (!musicSource.isPlaying)
        {
            musicSource.UnPause();
            musicSource.PlayDelayed(0f);
        }

        if (!musicSource.isPlaying && !warnedMusicPlayFailed)
        {
            warnedMusicPlayFailed = true;
            Debug.LogWarning("[BossThreePhaseController] Boss music could not start playing. Check Game mute/audio focus and clip import settings.");
        }

        bossMusicStarted = true;
    }

    private void RestorePreviousMusicIfNeeded()
    {
        if (!Application.isPlaying || !restorePreviousMusicOnDefeat || !hasStoredMusicState || musicSource == null)
        {
            return;
        }

        musicSource.clip = storedMusicClip;
        musicSource.volume = storedMusicVolume;
        musicSource.loop = storedMusicLoop;

        warnedMusicPlayFailed = false;
        if (storedMusicClip != null)
        {
            musicSource.Play();
        }
        else
        {
            musicSource.Stop();
        }

        hasStoredMusicState = false;
    }

    private AudioSource FindBestMusicSource()
    {
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        AudioSource loopingFallback = null;
        AudioSource anyFallback = null;

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
            {
                continue;
            }

            if (anyFallback == null)
            {
                anyFallback = source;
            }

            if (loopingFallback == null && source.loop)
            {
                loopingFallback = source;
            }

            string lowerName = source.gameObject.name.ToLowerInvariant();
            if (lowerName.Contains("music") || lowerName.Contains("bgm"))
            {
                return source;
            }
        }

        return loopingFallback != null ? loopingFallback : anyFallback;
    }

    private void ConfigureMusicSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
    }

    private void RunPhase1()
    {
        if (Time.time >= nextLineTime)
        {
            nextLineTime = Time.time + Mathf.Max(0.08f, lineInterval);
            FireStraightLinePattern();
        }

        if (Time.time >= nextDownLineTime)
        {
            nextDownLineTime = Time.time + Mathf.Max(0.08f, downLineInterval);
            FireDownwardLinePattern();
        }
    }

    private void RunPhase3()
    {
        if (!MoveToPhase3HomeIfNeeded())
        {
            return;
        }

        if (Time.time >= nextFloodFanTime)
        {
            nextFloodFanTime = Time.time + Mathf.Max(0.06f, floodFanInterval);
            FireFloodFanPattern();
        }

        if (Time.time >= nextFloodSpiralTime)
        {
            nextFloodSpiralTime = Time.time + Mathf.Max(0.08f, floodSpiralInterval);
            FireFloodSpiralPattern();
        }
    }

    private bool MoveToPhase2CenterIfNeeded()
    {
        if (!moveToCenterOnPhase2 || phase2ReachedCenter)
        {
            return true;
        }

        Vector3 target = GetPhase2CenterPoint();
        float step = Mathf.Max(0.01f, phaseMoveSpeed) * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, step);

        float arriveDist = Mathf.Max(0.001f, phaseArrivalDistance);
        if ((transform.position - target).sqrMagnitude <= arriveDist * arriveDist)
        {
            transform.position = target;
            phase2ReachedCenter = true;
        }

        return phase2ReachedCenter;
    }

    private bool MoveToPhase3HomeIfNeeded()
    {
        if (!returnToHomeOnPhase3 || phase3ReachedHome)
        {
            return true;
        }

        float step = Mathf.Max(0.01f, phaseMoveSpeed) * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, homePosition, step);

        float arriveDist = Mathf.Max(0.001f, phaseArrivalDistance);
        if ((transform.position - homePosition).sqrMagnitude <= arriveDist * arriveDist)
        {
            transform.position = homePosition;
            phase3ReachedHome = true;
        }

        return phase3ReachedHome;
    }

    private Vector3 GetPhase2CenterPoint()
    {
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        Vector3 target = new Vector3(phase2CenterOffset.x, phase2CenterOffset.y, transform.position.z);
        if (cachedCamera != null)
        {
            Vector3 cameraPos = cachedCamera.transform.position;
            target.x += cameraPos.x;
            target.y += cameraPos.y;
        }

        return target;
    }

    private void RunPhase2()
    {
        if (!MoveToPhase2CenterIfNeeded())
        {
            return;
        }

        if (Time.time < nextCircleTime)
        {
            return;
        }

        nextCircleTime = Time.time + Mathf.Max(0.06f, circleInterval);
        FireCirclePattern();
    }

    private void FireStraightLinePattern()
    {
        int count = Mathf.Max(1, lineCount);
        float half = (count - 1) * 0.5f;
        Vector2 direction = DirectionFromDegrees(phase1LineAngle).normalized;
        Vector2 right = new Vector2(-direction.y, direction.x);
        Vector3 center = transform.position + (Vector3)(direction * spawnRadius);

        for (int i = 0; i < count; i++)
        {
            float offsetX = (i - half) * lineSpacing;
            Vector3 spawnPos = center + (Vector3)(right * offsetX);
            SpawnProjectileAtPosition(spawnPos, direction, lineSpeed);
        }

        phase1VolleyCount++;
        if (phase1VolleyCount > lineVolleysBeforeRotate)
        {
            phase1LineAngle += lineRotatePerVolley;
        }
    }

    private void FireDownwardLinePattern()
    {
        int count = Mathf.Max(1, downLineCount);
        float half = (count - 1) * 0.5f;
        Vector3 center = transform.position + (Vector3)(Vector2.down * spawnRadius);

        for (int i = 0; i < count; i++)
        {
            float offsetX = (i - half) * downLineSpacing;
            Vector3 spawnPos = center + new Vector3(offsetX, 0f, 0f);
            SpawnProjectileAtPosition(spawnPos, Vector2.down, downLineSpeed);
        }
    }

    private void FireCirclePattern()
    {
        int count = Mathf.Max(6, circleBulletCount);
        float step = 360f / count;
        phaseAngle += circleSpinPerShot;

        for (int i = 0; i < count; i++)
        {
            float angleA = phaseAngle + (step * i);
            float angleB = phaseAngle + (step * i) + (step * 0.5f);
            SpawnProjectile(DirectionFromDegrees(angleA), circleSpeed);
            SpawnProjectile(DirectionFromDegrees(angleB), circleSpeed * 0.92f);
        }
    }

    private void FireFloodFanPattern()
    {
        int count = Mathf.Max(5, floodFanCount);
        float spread = Mathf.Clamp(floodFanSpreadAngle, 10f, 340f);
        float start = -spread * 0.5f;
        float step = count > 1 ? spread / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float offset = start + (step * i);
            float angle = 180f + offset;
            float speed = floodFanBaseSpeed + (Mathf.Abs(i - ((count - 1) * 0.5f)) * floodFanSpeedStep);
            SpawnProjectile(DirectionFromDegrees(angle), speed);
        }
    }

    private void FireFloodSpiralPattern()
    {
        int count = Mathf.Max(8, floodSpiralCount);
        float step = 360f / count;
        phaseAngle += floodSpiralSpinPerShot;

        for (int i = 0; i < count; i++)
        {
            float angle = phaseAngle + (step * i);
            SpawnProjectile(DirectionFromDegrees(angle), floodSpiralSpeed);
        }
    }

    private void SpawnProjectile(Vector2 direction, float speed)
    {
        Vector3 spawnPos = transform.position + (Vector3)(direction.normalized * spawnRadius);
        SpawnProjectileAtPosition(spawnPos, direction, speed);
    }

    private void SpawnProjectileAtPosition(Vector3 position, Vector2 direction, float speed)
    {
        Transform bulletGroup = RuntimeSpawnGroups.GetEnemyBulletsGroup();
        GameObject projectile = projectilePrefab != null
            ? Instantiate(projectilePrefab, position, Quaternion.identity, bulletGroup)
            : CreateFallbackProjectile(position, bulletGroup);

        RuntimeSpawnGroups.MoveToEnemyBullets(projectile.transform);

        EnemyProjectile projectileLogic = projectile.GetComponent<EnemyProjectile>();
        if (projectileLogic == null)
        {
            projectileLogic = projectile.AddComponent<EnemyProjectile>();
        }

        projectileLogic.Initialize(direction, speed, projectileLifetime);
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

    private void ClearEnemyBullets()
    {
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        for (int i = 0; i < bullets.Length; i++)
        {
            Destroy(bullets[i]);
        }
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
            renderer.sprite = GetOrCreateBossSprite();
        }

        renderer.enabled = true;
        renderer.color = bossColor;
        renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 65);
    }

    private void EnsureCollider()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.42f;
            return;
        }

        col.isTrigger = true;
    }

    private float GetPhaseMaxHealth(BossPhase phase)
    {
        return phase switch
        {
            BossPhase.Phase1 => Mathf.Max(1f, phase1MaxHealth),
            BossPhase.Phase2 => Mathf.Max(1f, phase2MaxHealth),
            BossPhase.Phase3 => Mathf.Max(1f, phase3MaxHealth),
            _ => 1f
        };
    }

    private static Vector2 DirectionFromDegrees(float angleDeg)
    {
        return Quaternion.Euler(0f, 0f, angleDeg) * Vector2.up;
    }

    private static Sprite GetOrCreateBossSprite()
    {
        if (fallbackBossSprite != null)
        {
            return fallbackBossSprite;
        }

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        int center = size / 2;
        int radius = Mathf.Max(1, center - 3);

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
        texture.name = "BossRuntime";

        fallbackBossSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);

        return fallbackBossSprite;
    }
}
