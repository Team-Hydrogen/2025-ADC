using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Antennas")]
    [SerializeField] private Transform antennasGrid;
    [SerializeField] private List<string> antennaNames = new();
    [SerializeField] private List<Transform> antennaLabelObjects = new();
    [SerializeField] private List<Color> antennaBackgroundColor = new();
    [SerializeField] private Color disabledAntennaBackgroundColor = new(0.8f, 0.8f, 0.8f);

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
    [SerializeField, Range(0, 1f)] private float minimumUIVisibility;
    
    public static UIManager Instance { get; private set; }

    private Vector3 _lastMousePosition;
    private float _inactivityTimer = 0f;
    private bool _isFadingOut = false;

    private UnitSystem _currentLengthUnit = UnitSystem.Metric;
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
    #endregion

    public void RestartButtonPressed()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitButtonPressed()
    {
        SceneManager.LoadScene(0);
    }

    public void ImperialButtonPressed()
    {
        _currentLengthUnit = UnitSystem.Imperial;
    }

    public void MetricButtonPressed()
    {
        _currentLengthUnit = UnitSystem.Metric;
    }
    #endregion

    private void UpdateUIFromData(int currentIndex)
    {
        UpdateCoordinatesFromData(currentIndex);
        UpdateTimeFromData(currentIndex);
        UpdateAntennaFromData(currentIndex);
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
    
    private void UpdateTimeFromData(int currentIndex)
    {
        const int minutesPerDay = 1440;
        const int minutesPerHour = 60;
        const int secondsPerMinute = 60;
        
        float totalTimeInMinutes;
        try
        {
            totalTimeInMinutes = float.Parse(DataManager.trajectoryDataValues[currentIndex][0]);
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
        
        if (_currentLengthUnit == UnitSystem.Metric)
        {
            units = " km";
            xCoordinate.text = x.ToString("N0") + units;
            yCoordinate.text = y.ToString("N0") + units;
            zCoordinate.text = z.ToString("N0") + units;
        } else if (_currentLengthUnit == UnitSystem.Imperial)
        {
            units = " mi";
            xCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(x).ToString("N0") + units;
            yCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(y).ToString("N0") + units;
            zCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(z).ToString("N0") + units;
        }
    }
    
    private void UpdateCoordinatesFromData(int currentIndex)
    {
        float x;
        float y;
        float z;

        try
        {
            x = float.Parse(DataManager.trajectoryDataValues[currentIndex][1]);
            y = float.Parse(DataManager.trajectoryDataValues[currentIndex][2]);
            z = float.Parse(DataManager.trajectoryDataValues[currentIndex][3]);
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
        if (_currentLengthUnit == UnitSystem.Metric)
        {
            totalDistanceTravelledText.text = totalDistance.ToString(ThreeDecimalPlaces) + " km";
        }
        else if (_currentLengthUnit == UnitSystem.Imperial)
        {
            totalDistanceTravelledText.text = UnitAndCoordinateConverter.KilometersToMiles(totalDistance).ToString(ThreeDecimalPlaces) + " mi";
        }
    }

    private void SetDistanceFromEarth(float fromEarth)
    {
        if (_currentLengthUnit == UnitSystem.Metric)
        {
            distanceFromEarthText.text = fromEarth.ToString(ThreeDecimalPlaces) + " km";
        } else if (_currentLengthUnit == UnitSystem.Imperial)
        {
            distanceFromEarthText.text = UnitAndCoordinateConverter.KilometersToMiles(fromEarth).ToString(ThreeDecimalPlaces) + " mi";
        }
    }

    private void SetDistanceFromMoon(float fromMoon)
    {
        if (_currentLengthUnit == UnitSystem.Metric)
        {
            distanceFromMoonText.text = fromMoon.ToString(ThreeDecimalPlaces) + " km";
        }
        else if (_currentLengthUnit == UnitSystem.Imperial)
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

    private void UpdateAntennaFromData(int currentIndex)
    {
        UpdateAntenna(
            DataManager.linkBudgetDataValues[currentIndex][1],
            float.Parse(DataManager.linkBudgetDataValues[currentIndex][2])
        );
    }

    private void UpdateAntenna(string antennaName, float connectionSpeed)
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
            titleText.color = disabledAntennaBackgroundColor;
            antennaLabel.GetComponentInChildren<Image>().color = disabledAntennaBackgroundColor;

            for (int i = 1; i < antennaTexts.Length; i++)
            {
                // antennaTexts[i].color = disabledAntennaBackgroundColor;
                antennaTexts[i].text = "";
            }

            return;
        }

        // foreach (var text in antennaTexts)
        // {
        //     text.color = antennaBackgroundColor[antennaIndex];
        // }

        colonText.text = ":";
        connectionSpeedText.text = connectionSpeed.ToString("N0");
        unitsText.text = " " + connectionSpeedUnit;
        antennaLabel.GetComponentInChildren<Image>().color = antennaBackgroundColor[antennaIndex];

        PrioritizeAntennas();
    }

    /// <summary>
    /// Reorders antenna labels by distance, by changing hierarchy.
    /// </summary>
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
            _inactivityTimer = 0.0f;
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
        canvasGroup.alpha = Mathf.Max(minimumUIVisibility, canvasGroup.alpha - uiFadeSpeed * Time.deltaTime);
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

    private enum UnitSystem {
        Metric,
        Imperial
    }
}
