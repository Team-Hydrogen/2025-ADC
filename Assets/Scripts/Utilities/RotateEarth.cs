using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateEarth : MonoBehaviour
{
    [SerializeField] private Transform surface;
    [SerializeField] private Transform clouds;
    [SerializeField] private float surfaceRotationSpeedIfNotSet;
    [SerializeField] private float cloudsRotationSpeedIfNotSet;

    private float surfaceRotationSpeed;
    private float cloudsRotationSpeed;

    //private Quaternion earthTilt;

    private void Start()
    {
        //earthTilt = surface.transform.rotation;
        surfaceRotationSpeed = surfaceRotationSpeedIfNotSet;
        cloudsRotationSpeed = cloudsRotationSpeedIfNotSet;
    }

    private void OnEnable()
    {
        SatelliteManager.OnTimeScaleSet += UpdateRotationSpeed;
    }

    private void OnDisable()
    {
        SatelliteManager.OnTimeScaleSet -= UpdateRotationSpeed;
    }

    private void Update()
    {
        //surface.transform.rotation = earthTilt;
        surface.transform.Rotate(Vector3.up, surfaceRotationSpeed * Time.deltaTime, Space.Self);

        //clouds.transform.rotation = earthTilt;
        clouds.transform.Rotate(Vector3.up, cloudsRotationSpeed * Time.deltaTime, Space.Self);
    }

    private void UpdateRotationSpeed(float timeScale)
    {
        //print(timeScale);
        float updatedRotationSpeed = 360f / 24f / 60f / 60f * timeScale;

        surfaceRotationSpeed = -updatedRotationSpeed;
        //cloudsRotationSpeed = -0.0012f;
        cloudsRotationSpeed = -(updatedRotationSpeed + 0.008f);
    }
}
