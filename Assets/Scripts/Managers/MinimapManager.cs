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
        // The current future trajectory is loaded.
        Vector3[] futureTrajectoryPoints = new Vector3[futureMinimapTrajectory.positionCount];
        futureMinimapTrajectory.GetPositions(futureTrajectoryPoints);
        // The past trajectory's list of positions expands, so the next future data point is added.
        Vector3 nextTrajectoryPoint = futureTrajectoryPoints[1];
        pastMinimapTrajectory.positionCount++;
        pastMinimapTrajectory.SetPosition(pastMinimapTrajectory.positionCount - 1, nextTrajectoryPoint);
        // The next point in the future trajectory gets removed.
        futureTrajectoryPoints = futureTrajectoryPoints[1..^1];
        futureMinimapTrajectory.positionCount--;
        futureMinimapTrajectory.SetPositions(futureTrajectoryPoints);
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
