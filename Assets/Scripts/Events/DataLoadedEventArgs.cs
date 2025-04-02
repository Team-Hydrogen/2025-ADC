using System;
using System.Collections.Generic;

public class DataLoadedEventArgs : EventArgs
{
    public List<string[]> NominalTrajectoryData { get; }
    public List<string[]> OffNominalTrajectoryData { get; }
    public List<string[]> AntennaAvailabilityData { get; }
    public List<string[]> NominalLinkBudget { get; }
    public List<string[]> OffNominalLinkBudget { get; }
    public List<string[]> ThrustData { get; }
    public MissionStage MissionStage { get; }

    public DataLoadedEventArgs(List<string[]> nominalTrajectoryData,
        List<string[]> offNominalTrajectoryData,
        List<string[]> antennaAvailability,
        List<string[]> nominalLinkBudget,
        List<string[]> offNominalLinkBudget,
        List<string[]> thrustData, 
        MissionStage missionStage)
    {
        NominalTrajectoryData = nominalTrajectoryData;
        OffNominalTrajectoryData = offNominalTrajectoryData;
        AntennaAvailabilityData = antennaAvailability;
        NominalLinkBudget = nominalLinkBudget;
        OffNominalLinkBudget = offNominalLinkBudget;
        ThrustData = thrustData;
        MissionStage = missionStage;
    }
}