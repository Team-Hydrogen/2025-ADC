using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SatelliteManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float trajectoryScale;
    [Tooltip("How fast the satellite moves in data points per second"), Range(1, 400)]
    [SerializeField] private int trajectorySpeed;
    
    [Header("Satellite")]
    [SerializeField] private GameObject satellite;
    
    [Header("Trajectory")]
    [SerializeField] private LineRenderer pastTrajectory;
    [SerializeField] private LineRenderer futureTrajectory;
    
    /// <param name="pointsData">List containing data points in cartesian coordinates</param>
    /// <summary>
    /// Plots the provided data points into a visual trajectory. PlotTrajectory() is meant to be run only once.
    /// </summary>
    public void PlotTrajectory(List<string[]> pointsData)
    {
        // An array of trajectory points is constructed by reading the processed CSV file.
        int numberOfPoints = pointsData.Count;
        Vector3[] futureTrajectoryPoints = new Vector3[numberOfPoints];
        for (int index = 0; index < pointsData.Count; index++)
        {
            string[] point = pointsData[index];

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
        
        // The processed points are pushed to the future trajectory line.
        futureTrajectory.positionCount = numberOfPoints;
        futureTrajectory.SetPositions(futureTrajectoryPoints);
    }
    
    /// <summary>
    /// Updates the trajectory of the Orion capsule
    /// </summary>
    public void UpdateTrajectory()
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
    public void UpdateSatellitePosition()
    {
        if (futureTrajectory.positionCount > 0)
        {
            satellite.transform.position = futureTrajectory.GetPosition(0);
        }
    }
}
