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
    public List<string[]> linkBudgetDataValues { get; private set; }
    
    public string currentPrioritizedAntenna {get; private set;}
    private List<Vector3> _positionVectorsForGizmos;

    private MissionStage currentMissionStage;
        
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
        _nominalTrajectoryDataValues = ReadDataFile(nominalTrajectoryDataFile);
        _offNominalTrajectoryDataValues = ReadDataFile(offNominalTrajectoryDataFile);
        _antennaAvailabilityDataValues = ReadDataFile(antennaAvailabilityDataFile);
        linkBudgetDataValues = ReadDataFile(linkBudgetDataFile);
        
        OnDataLoaded?.Invoke(
            new DataLoadedEventArgs(
                _nominalTrajectoryDataValues, 
                _offNominalTrajectoryDataValues, 
                _antennaAvailabilityDataValues,
                stages[0]) // First stage should start right after simulation begins
            );
        //OnMissionStageUpdated?.Invoke(stages[0]);
    }
    
    private void OnEnable()
    {
        SatelliteManager.OnCurrentIndexUpdated += UpdateDataManager;
    }
    
    private void OnDisable()
    {
        SatelliteManager.OnCurrentIndexUpdated -= UpdateDataManager;
    }
    
    private void UpdateDataManager(int index)
    {
        UpdateMissionStage(index);
        currentPrioritizedAntenna = PrioritizeLinkBudget(index);
    }
    
    /// <summary>
    /// Reads a given CSV file and processes it.
    /// </summary>
    /// <param name="dataFile">The data file</param>
    /// <returns></returns>
    private List<string[]> ReadDataFile(TextAsset dataFile)
    {
        var dataValues = CsvReader.ReadCsvFile(dataFile);
        dataValues.RemoveAt(0);
        return dataValues;
    }
    
    /// <summary>
    /// Finds the more prioritized antenna.
    /// </summary>
    /// <param name="index">The index of the current row</param>
    /// <returns>The name of the prioritized antenna</returns>
    private string PrioritizeLinkBudget(int index)
    {
        var currentSatelliteName = _antennaAvailabilityDataValues[index][1];

        if (index <= 0)
        {
            return currentSatelliteName;
        }
        
        var previousSatelliteName = _antennaAvailabilityDataValues[index - 1][1];
        
        if (previousSatelliteName == currentSatelliteName)
        {
            return previousSatelliteName;
        }
        
        for (var futureIndex = 1; futureIndex <= 60; futureIndex++)
        {
            var futureSatelliteName = _antennaAvailabilityDataValues[index + futureIndex][1];
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
        
        if (index != -1)
        {
            if (!stages[index].Equals(currentMissionStage))
            {
                currentMissionStage = stages[index];
                OnMissionStageUpdated?.Invoke(stages[index]);
            }
        }
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
