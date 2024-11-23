using Cinemachine;
using UnityEngine;

public class CameraPan : MonoBehaviour
{
    [SerializeField, Range(10.0f, 40.0f)]
    private float horizontalPanSpeed = 20.0f;
    [SerializeField, Range(0.0f, 0.5f)]
    private float verticalPanSpeed = 0.1f;
    [SerializeField]
    private CinemachineFreeLook freeLookCamera;
    
    private void Start()
    {
        //if (freeLookCamera == null)
        //{
        //    freeLookCamera = GetComponent<CinemachineFreeLook>();
        //}

        CinemachineCore.GetInputAxis = GetAxisCustom;
    }
    
    //private void Update()
    //{
    //    HorizontalPan();
    //    VerticalPan();
    //}
    
    //private void HorizontalPan()
    //{
    //    if (!Input.GetMouseButton(0))
    //    {
    //        return;
    //    }
    //    var netScrollSpeed = horizontalPanSpeed * Input.GetAxis("Mouse X");
    //    freeLookCamera.m_XAxis.Value += netScrollSpeed;
    //}

    //private void VerticalPan()
    //{
    //    if (!Input.GetMouseButton(0))
    //    {
    //        return;
    //    }
    //    var netScrollSpeed = verticalPanSpeed * Input.GetAxis("Mouse Y");
    //    freeLookCamera.m_YAxis.Value += netScrollSpeed;
    //}

    private float GetAxisCustom(string axisName)
    {
        if (axisName == "Mouse X")
        {
            if (Input.GetMouseButton(0))
            {
                return Input.GetAxis("Mouse X");
            }
        }

        if (axisName == "Mouse Y")
        {
            if (Input.GetMouseButton(0))
            {
                return Input.GetAxis("Mouse Y");
            }
        }

        return Input.GetAxis(axisName);
    }
}