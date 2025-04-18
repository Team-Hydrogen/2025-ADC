using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    #region Variables
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
    
    [SerializeField] private TMP_Dropdown prioritizationMethod;

    [SerializeField] private GameObject dataPanel;
    [SerializeField] private GameObject actionsPanel;
    [SerializeField] private Button dataPanelButton;
    [SerializeField] private Button actionsPanelButton;
    [SerializeField] private GameObject dataPanelEnabledBar;
    [SerializeField] private GameObject actionsPanelEnabledBar;

    [Header("Timeline")]
    [SerializeField] private GameObject timeCounter;
    [SerializeField] private TextMeshProUGUI dayCounter;
    [SerializeField] private TextMeshProUGUI hourCounter;
    [SerializeField] private TextMeshProUGUI minuteCounter;
    [SerializeField] private TextMeshProUGUI secondCounter;
    [Space]
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject pauseButton;
    [Space]
    [SerializeField] private GameObject timeElapsedBar;
    [SerializeField] private TextMeshProUGUI timeScaleIndicator;

    [Header("Color Key")]
    [SerializeField] private Toggle colorKeyToggle;
    [SerializeField] private GameObject colorKey;
    
    [Header("Spacecraft")]
    [SerializeField] private TextMeshProUGUI spacecraftMass;
    
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
    [SerializeField] private Transform notificationParent;
    [SerializeField] private GameObject notificationPrefab;
    
    [Header("Intelligence")]
    [SerializeField] private GameObject thrustSection;
    [SerializeField] private TextMeshProUGUI thrustText;
    [SerializeField] private Button transitionPathButton;
    [SerializeField] private Button bumpOffCoursePathButton;
    
    [Header("UI Settings")]
    [SerializeField] private float uiFadeSpeed;
    [SerializeField] private float inputInactivityTime;
    [SerializeField, Range(0, 1f)] private float minimumUIVisibility;
    
    // Timeline controls
    private Transform _bar;
    private float _barXMargin;
    
    private bool _isAntennaColored = true;
    
    // UI inactivity
    private Vector3 _lastMousePosition;
    private float _inactivityTimer = 0.0f;
    private bool _isFadingOut = false;
    private bool _shouldUiFadeOut = false;
    public bool isUiHidden { get; private set; } = false;

    // Measurement variables
    private UnitSystem _currentLengthUnit = UnitSystem.Metric;
    
    private const float MaximumConnectionSpeed = 10_000.0f;
    private const string ConnectionSpeedUnit = "kbps";
    
    private readonly List<string> _disabledAntennas = new();
    
    // Timeline actions
    public static event Action IncreaseTimeScale;
    public static event Action DecreaseTimeScale;
    public static event Action IncreaseTime;
    public static event Action DecreaseTime;

    public static event Action PauseTime;
    public static event Action ResumeTime;
    // Actions panel
    public static event Action<SpacecraftManager.SpacecraftState> TrajectorySelected;
    public static event Action<int> PrioritizationAlgorithmSelected;
    public static event Action TransitionPath;
    public static event Action BumpOffCourse;

    private string[][] _nominalLinkBudgetData;
    private string[][] _offNominalLinkBudgetData;
    private string[][] _thrustData;
    private SpacecraftManager.SpacecraftState _spacecraftState;

    private bool _showedStageFiredNotification = false;

    #endregion

    #region Event Functions

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        _bar = timeElapsedBar.transform.GetChild(0);
        _barXMargin = _bar.GetComponent<HorizontalLayoutGroup>().padding.horizontal;
        
        PrioritizationAlgorithmSelected?.Invoke(prioritizationMethod.value);

        ShowNotification("Show Color Key?", Notification.NotificationType.AskYesNo, ShowColorKey);
    }

    private void Update()
    {
        HandleUIVisibility();
    }
    
    private void OnEnable()
    {
        DataManager.CoordinatesUpdated += UpdateCoordinatesText;
        DataManager.DataIndexUpdated += UpdateAntennasFromData;
        DataManager.DataIndexUpdated += UpdateThrust;
        DataManager.DataLoaded += DataLoaded;
        DataManager.MissionStageUpdated += UpdateMissionStage;
        DataManager.ShowNotification += ShowStageFiredNotification;
        DataManager.SpacecraftMassUpdated += SetSpacecraftMassUpdated;
        DataManager.TotalDistanceTraveledUpdated += SetTotalDistance;
        
        SimulationManager.ElapsedTimeUpdated += UpdateTimeFromMinutes;
        SimulationManager.TimeScaleSet += OnTimeScaleSet;

        SpacecraftManager.DistancesUpdated += UpdateDistances;
        SpacecraftManager.SpacecraftStateUpdated += UpdateSpacecraftState;

        InputManager.OnSwitchActionsPanel += ShowActionsPanel;
        InputManager.OnSwitchDataPanel += ShowDataPanel;
        InputManager.OnToggleUIVisibility += ToggleUiVisibility;
    }
    
    private void OnDisable()
    {
        DataManager.CoordinatesUpdated -= UpdateCoordinatesText;
        DataManager.DataIndexUpdated -= UpdateAntennasFromData;
        DataManager.DataIndexUpdated -= UpdateThrust;
        DataManager.DataLoaded -= DataLoaded;
        DataManager.MissionStageUpdated -= UpdateMissionStage;
        DataManager.ShowNotification -= ShowStageFiredNotification;
        DataManager.SpacecraftMassUpdated -= SetSpacecraftMassUpdated;
        DataManager.TotalDistanceTraveledUpdated -= SetTotalDistance;
        
        SimulationManager.ElapsedTimeUpdated -= UpdateTimeFromMinutes;
        SimulationManager.TimeScaleSet -= OnTimeScaleSet;

        SpacecraftManager.DistancesUpdated -= UpdateDistances;
        SpacecraftManager.SpacecraftStateUpdated -= UpdateSpacecraftState;

        InputManager.OnSwitchActionsPanel -= ShowActionsPanel;
        InputManager.OnSwitchDataPanel -= ShowDataPanel;
        InputManager.OnToggleUIVisibility -= ToggleUiVisibility;
    }

    #endregion


    #region Timeline
    #region Timeline Controls

    public void PlayButtonPressed()
    {
        ResumeTime?.Invoke();
    }

    public void PauseButtonPressed()
    {
        PauseTime?.Invoke();
    }
    
    public void SpeedUpButtonPressed()
    {
        IncreaseTimeScale?.Invoke();
    }

    public void SlowDownButtonPressed()
    {
        DecreaseTimeScale?.Invoke();
    }

    public void SkipForwardButtonPressed()
    {
        IncreaseTime?.Invoke();
    }

    public void SkipBackwardButtonPressed()
    {
        DecreaseTime?.Invoke();
    }

    public void RestartButtonPressed()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitButtonPressed()
    {
        LoadingSceneManager.sceneToLoad = 1;
        SceneManager.LoadScene(0);
    }

    #endregion
    
    
    #region Time Counter and Elapsed Bar

    private void UpdateTimeFromMinutes(float timeInMinutes)
    {
        const int minutesPerDay = 1440;
        const int minutesPerHour = 60;
        const int secondsPerMinute = 60;

        var minutesLeft = timeInMinutes;

        int days = Mathf.FloorToInt(minutesLeft / minutesPerDay);
        minutesLeft %= minutesPerDay;
        int hours = Mathf.FloorToInt(minutesLeft / minutesPerHour);
        minutesLeft %= minutesPerHour;
        int minutes = Mathf.FloorToInt(minutesLeft);
        minutesLeft -= minutes;
        int seconds = Mathf.FloorToInt(minutesLeft * secondsPerMinute);

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
        var barWidth = ((RectTransform)_bar.transform).sizeDelta.x;
        var barContentWidth = barWidth - _barXMargin;

        var stageIndex = (int)DataManager.Instance.CurrentMissionStage.stageType - 1;

        var stageSection = _bar.transform.GetChild(stageIndex);
        var stageSectionTransform = (RectTransform)stageSection;
        var stageSectionWidth = timeInMinutes / 12983.16998f * barContentWidth
                                - stageSectionTransform.anchoredPosition.x + _barXMargin / 2.0f;

        stageSectionTransform.sizeDelta = new Vector2(stageSectionWidth, stageSectionTransform.sizeDelta.y);
    }

    private void OnTimeScaleSet(float timeScale)
    {
        SetTimeScaleIndicator(timeScale);
        SetPausePlayButton();
    }

    /// <summary>
    /// Updates the time scale indicator
    /// </summary>
    /// <param name="timeScale">Current simulation time scale</param>
    private void SetTimeScaleIndicator(float timeScale)
    {
        // The time indicator will only appear if the timescale is neither 0 (paused) nor 1 (normal speed).
        bool isTimeIndicatorVisible = !Mathf.Approximately(timeScale, 0.0f) && !Mathf.Approximately(timeScale, 1.0f);

        timeScaleIndicator.text = isTimeIndicatorVisible
            ? $"{timeScale:F0}x"
            : "";
    }

    private void SetPausePlayButton()
    {
        if (Time.timeScale == 0.0f)
        {
            playButton.SetActive(true);
            pauseButton.SetActive(false);
        }
        else
        {
            playButton.SetActive(false);
            pauseButton.SetActive(true);
        }
    }

    #endregion
    #endregion


    #region Actions Panel

    #region Settings

    private void ShowColorKey()
    {
        ToggleColorKeyVisibility(true);
        colorKeyToggle.isOn = true;
    }

    public void HideColorKey()
    {
        ToggleColorKeyVisibility(false);
        colorKeyToggle.isOn = false;
    }

    public void ToggleColorKeyVisibility(bool isVisible)
    {
        colorKey.SetActive(isVisible);
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
    
    #region Trajectory Generation
    
    public void OnTransitionPathButtonPressed()
    {
        TransitionPath?.Invoke();
    }
    
    public void OnBumpOffCourseButtonPressed()
    {
        BumpOffCourse?.Invoke();
    }
    
    #endregion
    
    #endregion

    public void ShowActionsPanel()
    {
        dataPanel.SetActive(false);
        actionsPanel.SetActive(true);

        dataPanelButton.interactable = true;
        actionsPanelButton.interactable = false;

        actionsPanelEnabledBar.SetActive(true);
        dataPanelEnabledBar.SetActive(false);
    }

    public void ShowDataPanel()
    {
        actionsPanel.SetActive(false);
        dataPanel.SetActive(true);

        dataPanelButton.interactable = false;
        actionsPanelButton.interactable = true;

        actionsPanelEnabledBar.SetActive(false);
        dataPanelEnabledBar.SetActive(true);
    }
    
    
    #region Spacecraft

    private void SetSpacecraftMassUpdated(float massInKilograms)
    {
        spacecraftMass.text = _currentLengthUnit switch
        {
            UnitSystem.Metric => $"{massInKilograms:N3} kg",
            UnitSystem.Imperial => $"{UnitAndCoordinateConverter.KilogramsToPounds(massInKilograms):N3} lb",
            _ => spacecraftMass.text
        };
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
                xCoordinate.text = $"{position.x:N3} {units}";
                yCoordinate.text = $"{position.y:N3} {units}";
                zCoordinate.text = $"{position.z:N3} {units}";
                break;
            case UnitSystem.Imperial:
                units = " mi";
                xCoordinate.text = $"{UnitAndCoordinateConverter.KilometersToMiles(position.x):N3} {units}";
                yCoordinate.text = $"{UnitAndCoordinateConverter.KilometersToMiles(position.y):N3} {units}";
                zCoordinate.text = $"{UnitAndCoordinateConverter.KilometersToMiles(position.z):N3} {units}";
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
            UnitSystem.Metric => $"{totalDistance:N3} km",
            UnitSystem.Imperial => $"{UnitAndCoordinateConverter.KilometersToMiles(totalDistance):N3} mi",
            _ => totalDistanceTravelledText.text
        };
    }

    private void SetDistanceFromEarth(float fromEarth)
    {
        distanceFromEarthText.text = _currentLengthUnit switch
        {
            UnitSystem.Metric => $"{fromEarth:N3} km",
            UnitSystem.Imperial => $"{UnitAndCoordinateConverter.KilometersToMiles(fromEarth):N3} mi",
            _ => distanceFromEarthText.text
        };
    }

    private void SetDistanceFromMoon(float fromMoon)
    {
        distanceFromMoonText.text = _currentLengthUnit switch
        {
            UnitSystem.Metric => $"{fromMoon:N3} km",
            UnitSystem.Imperial => $"{UnitAndCoordinateConverter.KilometersToMiles(fromMoon):N3} mi",
            _ => distanceFromMoonText.text
        };
    }
    
    private void UpdateDistances(DistanceEventArgs distances)
    {
        SetDistanceFromEarth(distances.DistanceFromEarth);
        SetDistanceFromMoon(distances.DistanceFromMoon);
    }
    
    #endregion
    
    
    #region Antennas and Link Budget
    
    public void ToggleAntennaColors(bool isAntennaColored)
    {
        _isAntennaColored = isAntennaColored;
    }

    public void ToggleAntennaPrioritization(int selectedIndex)
    {
        PrioritizationAlgorithmSelected?.Invoke(prioritizationMethod.value);
    }

    private void UpdateAntennasFromData(int currentIndex)
    {
        float[] currentLinkBudget = new float[antennaNames.Count];

        string[][] linkBudgetData = _spacecraftState == SpacecraftManager.SpacecraftState.Nominal
            ? _nominalLinkBudgetData
            : _offNominalLinkBudgetData;
        string[] currentLinkBudgetValues = linkBudgetData[currentIndex][^4..^1];
        
        for (int antennaIndex = 0; antennaIndex < currentLinkBudgetValues.Length; antennaIndex++)
        {
            float antennaLinkBudgetValue = float.Parse(currentLinkBudgetValues[antennaIndex]);
            currentLinkBudget[antennaIndex] = Mathf.Min(antennaLinkBudgetValue, MaximumConnectionSpeed);
        }
        
        // Updates each antenna with the latest link budget value.
        for (int antennaIndex = 0; antennaIndex < antennaNames.Count; antennaIndex++)
        {
            UpdateAntenna(antennaNames[antennaIndex], currentLinkBudget[antennaIndex]);
        }
        
        PrioritizeAntennas();
        
        if (_isAntennaColored)
        {
            ColorAntennas();
        }
    }
    
    private void UpdateAntenna(string antennaName, float connectionSpeed = 0.0f)
    {
        // Gets the index of the antenna name and maps it to its text object.
        int antennaIndex = antennaNames.IndexOf(antennaName);
        Transform antennaLabel = antennaLabelObjects[antennaIndex];
        Image antennaBackground = antennaLabel.GetComponentInChildren<Image>();
        
        // The connection speed and units text is fetched and updated.
        TextMeshProUGUI[] antennaTexts = antennaLabel.GetComponentsInChildren<TextMeshProUGUI>();
        TextMeshProUGUI connectionSpeedText = antennaTexts[1];
        TextMeshProUGUI unitsText = antennaTexts[2];
        
        connectionSpeedText.text = $"{connectionSpeed:N0}";
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
        int childCount = antennasGrid.childCount;
        Transform[] antennaLabels = new Transform[childCount];
        
        for (int index = 0; index < childCount; index++)
        {
            Transform antennaLabel = antennasGrid.GetChild(index);
            antennaLabels[index] = antennaLabel;
        }
        
        var selectedAntennaLabels = antennaLabels
            .Select(antennaLabel => new
            {
                Label = antennaLabel,
                ConnectionSpeed = float.TryParse(
                    antennaLabel.GetComponentsInChildren<TextMeshProUGUI>()[1].text, out float speed)
                        ? speed
                        : 0.0f,
                PriorityWeight = antennaLabel.GetComponentsInChildren<TextMeshProUGUI>()[0].text == DataManager.Instance.PrioritizedAntenna
                    ? 1.0f
                    : 0.0f,
                Name = antennaLabel.GetComponentsInChildren<TextMeshProUGUI>()[0].text,
            });

        List<Transform> sortedAntennaLabels;
        switch (DataManager.Instance.PriorityAlgorithm)
        {
            case DataManager.LinkBudgetAlgorithm.Signal:
                sortedAntennaLabels = selectedAntennaLabels
                    .OrderByDescending(item => item.ConnectionSpeed)
                    .ThenBy(item => item.Name)
                    .Select(item => item.Label)
                    .ToList();
                break;
            case DataManager.LinkBudgetAlgorithm.Switch: // Switch: Looks ahead by 60 data values.
            case DataManager.LinkBudgetAlgorithm.Asset: // Asset: Looks ahead by 20 data values.
                sortedAntennaLabels = selectedAntennaLabels
                    .OrderByDescending(item => item.PriorityWeight)
                    .ThenByDescending(item => item.ConnectionSpeed)
                    .ThenBy(item => item.Name)
                    .Select(item => item.Label)
                    .ToList();
                break;
            case DataManager.LinkBudgetAlgorithm.None:
            default:
                sortedAntennaLabels = selectedAntennaLabels.OrderBy(item => item.Name)
                    .Select(item => item.Label)
                    .ToList();
                break;
        }
        
        foreach (Transform label in sortedAntennaLabels)
        {
            label.SetSiblingIndex(sortedAntennaLabels.IndexOf(label));
        }
    }
    
    private void ColorAntennas()
    {
        int index = 0;
        foreach (Transform antennaBackground in antennasGrid)
        {
            antennaBackground.GetComponent<Image>().color = index < 4 - _disabledAntennas.Count
                ? enabledAntennaBackgroundColors[_disabledAntennas.Count]
                : disabledAntennaBackgroundColor;
            index++;
        }
    }
    
    #endregion
    
    private void DataLoaded(DataLoadedEventArgs dataLoadedEventArgs)
    {
        UpdateMissionStage(dataLoadedEventArgs.MissionStage);
        _nominalLinkBudgetData = dataLoadedEventArgs.NominalLinkBudget;
        _offNominalLinkBudgetData = dataLoadedEventArgs.OffNominalLinkBudget;
        _thrustData = dataLoadedEventArgs.ThrustData;
    }

    #region Mission Stage
    
    private void UpdateMissionStage(MissionStage stage)
    {
        missionStageText.text = stage.name;
        missionStageText.color = stage.color;
    }

    #endregion

    #region Thrust

    private void UpdateThrust(int index)
    {
        Vector3 thrust = new Vector3(float.Parse(_thrustData[index][23]), float.Parse(_thrustData[index][24]), float.Parse(_thrustData[index][25]));
        float magnitude = thrust.magnitude;

        if (_spacecraftState == SpacecraftManager.SpacecraftState.OffNominal)
        {
            thrustText.text = $"{magnitude:N3} N";
        }
    }

    #endregion

    private void UpdateSpacecraftState(SpacecraftManager.SpacecraftState state)
    {
        _spacecraftState = state;
        
        thrustSection.SetActive(_spacecraftState == SpacecraftManager.SpacecraftState.OffNominal);
        if (_spacecraftState != SpacecraftManager.SpacecraftState.OffNominal)
        {
            thrustText.text = "";
        }
    }
    
    
    #region Notifications

    private void ShowStageFiredNotification(string text)
    {
        if (_showedStageFiredNotification)
        {
            return;
        }

        ShowNotification(text);
        _showedStageFiredNotification = true;
    }

    public void ShowNotification(string text)
    {
        ShowNotification(text, Notification.NotificationType.Dismissible, null);
    }

    public void ShowNotification(string text, Notification.NotificationType notificationType, Action onYesButtonPressedCallback)
    {
        //notification.SetActive(true);
        //notificationText.text = text;
        GameObject notification = Instantiate(notificationPrefab, notificationParent);
        notification.GetComponent<Notification>().Setup(
            text,
            notificationType,
            onYesButtonPressedCallback
        );
    }
    
    #endregion
    
    #region UI Visibility
    
    private void HandleUIVisibility()
    {
        if (Input.anyKey || Input.mousePosition != _lastMousePosition)
        {
            _inactivityTimer = 0.0f;
            _isFadingOut = false;

            if (!isUiHidden)
            {
                StartCoroutine(FadeUIIn());
            }
        }
        else
        {
            if (!isUiHidden)
            {
                if (_shouldUiFadeOut)
                {
                    _inactivityTimer += Time.unscaledDeltaTime;
                }

                if (_inactivityTimer >= inputInactivityTime && canvasGroup.alpha > 0)
                {
                    _isFadingOut = true;
                }
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
        canvasGroup.alpha = Mathf.Max(minimumUIVisibility, canvasGroup.alpha - uiFadeSpeed * Time.unscaledDeltaTime);
    }

    private IEnumerator FadeUIIn()
    {
        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += uiFadeSpeed * Time.unscaledDeltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1;
    }

    private void ToggleUiVisibility(bool setAlphaInsteadOfFade = false)
    {
        isUiHidden = !isUiHidden;

        canvasGroup.interactable = !isUiHidden;
        canvasGroup.blocksRaycasts = !isUiHidden;

        if (setAlphaInsteadOfFade)
        {
            canvasGroup.alpha = 1.0f;
        } else
        {
            canvasGroup.alpha = 0.0f;
        }
    }

    #endregion

    public void NominalTogglePressed()
    {
        TrajectorySelected?.Invoke(SpacecraftManager.SpacecraftState.Nominal);
    }

    public void OffnominalTogglePressed()
    {
        TrajectorySelected?.Invoke(SpacecraftManager.SpacecraftState.OffNominal);
    }
    
    private enum UnitSystem {
        Metric,
        Imperial
    }
}