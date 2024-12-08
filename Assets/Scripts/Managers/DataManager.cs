using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class DataManager : MonoBehaviour
{
    [Header("Data Files")] [SerializeField]
    private TextAsset nominalTrajectoryDataFile;

    [SerializeField] private TextAsset offNominalTrajectoryDataFile;
    [SerializeField] private TextAsset antennaAvailabilityDataFile;
    [SerializeField] private TextAsset linkBudgetDataFile;

    [Header("Mission Stages")] [SerializeField]
    private List<MissionStage> stages;

    [Header("Scene View Settings")] [SerializeField]
    private bool drawGizmos;

    [SerializeField] private Color beginningGizmosLineColor;
    [SerializeField] private Color endGizmosLineColor;
    [SerializeField, Range(1f, 100f)] private int gizmosLevelOfDetail;

    public static event Action<DataLoadedEventArgs> OnDataLoaded;
    public static event Action<MissionStage> OnMissionStageUpdated;

    public static DataManager Instance { get; private set; }

    private List<string[]> _nominalTrajectoryDataValues;
    private List<string[]> _offNominalTrajectoryDataValues;
    private List<string[]> _antennaAvailabilityDataValues;
    public List<string[]> linkBudgetDataValues { get; private set; }

private string _currentPrioritizedAntenna;
    List<Vector3> _positionVectorsForGizmos;
    
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
        _nominalTrajectoryDataValues = ReadDataFile(nominalTrajectoryDataFile);
        _offNominalTrajectoryDataValues = ReadDataFile(offNominalTrajectoryDataFile);
        _antennaAvailabilityDataValues = ReadDataFile(antennaAvailabilityDataFile);
        linkBudgetDataValues = ReadDataFile(linkBudgetDataFile);
        
        OnDataLoaded?.Invoke(
            new DataLoadedEventArgs(
                _nominalTrajectoryDataValues, _offNominalTrajectoryDataValues, _antennaAvailabilityDataValues));
        OnMissionStageUpdated?.Invoke(stages[0]);
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
        _currentPrioritizedAntenna = PrioritizeLinkBudget(index);
    }

    private List<string[]> ReadDataFile(TextAsset dataFile)
    {
        var dataValues = CsvReader.ReadCsvFile(dataFile);
        dataValues.RemoveAt(0);
        return dataValues;
    }

    // /// <summary>
    // /// Reads the nominal trajectory data.
    // /// </summary>
    // /// <returns>A list of String arrays representing the CSV file</returns>
    // private List<string[]> ReadNominalTrajectoryData()
    // {
    //     _nominalTrajectoryDataValues = CsvReader.ReadCsvFile(nominalTrajectoryDataFile);
    //     _nominalTrajectoryDataValues.RemoveAt(0);
    //     return _nominalTrajectoryDataValues;
    // }
    //
    // /// <summary>
    // /// Reads the offnominal trajectory data.
    // /// </summary>
    // /// <returns>A list of String arrays representing the CSV file</returns>
    // private List<string[]> ReadOffNominalTrajectoryData()
    // {
    //     _offNominalTrajectoryDataValues = CsvReader.ReadCsvFile(offNominalTrajectoryDataFile);
    //     _offNominalTrajectoryDataValues.RemoveAt(0);
    //     return _offNominalTrajectoryDataValues;
    // }
    //
    // /// <summary>
    // /// Reads the link budget data.
    // /// </summary>
    // /// <returns>A list of String arrays representing the CSV file</returns>
    // private List<string[]> ReadAntennaAvailabilityData()
    // {
    //     _antennaAvailabilityDataValues = CsvReader.ReadCsvFile(antennaAvailabilityDataFile);
    //     _antennaAvailabilityDataValues.RemoveAt(0);
    //     return _antennaAvailabilityDataValues;
    // }
    //
    // private List<string[]> ReadLinkBudgetData()
    // {
    //     _linkBudgetDataValues = CsvReader.ReadCsvFile(linkBudgetDataFile);
    //     _linkBudgetDataValues.RemoveAt(0);
    //     return _linkBudgetDataValues;
    // }
    
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
        int index = stages.FindLastIndex(stage => dataIndex >= stage.startDataIndex);

        if (index != -1)
        {
            OnMissionStageUpdated?.Invoke(stages[index]);
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
