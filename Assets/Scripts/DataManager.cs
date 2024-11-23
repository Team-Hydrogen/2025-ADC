using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class DataManager : MonoBehaviour
{
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
    
    public UnityEvent<List<string[]>> onDataLoaded;
    public UnityEvent<string[]> onDataUpdated;
    
    private const string TrajectoryPointsFilepath = "Assets/Data/hsdata.csv";
    private static List<string[]> dataValues { get; set; }
    
    private int _currentDataIndex;
    private string[] _currentData;
    
    private float _currentUpdateSpeed;
    private float _timeSinceLastDataPoint = 0.0f;
    private float _timePerDataPoint;

    List<Vector3> positionVectorsForGizmos;
    
    // Start is called before the first frame update
    private void Start()
    {
        _currentDataIndex = 0;
        _currentUpdateSpeed = initialUpdateSpeed;
        _timePerDataPoint =  1.0f / _currentUpdateSpeed;

        dataValues = ReadData();

        onDataLoaded.Invoke(dataValues);
    }

    // Update is called once per frame
    private void Update()
    {
        // The tick variable updates.
        _timeSinceLastDataPoint += Time.deltaTime;
        
        if (_timeSinceLastDataPoint >= _timePerDataPoint && _currentDataIndex < dataValues.Count)
        {
            onDataUpdated.Invoke(dataValues[_currentDataIndex]);
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

    // DRAW TRAJECTORY IN EDITOR
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
            Gizmos.DrawLine(positionVectorsForGizmos[i], positionVectorsForGizmos[i + 1]);
        }

        Gizmos.color = endGizmosLineColor;
        for (int i = midpoint; i < positionVectorsForGizmos.Count - 1; i += gizmosLevelOfDetail)
        {
            Gizmos.DrawLine(positionVectorsForGizmos[i], positionVectorsForGizmos[i + 1]);
        }
    }

    private List<string[]> ReadData()
    {
        dataValues = CsvReader.ReadCsvFile(TrajectoryPointsFilepath);
        dataValues.RemoveAt(0);

        return dataValues;
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
        dataValues = ReadData();

        float trajectoryScale = 0.01f;

        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = dataValues.Count;
        Vector3[] trajectoryPoints = new Vector3[numberOfPoints];
        for (int i = 0; i < dataValues.Count; i++)
        {
            string[] point = dataValues[i];

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
