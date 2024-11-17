using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SatelliteManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float trajectoryScale;
    
    [Header("Satellite")]
    [SerializeField] private GameObject satellite;
    
    [Header("Trajectory")]
    [SerializeField] private LineRenderer pastTrajectory;
    [SerializeField] private LineRenderer futureTrajectory;

    private void Start()
    {
        pastTrajectory.positionCount = 0;
        PlotTrajectory();
    }

    private void Update()
    {
        UpdateSatellitePosition();
        UpdateTrajectory();
    }

    /// <summary>
    /// Plots the trajectory of the Orion capsule
    /// </summary>
    private void PlotTrajectory()
    {
        // The CSV data containing the coordinates of the trajectory is read.
        const string trajectoryPointsFilepath = "Assets/Data/hsdata.csv";
        List<string[]> pointsData = CsvReader.ReadCsvFile(trajectoryPointsFilepath);
        // The first row is removed, so only the numerical data remains.
        pointsData.RemoveAt(0);

        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = pointsData.Count;
        Vector3[] futureTrajectoryPoints = new Vector3[numberOfPoints];
        for (int index = 0; index < pointsData.Count; index++)
        {
            string[] point = pointsData[index];

            try
            {
                Vector3 pointAsVector = new Vector3(float.Parse(point[1]) * trajectoryScale, float.Parse(point[2]) * trajectoryScale, float.Parse(point[3]) * trajectoryScale);
                futureTrajectoryPoints[index] = pointAsVector;
            }
            catch
            {
                Debug.LogWarning("No positional data on line " + index + "!");
            }
        }

        // The processed points are pushed to the future trajectory line.
        futureTrajectory.positionCount = numberOfPoints;
        futureTrajectory.SetPositions(futureTrajectoryPoints);
    }
    
    /// <summary>
    /// Updates the trajectory of the Orion capsule
    /// </summary>
    private void UpdateTrajectory()
    {
        Vector3[] futureTrajectoryPoints = new Vector3[futureTrajectory.positionCount];
        futureTrajectory.GetPositions(futureTrajectoryPoints);
        
        List<Vector3> newFutureTrajectoryPoints = futureTrajectoryPoints.ToList();
        newFutureTrajectoryPoints.RemoveAt(0);
        
        pastTrajectory.positionCount++;
        pastTrajectory.SetPosition(pastTrajectory.positionCount - 1, futureTrajectoryPoints[0]);
        
        futureTrajectoryPoints = newFutureTrajectoryPoints.ToArray();
        futureTrajectory.positionCount--;
        futureTrajectory.SetPositions(futureTrajectoryPoints);
    }

    /// <summary>
    /// Updates the position of the Orion capsule
    /// </summary>
    private void UpdateSatellitePosition()
    {
        if (futureTrajectory.positionCount > 0)
        {
            satellite.transform.position = futureTrajectory.GetPosition(0);
        }
    }
}
