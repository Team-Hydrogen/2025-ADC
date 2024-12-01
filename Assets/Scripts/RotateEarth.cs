using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateEarth : MonoBehaviour
{
    [SerializeField] private Transform surface;
    [SerializeField] private float rotationSpeed;

    private Quaternion earthTilt;

    private void Start()
    {
        earthTilt = surface.transform.rotation;
    }

    void Update()
    {
        transform.rotation = Quaternion.Euler(23.5f, 0, 0) * Quaternion.Euler(0, Time.time * rotationSpeed, 0);
    }
}
