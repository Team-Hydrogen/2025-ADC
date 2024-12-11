using System;
using System.Collections.Generic;

public class DataLoadedEventArgs : EventArgs
{
    public List<string[]> NominalTrajectoryData { get; }
    public List<string[]> OffNominalTrajectoryData { get; }
    public List<string[]> AntennaAvailabilityData { get; }
    public List<string[]> LinkBudgetData { get; }
    public MissionStage MissionStage { get; }

    public DataLoadedEventArgs(List<string[]> norminalTrajectoryData, List<string[]> offNominalTrajectoryData, List<string[]> antennaAvailability, List<string[]> linkBudgetData, MissionStage missionStage)
    {
        NominalTrajectoryData = norminalTrajectoryData;
        OffNominalTrajectoryData = offNominalTrajectoryData;
        AntennaAvailabilityData = antennaAvailability;
        LinkBudgetData = linkBudgetData;
        MissionStage = missionStage;
    }
}