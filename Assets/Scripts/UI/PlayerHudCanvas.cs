using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[ExecuteAlways]
public class PlayerHudCanvas : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private ScoreSystem scoreSystem;
    [SerializeField] private BossThreePhaseController boss;
    [SerializeField] private Text statsText;
    [SerializeField] private Image livesFrameImage;
    [SerializeField] private Image healthBarFillImage;
    [SerializeField] private Image bossBarFrameImage;
    [SerializeField] private Image bossBarFillImage;
    [SerializeField] private Text bossPhaseText;

    [Header("Health Bar")]
    [SerializeField] private bool autoUseLivesFrameAsHealthBar = true;
    [SerializeField] private bool autoConfigureFill = true;

    [Header("Boss HUD")]
    [SerializeField] private bool showBossHud = true;
    [SerializeField] private bool autoConfigureBossBar = true;

    [Header("Display")]
    [SerializeField] private bool showScore = true;
    [SerializeField] private bool showMultiplier = true;
    [SerializeField] private bool showGraze = true;

    [Header("Edit Mode Preview")]
    [SerializeField] private bool previewInEditMode = true;
    [SerializeField] private int previewLives = 3;
    [SerializeField] private int previewBombs = 3;
    [SerializeField] private int previewPower = 1;
    [SerializeField] private int previewHitPoints = 3;
    [SerializeField] private int previewMaxHitPoints = 3;
    [SerializeField] private int previewScore = 0;
    [SerializeField] private float previewMultiplier = 1f;
    [SerializeField] private int previewGraze = 0;

    private readonly StringBuilder builder = new StringBuilder(128);
    private static Sprite fallbackBarSprite;

    private void Reset()
    {
        AutoAssign();
        RefreshHud();
    }

    private void Awake()
    {
        AutoAssign();
        RefreshHud();
    }

    private void OnValidate()
    {
        AutoAssign();
        RefreshHud();
    }

    private void Update()
    {
        RefreshHud();
    }

    private void AutoAssign()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        if (scoreSystem == null)
        {
            scoreSystem = ScoreSystem.Instance;
            if (scoreSystem == null)
            {
                scoreSystem = FindAnyObjectByType<ScoreSystem>();
            }
        }

        if (boss == null)
        {
            boss = FindAnyObjectByType<BossThreePhaseController>();
        }

        if (statsText == null)
        {
            statsText = GetComponentInChildren<Text>(true);
        }

        if (livesFrameImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject != gameObject)
                {
                    livesFrameImage = images[i];
                    break;
                }
            }
        }

        if (healthBarFillImage == null && autoUseLivesFrameAsHealthBar)
        {
            healthBarFillImage = livesFrameImage;
        }

        if (bossBarFrameImage == null || bossBarFillImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null)
                {
                    continue;
                }

                if (bossBarFrameImage == null && images[i].name == "BossBarFrame")
                {
                    bossBarFrameImage = images[i];
                }

                if (bossBarFillImage == null && images[i].name == "BossBarFill")
                {
                    bossBarFillImage = images[i];
                }
            }
        }

        if (bossPhaseText == null)
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "BossPhaseText")
                {
                    bossPhaseText = texts[i];
                    break;
                }
            }
        }

        ConfigureHealthBarFill();
        ConfigureBossBarFill();
    }

    private void RefreshHud()
    {
        if (statsText == null)
        {
            return;
        }

        if (!Application.isPlaying)
        {
            if (!previewInEditMode)
            {
                return;
            }

            statsText.text = BuildPreviewText();
            UpdateHealthBar(previewHitPoints, previewMaxHitPoints);
            UpdateBossHud();
            return;
        }

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        if (scoreSystem == null)
        {
            scoreSystem = ScoreSystem.Instance;
            if (scoreSystem == null)
            {
                scoreSystem = FindAnyObjectByType<ScoreSystem>();
            }
        }

        builder.Length = 0;
        builder.Append("LIVES ").Append(player != null ? player.Lives : 0)
            .Append("  HP ").Append(player != null ? player.HitPoints : 0)
            .Append('/').Append(player != null ? player.MaxHitPoints : 0)
            .Append("  BOMBS ").Append(player != null ? player.Bombs : 0)
            .Append("\nPOWER ").Append(player != null ? player.PowerLevel : 0);

        if (showScore && scoreSystem != null)
        {
            builder.Append("  SCORE ").Append(scoreSystem.Score);
        }

        if (showMultiplier && scoreSystem != null)
        {
            builder.Append("\nMULT x").Append(scoreSystem.Multiplier.ToString("0.00"));
        }

        if (showGraze && scoreSystem != null)
        {
            builder.Append("  GRAZE ").Append(scoreSystem.GrazeCount);
        }

        statsText.text = builder.ToString();
        UpdateHealthBar(player != null ? player.HitPoints : 0, player != null ? player.MaxHitPoints : 1);
        UpdateBossHud();
    }

    private string BuildPreviewText()
    {
        builder.Length = 0;
        builder.Append("LIVES ").Append(Mathf.Max(0, previewLives))
            .Append("  HP ").Append(Mathf.Max(0, previewHitPoints))
            .Append('/').Append(Mathf.Max(1, previewMaxHitPoints))
            .Append("  BOMBS ").Append(Mathf.Max(0, previewBombs))
            .Append("\nPOWER ").Append(Mathf.Max(0, previewPower));

        if (showScore)
        {
            builder.Append("  SCORE ").Append(Mathf.Max(0, previewScore));
        }

        if (showMultiplier)
        {
            builder.Append("\nMULT x").Append(Mathf.Max(0f, previewMultiplier).ToString("0.00"));
        }

        if (showGraze)
        {
            builder.Append("  GRAZE ").Append(Mathf.Max(0, previewGraze));
        }

        return builder.ToString();
    }

    private void ConfigureHealthBarFill()
    {
        if (!autoConfigureFill || healthBarFillImage == null)
        {
            return;
        }

        if (healthBarFillImage.sprite == null)
        {
            healthBarFillImage.sprite = GetOrCreateFallbackBarSprite();
        }

        if (healthBarFillImage.type != Image.Type.Filled)
        {
            healthBarFillImage.type = Image.Type.Filled;
        }

        healthBarFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthBarFillImage.fillOrigin = 0;

        if (healthBarFillImage.color.a < 0.6f)
        {
            healthBarFillImage.color = new Color(0.95f, 0.25f, 0.25f, 0.95f);
        }
    }

    private void UpdateHealthBar(int current, int maximum)
    {
        if (healthBarFillImage == null)
        {
            return;
        }

        float maxValue = Mathf.Max(1f, maximum);
        float normalized = Mathf.Clamp01(Mathf.Max(0f, current) / maxValue);
        healthBarFillImage.fillAmount = normalized;
    }

    private void ConfigureBossBarFill()
    {
        if (!autoConfigureBossBar || bossBarFillImage == null)
        {
            return;
        }

        if (bossBarFillImage.sprite == null)
        {
            bossBarFillImage.sprite = GetOrCreateFallbackBarSprite();
        }

        if (bossBarFillImage.type != Image.Type.Filled)
        {
            bossBarFillImage.type = Image.Type.Filled;
        }

        bossBarFillImage.fillMethod = Image.FillMethod.Horizontal;
        bossBarFillImage.fillOrigin = 0;
        bossBarFillImage.color = new Color(0.96f, 0.18f, 0.18f, 0.95f);

        if (bossBarFrameImage != null && bossBarFrameImage.color.a < 0.55f)
        {
            bossBarFrameImage.color = new Color(0f, 0f, 0f, 0.7f);
        }
    }

