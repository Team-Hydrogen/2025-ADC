using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCelestialBodies : MonoBehaviour
{
    [SerializeField] Transform targetTransform;

    Vector3 cameraOffset;

    private void Start()
    {
        cameraOffset = transform.position - targetTransform.position;
    }

    private void Update()
    {
        transform.position = targetTransform.position + cameraOffset;
    }
}
