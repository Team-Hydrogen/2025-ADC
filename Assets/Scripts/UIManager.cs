using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

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
    [SerializeField] private TextMeshProUGUI totalDistanceTravelledText;
    [SerializeField] private TextMeshProUGUI distanceFromEarthText;
    [SerializeField] private TextMeshProUGUI distanceFromMoonText;

    [Header("UI Settings")]
    [SerializeField] private float uiFadeSpeed;
    [SerializeField] private float inputInactivityTime;
    [Range(0, 1f)]
    [SerializeField] private float minimumUIVisiblity;

    private Vector3 lastMousePosition;
    private float inactivityTimer = 0f;
    private bool isFadingOut = false;

    private LengthUnit currentLengthUnit = LengthUnit.Kilometers;

    private Vector3 currentCoordinates;
    private float totalDistanceTravelled;
    private float distanceFromEarth;
    private float distanceFromMoon;

    void Update()
    {
        HandleUIVisibility();
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

    private void SetCoordinates(Vector3 position)
    {
        if (currentLengthUnit == LengthUnit.Kilometers)
        {
            xCoordinate.text = position.x.ToString("N0");
            yCoordinate.text = position.y.ToString("N0");
            zCoordinate.text = position.z.ToString("N0");
        } else if (currentLengthUnit == LengthUnit.Miles)
        {

            xCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(position.x).ToString("N0");
            yCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(position.y).ToString("N0");
            zCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(position.z).ToString("N0");
        }
    }

    private void SetDistances(float totalDistance, float fromEarth, float fromMoon)
    {
        SetTotalDistance(totalDistance);
        SetDistanceFromEarth(fromEarth);
        SetDistanceFromMoon(fromMoon);
    }

    private void SetDistanceFromEarth(float fromEarth)
    {
        if (currentLengthUnit == LengthUnit.Kilometers)
        {
            distanceFromEarthText.text = fromEarth.ToString("N0");
        } else if (currentLengthUnit == LengthUnit.Miles)
        {
            distanceFromEarthText.text = UnitAndCoordinateConverter.KilometersToMiles(fromEarth).ToString("N0");
        }
    }

    private void SetDistanceFromMoon(float fromMoon)
    {
        if (currentLengthUnit == LengthUnit.Kilometers)
        {
            distanceFromEarthText.text = fromMoon.ToString("N0");
        }
        else if (currentLengthUnit == LengthUnit.Miles)
        {
            distanceFromEarthText.text = UnitAndCoordinateConverter.KilometersToMiles(fromMoon).ToString("N0");
        }
    }

    private void SetTotalDistance(float totalDistance)
    {
        if (currentLengthUnit == LengthUnit.Kilometers)
        {
            distanceFromEarthText.text = totalDistance.ToString("N0");
        }
        else if (currentLengthUnit == LengthUnit.Miles)
        {
            distanceFromEarthText.text = UnitAndCoordinateConverter.KilometersToMiles(totalDistance).ToString("N0");
        }
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

        PrioritizeAntennas();
    }

    // Reorders antenna labels by distance, by changing hierarchy.
    private void PrioritizeAntennas()
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

    private enum LengthUnit {
        Kilometers,
        Miles
    }
}
