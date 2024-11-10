using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraPan : MonoBehaviour
{
    [SerializeField] private float panSpeed = 5.0f;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    
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
        var netScrollSpeed = panSpeed * Input.GetAxis("Mouse X");
        thirdPersonFollow.ShoulderOffset += new Vector3(netScrollSpeed, 0, 0);
    }
}