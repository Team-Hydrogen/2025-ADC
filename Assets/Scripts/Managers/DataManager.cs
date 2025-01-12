using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager instance { get; private set; }
        
    [Header("Data Files")]
    [SerializeField] private TextAsset nominalTrajectoryDataFile;
    [SerializeField] private TextAsset offNominalTrajectoryDataFile;
    [SerializeField] private TextAsset antennaAvailabilityDataFile;
    [SerializeField] private TextAsset linkBudgetDataFile;
    [SerializeField] private TextAsset offnominalLinkBudgetDataFile;
    [SerializeField] private TextAsset thrustDataFile;
    
    [Header("Mission Stages")]
    [SerializeField] private List<MissionStage> stages;
    
    [Header("Scene View Settings")]
    [SerializeField] private bool drawGizmos;
    
    [SerializeField] private Color beginningGizmosLineColor;
    [SerializeField] private Color endGizmosLineColor;
    [SerializeField, Range(1f, 100f)] private int gizmosLevelOfDetail;
    
    public static event Action<DataLoadedEventArgs> OnDataLoaded;
    public static event Action<MissionStage> OnMissionStageUpdated;
    
    private List<string[]> _nominalTrajectoryDataValues;
    private List<string[]> _offNominalTrajectoryDataValues;
    private List<string[]> _antennaAvailabilityDataValues;
    private List<string[]> _thrustDataValues;
    public List<string[]> _linkBudgetDataValues { get; private set; }
    public List<string[]> _offnominalLinkBudgetDataValues { get; private set; }
    
    public string currentPrioritizedAntenna { get; private set; }
    private List<Vector3> _positionVectorsForGizmos;

    public MissionStage currentMissionStage { get; private set; }

    private SatelliteManager.SatelliteState _satelliteState;
    
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
        currentMissionStage = stages[0];
        
        _nominalTrajectoryDataValues = ReadDataFile(nominalTrajectoryDataFile);
        _offNominalTrajectoryDataValues = ReadDataFile(offNominalTrajectoryDataFile);
        _antennaAvailabilityDataValues = ReadDataFile(antennaAvailabilityDataFile);
        _linkBudgetDataValues = ReadDataFile(linkBudgetDataFile);
        _offnominalLinkBudgetDataValues = ReadDataFile(offnominalLinkBudgetDataFile);
        _thrustDataValues = ReadDataFile(thrustDataFile);
        
        OnDataLoaded?.Invoke(
            new DataLoadedEventArgs(
                _nominalTrajectoryDataValues, 
                _offNominalTrajectoryDataValues, 
                _antennaAvailabilityDataValues,
                _linkBudgetDataValues,
                _offnominalLinkBudgetDataValues,
                _thrustDataValues,
                stages[0]) // First stage should start right after simulation begins
            );
    }
    
    private void OnEnable()
    {
        SatelliteManager.OnCurrentIndexUpdated += UpdateDataManager;
        SatelliteManager.OnSatelliteStateUpdated += SatelliteStateUpdated;
    }
    
    private void OnDisable()
    {
        SatelliteManager.OnCurrentIndexUpdated -= UpdateDataManager;
        SatelliteManager.OnSatelliteStateUpdated -= SatelliteStateUpdated;
    }
    #endregion
    
    private void UpdateDataManager(int index)
    {
        UpdateMissionStage(index);
        currentPrioritizedAntenna = GetHighestPriorityAntenna(index);
    }
    
    /// <summary>
    /// Reads and processes a given CSV file.
    /// </summary>
    /// <param name="dataFile">The raw data file (CSV only)</param>
    /// <returns>The processed data file</returns>
    private List<string[]> ReadDataFile(TextAsset dataFile)
    {
        var dataValues = CsvReader.ReadCsvFile(dataFile);
        dataValues.RemoveAt(0); // The first row of headers is removed.
        return dataValues;
    }

    private void SatelliteStateUpdated(SatelliteManager.SatelliteState state)
    {
        _satelliteState = state;
    }

    /// <summary>
    /// Determines the highest priority antenna using link budget and future asset changes.
    /// </summary>
    /// <param name="index">The current data index</param>
    /// <returns>The name of the highest priority antenna</returns>
    private string GetHighestPriorityAntenna(int index)
    {
        var currentSatelliteName = _satelliteState == SatelliteManager.SatelliteState.Nominal
            ? _antennaAvailabilityDataValues[index][1]
            : _offNominalTrajectoryDataValues[index][1];
        
        if (index <= 0)
        {
            return currentSatelliteName;
        }
        
        var previousSatelliteName = _satelliteState == SatelliteManager.SatelliteState.Nominal
            ? _antennaAvailabilityDataValues[index - 1][1]
            : _offNominalTrajectoryDataValues[index - 1][1];

        if (previousSatelliteName == currentSatelliteName)
        {
            return previousSatelliteName;
        }
        
        for (var futureIndex = 1; futureIndex <= 20; futureIndex++)
        {
            var futureSatelliteName =_satelliteState == SatelliteManager.SatelliteState.Nominal
                ? _antennaAvailabilityDataValues[index + futureIndex][1]
                : _offNominalTrajectoryDataValues[index + futureIndex][1];
            if (currentSatelliteName != futureSatelliteName)
            {
                return previousSatelliteName;
            }
            futureIndex++;
        }
        
        return currentSatelliteName;
    }
    
    /// <summary>
    /// Updates the mission stage.
    /// </summary>
    /// <param name="dataIndex"></param>
    private void UpdateMissionStage(int dataIndex)
    {
        var index = stages.FindLastIndex(stage => dataIndex >= stage.startDataIndex);
        
        if (index == -1 || stages[index].Equals(currentMissionStage))
        {
            return;
        }
        
        currentMissionStage = stages[index];
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
}
