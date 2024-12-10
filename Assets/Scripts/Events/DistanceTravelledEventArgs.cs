using System;
using System.Collections.Generic;

public class DistanceTravelledEventArgs : EventArgs
{
    public float TotalDistance { get; }
    public float DistanceFromEarth { get; }
    public float DistanceFromMoon { get; }

    public DistanceTravelledEventArgs(float totalDistance, float distanceFromEarth, float distanceFromMoon)
    {
        TotalDistance = totalDistance;
        DistanceFromEarth = distanceFromEarth;
        DistanceFromMoon = distanceFromMoon;
    }
}