using System;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    public float ElapsedTimeInMinutes { get; private set; } = 0.0f;
    public float TimeScale { get; private set; } = 1.0f;

    private const float SecondsPerMinutes = 60f;
    private const float SkipForwardTimeInMinutes = 10.0f;
    private const float SkipBackwardTimeInMinutes = 10.0f;
    
    private int _timeScaleFactorIndex = 0;
    private readonly float[] _timeScaleFactors = { 1f, 10f, 100f, 1_000f, 10_000f, 100_000f };
    
    public static event Action<float> ElapsedTimeUpdated;
    public static event Action<float> TimeScaleSet;
    
    
    #region Event Functions
    
    private void Start()
    {
        // 
    }
    
    private void Update()
    {
        UpdateElapsedTime();
    }

    private void OnEnable()
    {
        UIManager.IncreaseTime += IncreaseTime;
        UIManager.DecreaseTime += DecreaseTime;
        UIManager.IncreaseTimeScale += IncreaseTimeScale;
        UIManager.DecreaseTimeScale += DecreaseTimeScale;
        UIManager.PauseTime += PauseTime;
        UIManager.ResumeTime += ResumeTime;
    }

    private void OnDisable()
    {
        UIManager.IncreaseTime -= IncreaseTime;
        UIManager.DecreaseTime -= DecreaseTime;
        UIManager.IncreaseTimeScale -= IncreaseTimeScale;
        UIManager.DecreaseTimeScale -= DecreaseTimeScale;
        UIManager.PauseTime -= PauseTime;
        UIManager.ResumeTime -= ResumeTime;
    }
    
    #endregion
    
    
    #region Time
    
    private void UpdateElapsedTime()
    {
        if (TimeScale == 0f)
        {
            return;
        }
        
        ElapsedTimeInMinutes += Time.deltaTime * TimeScale / SecondsPerMinutes;
        ElapsedTimeUpdated?.Invoke(ElapsedTimeInMinutes);
    }
    
    private void PauseTime()
    {
        Time.timeScale = 0f;
        //TimeScale = 1f;
        TimeScaleSet?.Invoke(TimeScale);
    }

    private void ResumeTime()
    {
        Time.timeScale = 1f;
        TimeScale = _timeScaleFactors[_timeScaleFactorIndex];
        TimeScaleSet?.Invoke(TimeScale);
    }
    
    private void IncreaseTime()
    {
        ElapsedTimeInMinutes += SkipForwardTimeInMinutes * TimeScale;
    }

    private void DecreaseTime()
    {
        ElapsedTimeInMinutes = Mathf.Max(ElapsedTimeInMinutes - SkipBackwardTimeInMinutes * TimeScale, 0f);
    }
    
    private void IncreaseTimeScale()
    {
        _timeScaleFactorIndex = Mathf.Min(_timeScaleFactorIndex + 1, _timeScaleFactors.Length - 1);
        TimeScale = _timeScaleFactors[_timeScaleFactorIndex];
        TimeScaleSet?.Invoke(TimeScale);
    }

    private void DecreaseTimeScale()
    {
        _timeScaleFactorIndex = Mathf.Max(_timeScaleFactorIndex - 1, 0);
        TimeScale = _timeScaleFactors[_timeScaleFactorIndex];
        TimeScaleSet?.Invoke(TimeScale);
    }
    
    #endregion
}
