using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [HideInInspector] public static UIManager Instance { get; private set; }

    private Vector3 _lastMousePosition;
    private float _inactivityTimer = 0f;
    private bool _isFadingOut = false;

    private LengthUnit _currentLengthUnit = LengthUnit.Kilometers;
    private const string NoDecimalPlaces = "N0";
    private const string ThreeDecimalPlaces = "N3";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        DataManager.OnDataUpdated += UpdateUIFromData;
        SatelliteManager.OnDistanceCalculated += UpdateUIDistances;
    }

    private void OnDisable()
    {
        DataManager.OnDataUpdated -= UpdateUIFromData;
        SatelliteManager.OnDistanceCalculated -= UpdateUIDistances;
    }

    private void Update()
    {
        HandleUIVisibility();
    }

    #region Manage Buttons

    #region Manage Timeline Buttons
    public void PlayButtonPressed()
    {
        Time.timeScale = 1f;
    }

    public void PauseButtonPressed()
    {
        Time.timeScale = 0f;
    }

    public void SkipForwardButtonPressed()
    {
        Debug.Log("Skipping forward not implemented!");
    }

    public void SkipBackwardButtonPressed()
    {
        Debug.Log("Skipping backward not implemented!");
    }
    #endregion

    public void ImperialButtonPressed()
    {
        _currentLengthUnit = LengthUnit.Miles;
    }

    public void MetricButtonPressed()
    {
        _currentLengthUnit = LengthUnit.Kilometers;
    }
    #endregion

    private void UpdateUIFromData(string[] data)
    {
        UpdateCoordinatesFromData(data);
        UpdateTimeFromData(data);
    }

    private void UpdateUIDistances(float[] distances)
    {
        SetDistances(distances[0], distances[1], distances[2]);
    }

    #region Manage Time
    private void SetTime(int days, int hours, int minutes, int seconds)
    {
        const int maxNumberLength = 2;
        dayCounter.text = days.ToString().PadLeft(maxNumberLength, '0');
        hourCounter.text = hours.ToString().PadLeft(maxNumberLength, '0');
        minuteCounter.text = minutes.ToString().PadLeft(maxNumberLength, '0');
        secondCounter.text = seconds.ToString().PadLeft(maxNumberLength, '0');
    }
    
    private void UpdateTimeFromData(string[] currentData)
    {
        const int minutesPerDay = 1440;
        const int minutesPerHour = 60;
        const int secondsPerMinute = 60;
        
        float totalTimeInMinutes;
        try
        {
            totalTimeInMinutes = float.Parse(currentData[0]);
        }
        catch
        {
            Debug.LogWarning("No time data available!");
            return;
        }
        
        int days = Mathf.FloorToInt(totalTimeInMinutes / minutesPerDay);
        totalTimeInMinutes %= minutesPerDay;
        int hours = Mathf.FloorToInt(totalTimeInMinutes / minutesPerHour);
        totalTimeInMinutes %= minutesPerHour;
        int minutes = Mathf.FloorToInt(totalTimeInMinutes);
        totalTimeInMinutes -= minutes;
        int seconds = Mathf.FloorToInt(totalTimeInMinutes * secondsPerMinute);
        
        SetTime(days, hours, minutes, seconds);
    }

    public void IncrementTime(int changeInDays, int changeInHours, int changeInMinutes, int changeInSeconds)
    {
        dayCounter.text = (int.Parse(dayCounter.text) - changeInDays).ToString();
        hourCounter.text = (int.Parse(hourCounter.text) - changeInHours).ToString();
        minuteCounter.text = (int.Parse(minuteCounter.text) - changeInMinutes).ToString();
        secondCounter.text = (int.Parse(secondCounter.text) - changeInSeconds).ToString();
    }
    #endregion

    #region Manage Coordinates
    private void SetCoordinates(float x, float y, float z)
    {
        string units;
        
        if (_currentLengthUnit == LengthUnit.Kilometers)
        {
            units = " km";
            xCoordinate.text = x.ToString("N0") + units;
            yCoordinate.text = y.ToString("N0") + units;
            zCoordinate.text = z.ToString("N0") + units;
        } else if (_currentLengthUnit == LengthUnit.Miles)
        {
            units = " mi";
            xCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(x).ToString("N0") + units;
            yCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(y).ToString("N0") + units;
            zCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(z).ToString("N0") + units;
        }
    }
    
    private void UpdateCoordinatesFromData(string[] currentData)
    {
        float x;
        float y;
        float z;

        try
        {
            x = float.Parse(currentData[1]);
            y = float.Parse(currentData[2]);
            z = float.Parse(currentData[3]);
        }
        catch
        {
            Debug.LogWarning("No positional data available!");
            return;
        }
        
        SetCoordinates(x, y, z);
    }
    #endregion

    #region Update Distances
    private void SetTotalDistance(float totalDistance)
    {
        if (_currentLengthUnit == LengthUnit.Kilometers)
        {
            totalDistanceTravelledText.text = totalDistance.ToString(ThreeDecimalPlaces) + " km";
        }
        else if (_currentLengthUnit == LengthUnit.Miles)
        {
            totalDistanceTravelledText.text = UnitAndCoordinateConverter.KilometersToMiles(totalDistance).ToString(ThreeDecimalPlaces) + " mi";
        }
    }

    private void SetDistanceFromEarth(float fromEarth)
    {
        if (_currentLengthUnit == LengthUnit.Kilometers)
        {
            distanceFromEarthText.text = fromEarth.ToString(ThreeDecimalPlaces) + " km";
        } else if (_currentLengthUnit == LengthUnit.Miles)
        {
            distanceFromEarthText.text = UnitAndCoordinateConverter.KilometersToMiles(fromEarth).ToString(ThreeDecimalPlaces) + " mi";
        }
    }

    private void SetDistanceFromMoon(float fromMoon)
    {
        if (_currentLengthUnit == LengthUnit.Kilometers)
        {
            distanceFromMoonText.text = fromMoon.ToString(ThreeDecimalPlaces) + " km";
        }
        else if (_currentLengthUnit == LengthUnit.Miles)
        {
            distanceFromMoonText.text = UnitAndCoordinateConverter.KilometersToMiles(fromMoon).ToString(ThreeDecimalPlaces) + " mi";
        }
    }
    
    private void SetDistances(float totalDistance, float fromEarth, float fromMoon)
    {
        SetTotalDistance(totalDistance);
        SetDistanceFromEarth(fromEarth);
        SetDistanceFromMoon(fromMoon);
    }
    #endregion

    #region Manage Antennas
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
    #endregion

    #region UI Visibility
    private void HandleUIVisibility()
    {
        if (Input.anyKey || Input.mousePosition != _lastMousePosition)
        {
            _inactivityTimer = 0f;
            _isFadingOut = false;
            StartCoroutine(FadeUIIn());
        }
        else
        {
            _inactivityTimer += Time.deltaTime;

            if (_inactivityTimer >= inputInactivityTime && canvasGroup.alpha > 0)
            {
                _isFadingOut = true;
            }
        }

        if (_isFadingOut)
        {
            FadeUIOut();
        }

        _lastMousePosition = Input.mousePosition;
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
    #endregion

    private enum LengthUnit {
        Kilometers,
        Miles
    }
}
