using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 5.0f;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    
    [SerializeField] private const float MinimumCameraDistance = 10.0f;
    [SerializeField] private const float MaximumCameraDistance = 100.0f;
    
    // Start is called before the first frame update
    void Start()
    {
        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        var thirdPersonFollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        var netScrollSpeed = scrollSpeed * Input.GetAxis("Mouse ScrollWheel");
        var newCameraDistance = thirdPersonFollow.CameraDistance + netScrollSpeed;
        thirdPersonFollow.CameraDistance = Mathf.Clamp(
            newCameraDistance, MinimumCameraDistance, MaximumCameraDistance);
    }
}
