using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveMoon : MonoBehaviour
{
    // Moon data storage
    private string[][] _data;
    
    private int _dataIndex;
    private float _progress;
    
    private void OnEnable()
    {
        DataManager.DataLoaded += LoadData;
        DataManager.DataIndexUpdated += UpdateLowerIndex;
        DataManager.ProgressUpdated += UpdateInterpolationRatio;
    }

    private void OnDisable()
    {
        DataManager.DataLoaded -= LoadData;
        DataManager.DataIndexUpdated -= UpdateLowerIndex;
        DataManager.ProgressUpdated -= UpdateInterpolationRatio;
    }

    private void Update()
    {
        UpdatePosition();
        UpdateRotation();
    }

    private void LoadData(DataLoadedEventArgs arguments)
    {
        _data = arguments.OffNominalTrajectoryData;
    }

    private void UpdateLowerIndex(int index)
    {
        _dataIndex = index;
    }

    private void UpdateInterpolationRatio(float ratio)
    {
        _progress = ratio;
    }

    private void UpdatePosition()
    {
        // Determine the index bounds.
        int lowerIndex = _dataIndex;
        int upperIndex = Mathf.Min(_dataIndex + 1, _data.Length - 1);
        
        // Get the lower and upper position bounds.
        Vector3 lowerPosition = new Vector3(
            float.Parse(_data[lowerIndex][14]),
            float.Parse(_data[lowerIndex][15]),
            float.Parse(_data[lowerIndex][16])
        ) * 0.01f;
        Vector3 upperPosition = new Vector3(
            float.Parse(_data[upperIndex][14]),
            float.Parse(_data[upperIndex][15]),
            float.Parse(_data[upperIndex][16])
        ) * 0.01f;
        
        transform.position = Vector3.Lerp(lowerPosition, upperPosition, _progress);
    }

    private void UpdateRotation()
    {
        // Determine the index bounds.
        int lowerIndex = _dataIndex;
        int upperIndex = Mathf.Min(_dataIndex + 1, _data.Length - 1);
        
        // Get the lower and upper velocity bounds.
        Vector3 lowerVelocity = new Vector3(
            float.Parse(_data[lowerIndex][17]),
            float.Parse(_data[lowerIndex][18]),
            float.Parse(_data[lowerIndex][19])
        );
        Vector3 upperVelocity = new Vector3(
            float.Parse(_data[upperIndex][17]),
            float.Parse(_data[upperIndex][18]),
            float.Parse(_data[upperIndex][19])
        );
        
        transform.rotation = Quaternion.Euler(Vector3.Lerp(lowerVelocity, upperVelocity, _progress));
    }
}