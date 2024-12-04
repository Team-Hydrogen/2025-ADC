using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Antennas")]
    [SerializeField] private Transform antennasGrid;
    [SerializeField] private List<string> antennaNames = new();
    [SerializeField] private List<Transform> antennaLabelObjects = new();
    [SerializeField] private List<Color> enabledAntennaBackgroundColors = new()
    {
        new Color(0.2588f, 0.6824f, 0.9451f),
        new Color(0.5451f, 0.9294f, 0.1804f),
        new Color(1.0000f, 0.7569f, 0.0000f),
        new Color(0.9373f, 0.2588f, 0.2588f),
    };
    [SerializeField] private Color disabledAntennaBackgroundColor = new(0.8431f, 0.8510f, 0.9098f);

    [Header("Time Counter")]
    [SerializeField] private GameObject timeCounter;
    [SerializeField] private TextMeshProUGUI dayCounter;
    [SerializeField] private TextMeshProUGUI hourCounter;
    [SerializeField] private TextMeshProUGUI minuteCounter;
    [SerializeField] private TextMeshProUGUI secondCounter;
    
    [Header("Time Elapsed Bar")]
    [SerializeField] private GameObject timeElapsedBar;

    [Header("Coordinate")]
    [SerializeField] private TextMeshProUGUI xCoordinate;
    [SerializeField] private TextMeshProUGUI yCoordinate;
    [SerializeField] private TextMeshProUGUI zCoordinate;

    [Header("Distance")]
    [SerializeField] private TextMeshProUGUI totalDistanceTravelledText;
    [SerializeField] private TextMeshProUGUI distanceFromEarthText;
    [SerializeField] private TextMeshProUGUI distanceFromMoonText;

    [Header("Mission Stage")]
    [SerializeField] private TextMeshProUGUI missionStageText;
    
    [Header("Notification")]
    [SerializeField] private GameObject notification;
    [SerializeField] private TextMeshProUGUI notificationText;

    [Header("UI Settings")]
    [SerializeField] private float uiFadeSpeed;
    [SerializeField] private float inputInactivityTime;
    [SerializeField, Range(0, 1f)] private float minimumUIVisibility;

    private Vector3 _lastMousePosition;
    private float _inactivityTimer = 0f;
    private bool _isFadingOut = false;

    private UnitSystem _currentLengthUnit = UnitSystem.Metric;
    private const string ConnectionSpeedUnit = "kbps";
    private const string NoDecimalPlaces = "N0";
    private const string ThreeDecimalPlaces = "N3";
    
    private readonly List<string> _disabledAntennas = new();

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
        //SimulationManager.OnDataUpdated += UpdateUIFromData;
        //SimulationManager.OnMissionStageUpdated += UpdateMissionStage;
        SatelliteManager.OnUpdateTime += UpdateTimeFromMinutes;
        SatelliteManager.OnDistanceCalculated += UpdateUIDistances;
        SatelliteManager.OnUpdateCoordinates += UpdateCoordinatesText;
        DataManager.OnMissionStageUpdated += UpdateMissionStage;
    }

    private void OnDisable()
    {
        //SimulationManager.OnDataUpdated -= UpdateUIFromData;
        //SimulationManager.OnMissionStageUpdated -= UpdateMissionStage;
        SatelliteManager.OnUpdateTime -= UpdateTimeFromMinutes;
        SatelliteManager.OnDistanceCalculated -= UpdateUIDistances;
        SatelliteManager.OnUpdateCoordinates -= UpdateCoordinatesText;
        DataManager.OnMissionStageUpdated += UpdateMissionStage;
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

    public void ToggleTimeElapsedBar(bool isBarEnabled)
    {
        timeElapsedBar.SetActive(isBarEnabled);
        timeCounter.SetActive(!isBarEnabled);
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

    //private void UpdateUIFromData(int currentIndex)
    //{
    //    UpdateCoordinatesFromData(currentIndex);
    //    UpdateTimeFromData(currentIndex);
    //    UpdateAntennaFromData(currentIndex);
        
    //    // Notifications
    //    var currentTime = float.Parse(SimulationManager.Instance.nominalTrajectoryDataValues[currentIndex][0]); 
    //    const float secondStageFireTime = 5_000.0f;
    //    const float serviceModuleFireTime = 10_000.0f;
    //    if (Mathf.Approximately(currentTime, secondStageFireTime))
    //    {
    //        ShowNotification("Second Stage Fired");
    //    }
    //    if (Mathf.Approximately(currentTime, serviceModuleFireTime))
    //    {
    //        ShowNotification("Service Module Fired");
    //    }
    //}
    
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

    private void UpdateTimeFromMinutes(float timeInMinutes)
    {
        const int minutesPerDay = 1440;
        const int minutesPerHour = 60;
        const int secondsPerMinute = 60;

        float minutesLeft = timeInMinutes;

        int days = Mathf.FloorToInt(minutesLeft / minutesPerDay);
        minutesLeft %= minutesPerDay;
        int hours = Mathf.FloorToInt(minutesLeft / minutesPerHour);
        minutesLeft %= minutesPerHour;
        int minutes = Mathf.FloorToInt(minutesLeft);
        minutesLeft -= minutes;
        int seconds = Mathf.FloorToInt(minutesLeft * secondsPerMinute);

        SetTime(days, hours, minutes, seconds);
        UpdateTimeElapsedBar();
    }

    private void UpdateTimeElapsedBar()
    {
        var bar = timeElapsedBar.transform.GetChild(0);
        foreach (Transform stageSection in bar)
        {
            var stageSectionTransform = (RectTransform)stageSection;
            stageSectionTransform.sizeDelta = new Vector2(100, stageSectionTransform.sizeDelta.y);
        }
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
    private void UpdateCoordinatesText(Vector3 position)
    {
        string units;
        
        if (_currentLengthUnit == UnitSystem.Metric)
        {
            units = " km";
            xCoordinate.text = position.x.ToString("N0") + units;
            yCoordinate.text = position.y.ToString("N0") + units;
            zCoordinate.text = position.z.ToString("N0") + units;
        } else if (_currentLengthUnit == UnitSystem.Imperial)
        {
            units = " mi";
            xCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(position.x).ToString("N0") + units;
            yCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(position.y).ToString("N0") + units;
            zCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(position.z).ToString("N0") + units;
        }
    }
    
    //private void UpdateCoordinatesFromData(int currentIndex)
    //{
    //    float x;
    //    float y;
    //    float z;

    //    try
    //    {
    //        x = float.Parse(SimulationManager.Instance.nominalTrajectoryDataValues[currentIndex][1]);
    //        y = float.Parse(SimulationManager.Instance.nominalTrajectoryDataValues[currentIndex][2]);
    //        z = float.Parse(SimulationManager.Instance.nominalTrajectoryDataValues[currentIndex][3]);
    //    }
    //    catch
    //    {
    //        Debug.LogWarning("No positional data available!");
    //        return;
    //    }
        
    //    SetCoordinates(x, y, z);
    //}
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

    //private void UpdateAntennaFromData(int currentIndex)
    //{
    //    var currentLinkBudgetData = SimulationManager.Instance.linkBudgetDataValues[currentIndex];
    //    UpdateAntenna(currentLinkBudgetData[1], float.Parse(currentLinkBudgetData[2]));
    //    PrioritizeAntennas();
    //    ColorAntennas();
    //}

    private void UpdateAntenna(string antennaName, float connectionSpeed)
    {
        // Gets the index of the antenna name and maps it to its text object.
        var antennaIndex = antennaNames.IndexOf(antennaName);
        var antennaLabel = antennaLabelObjects[antennaIndex];
        var antennaBackground = antennaLabel.GetComponentInChildren<UnityEngine.UI.Image>();
        
        // The connection speed and units text is fetched and updated.
        var antennaTexts = antennaLabel.GetComponentsInChildren<TextMeshProUGUI>();
        var connectionSpeedText = antennaTexts[1];
        var unitsText = antennaTexts[2];
        
        connectionSpeedText.text = connectionSpeed.ToString(NoDecimalPlaces);
        unitsText.text = $" {ConnectionSpeedUnit}";

        switch (connectionSpeed)
        {
            case 0 when !_disabledAntennas.Contains(antennaName):
                _disabledAntennas.Add(antennaName);
                antennaBackground.color = disabledAntennaBackgroundColor;
                break;
            case > 0 when _disabledAntennas.Contains(antennaName):
                _disabledAntennas.Remove(antennaName);
                break;
        }
    }

    /// <summary>
    /// Reorders antenna labels by distance, by changing hierarchy.
    /// </summary>
    private void PrioritizeAntennas()
    {
        var childCount = antennasGrid.childCount;
        var antennaLabels = new Transform[childCount];

        for (var i = 0; i < childCount; i++)
        {
            antennaLabels[i] = antennasGrid.GetChild(i);
        }

        var sortedLabels = antennaLabels
            .Select(antennaLabel => new
            {
                Label = antennaLabel,
                ConnectionSpeed = float.TryParse(
                    antennaLabel.GetComponentsInChildren<TextMeshProUGUI>()[1].text, out float speed)
                        ? speed : float.MinValue
            })
            .OrderByDescending(item => item.ConnectionSpeed)
            .Select(item => item.Label)
            .ToList();

        foreach (var label in sortedLabels)
        {
            label.SetSiblingIndex(sortedLabels.IndexOf(label));
        }
    }

    private void ColorAntennas()
    {
        var index = 0;
        foreach (Transform antennaBackground in antennasGrid)
        {
            antennaBackground.GetComponent<UnityEngine.UI.Image>().color = index < 4 - _disabledAntennas.Count
                ? enabledAntennaBackgroundColors[_disabledAntennas.Count]
                : disabledAntennaBackgroundColor;
            index++;
        }
    }
    
    #endregion

    private void UpdateMissionStage(MissionStage stage)
    {
        missionStageText.text = stage.name;
        missionStageText.color = stage.color;
    }

    private void ShowNotification(string text)
    {
        notification.SetActive(true);
        notificationText.text = text;
    }

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
