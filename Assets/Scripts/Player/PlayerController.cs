using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    [Header("Movement (px/frame)")]
    [SerializeField] private float normalSpeedPxPerFrame = 5f;
    [SerializeField] private float focusSpeedPxPerFrame = 2f;
    [SerializeField] private float pixelsPerUnit = 100f;
    [SerializeField] private float referenceFrameRate = 60f;
    [SerializeField] private bool clampToCameraBounds = true;

    [Header("Hitbox & Graze")]
    [SerializeField] private float hitboxRadiusPixels = 3f;
    [SerializeField] private float grazeRadiusPixels = 15f;
    [SerializeField] private LayerMask enemyBulletMask;

    [Header("Resources")]
    [SerializeField] private int initialLives = 3;
    [SerializeField] private int initialBombs = 3;
    [SerializeField] private int initialPowerLevel = 1;
    [SerializeField] private int maxPowerLevel = 5;

    [Header("Damage Model")]
    [SerializeField] private int hitsPerLife = 3;
    [SerializeField] private float hitInvulnerabilitySeconds = 0.75f;

    [Header("State")]
    [SerializeField] private float deathInvulnerabilitySeconds = 3f;
    [SerializeField] private float bombInvulnerabilitySeconds = 3f;
    [SerializeField] private float bombClearRadiusUnits = 100f;

    [Header("Input")]
    [SerializeField] private KeyCode focusKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode bombKey = KeyCode.X;

    [Header("Visual")]
    [SerializeField] private GameObject hitboxVisual;
    [SerializeField] private bool autoCreateHitboxVisual = true;

    [Header("Feedback")]
    [SerializeField] private bool enableDamageFlash = true;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float damageFlashDuration = 0.12f;

    public bool IsFocused { get; private set; }
    public int Lives { get; private set; }
    public int Bombs { get; private set; }
    public int PowerLevel { get; private set; }
    public int HitPoints => currentHitPoints;
    public int MaxHitPoints => Mathf.Max(1, hitsPerLife);

    private int currentHitPoints;
    private float invulnerabilityTimer;
    private float damageFlashTimer;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Color baseSpriteColor = Color.white;
    private Vector3 respawnPosition;

    private readonly HashSet<int> grazedBulletIds = new HashSet<int>();

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            EnsureEditorPresentation();
            return;
        }

        Lives = Mathf.Max(1, initialLives);
        Bombs = Mathf.Max(0, initialBombs);
        PowerLevel = Mathf.Clamp(initialPowerLevel, 1, maxPowerLevel);
        currentHitPoints = Mathf.Max(1, hitsPerLife);

        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        respawnPosition = transform.position;

        EnsureScoreSystemExists();
        EnsurePlayerVisual();
        EnsureHitboxVisual();
        SetHitboxVisible(false);

        if (spriteRenderer != null)
        {
            baseSpriteColor = spriteRenderer.color;
        }

        if (ScoreSystem.Instance != null)
        {
            ScoreSystem.Instance.OnExtendAwarded += HandleExtendAwarded;
        }
    }

    private void OnDestroy()
    {
        if (ScoreSystem.Instance != null)
        {
            ScoreSystem.Instance.OnExtendAwarded -= HandleExtendAwarded;
        }
    }

    private void OnEnable()
    {
        EnsureEditorPresentation();
    }

    private void OnValidate()
    {
        EnsureEditorPresentation();
    }

    private void EnsureEditorPresentation()
    {
        if (Application.isPlaying)
        {
            return;
        }

        hitsPerLife = Mathf.Max(1, hitsPerLife);
        maxPowerLevel = Mathf.Max(1, maxPowerLevel);
        initialPowerLevel = Mathf.Clamp(initialPowerLevel, 1, maxPowerLevel);
        initialLives = Mathf.Max(1, initialLives);
        initialBombs = Mathf.Max(0, initialBombs);

        spriteRenderer = GetComponent<SpriteRenderer>();
        EnsurePlayerVisual();
        EnsureHitboxVisual();
        SetHitboxVisible(false);

        Lives = Mathf.Max(1, initialLives);
        Bombs = Mathf.Max(0, initialBombs);
        PowerLevel = Mathf.Clamp(initialPowerLevel, 1, maxPowerLevel);
        currentHitPoints = Mathf.Max(1, hitsPerLife);
        invulnerabilityTimer = 0f;
        damageFlashTimer = 0f;

        if (spriteRenderer != null)
        {
            baseSpriteColor = spriteRenderer.color;
            spriteRenderer.color = baseSpriteColor;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            EnsureEditorPresentation();
            return;
        }

        IsFocused = InputBridge.IsKeyPressed(focusKey);
        SetHitboxVisible(IsFocused);

        HandleMovement();
        HandleBombInput();
        HandleGraze();
        HandleHitDetection();
        HandleInvulnerabilityVisual();
    }

    public void AddLife(int amount = 1)
    {
        Lives = Mathf.Max(0, Lives + amount);
    }

    public void AddBomb(int amount = 1)
    {
        Bombs = Mathf.Max(0, Bombs + amount);
    }

    public void CollectPowerItem(int amount = 1)
    {
        PowerLevel = Mathf.Clamp(PowerLevel + amount, 1, maxPowerLevel);
    }

    public void SetPowerLevel(int level)
    {
        PowerLevel = Mathf.Clamp(level, 1, maxPowerLevel);
    }

    public void ReceiveEnemyHit()
    {
        RegisterHit();
    }

    public void UseBomb()
    {
        if (Bombs <= 0)
        {
            return;
        }

        Bombs--;
        invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, bombInvulnerabilitySeconds);
        ClearEnemyBullets();

        if (ScoreSystem.Instance != null)
        {
            ScoreSystem.Instance.RegisterBombUse();
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = ReadMoveInput();

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        float speedPxPerFrame = IsFocused ? focusSpeedPxPerFrame : normalSpeedPxPerFrame;
        float unitsPerFrame = speedPxPerFrame / Mathf.Max(1f, pixelsPerUnit);
        float frameFactor = Time.deltaTime * referenceFrameRate;

        Vector3 delta = (Vector3)(moveInput * unitsPerFrame * frameFactor);
        transform.position += delta;

        if (clampToCameraBounds)
        {
            ClampToCamera();
        }
    }

    private void HandleBombInput()
    {
        if (InputBridge.WasKeyPressedThisFrame(bombKey))
        {
            UseBomb();
        }
    }

    private void HandleGraze()
    {
        if (ScoreSystem.Instance == null)
        {
            return;
        }

        float grazeRadiusUnits = grazeRadiusPixels / Mathf.Max(1f, pixelsPerUnit);
        float hitboxRadiusUnits = hitboxRadiusPixels / Mathf.Max(1f, pixelsPerUnit);

        if (enemyBulletMask.value != 0)
        {
            Collider2D[] nearBullets = Physics2D.OverlapCircleAll(transform.position, grazeRadiusUnits, enemyBulletMask);
            for (int i = 0; i < nearBullets.Length; i++)
            {
                Collider2D bullet = nearBullets[i];
                int id = bullet.GetInstanceID();
                if (grazedBulletIds.Contains(id))
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, bullet.transform.position);
                if (distance <= hitboxRadiusUnits)
                {
                    continue;
                }

                grazedBulletIds.Add(id);
                ScoreSystem.Instance.RegisterGraze();
            }
        }
        else
        {
            GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
            for (int i = 0; i < bullets.Length; i++)
            {
                GameObject bullet = bullets[i];
                int id = bullet.GetInstanceID();
                if (grazedBulletIds.Contains(id))
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, bullet.transform.position);
                if (distance > hitboxRadiusUnits && distance <= grazeRadiusUnits)
                {
                    grazedBulletIds.Add(id);
                    ScoreSystem.Instance.RegisterGraze();
                }
            }
        }

        if (grazedBulletIds.Count > 25000)
        {
            grazedBulletIds.Clear();
        }
    }

    private void HandleHitDetection()
    {
        if (invulnerabilityTimer > 0f)
        {
            return;
        }

        float hitboxRadiusUnits = hitboxRadiusPixels / Mathf.Max(1f, pixelsPerUnit);

        Collider2D hitBullet = null;

        if (enemyBulletMask.value != 0)
        {
            hitBullet = Physics2D.OverlapCircle(transform.position, hitboxRadiusUnits, enemyBulletMask);
            if (hitBullet == null)
            {
                hitBullet = FindTaggedEnemyBulletInRadius(hitboxRadiusUnits);
            }
        }
        else
        {
            hitBullet = FindTaggedEnemyBulletInRadius(hitboxRadiusUnits);
        }

        if (hitBullet == null)
        {
            return;
        }

        if (hitBullet.CompareTag("EnemyBullet"))
        {
            Destroy(hitBullet.gameObject);
        }

        RegisterHit();
    }

    private void RegisterHit()
    {
        if (invulnerabilityTimer > 0f)
        {
            return;
        }

        currentHitPoints = Mathf.Max(0, currentHitPoints - 1);
        TriggerDamageFlash();

        if (currentHitPoints > 0)
        {
            invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, hitInvulnerabilitySeconds);
            return;
        }

        ApplyDeathPenalty();
    }

    private void ApplyDeathPenalty()
    {
        Lives = Mathf.Max(0, Lives - 1);
        PowerLevel = Mathf.Max(1, PowerLevel - 1);
        grazedBulletIds.Clear();

        if (ScoreSystem.Instance != null)
        {
            ScoreSystem.Instance.RegisterDeath();
        }

        if (Lives <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position = respawnPosition;
        currentHitPoints = Mathf.Max(1, hitsPerLife);
        invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, deathInvulnerabilitySeconds);
    }

    private void TriggerDamageFlash()
    {
        if (!enableDamageFlash)
        {
            return;
        }

        damageFlashTimer = Mathf.Max(damageFlashTimer, damageFlashDuration);
    }

    private void HandleInvulnerabilityVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - Time.deltaTime);
        }

        if (damageFlashTimer > 0f)
        {
            damageFlashTimer = Mathf.Max(0f, damageFlashTimer - Time.deltaTime);
        }

        float alpha = baseSpriteColor.a;
        if (invulnerabilityTimer > 0f)
        {
            alpha = 0.35f + Mathf.PingPong(Time.time * 10f, 0.65f);
        }

        Color color = baseSpriteColor;
        color.a = alpha;

        if (enableDamageFlash && damageFlashTimer > 0f)
        {
            float t = damageFlashTimer / Mathf.Max(0.0001f, damageFlashDuration);
            Color flashColor = damageFlashColor;
            flashColor.a = alpha;
            color = Color.Lerp(color, flashColor, Mathf.Clamp01(t));
        }

        spriteRenderer.color = color;
    }

    private void ClearEnemyBullets()
    {
        if (enemyBulletMask.value != 0)
        {
            Collider2D[] bullets = Physics2D.OverlapCircleAll(transform.position, bombClearRadiusUnits, enemyBulletMask);
            for (int i = 0; i < bullets.Length; i++)
            {
                Destroy(bullets[i].gameObject);
            }
        }
        else
        {
            GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
            for (int i = 0; i < bullets.Length; i++)
            {
                if (Vector2.Distance(transform.position, bullets[i].transform.position) <= bombClearRadiusUnits)
                {
                    Destroy(bullets[i]);
                }
            }
        }
    }

    private Vector2 ReadMoveInput()
    {
        return InputBridge.GetMoveVector2D();
    }

    private void ClampToCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        if (!mainCamera.orthographic)
        {
            return;
        }

        float hitboxRadiusUnits = hitboxRadiusPixels / Mathf.Max(1f, pixelsPerUnit);

        Vector3 cameraPos = mainCamera.transform.position;
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;

        float minX = cameraPos.x - halfWidth + hitboxRadiusUnits;
        float maxX = cameraPos.x + halfWidth - hitboxRadiusUnits;
        float minY = cameraPos.y - halfHeight + hitboxRadiusUnits;
        float maxY = cameraPos.y + halfHeight - hitboxRadiusUnits;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }

    private void EnsureScoreSystemExists()
    {
        if (ScoreSystem.Instance != null)
        {
            return;
        }

        ScoreSystem existingScoreSystem = FindAnyObjectByType<ScoreSystem>();
        if (existingScoreSystem != null)
        {
            return;
        }

        GameObject scoreObject = new GameObject("ScoreSystem");
        scoreObject.AddComponent<ScoreSystem>();
    }

    private void EnsurePlayerVisual()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = CreateDiamondSprite(24, new Color(0.9f, 0.95f, 1f, 1f));
        }

        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 50);
    }

    private void EnsureHitboxVisual()
    {
        if (hitboxVisual != null)
        {
            return;
        }

        if (!autoCreateHitboxVisual)
        {
            return;
        }

        GameObject hitbox = new GameObject("HitboxVisual");
        hitbox.transform.SetParent(transform);
        hitbox.transform.localPosition = Vector3.zero;
        hitbox.transform.localRotation = Quaternion.identity;

        SpriteRenderer renderer = hitbox.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateRingSprite(24, Color.white);
        renderer.sortingOrder = 120;

        float diameterUnits = (hitboxRadiusPixels * 2f) / Mathf.Max(1f, pixelsPerUnit);
        hitbox.transform.localScale = new Vector3(diameterUnits, diameterUnits, 1f);

        hitboxVisual = hitbox;
    }

    private void SetHitboxVisible(bool isVisible)
    {
        if (hitboxVisual != null)
        {
            hitboxVisual.SetActive(isVisible);
        }
    }

    private Collider2D FindTaggedEnemyBulletInRadius(float radiusUnits)
    {
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        for (int i = 0; i < bullets.Length; i++)
        {
            if (Vector2.Distance(transform.position, bullets[i].transform.position) > radiusUnits)
            {
                continue;
            }

            Collider2D collider = bullets[i].GetComponent<Collider2D>();
            if (collider != null)
            {
                return collider;
            }
        }

        return null;
    }

    private void HandleExtendAwarded(int _)
    {
        AddLife(1);
    }

    private static Sprite CreateDiamondSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        int center = size / 2;
        int radius = Mathf.Max(1, center - 1);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - center);
                int dy = Mathf.Abs(y - center);
                bool inside = (dx + dy) <= radius;
                texture.SetPixel(x, y, inside ? color : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateRingSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        float center = (size - 1) * 0.5f;
        float outer = center;
        float inner = outer * 0.65f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                bool isRing = dist <= outer && dist >= inner;
                texture.SetPixel(x, y, isRing ? color : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void OnDrawGizmosSelected()
    {
        float ppu = Mathf.Max(1f, pixelsPerUnit);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitboxRadiusPixels / ppu);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grazeRadiusPixels / ppu);
    }
}
