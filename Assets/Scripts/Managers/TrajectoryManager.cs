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
    /// <param name="data">The trajectory data</param>
    /// <param name="past">The line renderer visualizing the past trajectory</param>
    /// <param name="future">The line renderer visualizing the future trajectory</param>
    /// <param name="errorLines">The number of trailing erroneous or null lines</param>
    /// <returns>The spacecraft's interpolated position</returns>
    private Vector3 SplitTrajectory(float currentTime, List<string[]> data, LineRenderer past, LineRenderer future, int errorLines=3)
    {
        // The number of data points is calculated ahead of time based on the number of erroneous lines.
        int numberOfValidPoints = data.Count - errorLines;
        
        // The pivot index is the first index of the dataset where the time is ahead of the provided `currentTime`.
        int pivotIndex = 1;
        float lowerTime = float.Parse(data[pivotIndex - 1][0]);
        float upperTime = float.Parse(data[pivotIndex][0]);
        
        while (lowerTime >= currentTime && currentTime < upperTime && pivotIndex < numberOfValidPoints)
        {
            lowerTime = float.Parse(data[pivotIndex - 1][0]);
            upperTime = float.Parse(data[pivotIndex][0]);
            pivotIndex++;
        }
        
        // The interpolation ratio is found between the two times, which will later be applied to positions.
        float interpolationRatio = Mathf.InverseLerp(lowerTime, upperTime, currentTime);
        // The spacecraft's current position is calculated.
        Vector3 interpolatedPoint = new Vector3(
            Mathf.Lerp(float.Parse(data[pivotIndex - 1][1]), float.Parse(data[pivotIndex][1]), interpolationRatio),
            Mathf.Lerp(float.Parse(data[pivotIndex - 1][2]), float.Parse(data[pivotIndex][2]), interpolationRatio),
            Mathf.Lerp(float.Parse(data[pivotIndex - 1][3]), float.Parse(data[pivotIndex][3]), interpolationRatio)
        ) * trajectoryScale;
        
        // The number of points in the past trajectory and future trajectory are calculated and set.
        past.positionCount = pivotIndex + 1;
        future.positionCount = numberOfValidPoints - pivotIndex + 2;
        
        // The points in the past trajectory are determined by including all positions before the pivot index. The
        // points in the future trajectory are determined by including all positions at and after the pivot index. The
        // interpolated position is included as the past trajectory's final data point and the future trajectory's
        // first data point to visually connect the trajectory.
        Vector3[] pastTrajectoryPoints = new Vector3[pivotIndex + 1];
        Vector3[] futureTrajectoryPoints = new Vector3[numberOfValidPoints - pivotIndex + 2];
        
        for (int dataIndex = 0; dataIndex < pivotIndex; dataIndex++)
        {
            pastTrajectoryPoints[dataIndex] = new Vector3(
                float.Parse(data[dataIndex][1]),
                float.Parse(data[dataIndex][2]),
                float.Parse(data[dataIndex][3])
            ) * trajectoryScale;
        }
        for (int dataIndex = pivotIndex; dataIndex < numberOfValidPoints; dataIndex++)
        {
            futureTrajectoryPoints[dataIndex - pivotIndex + 1] = new Vector3(
                float.Parse(data[dataIndex][1]),
                float.Parse(data[dataIndex][2]),
                float.Parse(data[dataIndex][3])
            ) * trajectoryScale;
        }
        
        pastTrajectoryPoints[^1] = interpolatedPoint; // The past trajectory's final data point
        futureTrajectoryPoints[0] = interpolatedPoint; // The future trajectory's first data point
        
        past.SetPositions(pastTrajectoryPoints);
        future.SetPositions(futureTrajectoryPoints);
        
        return interpolatedPoint;
    }
    
    #endregion
}
