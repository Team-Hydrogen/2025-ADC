using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField, Header("Free Look Camera")]
    private CinemachineFreeLook freeLookCamera;
    
    [Header("Camera Zoom Settings")]
    [SerializeField, Range(0.0f, 15.0f)]
    private float zoomSpeed = 5.0f;
    [SerializeField, Range(0.0f, 25.0f)]
    private float minimumCameraDistance = 10.0f;
    [SerializeField, Range(25.0f, 200.0f)]
    private float maximumCameraDistance = 100.0f;
    
    private const float TopBottomRadiusRatio = 0.01f;
    
    private void Start()
    {
        if (freeLookCamera == null)
        {
            freeLookCamera = GetComponent<CinemachineFreeLook>();
        }
        CinemachineCore.GetInputAxis = GetAxisCustom;
    }
    
    private void Update()
    {
        Zoom();
    }

    private void Zoom()
    {
        var netScrollSpeed = -zoomSpeed * Input.GetAxis("Mouse ScrollWheel");
        var newCameraRadius = freeLookCamera.m_Orbits[1].m_Radius + netScrollSpeed;
        
        freeLookCamera.m_Orbits[0].m_Radius = Mathf.Clamp(
            newCameraRadius * TopBottomRadiusRatio,
            minimumCameraDistance * TopBottomRadiusRatio,
            maximumCameraDistance * TopBottomRadiusRatio);
        freeLookCamera.m_Orbits[0].m_Height = newCameraRadius;
        
        freeLookCamera.m_Orbits[1].m_Radius = Mathf.Clamp(
            newCameraRadius,
            minimumCameraDistance,
            maximumCameraDistance);
        freeLookCamera.m_Orbits[1].m_Height = 0.0f;
        
        freeLookCamera.m_Orbits[2].m_Radius = Mathf.Clamp(
            newCameraRadius * TopBottomRadiusRatio,
            minimumCameraDistance * TopBottomRadiusRatio,
            maximumCameraDistance * TopBottomRadiusRatio);
        freeLookCamera.m_Orbits[2].m_Height = -newCameraRadius;
    }
    
    public float GetAxisCustom(string axisName)
    {
        if (axisName == "Mouse X")
        {
            if (Input.GetMouseButton(1))
            {
                return Input.GetAxis("Mouse X");
            }

            return 0;
        }

        if (axisName == "Mouse Y")
        {
            if (Input.GetMouseButton(1))
            {
                return Input.GetAxis("Mouse Y");
            }

            return 0;
        }

        return Input.GetAxis(axisName);
    }
}