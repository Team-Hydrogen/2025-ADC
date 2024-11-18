using Cinemachine;
using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    [SerializeField, Range(0.0f, 15.0f)]
    private float scrollSpeed = 5.0f;
    [SerializeField]
    private CinemachineFreeLook freeLookCamera;
    
    private const float MinimumCameraDistance = 10.0f;
    private const float MaximumCameraDistance = 100.0f;
    
    private void Start()
    {
        if (freeLookCamera == null)
        {
            freeLookCamera = GetComponent<CinemachineFreeLook>();
        }
    }
    
    private void Update()
    {
        Zoom();
    }

    private void Zoom()
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