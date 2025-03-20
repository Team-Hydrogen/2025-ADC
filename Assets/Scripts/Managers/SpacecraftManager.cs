using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpacecraftManager : MonoBehaviour
{
    public static SpacecraftManager Instance { get; private set; }
    
    // Available in Inspector
    [Header("Settings")]
    [SerializeField] private float trajectoryScale;
    
    [Header("Spacecraft")]
    [SerializeField] private Transform spacecraft;
    [SerializeField] private Transform velocityVector;
    
    // NOTE TO OTHER DEVELOPERS
    // Please keep the `field:` prefix. It is needed to show these specific variables in the inspector.
    [field: Header("Positions")]
    [field: SerializeField] public Transform NominalSpacecraftTransform { get; private set; }
    [field: SerializeField] public Transform OffNominalSpacecraftTransform { get; private set; }
    [field: SerializeField] public Transform MergeSpacecraftTransform { get; private set; }
    
    [Header("Current Trajectories")]
    [SerializeField] private LineRenderer currentMergeTrajectoryRenderer;
    
    [Header("Future Trajectories")]
    [SerializeField] private LineRenderer futureNominalTrajectory;
    [SerializeField] private LineRenderer futureOffNominalTrajectory;
    [SerializeField] private LineRenderer futureMergeTrajectory;
    
    [Header("Celestial Bodies")]
    [SerializeField] private GameObject earth;
    [SerializeField] private GameObject moon;
    
    [Header("Time Scale")]
    [SerializeField] private float timeScale;
    
    // Private variables
    private SpacecraftState _currentState = SpacecraftState.Nominal;

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
    private List<string[]> _mergePathPoints;
    
    private LineRenderer _currentNominalTrajectoryRenderer;
    private LineRenderer _currentOffNominalTrajectoryRenderer;
    
    // The second stage is the same as the service module.
    private const int SecondStageFireIndex = 120;
    
    private Vector3 _lastAutomaticSpacecraftPosition;
    private Vector3 _lastManualSpacecraftPosition;
    private int _lastAutomaticSpacecraftIndex;
    private int _lastManualSpacecraftIndex;
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
    public static event Action<SpacecraftState> OnSpacecraftStateUpdated;
    
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
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
            UpdateSpacecraftPosition();
            
            if (_currentState == SpacecraftState.Manual)
            {
                ManuallyControlSpacecraft();
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
        HttpManager.OnPathCalculated += OnPathCalculated;
        UIManager.OnCurrentPathChanged += OnChangedCurrentPath;
    }
    
    private void OnDisable()
    {
        CutsceneManager.OnCutsceneStart -= UpdateModel;
        DataManager.OnDataLoaded -= OnDataLoaded;
        DataManager.OnMissionStageUpdated -= OnMissionStageUpdated;
        HttpManager.OnPathCalculated -= OnPathCalculated;
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
        UpdateVelocityVector(_currentPointIndex);
        
        _isPlaying = true;
        
        OnCurrentIndexUpdated?.Invoke(_currentPointIndex);
    }

    #region Plot Trajectories
    
    /// <summary>
    /// Converts trajectory data into a visualization
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
    
    /// <summary>
    /// Updates a trajectory
    /// </summary>
    /// <param name="spacecraftTransform"></param>
    /// <param name="current"></param>
    /// <param name="future"></param>
    /// <param name="indexUpdated"></param>
    /// <param name="positionUpdated"></param>
    private void UpdateTrajectory(Transform spacecraftTransform, LineRenderer current, LineRenderer future, bool indexUpdated, bool positionUpdated)
    {
        if (positionUpdated)
        {
            future.SetPosition(0, spacecraftTransform.position);
            current.SetPosition(current.positionCount - 1, spacecraftTransform.position);
        }

        if (!indexUpdated)
        {
            return;
        }

        var indexChange = _currentState == SpacecraftState.Merging
            ? 1
            : _currentPointIndex - _previousPointIndex;
        
        switch (indexChange)
        {
            case > 0: // The spacecraft moves forward.
            {
                // Get all points in the future trajectory
                var futureTrajectoryPoints = new Vector3[future.positionCount];
                future.GetPositions(futureTrajectoryPoints);
                
                // Extract points to push to the past trajectory
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
            case < 0: // The spacecraft moves backwards.
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
    
    #endregion

    /// <summary>
    /// Updates the position of the Orion capsule
    /// </summary>
    private void UpdateSpacecraftPosition()
    {
        if (_currentState is SpacecraftState.Manual or SpacecraftState.Returning)
        {
            return;
        }
        
        UpdateTimeIntervalAndProgress();
        
        _totalNominalDistance += UpdateSpacecraftPositionOnPath(_nominalPathPoints, NominalSpacecraftTransform);
        _totalOffNominalDistance += UpdateSpacecraftPositionOnPath(_offNominalPathPoints, OffNominalSpacecraftTransform);
        if (_currentState == SpacecraftState.Merging)
        {
            UpdateSpacecraftPositionOnPathFromTime(_estimatedElapsedTime, _mergePathPoints, MergeSpacecraftTransform);
        }
        
        UpdateAfter();
        SetSpacecraftVisualToPosition();
    }

    private void SetSpacecraftVisualToPosition()
    {
        switch (_currentState)
        {
            case SpacecraftState.Nominal:
                spacecraft.position = NominalSpacecraftTransform.position;
                spacecraft.rotation = NominalSpacecraftTransform.rotation;
                break;
            case SpacecraftState.OffNominal:
                spacecraft.position = OffNominalSpacecraftTransform.position;
                spacecraft.rotation = OffNominalSpacecraftTransform.rotation;
                break;
            case SpacecraftState.Merging:
                spacecraft.position = MergeSpacecraftTransform.position;
                spacecraft.rotation = MergeSpacecraftTransform.rotation;
                break;
            case SpacecraftState.Manual:
            case SpacecraftState.Returning:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private float UpdateSpacecraftPositionOnPath(List<string[]> points, Transform spacecraftPosition)
    {
        var currentPoint = points[_currentPointIndex];
        Vector3 currentVelocityVector;
        try
        {
            currentVelocityVector = new Vector3(
                float.Parse(currentPoint[4]),
                float.Parse(currentPoint[5]),
                float.Parse(currentPoint[6])
            );
        }
        catch (FormatException)
        {
            currentVelocityVector = Vector3.zero;
        }
        
        var nextPoint = points[(_currentPointIndex + 1) % points.Count];
        Vector3 nextVelocityVector;
        try
        {
            nextVelocityVector = new Vector3(
                float.Parse(nextPoint[4]),
                float.Parse(nextPoint[5]),
                float.Parse(nextPoint[6])
            );
        }
        catch (FormatException)
        {
            nextVelocityVector = Vector3.zero;
        }

        Vector3 currentPosition;
        try
        {
            currentPosition = new Vector3(
                float.Parse(currentPoint[1]) * trajectoryScale,
                float.Parse(currentPoint[2]) * trajectoryScale,
                float.Parse(currentPoint[3]) * trajectoryScale
            );
        }
        catch (FormatException)
        {
            currentPosition = Vector3.zero;
        }
        
        Vector3 nextPosition = Vector3.zero;
        try
        {
            nextPosition = new Vector3(
                float.Parse(nextPoint[1]) * trajectoryScale,
                float.Parse(nextPoint[2]) * trajectoryScale,
                float.Parse(nextPoint[3]) * trajectoryScale
            );
        }
        catch (FormatException)
        {
            currentPosition = Vector3.zero;
        }

        // Interpolate position
        var previousPosition = spacecraftPosition.position;
        spacecraftPosition.position = Vector3.Lerp(currentPosition, nextPosition, _progress);

        var netDistance = Vector3.Distance(previousPosition, spacecraftPosition.position) / trajectoryScale;

        // Calculate spacecraft direction
        var direction = (nextPosition - currentPosition).normalized;
        if (direction == Vector3.zero)
        {
            return netDistance;
        }
        
        // Interpolate rotation
        spacecraftPosition.rotation = Quaternion.Slerp(
            Quaternion.LookRotation(currentVelocityVector) * Quaternion.Euler(90.0f, 0.0f, 0.0f),
            Quaternion.LookRotation(nextVelocityVector) * Quaternion.Euler(90.0f, 0.0f, 0.0f), 
            _progress
        );

        return netDistance;
    }
    
    private float UpdateSpacecraftPositionOnPathFromTime(float elapsedTime, List<string[]> points, Transform spacecraftPosition)
    {
        int[] indexBounds = GetIndexBoundsFromTime(elapsedTime, points);
        int lowerIndex = indexBounds[0];
        int upperIndex = indexBounds[1];
        
        var currentPoint = points[lowerIndex];
        var currentVelocityVector = new Vector3(
            float.Parse(currentPoint[4]),
            float.Parse(currentPoint[5]),
            float.Parse(currentPoint[6]));
        
        var nextPoint = points[upperIndex % points.Count];
        var nextVelocityVector = new Vector3(
            float.Parse(nextPoint[4]),
            float.Parse(nextPoint[5]),
            float.Parse(nextPoint[6]));
        
        
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
        var previousPosition = spacecraftPosition.position;
        spacecraftPosition.position = Vector3.Lerp(currentPosition, nextPosition, _progress);

        var netDistance = Vector3.Distance(previousPosition, spacecraftPosition.position) / trajectoryScale;

        // Calculate spacecraft direction
        var direction = (nextPosition - currentPosition).normalized;
        if (direction == Vector3.zero)
        {
            return netDistance;
        }
        
        // Interpolate rotation
        spacecraftPosition.rotation = Quaternion.Slerp(
            Quaternion.LookRotation(currentVelocityVector) * Quaternion.Euler(90.0f, 0.0f, 0.0f),
            Quaternion.LookRotation(nextVelocityVector) * Quaternion.Euler(90.0f, 0.0f, 0.0f), 
            _progress
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
        OnUpdateCoordinates?.Invoke(spacecraft.position / trajectoryScale);
        CalculateDistances();
        
        UpdateTrajectory(
            NominalSpacecraftTransform,
            _currentNominalTrajectoryRenderer,
            futureNominalTrajectory,
            false,
            true
        );
        UpdateTrajectory(
            OffNominalSpacecraftTransform,
            _currentOffNominalTrajectoryRenderer,
            futureOffNominalTrajectory,
            false,
            true
        );
        if (_currentState == SpacecraftState.Merging)
        {
            UpdateTrajectory(
                MergeSpacecraftTransform,
                currentMergeTrajectoryRenderer,
                futureMergeTrajectory,
                false,
                true
            );
        }
        
        // Move to the next point when progress is complete
        if (_progress is < 1.0f and > -1.0f)
        {
            return;
        }

        _previousPointIndex = _currentPointIndex;
        
        // The simulation is reset.
        _currentPointIndex = (_currentPointIndex + Mathf.FloorToInt(_progress)) % _nominalPathPoints.Count;
        
        // The progress is reset.
        _progress %= 1;
        
        OnCurrentIndexUpdated?.Invoke(_currentPointIndex);
        
        UpdateTrajectory(
            NominalSpacecraftTransform,
            _currentNominalTrajectoryRenderer,
            futureNominalTrajectory,
            true,
            false
        );
        UpdateTrajectory(
            OffNominalSpacecraftTransform,
            _currentOffNominalTrajectoryRenderer,
            futureOffNominalTrajectory,
            true,
            false
        );
        if (_currentState == SpacecraftState.Merging)
        {
            UpdateTrajectory(
                MergeSpacecraftTransform,
                currentMergeTrajectoryRenderer,
                futureMergeTrajectory,
                true,
                false
            );
        }
        
        UpdateVelocityVector(_currentPointIndex);
    }
    
    private void OnChangedCurrentPath(SpacecraftState state)
    {
        _currentState = state;
        OnSpacecraftStateUpdated?.Invoke(_currentState);
    }
    
    private void OnMissionStageUpdated(MissionStage stage)
    {
        if (_currentNominalTrajectoryRenderer.Equals(stage.nominalLineRenderer))
        {
            return;
        }
        
        _currentNominalTrajectoryRenderer = stage.nominalLineRenderer;
        _currentNominalTrajectoryRenderer.SetPosition(0, NominalSpacecraftTransform.position);

        _currentOffNominalTrajectoryRenderer = stage.offnominalLineRenderer;
        _currentOffNominalTrajectoryRenderer.SetPosition(0, OffNominalSpacecraftTransform.position);
        
        // trigger animation here if it is correct stage
    }
    
    private void UpdateVelocityVector(int currentIndex)
    {
        Vector3 currentVelocityVector;
        Vector3 nextVelocityVector;

        List<string[]> pathPoints = _currentState == SpacecraftState.Nominal ? _nominalPathPoints : _offNominalPathPoints;

        try
        {
            // A Vector3 variable is created to store and compute information about the current velocity vector.
            currentVelocityVector = new Vector3(
                float.Parse(pathPoints[currentIndex][4]),
                float.Parse(pathPoints[currentIndex][5]),
                float.Parse(pathPoints[currentIndex][6]));
            nextVelocityVector = new Vector3(
                float.Parse(pathPoints[currentIndex + 1][4]),
                float.Parse(pathPoints[currentIndex + 1][5]),
                float.Parse(pathPoints[currentIndex + 1][6]));
        } 
        catch (FormatException e)
        {
            Debug.LogWarning($"Incorrect data format provided at line {currentIndex}: {e}");
            return;
        }

        velocityVector.position = spacecraft.position - spacecraft.forward;
        // velocityVector.rotation = Quaternion.Slerp(
        //     Quaternion.LookRotation(currentVelocityVector),
        //     Quaternion.LookRotation(nextVelocityVector), 
        //     _progress
        // );

        float magnitude = Mathf.Lerp(currentVelocityVector.magnitude, nextVelocityVector.magnitude, _progress);
        Transform velocityVectorModel = velocityVector.GetChild(0);

        velocityVectorModel.GetChild(1).localScale = new Vector3(1.0f, magnitude, 1.0f);
        velocityVectorModel.GetChild(0).localPosition = new Vector3(0.0f, magnitude + 1, 0.0f);
        
        int bracketIndex = magnitude switch
        {
            >= HighThreshold => 0,
            >= MediumThreshold => 1,
            >= LowThreshold => 2,
            _ => 2
        };

        foreach (Renderer meshRenderer in _vectorRenderers)
        {
            meshRenderer.material.SetColor(BaseColor, _colors[bracketIndex]);
            meshRenderer.material.SetColor(EmissionColor, _colors[bracketIndex] * Intensity);
        }
    }
    
    private void CalculateDistances()
    {
        float distanceToEarth = Vector3.Distance(
            spacecraft.position, earth.transform.position) / trajectoryScale;
        float distanceToMoon = Vector3.Distance(
            spacecraft.position, moon.transform.position) / trajectoryScale;

        float distanceTravelledToSend = _currentState == SpacecraftState.OffNominal ? _totalOffNominalDistance : _totalNominalDistance;

        OnDistanceCalculated?.Invoke(
            new DistanceTravelledEventArgs(distanceTravelledToSend, distanceToEarth, distanceToMoon));
    }

    private void DisplayModel(int displayedModelIndex)
    {
        Transform rocketParts = spacecraft.GetChild(0);
        for (int modelIndex = 0; modelIndex < rocketParts.childCount; modelIndex++)
        {
            rocketParts.GetChild(modelIndex).gameObject.SetActive(modelIndex == displayedModelIndex);
        }
    }
    
    private void OnBumpOffCourse()
    {
        _currentState = SpacecraftState.Manual;
        _lastAutomaticSpacecraftPosition = transform.position;
        _lastAutomaticSpacecraftIndex = _currentPointIndex;
        
        Invoke(nameof(PushOnCourse), MaximumManualControlTime);
    }
    
    private void PushOnCourse()
    {
        _currentState = SpacecraftState.Returning;
        _lastManualSpacecraftPosition = transform.position;
        _lastManualSpacecraftIndex = _currentPointIndex;
        
        // The future path is predicted.
        var futureExpectedPositionIndex = GetClosestIndexFromTime(
            _estimatedElapsedTime + MaximumManualControlTime, _nominalPathPoints);
        var futureExpectedPosition = new Vector3(
            float.Parse(_nominalPathPoints[futureExpectedPositionIndex][1]),
            float.Parse(_nominalPathPoints[futureExpectedPositionIndex][2]),
            float.Parse(_nominalPathPoints[futureExpectedPositionIndex][3]));
        
        // Get the current velocity.
        var velocity = new Vector3(
            float.Parse(_nominalPathPoints[_lastManualSpacecraftIndex][4]),
            float.Parse(_nominalPathPoints[_lastManualSpacecraftIndex][5]),
            float.Parse(_nominalPathPoints[_lastManualSpacecraftIndex][6]));
    
        var minimumTimes = new List<float>();
        
        // Reads up to 60 points into the future
        for (var futureIndex = 0; futureIndex <= MaximumFutureDataPoints; futureIndex++)
        {
            var machineLearningFutureIndex = futureExpectedPositionIndex + futureIndex;
            var machineLearningPosition = new Vector3(
                float.Parse(_nominalPathPoints[machineLearningFutureIndex][1]),
                float.Parse(_nominalPathPoints[machineLearningFutureIndex][2]),
                float.Parse(_nominalPathPoints[machineLearningFutureIndex][3]));
            
            var distance = Vector3.Distance(spacecraft.position, machineLearningPosition);
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
    
        // Vector3.Lerp(_lastManualSpacecraftPosition, futureExpectedPosition, 0.5f);
        
        _currentState = SpacecraftState.Nominal;
    }
    
    private void ManuallyControlSpacecraft()
    {
        const float speed = 2.125f;
        foreach (var key in _manualControlScheme.Keys.Where(Input.GetKey))
        {
            spacecraft.position += speed * Time.deltaTime * _manualControlScheme[key];
        }
    }
    
    private int GetClosestIndexFromTime(float time, List<string[]> pathPoints)
    {
        var closestIndex = 0;
        var closestTime = float.MaxValue;
    
        for (var i = 0; i < pathPoints.Count; i++)
        {
            try
            {
                var timeDistance = Mathf.Abs(float.Parse(pathPoints[i][0]) - time);
    
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
        
        return closestIndex;
    }
    
    private int[] GetIndexBoundsFromTime(float elapsedTime, List<string[]> pathPoints)
    {
        var closestIndex = GetClosestIndexFromTime(elapsedTime, pathPoints);
        
        var closestTime = float.Parse(pathPoints[closestIndex][0]);
        
        var indexBounds = new int[2];
        indexBounds[0] = closestTime < elapsedTime ? closestIndex : closestIndex - 1;
        indexBounds[1] = closestTime < elapsedTime ? closestIndex + 1 : closestIndex;
        
        return indexBounds;
    }
    
    private Vector3 GetPositionFromTime(List<string[]> pathPoints, float elapsedTime)
    {
        var indexBounds = GetIndexBoundsFromTime(elapsedTime, _nominalPathPoints);
        var lowerIndex = indexBounds[0];
        var upperIndex = indexBounds[1];
        
        var lowerTime = float.Parse(pathPoints[lowerIndex][0]);
        var upperTime = float.Parse(pathPoints[upperIndex][0]);
        
        var interpolationRatio = Mathf.InverseLerp(lowerTime, upperTime, elapsedTime);

        var lowerPositionX = float.Parse(pathPoints[lowerIndex][1]);
        var lowerPositionY = float.Parse(pathPoints[lowerIndex][2]);
        var lowerPositionZ = float.Parse(pathPoints[lowerIndex][3]);
        
        var upperPositionX = float.Parse(pathPoints[upperIndex][1]);
        var upperPositionY = float.Parse(pathPoints[upperIndex][2]);
        var upperPositionZ = float.Parse(pathPoints[upperIndex][3]);
        
        return new Vector3(
            Mathf.Lerp(lowerPositionX, upperPositionX, interpolationRatio),
            Mathf.Lerp(lowerPositionY, upperPositionY, interpolationRatio),
            Mathf.Lerp(lowerPositionZ, upperPositionZ, interpolationRatio)
        );
    }
    
    public Vector3 GetNominalPositionFromTime(float elapsedTime)
    {
        return GetPositionFromTime(_nominalPathPoints, elapsedTime);
    }
    
    public Vector3 GetOffNominalPositionFromTime(float elapsedTime)
    {
        return GetPositionFromTime(_offNominalPathPoints, elapsedTime);
    }
    
    private void OnPathCalculated(string data)
    {
        Debug.Log(data);
        
        _mergePathPoints = CsvReader.TextToData(data);
        _mergePathPoints.RemoveAt(0);
        
        PlotTrajectory(_mergePathPoints, currentMergeTrajectoryRenderer, futureMergeTrajectory);
        
        _currentState = SpacecraftState.Merging;
    }
    
    
    # region Models
    
    private void UpdateModel(int cutsceneIndex)
    {
        switch (cutsceneIndex)
        {
            case < 3:
                DisplayModel(0);
                return;
            case <= 6:
                DisplayModel(cutsceneIndex - 2);
                return;
            default:
                DisplayModel(4);
                return;
        }
    }
    
    # endregion
    
    public enum SpacecraftState
    {
        Nominal,
        OffNominal,
        Merging,
        Manual,
        Returning,
    }
}