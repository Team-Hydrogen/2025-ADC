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
    private float _totalDistanceTravelled = 0.0f;
    
    private List<string[]> _nominalTrajectoryPoints;
    private List<string[]> _offNominalTrajectoryPoints;
    private LineRenderer _currentTrajectoryRenderer;
    
    private const int SecondStageFireIndex = 5_000;
    private const int ServiceModuleFireIndex = 10_000;
    
    private Vector3 _lastAutomaticSatellitePosition;
    private Vector3 _lastManualSatellitePosition;
    private int _lastAutomaticSatelliteIndex;
    private int _lastManualSatelliteIndex;
    
    private const float MaximumManualControlTime = 5.0f;
    private const int MaximumFutureDataPoints = 60;
    
    public static event Action<int> OnCurrentIndexUpdated; 
    public static event Action<float> OnUpdateTime;
    public static event Action<Vector3> OnUpdateCoordinates;
    public static event Action<DistanceTravelledEventArgs> OnDistanceCalculated;
    public static event Action<float> OnTimeScaleSet;
    public static event Action<string> OnStageFired;

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
        _totalDistanceTravelled = 0.0f;
        _vectorRenderers = velocityVector.GetComponentsInChildren<Renderer>();
        OnTimeScaleSet?.Invoke(timeScale);
    }
    
    private void Update()
    {
        if (_isPlaying)
        {
            UpdateSatellitePosition();
            MoveSatellite();
            
            switch (_currentPointIndex)
            {
                case SecondStageFireIndex:
                    DisplayModel(1);
                    OnStageFired?.Invoke("Second Stage Fired");
                    break;
                case ServiceModuleFireIndex:
                    DisplayModel(2);
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
    }
    
    private void OnDisable()
    {
        DataManager.OnDataLoaded -= OnDataLoaded;
        DataManager.OnMissionStageUpdated -= OnMissionStageUpdated;
        UIManager.OnBumpOffCoursePressed -= OnBumpOffCourse;
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
        _offNominalTrajectoryPoints = data.OffNominalTrajectoryData;
        _currentTrajectoryRenderer = data.MissionStage.nominalLineRenderer;

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
        _currentTrajectoryRenderer.positionCount = 2;
        _currentTrajectoryRenderer.SetPosition(0, futureTrajectoryPoints[0]);
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
        int numberOfPoints = _offNominalTrajectoryPoints.Count;
        Vector3[] futureTrajectoryPoints = new Vector3[numberOfPoints];
        for (int index = 0; index < numberOfPoints; index++)
        {
            string[] point = _offNominalTrajectoryPoints[index];

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
        //currentTrajectoryRenderer.positionCount = 1;
        //currentTrajectoryRenderer.SetPosition(0, futureTrajectoryPoints[0]);
        //// The processed points are pushed to the future trajectory line.
        //futureOffnominalTrajectory.positionCount = numberOfPoints;
        //futureOffnominalTrajectory.SetPositions(futureTrajectoryPoints);
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
        
        var currentPoint = _nominalTrajectoryPoints[_currentPointIndex];
        var nextPoint = _nominalTrajectoryPoints[(_currentPointIndex + 1) % _nominalTrajectoryPoints.Count];
        
        var currentTime = float.Parse(currentPoint[0]);
        var nextTime = float.Parse(nextPoint[0]);
        var timeInterval = (nextTime - currentTime) * 60f;
        
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
        
        _progress += Time.deltaTime / timeInterval * timeScale;
        
        // Interpolate position
        var previousSatellitePosition = satellite.transform.position;
        satellite.transform.position = Vector3.Lerp(currentPosition, nextPosition, _progress);

        var netDistance = Vector3.Distance(previousSatellitePosition, satellite.transform.position);
        _totalDistanceTravelled += netDistance / trajectoryScale;
        
        // Calculate satellite direction
        var direction = (nextPosition - currentPosition).normalized;
        
        const float rotationSpeed = 2.0f;
        if (direction != Vector3.zero)
        {
            var targetRotation = Quaternion.LookRotation(direction);
            targetRotation *= Quaternion.Euler(90f, 0f, 0f);
            
            satellite.transform.rotation = Quaternion.Slerp(
                satellite.transform.rotation,
                targetRotation, 
                rotationSpeed * timeScale * Time.deltaTime
            );
        }

        _estimatedElapsedTime = currentTime + (nextTime - currentTime) * _progress;
        
        OnUpdateTime?.Invoke(_estimatedElapsedTime);
        OnUpdateCoordinates?.Invoke(satellite.transform.position / trajectoryScale);
        CalculateDistances();
        
        UpdateNominalTrajectory(false, true);
        
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
            _currentTrajectoryRenderer.SetPosition(_currentTrajectoryRenderer.positionCount-1, satellite.transform.position);
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
                var pastTrajectoryPoints = new Vector3[_currentTrajectoryRenderer.positionCount];
                _currentTrajectoryRenderer.GetPositions(pastTrajectoryPoints);

                // Combine past trajectory points and new points
                var newPastTrajectoryPoints = new Vector3[pastTrajectoryPoints.Length + pointsToMove.Length];
                Array.Copy(pastTrajectoryPoints, 0, newPastTrajectoryPoints, 0, pastTrajectoryPoints.Length);
                Array.Copy(pointsToMove, 0, newPastTrajectoryPoints, pastTrajectoryPoints.Length, pointsToMove.Length);

                // Update past trajectory
                _currentTrajectoryRenderer.positionCount = newPastTrajectoryPoints.Length;
                _currentTrajectoryRenderer.SetPositions(newPastTrajectoryPoints);

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
                var pastTrajectoryPoints = new Vector3[_currentTrajectoryRenderer.positionCount];
                _currentTrajectoryRenderer.GetPositions(pastTrajectoryPoints);

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
                var newPastPointCount = _currentTrajectoryRenderer.positionCount - indexChange;
                var newPastTrajectoryPoints = new Vector3[newPastPointCount];
                Array.Copy(pastTrajectoryPoints, 0, newPastTrajectoryPoints, 0, newPastPointCount);

                _currentTrajectoryRenderer.positionCount = newPastPointCount;
                _currentTrajectoryRenderer.SetPositions(newPastTrajectoryPoints);
                break;
            }
        }
    }
    
    /// <summary>
    /// Updates the trajectory of the Orion capsule
    /// </summary>
    private void UpdateOffnominalTrajectory()
    {
        // // The current future trajectory is loaded.
        // Vector3[] futureTrajectoryPoints = new Vector3[futureOffnominalTrajectory.positionCount];
        // futureOffnominalTrajectory.GetPositions(futureTrajectoryPoints);
        // // The past trajectory's list of positions expands, so the next future data point is added.
        // Vector3 nextTrajectoryPoint = futureTrajectoryPoints[1];
        // pastOffnominalTrajectory.positionCount++;
        // pastOffnominalTrajectory.SetPosition(pastOffnominalTrajectory.positionCount - 1, nextTrajectoryPoint);
        // // The next point in the future trajectory gets removed.
        // futureTrajectoryPoints = futureTrajectoryPoints[1..^1];
        // futureOffnominalTrajectory.positionCount--;
        // futureOffnominalTrajectory.SetPositions(futureTrajectoryPoints);
    }
    
    private void OnMissionStageUpdated(MissionStage stage)
    {
        if (_currentTrajectoryRenderer.Equals(stage.nominalLineRenderer))
        {
            return;
        }
        
        _currentTrajectoryRenderer = stage.nominalLineRenderer;
        _currentTrajectoryRenderer.SetPosition(0, satellite.transform.position);
        
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
        OnDistanceCalculated?.Invoke(
            new DistanceTravelledEventArgs(_totalDistanceTravelled, distanceToEarth, distanceToMoon));
    }

    private void DisplayModel(int displayedModelIndex)
    {
        var rocketParts = transform.GetChild(0);
        for (var modelIndex = 0; modelIndex < rocketParts.childCount; modelIndex++)
        {
            rocketParts.GetChild(displayedModelIndex).gameObject.SetActive(modelIndex == displayedModelIndex);
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
    
    private void MoveSatellite()
    {
        if (_currentState != SatelliteState.Manual)
        {
            return;
        }
        
        // Control the satellite on the left-right axis.
        if (Input.GetKey(KeyCode.A))
        {
            satellite.transform.position += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            satellite.transform.position += Vector3.right;
        }
        
        // Control the satellite on the forward-backward axis.
        if (Input.GetKey(KeyCode.W))
        {
            satellite.transform.position += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            satellite.transform.position += Vector3.back;
        }
        
        // Control the satellite on the up-down axis.
        if (Input.GetKey(KeyCode.Q))
        {
            satellite.transform.position += Vector3.down;
        }
        if (Input.GetKey(KeyCode.E))
        {
            satellite.transform.position += Vector3.up;
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
    
    private enum SatelliteState
    {
        Nominal,
        OffNominal,
        Manual,
        Returning,
    }
}