using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateEarth : MonoBehaviour
{
    [SerializeField] private Transform surface;
    [SerializeField] private float surfaceRotationSpeed;
    [SerializeField] private Transform clouds;
    [SerializeField] private float cloudsRotationSpeed;

    //private Quaternion earthTilt;

    private void Start()
    {
        //earthTilt = surface.transform.rotation;
    }

    void Update()
    {
        //surface.transform.rotation = earthTilt;
        surface.transform.Rotate(Vector3.up, surfaceRotationSpeed * Time.time, Space.Self);

        //clouds.transform.rotation = earthTilt;
        clouds.transform.Rotate(Vector3.up, cloudsRotationSpeed * Time.time, Space.Self);
    }
}
