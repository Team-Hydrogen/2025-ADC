using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DataManager : MonoBehaviour
{
    [Tooltip("How fast the data manager updates in data points per second"), Range(1, 400)]
    [SerializeField] private int updateSpeed;
    
    [Header("Celestial Bodies")]
    [SerializeField] private GameObject earth;
    [SerializeField] private GameObject moon;
    
    public UnityEvent<List<string[]>> onDataLoaded;
    public UnityEvent<string[]> onDataUpdated;
    
    private const string TrajectoryPointsFilepath = "Assets/Data/hsdata.csv";
    private static List<string[]> dataValues { get; set; }
    
    private int _currentDataIndex;
    private string[] _currentData;
    
    private float _timeSinceLastDataPoint = 0.0f;
    private float _timePerDataPoint;
    
    // Start is called before the first frame update
    private void Start()
    {
        _currentDataIndex = 0;
        _timePerDataPoint =  1.0f / updateSpeed;
        
        dataValues = CsvReader.ReadCsvFile(TrajectoryPointsFilepath);
        dataValues.RemoveAt(0);
        
        onDataLoaded.Invoke(dataValues);
    }

    // Update is called once per frame
    private void Update()
    {
        _timeSinceLastDataPoint += Time.deltaTime;
        if (_timeSinceLastDataPoint >= _timePerDataPoint && _currentDataIndex < dataValues.Count)
        {
            onDataUpdated.Invoke(dataValues[_currentDataIndex]);
            _currentDataIndex++;
            _timeSinceLastDataPoint -= _timePerDataPoint;
        }
    }
}
