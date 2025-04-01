using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrajectoryManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float trajectoryScale;
    [SerializeField] private float timeScale;
    
    [Header("Future Trajectories")]
    [SerializeField] private LineRenderer futureNominalTrajectory;
    [SerializeField] private LineRenderer futureOffNominalTrajectory;
    
    [Header("Spacecraft Positions")]
    [SerializeField] private Transform nominalSpacecraftTransform;
    [SerializeField] private Transform offNominalSpacecraftTransform;
    [SerializeField] private Transform transitionSpacecraftTransform;
    private Transform _selectedSpacecraftTransform;
    
    private List<string[]> _nominalTrajectoryData;
    private List<string[]> _offNominalTrajectoryData;
    private List<string[]> _transitionTrajectoryData;
    private List<string[]> _selectedTrajectoryData;
    
    private LineRenderer _pastNominalTrajectoryRenderer;
    private LineRenderer _pastOffNominalTrajectoryRenderer;
    
    private LineRenderer _pastMergeTrajectoryRenderer;
    private LineRenderer _futureMergeTrajectoryRenderer;
    
    #region Index and Time Variables

    private float _previousElapsedTime = 0.0f;
    private int _previousTimeLowerIndex = 0;
    private int _previousTimeUpperIndex = 1;
    
    private float _currentElapsedTime = 0.0f;
    private int _currentTimeLowerIndex = 0;
    private int _currentTimeUpperIndex = 1;
    
    #endregion
    
    
    private void Update()
    {
        _currentElapsedTime += Time.deltaTime * timeScale;
        
        UpdateIndexBounds();
        
        _previousElapsedTime = _currentElapsedTime;
    }


    private void OnEnable()
    {
        DataManager.OnDataLoaded += LoadData;
    }

    private void OnDisable()
    {
        DataManager.OnDataLoaded -= LoadData;
    }

    private void LoadData(DataLoadedEventArgs data)
    {
        // Get both the nominal and off-nominal trajectory data.
        _nominalTrajectoryData = data.NominalTrajectoryData;
        _offNominalTrajectoryData = data.OffNominalTrajectoryData;
        // Get both the nominal and off-nominal line renderers.
        _pastNominalTrajectoryRenderer = data.MissionStage.nominalLineRenderer;
        _pastOffNominalTrajectoryRenderer = data.MissionStage.offnominalLineRenderer;
        // Generate an initial plot of both trajectories. 
        PlotTrajectory(_nominalTrajectoryData, _pastNominalTrajectoryRenderer, futureNominalTrajectory);
        PlotTrajectory(_offNominalTrajectoryData, _pastOffNominalTrajectoryRenderer, futureOffNominalTrajectory);
    }
    
    private void UpdateIndexBounds()
    {
        int[] lowerIndexBounds = GetIndexBoundsFromTime(_selectedTrajectoryData, _previousElapsedTime);
        int[] upperIndexBounds = GetIndexBoundsFromTime(_selectedTrajectoryData, _currentElapsedTime);
        
        _previousTimeLowerIndex = lowerIndexBounds[0];
        _previousTimeUpperIndex = lowerIndexBounds[1];
        _currentTimeLowerIndex = upperIndexBounds[0];
        _currentTimeUpperIndex = upperIndexBounds[1];
    }
    
    /// <summary>
    /// Converts trajectory data into a visualization
    /// </summary>
    /// <param name="data">A list of three-dimensional points</param>
    /// <param name="past">A line to represent the past path</param>
    /// <param name="future">A line to represent the future path</param>
    private void PlotTrajectory(List<string[]> data, LineRenderer past, LineRenderer future)
    {
        // An array of three-dimensional points is constructed by processing the CSV file.
        int numberOfPoints = data.Count;
        Vector3[] futurePoints = new Vector3[numberOfPoints];
        
        for (int index = 0; index < numberOfPoints; index++)
        {
            string[] point = data[index];
            
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
        // The past trajectory's first point is added.
        past.positionCount = 2;
        past.SetPosition(0, futurePoints[0]);
        // The processed points are pushed to the future trajectory.
        future.positionCount = numberOfPoints;
        future.SetPositions(futurePoints);
    }

    private void UpdateSpacecraftPosition(List<string[]> data, Transform trajectoryTransform)
    {
        float interpolationRatio = Mathf.InverseLerp(
            float.Parse(data[_currentTimeLowerIndex][0]),
            float.Parse(data[_currentTimeUpperIndex][0]),
            _currentElapsedTime
        );
        
        Vector3 interpolatedPosition = Vector3.Lerp(
            new Vector3(
                float.Parse(data[_currentTimeLowerIndex][1]),
                float.Parse(data[_currentTimeLowerIndex][2]),
                float.Parse(data[_currentTimeLowerIndex][3])
            ),
            new Vector3(
                float.Parse(data[_currentTimeUpperIndex][1]),
                float.Parse(data[_currentTimeUpperIndex][2]),
                float.Parse(data[_currentTimeUpperIndex][3])
            ),
            interpolationRatio
        );
        
        trajectoryTransform.position = interpolatedPosition;
    }
    
    /// <summary>
    /// Updates the trajectory
    /// </summary>
    /// <param name="spacecraftTransform"></param>
    /// <param name="past"></param>
    /// <param name="future"></param>
    /// <param name="indexUpdated"></param>
    /// <param name="positionUpdated"></param>
    private void UpdateTrajectory(Transform spacecraftTransform, LineRenderer past, LineRenderer future)
    {
        int indexChange = _currentTimeLowerIndex - _previousTimeLowerIndex;
        
        switch (indexChange)
        {
            // If the index change is positive, the spacecraft moves forward across data points.
            case > 0:
            {
                // Get all past trajectory data points.
                Vector3[] pastTrajectoryPoints = new Vector3[past.positionCount];
                past.GetPositions(pastTrajectoryPoints);
                // Get all future trajectory data points.
                Vector3[] futureTrajectoryPoints = new Vector3[future.positionCount];
                future.GetPositions(futureTrajectoryPoints);
                
                // Get the traversed points between the past and current indexes.
                Vector3[] traversedPoints = new Vector3[indexChange];
                Array.Copy(
                    futureTrajectoryPoints,
                    1,
                    traversedPoints,
                    0,
                    indexChange);
                
                // Create new trajectory arrays.
                Vector3[] newPastTrajectoryPoints = new Vector3[past.positionCount + indexChange];
                Vector3[] newFutureTrajectoryPoints = new Vector3[future.positionCount - indexChange];
                
                // Append the traversed points with the past trajectory.
                pastTrajectoryPoints.CopyTo(newPastTrajectoryPoints, 0);
                traversedPoints.CopyTo(newPastTrajectoryPoints, pastTrajectoryPoints.Length);
                // Update the past trajectory.
                past.positionCount = newPastTrajectoryPoints.Length;
                past.SetPositions(newPastTrajectoryPoints);
                
                // Remove the traversed points from the future trajectory.
                Array.Copy(
                    futureTrajectoryPoints,
                    indexChange,
                    newFutureTrajectoryPoints,
                    0,
                    newFutureTrajectoryPoints.Length);
                // Update the future trajectory.
                future.positionCount = newFutureTrajectoryPoints.Length;
                future.SetPositions(newFutureTrajectoryPoints);
                
                break;
            }
            // Otherwise, if the index change is negative, the spacecraft moves backward across data points.
            case < 0:
            {
                indexChange = -indexChange;
                
                // Get all past trajectory data points.
                Vector3[] pastTrajectoryPoints = new Vector3[past.positionCount];
                past.GetPositions(pastTrajectoryPoints);
                // Get all future trajectory data points.
                Vector3[] futureTrajectoryPoints = new Vector3[future.positionCount];
                future.GetPositions(futureTrajectoryPoints);
                
                // Get the traversed points between the past and current indexes.
                Vector3[] traversedPoints = new Vector3[indexChange];
                Array.Copy(
                    pastTrajectoryPoints,
                    pastTrajectoryPoints.Length - indexChange - 1,
                    traversedPoints,
                    0,
                    indexChange);
                
                // Create new trajectory arrays.
                Vector3[] newPastTrajectoryPoints = new Vector3[past.positionCount - indexChange];
                Vector3[] newFutureTrajectoryPoints = new Vector3[future.positionCount + indexChange];
                
                // Prepend the traversed points with the future trajectory.
                traversedPoints.CopyTo(newFutureTrajectoryPoints, 0);
                futureTrajectoryPoints.CopyTo(newFutureTrajectoryPoints, traversedPoints.Length);
                // Update the future trajectory.
                future.positionCount = newFutureTrajectoryPoints.Length;
                future.SetPositions(newFutureTrajectoryPoints);
                
                // Remove the traversed points from the past trajectory.
                Array.Copy(
                    pastTrajectoryPoints,
                    0,
                    newPastTrajectoryPoints,
                    0,
                    newPastTrajectoryPoints.Length);
                // Update the past trajectory.
                past.positionCount = newPastTrajectoryPoints.Length;
                past.SetPositions(newPastTrajectoryPoints);
                
                break;
            }
        }
        
        past.SetPosition(past.positionCount - 1, spacecraftTransform.position);
        future.SetPosition(0, spacecraftTransform.position);
    }
    
    #region Helper Functions
    
    private static int[] GetIndexBoundsFromTime(List<string[]> data, float elapsedTime)
    {
        // Convert string times to floats once for retrieval efficiency.
        float[] times = data.Select(line => float.Parse(line[0])).ToArray();
        
        // `Array.BinarySearch` is used to achieve O(log n) performance. If `elapsedTime` is not found, the method
        // returns the negative index of its otherwise sorted position. The bitwise complement operator is used to get
        // the positive array index.
        int closestIndex = Array.BinarySearch(times, elapsedTime);
        closestIndex = closestIndex >= 0 ? closestIndex : ~closestIndex;
        
        // Determine the lower and upper index bounds.
        int lowerIndex = Math.Max(0, closestIndex - 1);
        int upperIndex = Math.Min(times.Length - 1, closestIndex);
        
        // Returns the index bounds as an integer array.
        return new[] { lowerIndex, upperIndex };
    }
    
    #endregion
}