using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 5.0f;
    [SerializeField] private CinemachineFreeLook freeLookCamera;
    private CinemachineOrbitalTransposer _orbitalTransposer;
    
    [SerializeField] private const float MinimumCameraDistance = 10.0f;
    [SerializeField] private const float MaximumCameraDistance = 100.0f;
    
    private void Start()
    {
        if (freeLookCamera == null)
        {
            freeLookCamera = GetComponent<CinemachineFreeLook>();
        }
        _orbitalTransposer = freeLookCamera.GetComponent<CinemachineOrbitalTransposer>();
    }
    
    private void Update()
    {
        var netScrollSpeed = scrollSpeed * Input.GetAxis("Mouse ScrollWheel");
        var newCameraRadius = freeLookCamera.m_Orbits[1].m_Radius + netScrollSpeed;
        
        freeLookCamera.m_Orbits[0].m_Radius = Mathf.Clamp(
            newCameraRadius / 100f, MinimumCameraDistance / 100f, MaximumCameraDistance / 100f);
        freeLookCamera.m_Orbits[0].m_Height = newCameraRadius;
        
        freeLookCamera.m_Orbits[1].m_Radius = Mathf.Clamp(
            newCameraRadius, MinimumCameraDistance, MaximumCameraDistance);
        
        freeLookCamera.m_Orbits[2].m_Radius = Mathf.Clamp(
            newCameraRadius / 100f, MinimumCameraDistance / 100f, MaximumCameraDistance / 100f);
        freeLookCamera.m_Orbits[2].m_Height = -newCameraRadius;
    }
}
