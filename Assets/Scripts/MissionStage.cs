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
        { StageTypes.Launch, new Color(1f, 0, 0) },
        { StageTypes.OrbitingEarth, new Color(1f, 0.76f, 0f) },
        { StageTypes.TravellingToMoon, new Color(0.55f, 0.93f, 0.18f) },
        { StageTypes.FlyingByMoon, new Color(0.26f, 0.68f, 0.95f) },
        { StageTypes.ReturningToEarth, new Color(0.93f, 0.26f, 0.95f) },
        { StageTypes.ReEntryAndSplashdown, new Color(1, 0.59f, 0.59f) }
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
