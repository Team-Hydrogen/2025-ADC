using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    [Header("Data Files")]
    [SerializeField] private TextAsset nominalTrajectoryDataFile;
    [SerializeField] private TextAsset offnominalTrajectoryDataFile;
    [SerializeField] private TextAsset linkBudgetDataFile;

    [Header("Scene View Settings")]
    [SerializeField] private bool drawGizmos;
    [SerializeField] private Color beginningGizmosLineColor;
    [SerializeField] private Color endGizmosLineColor;
    [SerializeField, Range(1f, 100f)] private int gizmosLevelOfDetail;

    [Header("Stages")]
    [SerializeField] private List<MissionStage> stages;

    public static event Action<DataLoadedEventArgs> OnDataLoaded;
    public static event Action<int> OnDataUpdated;
    public static event Action<MissionStage> OnMissionStageUpdated;

    public static SimulationManager Instance { get; private set; }
    
    public List<string[]> nominalTrajectoryDataValues { get; private set; }
    public List<string[]> offnominalTrajectoryDataValues { get; private set; }
    public List<string[]> linkBudgetDataValues { get; private set; }
    
    private int _currentDataIndex;
    private string[] _currentData;
    private MissionStage _currentMissionStage;
    private const int DataPointsForward = 500;
    private const int DataPointsBackward = 500;
    
    private float _currentUpdateSpeed;
    private float _timeSinceLastDataPoint = 0.0f;
    private float _timePerDataPoint;

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
        //_currentDataIndex = 0;
        //_currentUpdateSpeed = initialUpdateSpeed;
        //_timePerDataPoint =  1.0f / _currentUpdateSpeed;

        nominalTrajectoryDataValues = ReadNominalTrajectoryData();
        offnominalTrajectoryDataValues = ReadOffnominalTrajectoryData();
        linkBudgetDataValues = ReadLinkBudgetData();

        OnDataLoaded?.Invoke(new DataLoadedEventArgs(nominalTrajectoryDataValues, offnominalTrajectoryDataValues, linkBudgetDataValues));
    }

    private void Update()
    {
        //// The tick variable updates.
        //_timeSinceLastDataPoint += Time.deltaTime;
        
        //if (_timeSinceLastDataPoint >= _timePerDataPoint && _currentDataIndex < nominalTrajectoryDataValues.Count)
        //{
        //    OnDataUpdated?.Invoke(_currentDataIndex);
        //    _currentDataIndex++;
        //    _timeSinceLastDataPoint -= _timePerDataPoint;
        //}
        
        //// The current update speed increases with acceleration.
        //_currentUpdateSpeed += updateSpeedAcceleration * Time.deltaTime;
        //if (_currentUpdateSpeed > maximumUpdateSpeed)
        //{
        //    _currentUpdateSpeed = maximumUpdateSpeed;
        //}
        //_timePerDataPoint =  1.0f / _currentUpdateSpeed;
        
        //if (!_currentMissionStage.Equals(GetCurrentMissionStage()))
        //{
        //    _currentMissionStage = GetCurrentMissionStage();
        //    OnMissionStageUpdated?.Invoke(_currentMissionStage);
        //}
    }

    public void SkipBackward(float timeInSeconds)
    {
        _currentDataIndex = Mathf.Max(0, _currentDataIndex - DataPointsBackward);
    }

    public void SkipForward(float timeInSeconds)
    {
        _currentDataIndex = Mathf.Min(_currentDataIndex + DataPointsForward, nominalTrajectoryDataValues.Count - 1);
    }

    private MissionStage GetCurrentMissionStage()
    {
        MissionStage latestStage = new MissionStage(_currentDataIndex, MissionStage.StageTypes.None);

        for (int i = 0; i < stages.Count; i++)
        {
            if (_currentDataIndex >= stages[i].startDataIndex)
            {
                latestStage = stages[i];
            }

            else if (_currentDataIndex < stages[i].startDataIndex)
            {
                return latestStage;
            }
        }

        return latestStage;
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
    
    private List<string[]> ReadLinkBudgetData()
    {
        linkBudgetDataValues = CsvReader.ReadCsvFile(linkBudgetDataFile);
        linkBudgetDataValues.RemoveAt(0);
        return linkBudgetDataValues;
    }

    private void OnValidate()
    {
        if (drawGizmos)
        {
            LoadGizmosPathData();
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
}
