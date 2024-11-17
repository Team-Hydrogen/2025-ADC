using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraPan : MonoBehaviour
{
    [SerializeField] private float horizontalPanSpeed = 20.0f;
    [SerializeField] private float verticalPanSpeed = 0.1f;
    [SerializeField] private CinemachineFreeLook freeLookCamera;
    
    private void Start()
    {
        if (freeLookCamera == null)
        {
            freeLookCamera = GetComponent<CinemachineFreeLook>();
        }
    }
    
    private void Update()
    {
        HorizontalPan();
        VerticalPan();
    }
    
    private void HorizontalPan()
    {
        if (Input.GetMouseButton(0))
        {
            var netScrollSpeed = horizontalPanSpeed * Input.GetAxis("Mouse X");
            freeLookCamera.m_XAxis.Value += netScrollSpeed;
        }
    }

    private void VerticalPan()
    {
        if (Input.GetMouseButton(0))
        {
            var netScrollSpeed = verticalPanSpeed * Input.GetAxis("Mouse Y");
            freeLookCamera.m_YAxis.Value += netScrollSpeed;
        }
    }
}