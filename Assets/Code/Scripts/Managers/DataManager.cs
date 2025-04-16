using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Stores and computes provided data
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    
    #region Files
    
    [Header("Trajectory")]
    [SerializeField] private TextAsset nominalTrajectoryDataFile;
    [SerializeField] private TextAsset offNominalTrajectoryDataFile;
    
    [Header("Antenna Availability")]
    [SerializeField] private TextAsset nominalAntennaAvailabilityDataFile;
    [SerializeField] private TextAsset offNominalAntennaAvailabilityDataFile;
    
    [Header("Link Budget")]
    [SerializeField] private TextAsset nominalLinkBudgetDataFile;
    [SerializeField] private TextAsset offNominalLinkBudgetDataFile;
    
    [Header("Intelligence")]
    [SerializeField] private TextAsset thrustDataFile;
    
    #endregion
    
    [Header("Mission Stages")]
    [SerializeField] private List<MissionStage> stages;
    
    [Header("Scene View Settings")]
    [SerializeField] private bool drawGizmos;
    
    [SerializeField] private Color beginningGizmosLineColor;
    [SerializeField] private Color endGizmosLineColor;
    [SerializeField, Range(1f, 100f)] private int gizmosLevelOfDetail;
    
    public MissionStage CurrentMissionStage { get; private set; }
    
    // Tracks the current link budget algorithm
    public enum LinkBudgetAlgorithm { None, Signal, Switch, Asset }
    public LinkBudgetAlgorithm PriorityAlgorithm { get; private set; }
    
    // Given data
    private string[][] _nominalTrajectoryData;
    private string[][] _offNominalTrajectoryData;
    private string[][] _nominalAntennaAvailabilityData;
    private string[][] _offNominalAntennaAvailabilityData;
    private string[][] _nominalLinkBudgetData;
    private string[][] _offNominalLinkBudgetData;
    private string[][] _thrustData;

    private string[][] _selectedTrajectoryData;
    private string[][] _selectedAntennaAvailabilityData;
    private string[][] _selectedLinkBudgetData;
    
    // Index tracking
    private const int SecondStageFireIndex = 120;
    
    private int _lowerIndex;
    private int _upperIndex;
    private float _progress; // This must always fall between 0.0 and 1.0.
    
    // Total distance traveled
    private float[] _nominalCumulativeDistances;
    private float[] _offNominalCumulativeDistances;
    private float[] _selectedCumulativeDistances;
    
    // Link budget prioritization
    public string PrioritizedAntenna { get; private set; }
    private List<Vector3> _positionVectorsForGizmos;
    
    // Actions
    public static event Action<DataLoadedEventArgs> DataLoaded;
    public static event Action<MissionStage> MissionStageUpdated;
    public static event Action<int> DataIndexUpdated;
    public static event Action<float> ProgressUpdated;
    public static event Action<string> ShowNotification;
    public static event Action<float> TotalDistanceTraveledUpdated;
    public static event Action<Vector3> CoordinatesUpdated;
    public static event Action<Vector3> VelocityUpdated;
    public static event Action<float> SpacecraftMassUpdated;
    
    
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
        CurrentMissionStage = stages[0];
        
        _nominalTrajectoryData = ReadDataFile(nominalTrajectoryDataFile);
        _offNominalTrajectoryData = ReadDataFile(offNominalTrajectoryDataFile);
        
        _nominalAntennaAvailabilityData = ReadDataFile(nominalAntennaAvailabilityDataFile);
        _offNominalAntennaAvailabilityData = ReadDataFile(offNominalAntennaAvailabilityDataFile);
        
        _nominalLinkBudgetData = ReadDataFile(nominalLinkBudgetDataFile);
        _offNominalLinkBudgetData = ReadDataFile(offNominalLinkBudgetDataFile);
        
        _thrustData = ReadDataFile(thrustDataFile);
        
        // The nominal trajectory is the default trajectory.
        _selectedTrajectoryData = _nominalTrajectoryData;
        _selectedAntennaAvailabilityData = _nominalAntennaAvailabilityData;
        _selectedLinkBudgetData = _nominalLinkBudgetData;
        
        // The cumulative distance arrays are assigned.
        _nominalCumulativeDistances = CalculateCumulativeDistances(_nominalTrajectoryData);
        _offNominalCumulativeDistances = CalculateCumulativeDistances(_offNominalTrajectoryData);
        _selectedCumulativeDistances = _nominalCumulativeDistances;
        
        DataLoaded?.Invoke(
            new DataLoadedEventArgs(
                _nominalTrajectoryData, 
                _offNominalTrajectoryData, 
                _nominalAntennaAvailabilityData,
                _offNominalAntennaAvailabilityData,
                _nominalLinkBudgetData,
                _offNominalLinkBudgetData,
                _thrustData,
                stages[0] // The first stage should start right after simulation begins.
            )
        );
    }

    private void Update()
    {
        UpdateCoordinates();
        UpdateVelocity();
        UpdateDistanceTraveled();
        UpdateSpacecraftMass();
        UpdateMissionStage(_lowerIndex);
        HandleNotifications(_lowerIndex);
        PrioritizedAntenna = GetHighestPriorityAntenna(_lowerIndex);
    }
    
    private void OnEnable()
    {
        IntelligenceManager.PathCalculated += ResetIndexTrackers;
        SimulationManager.ElapsedTimeUpdated += UpdateIndexTrackers;
        SpacecraftManager.SpacecraftStateUpdated += UpdateSelectedData;
        UIManager.PrioritizationAlgorithmSelected += SetPrioritizationAlgorithm;
    }
    
    private void OnDisable()
    {
        IntelligenceManager.PathCalculated -= ResetIndexTrackers;
        SimulationManager.ElapsedTimeUpdated -= UpdateIndexTrackers;
        SpacecraftManager.SpacecraftStateUpdated -= UpdateSelectedData;
        UIManager.PrioritizationAlgorithmSelected -= SetPrioritizationAlgorithm;
    }
    
    #endregion
    
    
    #region Data Processing
    
    /// <summary>
    /// Reads and processes a given CSV file.
    /// </summary>
    /// <param name="dataFile">The raw data file (CSV only)</param>
    /// <returns>The processed data file</returns>
    private static string[][] ReadDataFile(TextAsset dataFile)
    {
        string[][] data = CsvReader.ReadCsvFile(dataFile);
        return new ArraySegment<string[]>(data, 1, data.Length - 1).ToArray();
    }
    
    private void UpdateSelectedData(SpacecraftManager.SpacecraftState state)
    {
        bool isOnNominalTrajectory = state == SpacecraftManager.SpacecraftState.Nominal;
        
        _selectedTrajectoryData = isOnNominalTrajectory
            ? _nominalTrajectoryData
            : _offNominalTrajectoryData;
        _selectedAntennaAvailabilityData = isOnNominalTrajectory
            ? _nominalAntennaAvailabilityData
            : _offNominalAntennaAvailabilityData;
        _selectedLinkBudgetData = isOnNominalTrajectory
            ? _nominalLinkBudgetData
            : _offNominalLinkBudgetData;
        _selectedCumulativeDistances = isOnNominalTrajectory
            ? _nominalCumulativeDistances
            : _offNominalCumulativeDistances;
    }
    
    #endregion
    
    
    #region Index Tracking

    private void ResetIndexTrackers(string dataAsString="")
    {
        _lowerIndex = 0;
        _upperIndex = 1;
        _progress = 0.0f;
        
        DataIndexUpdated?.Invoke(_lowerIndex);
        ProgressUpdated?.Invoke(_progress);
    }
    
    private void UpdateIndexTrackers(float elapsedTime)
    {
        int[] indexBounds = GetIndexBoundsFromTime(_selectedTrajectoryData, elapsedTime);
        
        // If the bounds have changed, update the bounds.
        if (_lowerIndex != indexBounds[0] || _upperIndex != indexBounds[1])
        {
            _lowerIndex = indexBounds[0];
            _upperIndex = indexBounds[1];
            
            DataIndexUpdated?.Invoke(_lowerIndex);
        }
        
        // Update the progress between the lower and upper bounds.
        _progress = GetProgress(elapsedTime);
        ProgressUpdated?.Invoke(_progress);
    }
    
    private float GetProgress(float elapsedTime)
    {
        float lowerTime = float.Parse(_selectedTrajectoryData[_lowerIndex][0]);
        float upperTime = float.Parse(_selectedTrajectoryData[_upperIndex][0]);
        return Mathf.InverseLerp(lowerTime, upperTime, elapsedTime);
    }
    
    private static int[] GetIndexBoundsFromTime(string[][] data, float elapsedTime)
    {
        // Convert string times to floats once for retrieval efficiency.
        float[] times = data.Select(line => float.Parse(line[0])).ToArray();
        
        // `Array.BinarySearch` is used to achieve O(log n) performance. If `elapsedTime` is not found, the method
        // returns the negative index of its otherwise sorted position. The bitwise complement operator is used to get
        // the positive array index.
        int closestIndex = Array.BinarySearch(times, elapsedTime);
        closestIndex = closestIndex >= 0 ? closestIndex : ~closestIndex;
        
        // Determine the lower and upper index bounds.
        int lowerIndex = Mathf.Clamp(0, closestIndex - 1, times.Length - 1);
        int upperIndex = Mathf.Clamp(0, closestIndex, times.Length - 1);
        
        // Returns the index bounds as an integer array.
        return new[] { lowerIndex, upperIndex };
    }
    
    #endregion
    
    
    #region Timeline
    
    /// <summary>
    /// Updates the mission stage.
    /// </summary>
    /// <param name="dataIndex"></param>
    private void UpdateMissionStage(int dataIndex)
    {
        int index = stages.FindLastIndex(stage => dataIndex >= stage.startDataIndex);
        
        if (index == -1 || stages[index].Equals(CurrentMissionStage))
        {
            return;
        }
        
        CurrentMissionStage = stages[index];
        MissionStageUpdated?.Invoke(stages[index]);
    }
    
    #endregion
    
    #region Distance

    private void UpdateDistanceTraveled()
    {
        float totalDistanceTraveled = GetTotalDistanceTraveled(_selectedCumulativeDistances);
        TotalDistanceTraveledUpdated?.Invoke(totalDistanceTraveled);
    }
    
    private float[] CalculateCumulativeDistances(string[][] trajectoryData)
    {
        float[] cumulativeDistances = new float[trajectoryData.Length];
        
        float cumulativeDistance = 0.0f;
        Vector3 previousPosition = Vector3.zero;
        
        for (var index = 0; index < trajectoryData.Length; index++)
        {
            // Assign the cumulative total distance to the current item.
            cumulativeDistances[index] = cumulativeDistance;
            // Determine the current position at a given index.
            Vector3 currentPosition = new Vector3(
                float.Parse(trajectoryData[index][1]),
                float.Parse(trajectoryData[index][2]),
                float.Parse(trajectoryData[index][3])
            );
            // Add the difference vector's magnitude to the cumulative distance. 
            cumulativeDistance += (currentPosition - previousPosition).magnitude;
            // To prepare for the next iteration, set the previous position to be the current position.
            previousPosition = currentPosition;
        }
        
        return cumulativeDistances;
    }
    
    /// <summary>
    /// Finds the approximate distance traveled along a trajectory using linear interpolation. 
    /// </summary>
    /// <param name="cumulativeDistances">An array of cumulative distances along each timestamp of a trajectory</param>
    /// <returns>The approximate total distance traveled as a float</returns>
    private float GetTotalDistanceTraveled(float[] cumulativeDistances)
    {
        return Mathf.Lerp(cumulativeDistances[_lowerIndex], cumulativeDistances[_upperIndex], _progress);
    }
    
    #endregion
    
    
    #region Coordinates
    
    public Vector3 GetCoordinates(string[][] data)
    {
        Vector3 lowerCoordinate = new Vector3(
            float.Parse(data[_lowerIndex][1]),
            float.Parse(data[_lowerIndex][2]),
            float.Parse(data[_lowerIndex][3])
        );
        
        Vector3 upperCoordinate = new Vector3(
            float.Parse(data[_upperIndex][1]),
            float.Parse(data[_upperIndex][2]),
            float.Parse(data[_upperIndex][3])
        );
        
        return Vector3.Lerp(lowerCoordinate, upperCoordinate, _progress);
    }
    
    private void UpdateCoordinates()
    {
        Vector3 selectedTrajectoryCoordinates = GetCoordinates(_selectedTrajectoryData);
        CoordinatesUpdated?.Invoke(selectedTrajectoryCoordinates);
    }
    
    #endregion
    
    
    #region Velocity
    
    public Vector3 GetVelocity(string[][] data)
    {
        Vector3 lowerVelocity = new Vector3(
            float.Parse(data[_lowerIndex][4]),
            float.Parse(data[_lowerIndex][5]),
            float.Parse(data[_lowerIndex][6])
        );
        
        Vector3 upperVelocity = new Vector3(
            float.Parse(data[_upperIndex][4]),
            float.Parse(data[_upperIndex][5]),
            float.Parse(data[_upperIndex][6])
        );
        
        return Vector3.Lerp(lowerVelocity, upperVelocity, _progress);
    }
    
    private void UpdateVelocity()
    {
        Vector3 selectedVelocity = GetVelocity(_selectedTrajectoryData);
        VelocityUpdated?.Invoke(selectedVelocity);
    }
    
    #endregion
    
    
    #region Mass
    
    private void UpdateSpacecraftMass()
    {
        bool isMassRecorded = float.TryParse(_selectedTrajectoryData[_lowerIndex][7], out float spacecraftMass);
        
        if (!isMassRecorded)
        {
            throw new FormatException($"There is no spacecraft mass recorded on line {_lowerIndex}.");
        }
        
        SpacecraftMassUpdated?.Invoke(spacecraftMass);
    }
    
    #endregion
    
    
    #region Link Budget Prioritization
    
    private void SetPrioritizationAlgorithm(int algorithmIndex)
    {
        PriorityAlgorithm = (LinkBudgetAlgorithm)algorithmIndex;
    }

    /// <summary>
    /// Determines the highest priority antenna using link budget and future asset changes.
    /// </summary>
    /// <param name="dataIndex">The current data index</param>
    /// <returns>The name of the highest priority antenna</returns>
    private string GetHighestPriorityAntenna(int dataIndex)
    {
        string currentAntennaName = _selectedAntennaAvailabilityData[dataIndex][1];
        
        if (dataIndex <= 0)
        {
            return currentAntennaName;
        }
        
        string previousAntennaName = _selectedAntennaAvailabilityData[dataIndex - 1][1];

        if (previousAntennaName == currentAntennaName)
        {
            return previousAntennaName;
        }
        
        int maximumOffset = PriorityAlgorithm == LinkBudgetAlgorithm.Asset ? 60 : 20;
        int maximumIndex = _selectedAntennaAvailabilityData.Length - 1;
        
        for (int offset = 1; offset <= maximumOffset && dataIndex + offset <= maximumIndex; offset++)
        {
            string futureAntennaName;
            
            try
            {
                futureAntennaName = _selectedAntennaAvailabilityData[dataIndex + offset][1];
            }
            catch (IndexOutOfRangeException)
            {
                continue;
            }
            
            if (currentAntennaName != futureAntennaName)
            {
                return previousAntennaName;
            }
        }
        
        return currentAntennaName;
    }
    
    #endregion
    
    
    #region Notifications
    
    private void HandleNotifications(int dataIndex)
    {
        Dictionary<int, string> notificationMap = new Dictionary<int, string>
        {
            { SecondStageFireIndex, "Second Stage / Service Module Fired" }
        }; 
        
        if (notificationMap.TryGetValue(dataIndex, out string message))
        {
            // The second stage is the equivalent of the service module.
            ShowNotification?.Invoke(message);
        }
    }
    
    #endregion
    
    
    #region Gizmos
    
    private void OnValidate()
    {
        if (drawGizmos)
        {
            LoadGizmosPathData();
        }
    }

    /// <summary>
    /// Draws trajectory in the editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        int midpoint = _positionVectorsForGizmos.Count / 2;

        Gizmos.color = beginningGizmosLineColor;
        for (int i = 0; i < midpoint; i += gizmosLevelOfDetail)
        {
            Gizmos.DrawLine(_positionVectorsForGizmos[i], _positionVectorsForGizmos[i + gizmosLevelOfDetail]);
        }

        Gizmos.color = endGizmosLineColor;
        for (int i = midpoint; i < _positionVectorsForGizmos.Count - gizmosLevelOfDetail; i += gizmosLevelOfDetail)
        {
            Gizmos.DrawLine(_positionVectorsForGizmos[i], _positionVectorsForGizmos[i + gizmosLevelOfDetail]);
        }
    }
    
    [ContextMenu("Reload Gizmos Path Data")]
    private void LoadGizmosPathData()
    {
        _nominalTrajectoryData = ReadDataFile(offNominalTrajectoryDataFile);

        float trajectoryScale = 0.01f;

        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = _nominalTrajectoryData.Length;
        Vector3[] trajectoryPoints = new Vector3[numberOfPoints];
        for (int i = 0; i < _nominalTrajectoryData.Length; i++)
        {
            string[] point = _nominalTrajectoryData[i];

            try
            {
                Vector3 pointAsVector = new Vector3(
                    float.Parse(point[1]) * trajectoryScale,
                    float.Parse(point[2]) * trajectoryScale,
                    float.Parse(point[3]) * trajectoryScale);
                trajectoryPoints[i] = pointAsVector;
            }
            catch
            {
                Debug.LogWarning("Gizmos Line Rendering: no positional data on line " + i + "!");
            }
        }

        _positionVectorsForGizmos = trajectoryPoints.ToList();
    }
    #endregion
    
    
    // Reference
    private enum NominalDataStructure
    {
        Time,
        PositionX,
        PositionY,
        PositionZ,
        VelocityX,
        VelocityY,
        VelocityZ,
        SpacecraftMass
    }

    private enum OffNominalDataStructure
    {
        Time,
    }
}
