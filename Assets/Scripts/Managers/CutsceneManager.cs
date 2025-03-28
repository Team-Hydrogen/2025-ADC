using System;
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
    
    private int _cutscenesPlayed = 0;
    private CutsceneState _state = CutsceneState.NotPlaying;

    public static event Action<int> OnCutsceneStart;
    public static event Action OnCutsceneEnd; // unused

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
        TryPlayCutscene(-3);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && _state == CutsceneState.Playing)
        {
            StopCutscene(videoPlayer);
        }
    }

    private void OnEnable()
    {
        SpacecraftManager.OnUpdateTime += TryPlayCutscene;
        videoPlayer.loopPointReached += StopCutscene;
    }
    
    private void OnDisable()
    {
        SpacecraftManager.OnUpdateTime -= TryPlayCutscene;
        videoPlayer.loopPointReached -= StopCutscene;
    }
    
    #endregion

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

        cutsceneImage.gameObject.SetActive(true);
        skipCutsceneHint.gameObject.SetActive(true);

        OnCutsceneStart?.Invoke(_cutscenesPlayed);
    }

    private void StopCutscene(VideoPlayer source)
    {
        videoPlayer.Stop();

        cutsceneImage.gameObject.SetActive(false);
        skipCutsceneHint.gameObject.SetActive(false);

        OnCutsceneEnd?.Invoke();

        _cutscenesPlayed++;

        try
        {
            videoPlayer.clip = cutscenes[_cutscenesPlayed].clip;
        }
        catch (ArgumentOutOfRangeException) { }

        _state = CutsceneState.NotPlaying;
        Time.timeScale = 1.0f;
    }
    
    private enum CutsceneState
    {
        NotPlaying,
        FadeOut,
        Playing,
        FadeIn,
    }
}