using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SatelliteManager : MonoBehaviour
{
    public static SatelliteManager instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private float trajectoryScale;
    
    [Header("Satellite")]
    [SerializeField] private GameObject satellite;
    [SerializeField] private GameObject velocityVector;
    
    [Header("Celestial Bodies")]
    [SerializeField] private GameObject earth;
    [SerializeField] private GameObject moon;
    
    [Header("Nominal Trajectory")]
    [SerializeField] private LineRenderer pastNominalTrajectory;
    [SerializeField] private LineRenderer futureNominalTrajectory;
    
    [Header("Offnominal Trajectory")]
    [SerializeField] private LineRenderer pastOffnominalTrajectory;
    [SerializeField] private LineRenderer futureOffnominalTrajectory;
    
    private float _totalDistance = 0.0f;

    #region Material Variables
    
    private const float LowThreshold = 0.4000f;
    private const float MediumThreshold = 3.0000f;
    private const float HighThreshold = 8.0000f;
    
    private Renderer[] _vectorRenderers;
    private readonly Color[] _colors = {
        new(0.9373f, 0.2588f, 0.2588f, 1.0000f),
        new(1.0000f, 0.7569f, 0.0000f, 1.0000f),
        new(0.5451f, 0.9294f, 0.1804f, 1.0000f)
    };
    
    #endregion
    
    public static Action<float[]> OnDistanceCalculated;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
    }

    private void Start()
    { 
        _vectorRenderers = velocityVector.GetComponentsInChildren<Renderer>();
    }

    private void OnEnable()
    {
        DataManager.OnDataLoaded += PlotNominalTrajectory;
        DataManager.OnDataLoaded += PlotOffnominalTrajectory;
        DataManager.OnDataUpdated += UpdateSatelliteFromData;
    }

    private void OnDisable()
    {
        DataManager.OnDataLoaded -= PlotNominalTrajectory;
        DataManager.OnDataLoaded -= PlotOffnominalTrajectory;
        DataManager.OnDataUpdated -= UpdateSatelliteFromData;
    }

    private void UpdateSatelliteFromData(int currentIndex)
    {
        UpdateSatellitePosition();
        UpdateNominalTrajectory();
        UpdateOffnominalTrajectory();
        CalculateDistance();
        UpdateVelocityVector(currentIndex);
    }
    
    /// <summary>
    /// Plots the provided data points into a visual trajectory. PlotTrajectory() is meant to be run only once.
    /// </summary>
    private void PlotNominalTrajectory()
    {
        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = DataManager.nominalTrajectoryDataValues.Count;
        Vector3[] futureTrajectoryPoints = new Vector3[numberOfPoints];
        for (int index = 0; index < numberOfPoints; index++)
        {
            string[] point = DataManager.nominalTrajectoryDataValues[index];

            try
            {
                Vector3 pointAsVector = new Vector3(
                    float.Parse(point[1]) * trajectoryScale,
                    float.Parse(point[2]) * trajectoryScale,
                    float.Parse(point[3]) * trajectoryScale);
                futureTrajectoryPoints[index] = pointAsVector;
            }
            catch
            {
                Debug.LogWarning("No positional data on line " + index + "!");
            }
        }
        // The first point of the pastTrajectory is added.
        pastNominalTrajectory.positionCount = 1;
        pastNominalTrajectory.SetPosition(0, futureTrajectoryPoints[0]);
        // The processed points are pushed to the future trajectory line.
        futureNominalTrajectory.positionCount = numberOfPoints;
        futureNominalTrajectory.SetPositions(futureTrajectoryPoints);
    }
    
    /// <summary>
    /// Plots the provided data points into a visual trajectory. PlotTrajectory() is meant to be run only once.
    /// </summary>
    private void PlotOffnominalTrajectory()
    {
        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = DataManager.offnominalTrajectoryDataValues.Count;
        Vector3[] futureTrajectoryPoints = new Vector3[numberOfPoints];
        for (int index = 0; index < numberOfPoints; index++)
        {
            string[] point = DataManager.offnominalTrajectoryDataValues[index];

            try
            {
                Vector3 pointAsVector = new Vector3(
                    float.Parse(point[1]) * trajectoryScale,
                    float.Parse(point[2]) * trajectoryScale,
                    float.Parse(point[3]) * trajectoryScale);
                futureTrajectoryPoints[index] = pointAsVector;
            }
            catch
            {
                Debug.LogWarning("No positional data on line " + index + "!");
            }
        }
        // The first point of the pastTrajectory is added.
        pastOffnominalTrajectory.positionCount = 1;
        pastOffnominalTrajectory.SetPosition(0, futureTrajectoryPoints[0]);
        // The processed points are pushed to the future trajectory line.
        futureOffnominalTrajectory.positionCount = numberOfPoints;
        futureOffnominalTrajectory.SetPositions(futureTrajectoryPoints);
    }
    
    /// <summary>
    /// Updates the position of the Orion capsule
    /// </summary>
    private void UpdateSatellitePosition()
    {
        if (futureNominalTrajectory.positionCount <= 0)
        {
            return;
        }
        // The second point of the future trajectory is chosen because the first point is the satellite's position.
        Vector3 currentSatellitePosition = futureNominalTrajectory.GetPosition(0);
        Vector3 newSatellitePosition = futureNominalTrajectory.GetPosition(1);
        // The distance is calculated by taking the current point.
        _totalDistance += Vector3.Distance(currentSatellitePosition, newSatellitePosition);
        // The satellite transforms to its new position.
        satellite.transform.position = newSatellitePosition;
        satellite.transform.rotation = Quaternion.LookRotation(newSatellitePosition - currentSatellitePosition);
        // Rotation correction
        satellite.transform.Rotate(new Vector3(90, 0, 0));
    }
    
    /// <summary>
    /// Updates the trajectory of the Orion capsule
    /// </summary>
    private void UpdateNominalTrajectory()
    {
        // The current future trajectory is loaded.
        Vector3[] futureTrajectoryPoints = new Vector3[futureNominalTrajectory.positionCount];
        futureNominalTrajectory.GetPositions(futureTrajectoryPoints);
        // The past trajectory's list of positions expands, so the next future data point is added.
        Vector3 nextTrajectoryPoint = futureTrajectoryPoints[1];
        pastNominalTrajectory.positionCount++;
        pastNominalTrajectory.SetPosition(pastNominalTrajectory.positionCount - 1, nextTrajectoryPoint);
        // The next point in the future trajectory gets removed.
        futureTrajectoryPoints = futureTrajectoryPoints[1..^1];
        futureNominalTrajectory.positionCount--;
        futureNominalTrajectory.SetPositions(futureTrajectoryPoints);
    }
    
    /// <summary>
    /// Updates the trajectory of the Orion capsule
    /// </summary>
    private void UpdateOffnominalTrajectory()
    {
        // The current future trajectory is loaded.
        Vector3[] futureTrajectoryPoints = new Vector3[futureOffnominalTrajectory.positionCount];
        futureOffnominalTrajectory.GetPositions(futureTrajectoryPoints);
        // The past trajectory's list of positions expands, so the next future data point is added.
        Vector3 nextTrajectoryPoint = futureTrajectoryPoints[1];
        pastOffnominalTrajectory.positionCount++;
        pastOffnominalTrajectory.SetPosition(pastOffnominalTrajectory.positionCount - 1, nextTrajectoryPoint);
        // The next point in the future trajectory gets removed.
        futureTrajectoryPoints = futureTrajectoryPoints[1..^1];
        futureOffnominalTrajectory.positionCount--;
        futureOffnominalTrajectory.SetPositions(futureTrajectoryPoints);
    }
    
    private void UpdateVelocityVector(int currentIndex)
    {
        // A Vector3 variable is created to store and compute information about the current velocity vector.
        Vector3 vector = new Vector3(
            float.Parse(DataManager.nominalTrajectoryDataValues[currentIndex][4]),
            float.Parse(DataManager.nominalTrajectoryDataValues[currentIndex][5]),
            float.Parse(DataManager.nominalTrajectoryDataValues[currentIndex][6]));
        
        velocityVector.transform.position = satellite.transform.position - satellite.transform.forward;
        velocityVector.transform.localScale = Vector3.one * vector.magnitude;
        velocityVector.transform.rotation = Quaternion.LookRotation(vector);
        // Correction rotation
        velocityVector.transform.Rotate(new Vector3(90, 0, 0));
        
        var bracketIndex = vector.magnitude switch
        {
            >= HighThreshold => 0,
            >= MediumThreshold => 1,
            >= LowThreshold => 2,
            _ => 2
        };
        foreach (var meshRenderer in _vectorRenderers)
        {
            meshRenderer.material.SetColor("_BaseColor", _colors[bracketIndex]);
        }
    }
        
    private void CalculateDistance()
    {
        float distanceToEarth = Vector3.Distance(satellite.transform.position, earth.transform.position);
        float distanceToMoon = Vector3.Distance(satellite.transform.position, moon.transform.position);
        float[] distances = { _totalDistance, distanceToEarth, distanceToMoon };
        OnDistanceCalculated?.Invoke(distances);
    }
}
