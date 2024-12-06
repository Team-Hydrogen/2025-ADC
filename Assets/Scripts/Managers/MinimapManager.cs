using System;
using System.Collections.Generic;
using UnityEngine;

public class MinimapManager : MonoBehaviour
{
    [Header("Nominal Minimap Trajectory")]
    [SerializeField] private LineRenderer pastMinimapTrajectory;
    [SerializeField] private LineRenderer futureMinimapTrajectory;
    
    [Header("Minimap Marker")]
    [SerializeField] private GameObject minimapMarker;
    
    [Header("Settings")]
    [SerializeField] private float trajectoryScale;

    private int previousIndex = 0;
    
    public static MinimapManager Instance { get; private set; }

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
        DataManager.OnDataLoaded += PlotMinimapTrajectory;
        SatelliteManager.OnCurrentIndexUpdated += UpdateMinimapTrajectory;
    }

    private void OnDisable()
    {
        DataManager.OnDataLoaded -= PlotMinimapTrajectory;
        SatelliteManager.OnCurrentIndexUpdated += UpdateMinimap;
    }

    private void UpdateMinimap(int index)
    {
        UpdateMinimapTrajectory(index);
        UpdateMarkerPosition(index);
    }
    
    private void PlotMinimapTrajectory(DataLoadedEventArgs data)
    {
        List<string[]> nominalTrajectoryData = data.NominalTrajectoryData;

        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = nominalTrajectoryData.Count;
        Vector3[] futureTrajectoryPoints = new Vector3[numberOfPoints];
        for (int index = 0; index < numberOfPoints; index++)
        {
            string[] point = nominalTrajectoryData[index];

            try
            {
                Vector3 pointAsVector = new Vector3(
                    float.Parse(point[1]) * trajectoryScale,
                    float.Parse(point[2]) * trajectoryScale,
                    0.0f); // float.Parse(point[3]) * trajectoryScale
                futureTrajectoryPoints[index] = pointAsVector;
            }
            catch
            {
                Debug.LogWarning("No positional data on line " + index + "!");
            }
        }
        // The first point of the pastTrajectory is added.
        pastMinimapTrajectory.positionCount = 1;
        pastMinimapTrajectory.SetPosition(0, futureTrajectoryPoints[0]);
        // The processed points are pushed to the future trajectory line.
        futureMinimapTrajectory.positionCount = numberOfPoints;
        futureMinimapTrajectory.SetPositions(futureTrajectoryPoints);
    }

    private void UpdateMinimapTrajectory(int index)
    {
        int indexChange = index - previousIndex;
        previousIndex = index;

        if (indexChange > 0)
        {
            Vector3[] futureTrajectoryPoints = new Vector3[futureMinimapTrajectory.positionCount];
            futureMinimapTrajectory.GetPositions(futureTrajectoryPoints);

            Vector3[] pointsToMove = new Vector3[indexChange];
            Array.Copy(futureTrajectoryPoints, 0, pointsToMove, 0, indexChange);

            // Add these points to the past trajectory
            Vector3[] pastTrajectoryPoints = new Vector3[pastMinimapTrajectory.positionCount];
            pastMinimapTrajectory.GetPositions(pastTrajectoryPoints);

            // Combine past trajectory points and new points
            Vector3[] newPastTrajectoryPoints = new Vector3[pastTrajectoryPoints.Length + pointsToMove.Length];
            Array.Copy(pastTrajectoryPoints, 0, newPastTrajectoryPoints, 0, pastTrajectoryPoints.Length);
            Array.Copy(pointsToMove, 0, newPastTrajectoryPoints, pastTrajectoryPoints.Length, pointsToMove.Length);

            // Update past trajectory
            pastMinimapTrajectory.positionCount = newPastTrajectoryPoints.Length;
            pastMinimapTrajectory.SetPositions(newPastTrajectoryPoints);

            // Remove moved points from future trajectory
            int newFuturePointCount = futureMinimapTrajectory.positionCount - indexChange;
            Vector3[] newFutureTrajectoryPoints = new Vector3[newFuturePointCount];
            Array.Copy(futureTrajectoryPoints, indexChange, newFutureTrajectoryPoints, 0, newFuturePointCount);

            futureMinimapTrajectory.positionCount = newFuturePointCount;
            futureMinimapTrajectory.SetPositions(newFutureTrajectoryPoints);
        }

        else if (indexChange < 0)
        {
            indexChange = -indexChange;

            // Get all points in the past trajectory
            Vector3[] pastTrajectoryPoints = new Vector3[pastMinimapTrajectory.positionCount];
            pastMinimapTrajectory.GetPositions(pastTrajectoryPoints);

            // Extract points to move back to the future trajectory
            Vector3[] pointsToMove = new Vector3[indexChange];
            Array.Copy(pastTrajectoryPoints, pastTrajectoryPoints.Length - indexChange, pointsToMove, 0, indexChange);

            // Add these points back to the future trajectory
            Vector3[] futureTrajectoryPoints = new Vector3[futureMinimapTrajectory.positionCount];
            futureMinimapTrajectory.GetPositions(futureTrajectoryPoints);

            Vector3[] newFutureTrajectoryPoints = new Vector3[futureTrajectoryPoints.Length + pointsToMove.Length];
            Array.Copy(pointsToMove, 0, newFutureTrajectoryPoints, 0, pointsToMove.Length);
            Array.Copy(futureTrajectoryPoints, 0, newFutureTrajectoryPoints, pointsToMove.Length, futureTrajectoryPoints.Length);

            // Update future trajectory
            futureMinimapTrajectory.positionCount = newFutureTrajectoryPoints.Length;
            futureMinimapTrajectory.SetPositions(newFutureTrajectoryPoints);

            // Remove moved points from past trajectory
            int newPastPointCount = pastMinimapTrajectory.positionCount - indexChange;
            Vector3[] newPastTrajectoryPoints = new Vector3[newPastPointCount];
            Array.Copy(pastTrajectoryPoints, 0, newPastTrajectoryPoints, 0, newPastPointCount);

            pastMinimapTrajectory.positionCount = newPastPointCount;
            pastMinimapTrajectory.SetPositions(newPastTrajectoryPoints);
        }
    }
    
    /// <summary>
    /// Updates the position of the Orion capsule marker
    /// </summary>
    private void UpdateMarkerPosition(int index)
    {
        if (futureMinimapTrajectory.positionCount <= 0)
        {
            return;
        }
        // The second point of the future trajectory is chosen because the first point is the satellite's position.
        Vector3 newSatellitePosition = futureMinimapTrajectory.GetPosition(1);
        // The satellite transforms to its new position.
        minimapMarker.transform.position = newSatellitePosition;
        // Rotation correction
        minimapMarker.transform.Rotate(new Vector3(90, 0, 0));
    }
}
