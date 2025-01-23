using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }
    
    private static readonly int FadeToCutscene = Animator.StringToHash("StartCutscene");
    private static readonly int FadeToSimulation = Animator.StringToHash("StopCutscene");
    
    [Header("UI Elements")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage cutsceneImage;
    
    [Header("UI Visual Effects")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject fadeImage;
    
    [Header("Animations")]
    [SerializeField] private List<VideoClip> cutscenes;
    [SerializeField] private List<float> cutsceneSimulationTimes;
    
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
        StartCutsceneTransition(-3);
    }
    
    private void OnEnable()
    {
        SpacecraftManager.OnUpdateTime += StartCutsceneTransition;
        videoPlayer.loopPointReached += EndCutsceneTransition;
    }
    
    private void OnDisable()
    {
        SpacecraftManager.OnUpdateTime -= StartCutsceneTransition;
        videoPlayer.loopPointReached -= EndCutsceneTransition;
    }
    
    #endregion

    public void TogglePlayCutscenes(bool value)
    {
        _playCutscenes = value;
    }
    
    private void StartCutsceneTransition(float currentTimeInMinutes)
    {
        if (_state == CutsceneState.Playing)
        {
            return;
        }
        
        if (_cutscenesPlayed >= cutscenes.Count)
        {
            return;
        }
        
        if (currentTimeInMinutes < cutsceneSimulationTimes[_cutscenesPlayed])
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
        
        _state = CutsceneState.Playing;
        Time.timeScale = 0.0f;
        
        fadeImage.gameObject.SetActive(true);
        animator.enabled = true;
        
        StartCoroutine(nameof(PlayCutscene));
    }
    
    private IEnumerator PlayCutscene()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        
        animator.SetTrigger(FadeToCutscene);
        
        videoPlayer.clip = cutscenes[_cutscenesPlayed];
        videoPlayer.Play();
        cutsceneImage.gameObject.SetActive(true);
        
        OnCutsceneStart?.Invoke(_cutscenesPlayed);
    }
    
    private void EndCutsceneTransition(VideoPlayer video)
    {
        animator.SetTrigger(FadeToSimulation);
        
        StartCoroutine(nameof(StopCutscene));
    }
    
    private IEnumerator StopCutscene()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        
        animator.SetTrigger(FadeToCutscene);
        
        videoPlayer.Stop();
        cutsceneImage.gameObject.SetActive(false);
        
        OnCutsceneEnd?.Invoke();
        
        yield return new WaitForSecondsRealtime(1.5f);
        
        animator.enabled = false;
        fadeImage.gameObject.SetActive(false);
        
        _cutscenesPlayed++;
        
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