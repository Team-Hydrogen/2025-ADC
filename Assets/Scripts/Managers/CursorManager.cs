using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [HideInInspector] public static CursorManager Instance { get; private set; }
    
    private const int LeftMouseButton = 0;
    private const int RightMouseButton = 1;
    private const int MiddleMouseButton = 2;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (Input.GetMouseButton(RightMouseButton))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        } 
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}