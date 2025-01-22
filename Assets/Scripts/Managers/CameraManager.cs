using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraManager : MonoBehaviour
{
    [SerializeField, Header("Orbital Follow")]
    private CinemachineOrbitalFollow orbitalFollow;
    
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
        if (orbitalFollow == null)
        {
            orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        }
        CinemachineCore.GetInputAxis = GetAxisCustom;
    }
    
    private void LateUpdate()
    {
        Zoom();
    }

    private void Zoom()
    {
        var netScrollSpeed = -zoomSpeed * Input.GetAxis("Mouse ScrollWheel");
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
                return -Input.GetAxis("Mouse Y");
            }

            return 0;
        }

        return Input.GetAxis(axisName);
    }
}