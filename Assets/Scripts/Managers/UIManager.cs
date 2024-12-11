using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance { get; private set; }
    
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
    
    [Header("Coordinates")]
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
    
    [Header("Machine Learning")]
    [SerializeField] private Button bumpOffCourseButton;
    [SerializeField] private TextMeshProUGUI thrustText;
    
    [Header("UI Settings")]
    [SerializeField] private float uiFadeSpeed;
    [SerializeField] private float inputInactivityTime;
    [SerializeField, Range(0, 1f)] private float minimumUIVisibility;
    
    private bool _isAntennaColored = true;
    private bool _isAntennaPrioritized = true;
    
    private Vector3 _lastMousePosition;
    private float _inactivityTimer = 0.0f;
    private bool _isFadingOut = false;
    
    // Measurement variables
    private UnitSystem _currentLengthUnit = UnitSystem.Metric;
    
    private const float MaximumConnectionSpeed = 10_000.0f;
    private const string ConnectionSpeedUnit = "kbps";
    
    private readonly List<string> _disabledAntennas = new();
    
    public static event Action OnBumpOffCoursePressed;
    public static event Action<SatelliteManager.SatelliteState> OnCurrentPathChanged;

    private List<string[]> _linkBudgetData;
    private List<string[]> _offnominalLinkBudgetData;
    private List<string[]> _thrustData;
    private SatelliteManager.SatelliteState _satelliteState;
    
    #region Event Functions
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Update()
    {
        HandleUIVisibility();
    }
    
    private void OnEnable()
    {
        SatelliteManager.OnUpdateTime += UpdateTimeFromMinutes;
        SatelliteManager.OnDistanceCalculated += UpdateDistances;
        SatelliteManager.OnUpdateCoordinates += UpdateCoordinatesText;
        SatelliteManager.OnCurrentIndexUpdated += UpdateAntennasFromData;
        SatelliteManager.OnCurrentIndexUpdated += UpdateThrust;
        SatelliteManager.OnStageFired += ShowNotification;
        SatelliteManager.OnSatelliteStateUpdated += UpdateSatelliteState;
        DataManager.OnDataLoaded += OnDataLoaded;
        DataManager.OnMissionStageUpdated += UpdateMissionStage;
        DataManager.OnMissionStageUpdated += SetBumpOffCourseButtonActive;
    }
    
    private void OnDisable()
    {
        SatelliteManager.OnUpdateTime -= UpdateTimeFromMinutes;
        SatelliteManager.OnDistanceCalculated -= UpdateDistances;
        SatelliteManager.OnUpdateCoordinates -= UpdateCoordinatesText;
        SatelliteManager.OnCurrentIndexUpdated -= UpdateAntennasFromData;
        SatelliteManager.OnCurrentIndexUpdated -= UpdateThrust;
        SatelliteManager.OnStageFired -= ShowNotification;
        SatelliteManager.OnSatelliteStateUpdated -= UpdateSatelliteState;
        DataManager.OnDataLoaded -= OnDataLoaded;
        DataManager.OnMissionStageUpdated -= UpdateMissionStage;
        DataManager.OnMissionStageUpdated -= SetBumpOffCourseButtonActive;
    }
    
    #endregion
    
    
    #region Timeline Controls
    
    public void PlayButtonPressed()
    {
        Time.timeScale = 1f;
    }

    public void PauseButtonPressed()
    {
        Time.timeScale = 0f;
    }
    
    public void RestartButtonPressed()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitButtonPressed()
    {
        SceneManager.LoadScene(0);
    }
    
    #endregion
    
    
    #region Actions Panel
    
    #region Settings
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
    
    #region Machine Learning
    
    private void SetBumpOffCourseButtonActive(MissionStage missionStage)
    {
        bumpOffCourseButton.interactable = missionStage.stageType is MissionStage.StageTypes.TravellingToMoon 
            or MissionStage.StageTypes.ReturningToEarth;
    }
    
    public void BumpOffCourseButtonPressed()
    {
        OnBumpOffCoursePressed?.Invoke();
    }
    
    #endregion
    
    #endregion
    
    
    #region Time Counter and Elapsed Bar
    
    private void UpdateTimeFromMinutes(float timeInMinutes)
    {
        const int minutesPerDay = 1440;
        const int minutesPerHour = 60;
        const int secondsPerMinute = 60;

        var minutesLeft = timeInMinutes;

        var days = Mathf.FloorToInt(minutesLeft / minutesPerDay);
        minutesLeft %= minutesPerDay;
        var hours = Mathf.FloorToInt(minutesLeft / minutesPerHour);
        minutesLeft %= minutesPerHour;
        var minutes = Mathf.FloorToInt(minutesLeft);
        minutesLeft -= minutes;
        var seconds = Mathf.FloorToInt(minutesLeft * secondsPerMinute);

        SetTimeCounter(days, hours, minutes, seconds);
        SetTimeElapsedBar(timeInMinutes);
    }
    
    private void SetTimeCounter(int days, int hours, int minutes, int seconds)
    {
        const int maxNumberLength = 2;
        dayCounter.text = days.ToString().PadLeft(maxNumberLength, '0');
        hourCounter.text = hours.ToString().PadLeft(maxNumberLength, '0');
        minuteCounter.text = minutes.ToString().PadLeft(maxNumberLength, '0');
        secondCounter.text = seconds.ToString().PadLeft(maxNumberLength, '0');
    }
    
    private void SetTimeElapsedBar(float timeInMinutes)
    {
        var bar = timeElapsedBar.transform.GetChild(0);
        var barWidth = ((RectTransform)bar.transform).sizeDelta.x;
        var barXMargin = bar.GetComponent<HorizontalLayoutGroup>().padding.horizontal;
        var barContentWidth = barWidth - barXMargin;
        
        var stageIndex = (int) DataManager.instance.currentMissionStage.stageType - 1;
        
        var stageSection = bar.transform.GetChild(stageIndex);
        var stageSectionTransform = (RectTransform)stageSection;
        var stageSectionWidth = timeInMinutes / 12983.16998f * barContentWidth 
                                - stageSectionTransform.anchoredPosition.x + barXMargin / 2.0f;
        
        stageSectionTransform.sizeDelta = new Vector2(stageSectionWidth, stageSectionTransform.sizeDelta.y);
    }
    
    /// <summary>
    /// This function will be deprecated before the application is sent to production.
    /// </summary>
    /// <param name="changeInDays"></param>
    /// <param name="changeInHours"></param>
    /// <param name="changeInMinutes"></param>
    /// <param name="changeInSeconds"></param>
    public void IncrementTime(int changeInDays, int changeInHours, int changeInMinutes, int changeInSeconds)
    {
        dayCounter.text = (int.Parse(dayCounter.text) - changeInDays).ToString();
        hourCounter.text = (int.Parse(hourCounter.text) - changeInHours).ToString();
        minuteCounter.text = (int.Parse(minuteCounter.text) - changeInMinutes).ToString();
        secondCounter.text = (int.Parse(secondCounter.text) - changeInSeconds).ToString();
    }
    
    #endregion
    
    
    #region Coordinates
    
    private void UpdateCoordinatesText(Vector3 position)
    {
        string units;

        switch (_currentLengthUnit)
        {
            case UnitSystem.Metric:
                units = " km";
                xCoordinate.text = position.x.ToString("N0") + units;
                yCoordinate.text = position.y.ToString("N0") + units;
                zCoordinate.text = position.z.ToString("N0") + units;
                break;
            case UnitSystem.Imperial:
                units = " mi";
                xCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(position.x).ToString("N0") + units;
                yCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(position.y).ToString("N0") + units;
                zCoordinate.text = UnitAndCoordinateConverter.KilometersToMiles(position.z).ToString("N0") + units;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    #endregion
    
    
    #region Distances
    
    private void SetTotalDistance(float totalDistance)
    {
        totalDistanceTravelledText.text = _currentLengthUnit switch
        {
            UnitSystem.Metric => $"{totalDistance:F3} km",
            UnitSystem.Imperial => $"{UnitAndCoordinateConverter.KilometersToMiles(totalDistance):F3} mi",
            _ => totalDistanceTravelledText.text
        };
    }

    private void SetDistanceFromEarth(float fromEarth)
    {
        distanceFromEarthText.text = _currentLengthUnit switch
        {
            UnitSystem.Metric => $"{fromEarth:F3} km",
            UnitSystem.Imperial => $"{UnitAndCoordinateConverter.KilometersToMiles(fromEarth):F3} mi",
            _ => distanceFromEarthText.text
        };
    }

    private void SetDistanceFromMoon(float fromMoon)
    {
        distanceFromMoonText.text = _currentLengthUnit switch
        {
            UnitSystem.Metric => $"{fromMoon:F3} km",
            UnitSystem.Imperial => $"{UnitAndCoordinateConverter.KilometersToMiles(fromMoon):F3} mi",
            _ => distanceFromMoonText.text
        };
    }
    
    private void UpdateDistances(DistanceTravelledEventArgs distances)
    {
        SetTotalDistance(distances.TotalDistance);
        SetDistanceFromEarth(distances.DistanceFromEarth);
        SetDistanceFromMoon(distances.DistanceFromMoon);
    }
    
    #endregion
    
    
    #region Antennas and Link Budget
    
    public void ToggleAntennaColors(bool isAntennaColored)
    {
        _isAntennaColored = isAntennaColored;
    }
    
    public void ToggleAntennaPrioritization(bool isAntennaPrioritized)
    {
        _isAntennaPrioritized = isAntennaPrioritized;
    }
    
    private void UpdateAntennasFromData(int currentIndex)
    {
        var currentLinkBudget = new float[antennaNames.Count];
        
        var currentLinkBudgetValues = _linkBudgetData[currentIndex][18..22];
        for (var antennaIndex = 0; antennaIndex < currentLinkBudgetValues.Length; antennaIndex++)
        {
            var antennaLinkBudgetValue = float.Parse(currentLinkBudgetValues[antennaIndex]);
            currentLinkBudget[antennaIndex] = antennaLinkBudgetValue > MaximumConnectionSpeed
                ? MaximumConnectionSpeed : antennaLinkBudgetValue;
        }
        
        // Updates each antenna with the latest link budget value.
        for (var antennaIndex = 0; antennaIndex < antennaNames.Count; antennaIndex++)
        {
            UpdateAntenna(antennaNames[antennaIndex], currentLinkBudget[antennaIndex]);
        }
        
        if (_isAntennaPrioritized)
        {
            PrioritizeAntennas();
        }
        if (_isAntennaColored)
        {
            ColorAntennas();
        }
    }

    private void UpdateAntenna(string antennaName, float connectionSpeed = 0.0f)
    {
        // Gets the index of the antenna name and maps it to its text object.
        var antennaIndex = antennaNames.IndexOf(antennaName);
        var antennaLabel = antennaLabelObjects[antennaIndex];
        var antennaBackground = antennaLabel.GetComponentInChildren<UnityEngine.UI.Image>();
        
        // The connection speed and units text is fetched and updated.
        var antennaTexts = antennaLabel.GetComponentsInChildren<TextMeshProUGUI>();
        var connectionSpeedText = antennaTexts[1];
        var unitsText = antennaTexts[2];
        
        connectionSpeedText.text = $"{connectionSpeed:F0}";
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
        
        for (var index = 0; index < childCount; index++)
        {
            var antennaLabel = antennasGrid.GetChild(index);
            antennaLabels[index] = antennaLabel;
        }
        
        var sortedLabels = antennaLabels
            .Select(antennaLabel => new
            {
                Label = antennaLabel,
                ConnectionSpeed = float.TryParse(
                    antennaLabel.GetComponentsInChildren<TextMeshProUGUI>()[1].text, out var speed)
                        ? speed : float.MinValue,
                PriorityWeight = antennaLabel.GetComponentsInChildren<TextMeshProUGUI>()[0].text
                                  == DataManager.instance.currentPrioritizedAntenna ? 1.0f : 0.0f,
                Name = antennaLabel.GetComponentsInChildren<TextMeshProUGUI>()[0].text,
            })
            .OrderByDescending(item => item.PriorityWeight)
            .ThenByDescending(item => item.ConnectionSpeed)
            .ThenBy(item => item.Name)
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
    
    
    private void OnDataLoaded(DataLoadedEventArgs dataLoadedEventArgs)
    {
        UpdateMissionStage(dataLoadedEventArgs.MissionStage);
        SetBumpOffCourseButtonActive(dataLoadedEventArgs.MissionStage);
        _linkBudgetData = dataLoadedEventArgs.LinkBudgetData;
        _offnominalLinkBudgetData = dataLoadedEventArgs.OffnominalLinkBudgetData;
        _thrustData = dataLoadedEventArgs.ThrustData;
    }

    #region Mission Stage
    private void UpdateMissionStage(MissionStage stage)
    {
        missionStageText.text = stage.name;
        missionStageText.color = stage.color;
    }

    #endregion

    #region thrust

    private void UpdateThrust(int index)
    {
        Vector3 thrust = new Vector3(float.Parse(_thrustData[index][23]), float.Parse(_thrustData[index][24]), float.Parse(_thrustData[index][25]));
        float magnitude = thrust.magnitude;

        if (_satelliteState == SatelliteManager.SatelliteState.OffNominal)
        {
            thrustText.text = $"{magnitude:f3} N";
        }
    }

    #endregion

    private void UpdateSatelliteState(SatelliteManager.SatelliteState state)
    {
        _satelliteState = state;
        if (!(_satelliteState == SatelliteManager.SatelliteState.OffNominal))
        {
            thrustText.text = "";
        }
    }


    #region Notifications

    private void ShowNotification(string text)
    {
        notification.SetActive(true);
        notificationText.text = text;
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

    public void NominalTogglePressed()
    {
        OnCurrentPathChanged?.Invoke(SatelliteManager.SatelliteState.Nominal);
    }

    public void OffnominalTogglePressed()
    {
        OnCurrentPathChanged?.Invoke(SatelliteManager.SatelliteState.OffNominal);
    }
    
    private enum UnitSystem {
        Metric,
        Imperial
    }
}