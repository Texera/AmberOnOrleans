using Orleans;
using System.Threading.Tasks;
using System.Collections.Generic;
using Engine.OperatorImplementation.Common;
using System;
using Engine.Breakpoint.GlobalBreakpoint;
using Orleans.Runtime;
public interface IDeployGrain : IGrainWithGuidKey
{
    Task<Engine.Controller.IControllerGrain> Init(Guid workflowID, string plan,bool checkpointActivated);
}