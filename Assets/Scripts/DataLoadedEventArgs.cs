using System;
using System.Collections.Generic;

public class DataLoadedEventArgs : EventArgs
{
    public List<string[]> NominalTrajectoryData { get; }
    public List<string[]> OffNominalTrajectoryData { get; }
    public List<string[]> LinkBudgetData { get; }

    public DataLoadedEventArgs(List<string[]> norminalTrajectoryData, List<string[]> offNominalTrajectoryData, List<string[]> linkBudgetData)
    {
        NominalTrajectoryData = norminalTrajectoryData;
        OffNominalTrajectoryData = offNominalTrajectoryData;
        LinkBudgetData = linkBudgetData;
    }
}