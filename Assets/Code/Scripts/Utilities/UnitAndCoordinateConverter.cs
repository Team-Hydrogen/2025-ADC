using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public static class UnitAndCoordinateConverter
{
    // Distance conversion factors
    private const float KilometersPerMile = 1.609344f;
    private const float MilesPerKilometer = 0.6213711922f;
    // Mass conversion factors
    private const float KilogramsPerPound = 0.45359237f;
    private const float PoundsPerKilogram = 2.2046226218f;
    
    
    #region Distance Converters
    
    public static float MilesToKilometers(float distance)
    {
        return KilometersPerMile * distance;
    }

    public static float KilometersToMiles(float distance)
    {
        return MilesPerKilometer * distance;
    }
    
    #endregion
    
    
    #region Mass Converters
    
    public static float PoundsToKilograms(float mass)
    {
        return KilogramsPerPound * mass;
    }
    
    public static float KilogramsToPounds(float mass)
    {
        return PoundsPerKilogram * mass;
    }
    
    #endregion
}
