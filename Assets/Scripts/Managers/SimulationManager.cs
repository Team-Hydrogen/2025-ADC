using System;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    public float ElapsedTimeInMinutes { get; private set; } = 0.0f;
    public float TimeScale { get; private set; } = 1.0f;

    private float _endTime;

    private const float SecondsPerMinutes = 60f;
    private const float SkipForwardTimeInMinutes = 0.16666666666f;
    private const float SkipBackwardTimeInMinutes = 0.16666666666f;
    
    private int _timeScaleFactorIndex = 0;
    private readonly float[] _timeScaleFactors = { 1f, 10f, 100f, 1_000f, 10_000f, 100_000f };
    
    public static event Action<float> ElapsedTimeUpdated;
    public static event Action<float> TimeScaleSet;

    private bool _isPaused = false;
    
    
    #region Event Functions
    
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
        DataManager.DataLoaded += OnDataLoaded;

        InputManager.Instance.OnSkipForward += IncreaseTime;
        InputManager.Instance.OnSkipBackward += DecreaseTime;
        InputManager.Instance.OnPlayPause += PlayPause;
        InputManager.Instance.OnAccelerateTime += IncreaseTimeScale;
        InputManager.Instance.OnDecelerateTime += DecreaseTimeScale;
    }

    private void OnDisable()
    {
        UIManager.IncreaseTime -= IncreaseTime;
        UIManager.DecreaseTime -= DecreaseTime;
        UIManager.IncreaseTimeScale -= IncreaseTimeScale;
        UIManager.DecreaseTimeScale -= DecreaseTimeScale;
        UIManager.PauseTime -= PauseTime;
        UIManager.ResumeTime -= ResumeTime;
        DataManager.DataLoaded -= OnDataLoaded;

        InputManager.Instance.OnSkipForward -= IncreaseTime;
        InputManager.Instance.OnSkipBackward -= DecreaseTime;
        InputManager.Instance.OnPlayPause -= PlayPause;
        InputManager.Instance.OnAccelerateTime -= IncreaseTimeScale;
        InputManager.Instance.OnDecelerateTime -= DecreaseTimeScale;
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

        // TODO: Might need to reorder this call and put it after the invoke
        CheckIfEndOfSimulation();

        ElapsedTimeUpdated?.Invoke(ElapsedTimeInMinutes);
    }
    
    private void PauseTime()
    {
        Time.timeScale = 0f;
        _isPaused = true;
        //TimeScale = 1f;
        TimeScaleSet?.Invoke(TimeScale);
    }

    private void ResumeTime()
    {
        Time.timeScale = 1f;
        _isPaused = false;
        TimeScale = _timeScaleFactors[_timeScaleFactorIndex];
        TimeScaleSet?.Invoke(TimeScale);
    }

    private void PlayPause()
    {
        if (_isPaused)
        {
            ResumeTime();
        }
        else
        {
            PauseTime();
        }
    }


    private void IncreaseTime()
    {
        ElapsedTimeInMinutes = Mathf.Min(ElapsedTimeInMinutes + SkipForwardTimeInMinutes * TimeScale, _endTime);

        CheckIfEndOfSimulation();
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

    private void CheckIfEndOfSimulation()
    {
        if (ElapsedTimeInMinutes >= _endTime)
        {
            ElapsedTimeInMinutes = _endTime;
            TimeScale = 0f;
            TimeScaleSet?.Invoke(TimeScale);
        }
    }

    #endregion

    private void OnDataLoaded(DataLoadedEventArgs eventArgs)
    {
        // Gets last time in the trajectory data
        _endTime = float.Parse(eventArgs.NominalTrajectoryData[^1][0]);
    }
}
