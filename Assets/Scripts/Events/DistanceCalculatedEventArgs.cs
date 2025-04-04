using System;
using System.Collections.Generic;

public class DistanceCalculatedEventArgs : EventArgs
{
    public float TotalDistanceTraveled { get; }
    public float DistanceFromEarth { get; }
    public float DistanceFromMoon { get; }

    public DistanceCalculatedEventArgs(float totalDistanceTraveled, float distanceFromEarth, float distanceFromMoon)
    {
        TotalDistanceTraveled = totalDistanceTraveled;
        DistanceFromEarth = distanceFromEarth;
        DistanceFromMoon = distanceFromMoon;
    }
}