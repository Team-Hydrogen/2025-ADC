using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }
    
    [SerializeField] private List<Cutscene> cutscenes;

    [Header("UI Elements")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage cutsceneImage;
    [SerializeField] private Transform skipCutsceneHint;
    [SerializeField] private GameObject blackScreen;
    
    private Image _blackScreenImage;
    
    private int _cutscenesPlayed = 0;
    private enum CutsceneState { NotPlaying, FadeOut, Playing, FadeIn }
    private CutsceneState _state = CutsceneState.NotPlaying;

    public static event Action<int> OnCutsceneStart;
    public static event Action OnCutsceneEnd;

    private bool _playCutscenes = true;
    
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
        _blackScreenImage = blackScreen.GetComponent<Image>();
        
        ShowBlackScreen();
        videoPlayer.Prepare();
        StartCoroutine(HideBlackScreenOnStart());
        TryPlayCutscene(-3);
    }

    private void OnEnable()
    {
        SimulationManager.ElapsedTimeUpdated += TryPlayCutscene;
        videoPlayer.loopPointReached += StopCutscene;

        InputManager.OnSkipCutscene += SkipCutscene;
    }
    
    private void OnDisable()
    {
        SimulationManager.ElapsedTimeUpdated -= TryPlayCutscene;
        videoPlayer.loopPointReached -= StopCutscene;

        InputManager.OnSkipCutscene -= SkipCutscene;
    }

    #endregion

    private void SkipCutscene()
    {
        if (_state == CutsceneState.Playing)
        {
            StopCutscene(videoPlayer);
        }
    }

    public void TogglePlayCutscenes(bool value)
    {
        _playCutscenes = value;
    }
    
    private void TryPlayCutscene(float currentTimeInMinutes)
    {
        if (_state == CutsceneState.Playing)
        {
            return;
        }
        
        if (_cutscenesPlayed >= cutscenes.Count)
        {
            return;
        }
        
        if (currentTimeInMinutes < cutscenes[_cutscenesPlayed].triggerTimeInMinutes)
        {
            return;
        }

        if (!_playCutscenes)
        {
            OnCutsceneStart?.Invoke(_cutscenesPlayed);
            OnCutsceneEnd?.Invoke();
            _cutscenesPlayed++;

            return;
        }
        
        PlayCutscene();
    }

    private void PlayCutscene()
    {
        _state = CutsceneState.Playing;
        Time.timeScale = 0.0f;

        videoPlayer.clip = cutscenes[_cutscenesPlayed].clip;
        videoPlayer.Play();

        ShowBlackScreen();

        cutsceneImage.gameObject.SetActive(true);
        skipCutsceneHint.gameObject.SetActive(true);

        OnCutsceneStart?.Invoke(_cutscenesPlayed);
    }

    private void StopCutscene(VideoPlayer source)
    {
        videoPlayer.Stop();

        cutsceneImage.gameObject.SetActive(false);
        skipCutsceneHint.gameObject.SetActive(false);

        StartCoroutine(HideBlackScreen());

        OnCutsceneEnd?.Invoke();

        _cutscenesPlayed++;

        try
        {
            videoPlayer.clip = cutscenes[_cutscenesPlayed].clip;
        } catch (ArgumentOutOfRangeException) { }

        videoPlayer.Prepare();

        _state = CutsceneState.NotPlaying;
        Time.timeScale = 1.0f;
    }

    private void ShowBlackScreen()
    {
        _blackScreenImage.color = new Color(0, 0, 0, 1);
        blackScreen.SetActive(true);
    }

    private IEnumerator HideBlackScreenOnStart()
    {
        yield return new WaitForSeconds(0.2f);

        float fadeOutSpeedMultiplier = 2.0f;
        Color color = _blackScreenImage.color;

        while (color.a > 0)
        {
            color.a -= Time.deltaTime * fadeOutSpeedMultiplier;
            _blackScreenImage.color = color;
            yield return null;
        }

        blackScreen.SetActive(false);
    }

    private IEnumerator HideBlackScreen()
    {
        float fadeOutSpeedMultiplier = 2.0f;
        Color color = _blackScreenImage.color;

        while (color.a > 0)
        {
            color.a -= Time.deltaTime * fadeOutSpeedMultiplier;
            _blackScreenImage.color = color;
            yield return null;
        }

        blackScreen.SetActive(false);
    }
}