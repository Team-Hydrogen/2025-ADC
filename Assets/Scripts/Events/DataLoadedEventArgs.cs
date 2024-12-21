using System;
using System.Collections.Generic;

public class DataLoadedEventArgs : EventArgs
{
    public List<string[]> NominalTrajectoryData { get; }
    public List<string[]> OffNominalTrajectoryData { get; }
    public List<string[]> AntennaAvailabilityData { get; }
    public List<string[]> LinkBudgetData { get; }
    public List<string[]> OffnominalLinkBudgetData { get; }
    public List<string[]> ThrustData { get; }
    public MissionStage MissionStage { get; }

    public DataLoadedEventArgs(List<string[]> nominalTrajectoryData,
        List<string[]> offNominalTrajectoryData,
        List<string[]> antennaAvailability,
        List<string[]> linkBudgetData,
        List<string[]> offnominalLinkBudgetData,
        List<string[]> thrustData, 
        MissionStage missionStage)
    {
        NominalTrajectoryData = nominalTrajectoryData;
        OffNominalTrajectoryData = offNominalTrajectoryData;
        AntennaAvailabilityData = antennaAvailability;
        LinkBudgetData = linkBudgetData;
        OffnominalLinkBudgetData = offnominalLinkBudgetData;
        ThrustData = thrustData;
        MissionStage = missionStage;
    }
}