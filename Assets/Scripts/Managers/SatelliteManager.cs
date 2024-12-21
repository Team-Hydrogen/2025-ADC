using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;

public class SatelliteManager : MonoBehaviour
{
    public static SatelliteManager instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private float trajectoryScale;
    
    [Header("Satellite")]
    [SerializeField] private GameObject satellite;
    [SerializeField] private GameObject velocityVector;
    [SerializeField] private Transform nominalSatellitePosition;
    [SerializeField] private Transform offnominalSatellitePosition;
    
    [Header("Celestial Bodies")]
    [SerializeField] private GameObject earth;
    [SerializeField] private GameObject moon;
    
    [Header("Nominal Trajectory")]
    // [SerializeField] private LineRenderer pastNominalTrajectory;
    [SerializeField] private LineRenderer futureNominalTrajectory;
    
    [Header("Off Nominal Trajectory")]
    // [SerializeField] private LineRenderer pastOffnominalTrajectory;
    [SerializeField] private LineRenderer futureOffnominalTrajectory;
    
    [Header("Time Scale")]
    [SerializeField] private float timeScale;
    
    private SatelliteState _currentState = SatelliteState.Nominal;
    
    private int _previousPointIndex = 0;
    private int _currentPointIndex = 0;
    private bool _isPlaying = false;
    private const int SkipIndexChange = 100;
    
    private float _progress = 0.0f;
    private float _estimatedElapsedTime;
    private float _totalDistanceTravelledNominal = 0.0f;
    private float _totalDistanceTravelledOffnominal = 0.0f;
    
    private List<string[]> _nominalTrajectoryPoints;
    private List<string[]> _offnominalTrajectoryPoints;
    private LineRenderer _currentNominalTrajectoryRenderer;
    private LineRenderer _currentOffnominalTrajectoryRenderer;
    
    private const int SecondStageFireIndex = 5_000;
    private const int ServiceModuleFireIndex = 10_000;
    
    private Vector3 _lastAutomaticSatellitePosition;
    private Vector3 _lastManualSatellitePosition;
    private int _lastAutomaticSatelliteIndex;
    private int _lastManualSatelliteIndex;
    private float _timeInterval;
    
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
        _totalDistanceTravelledNominal = 0.0f;
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
            
