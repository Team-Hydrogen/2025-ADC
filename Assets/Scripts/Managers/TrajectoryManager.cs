using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This manager specializes in visualizing the trajectory.
/// </summary>

/// <summary>
/// This manager specializes in visualizing the trajectory.
/// </summary>
public class TrajectoryManager : MonoBehaviour
{
    [Header("Rendering")]
    [SerializeField] private Transform nominalTrajectory;
    [SerializeField] private Transform offNominalTrajectory;
    [SerializeField] private Transform minimapTrajectory;
    [SerializeField] private Transform mergeTrajectory;
    
    #region Past Trajectory Renderers
    
    private LineRenderer _pastNominalTrajectoryLineRenderer;
    private LineRenderer _pastOffNominalTrajectoryLineRenderer;
    private LineRenderer _pastMinimapTrajectoryLineRenderer;
    private LineRenderer _pastMergeTrajectoryLineRenderer;
    
    # endregion
    
    #region Future Trajectory Renderers
    
    private LineRenderer _futureNominalTrajectoryLineRenderer;
    private LineRenderer _futureOffNominalTrajectoryLineRenderer;
    private LineRenderer _futureMinimapTrajectoryLineRenderer;
    private LineRenderer _futureMergeTrajectoryLineRenderer;
    
    # endregion
    
    [Header("Plotting")]
    [SerializeField] private float trajectoryScale;

    private List<string[]> _nominalTrajectoryData;
    private List<string[]> _offNominalTrajectoryData;
    private List<string[]> _minimapTrajectoryData;
    private List<string[]> _mergeTrajectoryData;
    
    #region Event Functions
    
    private void OnEnable()
    {
        DataManager.OnDataLoaded += LoadTrajectoryData;
        HttpManager.OnPathCalculated += LoadMergeTrajectoryData;
    }

    private void OnDisable()
    {
        DataManager.OnDataLoaded -= LoadTrajectoryData;
        HttpManager.OnPathCalculated -= LoadMergeTrajectoryData;
    }
    
    #endregion

    #region Essential Methods
    
    private void LoadTrajectoryData(DataLoadedEventArgs data)
    {
        _nominalTrajectoryData = data.NominalTrajectoryData;
        _offNominalTrajectoryData = data.OffNominalTrajectoryData;
    }

    private void LoadMergeTrajectoryData(string rawData)
    {
        
    }
    
    /// <summary>
    /// Visualizes provided trajectory data
    /// </summary>
    /// <param name="currentTime">The provided simulation time</param>
    /// <param name="trajectoryStageCutoffs"></param>
    /// <param name="trajectoryData"></param>
    /// <param name="trajectory"></param>
    private void PlotTrajectory(float currentTime, float[] trajectoryStageCutoffs, List<string[]> trajectoryData, 
        Transform trajectory)
    {
        // The total amount of data points and sub-trajectories are determined. The total number of sub-trajectories
        // includes multiple past trajectories and the designated future trajectory.  
        int totalDataPoints = trajectoryData.Count; // This is the amount of lines in the trajectory dataset.
        int totalSubTrajectories = trajectory.childCount; // The final sub-trajectory is always the future trajectory.
        
        List<Vector3>[] subTrajectoryPositions = new List<Vector3>[totalSubTrajectories];
        int subTrajectoryIndex = 0;

        for (int dataIndex = 0; dataIndex < totalDataPoints; dataIndex++)
        {
            string[] dataLine = trajectoryData[dataIndex];

            float time;
            Vector3 currentPoint;
            
            try
            {
                time = float.Parse(dataLine[0]);
                currentPoint = new Vector3(
                    float.Parse(dataLine[1]), float.Parse(dataLine[2]), float.Parse(dataLine[3]));
            }
            catch
            {
                Debug.LogWarning($"Neither temporal nor positional trajectory data exists on data line {dataIndex}.");
                break;
            }
            
            if (time >= trajectoryStageCutoffs[subTrajectoryIndex] || time > currentTime)
            {
                float interpolationRatio = Mathf.InverseLerp(
                    float.Parse(trajectoryData[dataIndex - 1][0]), time, currentTime);
                
                Vector3 interpolatedPoint = new Vector3(
                    Mathf.Lerp(currentPoint.x, float.Parse(trajectoryData[dataIndex - 1][1]), interpolationRatio),
                    Mathf.Lerp(currentPoint.y, float.Parse(trajectoryData[dataIndex - 1][2]), interpolationRatio),
                    Mathf.Lerp(currentPoint.z, float.Parse(trajectoryData[dataIndex - 1][3]), interpolationRatio)
                );
                
                // The interpolated point is added as the last point of the current sub-trajectory. If the timestamp
                // is behind the simulation's timestamp, the interpolated point is added to as the first point of the
                // next sub-trajectory. If the timestamp is at the simulation's timestamp, the interpolated point is
                // added as the first point of the "future trajectory."
                subTrajectoryPositions[subTrajectoryIndex].Add(interpolatedPoint);
                subTrajectoryIndex = time > currentTime ? totalSubTrajectories - 1 : subTrajectoryIndex + 1;
                subTrajectoryPositions[subTrajectoryIndex].Add(interpolatedPoint);
            }

            subTrajectoryPositions[subTrajectoryIndex].Add(currentPoint);
        }
        
        // Assign each sub-trajectory their respective positions.
        for (int trajectoryIndex = 0; trajectoryIndex < subTrajectoryPositions.Length; trajectoryIndex++)
        {
            LineRenderer subTrajectoryRenderer = trajectory.GetChild(trajectoryIndex).GetComponent<LineRenderer>();
            
            Vector3[] positions = subTrajectoryPositions[trajectoryIndex].ToArray();
            subTrajectoryRenderer.positionCount = positions.Length;
            subTrajectoryRenderer.SetPositions(positions);
        }
    }
    
    #endregion
}
