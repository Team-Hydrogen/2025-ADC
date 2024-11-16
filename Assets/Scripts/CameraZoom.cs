using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 5.0f;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    private CinemachineOrbitalTransposer _orbitalTransposer;
    
    [SerializeField] private const float MinimumCameraDistance = 10.0f;
    [SerializeField] private const float MaximumCameraDistance = 100.0f;
    
    void Start()
    {
        _orbitalTransposer = virtualCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();
        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
        }
    }
    
    private void Update()
    {
        var netScrollSpeed = scrollSpeed * Input.GetAxis("Mouse ScrollWheel");
        var newCameraDistance = _orbitalTransposer.m_FollowOffset.z + netScrollSpeed;
        _orbitalTransposer.m_FollowOffset.z = Mathf.Clamp(
            newCameraDistance, MinimumCameraDistance, MaximumCameraDistance);
    }
}
