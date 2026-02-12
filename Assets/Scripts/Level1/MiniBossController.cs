using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MiniBossController : MonoBehaviour, IDamageable
{
    [Header("Boss Stats")]
    [SerializeField] private float maxHealth = 520f;
    [SerializeField] private float fightDurationTarget = 25f;
    [SerializeField] private int killScore = 18000;

    [Header("Movement")]
    [SerializeField] private float verticalAmplitude = 0.45f;
    [SerializeField] private float verticalFrequency = 1.1f;

    [Header("Pattern Speeds")]
    [SerializeField] private float phase1BulletSpeed = 3.4f;
    [SerializeField] private float phase2BulletSpeed = 4.25f;
    [SerializeField] private float phase3BulletSpeed = 5.1f;
    [SerializeField] private float bulletLifetime = 7f;

    [Header("Optional Drop")]
    [SerializeField] private GameObject optionalDropPrefab;

    public event Action<MiniBossController> OnMiniBossDefeated;

    private BulletPatternSystem bulletPatternSystem;
    private Transform playerTarget;
    private Action<MiniBossController> onReturnedToPool;
    private float currentHealth;
    private float phaseTimer;
    private float combatStartTime;
    private float spiralAngle;
    private float nextRadialTime;
    private float nextAimedTime;
    private float nextSpiralTime;
    private float nextSpreadTime;
    private Vector3 homePosition;
    private bool initialized;
    private bool defeated;

    public float Health01 => Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));

    private void Awake()
    {
        EnsurePresentation();
    }

    private void OnEnable()
    {
        currentHealth = Mathf.Max(1f, maxHealth);
        phaseTimer = 0f;
        combatStartTime = Time.time;
        homePosition = transform.position;
        spiralAngle = 0f;
        nextRadialTime = Time.time + 0.75f;
        nextAimedTime = Time.time + 0.95f;
        nextSpiralTime = Time.time + 0.12f;
        nextSpreadTime = Time.time + 1.1f;
        defeated = false;
        EnsurePresentation();
    }

    public void Configure(
        BulletPatternSystem patternSystem,
        Transform player,
        Action<MiniBossController> onDespawn)
    {
        bulletPatternSystem = patternSystem;
        playerTarget = player;
        onReturnedToPool = onDespawn;
        currentHealth = Mathf.Max(1f, maxHealth);
        phaseTimer = 0f;
        combatStartTime = Time.time;
        homePosition = transform.position;
        spiralAngle = 0f;
        nextRadialTime = Time.time + 0.75f;
        nextAimedTime = Time.time + 0.95f;
        nextSpiralTime = Time.time + 0.12f;
        nextSpreadTime = Time.time + 1.1f;
        defeated = false;
        initialized = true;
        EnsurePresentation();
    }

    private void Update()
    {
        if (!initialized || defeated || bulletPatternSystem == null)
        {
            return;
        }

        phaseTimer += Time.deltaTime;
        ApplyMovement();

        float health = Health01;
        if (health > 0.6f)
        {
            RunPhase1();
            return;
        }

        if (health > 0.3f)
        {
            RunPhase2();
            return;
        }

        RunPhase3();
    }

    public void TakeDamage(float amount)
    {
        if (!initialized || defeated || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth > 0f)
        {
            return;
        }

        Defeat();
    }

    private void RunPhase1()
    {
        if (Time.time >= nextRadialTime)
        {
            nextRadialTime = Time.time + 1.1f;
            bulletPatternSystem.FireRadial(transform.position, 10, phase1BulletSpeed, bulletLifetime, phaseTimer * 10f);
        }

        if (Time.time >= nextAimedTime)
        {
            nextAimedTime = Time.time + 1.3f;
            bulletPatternSystem.FireAimedAtTarget(transform.position, playerTarget, phase1BulletSpeed + 0.7f, bulletLifetime);
        }
    }

    private void RunPhase2()
    {
        if (Time.time >= nextSpiralTime)
        {
            nextSpiralTime = Time.time + 0.09f;
            bulletPatternSystem.FireSpiralStep(
                transform.position,
                ref spiralAngle,
                9f,
                1,
                phase2BulletSpeed,
                bulletLifetime);
        }

        if (Time.time >= nextSpreadTime)
        {
            nextSpreadTime = Time.time + 1.0f;
            bulletPatternSystem.FireSpread(transform.position, Vector2.down, 7, 95f, phase2BulletSpeed + 0.35f, bulletLifetime);
        }
    }

    private void RunPhase3()
    {
        float enrage = Mathf.Clamp01((Time.time - combatStartTime) / Mathf.Max(10f, fightDurationTarget));
        float speed = phase3BulletSpeed + (enrage * 1.2f);

        if (Time.time >= nextSpiralTime)
        {
            nextSpiralTime = Time.time + 0.07f;
            bulletPatternSystem.FireSpiralStep(
                transform.position,
                ref spiralAngle,
                12f,
                1,
                speed,
                bulletLifetime);
        }

        if (Time.time >= nextAimedTime)
        {
            nextAimedTime = Time.time + 0.7f;
            bulletPatternSystem.FireAimedAtTarget(transform.position, playerTarget, speed + 0.8f, bulletLifetime);
        }
    }

    private void ApplyMovement()
    {
        float verticalOffset = Mathf.Sin(phaseTimer * verticalFrequency) * verticalAmplitude;
        transform.position = new Vector3(homePosition.x, homePosition.y + verticalOffset, homePosition.z);
    }

    private void Defeat()
    {
        if (defeated)
        {
            return;
        }

        defeated = true;

        if (ScoreSystem.Instance != null && killScore > 0)
        {
            ScoreSystem.Instance.AddScore(killScore);
        }

        if (optionalDropPrefab != null)
        {
            Instantiate(optionalDropPrefab, transform.position, Quaternion.identity);
        }

        StartCoroutine(PlayExplosionAndReturn());
    }

    private IEnumerator PlayExplosionAndReturn()
    {
        GameObject explosion = new GameObject("MiniBossExplosion");
        explosion.transform.position = transform.position;
        SpriteRenderer renderer = explosion.AddComponent<SpriteRenderer>();
        try
        {
            renderer.sprite = RuntimeSpriteLibrary.GetCircleSprite();
        }
        catch
        {
            renderer.sprite = null;
        }

        renderer.color = new Color(1f, 0.42f, 0.15f, 0.95f);
        renderer.sortingOrder = 200;

        float duration = 0.65f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scale = Mathf.Lerp(0.4f, 3.4f, t);
            explosion.transform.localScale = new Vector3(scale, scale, 1f);
            Color c = renderer.color;
            c.a = Mathf.Lerp(0.95f, 0f, t);
            renderer.color = c;
            yield return null;
        }

        Destroy(explosion);
        OnMiniBossDefeated?.Invoke(this);
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

    private void EnsurePresentation()
    {
        if (gameObject.tag != "Enemy")
        {
            gameObject.tag = "Enemy";
        }

        Collider2D collider2D = GetComponent<Collider2D>();
        if (collider2D == null)
        {
            collider2D = gameObject.AddComponent<CircleCollider2D>();
        }

        collider2D.isTrigger = true;
        if (collider2D is CircleCollider2D circle)
        {
            circle.radius = 0.48f;
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

        renderer.color = new Color(1f, 0.2f, 0.2f, 1f);
        renderer.sortingOrder = Mathf.Max(80, renderer.sortingOrder);
        transform.localScale = new Vector3(1.75f, 1.75f, 1f);
    }
}
