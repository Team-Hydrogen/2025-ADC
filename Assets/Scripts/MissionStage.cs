using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct MissionStage
{
    public int startDataIndex;
    public StageTypes stageType;

    public MissionStage(int startDataIndex, StageTypes stageType)
    {
        this.startDataIndex = startDataIndex;
        this.stageType = stageType;
    }

    private static readonly Dictionary<StageTypes, Color> stageColors = new Dictionary<StageTypes, Color>
    {
        { StageTypes.Launch, Color.red },
        { StageTypes.OrbitingEarth, Color.yellow },
        { StageTypes.TravellingToMoon, Color.green },
        { StageTypes.FlyingByMoon, Color.blue },
        { StageTypes.ReturningToEarth, Color.cyan },
        { StageTypes.ReEntryAndSplashdown, Color.magenta }
    };

    private static readonly Dictionary<StageTypes, string> stageNames = new Dictionary<StageTypes, string>
    {
        { StageTypes.Launch, "Launch" },
        { StageTypes.OrbitingEarth, "Orbiting Earth" },
        { StageTypes.TravellingToMoon, "Travelling to Moon" },
        { StageTypes.FlyingByMoon, "Flying by Moon" },
        { StageTypes.ReturningToEarth, "Returning to Earth" },
        { StageTypes.ReEntryAndSplashdown, "Re-Entry + Splashdown" }
    };

    public enum StageTypes
    {
        None,
        Launch,
        OrbitingEarth,
        TravellingToMoon,
        FlyingByMoon,
        ReturningToEarth,
        ReEntryAndSplashdown
    }

    public string name
    {
        get
        {
            return GetStageName(stageType);
        }
    }

    public Color color
    {
        get
        {
            return GetStageColor(stageType);
        }
    }

    //public Color GetStageColor()
    //{
    //    return GetStageColor(stageType);
    //}

    public static Color GetStageColor(StageTypes stageType)
    {
        if (stageColors.TryGetValue(stageType, out var color))
        {
            return color;
        }

        // Default color
        return Color.white;
    }

    //public string GetStageName()
    //{
    //    return GetStageName(stageType);
    //}

    public static string GetStageName(StageTypes stageType)
    {
        if (stageNames.TryGetValue(stageType, out var name))
        {
            return name;
        }

        // Default color
        return "ERROR: NO STAGE NAME!";
    }

    public override bool Equals(object obj)
    {
        if (obj is MissionStage other)
        {
            return startDataIndex == other.startDataIndex && stageType == other.stageType;
        }

        return false;
    }
}
