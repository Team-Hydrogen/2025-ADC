using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntelligenceManager : MonoBehaviour
{
    public static IntelligenceManager Instance { get; private set; }
    
    private List<string[]> _thrustData;
    
    
    #region Event Functions
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    #endregion
    
    #region Thrust
    
    #endregion
    
    #region Bump Off Course
    
    #endregion
}