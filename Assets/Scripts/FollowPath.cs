using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPath : MonoBehaviour
{
    [SerializeField]
    private LineRenderer pathRenderer;

    private int _currentPositionIndex = 0;
    
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = pathRenderer.GetPosition(_currentPositionIndex);
        
        
        print("ITERATION " + _currentPositionIndex);
        print(pathRenderer.GetPosition(_currentPositionIndex));
        print(transform.position);
        print(transform.position == pathRenderer.GetPosition(_currentPositionIndex));
        print("END OF ITERATION");
        
        if (_currentPositionIndex < pathRenderer.positionCount - 1)
        {
            _currentPositionIndex++;
        }
    }
}
