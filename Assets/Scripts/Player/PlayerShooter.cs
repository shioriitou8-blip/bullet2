using System;
using UnityEngine;

public enum ShotType
{
    TypeA,
    TypeB,
    TypeC
}

[DisallowMultipleComponent]
public class PlayerShooter : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode fireKey = KeyCode.Z;
    [SerializeField] private bool allowShotTypeCycling = true;
    [SerializeField] private KeyCode cycleForwardKey = KeyCode.E;
    [SerializeField] private KeyCode cycleBackwardKey = KeyCode.Q;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private float spawnOffsetUnits = 0.22f;
    [SerializeField] private float pointBlankDistance = 1.25f;
    [SerializeField] private LayerMask enemyMask;

    [Header("Shot Type")]
    [SerializeField] private ShotType currentShotType = ShotType.TypeA;

    public ShotType CurrentShotType => currentShotType;

    private PlayerController playerController;
    private float nextFireTime;

    private struct ShotProfile
    {
        public float fireInterval;
        public float damage;
        public float projectileSpeed;
        public int projectileCount;
        public float spreadAngle;
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        HandleShotTypeInput();

        if (IsFiringPressed())
        {
            TryFire();
        }
    }

    private void HandleShotTypeInput()
    {
        if (!allowShotTypeCycling)
        {
            return;
        }

        if (InputBridge.WasKeyPressedThisFrame(cycleForwardKey))
        {
            currentShotType = (ShotType)(((int)currentShotType + 1) % Enum.GetValues(typeof(ShotType)).Length);
        }

        if (InputBridge.WasKeyPressedThisFrame(cycleBackwardKey))
        {
            int length = Enum.GetValues(typeof(ShotType)).Length;
            currentShotType = (ShotType)(((int)currentShotType - 1 + length) % length);
        }
    }

    private bool IsFiringPressed()
    {
        if (InputBridge.IsKeyPressed(fireKey))
        {
            return true;
        }

        return InputBridge.IsFire1Pressed();
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        bool isFocused = playerController != null && playerController.IsFocused;
        int powerLevel = playerController != null ? playerController.PowerLevel : 1;

        ShotProfile profile = BuildProfile(currentShotType, powerLevel, isFocused);
        nextFireTime = Time.time + profile.fireInterval;
        FireProfile(profile);
    }

private ShotProfile BuildProfile(ShotType shotType, int powerLevel, bool isFocused)
    {
        powerLevel = Mathf.Clamp(powerLevel, 1, 5);
        int powerIndex = powerLevel - 1;

        ShotProfile profile = new ShotProfile
        {
            fireInterval = 0.1f,
            damage = 1f,
            projectileSpeed = 18f,
            projectileCount = powerLevel,
            spreadAngle = 0f
        };

        switch (shotType)
        {
            case ShotType.TypeA:
                profile.fireInterval = 0.065f;
                profile.damage = 0.55f + (0.18f * powerIndex);
                profile.projectileSpeed = 16f;
                profile.spreadAngle = 34f;
                break;

            case ShotType.TypeB:
                profile.fireInterval = 0.14f;
                profile.damage = 2.3f + (0.45f * powerIndex);
                profile.projectileSpeed = 21f;
                profile.spreadAngle = 8f;
                break;

            case ShotType.TypeC:
                profile.fireInterval = 0.1f;
                profile.damage = 1.1f + (0.3f * powerIndex);
                profile.projectileSpeed = 18f;
                profile.spreadAngle = 20f;
                break;
        }

        if (isFocused)
        {
            profile.spreadAngle *= 0.3f;
            profile.projectileSpeed += 2f;
            if (shotType == ShotType.TypeB)
            {
                profile.damage *= 1.1f;
            }
        }

        profile.projectileCount = Mathf.Max(1, profile.projectileCount);
        return profile;
    }

    private void FireProfile(ShotProfile profile)
    {
        if (profile.projectileCount == 1)
        {
            SpawnProjectileAtAngle(0f, profile);
            return;
        }

        float startAngle = -profile.spreadAngle * 0.5f;
        float step = profile.projectileCount > 1
            ? profile.spreadAngle / (profile.projectileCount - 1)
            : 0f;

        for (int i = 0; i < profile.projectileCount; i++)
        {
            float angle = startAngle + (step * i);
            SpawnProjectileAtAngle(angle, profile);
        }
    }

    private void SpawnProjectileAtAngle(float angleOffset, ShotProfile profile)
    {
        Vector2 direction = Quaternion.Euler(0f, 0f, angleOffset) * Vector2.up;
        Vector3 spawnPosition = transform.position + (Vector3)(direction * spawnOffsetUnits);

        GameObject projectile = projectilePrefab != null
            ? Instantiate(projectilePrefab, spawnPosition, Quaternion.identity)
            : CreateFallbackProjectile(spawnPosition);

        PlayerProjectile projectileLogic = projectile.GetComponent<PlayerProjectile>();
        if (projectileLogic == null)
        {
            projectileLogic = projectile.AddComponent<PlayerProjectile>();
        }

        projectileLogic.Initialize(
            direction,
            profile.projectileSpeed,
            profile.damage,
            enemyMask,
            transform,
            projectileLifetime,
            pointBlankDistance);

        Destroy(projectile, projectileLifetime + 0.1f);
    }

    private GameObject CreateFallbackProjectile(Vector3 position)
    {
        GameObject bullet = new GameObject("PlayerProjectile");
        bullet.transform.position = position;

        CircleCollider2D collider = bullet.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.05f;

        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        return bullet;
    }
}
