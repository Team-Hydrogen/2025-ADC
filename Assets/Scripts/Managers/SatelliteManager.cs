using System;
using System.Collections.Generic;
using UnityEngine;

public class SatelliteManager : MonoBehaviour
{
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
    
    public static Action<float[]> OnDistanceCalculated; 
    public static SatelliteManager Instance { get; private set; }

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

    /// <param name="pointsData">List containing data points in cartesian coordinates</param>
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
    
    /// <param name="pointsData">List containing data points in cartesian coordinates</param>
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
        
        velocityVector.transform.localScale = Vector3.one * vector.magnitude;
        velocityVector.transform.rotation = Quaternion.LookRotation(vector);
    }
        
    private void CalculateDistance()
    {
        float distanceToEarth = Vector3.Distance(satellite.transform.position, earth.transform.position);
        float distanceToMoon = Vector3.Distance(satellite.transform.position, moon.transform.position);
        float[] distances = { _totalDistance, distanceToEarth, distanceToMoon };
        OnDistanceCalculated?.Invoke(distances);
    }
}
