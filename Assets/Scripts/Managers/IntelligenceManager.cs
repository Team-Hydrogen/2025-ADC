using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntelligenceManager : MonoBehaviour
{
    public static IntelligenceManager instance { get; private set; }
    
    
    #region Event Functions
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
    }
    
    #endregion
    
    
    #region Bump Off Course
    
    #endregion
    
    
    #region Thrust
    
    #endregion
}