private void UpdateBossHud()
    {
        if (!showBossHud)
        {
            SetBossHudVisible(false);
            return;
        }

        if (!Application.isPlaying)
        {
            if (!previewInEditMode)
            {
                SetBossHudVisible(false);
                return;
            }

            SetBossHudVisible(true);
            if (bossBarFillImage != null)
            {
                bossBarFillImage.fillAmount = 1f;
            }

            if (bossPhaseText != null)
            {
                bossPhaseText.text = "BOSS PHASE 1";
            }

            return;
        }

        if (boss == null)
        {
            boss = FindAnyObjectByType<BossThreePhaseController>();
        }

        bool hasBoss = boss != null && boss.IsEncounterActive;
        SetBossHudVisible(hasBoss);
        if (!hasBoss)
        {
            return;
        }

        float fill = boss.IsTransitioning ? boss.TransitionProgress01 : boss.PhaseHealth01;
        if (bossBarFillImage != null)
        {
            bossBarFillImage.fillAmount = fill;
        }

        if (bossPhaseText != null)
        {
            string suffix = boss.IsTransitioning ? "  TRANSITION" : string.Empty;
            bossPhaseText.text = $"BOSS PHASE {boss.CurrentPhaseNumber}{suffix}";
        }
    }

    private void SetBossHudVisible(bool isVisible)
    {
        if (bossBarFrameImage != null)
        {
            bossBarFrameImage.enabled = isVisible;
        }

        if (bossBarFillImage != null)
        {
            bossBarFillImage.enabled = isVisible;
        }

        if (bossPhaseText != null)
        {
            bossPhaseText.enabled = isVisible;
        }
    }

    private static Sprite GetOrCreateFallbackBarSprite()
    {
        if (fallbackBarSprite != null)
        {
            return fallbackBarSprite;
        }

        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        texture.name = "HudBarFallback";
        fallbackBarSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            4f);

        return fallbackBarSprite;
    }

    public Image LivesFrameImage => livesFrameImage;
}
