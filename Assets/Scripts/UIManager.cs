using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ReadCsv;

public class UIManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Antennas")]
    [SerializeField] Transform antennasGrid;
    [SerializeField] private List<string> antennaNames = new List<string>();
    [SerializeField] private List<Transform> antennaLabelObjects = new List<Transform>();
    [SerializeField] private List<Color> antennaTextColor = new List<Color>();
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

    [Header("UI Settings")]
    [SerializeField] private float uiFadeSpeed;
    [SerializeField] private float inputInactivityTime;
    [Range(0, 1f)]
    [SerializeField] private float minimumUIVisiblity;

    private Vector3 lastMousePosition;
    private float inactivityTimer = 0f;
    private bool isFadingOut = false;

    // Start is called before the first frame update
    void Start()
    {
        PlotTrajectory();
    }

    // Update is called once per frame
    void Update()
    {
        HandleUIVisibility();
    }

    public void UpdateAntenna(string antennaName, int connectionSpeed)
    {
        const string connectionSpeedUnit = "kbps";

        // Gets the index of the antenna name and maps it to its text object.
        int antennaIndex = antennaNames.IndexOf(antennaName);
        Transform antennaLabel = antennaLabelObjects[antennaIndex];

        TextMeshProUGUI[] antennaTexts = antennaLabel.GetComponentsInChildren<TextMeshProUGUI>();

        TextMeshProUGUI titleText = antennaTexts[0];
        TextMeshProUGUI colonText = antennaTexts[1];
        TextMeshProUGUI connectionSpeedText = antennaTexts[2];
        TextMeshProUGUI unitsText = antennaTexts[3];

        if (connectionSpeed == 0)
        {
            titleText.color = disabledAntennaTextColor;
            antennaLabel.GetComponentInChildren<Image>().color = disabledAntennaTextColor;

            for (int i = 1; i < antennaTexts.Length; i++)
            {
                antennaTexts[i].color = disabledAntennaTextColor;
                antennaTexts[i].text = "";
            }

            return;
        }

        for (int i = 0; i < antennaTexts.Length; i++)
        {
            antennaTexts[i].color = antennaTextColor[antennaIndex];
        }

        colonText.text = ":";
        connectionSpeedText.text = connectionSpeed.ToString("N0");
        unitsText.text = " " + connectionSpeedUnit;
        antennaLabel.GetComponentInChildren<Image>().color = antennaTextColor[antennaIndex];

        ReorderAttennaLabels();
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

    // Reorders antenna labels by distance, by changing hierarchy.
    private void ReorderAttennaLabels()
    {
        int childCount = antennasGrid.childCount;
        Transform[] antennaLabels = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            antennaLabels[i] = antennasGrid.GetChild(i);
        }

        var sortedLabels = antennaLabels
            .Select(antennaLabel => new
            {
                Label = antennaLabel,
                ConnectionSpeed = float.TryParse(antennaLabel.GetComponentsInChildren<TextMeshProUGUI>()[2].text, out float speed) ? speed : float.MinValue
            })
        .OrderByDescending(item => item.ConnectionSpeed)
        .Select(item => item.Label)
        .ToList();

        foreach (var label in sortedLabels)
        {
            label.SetSiblingIndex(sortedLabels.IndexOf(label));
        }
    }

    private void HandleUIVisibility()
    {
        if (Input.anyKey || Input.mousePosition != lastMousePosition)
        {
            inactivityTimer = 0f;
            isFadingOut = false;
            StartCoroutine(FadeUIIn());
        }
        else
        {
            inactivityTimer += Time.deltaTime;

            if (inactivityTimer >= inputInactivityTime && canvasGroup.alpha > 0)
            {
                isFadingOut = true;
            }
        }

        if (isFadingOut)
        {
            FadeUIOut();
        }

        lastMousePosition = Input.mousePosition;
    }

    private void FadeUIOut()
    {
        canvasGroup.alpha = Mathf.Max(minimumUIVisiblity, canvasGroup.alpha - uiFadeSpeed * Time.deltaTime);
    }

    private IEnumerator FadeUIIn()
    {
        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += uiFadeSpeed * Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1;
    }
}
