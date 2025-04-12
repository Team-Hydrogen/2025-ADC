using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

public class TrajectoryManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float trajectoryScale;
    [SerializeField] private float timeScale;
    
    [Header("Future Trajectories")]
    [SerializeField] private LineRenderer futureNominalTrajectory;
    [SerializeField] private LineRenderer futureOffNominalTrajectory;
    
    [Header("Transition Trajectory")]
    [SerializeField] private Transform trajectoryParent;
    [SerializeField] private GameObject transitionTrajectoryPrefab;

    private MissionStage[] _allMissionStages;
    
    #region Trajectory Data
    
    private string[][] _nominalTrajectoryData;
    private string[][] _offNominalTrajectoryData;
    private string[][] _transitionTrajectoryData;
    private string[][] _selectedTrajectoryData;
    
    #endregion
    
    
    #region Line Renderers
    
    private LineRenderer _pastNominalTrajectoryRenderer;
    private LineRenderer _pastOffNominalTrajectoryRenderer;
    
    private LineRenderer _pastTransitionTrajectoryRenderer;
    private LineRenderer _futureTransitionTrajectoryRenderer;

    #endregion


    #region Index and Time Variables

    private float _currentTime;
    
    private int _previousTimeLowerIndex = 0;
    private int _previousTimeUpperIndex = 1;
    
    private int _currentTimeLowerIndex = 0;
    private int _currentTimeUpperIndex = 1;

    private bool _isTransitioning = false;
    
    #endregion
    
    #region Properties
    
    
    
    #endregion
    
    
    #region Event Functions
    
    private void OnEnable()
    {
        DataManager.DataIndexUpdated += UpdateIndexBounds;
        DataManager.DataLoaded += LoadData;
        DataManager.MissionStageUpdated += UpdatePastTrajectoryRenderers;
        
        IntelligenceManager.PathCalculated += PlotTransitionTrajectory;
        
        SpacecraftManager.PositionUpdated += UpdateTrajectories;
        SpacecraftManager.SpacecraftStateUpdated += SetIsTransitioning;
        SpacecraftManager.SpacecraftStateUpdated += UpdateSelectedTrajectoryData;

        SimulationManager.ElapsedTimeUpdated += UpdateElapsedTime;
    }
    
    private void OnDisable()
    {
        DataManager.DataIndexUpdated -= UpdateIndexBounds;
        DataManager.DataLoaded -= LoadData;
        DataManager.MissionStageUpdated -= UpdatePastTrajectoryRenderers;
        
        IntelligenceManager.PathCalculated -= PlotTransitionTrajectory;
        
        SpacecraftManager.PositionUpdated -= UpdateTrajectories;
        SpacecraftManager.SpacecraftStateUpdated -= SetIsTransitioning;
        SpacecraftManager.SpacecraftStateUpdated -= UpdateSelectedTrajectoryData;

        SimulationManager.ElapsedTimeUpdated -= UpdateElapsedTime;
    }
    
    #endregion
    
    private void LoadData(DataLoadedEventArgs data)
    {
        // Get both the nominal and off-nominal trajectory data.
        _nominalTrajectoryData = data.NominalTrajectoryData;
        _offNominalTrajectoryData = data.OffNominalTrajectoryData;
        // Get both the nominal and off-nominal line renderers.
        _pastNominalTrajectoryRenderer = data.MissionStage.nominalLineRenderer;
        _pastOffNominalTrajectoryRenderer = data.MissionStage.offNominalLineRenderer;
        // Generate an initial plot of both trajectories. 
        PlotTrajectory(_nominalTrajectoryData, _pastNominalTrajectoryRenderer, futureNominalTrajectory);
        PlotTrajectory(_offNominalTrajectoryData, _pastOffNominalTrajectoryRenderer, futureOffNominalTrajectory);
        // By default, select the nominal trajectory data.
        _selectedTrajectoryData = _nominalTrajectoryData;

        _allMissionStages = data.AllMissionStages;
    }
    
    private void UpdateTrajectories()
    {
        UpdateTrajectory(
            SpacecraftManager.Instance.NominalSpacecraftTransform.position,
            _pastNominalTrajectoryRenderer,
            futureNominalTrajectory
        );
        UpdateTrajectory(
            SpacecraftManager.Instance.OffNominalSpacecraftTransform.position,
            _pastOffNominalTrajectoryRenderer,
            futureOffNominalTrajectory
        );
        if (_isTransitioning)
        {
            UpdateTrajectory(
                SpacecraftManager.Instance.TransitionSpacecraftTransform.position,
                _pastTransitionTrajectoryRenderer,
                _futureTransitionTrajectoryRenderer
            );
        }
        
        // Ensures the trajectory does not continuously remove points even if the lower index does not change.
        _previousTimeLowerIndex = _currentTimeLowerIndex;
    }
    
    private void UpdateIndexBounds(int newLowerIndex)
    {
        _previousTimeLowerIndex = _currentTimeLowerIndex;
        _previousTimeUpperIndex = _currentTimeUpperIndex;
        
        _currentTimeLowerIndex = newLowerIndex;
        _currentTimeUpperIndex = newLowerIndex + 1;
    }
    
    private void UpdatePastTrajectoryRenderers(MissionStage stage)
    {
        if (_pastNominalTrajectoryRenderer.Equals(stage.nominalLineRenderer))
        {
            return;
        }
        
        _pastNominalTrajectoryRenderer = stage.nominalLineRenderer;
        _pastNominalTrajectoryRenderer.SetPosition(0, SpacecraftManager.Instance.NominalSpacecraftTransform.position);

        _pastOffNominalTrajectoryRenderer = stage.offNominalLineRenderer;
        _pastOffNominalTrajectoryRenderer.SetPosition(0, SpacecraftManager.Instance.OffNominalSpacecraftTransform.position);
        
        // trigger animation here if it is correct stage
    }
    
    /// <summary>
    /// Converts trajectory data into a visualization
    /// </summary>
    /// <param name="data">A list of three-dimensional points</param>
    /// <param name="past">A line to represent the past path</param>
    /// <param name="future">A line to represent the future path</param>
    private void PlotTrajectory(string[][] data, LineRenderer past, LineRenderer future)
    {
        // An array of three-dimensional points is constructed by processing the CSV file.
        int numberOfPoints = data.Length;
        Vector3[] futurePoints = new Vector3[numberOfPoints];
        
        for (int index = 0; index < numberOfPoints; index++)
        {
            string[] point = data[index];
            
            try
            {
                Vector3 pointAsVector = new Vector3(
                    float.Parse(point[1]),
                    float.Parse(point[2]),
                    float.Parse(point[3])
                ) * trajectoryScale;
                
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
    
    /// <summary>
    /// Updates the trajectory
    /// </summary>
    /// <param name="spacecraftPosition">The current position of the spacecraft</param>
    /// <param name="past">The past trajectory line renderer</param>
    /// <param name="future">The future trajectory line renderer</param>
    private void UpdateTrajectory(Vector3 spacecraftPosition, LineRenderer past, LineRenderer future)
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
                Vector3[] traversedPoints = new ArraySegment<Vector3>(
                    futureTrajectoryPoints,
                    1,
                    indexChange).ToArray();
                
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
                ReplotTrajectory();
                return;
                indexChange = -indexChange;
                
                // Get all past trajectory data points.
                Vector3[] pastTrajectoryPoints = new Vector3[past.positionCount];
                past.GetPositions(pastTrajectoryPoints);
                // Get all future trajectory data points.
                Vector3[] futureTrajectoryPoints = new Vector3[future.positionCount];
                future.GetPositions(futureTrajectoryPoints);
                // Get the traversed points between the past and current indexes.
                Vector3[] traversedPoints = new ArraySegment<Vector3>(
                    pastTrajectoryPoints,
                    pastTrajectoryPoints.Length - indexChange - 1,
                    indexChange).ToArray();
                
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

        past.SetPosition(past.positionCount - 1, spacecraftPosition);

        //try
        //{
        //    past.SetPosition(past.positionCount - 1, spacecraftPosition);
        //} catch (IndexOutOfRangeException e)
        //{
        //    past.positionCount++;
        //    past.SetPosition(past.positionCount - 1, spacecraftPosition);
        //}

        future.SetPosition(0, spacecraftPosition);
    }

    private void ReplotTrajectory()
    {
        for (int i = 0; i < _allMissionStages.Length; i++)
        {
            _allMissionStages[i].nominalLineRenderer.positionCount = 0;
            _allMissionStages[i].nominalLineRenderer.positionCount = 2;
            _allMissionStages[i].offNominalLineRenderer.positionCount = 0;
            _allMissionStages[i].offNominalLineRenderer.positionCount = 2;
            _allMissionStages[i].minimapLineRenderer.positionCount = 0;
        }

        PlotTrajectory(_nominalTrajectoryData, _allMissionStages[0].nominalLineRenderer, futureNominalTrajectory);
        PlotTrajectory(_offNominalTrajectoryData, _allMissionStages[0].offNominalLineRenderer, futureOffNominalTrajectory);

        int[] indexBounds = GetIndexBoundsFromTime(_nominalTrajectoryData, _currentTime);
        _currentTimeLowerIndex = indexBounds[0];
        _currentTimeUpperIndex = indexBounds[1];

        int indexesUntilCurrentIndex = _currentTimeLowerIndex;

        MissionStage spacecraftMissionStage = _allMissionStages[0];
        int spacecraftMissionStageIndex = 0;

        for (int i = 0; i < _allMissionStages.Length; i++)
        {
            if (_allMissionStages[i].startDataIndex > indexesUntilCurrentIndex)
            {
                spacecraftMissionStage = _allMissionStages[i];
                spacecraftMissionStageIndex = i;
                break;
            }
        }

        int currentMissionStageIndex = 0;
        for (int i = 0; i < _nominalTrajectoryData.Length; i++)
        {
            if (i >= _currentTimeLowerIndex)
            {
                break;
            }

            if (i >= _allMissionStages[currentMissionStageIndex].startDataIndex)
            {
                currentMissionStageIndex++;
            }

            Vector3 position = new Vector3(
                float.Parse(_nominalTrajectoryData[i][1]),
                float.Parse(_nominalTrajectoryData[i][2]),
                float.Parse(_nominalTrajectoryData[i][3])
            ) * trajectoryScale;

            _allMissionStages[currentMissionStageIndex].nominalLineRenderer.positionCount++;
            _allMissionStages[currentMissionStageIndex].nominalLineRenderer.SetPosition(
                _allMissionStages[currentMissionStageIndex].nominalLineRenderer.positionCount - 1,
                position);

            Vector3[] futureTrajectoryPoints = new Vector3[futureNominalTrajectory.positionCount];
            futureNominalTrajectory.GetPositions(futureTrajectoryPoints);

            Vector3[] newFutureTrajectoryPoints = new Vector3[futureTrajectoryPoints.Length - 1];
            Array.Copy(
                futureTrajectoryPoints,
                1,
                newFutureTrajectoryPoints,
                0,
                newFutureTrajectoryPoints.Length);
        }

        _pastNominalTrajectoryRenderer = spacecraftMissionStage.nominalLineRenderer;

        //_pastNominalTrajectoryRenderer = spacecraftMissionStage.nominalLineRenderer;

        //for (int i = 0; i < spacecraftMissionStageIndex; i++)
        //{
        //    if (_allMissionStages[i].Equals(spacecraftMissionStage))
        //    {
        //        int rowCount = _allMissionStages[i].startDataIndex - _currentTimeLowerIndex;

        //        for (int j = _allMissionStages[i].startDataIndex; j < )
        //    }

        //    else
        //    {
        //        for (int j = _allMissionStages[i].startDataIndex; j < _allMissionStages[i+1].startDataIndex; j++)
        //        {
        //            int rowCount = _allMissionStages[i].startDataIndex - _allMissionStages[i + 1].startDataIndex;
        //            //_allMissionStages[i].nominalLineRenderer.positionCount = rowCount;
        //            //string[][] data = new string[rowCount][];

        //            //Array.Copy(_nominalTrajectoryData, _allMissionStages[i].startDataIndex, data, 0, rowCount);

        //            //Vector3[] positions = new Vector3[rowCount];

        //            for (int k = 0; k < rowCount; k++)
        //            {
        //                Vector3 s = futureNominalTrajectory[k];
        //                //catch
        //                //{
        //                //    Debug.LogWarning($"No positional data exists on line {k}!");
        //                //}
        //            }

        //            _allMissionStages[i].nominalLineRenderer.SetPositions(positions);
        //        }
        //    }
        //}

    }

    private void UpdateElapsedTime(float time)
    {
        _currentTime = time;
    }


    private void PlotTransitionTrajectory(string dataAsString)
    {
        Debug.Log($"Transition Trajectory CSV File\n{dataAsString}");
        
        _transitionTrajectoryData = CsvReader.ReadCsvString(dataAsString);
        _transitionTrajectoryData = new ArraySegment<string[]>(
            _transitionTrajectoryData, 1, _transitionTrajectoryData.Length).ToArray();
        
        GameObject transitionTrajectory = Instantiate(this.transitionTrajectoryPrefab, trajectoryParent);
        LineRenderer[] transitionTrajectoryRenderers = transitionTrajectory.GetComponentsInChildren<LineRenderer>();
        
        _pastTransitionTrajectoryRenderer = transitionTrajectoryRenderers[0];
        _futureTransitionTrajectoryRenderer = transitionTrajectoryRenderers[1];
        
        PlotTrajectory(
            _transitionTrajectoryData,
            _pastTransitionTrajectoryRenderer,
            _futureTransitionTrajectoryRenderer
        );
    }

    private void SetIsTransitioning(SpacecraftManager.SpacecraftState state)
    {
        _isTransitioning = state == SpacecraftManager.SpacecraftState.Transition;
    }

    private void UpdateSelectedTrajectoryData(SpacecraftManager.SpacecraftState state)
    {
        _selectedTrajectoryData = state switch
        {
            SpacecraftManager.SpacecraftState.Nominal => _nominalTrajectoryData,
            SpacecraftManager.SpacecraftState.OffNominal => _offNominalTrajectoryData,
            SpacecraftManager.SpacecraftState.Transition => _transitionTrajectoryData,
            _ => _nominalTrajectoryData
        };
    }
    
    #region Helper Functions
    
    private static int[] GetIndexBoundsFromTime(string[][] data, float elapsedTime)
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