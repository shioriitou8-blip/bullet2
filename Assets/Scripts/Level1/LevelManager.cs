using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LevelManager : MonoBehaviour
{
    [Header("Timeline (seconds)")]
    [SerializeField] private float introEnd = 30f;
    [SerializeField] private float firstPatternEnd = 60f;
    [SerializeField] private float radialSimpleEnd = 90f;
    [SerializeField] private float combinationEnd = 120f;
    [SerializeField] private float highPressureEnd = 150f;
    [SerializeField] private float preBossEnd = 180f;

    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private BulletPatternSystem bulletPatternSystem;
    [SerializeField] private ObjectPool objectPool;
    [SerializeField] private Transform playerTarget;

    [Header("MiniBoss")]
    [SerializeField] private Vector2 miniBossSpawnPosition = new Vector2(0f, 3.6f);

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip phaseMusic;
    [SerializeField] private AudioClip preBossMusic;
    [SerializeField] private AudioClip miniBossMusic;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.85f;

    [Header("Transition Visual")]
    [SerializeField] private Canvas transitionCanvas;
    [SerializeField] private Image transitionOverlay;
    [SerializeField] private Color preBossColor = new Color(0.9f, 0.15f, 0.12f, 0.45f);
    [SerializeField] private float flashDuration = 1.2f;

    public Level1Phase CurrentPhase { get; private set; } = Level1Phase.Intro;
    public float ElapsedTime { get; private set; }

    public event Action<Level1Phase> OnPhaseChanged;
    public event Action OnLevelCompleted;

    private float transitionTimer;
    private bool minibossSpawned;
    private bool levelCompleted;

    private void Awake()
    {
        ResolveReferences();
        EnsureTransitionOverlay();
        ConfigureMusicSource();
        PlayMusic(phaseMusic, fallbackPitch: 1f);
    }

    private void Start()
    {
        if (ScoreSystem.Instance != null)
        {
            ScoreSystem.Instance.StartStage();
        }

        SetPhase(Level1Phase.Intro);
    }

    private void Update()
    {
        if (levelCompleted)
        {
            TickTransitionOverlay();
            return;
        }

        ElapsedTime += Time.deltaTime;
        TickTimelinePhase();
        TickTransitionOverlay();
    }

    private void ResolveReferences()
    {
        if (waveManager == null)
        {
            waveManager = FindAnyObjectByType<WaveManager>();
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindAnyObjectByType<EnemySpawner>();
        }

        if (bulletPatternSystem == null)
        {
            bulletPatternSystem = FindAnyObjectByType<BulletPatternSystem>();
        }

        if (objectPool == null)
        {
            objectPool = FindAnyObjectByType<ObjectPool>();
        }

        if (playerTarget == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        if (enemySpawner != null && playerTarget != null)
        {
            enemySpawner.SetPlayerTarget(playerTarget);
        }
    }

    private void TickTimelinePhase()
    {
        if (CurrentPhase != Level1Phase.MiniBoss && CurrentPhase != Level1Phase.Completed)
        {
            Level1Phase targetPhase = DeterminePhaseFromTime(ElapsedTime);
            if (targetPhase != CurrentPhase)
            {
                SetPhase(targetPhase);
            }
        }

        if (!minibossSpawned && ElapsedTime >= preBossEnd)
        {
            StartMiniBoss();
        }
    }

    private Level1Phase DeterminePhaseFromTime(float seconds)
    {
        if (seconds < introEnd)
        {
            return Level1Phase.Intro;
        }

        if (seconds < firstPatternEnd)
        {
            return Level1Phase.FirstPattern;
        }

        if (seconds < radialSimpleEnd)
        {
            return Level1Phase.RadialSimple;
        }

        if (seconds < combinationEnd)
        {
            return Level1Phase.Combination;
        }

        if (seconds < highPressureEnd)
        {
            return Level1Phase.HighPressure;
        }

        if (seconds < preBossEnd)
        {
            return Level1Phase.PreBoss;
        }

        return Level1Phase.MiniBoss;
    }

    private void SetPhase(Level1Phase nextPhase)
    {
        CurrentPhase = nextPhase;
        OnPhaseChanged?.Invoke(CurrentPhase);

        if (CurrentPhase == Level1Phase.PreBoss)
        {
            PlayMusic(preBossMusic, fallbackPitch: 1.08f);
            TriggerTransitionFlash();
        }
    }

    private void StartMiniBoss()
    {
        if (minibossSpawned || enemySpawner == null)
        {
            return;
        }

        minibossSpawned = true;
        SetPhase(Level1Phase.MiniBoss);

        if (waveManager != null)
        {
            waveManager.SetSpawningEnabled(false);
        }

        enemySpawner.DespawnAllActiveEnemies();
        PlayMusic(miniBossMusic, fallbackPitch: 1.16f);
        TriggerTransitionFlash();

        MiniBossController miniBoss = enemySpawner.SpawnMiniBoss(miniBossSpawnPosition, HandleMiniBossDefeated);
        if (miniBoss == null)
        {
            CompleteLevel();
        }
    }

    private void HandleMiniBossDefeated(MiniBossController _)
    {
        CompleteLevel();
    }

    private void CompleteLevel()
    {
        if (levelCompleted)
        {
            return;
        }

        levelCompleted = true;
        SetPhase(Level1Phase.Completed);
        TriggerTransitionFlash();

        if (ScoreSystem.Instance != null)
        {
            ScoreSystem.Instance.CompleteStageAndGetBonus();
        }

        OnLevelCompleted?.Invoke();
    }

    private void ConfigureMusicSource()
    {
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.spatialBlend = 0f;
            return;
        }

        AudioSource source = FindAnyObjectByType<AudioSource>();
        if (source == null)
        {
            GameObject musicObject = new GameObject("LevelMusicSource");
            source = musicObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = true;
        source.volume = musicVolume;
        source.spatialBlend = 0f;
        musicSource = source;
    }

    private void PlayMusic(AudioClip clip, float fallbackPitch)
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.volume = musicVolume;
        if (clip == null)
        {
            musicSource.pitch = fallbackPitch;
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }

            return;
        }

        if (musicSource.clip != clip)
        {
            musicSource.clip = clip;
            musicSource.pitch = 1f;
            musicSource.Play();
            return;
        }

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private void EnsureTransitionOverlay()
    {
        if (transitionCanvas == null)
        {
            transitionCanvas = FindAnyObjectByType<Canvas>();
            if (transitionCanvas == null)
            {
                GameObject canvasObj = new GameObject("LevelTransitionCanvas");
                transitionCanvas = canvasObj.AddComponent<Canvas>();
                transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                transitionCanvas.sortingOrder = 3000;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        if (transitionOverlay != null)
        {
            SetOverlayAlpha(0f);
            return;
        }

        GameObject overlayObj = new GameObject("TransitionOverlay");
        overlayObj.transform.SetParent(transitionCanvas.transform, false);
        transitionOverlay = overlayObj.AddComponent<Image>();
        transitionOverlay.color = new Color(preBossColor.r, preBossColor.g, preBossColor.b, 0f);

        RectTransform rect = transitionOverlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void TriggerTransitionFlash()
    {
        transitionTimer = Mathf.Max(0.05f, flashDuration);
    }

    private void TickTransitionOverlay()
    {
        if (transitionOverlay == null)
        {
            return;
        }

        if (transitionTimer <= 0f)
        {
            SetOverlayAlpha(0f);
            return;
        }

        transitionTimer = Mathf.Max(0f, transitionTimer - Time.deltaTime);
        float half = flashDuration * 0.5f;
        float alpha = transitionTimer > half
            ? Mathf.InverseLerp(flashDuration, half, transitionTimer)
            : Mathf.InverseLerp(0f, half, transitionTimer);
        SetOverlayAlpha(alpha * preBossColor.a);
    }

    private void SetOverlayAlpha(float alpha)
    {
        Color color = preBossColor;
        color.a = Mathf.Clamp01(alpha);
        transitionOverlay.color = color;
    }
}
