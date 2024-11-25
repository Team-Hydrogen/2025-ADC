using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class DataManager : MonoBehaviour
{
    [Header("Data Files")]
    [SerializeField] private TextAsset trajectoryDataFile;
    [SerializeField] private TextAsset linkBudgetDataFile;
    
    [Header("Scene View Settings")]
    [SerializeField] private bool drawGizmos;
    [SerializeField] private Color beginningGizmosLineColor;
    [SerializeField] private Color endGizmosLineColor;
    [SerializeField, Range(1f, 100f)] private int gizmosLevelOfDetail;
    
    [Header("Settings")]
    [Tooltip("How fast the data manager updates in data points per second initially"), Range(0, 400)]
    [SerializeField] private int initialUpdateSpeed;
    [Tooltip("The maximum speed the data manager updates in data points per second"), Range(0, 400)]
    [SerializeField] private int maximumUpdateSpeed;
    [Tooltip("The acceleration of the speed."), Range(10, 50)]
    [SerializeField] private int updateSpeedAcceleration;

    public static event Action<List<string[]>> OnDataLoaded;
    public static event Action<int> OnDataUpdated;

    [HideInInspector] public static DataManager Instance { get; private set; }
    
    public static List<string[]> trajectoryDataValues { get; private set; }
    public static List<string[]> linkBudgetDataValues { get; private set; }
    
    private int _currentDataIndex;
    private string[] _currentData;
    
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
        _currentDataIndex = 0;
        _currentUpdateSpeed = initialUpdateSpeed;
        _timePerDataPoint =  1.0f / _currentUpdateSpeed;

        trajectoryDataValues = ReadTrajectoryData();
        linkBudgetDataValues = ReadLinkBudgetData();

        OnDataLoaded?.Invoke(trajectoryDataValues);
    }

    private void Update()
    {
        // The tick variable updates.
        _timeSinceLastDataPoint += Time.deltaTime;
        
        if (_timeSinceLastDataPoint >= _timePerDataPoint && _currentDataIndex < trajectoryDataValues.Count)
        {
            OnDataUpdated?.Invoke(_currentDataIndex);
            _currentDataIndex++;
            _timeSinceLastDataPoint -= _timePerDataPoint;
        }
        
        // The current update speed increases with acceleration.
        _currentUpdateSpeed += updateSpeedAcceleration * Time.deltaTime;
        if (_currentUpdateSpeed > maximumUpdateSpeed)
        {
            _currentUpdateSpeed = maximumUpdateSpeed;
        }
        _timePerDataPoint =  1.0f / _currentUpdateSpeed;
    }

    public void SkipBackward(float timeInSeconds)
    {

    }

    public void SkipForward(float timeInSeconds)
    {

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
    /// Reads the trajectory data.
    /// </summary>
    /// <returns>A list of String arrays representing the CSV file</returns>
    private List<string[]> ReadTrajectoryData()
    {
        trajectoryDataValues = CsvReader.ReadCsvFile(trajectoryDataFile);
        trajectoryDataValues.RemoveAt(0);
        return trajectoryDataValues;
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
        trajectoryDataValues = ReadTrajectoryData();

        float trajectoryScale = 0.01f;

        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = trajectoryDataValues.Count;
        Vector3[] trajectoryPoints = new Vector3[numberOfPoints];
        for (int i = 0; i < trajectoryDataValues.Count; i++)
        {
            string[] point = trajectoryDataValues[i];

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
