using UnityEngine;

public class MoveSun : MonoBehaviour
{
    [SerializeField] Transform targetTransform;
    [SerializeField] float orbitSpeedIfNotSet;

    private float orbitSpeed;

    private Vector3 targetOffset;

    private void Start()
    {
        targetOffset = transform.position - targetTransform.position;
        orbitSpeed = orbitSpeedIfNotSet;
    }

    private void OnEnable()
    {
        SimulationManager.TimeScaleSet += UpdateRotationSpeed;
    }

    private void OnDisable()
    {
        SimulationManager.TimeScaleSet -= UpdateRotationSpeed;
    }

    private void Update()
    {
        transform.position = targetTransform.position + targetOffset;
        transform.Rotate(Vector3.up, orbitSpeed * Time.deltaTime);
    }

    private void UpdateRotationSpeed(float timeScale)
    {
        //print(timeScale);
        float updatedRotationSpeed = 360f / 27.3f / 24f / 60f / 60f * timeScale;

        orbitSpeed = updatedRotationSpeed;
    }
}
