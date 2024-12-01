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

    public static event Action<DataLoadedEventArgs> OnDataLoaded;
    public static event Action<int> OnDataUpdated;

    public static SimulationManager Instance { get; private set; }

    private List<string[]> nominalTrajectoryDataValues;
    private List<string[]> offnominalTrajectoryDataValues;
    private List<string[]> linkBudgetDataValues;

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

    //public void SkipBackward(float timeInSeconds)
    //{
    //    _currentDataIndex = Mathf.Max(0, _currentDataIndex - DataPointsBackward);
    //}

    //public void SkipForward(float timeInSeconds)
    //{
    //    _currentDataIndex = Mathf.Min(_currentDataIndex + DataPointsForward, nominalTrajectoryDataValues.Count - 1);
    //}

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
