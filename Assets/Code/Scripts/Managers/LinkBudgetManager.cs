using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinkBudgetManager : MonoBehaviour
{
    public static LinkBudgetManager Instance { get; private set; }
    
    private List<string[]> _nominalLinkBudgetData;
    private List<string[]> _offNominalLinkBudgetData;
    
    
    # region Event Functions
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    #endregion
    
    
    # region Priority Algorithms
    
    # endregion
}
