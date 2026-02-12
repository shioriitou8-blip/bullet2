using UnityEngine;

[DisallowMultipleComponent]
public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    private class DifficultySettings
    {
        [Min(0.2f)] public float spawnIntervalMultiplier = 1f;
        [Min(0.2f)] public float bulletSpeedMultiplier = 1f;
        [Min(0.2f)] public float bulletCountMultiplier = 1f;
        [Min(0.2f)] public float simultaneousSpawnMultiplier = 1f;
    }

    [Header("References")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private BulletPatternSystem bulletPatternSystem;

    [Header("Spawn Space")]
    [SerializeField] private float spawnRightX = 8.8f;
    [SerializeField] private float spawnYMin = -4.2f;
    [SerializeField] private float spawnYMax = 4.2f;
    [SerializeField] private float preBossYMin = -1.9f;
    [SerializeField] private float preBossYMax = 1.9f;

    [Header("Difficulty")]
    [SerializeField] private DifficultySettings difficulty = new DifficultySettings();

    private float nextPrimarySpawnTime;
    private float nextSecondarySpawnTime;
    private int laneIndex;
    private Level1Phase cachedPhase = Level1Phase.Intro;
    private bool spawningEnabled = true;

    private void Awake()
    {
        if (levelManager == null)
        {
            levelManager = FindAnyObjectByType<LevelManager>();
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindAnyObjectByType<EnemySpawner>();
        }

        if (bulletPatternSystem == null)
        {
            bulletPatternSystem = FindAnyObjectByType<BulletPatternSystem>();
        }
    }

    private void Update()
    {
        if (!spawningEnabled || levelManager == null || enemySpawner == null || bulletPatternSystem == null)
        {
            return;
        }

        Level1Phase phase = levelManager.CurrentPhase;
        if (phase == Level1Phase.MiniBoss || phase == Level1Phase.Completed)
        {
            return;
        }

        if (phase != cachedPhase)
        {
            cachedPhase = phase;
            nextPrimarySpawnTime = Time.time + 0.2f;
            nextSecondarySpawnTime = Time.time + 0.35f;
            laneIndex = 0;
            ApplyPhaseDifficulty(phase);
        }

        switch (phase)
        {
            case Level1Phase.Intro:
                TickPhaseIntro();
                break;

            case Level1Phase.FirstPattern:
                TickPhaseFirstPattern();
                break;

            case Level1Phase.RadialSimple:
                TickPhaseRadialSimple();
                break;

            case Level1Phase.Combination:
                TickPhaseCombination();
                break;

            case Level1Phase.HighPressure:
                TickPhaseHighPressure();
                break;

            case Level1Phase.PreBoss:
                TickPhasePreBoss();
                break;
        }
    }

    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;
    }

    private void TickPhaseIntro()
    {
        float now = Time.time;
        float interval = ScaleSpawnInterval(3.25f);
        if (now < nextPrimarySpawnTime)
        {
            return;
        }

        nextPrimarySpawnTime = now + interval;
        SpawnSingle(LevelEnemyController.EnemyType.TypeA, GetTopSpawnPosition(), 1f, 1f, 1f, 0.9f, false);
    }

    private void TickPhaseFirstPattern()
    {
        float now = Time.time;
        float interval = ScaleSpawnInterval(2.35f);
        if (now < nextPrimarySpawnTime)
        {
            return;
        }

        nextPrimarySpawnTime = now + interval;
        Vector2 lanePosition = GetAlternatingLaneSpawnPosition();
        SpawnSingle(LevelEnemyController.EnemyType.TypeB, lanePosition, 1f, 1.05f, 1f, 1f, false);
    }

    private void TickPhaseRadialSimple()
    {
        float now = Time.time;
        if (now >= nextPrimarySpawnTime)
        {
            nextPrimarySpawnTime = now + ScaleSpawnInterval(1.95f);
            SpawnSingle(LevelEnemyController.EnemyType.TypeC, GetTopSpawnPosition(), 1f, 1f, 1f, 0.98f, false);
        }

        if (now >= nextSecondarySpawnTime)
        {
            nextSecondarySpawnTime = now + ScaleSpawnInterval(3.25f);
            SpawnSingle(LevelEnemyController.EnemyType.TypeA, GetTopSpawnPosition(), 1f, 1.15f, 1f, 0.95f, false);
        }
    }

    private void TickPhaseCombination()
    {
        float now = Time.time;
        if (now >= nextPrimarySpawnTime)
        {
            nextPrimarySpawnTime = now + ScaleSpawnInterval(1.45f);
            SpawnSingle(GetMixedTypeABC(), GetTopSpawnPosition(), 1.05f, 1.05f, 1.08f, 1.08f, false);

            int extraCount = GetExtraSpawnCount();
            for (int i = 0; i < extraCount; i++)
            {
                SpawnSingle(GetMixedTypeABC(), GetTopSpawnPosition(), 1f, 1f, 1f, 1.05f, false);
            }
        }

        if (now >= nextSecondarySpawnTime)
        {
            nextSecondarySpawnTime = now + ScaleSpawnInterval(4f);
            SpawnSingle(LevelEnemyController.EnemyType.TypeB, GetTopSpawnPosition(), 1f, 1.1f, 1.15f, 1.1f, false);
        }
    }

    private void TickPhaseHighPressure()
    {
        float now = Time.time;
        if (now >= nextPrimarySpawnTime)
        {
            nextPrimarySpawnTime = now + ScaleSpawnInterval(1.02f);
            SpawnSingle(LevelEnemyController.EnemyType.TypeD, GetTopSpawnPosition(), 1.1f, 1.1f, 1.2f, 1.18f, false);
        }

        if (now >= nextSecondarySpawnTime)
        {
            nextSecondarySpawnTime = now + ScaleSpawnInterval(1.55f);
            SpawnSingle(
                Random.value > 0.5f ? LevelEnemyController.EnemyType.TypeB : LevelEnemyController.EnemyType.TypeC,
                GetTopSpawnPosition(),
                1.04f,
                1.12f,
                1.15f,
                1.16f,
                false);
        }
    }

    private void TickPhasePreBoss()
    {
        float now = Time.time;
        if (now >= nextPrimarySpawnTime)
        {
            nextPrimarySpawnTime = now + ScaleSpawnInterval(0.95f);
            SpawnSingle(LevelEnemyController.EnemyType.TypeC, GetTopSpawnPosition(restrictForPreBoss: true), 1.15f, 1.08f, 1.22f, 1.28f, true);
        }

        if (now >= nextSecondarySpawnTime)
        {
            nextSecondarySpawnTime = now + ScaleSpawnInterval(1.9f);
            SpawnSingle(LevelEnemyController.EnemyType.TypeC, GetTopSpawnPosition(restrictForPreBoss: true), 1.1f, 1.12f, 1.2f, 1.3f, true);
        }
    }

    private void SpawnSingle(
        LevelEnemyController.EnemyType type,
        Vector2 position,
        float healthScale,
        float moveScale,
        float fireRateScale,
        float bulletSpeedScale,
        bool forceRadialOnly)
    {
        enemySpawner.SpawnEnemy(
            type,
            position,
            healthScale,
            moveScale,
            fireRateScale,
            bulletSpeedScale,
            forceRadialOnly);
    }

    private void ApplyPhaseDifficulty(Level1Phase phase)
    {
        float phaseBulletSpeed = phase switch
        {
            Level1Phase.Intro => 0.85f,
            Level1Phase.FirstPattern => 0.95f,
            Level1Phase.RadialSimple => 1f,
            Level1Phase.Combination => 1.14f,
            Level1Phase.HighPressure => 1.3f,
            Level1Phase.PreBoss => 1.42f,
            _ => 1f
        };

        float phaseBulletCount = phase switch
        {
            Level1Phase.Intro => 0.85f,
            Level1Phase.FirstPattern => 0.95f,
            Level1Phase.RadialSimple => 1f,
            Level1Phase.Combination => 1.07f,
            Level1Phase.HighPressure => 1.18f,
            Level1Phase.PreBoss => 1.2f,
            _ => 1f
        };

        float speedMultiplier = phaseBulletSpeed * Mathf.Max(0.2f, difficulty.bulletSpeedMultiplier);
        float countMultiplier = phaseBulletCount * Mathf.Max(0.2f, difficulty.bulletCountMultiplier);
        bulletPatternSystem.ConfigureDifficulty(speedMultiplier, countMultiplier);
    }

    private float ScaleSpawnInterval(float baseInterval)
    {
        float multiplier = Mathf.Max(0.2f, difficulty.spawnIntervalMultiplier);
        return Mathf.Max(0.08f, baseInterval / multiplier);
    }

    private int GetExtraSpawnCount()
    {
        float extra = Mathf.Max(0f, difficulty.simultaneousSpawnMultiplier - 1f);
        int guaranteed = Mathf.FloorToInt(extra);
        float chance = extra - guaranteed;
        int additional = guaranteed;
        if (Random.value < chance)
        {
            additional++;
        }

        return additional;
    }

    private Vector2 GetTopSpawnPosition(bool restrictForPreBoss = false)
    {
        float minY = restrictForPreBoss ? preBossYMin : spawnYMin;
        float maxY = restrictForPreBoss ? preBossYMax : spawnYMax;
        float y = Random.Range(minY, maxY);
        return new Vector2(spawnRightX, y);
    }

    private Vector2 GetAlternatingLaneSpawnPosition()
    {
        float[] laneHeights = { 3f, 0f, -3f };
        float y = laneHeights[laneIndex % laneHeights.Length];
        laneIndex++;

        return new Vector2(spawnRightX, y);
    }

    private LevelEnemyController.EnemyType GetMixedTypeABC()
    {
        float roll = Random.value;
        if (roll < 0.45f)
        {
            return LevelEnemyController.EnemyType.TypeA;
        }

        if (roll < 0.78f)
        {
            return LevelEnemyController.EnemyType.TypeB;
        }

        return LevelEnemyController.EnemyType.TypeC;
    }
}
