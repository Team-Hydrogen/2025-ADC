using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraPan : MonoBehaviour
{
    [SerializeField] private float panSpeed = 20.0f;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    private CinemachineOrbitalTransposer _orbitalTransposer;
    
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
        if (Input.GetMouseButton(0))
        {
            var netScrollSpeed = panSpeed * Input.GetAxis("Mouse X");
            _orbitalTransposer.m_XAxis.Value += netScrollSpeed;
        }
    }
}