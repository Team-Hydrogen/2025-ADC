using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ReadCsv;

public class UIManager : MonoBehaviour
{
    [Header("Antennas")]
    [SerializeField] private List<string> antennaNames = new List<string>();
    [SerializeField] private List<TextMeshProUGUI> antennaText = new List<TextMeshProUGUI>();
    [SerializeField] private List<Color> antennaTextColor = new List<Color>();
    [SerializeField] private List<int> antennaSpeeds = new List<int>();
    [SerializeField] private Color disabledAntennaTextColor = new Color(0.8f, 0.8f, 0.8f);

    [Header("Time Counter")]
    [SerializeField] private TextMeshProUGUI dayCounter;
    [SerializeField] private TextMeshProUGUI hourCounter;
    [SerializeField] private TextMeshProUGUI minuteCounter;
    [SerializeField] private TextMeshProUGUI secondCounter;
    
    [Header("Coordinate")]
    [SerializeField] private TextMeshProUGUI xCoordinate;
    [SerializeField] private TextMeshProUGUI yCoordinate;
    [SerializeField] private TextMeshProUGUI zCoordinate;
    
    [Header("Distance")]
    [SerializeField] private TextMeshProUGUI totalDistanceTravelled;
    [SerializeField] private TextMeshProUGUI distanceFromEarth;
    [SerializeField] private TextMeshProUGUI distanceFromMoon;
    
    [Header("Trajectory")]
    [SerializeField] private LineRenderer pastTrajectory;
    [SerializeField] private LineRenderer futureTrajectory;

    // Start is called before the first frame update
    void Start()
    {
        // UpdateAntenna("DSS24", 0);
        // UpdateAntenna("DSS34", 789010);
        PlotTrajectory();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DisableAntenna()
    {
        
    }

    public void UpdateAntenna(string antennaName, int connectionSpeed)
    {
        const string connectionSpeedUnit = "kbps";
        // Gets the index of the antenna name and maps it to its text object.
        int index = antennaNames.IndexOf(antennaName);
        // The default display of the antenna name is added.
        antennaText[index].text = antennaName;
        antennaText[index].color = disabledAntennaTextColor;
        antennaText[index].GetComponentInChildren<Image>().color = disabledAntennaTextColor;
        // If the connection speed exceeds 0, it is displayed.
        if (connectionSpeed <= 0)
        {
            return;
        }
        antennaText[index].text += ": " + connectionSpeed.ToString("N0") + " " + connectionSpeedUnit;
        antennaText[index].color = antennaTextColor[index];
        antennaText[index].GetComponentInChildren<Image>().color = antennaTextColor[index];
    }

    public void SetTime(int days, int hours, int minutes, int seconds)
    {
        dayCounter.text = days.ToString();
        hourCounter.text = hours.ToString();
        minuteCounter.text = minutes.ToString();
        secondCounter.text = seconds.ToString();
    }

    public void IncrementTime(int changeInDays, int changeInHours, int changeInMinutes, int changeInSeconds)
    {
        dayCounter.text = (int.Parse(dayCounter.text) - changeInDays).ToString();
        hourCounter.text = (int.Parse(hourCounter.text) - changeInHours).ToString();
        minuteCounter.text = (int.Parse(minuteCounter.text) - changeInMinutes).ToString();
        secondCounter.text = (int.Parse(secondCounter.text) - changeInSeconds).ToString();
    }

    private void PrioritizeAntennas(List<GameObject> antennaArray)
    {
        GridLayout gridLayout = gameObject.GetComponent<GridLayout>();
        
        // var arrayLength = antennaArray.Length;
        // for (int i = 0; i < arrayLength - 1; i++)
        // {
        //     var smallestVal = i;
        //     for (int j = i + 1; j < arrayLength; j++)
        //     {
        //         if (antennaArray[j] < antennaArray[smallestVal])
        //         {
        //             smallestVal = j;
        //         }
        //     }
        //     var tempVar = antennaArray[smallestVal];
        //     antennaArray[smallestVal] = antennaArray[i];
        //     antennaArray[i] = tempVar;
        // }
        // return NumArray;
    }

    private void SetCoordinates(float x, float y, float z)
    {
        xCoordinate.text = x.ToString("N0");
        yCoordinate.text = y.ToString("N0");
        zCoordinate.text = z.ToString("N0");
    }

    private void SetDistance(float totalDistance, float fromEarth, float fromMoon)
    {
        totalDistanceTravelled.text = totalDistance.ToString("N0");
        distanceFromEarth.text = fromEarth.ToString("N0");
        distanceFromMoon.text = fromMoon.ToString("N0");
    }
    
    /// <summary>
    /// Plots the trajectory of the Artemis II
    /// </summary>
    private void PlotTrajectory()
    {
        // The CSV data containing the coordinates of the trajectory is read.
        const string trajectoryPointsFilepath = "Assets/Resources/hsdata.csv";
        var pointsData = ReadCsvFile(trajectoryPointsFilepath);
        // The first row is removed, so only the numerical data remains.
        pointsData.RemoveAt(0);
        
        // An array of trajectory points is constructed by reading the processed CSV file.
        var numberOfPoints = pointsData.Count;
        var futureTrajectoryPoints = new Vector3[numberOfPoints];
        for (var index = 0; index < pointsData.Count; index++)
        {
            var point = pointsData[index];
            var pointAsVector = new Vector3(float.Parse(point[0]), float.Parse(point[1]), float.Parse(point[2]));
            futureTrajectoryPoints[index] = pointAsVector;
        }
        
        // The processed points are pushed to the future trajectory line.
        futureTrajectory.positionCount = numberOfPoints;
        futureTrajectory.SetPositions(futureTrajectoryPoints);

        // REMAINING TRAJECTORY ALGORITHM
        // if (trajectoryPoints.Contains(satellite.transform.position) == false)
        // {
        //      pastTrajectory.positionCount = numberOfPoints - futureTrajectory.positionCount + 1;
        //      pastTrajectory.SetPosition(pastTrajectory.positionCount - 1, satellite.transform.position);
        // }
        // else
        // {
        //      var newFutureTrajectoryPoints = new Vector3[];
        //      Put it as the last coordinate of the Past Trajectory
        // }
    }
}
