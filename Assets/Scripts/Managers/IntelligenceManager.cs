using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntelligenceManager : MonoBehaviour
{
    public static IntelligenceManager Instance { get; private set; }
    
    private List<string[]> _thrustData;
    private int _dataIndex;
    
    
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

    private void OnEnable()
    {
        DataManager.OnDataLoaded += LoadThrustData;
        SpacecraftManager.OnCurrentIndexUpdated += SetDataIndex;
        UIManager.OnBumpOffCoursePressed += OnBumpOffCourse;
    }

    private void OnDisable()
    {
        DataManager.OnDataLoaded -= LoadThrustData;
        SpacecraftManager.OnCurrentIndexUpdated -= SetDataIndex;
        UIManager.OnBumpOffCoursePressed -= OnBumpOffCourse;
    }
    
    #endregion
    
    
    # region Getters and Setters

    private void SetDataIndex(int index)
    {
        _dataIndex = index;
    }
    
    # endregion
    
    
    #region Thrust

    private void LoadThrustData(DataLoadedEventArgs data)
    {
        _thrustData = data.ThrustData;
    }

    private float GetSpacecraftMass(int dataIndex)
    {
        return float.Parse(_thrustData[dataIndex][7]);
    }

    private Vector3 GetForceVector(int dataIndex)
    {
        var forceX = float.Parse(_thrustData[dataIndex][^3]);
        var forceY = float.Parse(_thrustData[dataIndex][^2]);
        var forceZ = float.Parse(_thrustData[dataIndex][^1]);
        
        return new Vector3(forceX, forceY, forceZ);
    }

    private Vector3 GetAccelerationVector(int dataIndex)
    {
        var forceVector = GetForceVector(dataIndex);
        var mass = GetSpacecraftMass(dataIndex);
        
        var accelerationX = forceVector.x / mass;
        var accelerationY = forceVector.y / mass;
        var accelerationZ = forceVector.z / mass;
        
        return new Vector3(accelerationX, accelerationY, accelerationZ);
    }

    #endregion
    
    
    #region Bump Off Course

    private static Vector3 GetDistanceVector()
    {
        var origin = SpacecraftManager.Instance.NominalSpacecraftTransform.position;
        var destination = SpacecraftManager.Instance.OffNominalSpacecraftTransform.position;
        return destination - origin;
    }

    private Vector3 GetTimeVector(int dataIndex)
    {
        var distanceVector = GetDistanceVector();
        var accelerationVector = GetAccelerationVector(dataIndex);

        var timeX = Mathf.Sqrt(2.0f * distanceVector.x / accelerationVector.x);
        var timeY = Mathf.Sqrt(2.0f * distanceVector.y / accelerationVector.y);
        var timeZ = Mathf.Sqrt(2.0f * distanceVector.z / accelerationVector.z);
        
        return new Vector3(timeX, timeY, timeZ);
    }

    private float CalculateDeltaTime(int dataIndex)
    {
        return Vector3.Magnitude(GetTimeVector(dataIndex));
    }

    private float CalculateFlightTime(int dataIndex)
    {
        // Creates a buffer time constant to avoid errors where the raw flight time is too little.
        const float bufferTime = 20.0f;
        
        // Creates a vector where the starting endpoint is the chosen path's present position, and the ending endpoint
        // is the opposite path's present position.
        var toOtherPath = SpacecraftManager.Instance.NominalSpacecraftTransform.position 
                          - SpacecraftManager.Instance.OffNominalSpacecraftTransform.position;
        // Creates a vector where the starting endpoint is the opposite path's present position, and the ending endpoint
        // is the opposite path's future position.
        var toFuturePosition = Vector3.zero 
                               - SpacecraftManager.Instance.NominalSpacecraftTransform.position;
        
        var thetaInDegrees = Vector3.Angle(toOtherPath, toFuturePosition);
        var thetaInRadians = thetaInDegrees * Mathf.Deg2Rad;
        var deltaTimeSquared = Mathf.Pow(CalculateDeltaTime(dataIndex), 2);
        var rawFlightTime = Mathf.Sqrt(2 * deltaTimeSquared * (1 + Mathf.Cos(thetaInRadians)));
        
        // Returns the raw flight time with the additional buffer time.
        return rawFlightTime + bufferTime;
    }

    private void OnBumpOffCourse()
    {
        var origin = SpacecraftManager.Instance.OffNominalSpacecraftTransform.position;
        var destination = SpacecraftManager.Instance.OffNominalSpacecraftTransform.position;
        var flightTime = CalculateFlightTime(_dataIndex);
        var startTime = SpacecraftManager.Instance.EstimatedElapsedTime;
        
        HttpManager.Instance.RequestBumpOffCourseApi(origin, destination, flightTime, startTime);
    }
    
    #endregion
}