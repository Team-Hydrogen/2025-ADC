using System;
using System.Collections.Generic;
using UnityEngine;

public class MinimapManager : MonoBehaviour
{
    [Header("Nominal Minimap Trajectory")]
    //[SerializeField] private LineRenderer pastMinimapTrajectory;
    [SerializeField] private LineRenderer futureMinimapTrajectory;
    
    [Header("Minimap Marker")]
    [SerializeField] private GameObject minimapMarker;
    
    [Header("Settings")]
    [SerializeField] private float trajectoryScale;

    private int previousIndex = 0;
    private LineRenderer currentMinimapTrajectory;
    
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
        DataManager.OnDataLoaded += OnDataLoaded;
        SatelliteManager.OnCurrentIndexUpdated += UpdateMinimapTrajectory;
        DataManager.OnMissionStageUpdated += OnMissionStageUpdated;
    }

    private void OnDisable()
    {
        DataManager.OnDataLoaded -= OnDataLoaded;
        SatelliteManager.OnCurrentIndexUpdated += UpdateMinimap;
        DataManager.OnMissionStageUpdated -= OnMissionStageUpdated;
    }

    private void UpdateMinimap(int index)
    {
        UpdateMinimapTrajectory(index);
    }

    private void OnMissionStageUpdated(MissionStage missionStage)
    {
        if (!currentMinimapTrajectory.Equals(missionStage.minimapLineRenderer))
        {
            currentMinimapTrajectory = missionStage.minimapLineRenderer;
        }
    }

    private void OnDataLoaded(DataLoadedEventArgs data)
    {
        currentMinimapTrajectory = data.MissionStage.minimapLineRenderer;
        PlotMinimapTrajectory(data.NominalTrajectoryData);
    }
    
    private void PlotMinimapTrajectory(List<string[]> nominalTrajectoryData)
    {
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
        currentMinimapTrajectory.positionCount = 1;
        currentMinimapTrajectory.SetPosition(0, futureTrajectoryPoints[0]);
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
            Vector3[] pastTrajectoryPoints = new Vector3[currentMinimapTrajectory.positionCount];
            currentMinimapTrajectory.GetPositions(pastTrajectoryPoints);

            // Combine past trajectory points and new points
            Vector3[] newPastTrajectoryPoints = new Vector3[pastTrajectoryPoints.Length + pointsToMove.Length];
            Array.Copy(pastTrajectoryPoints, 0, newPastTrajectoryPoints, 0, pastTrajectoryPoints.Length);
            Array.Copy(pointsToMove, 0, newPastTrajectoryPoints, pastTrajectoryPoints.Length, pointsToMove.Length);

            // Update past trajectory
            currentMinimapTrajectory.positionCount = newPastTrajectoryPoints.Length;
            currentMinimapTrajectory.SetPositions(newPastTrajectoryPoints);

            // Remove moved points from future trajectory
            int newFuturePointCount = futureMinimapTrajectory.positionCount - indexChange;
            Vector3[] newFutureTrajectoryPoints = new Vector3[newFuturePointCount];
            Array.Copy(futureTrajectoryPoints, indexChange, newFutureTrajectoryPoints, 0, newFuturePointCount);

            futureMinimapTrajectory.positionCount = newFuturePointCount;
            futureMinimapTrajectory.SetPositions(newFutureTrajectoryPoints);

            // Update minimap marker
            minimapMarker.transform.position = newFutureTrajectoryPoints[0];
        }

        else if (indexChange < 0)
        {
            indexChange = -indexChange;

            // Get all points in the past trajectory
            Vector3[] pastTrajectoryPoints = new Vector3[currentMinimapTrajectory.positionCount];
            currentMinimapTrajectory.GetPositions(pastTrajectoryPoints);

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
            int newPastPointCount = currentMinimapTrajectory.positionCount - indexChange;
            Vector3[] newPastTrajectoryPoints = new Vector3[newPastPointCount];
            Array.Copy(pastTrajectoryPoints, 0, newPastTrajectoryPoints, 0, newPastPointCount);

            currentMinimapTrajectory.positionCount = newPastPointCount;
            currentMinimapTrajectory.SetPositions(newPastTrajectoryPoints);

            // Update minimap marker
            minimapMarker.transform.position = newPastTrajectoryPoints[0];
        }
    }
}
