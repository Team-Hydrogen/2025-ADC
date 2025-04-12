using UnityEngine;

public class RotateSceneLight : MonoBehaviour
{
    [SerializeField] Transform earthPosition;

    void Update()
    {
        transform.LookAt(earthPosition);
    }
}
