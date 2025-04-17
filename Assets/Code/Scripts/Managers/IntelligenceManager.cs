using System;
using System.Collections;
using UnityEngine;

public class IntelligenceManager : MonoBehaviour
{
    public static IntelligenceManager Instance { get; private set; }
    
    private int _dataIndex;
    private float _time;
    
    // Thrust
    private string[][] _thrustData;
    
    // Transition Path
    private const string TransitionPathApiUri = "https://8683-2600-387-15-3a1b-00-2.ngrok-free.app/trajectory";
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
        Vector3 origin = SpacecraftManager.Instance.NominalSpacecraftTransform.position;
        Vector3 destination = SpacecraftManager.Instance.OffNominalSpacecraftTransform.position;
        return destination - origin;
    }

    private Vector3 GetTimeVector(int dataIndex)
    {
        Vector3 distanceVector = GetDistanceVector();
        Vector3 accelerationVector = GetAccelerationVector(dataIndex);

        float timeX = Mathf.Sqrt(2.0f * distanceVector.x / accelerationVector.x);
        float timeY = Mathf.Sqrt(2.0f * distanceVector.y / accelerationVector.y);
        float timeZ = Mathf.Sqrt(2.0f * distanceVector.z / accelerationVector.z);
        
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
        
        // Determines both the present and future positions on the original and opposite trajectories.
        Vector3 presentOriginalPosition = SpacecraftManager.Instance.OffNominalSpacecraftTransform.position;
        Vector3 presentOppositePosition = SpacecraftManager.Instance.NominalSpacecraftTransform.position;
        Vector3 futureOppositePosition = SpacecraftManager.Instance.GetNominalPositionFromTime(_time + deltaTime);
        
        // Creates a vector where the starting endpoint is the chosen path's present position, and the ending endpoint
        // is the opposite path's present position.
        Vector3 toOtherPath = presentOppositePosition - presentOriginalPosition;
        // Creates a vector where the starting endpoint is the opposite path's present position, and the ending endpoint
        // is the opposite path's future position.
        Vector3 toFuturePosition = futureOppositePosition - presentOppositePosition;
        
        float thetaInDegrees = Vector3.Angle(toOtherPath, toFuturePosition);
        float thetaInRadians = thetaInDegrees * Mathf.Deg2Rad;
        
        float deltaTimeSquared = deltaTime * deltaTime;
        float rawFlightTime = Mathf.Sqrt(2 * deltaTimeSquared * (1 + Mathf.Cos(thetaInRadians)));
        
        // Returns the raw flight time with the additional buffer time.
        return rawFlightTime + bufferTime;
    }
    
    private void StartTransitionPath()
    {
        float deltaTime = CalculateDeltaTime(_dataIndex);
        
        Vector3 originPosition = SpacecraftManager.Instance.OffNominalSpacecraftTransform.position;
        Vector3 originVelocity = SpacecraftManager.Instance.OffNominalSpacecraftTransform.rotation.eulerAngles;
        
        Vector3 presentOppositeTrajectoryPosition = SpacecraftManager.Instance.NominalSpacecraftTransform.position;
        Vector3 destinationPosition = SpacecraftManager.Instance.GetNominalPositionFromTime(_time + deltaTime);
        
        float flightTime = CalculateFlightTime(
            originPosition,
            presentOppositeTrajectoryPosition,
            destinationPosition,
            deltaTime);

        string transitionPathPostData = TransitionPathRequest.ToJson(
            originPosition,
            originVelocity,
            destinationPosition,
            _time,
            flightTime);
        
        IEnumerator request = HttpRequest.RequestApi(
            TransitionPathApiUri,
            transitionPathPostData,
            TransitionPathApiContentType,
            csvData => PathCalculated?.Invoke(csvData));
        
        StartCoroutine(request);
    }
    
    #endregion
    
    
    #region Bump Off Course
    
    
    
    #endregion
}