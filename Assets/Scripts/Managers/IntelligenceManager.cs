using System;
using UnityEngine;

public class IntelligenceManager : MonoBehaviour
{
    public static IntelligenceManager Instance { get; private set; }
    
    private int _dataIndex;
    private float _time;
    
    // Thrust
    private string[][] _thrustData;
    
    // Transition Path
    private const string TransitionPathApiUri = "https://5ef6-2601-18c-500-fbb-18f2-2a3b-3c1e-d7bb.ngrok-free.app/trajectory";
    private const string TransitionPathApiContentType = "application/json";
    
    public static event Action<string> PathCalculated;
    
    
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

    private void Update()
    {
        
    }

    private void OnEnable()
    {
        DataManager.DataIndexUpdated += SetDataIndex;
        DataManager.DataLoaded += LoadThrustData;
        SimulationManager.ElapsedTimeUpdated += SetElapsedTime;
        UIManager.TransitionPath += StartTransitionPath;
    }

    private void OnDisable()
    {
        DataManager.DataIndexUpdated -= SetDataIndex;
        DataManager.DataLoaded -= LoadThrustData;
        SimulationManager.ElapsedTimeUpdated -= SetElapsedTime;
        UIManager.TransitionPath -= StartTransitionPath;
    }
    
    #endregion
    
    
    # region Getters and Setters
    
    private void SetDataIndex(int index)
    {
        _dataIndex = index;
    }

    private void SetElapsedTime(float elapsedTime)
    {
        _time = elapsedTime;
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
    
    
    #region Transition Path

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
        var toFuturePosition = SpacecraftManager.Instance.GetNominalPositionFromTime(_time + deltaTime) 
                               - SpacecraftManager.Instance.NominalSpacecraftTransform.position;
        
        var thetaInDegrees = Vector3.Angle(toOtherPath, toFuturePosition);
        var thetaInRadians = thetaInDegrees * Mathf.Deg2Rad;
        
        var rawFlightTime = Mathf.Sqrt(2 * deltaTimeSquared * (1 + Mathf.Cos(thetaInRadians)));
        
        // Returns the raw flight time with the additional buffer time.
        return rawFlightTime + bufferTime;
    }

    private void StartTransitionPath()
    {
        var deltaTime = CalculateDeltaTime(_dataIndex);
        
        Vector3 originPosition = SpacecraftManager.Instance.OffNominalSpacecraftTransform.position;
        Vector3 originVelocity = SpacecraftManager.Instance.OffNominalSpacecraftTransform.rotation.eulerAngles;
        Vector3 oppositePathPosition = SpacecraftManager.Instance.NominalSpacecraftTransform.position;
        Vector3 destinationPosition = SpacecraftManager.Instance.GetNominalPositionFromTime(_time + deltaTime);
        Vector3 destinationVelocity = SpacecraftManager.Instance.GetNominalVelocityFromTime(_time + deltaTime);
        
        var flightTime = CalculateFlightTime(originPosition, oppositePathPosition, destinationPosition, deltaTime);
        
        HttpManager.Instance.TransitionPathApi(
            originPosition,
            originVelocity,
            destinationPosition, 
            destinationVelocity,
            _time,
            flightTime
        );
    }
    
    #endregion
    
    
    #region Bump Off Course
    
    
    
    #endregion
}