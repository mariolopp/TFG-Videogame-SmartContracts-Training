using System;
using UnityEngine;

// Lleva los puntos y las vidas avisa con eventos, no maneja UI
public class ContractScoreManager : MonoBehaviour
{
    public int Score { get; private set; }
    public int Lives { get; private set; }
    public bool IsDead => Lives <= 0;

    public event Action<int> OnScoreChanged;
    public event Action<int> OnLivesChanged;
    // Progreso 0-1 hacia la siguiente vida extra y si en este cambio se ha completado alguna
    public event Action<float, bool> OnExtraLifeProgressChanged;

    public float ExtraLifeProgress => config == null || config.pointsForExtraLife <= 0 ? 0f
        : Mathf.Clamp01((Score - (nextExtraLifeAt - config.pointsForExtraLife)) / (float)config.pointsForExtraLife);

    private ContractApproverConfig config;
    private int nextExtraLifeAt;

    public void ResetSession(ContractApproverConfig cfg)
    {
        config = cfg;
        Score = 0;
        Lives = cfg.startLives;
        nextExtraLifeAt = cfg.pointsForExtraLife;
        OnScoreChanged?.Invoke(Score);
        OnLivesChanged?.Invoke(Lives);
        OnExtraLifeProgressChanged?.Invoke(ExtraLifeProgress, false);
    }

    public void AddPoints(int points)
    {
        if (points <= 0) return;
        Score += points;
        OnScoreChanged?.Invoke(Score);

        // Vida extra cada X puntos acumulados, sin restar los puntos
        bool completed = false;
        while (config.pointsForExtraLife > 0 && Score >= nextExtraLifeAt)
        {
            nextExtraLifeAt += config.pointsForExtraLife;
            ChangeLives(+1);
            completed = true;
        }
        OnExtraLifeProgressChanged?.Invoke(ExtraLifeProgress, completed);
    }

    public void RemovePoints(int points)
    {
        if (points <= 0) return;
        Score = Mathf.Max(0, Score - points);
        OnScoreChanged?.Invoke(Score);
        OnExtraLifeProgressChanged?.Invoke(ExtraLifeProgress, false);
    }

    public void LoseLives(int amount)
    {
        if (amount > 0) ChangeLives(-amount);
    }

    private void ChangeLives(int delta)
    {
        int old = Lives;
        Lives = Mathf.Clamp(Lives + delta, 0, config.maxLives);
        if (Lives != old) OnLivesChanged?.Invoke(Lives);
    }
}
