using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SatelliteManager : MonoBehaviour
{
    public static SatelliteManager instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private float trajectoryScale;
    
    [Header("Satellite")]
    [SerializeField] private GameObject satellite;
    [SerializeField] private GameObject velocityVector;
    [SerializeField] private Transform nominalSatelliteTransform;
    [SerializeField] private Transform offNominalSatelliteTransform;
    
    [Header("Celestial Bodies")]
    [SerializeField] private GameObject earth;
    [SerializeField] private GameObject moon;
    
    [Header("Nominal Trajectory")]
    [SerializeField] private LineRenderer futureNominalTrajectory;
    
    [Header("Off Nominal Trajectory")]
    [SerializeField] private LineRenderer futureOffNominalTrajectory;
    
    [Header("Time Scale")]
    [SerializeField] private float timeScale;
    
    private SatelliteState _currentState = SatelliteState.Nominal;

    private const float MinimumTimeScale = 1.0f;
    private const float MaximumTimeScale = 100_000.0f;
    
    private int _previousPointIndex = 0;
    private int _currentPointIndex = 0;
    private bool _isPlaying = false;
    private const int SkipTimeChange = 10;
    
    private float _progress = 0.0f;
    private float _estimatedElapsedTime;
    private float _totalNominalDistance = 0.0f;
    private float _totalOffNominalDistance = 0.0f;
    
    private List<string[]> _nominalPathPoints;
    private List<string[]> _offNominalPathPoints;
    private LineRenderer _currentNominalTrajectoryRenderer;
    private LineRenderer _currentOffNominalTrajectoryRenderer;
    
    // The second stage is the same as the service module.
    private const int SecondStageFireIndex = 120;
    
    private Vector3 _lastAutomaticSatellitePosition;
    private Vector3 _lastManualSatellitePosition;
    private int _lastAutomaticSatelliteIndex;
    private int _lastManualSatelliteIndex;
    private float _timeInterval;

    private readonly Dictionary<KeyCode, Vector3> _manualControlScheme = new()
    {
        {KeyCode.A, Vector3.left},
        {KeyCode.D, Vector3.right},
        {KeyCode.W, Vector3.forward},
        {KeyCode.S, Vector3.back},
        {KeyCode.Q, Vector3.down},
        {KeyCode.E, Vector3.up},
    };
    
    private const float MaximumManualControlTime = 5.0f;
    private const int MaximumFutureDataPoints = 60;
    
    public static event Action<int> OnCurrentIndexUpdated; 
    public static event Action<float> OnUpdateTime;
    public static event Action<Vector3> OnUpdateCoordinates;
    public static event Action<DistanceTravelledEventArgs> OnDistanceCalculated;
    public static event Action<float> OnTimeScaleSet;
    public static event Action<string> OnStageFired;
    public static event Action<SatelliteState> OnSatelliteStateUpdated;
    
    #region Material Variables
    
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private const float Intensity = 0.5f;

    private const float LowThreshold = 0.4000f;
    private const float MediumThreshold = 3.0000f;
    private const float HighThreshold = 8.0000f;
    
    private Renderer[] _vectorRenderers;
    private readonly Color[] _colors = {
        new(0.9373f, 0.2588f, 0.2588f, 1.0000f),
        new(1.0000f, 0.7569f, 0.0000f, 1.0000f),
        new(0.5451f, 0.9294f, 0.1804f, 1.0000f)
    };
    
    #endregion
    
    #region Event Functions
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
    }
    
    private void Start()
    {
        _totalNominalDistance = 0.0f;
        _vectorRenderers = velocityVector.GetComponentsInChildren<Renderer>();
        OnTimeScaleSet?.Invoke(timeScale);
    }
    
    private void Update()
    {
        if (_isPlaying)
        {
            UpdateSatellitePosition();
            
            if (_currentState == SatelliteState.Manual)
            {
                ManuallyControlSatellite();
            }
            
            if (_currentPointIndex == SecondStageFireIndex)
            {
                OnStageFired?.Invoke("Second Stage / Service Module Fired");
            }
        }
    }
    
    private void OnEnable()
    {
        CutsceneManager.OnCutsceneStart += UpdateModel;
        DataManager.OnDataLoaded += OnDataLoaded;
        DataManager.OnMissionStageUpdated += OnMissionStageUpdated;
        UIManager.OnBumpOffCoursePressed += OnBumpOffCourse;
        UIManager.OnCurrentPathChanged += OnChangedCurrentPath;
    }
    
    private void OnDisable()
    {
        CutsceneManager.OnCutsceneStart -= UpdateModel;
        DataManager.OnDataLoaded -= OnDataLoaded;
        DataManager.OnMissionStageUpdated -= OnMissionStageUpdated;
        UIManager.OnBumpOffCoursePressed -= OnBumpOffCourse;
        UIManager.OnCurrentPathChanged -= OnChangedCurrentPath;
    }
    
    #endregion
    
    #region Time Scale

    public void ForwardButtonPressed()
    {
        _progress = SkipTimeChange * timeScale;
    }

    public void BackwardButtonPressed()
    {
        _progress = -SkipTimeChange * timeScale;
    }
    
    public void FastForwardButtonPressed()
    {
        timeScale = Mathf.Min(MaximumTimeScale, timeScale * 10);
        OnTimeScaleSet?.Invoke(timeScale);
    }

    public void RewindButtonPressed()
    {
        timeScale = Mathf.Max(MinimumTimeScale, timeScale / 10);
        OnTimeScaleSet?.Invoke(timeScale);
    }
    
    #endregion

    private void OnDataLoaded(DataLoadedEventArgs data)
    {
        _nominalPathPoints = data.NominalTrajectoryData;
        _offNominalPathPoints = data.OffNominalTrajectoryData;
        _currentNominalTrajectoryRenderer = data.MissionStage.nominalLineRenderer;
        _currentOffNominalTrajectoryRenderer = data.MissionStage.offnominalLineRenderer;
        
        PlotTrajectory(_nominalPathPoints, _currentNominalTrajectoryRenderer, futureNominalTrajectory);
        PlotTrajectory(_offNominalPathPoints, _currentOffNominalTrajectoryRenderer, futureOffNominalTrajectory);
        
        _isPlaying = true;
        
        OnCurrentIndexUpdated?.Invoke(_currentPointIndex);
    }

    #region Plot Trajectories
    
    /// <summary>
    /// Visualizes a trajectory
    /// </summary>
    /// <param name="points">A list of three-dimensional points</param>
    /// <param name="past">A line to represent the past path</param>
    /// <param name="future">A line to represent the future path</param>
    private void PlotTrajectory(List<string[]> points, LineRenderer past, LineRenderer future)
    {
        // An array of three-dimensional points is constructed by processing the CSV file.
        var numberOfPoints = points.Count;
        var futurePoints = new Vector3[numberOfPoints];
        
        for (var index = 0; index < numberOfPoints; index++)
        {
            var point = points[index];
            
            try
            {
                var pointAsVector = new Vector3(
                    float.Parse(point[1]) * trajectoryScale,
                    float.Parse(point[2]) * trajectoryScale,
                    float.Parse(point[3]) * trajectoryScale);
                
                futurePoints[index] = pointAsVector;
            }
            catch
            {
                Debug.LogWarning($"No positional data exists on line {index}!");
            }
        }
        // The past trajectory's first point is added.
        past.positionCount = 2;
        past.SetPosition(0, futurePoints[0]);
        // The processed points are pushed to the future trajectory.
        future.positionCount = numberOfPoints;
        future.SetPositions(futurePoints);
    }
    
    #endregion

    /// <summary>
    /// Updates the position of the Orion capsule
    /// </summary>
    private void UpdateSatellitePosition()
    {
        if (_currentState is SatelliteState.Manual or SatelliteState.Returning)
        {
            return;
        }
        
        UpdateTimeIntervalAndProgress();
        _totalNominalDistance += UpdateGeneralSatellitePosition(_nominalPathPoints, nominalSatelliteTransform);
        _totalOffNominalDistance += UpdateGeneralSatellitePosition(_offNominalPathPoints, offNominalSatelliteTransform);
        UpdateAfter();
        SetSatelliteVisualToPosition();
    }

    private void SetSatelliteVisualToPosition()
    {
        switch (_currentState)
        {
            case SatelliteState.Nominal:
                satellite.transform.position = nominalSatelliteTransform.position;
                satellite.transform.rotation = nominalSatelliteTransform.rotation;
                break;
            case SatelliteState.OffNominal:
                satellite.transform.position = offNominalSatelliteTransform.position;
                satellite.transform.rotation = offNominalSatelliteTransform.rotation;
                break;
            case SatelliteState.Manual:
            case SatelliteState.Returning:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private float UpdateGeneralSatellitePosition(List<string[]> points, Transform satellitePosition)
    {
        var currentPoint = points[_currentPointIndex];
        var nextPoint = points[(_currentPointIndex + 1) % points.Count];

        var currentPosition = new Vector3(
            float.Parse(currentPoint[1]) * trajectoryScale,
            float.Parse(currentPoint[2]) * trajectoryScale,
            float.Parse(currentPoint[3]) * trajectoryScale
        );
        var nextPosition = new Vector3(
            float.Parse(nextPoint[1]) * trajectoryScale,
            float.Parse(nextPoint[2]) * trajectoryScale,
            float.Parse(nextPoint[3]) * trajectoryScale
        );

        // Interpolate position
        var previousPosition = satellitePosition.position;
        satellitePosition.position = Vector3.Lerp(currentPosition, nextPosition, _progress);

        var netDistance = Vector3.Distance(previousPosition, satellitePosition.position) / trajectoryScale;

        // Calculate satellite direction
        var direction = (nextPosition - currentPosition).normalized;
        
        const float rotationSpeed = 2.0f;
        
        if (direction == Vector3.zero)
        {
            return netDistance;
        }
        
        var targetRotation = Quaternion.LookRotation(direction);
        targetRotation *= Quaternion.Euler(90.0f, 0.0f, 0.0f);
        
        satellitePosition.rotation = Quaternion.Slerp(
            satellitePosition.rotation,
            targetRotation, 
            rotationSpeed * Time.deltaTime * timeScale
        );

        return netDistance;
    }
    
    private void UpdateTimeIntervalAndProgress()
    {
        var currentPoint = _offNominalPathPoints[_currentPointIndex];
        var nextPoint = _offNominalPathPoints[(_currentPointIndex + 1) % _offNominalPathPoints.Count];
        
        var currentTime = float.Parse(currentPoint[0]);
        var nextTime = float.Parse(nextPoint[0]);
        _timeInterval = (nextTime - currentTime) * 60.0f;
        
        _progress += Time.deltaTime / _timeInterval * timeScale;
        
        _estimatedElapsedTime = currentTime + (nextTime - currentTime) * _progress;
        
        OnUpdateTime?.Invoke(_estimatedElapsedTime);
    }

    private void UpdateAfter()
    {
        OnUpdateCoordinates?.Invoke(satellite.transform.position / trajectoryScale);
        CalculateDistances();
        
        UpdateTrajectory(
            nominalSatelliteTransform,
            _currentNominalTrajectoryRenderer,
            futureNominalTrajectory,
            false,
            true);
        UpdateTrajectory(
            offNominalSatelliteTransform,
            _currentOffNominalTrajectoryRenderer,
            futureOffNominalTrajectory,
            false,
            true);
        
        // Move to the next point when progress is complete
        if (_progress is < 1.0f and > -1.0f)
        {
            return;
        }

        _previousPointIndex = _currentPointIndex;
        // The simulation is reset.

        _currentPointIndex = (_currentPointIndex + Mathf.FloorToInt(_progress)) % _nominalPathPoints.Count;

        //if (_progress >= 0.0f)
        //{
        //    _currentPointIndex = (_currentPointIndex + Mathf.FloorToInt(_progress)) % _nominalPathPoints.Count;
        //}

        //if (_progress <= 0.0f)
        //{
        //    _currentPointIndex = (_currentPointIndex + Mathf.CeilToInt(_progress)) % _nominalPathPoints.Count;
        //}

        // The progress is reset.
        _progress %= 1;

        OnCurrentIndexUpdated?.Invoke(_currentPointIndex);
        
        UpdateTrajectory(
            nominalSatelliteTransform,
            _currentNominalTrajectoryRenderer,
            futureNominalTrajectory,
            true,
            false);
        UpdateTrajectory(
            offNominalSatelliteTransform,
            _currentOffNominalTrajectoryRenderer,
            futureOffNominalTrajectory,
            true,
            false);
        
        UpdateVelocityVector(_currentPointIndex);
    }

    private void UpdateTrajectory(Transform satelliteTransform, LineRenderer current, LineRenderer future, bool indexUpdated, bool positionUpdated)
    {
        if (positionUpdated)
        {
            future.SetPosition(0, satelliteTransform.position);
            current.SetPosition(current.positionCount - 1, satelliteTransform.position);
        }

        if (!indexUpdated)
        {
            return;
        }
        
        var indexChange = _currentPointIndex - _previousPointIndex;
        
        switch (indexChange)
        {
            case > 0:
            {
                var futureTrajectoryPoints = new Vector3[future.positionCount];
                future.GetPositions(futureTrajectoryPoints);
                
                var pointsToMove = new Vector3[indexChange];
                Array.Copy(
                    futureTrajectoryPoints,
                    0,
                    pointsToMove,
                    0,
                    indexChange);

                // Add these points to the past trajectory
                var pastTrajectoryPoints = new Vector3[current.positionCount];
                current.GetPositions(pastTrajectoryPoints);
                
                // Combine past trajectory points and new points
                var newPastTrajectoryPoints = new Vector3[pastTrajectoryPoints.Length + pointsToMove.Length];
                Array.Copy(
                    pastTrajectoryPoints,
                    0,
                    newPastTrajectoryPoints,
                    0,
                    pastTrajectoryPoints.Length);
                Array.Copy(
                    pointsToMove,
                    0,
                    newPastTrajectoryPoints,
                    pastTrajectoryPoints.Length,
                    pointsToMove.Length);
                
                // Update past trajectory
                current.positionCount = newPastTrajectoryPoints.Length;
                current.SetPositions(newPastTrajectoryPoints);
                
                // Remove moved points from future trajectory
                var newFuturePointCount = future.positionCount - indexChange;
                var newFutureTrajectoryPoints = new Vector3[newFuturePointCount];
                Array.Copy(
                    futureTrajectoryPoints,
                    indexChange,
                    newFutureTrajectoryPoints,
                    0,
                    newFuturePointCount);
                
                future.positionCount = newFuturePointCount;
                future.SetPositions(newFutureTrajectoryPoints);
                break;
            }
            case < 0:
            {
                indexChange = -indexChange;

                // Get all points in the past trajectory
                var pastTrajectoryPoints = new Vector3[current.positionCount];
                current.GetPositions(pastTrajectoryPoints);

                // Extract points to move back to the future trajectory
                var pointsToMove = new Vector3[indexChange];
                Array.Copy(
                    pastTrajectoryPoints,
                    pastTrajectoryPoints.Length - indexChange,
                    pointsToMove,
                    0,
                    indexChange);

                // Add these points back to the future trajectory
                var futureTrajectoryPoints = new Vector3[future.positionCount];
                future.GetPositions(futureTrajectoryPoints);
                
                var newFutureTrajectoryPoints = new Vector3[futureTrajectoryPoints.Length + pointsToMove.Length];
                Array.Copy(
                    pointsToMove,
                    0,
                    newFutureTrajectoryPoints,
                    0,
                    pointsToMove.Length);
                Array.Copy(
                    futureTrajectoryPoints,
                    0,
                    newFutureTrajectoryPoints,
                    pointsToMove.Length,
                    futureTrajectoryPoints.Length);
                
                // Update future trajectory
                future.positionCount = newFutureTrajectoryPoints.Length;
                future.SetPositions(newFutureTrajectoryPoints);

                // Remove moved points from past trajectory
                var newPastPointCount = current.positionCount - indexChange;
                var newPastTrajectoryPoints = new Vector3[newPastPointCount];
                Array.Copy(
                    pastTrajectoryPoints,
                    0,
                    newPastTrajectoryPoints,
                    0,
                    newPastPointCount);
                
                current.positionCount = newPastPointCount;
                current.SetPositions(newPastTrajectoryPoints);
                break;
            }
        }
    }
    
    private void OnChangedCurrentPath(SatelliteState state)
    {
        _currentState = state;
        OnSatelliteStateUpdated?.Invoke(_currentState);
    }
    
    private void OnMissionStageUpdated(MissionStage stage)
    {
        if (_currentNominalTrajectoryRenderer.Equals(stage.nominalLineRenderer))
        {
            return;
        }
        
        _currentNominalTrajectoryRenderer = stage.nominalLineRenderer;
        _currentNominalTrajectoryRenderer.SetPosition(0, nominalSatelliteTransform.position);

        _currentOffNominalTrajectoryRenderer = stage.offnominalLineRenderer;
        _currentOffNominalTrajectoryRenderer.SetPosition(0, offNominalSatelliteTransform.position);
        
        // trigger animation here if it is correct stage
    }
    
    private void UpdateVelocityVector(int currentIndex)
    {
        Vector3 vector;
        try
        {
            // A Vector3 variable is created to store and compute information about the current velocity vector.
            vector = new Vector3(
                float.Parse(_nominalPathPoints[currentIndex][4]),
                float.Parse(_nominalPathPoints[currentIndex][5]),
                float.Parse(_nominalPathPoints[currentIndex][6]));
        } 
        catch (FormatException e)
        {
            Debug.LogWarning($"Incorrect data format provided at line {currentIndex}: {e}");
            return;
        }
        
        velocityVector.transform.SetPositionAndRotation(
            satellite.transform.position - satellite.transform.forward,
            Quaternion.LookRotation(vector));
        velocityVector.transform.GetChild(0).localScale = new Vector3(1.0f, 1.0f, vector.magnitude);
        velocityVector.transform.GetChild(1).localPosition = new Vector3(0.0f, 0.0f, vector.magnitude + 1);
        
        var bracketIndex = vector.magnitude switch
        {
            >= HighThreshold => 0,
            >= MediumThreshold => 1,
            >= LowThreshold => 2,
            _ => 2
        };
        foreach (var meshRenderer in _vectorRenderers)
        {
            meshRenderer.material.SetColor(BaseColor, _colors[bracketIndex]);
            meshRenderer.material.SetColor(EmissionColor, _colors[bracketIndex] * Intensity);
        }
    }
    
    private void CalculateDistances()
    {
        var distanceToEarth = Vector3.Distance(
            satellite.transform.position, earth.transform.position) / trajectoryScale;
        var distanceToMoon = Vector3.Distance(
            satellite.transform.position, moon.transform.position) / trajectoryScale;

        float distanceTravelledToSend = _currentState == SatelliteState.OffNominal ? _totalOffNominalDistance : _totalNominalDistance;

        OnDistanceCalculated?.Invoke(
            new DistanceTravelledEventArgs(distanceTravelledToSend, distanceToEarth, distanceToMoon));
    }
    
    public void DisplayModel(int displayedModelIndex)
    {
        var rocketParts = satellite.transform.GetChild(0);
        for (var modelIndex = 0; modelIndex < rocketParts.childCount; modelIndex++)
        {
            rocketParts.GetChild(modelIndex).gameObject.SetActive(modelIndex == displayedModelIndex);
        }
    }
    
    private void OnBumpOffCourse()
    {
        _currentState = SatelliteState.Manual;
        _lastAutomaticSatellitePosition = transform.position;
        _lastAutomaticSatelliteIndex = _currentPointIndex;
        
        Invoke(nameof(PushOnCourse), MaximumManualControlTime);
    }
    
    private void PushOnCourse()
    {
        _currentState = SatelliteState.Returning;
        _lastManualSatellitePosition = transform.position;
        _lastManualSatelliteIndex = _currentPointIndex;
        
        // The future path is predicted.
        var futureExpectedPositionIndex = GetClosestDataPointIndexFromTime(
            _estimatedElapsedTime + MaximumManualControlTime);
        var futureExpectedPosition = new Vector3(
            float.Parse(_nominalPathPoints[futureExpectedPositionIndex][1]),
            float.Parse(_nominalPathPoints[futureExpectedPositionIndex][2]),
            float.Parse(_nominalPathPoints[futureExpectedPositionIndex][3]));
        
        // Get the current velocity.
        var velocity = new Vector3(
            float.Parse(_nominalPathPoints[_lastManualSatelliteIndex][4]),
            float.Parse(_nominalPathPoints[_lastManualSatelliteIndex][5]),
            float.Parse(_nominalPathPoints[_lastManualSatelliteIndex][6]));
    
        var minimumTimes = new List<float>();
        
        // Reads up to 60 points into the future
        for (var futureIndex = 0; futureIndex <= MaximumFutureDataPoints; futureIndex++)
        {
            var machineLearningFutureIndex = futureExpectedPositionIndex + futureIndex;
            var machineLearningPosition = new Vector3(
                float.Parse(_nominalPathPoints[machineLearningFutureIndex][1]),
                float.Parse(_nominalPathPoints[machineLearningFutureIndex][2]),
                float.Parse(_nominalPathPoints[machineLearningFutureIndex][3]));
            
            var distance = Vector3.Distance(satellite.transform.position, machineLearningPosition);
            var minimumTime = distance / velocity.magnitude;
            
            var statusCode = 200;
            var isError = false;
            
            do
            {
                minimumTime += 30;
                // ping API
                isError = statusCode != 200;
            }
            while (isError);
            
            minimumTimes.Add(minimumTime);
        }
        
        var absoluteMinimumTime = minimumTimes.Min();
        var absoluteMinimumIndex = minimumTimes.IndexOf(absoluteMinimumTime);
        // ping API
    
        // Vector3.Lerp(_lastManualSatellitePosition, futureExpectedPosition, 0.5f);
        
        _currentState = SatelliteState.Nominal;
    }
    
    private void ManuallyControlSatellite()
    {
        const float speed = 2.125f;
        foreach (var key in _manualControlScheme.Keys.Where(Input.GetKey))
        {
            satellite.transform.position += speed * Time.deltaTime * _manualControlScheme[key];
        }
    }
    
    private int GetClosestDataPointIndexFromTime(float time)
    {
        var closestIndex = 0;
        var closestTime = float.MaxValue;
    
        for (var i = 0; i < _nominalPathPoints.Count; i++)
        {
            try
            {
                var timeDistance = Mathf.Abs(float.Parse(_nominalPathPoints[i][0]) - time);
    
                if (timeDistance >= closestTime)
                {
                    continue;
                }
    
                closestTime = timeDistance;
                closestIndex = i;
            }
            catch (FormatException)
            {
    
            }
        }
    
        Debug.Log(_nominalPathPoints[closestIndex][0]);
        return closestIndex;
    }

    private void UpdateModel(int cutsceneIndex)
    {
        switch (cutsceneIndex)
        {
            case <= 3:
                DisplayModel(cutsceneIndex + 1);
                return;

            default:
                DisplayModel(4);
                return;
        }
    }
    
    public enum SatelliteState
    {
        Nominal,
        OffNominal,
        Manual,
        Returning,
    }
}