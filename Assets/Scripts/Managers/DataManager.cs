using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    
    // Inspector
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
    
    [Header("Mission Stages")]
    [SerializeField] private List<MissionStage> stages;
    
    [Header("Scene View Settings")]
    [SerializeField] private bool drawGizmos;
    
    [SerializeField] private Color beginningGizmosLineColor;
    [SerializeField] private Color endGizmosLineColor;
    [SerializeField, Range(1f, 100f)] private int gizmosLevelOfDetail;
    
    // State management
    private SpacecraftManager.SpacecraftState _spacecraftState;
    public MissionStage CurrentMissionStage { get; private set; }
    public LinkBudgetAlgorithm PriorityAlgorithm { get; private set; }
    
    // Given data
    private List<string[]> _nominalTrajectoryDataValues;
    private List<string[]> _offNominalTrajectoryDataValues;
    private List<string[]> _nominalAntennaAvailabilityDataValues;
    private List<string[]> _offNominalAntennaAvailabilityDataValues;
    private List<string[]> _thrustDataValues;
    private List<string[]> _nominalLinkBudgetDataValues;
    private List<string[]> _offNominalLinkBudgetDataValues;
    
    public string PrioritizedAntenna { get; private set; }
    private List<Vector3> _positionVectorsForGizmos;
    
    // Actions
    public static event Action<DataLoadedEventArgs> OnDataLoaded;
    public static event Action<MissionStage> OnMissionStageUpdated;
    
    
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
        
        _nominalTrajectoryDataValues = ReadDataFile(nominalTrajectoryDataFile);
        _offNominalTrajectoryDataValues = ReadDataFile(offNominalTrajectoryDataFile);
        
        _nominalAntennaAvailabilityDataValues = ReadDataFile(nominalAntennaAvailabilityDataFile);
        _offNominalAntennaAvailabilityDataValues = ReadDataFile(offNominalAntennaAvailabilityDataFile);
        
        _nominalLinkBudgetDataValues = ReadDataFile(nominalLinkBudgetDataFile);
        _offNominalLinkBudgetDataValues = ReadDataFile(offNominalLinkBudgetDataFile);
        
        _thrustDataValues = ReadDataFile(thrustDataFile);
        
        OnDataLoaded?.Invoke(
            new DataLoadedEventArgs(
                _nominalTrajectoryDataValues, 
                _offNominalTrajectoryDataValues, 
                _nominalAntennaAvailabilityDataValues,
                _offNominalAntennaAvailabilityDataValues,
                _nominalLinkBudgetDataValues,
                _offNominalLinkBudgetDataValues,
                _thrustDataValues,
                stages[0]) // First stage should start right after simulation begins
            );
    }
    
    private void OnEnable()
    {
        SpacecraftManager.OnCurrentIndexUpdated += UpdateDataManager;
        SpacecraftManager.OnSpacecraftStateUpdated += UpdateSpacecraftState;
        UIManager.OnPrioritizationChanged += SetPriorityAlgorithm;
    }
    
    private void OnDisable()
    {
        SpacecraftManager.OnCurrentIndexUpdated -= UpdateDataManager;
        SpacecraftManager.OnSpacecraftStateUpdated -= UpdateSpacecraftState;
        UIManager.OnPrioritizationChanged -= SetPriorityAlgorithm;
    }
    
    #endregion
    
    private void UpdateDataManager(int index)
    {
        UpdateMissionStage(index);
        PrioritizedAntenna = GetHighestPriorityAntenna(index);
    }
    
    /// <summary>
    /// Reads and processes a given CSV file.
    /// </summary>
    /// <param name="dataFile">The raw data file (CSV only)</param>
    /// <returns>The processed data file</returns>
    private static List<string[]> ReadDataFile(TextAsset dataFile)
    {
        var dataValues = CsvReader.ReadCsvFile(dataFile);
        dataValues.RemoveAt(0); // The first row of headers is removed.
        //dataValues.RemoveAt(0); // The first row of data (time=0) is removed.
        return dataValues;
    }

    private void UpdateSpacecraftState(SpacecraftManager.SpacecraftState state)
    {
        _spacecraftState = state;
    }

    private void SetPriorityAlgorithm(int algorithmIndex)
    {
        PriorityAlgorithm = (LinkBudgetAlgorithm)algorithmIndex;
    }

    /// <summary>
    /// Determines the highest priority antenna using link budget and future asset changes.
    /// </summary>
    /// <param name="index">The current data index</param>
    /// <returns>The name of the highest priority antenna</returns>
    private string GetHighestPriorityAntenna(int index)
    {
        var currentAntennaName = _spacecraftState == SpacecraftManager.SpacecraftState.Nominal
            ? _nominalAntennaAvailabilityDataValues[index][1]
            : _offNominalAntennaAvailabilityDataValues[index][1];
        
        if (index <= 0)
        {
            return currentAntennaName;
        }
        
        var previousAntennaName = _spacecraftState == SpacecraftManager.SpacecraftState.Nominal
            ? _nominalAntennaAvailabilityDataValues[index - 1][1]
            : _offNominalAntennaAvailabilityDataValues[index - 1][1];

        if (previousAntennaName == currentAntennaName)
        {
            return previousAntennaName;
        }
        
        int maximumOffset = PriorityAlgorithm == LinkBudgetAlgorithm.Asset ? 60 : 20;
        int maximumIndex = _spacecraftState == SpacecraftManager.SpacecraftState.Nominal
            ? _nominalAntennaAvailabilityDataValues.Count - 1
            : _offNominalAntennaAvailabilityDataValues.Count - 1;
        
        for (int offset = 1; offset <= maximumOffset && index + offset <= maximumIndex; offset++)
        {
            string futureAntennaName;
            
            try
            {
                futureAntennaName = _spacecraftState == SpacecraftManager.SpacecraftState.Nominal
                    ? _nominalAntennaAvailabilityDataValues[index + offset][1]
                    : _offNominalAntennaAvailabilityDataValues[index + offset][1];
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
    
    /// <summary>
    /// Updates the mission stage.
    /// </summary>
    /// <param name="dataIndex"></param>
    private void UpdateMissionStage(int dataIndex)
    {
        var index = stages.FindLastIndex(stage => dataIndex >= stage.startDataIndex);
        
        if (index == -1 || stages[index].Equals(CurrentMissionStage))
        {
            return;
        }
        
        CurrentMissionStage = stages[index];
        OnMissionStageUpdated?.Invoke(stages[index]);
    }
    
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
        _nominalTrajectoryDataValues = ReadDataFile(offNominalTrajectoryDataFile);

        float trajectoryScale = 0.01f;

        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = _nominalTrajectoryDataValues.Count;
        Vector3[] trajectoryPoints = new Vector3[numberOfPoints];
        for (int i = 0; i < _nominalTrajectoryDataValues.Count; i++)
        {
            string[] point = _nominalTrajectoryDataValues[i];

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
    
    public enum LinkBudgetAlgorithm
    {
        None,
        Signal,
        Switch,
        Asset
    }
    
    public enum NominalDataStructure
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
}