            switch (_currentPointIndex)
            {
                case SecondStageFireIndex:
                    OnStageFired?.Invoke("Second Stage Fired");
                    break;
                case ServiceModuleFireIndex:
                    OnStageFired?.Invoke("Service Module Fired");
                    break;
            }
        }
    }
    
    private void OnEnable()
    {
        DataManager.OnDataLoaded += OnDataLoaded;
        DataManager.OnMissionStageUpdated += OnMissionStageUpdated;
        UIManager.OnBumpOffCoursePressed += OnBumpOffCourse;
        UIManager.OnCurrentPathChanged += OnChangedCurrentPath;
    }
    
    private void OnDisable()
    {
        DataManager.OnDataLoaded -= OnDataLoaded;
        DataManager.OnMissionStageUpdated -= OnMissionStageUpdated;
        UIManager.OnBumpOffCoursePressed -= OnBumpOffCourse;
        UIManager.OnCurrentPathChanged -= OnChangedCurrentPath;
    }
    
    #endregion
    
    #region Time Scale

    public void ForwardButtonPressed()
    {
        _currentPointIndex = GetClosestDataPointIndexFromTime(_estimatedElapsedTime + SkipIndexChange / timeScale);
    }

    public void BackwardButtonPressed()
    {
        _currentPointIndex = GetClosestDataPointIndexFromTime(_estimatedElapsedTime - SkipIndexChange / timeScale);
    }
    
    public void FastForwardButtonPressed()
    {
        timeScale *= 10;
        OnTimeScaleSet?.Invoke(timeScale);
    }

    public void RewindButtonPressed()
    {
        timeScale = Mathf.Max(1, timeScale / 10);
        OnTimeScaleSet?.Invoke(timeScale);
    }
    
    #endregion

    private void OnDataLoaded(DataLoadedEventArgs data)
    {
        _nominalTrajectoryPoints = data.NominalTrajectoryData;
        _offnominalTrajectoryPoints = data.OffNominalTrajectoryData;
        _currentNominalTrajectoryRenderer = data.MissionStage.nominalLineRenderer;
        _currentOffnominalTrajectoryRenderer = data.MissionStage.offnominalLineRenderer;

        PlotNominalTrajectory();
        PlotOffnominalTrajectory();

        _isPlaying = true;
        
        OnCurrentIndexUpdated?.Invoke(_currentPointIndex);
    }

    #region Plot Trajectories
    
    /// <summary>
    /// Plots the provided data points into a visual trajectory. PlotTrajectory() is meant to be run only once.
    /// </summary>
    private void PlotNominalTrajectory()
    {
        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = _nominalTrajectoryPoints.Count;
        Vector3[] futureTrajectoryPoints = new Vector3[numberOfPoints];

        for (int index = 0; index < numberOfPoints; index++)
        {
            string[] point = _nominalTrajectoryPoints[index];

            try
            {
                Vector3 pointAsVector = new Vector3(
                    float.Parse(point[1]) * trajectoryScale,
                    float.Parse(point[2]) * trajectoryScale,
                    float.Parse(point[3]) * trajectoryScale);
                futureTrajectoryPoints[index] = pointAsVector;
            }
            catch
            {
                Debug.LogWarning("No positional data on line " + index + "!");
            }
        }

        // The first point of the pastTrajectory is added.
        _currentNominalTrajectoryRenderer.positionCount = 2;
        _currentNominalTrajectoryRenderer.SetPosition(0, futureTrajectoryPoints[0]);
        // The processed points are pushed to the future trajectory line.
        futureNominalTrajectory.positionCount = numberOfPoints;
        futureNominalTrajectory.SetPositions(futureTrajectoryPoints);
    }
    
    /// <summary>
    /// Plots the provided data points into a visual trajectory. PlotTrajectory() is meant to be run only once.
    /// </summary>
    private void PlotOffnominalTrajectory()
    {
        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = _offnominalTrajectoryPoints.Count;
        Vector3[] futureTrajectoryPoints = new Vector3[numberOfPoints];
        for (int index = 0; index < numberOfPoints; index++)
        {
            string[] point = _offnominalTrajectoryPoints[index];

            try
            {
                Vector3 pointAsVector = new Vector3(
                    float.Parse(point[1]) * trajectoryScale,
                    float.Parse(point[2]) * trajectoryScale,
                    float.Parse(point[3]) * trajectoryScale);
                futureTrajectoryPoints[index] = pointAsVector;
            }
            catch
            {
                Debug.LogWarning("No positional data on line " + index + "!");
            }
        }
        //// The first point of the pastTrajectory is added.
        _currentOffnominalTrajectoryRenderer.positionCount = 1;
        _currentOffnominalTrajectoryRenderer.SetPosition(0, futureTrajectoryPoints[0]);
        //// The processed points are pushed to the future trajectory line.
        futureOffnominalTrajectory.positionCount = numberOfPoints;
        futureOffnominalTrajectory.SetPositions(futureTrajectoryPoints);
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
        UpdateNominalSatellitePosition();
        UpdateOffnominalSatellitePosition();
        SetSatelliteVisualToPosition();
        UpdateAfter();
    }

    private void SetSatelliteVisualToPosition()
    {
        if (_currentState == SatelliteState.Nominal)
        {
            satellite.transform.position = nominalSatellitePosition.position;
            satellite.transform.rotation = nominalSatellitePosition.rotation;
        } 
        else if ( _currentState == SatelliteState.OffNominal)
        {
            satellite.transform.position = offnominalSatellitePosition.position;
            satellite.transform.rotation = offnominalSatellitePosition.rotation;
        }
    }

    private void UpdateNominalSatellitePosition() {
        string[] currentPoint = _nominalTrajectoryPoints[_currentPointIndex];
        string[] nextPoint = _nominalTrajectoryPoints[(_currentPointIndex + 1) % _nominalTrajectoryPoints.Count];

        Vector3 currentPosition = new Vector3(
            float.Parse(currentPoint[1]) * trajectoryScale,
            float.Parse(currentPoint[2]) * trajectoryScale,
            float.Parse(currentPoint[3]) * trajectoryScale
        );
        Vector3 nextPosition = new Vector3(
            float.Parse(nextPoint[1]) * trajectoryScale,
            float.Parse(nextPoint[2]) * trajectoryScale,
            float.Parse(nextPoint[3]) * trajectoryScale
        );

        // Interpolate position
        Vector3 previousSatellitePosition = nominalSatellitePosition.position;
        nominalSatellitePosition.position = Vector3.Lerp(currentPosition, nextPosition, _progress);

        float netDistance = Vector3.Distance(previousSatellitePosition, nominalSatellitePosition.position);
        _totalDistanceTravelledNominal += netDistance / trajectoryScale;

        // Calculate satellite direction
        Vector3 direction = (nextPosition - currentPosition).normalized;
        
        const float rotationSpeed = 2.0f;
        if (direction != Vector3.zero)
        {
            var targetRotation = Quaternion.LookRotation(direction);
            targetRotation *= Quaternion.Euler(90f, 0f, 0f);
            
            nominalSatellitePosition.rotation = Quaternion.Slerp(
                nominalSatellitePosition.rotation,
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void UpdateOffnominalSatellitePosition()
    {
        string[] currentPoint = _offnominalTrajectoryPoints[_currentPointIndex];
        string[] nextPoint = _offnominalTrajectoryPoints[(_currentPointIndex + 1) % _offnominalTrajectoryPoints.Count];

        Vector3 currentPosition = new Vector3(
            float.Parse(currentPoint[1]) * trajectoryScale,
            float.Parse(currentPoint[2]) * trajectoryScale,
            float.Parse(currentPoint[3]) * trajectoryScale
        );
        Vector3 nextPosition = new Vector3(
            float.Parse(nextPoint[1]) * trajectoryScale,
            float.Parse(nextPoint[2]) * trajectoryScale,
            float.Parse(nextPoint[3]) * trajectoryScale
        );

        // Interpolate position
        Vector3 previousSatellitePosition = offnominalSatellitePosition.position;
        offnominalSatellitePosition.position = Vector3.Lerp(currentPosition, nextPosition, _progress);

        float netDistance = Vector3.Distance(previousSatellitePosition, offnominalSatellitePosition.position);
        _totalDistanceTravelledOffnominal += netDistance / trajectoryScale;

        // Calculate satellite direction
        Vector3 direction = (nextPosition - currentPosition).normalized;

        const float rotationSpeed = 2.0f;
        if (direction != Vector3.zero)
        {
            var targetRotation = Quaternion.LookRotation(direction);
            targetRotation *= Quaternion.Euler(90f, 0f, 0f);

            offnominalSatellitePosition.rotation = Quaternion.Slerp(
                offnominalSatellitePosition.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void UpdateTimeIntervalAndProgress()
    {
        string[] currentPoint = _offnominalTrajectoryPoints[_currentPointIndex];
        string[] nextPoint = _offnominalTrajectoryPoints[(_currentPointIndex + 1) % _offnominalTrajectoryPoints.Count];

        float currentTime = float.Parse(currentPoint[0]);
        float nextTime = float.Parse(nextPoint[0]);
        _timeInterval = (nextTime - currentTime) * 60f;

        _progress += Time.deltaTime / _timeInterval * timeScale;

        _estimatedElapsedTime = currentTime + (nextTime - currentTime) * _progress;

        OnUpdateTime?.Invoke(_estimatedElapsedTime);
    }

    private void UpdateAfter()
    {
        OnUpdateCoordinates?.Invoke(satellite.transform.position / trajectoryScale);
        CalculateDistances();

        UpdateNominalTrajectory(false, true);
        UpdateOffnominalTrajectory(false, true);

        // Move to the next point when progress is complete
        if (_progress < 1.0f)
        {
            return;
        }

        _previousPointIndex = _currentPointIndex;
        // The simulation is reset.
        _currentPointIndex = (_currentPointIndex + Mathf.FloorToInt(_progress)) % _nominalTrajectoryPoints.Count;
        // The progress is reset.
        _progress %= 1;

        OnCurrentIndexUpdated?.Invoke(_currentPointIndex);
        UpdateNominalTrajectory(true, false);
        UpdateOffnominalTrajectory(true, false);
        UpdateVelocityVector(_currentPointIndex);
    }

    /// <summary>
    /// Updates the trajectory of the Orion capsule
    /// </summary>
    private void UpdateNominalTrajectory(bool indexUpdated, bool positionUpdated)
    {
        if (positionUpdated)
        {
            futureNominalTrajectory.SetPosition(0, satellite.transform.position);
            _currentNominalTrajectoryRenderer.SetPosition(_currentNominalTrajectoryRenderer.positionCount-1, satellite.transform.position);
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
                var futureTrajectoryPoints = new Vector3[futureNominalTrajectory.positionCount];
                futureNominalTrajectory.GetPositions(futureTrajectoryPoints);

                var pointsToMove = new Vector3[indexChange];
                Array.Copy(futureTrajectoryPoints, 0, pointsToMove, 0, indexChange);

                // Add these points to the past trajectory
                var pastTrajectoryPoints = new Vector3[_currentNominalTrajectoryRenderer.positionCount];
                _currentNominalTrajectoryRenderer.GetPositions(pastTrajectoryPoints);

                // Combine past trajectory points and new points
                var newPastTrajectoryPoints = new Vector3[pastTrajectoryPoints.Length + pointsToMove.Length];
                Array.Copy(pastTrajectoryPoints, 0, newPastTrajectoryPoints, 0, pastTrajectoryPoints.Length);
                Array.Copy(pointsToMove, 0, newPastTrajectoryPoints, pastTrajectoryPoints.Length, pointsToMove.Length);

                // Update past trajectory
                _currentNominalTrajectoryRenderer.positionCount = newPastTrajectoryPoints.Length;
                _currentNominalTrajectoryRenderer.SetPositions(newPastTrajectoryPoints);

                // Remove moved points from future trajectory
                var newFuturePointCount = futureNominalTrajectory.positionCount - indexChange;
                var newFutureTrajectoryPoints = new Vector3[newFuturePointCount];
                Array.Copy(futureTrajectoryPoints, indexChange, newFutureTrajectoryPoints, 0, newFuturePointCount);

                futureNominalTrajectory.positionCount = newFuturePointCount;
                futureNominalTrajectory.SetPositions(newFutureTrajectoryPoints);
                break;
            }
            case < 0:
            {
                indexChange = -indexChange;

                // Get all points in the past trajectory
                var pastTrajectoryPoints = new Vector3[_currentNominalTrajectoryRenderer.positionCount];
                _currentNominalTrajectoryRenderer.GetPositions(pastTrajectoryPoints);

                // Extract points to move back to the future trajectory
                var pointsToMove = new Vector3[indexChange];
                Array.Copy(pastTrajectoryPoints, pastTrajectoryPoints.Length - indexChange, pointsToMove, 0, indexChange);

                // Add these points back to the future trajectory
                var futureTrajectoryPoints = new Vector3[futureNominalTrajectory.positionCount];
                futureNominalTrajectory.GetPositions(futureTrajectoryPoints);

                var newFutureTrajectoryPoints = new Vector3[futureTrajectoryPoints.Length + pointsToMove.Length];
                Array.Copy(pointsToMove, 0, newFutureTrajectoryPoints, 0, pointsToMove.Length);
                Array.Copy(futureTrajectoryPoints, 0, newFutureTrajectoryPoints, pointsToMove.Length, futureTrajectoryPoints.Length);

                // Update future trajectory
                futureNominalTrajectory.positionCount = newFutureTrajectoryPoints.Length;
                futureNominalTrajectory.SetPositions(newFutureTrajectoryPoints);

                // Remove moved points from past trajectory
                var newPastPointCount = _currentNominalTrajectoryRenderer.positionCount - indexChange;
                var newPastTrajectoryPoints = new Vector3[newPastPointCount];
                Array.Copy(pastTrajectoryPoints, 0, newPastTrajectoryPoints, 0, newPastPointCount);

                _currentNominalTrajectoryRenderer.positionCount = newPastPointCount;
                _currentNominalTrajectoryRenderer.SetPositions(newPastTrajectoryPoints);
                break;
            }
        }
    }

    private void UpdateOffnominalTrajectory(bool indexUpdated, bool positionUpdated)
    {
        if (positionUpdated)
        {
            futureNominalTrajectory.SetPosition(0, satellite.transform.position);
            _currentNominalTrajectoryRenderer.SetPosition(_currentNominalTrajectoryRenderer.positionCount - 1, satellite.transform.position);
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
                    var futureTrajectoryPoints = new Vector3[futureOffnominalTrajectory.positionCount];
                    futureOffnominalTrajectory.GetPositions(futureTrajectoryPoints);

                    var pointsToMove = new Vector3[indexChange];
                    Array.Copy(futureTrajectoryPoints, 0, pointsToMove, 0, indexChange);

                    // Add these points to the past trajectory
                    var pastTrajectoryPoints = new Vector3[_currentOffnominalTrajectoryRenderer.positionCount];
                    _currentOffnominalTrajectoryRenderer.GetPositions(pastTrajectoryPoints);

                    // Combine past trajectory points and new points
                    var newPastTrajectoryPoints = new Vector3[pastTrajectoryPoints.Length + pointsToMove.Length];
                    Array.Copy(pastTrajectoryPoints, 0, newPastTrajectoryPoints, 0, pastTrajectoryPoints.Length);
                    Array.Copy(pointsToMove, 0, newPastTrajectoryPoints, pastTrajectoryPoints.Length, pointsToMove.Length);

                    // Update past trajectory
                    _currentOffnominalTrajectoryRenderer.positionCount = newPastTrajectoryPoints.Length;
                    _currentOffnominalTrajectoryRenderer.SetPositions(newPastTrajectoryPoints);

                    // Remove moved points from future trajectory
                    var newFuturePointCount = futureOffnominalTrajectory.positionCount - indexChange;
                    var newFutureTrajectoryPoints = new Vector3[newFuturePointCount];
                    Array.Copy(futureTrajectoryPoints, indexChange, newFutureTrajectoryPoints, 0, newFuturePointCount);

                    futureOffnominalTrajectory.positionCount = newFuturePointCount;
                    futureOffnominalTrajectory.SetPositions(newFutureTrajectoryPoints);
                    break;
                }
            case < 0:
                {
                    indexChange = -indexChange;

                    // Get all points in the past trajectory
                    var pastTrajectoryPoints = new Vector3[_currentOffnominalTrajectoryRenderer.positionCount];
                    _currentOffnominalTrajectoryRenderer.GetPositions(pastTrajectoryPoints);

                    // Extract points to move back to the future trajectory
                    var pointsToMove = new Vector3[indexChange];
                    Array.Copy(pastTrajectoryPoints, pastTrajectoryPoints.Length - indexChange, pointsToMove, 0, indexChange);

                    // Add these points back to the future trajectory
                    var futureTrajectoryPoints = new Vector3[futureOffnominalTrajectory.positionCount];
                    futureOffnominalTrajectory.GetPositions(futureTrajectoryPoints);

                    var newFutureTrajectoryPoints = new Vector3[futureTrajectoryPoints.Length + pointsToMove.Length];
                    Array.Copy(pointsToMove, 0, newFutureTrajectoryPoints, 0, pointsToMove.Length);
                    Array.Copy(futureTrajectoryPoints, 0, newFutureTrajectoryPoints, pointsToMove.Length, futureTrajectoryPoints.Length);

                    // Update future trajectory
                    futureOffnominalTrajectory.positionCount = newFutureTrajectoryPoints.Length;
                    futureOffnominalTrajectory.SetPositions(newFutureTrajectoryPoints);

                    // Remove moved points from past trajectory
                    var newPastPointCount = _currentOffnominalTrajectoryRenderer.positionCount - indexChange;
                    var newPastTrajectoryPoints = new Vector3[newPastPointCount];
                    Array.Copy(pastTrajectoryPoints, 0, newPastTrajectoryPoints, 0, newPastPointCount);

                    _currentOffnominalTrajectoryRenderer.positionCount = newPastPointCount;
                    _currentOffnominalTrajectoryRenderer.SetPositions(newPastTrajectoryPoints);
                    break;
                }
        }
    }

    private void OnChangedCurrentPath(SatelliteState state)
    {
        if (_currentState != SatelliteState.Returning || _currentState != SatelliteState.Manual)
        {
            _currentState = state;
            OnSatelliteStateUpdated?.Invoke(_currentState);
        }
    }

    private void OnMissionStageUpdated(MissionStage stage)
    {
        if (_currentNominalTrajectoryRenderer.Equals(stage.nominalLineRenderer))
        {
            return;
        }
        
        _currentNominalTrajectoryRenderer = stage.nominalLineRenderer;
        _currentNominalTrajectoryRenderer.SetPosition(0, nominalSatellitePosition.position);

        _currentOffnominalTrajectoryRenderer = stage.offnominalLineRenderer;
        _currentOffnominalTrajectoryRenderer.SetPosition(0, offnominalSatellitePosition.position);
        
        // trigger animation here if it is correct stage
    }
    
    private void UpdateVelocityVector(int currentIndex)
    {
        // A Vector3 variable is created to store and compute information about the current velocity vector.
        var vector = new Vector3(
            float.Parse(_nominalTrajectoryPoints[currentIndex][4]),
            float.Parse(_nominalTrajectoryPoints[currentIndex][5]),
            float.Parse(_nominalTrajectoryPoints[currentIndex][6]));
        
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
            meshRenderer.material.SetColor("_BaseColor", _colors[bracketIndex]);
        }
    }
        
    private void CalculateDistances()
    {
        var distanceToEarth = Vector3.Distance(
            satellite.transform.position, earth.transform.position) / trajectoryScale;
        var distanceToMoon = Vector3.Distance(
            satellite.transform.position, moon.transform.position) / trajectoryScale;

        float distanceTravelledToSend = _currentState == SatelliteState.OffNominal ? _totalDistanceTravelledOffnominal : _totalDistanceTravelledNominal;

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
            float.Parse(_nominalTrajectoryPoints[futureExpectedPositionIndex][1]),
            float.Parse(_nominalTrajectoryPoints[futureExpectedPositionIndex][2]),
            float.Parse(_nominalTrajectoryPoints[futureExpectedPositionIndex][3]));
        
        // Get the current velocity.
        var velocity = new Vector3(
            float.Parse(_nominalTrajectoryPoints[_lastManualSatelliteIndex][4]),
            float.Parse(_nominalTrajectoryPoints[_lastManualSatelliteIndex][5]),
            float.Parse(_nominalTrajectoryPoints[_lastManualSatelliteIndex][6]));

        var minimumTimes = new List<float>();
        
        // Reads up to 60 points into the future
        for (var futureIndex = 0; futureIndex <= MaximumFutureDataPoints; futureIndex++)
        {
            var machineLearningFutureIndex = futureExpectedPositionIndex + futureIndex;
            var machineLearningPosition = new Vector3(
                float.Parse(_nominalTrajectoryPoints[machineLearningFutureIndex][1]),
                float.Parse(_nominalTrajectoryPoints[machineLearningFutureIndex][2]),
                float.Parse(_nominalTrajectoryPoints[machineLearningFutureIndex][3]));
            
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
        
        // Control the satellite on the left-right axis.
        if (Input.GetKey(KeyCode.A))
        {
            satellite.transform.position += speed * Time.deltaTime * Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            satellite.transform.position += speed * Time.deltaTime * Vector3.right;
        }
        
        // Control the satellite on the forward-backward axis.
        if (Input.GetKey(KeyCode.W))
        {
            satellite.transform.position += speed * Time.deltaTime * Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            satellite.transform.position += speed * Time.deltaTime * Vector3.back;
        }
        
        // Control the satellite on the up-down axis.
        if (Input.GetKey(KeyCode.Q))
        {
            satellite.transform.position += speed * Time.deltaTime * Vector3.down;
        }
        if (Input.GetKey(KeyCode.E))
        {
            satellite.transform.position += speed * Time.deltaTime * Vector3.up;
        }
    }
    
    private int GetClosestDataPointIndexFromTime(float time)
    {
        var closestIndex = 0;
        var closestTime = float.MaxValue;

        for (var i = 0; i < _nominalTrajectoryPoints.Count; i++)
        {
            try
            {
                var timeDistance = Mathf.Abs(float.Parse(_nominalTrajectoryPoints[i][0]) - time);

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

        Debug.Log(_nominalTrajectoryPoints[closestIndex][0]);
        return closestIndex;
    }
    
    public enum SatelliteState
    {
        Nominal,
        OffNominal,
        Manual,
        Returning,
    }
}