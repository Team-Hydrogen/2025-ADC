using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerInputActions inputActions;

    #region Events
    public event Action OnSkipCutscene;

    public event Action OnSkipForward;
    public event Action OnSkipBackward;
    public event Action OnPlayPause;
    public event Action OnAccelerateTime;
    public event Action OnDecelerateTime;

    public event Action<float> OnCameraZoom;
    #endregion

    #region Event Functions
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }

        Instance = this;

        inputActions = new PlayerInputActions();

        SubscribeToInput();
    }

    private void SubscribeToInput()
    {
        inputActions.Cutscene.SkipCutscene.performed += ctx => OnSkipCutscene?.Invoke();

        inputActions.Timeline.SkipForward.performed += ctx => OnSkipForward?.Invoke();
        inputActions.Timeline.SkipBackward.performed += ctx => OnSkipBackward?.Invoke();
        inputActions.Timeline.PlayPause.performed += ctx => OnPlayPause?.Invoke();
        inputActions.Timeline.AccelerateTime.performed += ctx => OnAccelerateTime?.Invoke();
        inputActions.Timeline.DecelerateTime.performed += ctx => OnDecelerateTime?.Invoke();

        inputActions.Camera.Zoom.performed += ctx => OnCameraZoom?.Invoke(ctx.ReadValue<float>());
        
        // This doesn't quite work for some reason
        //inputActions.Camera.Orbit.performed += ctx => Debug.Log(true);
        //inputActions.Camera.Orbit.canceled += ctx => Debug.Log(false);
    }

    private void OnEnable()
    {
        inputActions.Enable();

        CutsceneManager.OnCutsceneStart += OnCutsceneStart;
        CutsceneManager.OnCutsceneEnd += OnCutsceneEnd;
    }

    private void OnDisable()
    {
        inputActions.Disable();

        CutsceneManager.OnCutsceneStart -= OnCutsceneStart;
        CutsceneManager.OnCutsceneEnd -= OnCutsceneEnd;
    }
    #endregion

    private void OnCutsceneStart(int _)
    {
        inputActions.Cutscene.Enable();
        inputActions.Timeline.Disable();
    }

    private void OnCutsceneEnd()
    {
        inputActions.Cutscene.Disable();
        inputActions.Timeline.Enable();
    }

    public float GetCurrentZoomInput()
    {
        return inputActions.Camera.Zoom.ReadValue<float>();
    }
}
