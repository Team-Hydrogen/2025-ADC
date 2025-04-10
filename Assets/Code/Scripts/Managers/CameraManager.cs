using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField, Header("Orbital Follow")]
    private CinemachineOrbitalFollow orbitalFollow;
    
    [Header("Camera Zoom")]
    [SerializeField, Range(0.0f, 15.0f)]
    private float zoomSpeed = 1.0f;
    [SerializeField, Range(0.0f, 25.0f)]
    private float minimumCameraDistance = 10.0f;
    [SerializeField, Range(25.0f, 200.0f)]
    private float maximumCameraDistance = 100.0f;
    
    private const float TopBottomRadiusRatio = 0.01f;

    private void Update()
    {
        Zoom();
    }

    /// <summary>
    /// Enables a Cinemachine camera to zoom in and out using the mouse wheel.
    /// </summary>
    private void Zoom()
    {
        var netScrollSpeed = zoomSpeed * InputManager.Instance.GetCurrentZoomInput();
        var newCameraRadius = orbitalFollow.Orbits.Center.Radius + netScrollSpeed;
        
        orbitalFollow.Orbits.Top.Radius = Mathf.Clamp(
            newCameraRadius * TopBottomRadiusRatio,
            minimumCameraDistance * TopBottomRadiusRatio,
            maximumCameraDistance * TopBottomRadiusRatio);
        orbitalFollow.Orbits.Top.Height = newCameraRadius;
        
        orbitalFollow.Orbits.Center.Radius = Mathf.Clamp(
            newCameraRadius,
            minimumCameraDistance,
            maximumCameraDistance);
        orbitalFollow.Orbits.Center.Height = 0.0f;
        
        orbitalFollow.Orbits.Bottom.Radius = Mathf.Clamp(
            newCameraRadius * TopBottomRadiusRatio,
            minimumCameraDistance * TopBottomRadiusRatio,
            maximumCameraDistance * TopBottomRadiusRatio);
        orbitalFollow.Orbits.Bottom.Height = -newCameraRadius;
    }
}