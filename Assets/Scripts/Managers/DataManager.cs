using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    [Header("Data Files")]
    [SerializeField] private TextAsset nominalTrajectoryDataFile;
    [SerializeField] private TextAsset offnominalTrajectoryDataFile;
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

    public static DataManager Instance { get; private set; }

    private List<string[]> nominalTrajectoryDataValues;
    private List<string[]> offnominalTrajectoryDataValues;
    private List<string[]> linkBudgetDataValues;

    private string _currentPrioritizedAntenna;
    List<Vector3> positionVectorsForGizmos;
    
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
        nominalTrajectoryDataValues = ReadNominalTrajectoryData();
        offnominalTrajectoryDataValues = ReadOffnominalTrajectoryData();
        linkBudgetDataValues = ReadLinkBudgetData();

        OnDataLoaded?.Invoke(
            new DataLoadedEventArgs(nominalTrajectoryDataValues, offnominalTrajectoryDataValues, linkBudgetDataValues));
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

    /// <summary>
    /// Reads the nominal trajectory data.
    /// </summary>
    /// <returns>A list of String arrays representing the CSV file</returns>
    private List<string[]> ReadNominalTrajectoryData()
    {
        nominalTrajectoryDataValues = CsvReader.ReadCsvFile(nominalTrajectoryDataFile);
        nominalTrajectoryDataValues.RemoveAt(0);
        return nominalTrajectoryDataValues;
    }
    
    /// <summary>
    /// Reads the offnominal trajectory data.
    /// </summary>
    /// <returns>A list of String arrays representing the CSV file</returns>
    private List<string[]> ReadOffnominalTrajectoryData()
    {
        offnominalTrajectoryDataValues = CsvReader.ReadCsvFile(offnominalTrajectoryDataFile);
        offnominalTrajectoryDataValues.RemoveAt(0);
        return offnominalTrajectoryDataValues;
    }

    /// <summary>
    /// Reads the link budget data.
    /// </summary>
    /// <returns>A list of String arrays representing the CSV file</returns>
    private List<string[]> ReadLinkBudgetData()
    {
        linkBudgetDataValues = CsvReader.ReadCsvFile(linkBudgetDataFile);
        linkBudgetDataValues.RemoveAt(0);
        return linkBudgetDataValues;
    }
    
    private string PrioritizeLinkBudget(int index)
    {
        var currentSatelliteName = linkBudgetDataValues[index][1];
        
        if (index <= 0)
        {
            return currentSatelliteName;
        }
        
        var previousSatelliteName = linkBudgetDataValues[index - 1][1];

        if (previousSatelliteName == currentSatelliteName)
        {
            return previousSatelliteName;
        }

        for (var futureIndex = 1; futureIndex <= 60; futureIndex++)
        {
            var futureSatelliteName = linkBudgetDataValues[index + futureIndex][1];
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

        int midpoint = positionVectorsForGizmos.Count / 2;

        Gizmos.color = beginningGizmosLineColor;
        for (int i = 0; i < midpoint; i += gizmosLevelOfDetail)
        {
            Gizmos.DrawLine(positionVectorsForGizmos[i], positionVectorsForGizmos[i + gizmosLevelOfDetail]);
        }

        Gizmos.color = endGizmosLineColor;
        for (int i = midpoint; i < positionVectorsForGizmos.Count - gizmosLevelOfDetail; i += gizmosLevelOfDetail)
        {
            Gizmos.DrawLine(positionVectorsForGizmos[i], positionVectorsForGizmos[i + gizmosLevelOfDetail]);
        }
    }

    [ContextMenu("Reload Gizmos Path Data")]
    private void LoadGizmosPathData()
    {
        nominalTrajectoryDataValues = ReadOffnominalTrajectoryData();

        float trajectoryScale = 0.01f;

        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = nominalTrajectoryDataValues.Count;
        Vector3[] trajectoryPoints = new Vector3[numberOfPoints];
        for (int i = 0; i < nominalTrajectoryDataValues.Count; i++)
        {
            string[] point = nominalTrajectoryDataValues[i];

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

        positionVectorsForGizmos = trajectoryPoints.ToList();
    }
    #endregion
}
