using System;
using System.Collections.Generic;
using UnityEngine;

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
        DataManager.OnMissionStageUpdated += SetPastTrajectoryRendererByStage;
        HttpManager.OnPathCalculated += LoadMergeTrajectoryData;
    }

    private void OnDisable()
    {
        DataManager.OnDataLoaded -= LoadTrajectoryData;
        DataManager.OnMissionStageUpdated -= SetPastTrajectoryRendererByStage;
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
    /// Sets the past trajectory based on stage
    /// </summary>
    /// <param name="stage">Updated mission stage</param>
    private void SetPastTrajectoryRendererByStage(MissionStage stage)
    {
        // The past trajectory line renderer is set to the mission stage's respective line renderer.
        if (_pastNominalTrajectoryLineRenderer.Equals(stage.nominalLineRenderer))
        {
            return;
        }
        
        _pastNominalTrajectoryLineRenderer = stage.nominalLineRenderer;
        _pastNominalTrajectoryLineRenderer = stage.offNominalLineRenderer;
        
        // _pastNominalTrajectoryLineRenderer.SetPosition(0, NominalSpacecraftTransform.position);
        // _pastNominalTrajectoryLineRenderer.SetPosition(0, OffNominalSpacecraftTransform.position);
        
        // trigger animation here if it is correct stage
    }
    
    /// <summary>
    /// Visualizes provided trajectory data
    /// </summary>
    /// <param name="points">A list of three-dimensional points as strings</param>
    /// <param name="past">The line renderer visualizing the past trajectory</param>
    /// <param name="future">The line renderer visualizing the future trajectory</param>
    private void PlotTrajectory(List<string[]> points, LineRenderer past, LineRenderer future)
    {
        // An array of three-dimensional points is constructed by processing the CSV file.
        int numberOfPoints = points.Count;
        Vector3[] futurePoints = new Vector3[numberOfPoints];
        
        for (int index = 0; index < numberOfPoints; index++)
        {
            string[] point = points[index];
            
            try
            {
                Vector3 pointAsVector = new Vector3(
                    float.Parse(point[1]) * trajectoryScale,
                    float.Parse(point[2]) * trajectoryScale,
                    float.Parse(point[3]) * trajectoryScale);
                
                futurePoints[index] = pointAsVector;
            }
            catch
            {
                Debug.LogWarning($"No positional data exists on line {index}!");
            }
        }
        
        // The past trajectory has two points. The first point in the past trajectory represents the first given data
        // point. The second data point represents the spacecraft's current position. Upon initialization, the
        // spacecraft will begin at the first data point. Therefore, both points in the past trajectory are set to the
        // first data point.
        past.positionCount = 2;
        past.SetPosition(0, futurePoints[0]); // The first provided data point
        past.SetPosition(1, futurePoints[0]); // The spacecraft's current position
        
        // The future trajectory has N points. The first point in the future trajectory represents the spacecraft's
        // current position. The last data point of the past trajectory must be the first data point of the spacecraft
        // to ensure both ends of the trajectory connect to the spacecraft. Upon initialization, the spacecraft will
        // begin at the first data point. The spacecraft would have not visited any data points beyond the first data
        // point. Therefore, the remaining data points are pushed to the future trajectory. 
        future.positionCount = numberOfPoints;
        future.SetPositions(futurePoints);
    }
    
    /// <summary>
    /// Splits the trajectory into past and future halves based on a provided time
    /// </summary>
    /// <param name="currentTime">The provided simulation time</param>
    /// <param name="trajectoryStageCutoffs"></param>
    /// <param name="trajectoryData"></param>
    /// <param name="trajectory"></param>
    private void SplitTrajectory(float currentTime, float[] trajectoryStageCutoffs,
        List<string[]> trajectoryData, Transform trajectory)
    {
        // The total amount of data points and sub-trajectories are determined. The total number of sub-trajectories
        // includes multiple past trajectories and the designated future trajectory.  
        int totalDataPoints = trajectoryData.Count;
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
