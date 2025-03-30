using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntelligenceManager : MonoBehaviour
{
    public static IntelligenceManager Instance { get; private set; }
    
    private List<string[]> _thrustData;
    private int _dataIndex;
    private float _currentTime;
    
    
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
        SpacecraftManager.OnUpdateTime += SetCurrentTime;
        UIManager.OnTransitionPathPressed += OnTransitionPath;
    }

    private void OnDisable()
    {
        DataManager.OnDataLoaded -= LoadThrustData;
        SpacecraftManager.OnCurrentIndexUpdated -= SetDataIndex;
        SpacecraftManager.OnUpdateTime -= SetCurrentTime;
        UIManager.OnTransitionPathPressed -= OnTransitionPath;
    }
    
    #endregion
    
    
    # region Getters and Setters

    private void SetCurrentTime(float time)
    {
        _currentTime = time;
    }
    
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

    private float CalculateFlightTime(Vector3 origin, Vector3 oppositePathPosition, Vector3 destination, float deltaTime)
    {
        // Creates a buffer time constant to avoid errors where the raw flight time is too little.
        const float bufferTime = 20.0f;
        
        var deltaTimeSquared = Mathf.Pow(deltaTime, 2);
        
        // Creates a vector where the starting endpoint is the chosen path's present position, and the ending endpoint
        // is the opposite path's present position.
        var toOtherPath = SpacecraftManager.Instance.NominalSpacecraftTransform.position 
                          - SpacecraftManager.Instance.OffNominalSpacecraftTransform.position;
        // Creates a vector where the starting endpoint is the opposite path's present position, and the ending endpoint
        // is the opposite path's future position.
        var toFuturePosition = SpacecraftManager.Instance.GetNominalPositionFromTime(_currentTime + deltaTime) 
                               - SpacecraftManager.Instance.NominalSpacecraftTransform.position;
        
        var thetaInDegrees = Vector3.Angle(toOtherPath, toFuturePosition);
        var thetaInRadians = thetaInDegrees * Mathf.Deg2Rad;
        
        var rawFlightTime = Mathf.Sqrt(2 * deltaTimeSquared * (1 + Mathf.Cos(thetaInRadians)));
        
        // Returns the raw flight time with the additional buffer time.
        return rawFlightTime + bufferTime;
    }

    private void OnTransitionPath()
    {
        var deltaTime = CalculateDeltaTime(_dataIndex);
        
        Vector3 originPosition = SpacecraftManager.Instance.OffNominalSpacecraftTransform.position;
        Vector3 originVelocity = SpacecraftManager.Instance.OffNominalSpacecraftTransform.rotation.eulerAngles;
        Vector3 oppositePathPosition = SpacecraftManager.Instance.NominalSpacecraftTransform.position;
        Vector3 destinationPosition = SpacecraftManager.Instance.GetNominalPositionFromTime(_currentTime + deltaTime);
        Vector3 destinationVelocity = SpacecraftManager.Instance.GetNominalVelocityFromTime(_currentTime + deltaTime);
        
        var flightTime = CalculateFlightTime(originPosition, oppositePathPosition, destinationPosition, deltaTime);
        
        HttpManager.Instance.TransitionPathApi(
            originPosition,
            originVelocity,
            destinationPosition, 
            destinationVelocity,
            _currentTime,
            flightTime
        );
    }
    
    #endregion
}