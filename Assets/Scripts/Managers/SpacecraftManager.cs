using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpacecraftManager : MonoBehaviour
{
    public static SpacecraftManager Instance { get; private set; }
    
    #region References
    
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
    [field: SerializeField] public Transform TransitionSpacecraftTransform { get; private set; }
    
    [Header("Future Trajectories")]
    [SerializeField] private LineRenderer futureNominalTrajectory;
    [SerializeField] private LineRenderer futureOffNominalTrajectory;
    
    [Header("Celestial Bodies")]
    [SerializeField] private Transform earth;
    [SerializeField] private Transform moon;
    
    #endregion
    
    #region Timeline Variables
    
    private float _timeIntervalInSeconds;
    private float _interpolationRatio = 0.0f;
    private float _time;
    
    #endregion
    
    #region Data Variables
    
    private float _nominalDistanceTraveled = 0.0f;
    private float _offNominalDistanceTraveled = 0.0f;
    
    #endregion
    
    #region Index Variables
    
    private int _dataIndex = 0;
    private bool _isPlaying = false;
    
    #endregion
    
    #region Trajectory Data and Visualization
    
    public enum SpacecraftState
    {
        Nominal, // The spacecraft travels along the nominal trajectory.
        OffNominal, // The spacecraft travels along the off-nominal trajectory.
        Manual, // The spacecraft is subject to keyboard input by the user. 
        Transition // The spacecraft travels along a generated trajectory.
    }
    private SpacecraftState _state = SpacecraftState.Nominal;
    
    private string[][] _nominalTrajectoryData;
    private string[][] _offNominalTrajectoryData;
    private string[][] _transitionTrajectoryData;

    #endregion
    
    #region Keyboard Controls
    
    private const float SpacecraftSpeed = 2.5f;
    private const float MaximumInputTime = 5.0f;
    private const int MaximumIndexOffset = 60;
    
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
    
    public static event Action<DistanceCalculatedEventArgs> DistancesUpdated;
    public static event Action PositionUpdated;
    public static event Action<SpacecraftState> SpacecraftStateUpdated;
    
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
    }
    
    private void Update()
    {
        if (_isPlaying)
        {
            UpdateSpacecraft();
            
            if (_state == SpacecraftState.Manual)
            {
                HandleKeyboardInput();
            }
        }
    }
    
    private void OnEnable()
    {
        CutsceneManager.OnCutsceneStart += UpdateModel;
        DataManager.DataLoaded += LoadData;
        DataManager.DataIndexUpdated += SetIndex;
        DataManager.ProgressUpdated += SetInterpolationRatio;
        HttpManager.PathCalculated += StartTransition;
        SimulationManager.ElapsedTimeUpdated += SyncElapsedTime;
        UIManager.TrajectorySelected += SetTrajectory;
    }
    
    private void OnDisable()
    {
        CutsceneManager.OnCutsceneStart -= UpdateModel;
        DataManager.DataLoaded -= LoadData;
        DataManager.DataIndexUpdated -= SetIndex;
        DataManager.ProgressUpdated -= SetInterpolationRatio;
        HttpManager.PathCalculated -= StartTransition;
        SimulationManager.ElapsedTimeUpdated -= SyncElapsedTime;
        UIManager.TrajectorySelected -= SetTrajectory;
    }
    
    #endregion
    
    
    #region Timeline Methods
    
    private void SyncElapsedTime(float elapsedTime)
    {
        _time = elapsedTime;
    }
    
    #endregion

    #region Trajectory
    
    /// <summary>
    /// Updates the spacecraft's current state
    /// </summary>
    /// <param name="state">Selected trajectory</param>
    private void SetTrajectory(SpacecraftState state)
    {
        _state = state;
        SpacecraftStateUpdated?.Invoke(_state);
    }
    
    #endregion
    
    /// <summary>
    /// Updates the position of the Orion capsule
    /// </summary>
    private void UpdateSpacecraft()
    {
        // Avoids re-calculating data when a spacecraft travels along a generated trajectory or deviates from the main
        // trajectories through keyboard input.
        if (_state is SpacecraftState.Manual or SpacecraftState.Transition)
        {
            return;
        }
        
        _nominalDistanceTraveled += UpdateTrajectoryPosition(_nominalTrajectoryData, NominalSpacecraftTransform);
        _offNominalDistanceTraveled += UpdateTrajectoryPosition(_offNominalTrajectoryData, OffNominalSpacecraftTransform);
        
        if (_state == SpacecraftState.Transition)
        {
            UpdateTrajectoryPosition(_transitionTrajectoryData, TransitionSpacecraftTransform);
        }
        
        // The spacecraft data is updated.
        UpdateCoordinates();
        UpdateDistances();
        UpdateVelocityVector(_dataIndex);
        // Set the spacecraft to its selected position and rotation.
        SetSpacecraftTransform();
    }
    
    
    /* TODO: UpdateTrajectoryPosition should be entirely removed since other functions handle that responsibility. */
    private float UpdateTrajectoryPosition(string[][] data, Transform spacecraftTransform)
    {
        // Determine the index bounds.
        int lowerIndex = _dataIndex;
        int upperIndex = Mathf.Min(_dataIndex + 1, data.Length - 1);
        
        // Get the appropriate data bounds.
        string[] lowerData = data[lowerIndex];
        string[] upperData = data[upperIndex]; 
        
        // Get the lower and upper position bounds.
        Vector3 lowerPosition = new Vector3(
            float.Parse(lowerData[1]),
            float.Parse(lowerData[2]),
            float.Parse(lowerData[3])
        ) * trajectoryScale;
        Vector3 upperPosition = new Vector3(
            float.Parse(upperData[1]),
            float.Parse(upperData[2]),
            float.Parse(upperData[3])
        ) * trajectoryScale;
        
        // Get the lower and upper velocity bounds.
        Vector3 lowerVelocity = new Vector3(
            float.Parse(lowerData[4]),
            float.Parse(lowerData[5]),
            float.Parse(lowerData[6])
        );
        Vector3 upperVelocity = new Vector3(
            float.Parse(upperData[4]),
            float.Parse(upperData[5]),
            float.Parse(upperData[6])
        );
        
        // Interpolate position based on progress between the lower and upper index.
        Vector3 previousPosition = spacecraftTransform.position;
        spacecraftTransform.position = Vector3.Lerp(lowerPosition, upperPosition, _interpolationRatio);
        
        /* TODO: Transfer data calculation code to the TrajectoryManager for more accurate calculations. Please view
            the server for more information regarding this issue. */
        float netDistance = Vector3.Distance(previousPosition, spacecraftTransform.position) / trajectoryScale;
        
        // Calculate spacecraft direction
        Vector3 direction = (upperPosition - lowerPosition).normalized;
        if (direction == Vector3.zero)
        {
            return netDistance;
        }
        
        /* TODO: Rockets point in the direction of motion, not velocity. The code should change, so the velocity vector
            is accurately represented through the Vector prefab, not by the rocket's rotation. More research is needed
            on this topic. */
        // Interpolate velocity based on progress between the lower and upper index.
        spacecraftTransform.rotation = Quaternion.Slerp(
            Quaternion.LookRotation(lowerVelocity) * Quaternion.Euler(90.0f, 0.0f, 0.0f),
            Quaternion.LookRotation(upperVelocity) * Quaternion.Euler(90.0f, 0.0f, 0.0f), 
            _interpolationRatio
        );
        
        return netDistance;
    }
    
    private void SetSpacecraftTransform()
    {
        switch (_state)
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
                spacecraft.position = TransitionSpacecraftTransform.position;
                spacecraft.rotation = TransitionSpacecraftTransform.rotation;
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

        UpdateVelocityVector(_dataIndex);
        
        _isPlaying = true;
    }

    private void SetIndex(int dataIndex)
    {
        _dataIndex = dataIndex;
    }

    private void SetInterpolationRatio(float interpolationRatio)
    {
        _interpolationRatio = interpolationRatio;
    }

    private void UpdateCoordinates()
    {
        NominalSpacecraftTransform.position = 
            DataManager.Instance.GetCoordinates(_nominalTrajectoryData) * trajectoryScale;
        OffNominalSpacecraftTransform.position = 
            DataManager.Instance.GetCoordinates(_offNominalTrajectoryData) * trajectoryScale;
        
        if (_state == SpacecraftState.Transition)
        {
            TransitionSpacecraftTransform.position = 
                DataManager.Instance.GetCoordinates(_transitionTrajectoryData) * trajectoryScale;
        }
        
        PositionUpdated?.Invoke();
    }
    
    private void UpdateDistances()
    {
        // Get the distance between the spacecraft and the celestial bodies.
        float distanceToEarth = Vector3.Distance(spacecraft.position, earth.position) / trajectoryScale;
        float distanceToMoon = Vector3.Distance(spacecraft.position, moon.position) / trajectoryScale;
        
        // Get the distance the spacecraft traveled along its selected path.
        float selectedDistanceTraveled = _state == SpacecraftState.Nominal
            ? _nominalDistanceTraveled
            : _offNominalDistanceTraveled;

        DistancesUpdated?.Invoke(
            new DistanceCalculatedEventArgs(selectedDistanceTraveled, distanceToEarth, distanceToMoon));
    }
    
    private void UpdateVelocityVector(int lowerDataIndex)
    {
        string[][] selectedTrajectoryData = _state == SpacecraftState.Nominal
            ? _nominalTrajectoryData
            : _offNominalTrajectoryData;
        
        int upperDataIndex = Mathf.Min(lowerDataIndex + 1, selectedTrajectoryData.Length - 1);
        
        // A Vector3 variable is created to store and compute information about the current velocity vector.
        var lowerVelocityVector = new Vector3(
            float.Parse(selectedTrajectoryData[lowerDataIndex][4]),
            float.Parse(selectedTrajectoryData[lowerDataIndex][5]),
            float.Parse(selectedTrajectoryData[lowerDataIndex][6])
        );
        var upperVelocityVector = new Vector3(
            float.Parse(selectedTrajectoryData[upperDataIndex][4]),
            float.Parse(selectedTrajectoryData[upperDataIndex][5]),
            float.Parse(selectedTrajectoryData[upperDataIndex][6])
        );

        velocityVector.position = spacecraft.position - spacecraft.forward;
        // velocityVector.rotation = Quaternion.Slerp(
        //     Quaternion.LookRotation(currentVelocityVector),
        //     Quaternion.LookRotation(nextVelocityVector), 
        //     _progress
        // );
        
        float magnitude = Mathf.Lerp(lowerVelocityVector.magnitude, upperVelocityVector.magnitude, _interpolationRatio);
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
    
    
    #region Trajectory Generation
    
    #region Transition Path
    
    private void StartTransition(string dataAsString)
    {
        _state = SpacecraftState.Transition;
        SpacecraftStateUpdated?.Invoke(_state);
    }
    
    #endregion
    
    #region Bump Off Course

    private void OnBumpOffCourse()
    {
        _state = SpacecraftState.Manual;
        SpacecraftStateUpdated?.Invoke(_state);
        
        _lastAutomaticSpacecraftPosition = spacecraft.position;
        _lastAutomaticSpacecraftIndex = _dataIndex;
        
        Invoke(nameof(PushOnCourse), MaximumInputTime);
    }
    
    private void PushOnCourse()
    {
        _state = SpacecraftState.Transition;
        SpacecraftStateUpdated?.Invoke(_state);
        
        _lastManualSpacecraftPosition = spacecraft.position;
        _lastManualSpacecraftIndex = _dataIndex;
        
        // The future path is predicted.
        var futureExpectedPositionIndex = GetClosestIndexFromTime(_time + MaximumInputTime, _nominalTrajectoryData);
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
        for (var offset = 0; offset <= MaximumIndexOffset; offset++)
        {
            var machineLearningFutureIndex = futureExpectedPositionIndex + offset;
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
        
        _state = SpacecraftState.Nominal;
        SpacecraftStateUpdated?.Invoke(_state);
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

    private int GetClosestIndexFromTime(float time, string[][] pathPoints)
    {
        var closestIndex = 0;
        var closestTime = float.MaxValue;
    
        for (var i = 0; i < pathPoints.Length; i++)
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
    
    private int[] GetIndexBoundsFromTime(float elapsedTime, string[][] pathPoints)
    {
        var closestIndex = GetClosestIndexFromTime(elapsedTime, pathPoints);
        
        var closestTime = float.Parse(pathPoints[closestIndex][0]);
        
        var indexBounds = new int[2];
        indexBounds[0] = closestTime < elapsedTime ? closestIndex : closestIndex - 1;
        indexBounds[1] = closestTime < elapsedTime ? closestIndex + 1 : closestIndex;
        
        return indexBounds;
    }
    
    private Vector3 GetPositionFromTime(string[][] trajectoryData, float elapsedTime)
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
    
    private Vector3 GetVelocityFromTime(string[][] trajectoryData, float elapsedTime)
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
}