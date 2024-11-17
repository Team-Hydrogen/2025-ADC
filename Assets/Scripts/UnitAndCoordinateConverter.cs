using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public static class UnitAndCoordinateConverter
{
    public static float MilesToKilometers(float distance)
    {
        return 1.60934f * distance;
    }

    public static float KilometersToMiles(float distance)
    {
        return 0.621371192f * distance;
    }
}
