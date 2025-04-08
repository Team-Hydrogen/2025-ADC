using System;

public class DistanceEventArgs : EventArgs
{
    public float DistanceFromEarth { get; }
    public float DistanceFromMoon { get; }

    public DistanceEventArgs(float distanceFromEarth, float distanceFromMoon)
    {
        DistanceFromEarth = distanceFromEarth;
        DistanceFromMoon = distanceFromMoon;
    }
}