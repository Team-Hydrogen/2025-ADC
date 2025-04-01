using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpacecraftManager : MonoBehaviour
{
    #region References
    
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
    
    [Header("Future Trajectories")]
    [SerializeField] private LineRenderer futureNominalTrajectory;
    [SerializeField] private LineRenderer futureOffNominalTrajectory;
    
    [Header("Merge Trajectory")]
    [SerializeField] private Transform trajectoryParent;
    [SerializeField] private GameObject mergeTrajectoryPrefab;
    
    [Header("Celestial Bodies")]
    [SerializeField] private Transform earth;
    [SerializeField] private Transform moon;
    
    [Header("Time Scale")]
    [SerializeField] private float timeScale;
    
    #endregion
    
    #region Timeline Variables
    
    private const float MinimumTimeScale = 1.0f;
    private const float MaximumTimeScale = 100_000.0f;
    
    private const float SkipForwardTimeInMinutes = 10.0f;
    private const float SkipBackwardTimeInMinutes = 10.0f;
    
    private float _timeIntervalInSeconds;
    private float _progress = 0.0f;
    private float _elapsedTime;
    
    #endregion
    
    #region Data Variables
    
    private float _nominalDistanceTraveled = 0.0f;
    private float _offNominalDistanceTraveled = 0.0f;
    private float _mass;
    
    #endregion
    
    #region Index Variables
    
    private const int SecondStageFireIndex = 120; // The second stage is the same as the service module.
    
    private int _previousPointIndex = 0;
    private int _currentPointIndex = 0;
    private bool _isPlaying = false;
    
    #endregion
    
    #region Trajectory Data and Visualization
    
    private SpacecraftState _currentState = SpacecraftState.Nominal;
    
    private List<string[]> _nominalTrajectoryData;
    private List<string[]> _offNominalTrajectoryData;
    private List<string[]> _transitionTrajectoryData;
    
    private LineRenderer _pastNominalTrajectoryRenderer;
    private LineRenderer _pastOffNominalTrajectoryRenderer;
    
    private LineRenderer _pastMergeTrajectoryRenderer;
    private LineRenderer _futureMergeTrajectoryRenderer;
    
    #endregion
    
    #region Keyboard Controls
    
    private const float SpacecraftSpeed = 2.5f;
    private const float MaximumInputTime = 5.0f;
    private const int MaximumFutureDataPoints = 60;
    
    private int _lastAutomaticSpacecraftIndex;
    private int _lastManualSpacecraftIndex;
    
    private Vector3 _lastAutomaticSpacecraftPosition;
    private Vector3 _lastManualSpacecraftPosition;

    private readonly Dictionary<KeyCode, Vector3> _keyboardInputScheme = new()
    {
        {KeyCode.A, Vector3.left},
        {KeyCode.D, Vector3.right},
        {KeyCode.W, Vector3.forward},
        {KeyCode.S, Vector3.back},
        {KeyCode.Q, Vector3.down},
        {KeyCode.E, Vector3.up},
    };
    
    #endregion
    
    #region Vector Material Variables

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
    
    #region Actions
    
    public static event Action<int> OnCurrentIndexUpdated; 
    public static event Action<float> OnUpdateTime;
    public static event Action<Vector3> OnUpdateCoordinates;
    public static event Action<float> OnUpdateMass;
    public static event Action<DistanceTravelledEventArgs> OnDistanceCalculated;
    public static event Action<float> OnTimeScaleSet;
    public static event Action<string> OnStageFired;
    public static event Action<SpacecraftState> OnSpacecraftStateUpdated;
    
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
        _nominalDistanceTraveled = 0.0f;
        _vectorRenderers = velocityVector.GetComponentsInChildren<Renderer>();
        OnTimeScaleSet?.Invoke(timeScale);
    }
    
    private void Update()
    {
        if (_isPlaying)
        {
            UpdateSpacecraft();
            
            if (_currentState == SpacecraftState.Manual)
            {
                HandleKeyboardInput();
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
        DataManager.OnDataLoaded += LoadData;
        DataManager.OnMissionStageUpdated += OnMissionStageUpdated;
        HttpManager.OnPathCalculated += OnPathCalculated;
        UIManager.OnCurrentPathChanged += OnTrajectorySelected;
    }
    
    private void OnDisable()
    {
        CutsceneManager.OnCutsceneStart -= UpdateModel;
        DataManager.OnDataLoaded -= LoadData;
        DataManager.OnMissionStageUpdated -= OnMissionStageUpdated;
        HttpManager.OnPathCalculated -= OnPathCalculated;
        UIManager.OnCurrentPathChanged -= OnTrajectorySelected;
    }
    
    #endregion
    
    #region Timeline Methods

    public void ForwardButtonPressed()
    {
        _progress = SkipForwardTimeInMinutes * timeScale;
    }
    
    public void BackwardButtonPressed()
    {
        _progress = -SkipBackwardTimeInMinutes * timeScale;
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
    
    private void UpdateElapsedTime()
    {
        const float secondsPerMinute = 60.0f;
        
        // If the current index exceeds or is the last index, progress is reset to 0, and time freezes.
        if (_currentPointIndex >= _nominalTrajectoryData.Count - 1)
        {
            Time.timeScale = 0;
            return;
        }
        
        float currentTime = float.Parse(_nominalTrajectoryData[_currentPointIndex][0]);
        float futureTime = float.Parse(_nominalTrajectoryData[_currentPointIndex + 1][0]);
        
        _timeIntervalInSeconds = (futureTime - currentTime) * secondsPerMinute;
        _progress += Time.deltaTime * timeScale / _timeIntervalInSeconds;
        _elapsedTime = currentTime + (futureTime - currentTime) * _progress;
        
        // Reset the progress and elapsed time if the elapsed time is negative.
        if (_elapsedTime < 0.0f)
        {
            _progress = 0.0f;
            _elapsedTime = 0.0f;
        }
        
        OnUpdateTime?.Invoke(_elapsedTime);
    }
    
    private void OnMissionStageUpdated(MissionStage stage)
    {
        if (_pastNominalTrajectoryRenderer.Equals(stage.nominalLineRenderer))
        {
            return;
        }
        
        _pastNominalTrajectoryRenderer = stage.nominalLineRenderer;
        _pastNominalTrajectoryRenderer.SetPosition(0, NominalSpacecraftTransform.position);

        _pastOffNominalTrajectoryRenderer = stage.offnominalLineRenderer;
        _pastOffNominalTrajectoryRenderer.SetPosition(0, OffNominalSpacecraftTransform.position);
        
        // trigger animation here if it is correct stage
    }
    
    #endregion

    #region Trajectory
    
    /// <summary>
    /// Converts trajectory data into a visualization
    /// </summary>
    /// <param name="points">A list of three-dimensional points</param>
    /// <param name="past">A line to represent the past path</param>
    /// <param name="future">A line to represent the future path</param>
    private void PlotTrajectory(List<string[]> points, LineRenderer past, LineRenderer future)
    {
        // An array of three-dimensional points is constructed by processing the CSV file.
        int numberOfPoints = points.Count;
        Vector3[] futurePoints = new Vector3[numberOfPoints];
        
        for (int index = 0; index < numberOfPoints; index++)
        {
            string[] point = points[index];
            
            try
            {
                Vector3 pointAsVector = new Vector3(
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
    /// Updates the trajectory
    /// </summary>
    /// <param name="spacecraftTransform"></param>
    /// <param name="past"></param>
    /// <param name="future"></param>
    /// <param name="indexUpdated"></param>
    /// <param name="positionUpdated"></param>
    private void UpdateTrajectory(Transform spacecraftTransform, LineRenderer past, LineRenderer future, bool indexUpdated, bool positionUpdated)
    {
        if (positionUpdated)
        {
            past.SetPosition(past.positionCount - 1, spacecraftTransform.position);
            future.SetPosition(0, spacecraftTransform.position);
        }
        
        if (!indexUpdated)
        {
            return;
        }
        
        int indexChange = _currentPointIndex - _previousPointIndex;
        Debug.Log($"index change = {indexChange} @ t = {_elapsedTime}");
        
        switch (indexChange)
        {
            // If the index change is positive, the spacecraft moves forward across data points.
            case > 0:
            {
                // Get all past trajectory data points.
                Vector3[] pastTrajectoryPoints = new Vector3[past.positionCount];
                past.GetPositions(pastTrajectoryPoints);
                // Get all future trajectory data points.
                Vector3[] futureTrajectoryPoints = new Vector3[future.positionCount];
                future.GetPositions(futureTrajectoryPoints);
                
                // Get the traversed points between the past and current indexes.
                Vector3[] traversedPoints = new Vector3[indexChange];
                Array.Copy(futureTrajectoryPoints, 1, traversedPoints, 0, indexChange);
                
                // Create new trajectory arrays.
                Vector3[] newPastTrajectoryPoints = new Vector3[past.positionCount + indexChange];
                Vector3[] newFutureTrajectoryPoints = new Vector3[future.positionCount - indexChange];
                
                // Append the traversed points with the past trajectory.
                pastTrajectoryPoints.CopyTo(newPastTrajectoryPoints, 0);
                traversedPoints.CopyTo(newPastTrajectoryPoints, pastTrajectoryPoints.Length);
                // Update the past trajectory.
                past.positionCount = newPastTrajectoryPoints.Length;
                past.SetPositions(newPastTrajectoryPoints);
                
                // Remove the traversed points from the future trajectory.
                Array.Copy(
                    futureTrajectoryPoints,
                    indexChange,
                    newFutureTrajectoryPoints,
                    0,
                    newFutureTrajectoryPoints.Length);
                // Update the future trajectory.
                future.positionCount = newFutureTrajectoryPoints.Length;
                future.SetPositions(newFutureTrajectoryPoints);
                
                break;
            }
            // Otherwise, if the index change is negative, the spacecraft moves backward across data points.
            case < 0:
            {
                indexChange = -indexChange;
                
                // Get all past trajectory data points.
                Vector3[] pastTrajectoryPoints = new Vector3[past.positionCount];
                past.GetPositions(pastTrajectoryPoints);
                // Get all future trajectory data points.
                Vector3[] futureTrajectoryPoints = new Vector3[future.positionCount];
                future.GetPositions(futureTrajectoryPoints);
                
                // Get the traversed points between the past and current indexes.
                Vector3[] traversedPoints = new Vector3[indexChange];
                Array.Copy(
                    pastTrajectoryPoints,
                    pastTrajectoryPoints.Length - indexChange - 1,
                    traversedPoints,
                    0,
                    indexChange);
                
                // Create new trajectory arrays.
                Vector3[] newPastTrajectoryPoints = new Vector3[past.positionCount - indexChange];
                Vector3[] newFutureTrajectoryPoints = new Vector3[future.positionCount + indexChange];
                
                // Prepend the traversed points with the future trajectory.
                traversedPoints.CopyTo(newFutureTrajectoryPoints, 0);
                futureTrajectoryPoints.CopyTo(newFutureTrajectoryPoints, traversedPoints.Length);
                // Update the future trajectory.
                future.positionCount = newFutureTrajectoryPoints.Length;
                future.SetPositions(newFutureTrajectoryPoints);
                
                // Remove the traversed points from the past trajectory.
                Array.Copy(
                    pastTrajectoryPoints,
                    0,
                    newPastTrajectoryPoints,
                    0,
                    newPastTrajectoryPoints.Length);
                // Update the past trajectory.
                past.positionCount = newPastTrajectoryPoints.Length;
                past.SetPositions(newPastTrajectoryPoints);
                
                break;
            }
        }
    }
    
    /// <summary>
    /// Updates the spacecraft's current state
    /// </summary>
    /// <param name="state">Selected trajectory</param>
    private void OnTrajectorySelected(SpacecraftState state)
    {
        _currentState = state;
        OnSpacecraftStateUpdated?.Invoke(_currentState);
    }
    
    #endregion
    
    /// <summary>
    /// Updates the position of the Orion capsule
    /// </summary>
    private void UpdateSpacecraft()
    {
        // Avoids re-calculating data when a spacecraft travels along a generated trajectory or deviates from the main
        // trajectories through keyboard input.
        if (_currentState is SpacecraftState.Manual or SpacecraftState.Transition)
        {
            return;
        }
        
        UpdateElapsedTime();
        
        _nominalDistanceTraveled += UpdateSpacecraftPositionOnPath(_nominalTrajectoryData, NominalSpacecraftTransform);
        _offNominalDistanceTraveled += UpdateSpacecraftPositionOnPath(_offNominalTrajectoryData, OffNominalSpacecraftTransform);
        
        if (_currentState == SpacecraftState.Transition)
        {
            UpdateSpacecraftPositionOnPathFromTime(_elapsedTime, _transitionTrajectoryData, MergeSpacecraftTransform);
        }
        
        UpdateSpacecraftProgressAndTrajectories();
        SetSpacecraftVisualToPosition();
    }

    private float UpdateSpacecraftPositionOnPath(List<string[]> points, Transform spacecraftPosition)
    {
        Debug.Log($"p = {_progress}, ⌊p⌋ = {Mathf.FloorToInt(_progress)}, ⌈p⌉ = {Mathf.CeilToInt(_progress)} @ t = {_elapsedTime}");
        
        // Determine the current and future indices.
        int currentIndex = _currentPointIndex;
        int futureIndex = _currentPointIndex + 1;
        // int futureIndex = _currentPointIndex + Mathf.CeilToInt(_progress);
        futureIndex = Mathf.Min(futureIndex, points.Count - 1);
        
        string[] currentPoint = points[currentIndex];
        string[] futurePoint = points[futureIndex]; 
        
        // Get the current position and velocity.
        Vector3 currentPosition = new Vector3(
            float.Parse(currentPoint[1]),
            float.Parse(currentPoint[2]),
            float.Parse(currentPoint[3])
        ) * trajectoryScale;
        Vector3 currentVelocity = new Vector3(
            float.Parse(currentPoint[4]),
            float.Parse(currentPoint[5]),
            float.Parse(currentPoint[6])
        );
        
        // Get the future position and velocity.
        Vector3 futurePosition = new Vector3(
            float.Parse(futurePoint[1]),
            float.Parse(futurePoint[2]),
            float.Parse(futurePoint[3])
        ) * trajectoryScale;
        Vector3 futureVelocity = new Vector3(
            float.Parse(futurePoint[4]),
            float.Parse(futurePoint[5]),
            float.Parse(futurePoint[6])
        );
        
        // Interpolate position
        Vector3 previousPosition = spacecraftPosition.position;
        spacecraftPosition.position = Vector3.Lerp(currentPosition, futurePosition, _progress);
        // spacecraftPosition.position = Vector3.Lerp(currentPosition, futurePosition, _progress / Mathf.CeilToInt(_progress));
        
        float netDistance = Vector3.Distance(previousPosition, spacecraftPosition.position) / trajectoryScale;
        
        // Calculate spacecraft direction
        Vector3 direction = (futurePosition - currentPosition).normalized;
        if (direction == Vector3.zero)
        {
            return netDistance;
        }
        
        // Interpolate rotation
        spacecraftPosition.rotation = Quaternion.Slerp(
            Quaternion.LookRotation(currentVelocity) * Quaternion.Euler(90.0f, 0.0f, 0.0f),
            Quaternion.LookRotation(futureVelocity) * Quaternion.Euler(90.0f, 0.0f, 0.0f), 
            _progress
        );

        return netDistance;
    }
    
    private float UpdateSpacecraftPositionOnPathFromTime(float elapsedTime, List<string[]> points, Transform spacecraftPosition)
    {
        int[] indexBounds = GetIndexBoundsFromTime(elapsedTime, points);
        int lowerIndex = indexBounds[0];
        int upperIndex = indexBounds[1];
        
        string[] currentPoint = points[lowerIndex];
        Vector3 currentVelocityVector = new Vector3(
            float.Parse(currentPoint[4]),
            float.Parse(currentPoint[5]),
            float.Parse(currentPoint[6]));
        
        string[] nextPoint = points[upperIndex];
        Vector3 nextVelocityVector = new Vector3(
            float.Parse(nextPoint[4]),
            float.Parse(nextPoint[5]),
            float.Parse(nextPoint[6]));
        
        
        Vector3 currentPosition = new Vector3(
            float.Parse(currentPoint[1]),
            float.Parse(currentPoint[2]),
            float.Parse(currentPoint[3])
        ) * trajectoryScale;
        Vector3 nextPosition = new Vector3(
            float.Parse(nextPoint[1]),
            float.Parse(nextPoint[2]),
            float.Parse(nextPoint[3])
        ) * trajectoryScale;
        
        // Interpolate position
        Vector3 previousPosition = spacecraftPosition.position;
        spacecraftPosition.position = Vector3.Lerp(currentPosition, nextPosition, _progress);

        float netDistance = Vector3.Distance(previousPosition, spacecraftPosition.position) / trajectoryScale;

        // Calculate spacecraft direction
        Vector3 direction = (nextPosition - currentPosition).normalized;
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
    
    private void UpdateSpacecraftProgressAndTrajectories()
    {
        OnUpdateCoordinates?.Invoke(spacecraft.position / trajectoryScale);
        CalculateDistances();
        UpdateSpacecraftMass(_currentPointIndex);
        
        UpdateTrajectory(
            NominalSpacecraftTransform,
            _pastNominalTrajectoryRenderer,
            futureNominalTrajectory,
            false,
            true
        );

        UpdateTrajectory(
            OffNominalSpacecraftTransform,
            _pastOffNominalTrajectoryRenderer,
            futureOffNominalTrajectory,
            false,
            true
        );

        if (_currentState == SpacecraftState.Transition)
        {
            UpdateTrajectory(
                MergeSpacecraftTransform,
                _pastMergeTrajectoryRenderer,
                _futureMergeTrajectoryRenderer,
                false,
                true
            );
        }
        
        // Move to the next point when progress is complete.
        if (_progress is < 1.0f and > -1.0f)
        {
            return;
        }
        
        // The previous index is set to the current.
        _previousPointIndex = _currentPointIndex;
        _currentPointIndex = Mathf.Clamp(
            _currentPointIndex + Mathf.FloorToInt(_progress), 0, _nominalTrajectoryData.Count - 1);
        
        // The progress is reset.
        _progress %= 1;
        
        OnCurrentIndexUpdated?.Invoke(_currentPointIndex);
        
        UpdateTrajectory(
            NominalSpacecraftTransform,
            _pastNominalTrajectoryRenderer,
            futureNominalTrajectory,
            true,
            false
        );

        UpdateTrajectory(
            OffNominalSpacecraftTransform,
            _pastOffNominalTrajectoryRenderer,
            futureOffNominalTrajectory,
            true,
            false
        );

        if (_currentState == SpacecraftState.Transition)
        {
            UpdateTrajectory(
                MergeSpacecraftTransform,
                _pastMergeTrajectoryRenderer,
                _futureMergeTrajectoryRenderer,
                true,
                false
            );
        }
        
        UpdateVelocityVector(_currentPointIndex);
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
            case SpacecraftState.Transition:
                spacecraft.position = MergeSpacecraftTransform.position;
                spacecraft.rotation = MergeSpacecraftTransform.rotation;
                break;
            case SpacecraftState.Manual:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    #region Data Methods
    
    private void LoadData(DataLoadedEventArgs data)
    {
        // Get both the nominal and off-nominal trajectory data.
        _nominalTrajectoryData = data.NominalTrajectoryData;
        _offNominalTrajectoryData = data.OffNominalTrajectoryData;
        // Get both the nominal and off-nominal line renderers.
        _pastNominalTrajectoryRenderer = data.MissionStage.nominalLineRenderer;
        _pastOffNominalTrajectoryRenderer = data.MissionStage.offnominalLineRenderer;
        
        // Generate an initial plot of both trajectories. 
        PlotTrajectory(_nominalTrajectoryData, _pastNominalTrajectoryRenderer, futureNominalTrajectory);
        PlotTrajectory(_offNominalTrajectoryData, _pastOffNominalTrajectoryRenderer, futureOffNominalTrajectory);
        
        UpdateVelocityVector(_currentPointIndex);
        
        _isPlaying = true;
        
        OnCurrentIndexUpdated?.Invoke(_currentPointIndex);
    }
    
    private void CalculateDistances()
    {
        // Get the distance between the spacecraft and the celestial bodies.
        float distanceToEarth = Vector3.Distance(spacecraft.position, earth.position) / trajectoryScale;
        float distanceToMoon = Vector3.Distance(spacecraft.position, moon.position) / trajectoryScale;
        
        // Get the distance the spacecraft traveled along its selected path.
        float selectedDistanceTraveled = _currentState == SpacecraftState.Nominal
            ? _nominalDistanceTraveled
            : _offNominalDistanceTraveled;

        OnDistanceCalculated?.Invoke(
            new DistanceTravelledEventArgs(selectedDistanceTraveled, distanceToEarth, distanceToMoon));
    }
    
    private void UpdateSpacecraftMass(int currentIndex)
    {
        bool isMassValid = float.TryParse(_nominalTrajectoryData[currentIndex][7], out _mass);
        
        if (!isMassValid)
        {
            throw new FormatException($"There is no spacecraft mass data on line {currentIndex}.");
        }
        
        OnUpdateMass?.Invoke(_mass);
    }
    
    private void UpdateVelocityVector(int currentIndex)
    {
        Vector3 currentVelocityVector;
        Vector3 nextVelocityVector;

        List<string[]> pathPoints = _currentState == SpacecraftState.Nominal ? _nominalTrajectoryData : _offNominalTrajectoryData;

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
        catch (ArgumentOutOfRangeException e)
        {
            return;
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
    
    #endregion
    
    #region Machine Learning
    
    #region Transition Path
    
    private void OnPathCalculated(string data)
    {
        Debug.Log(data);
        
        _transitionTrajectoryData = CsvReader.TextToData(data);
        _transitionTrajectoryData.RemoveAt(0);
        
        GameObject mergeTrajectory = Instantiate(mergeTrajectoryPrefab, trajectoryParent);
        LineRenderer[] mergeTrajectoryRenderers = mergeTrajectory.GetComponentsInChildren<LineRenderer>();

        _pastMergeTrajectoryRenderer = mergeTrajectoryRenderers[0];
        _futureMergeTrajectoryRenderer = mergeTrajectoryRenderers[1];
        
        PlotTrajectory(_transitionTrajectoryData, _pastMergeTrajectoryRenderer, _futureMergeTrajectoryRenderer);
        
        _currentState = SpacecraftState.Transition;
    }
    
    #endregion
    
    #region Bump Off Course

    private void OnBumpOffCourse()
    {
        _currentState = SpacecraftState.Manual;
        _lastAutomaticSpacecraftPosition = transform.position;
        _lastAutomaticSpacecraftIndex = _currentPointIndex;
        
        Invoke(nameof(PushOnCourse), MaximumInputTime);
    }
    
    private void PushOnCourse()
    {
        _currentState = SpacecraftState.Transition;
        _lastManualSpacecraftPosition = transform.position;
        _lastManualSpacecraftIndex = _currentPointIndex;
        
        // The future path is predicted.
        var futureExpectedPositionIndex = GetClosestIndexFromTime(
            _elapsedTime + MaximumInputTime, _nominalTrajectoryData);
        var futureExpectedPosition = new Vector3(
            float.Parse(_nominalTrajectoryData[futureExpectedPositionIndex][1]),
            float.Parse(_nominalTrajectoryData[futureExpectedPositionIndex][2]),
            float.Parse(_nominalTrajectoryData[futureExpectedPositionIndex][3]));
        
        // Get the current velocity.
        var velocity = new Vector3(
            float.Parse(_nominalTrajectoryData[_lastManualSpacecraftIndex][4]),
            float.Parse(_nominalTrajectoryData[_lastManualSpacecraftIndex][5]),
            float.Parse(_nominalTrajectoryData[_lastManualSpacecraftIndex][6]));
    
        var minimumTimes = new List<float>();
        
        // Reads up to 60 points into the future
        for (var futureIndex = 0; futureIndex <= MaximumFutureDataPoints; futureIndex++)
        {
            var machineLearningFutureIndex = futureExpectedPositionIndex + futureIndex;
            var machineLearningPosition = new Vector3(
                float.Parse(_nominalTrajectoryData[machineLearningFutureIndex][1]),
                float.Parse(_nominalTrajectoryData[machineLearningFutureIndex][2]),
                float.Parse(_nominalTrajectoryData[machineLearningFutureIndex][3]));
            
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
    
    private void HandleKeyboardInput()
    {
        foreach (var key in _keyboardInputScheme.Keys.Where(Input.GetKey))
        {
            spacecraft.position += SpacecraftSpeed * Time.deltaTime * _keyboardInputScheme[key];
        }
    }

    #endregion
    
    #endregion
    
    #region Time Helper Functions

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
    
    private Vector3 GetPositionFromTime(List<string[]> trajectoryData, float elapsedTime)
    {
        var indexBounds = GetIndexBoundsFromTime(elapsedTime, _nominalTrajectoryData);
        var lowerIndex = indexBounds[0];
        var upperIndex = indexBounds[1];
        
        var lowerTime = float.Parse(trajectoryData[lowerIndex][0]);
        var upperTime = float.Parse(trajectoryData[upperIndex][0]);
        
        var interpolationRatio = Mathf.InverseLerp(lowerTime, upperTime, elapsedTime);

        var lowerPositionX = float.Parse(trajectoryData[lowerIndex][1]);
        var lowerPositionY = float.Parse(trajectoryData[lowerIndex][2]);
        var lowerPositionZ = float.Parse(trajectoryData[lowerIndex][3]);
        
        var upperPositionX = float.Parse(trajectoryData[upperIndex][1]);
        var upperPositionY = float.Parse(trajectoryData[upperIndex][2]);
        var upperPositionZ = float.Parse(trajectoryData[upperIndex][3]);
        
        return new Vector3(
            Mathf.Lerp(lowerPositionX, upperPositionX, interpolationRatio),
            Mathf.Lerp(lowerPositionY, upperPositionY, interpolationRatio),
            Mathf.Lerp(lowerPositionZ, upperPositionZ, interpolationRatio)
        );
    }
    
    private Vector3 GetVelocityFromTime(List<string[]> trajectoryData, float elapsedTime)
    {
        var indexBounds = GetIndexBoundsFromTime(elapsedTime, _nominalTrajectoryData);
        var lowerIndex = indexBounds[0];
        var upperIndex = indexBounds[1];
        
        var lowerTime = float.Parse(trajectoryData[lowerIndex][0]);
        var upperTime = float.Parse(trajectoryData[upperIndex][0]);
        
        var interpolationRatio = Mathf.InverseLerp(lowerTime, upperTime, elapsedTime);

        var lowerVelocityX = float.Parse(trajectoryData[lowerIndex][4]);
        var lowerVelocityY = float.Parse(trajectoryData[lowerIndex][5]);
        var lowerVelocityZ = float.Parse(trajectoryData[lowerIndex][6]);
        
        var upperVelocityX = float.Parse(trajectoryData[upperIndex][4]);
        var upperVelocityY = float.Parse(trajectoryData[upperIndex][5]);
        var upperVelocityZ = float.Parse(trajectoryData[upperIndex][6]);
        
        return new Vector3(
            Mathf.Lerp(lowerVelocityX, upperVelocityX, interpolationRatio),
            Mathf.Lerp(lowerVelocityY, upperVelocityY, interpolationRatio),
            Mathf.Lerp(lowerVelocityZ, upperVelocityZ, interpolationRatio)
        );
    }
    
    public Vector3 GetNominalPositionFromTime(float elapsedTime)
    {
        return GetPositionFromTime(_nominalTrajectoryData, elapsedTime);
    }
    
    public Vector3 GetNominalVelocityFromTime(float elapsedTime)
    {
        return GetVelocityFromTime(_nominalTrajectoryData, elapsedTime);
    }
    
    public Vector3 GetOffNominalPositionFromTime(float elapsedTime)
    {
        return GetPositionFromTime(_offNominalTrajectoryData, elapsedTime);
    }
    
    public Vector3 GetOffNominalVelocityFromTime(float elapsedTime)
    {
        return GetVelocityFromTime(_offNominalTrajectoryData, elapsedTime);
    }

    #endregion

    #region Spacecraft Model
    
    private void SetModel(int displayedModelIndex)
    {
        Transform rocketParts = spacecraft.GetChild(0);
        for (int modelIndex = 0; modelIndex < rocketParts.childCount; modelIndex++)
        {
            rocketParts.GetChild(modelIndex).gameObject.SetActive(modelIndex == displayedModelIndex);
        }
    }

    private void UpdateModel(int cutsceneIndex)
    {
        int maximumModelIndex = spacecraft.GetChild(0).childCount - 1;
        int modelIndex = Mathf.Min(cutsceneIndex, maximumModelIndex);
        SetModel(modelIndex);
    }
    
    #endregion
    
    /// <summary>
    /// Stores the spacecraft's current trajectory
    /// </summary>
    public enum SpacecraftState
    {
        Nominal, // The spacecraft travels along the nominal trajectory.
        OffNominal, // The spacecraft travels along the off-nominal trajectory.
        Manual, // The spacecraft is subject to keyboard input by the user. 
        Transition // The spacecraft travels along a generated trajectory.
    }
}