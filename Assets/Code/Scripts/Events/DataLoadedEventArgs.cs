using System;
using System.Collections.Generic;

public class DataLoadedEventArgs : EventArgs
{
    public string[][] NominalTrajectoryData { get; }
    public string[][] OffNominalTrajectoryData { get; }
    public string[][] NominalAntennaAvailabilityData { get; }
    public string[][] OffNominalAntennaAvailabilityData { get; }
    public string[][] NominalLinkBudget { get; }
    public string[][] OffNominalLinkBudget { get; }
    public string[][] ThrustData { get; }
    public MissionStage MissionStage { get; }

    public DataLoadedEventArgs(
        string[][] nominalTrajectoryData,
        string[][] offNominalTrajectoryData,
        string[][] nominalAntennaAvailability,
        string[][] offNominalAntennaAvailability,
        string[][] nominalLinkBudget,
        string[][] offNominalLinkBudget,
        string[][] thrustData, 
        MissionStage missionStage)
    {
        NominalTrajectoryData = nominalTrajectoryData;
        OffNominalTrajectoryData = offNominalTrajectoryData;
        NominalAntennaAvailabilityData = nominalAntennaAvailability;
        OffNominalAntennaAvailabilityData = offNominalAntennaAvailability;
        NominalLinkBudget = nominalLinkBudget;
        OffNominalLinkBudget = offNominalLinkBudget;
        ThrustData = thrustData;
        MissionStage = missionStage;
    }
}