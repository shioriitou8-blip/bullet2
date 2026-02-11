using System;
using UnityEngine;

public class ScoreSystem : MonoBehaviour
{
    public static ScoreSystem Instance { get; private set; }

    [Header("Extends")]
    [SerializeField] private int[] extendThresholds = { 10000000, 30000000, 60000000 };

    [Header("Multiplier")]
    [SerializeField] private float multiplierGrowthPerSecond = 0.08f;
    [SerializeField] private float maxMultiplier = 8f;

    [Header("Bonuses")]
    [SerializeField] private int grazeBasePoints = 50;
    [SerializeField] private int grazeScalingPerGraze = 10;
    [SerializeField] private int pointBlankBasePoints = 120;
    [SerializeField] private int noBombStageBonus = 250000;
    [SerializeField] private int grazeStageBonusPerGraze = 25;

    public int Score => score;
    public float Multiplier => multiplier;
    public int GrazeCount => grazeCount;
    public bool UsedBombThisStage => usedBombThisStage;

    public event Action<int> OnExtendAwarded;
    public event Action<int> OnScoreChanged;
    public event Action<float> OnMultiplierChanged;

    private int score;
    private float multiplier = 1f;
    private int grazeCount;
    private bool usedBombThisStage;
    private int nextExtendIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        float previousMultiplier = multiplier;
        multiplier = Mathf.Min(maxMultiplier, multiplier + (multiplierGrowthPerSecond * Time.deltaTime));
        if (!Mathf.Approximately(previousMultiplier, multiplier))
        {
            OnMultiplierChanged?.Invoke(multiplier);
        }
    }

    public void ResetRun()
    {
        score = 0;
        multiplier = 1f;
        grazeCount = 0;
        usedBombThisStage = false;
        nextExtendIndex = 0;
        OnScoreChanged?.Invoke(score);
        OnMultiplierChanged?.Invoke(multiplier);
    }

    public int AddScore(int basePoints, bool applyMultiplier = true)
    {
        if (basePoints <= 0)
        {
            return 0;
        }

        int gained = applyMultiplier
            ? Mathf.RoundToInt(basePoints * multiplier)
            : basePoints;

        score = Mathf.Max(0, score + gained);
        OnScoreChanged?.Invoke(score);
        CheckExtends();
        return gained;
    }

    public int RegisterGraze()
    {
        grazeCount++;
        int grazeBonus = grazeBasePoints + (grazeCount * grazeScalingPerGraze);
        AddScore(grazeBonus);
        return grazeBonus;
    }

    public int AddPointBlankBonus(float distance, float maxDistance)
    {
        if (maxDistance <= 0f || distance > maxDistance)
        {
            return 0;
        }

        float proximity = 1f - Mathf.Clamp01(distance / maxDistance);
        int bonus = pointBlankBasePoints + Mathf.RoundToInt(pointBlankBasePoints * proximity);
        AddScore(bonus);
        return bonus;
    }

    public void RegisterBombUse()
    {
        usedBombThisStage = true;
    }

    public void RegisterDeath()
    {
        multiplier = 1f;
        OnMultiplierChanged?.Invoke(multiplier);
    }

    public int CompleteStageAndGetBonus()
    {
        int bonus = grazeCount * grazeStageBonusPerGraze;
        if (!usedBombThisStage)
        {
            bonus += noBombStageBonus;
        }

        AddScore(bonus, applyMultiplier: false);
        usedBombThisStage = false;
        grazeCount = 0;
        return bonus;
    }

    public void StartStage()
    {
        usedBombThisStage = false;
        grazeCount = 0;
    }

    private void CheckExtends()
    {
        while (nextExtendIndex < extendThresholds.Length && score >= extendThresholds[nextExtendIndex])
        {
            int threshold = extendThresholds[nextExtendIndex];
            nextExtendIndex++;
            OnExtendAwarded?.Invoke(threshold);
        }
    }
}
