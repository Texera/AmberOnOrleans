using Orleans;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using Engine.OperatorImplementation.Operators;
using Engine.OperatorImplementation.Common;
using Orleans.Runtime;
using TexeraUtilities;
using Newtonsoft.Json.Linq;
using System.Linq;
using Engine.DeploySemantics;
using Engine.LinkSemantics;
using Engine.Breakpoint.GlobalBreakpoint;
using Engine.OperatorImplementation.FaultTolerance;
using Orleans.Concurrency;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Placement;
using Engine.Controller;

public class DeployGrain : Grain, IDeployGrain
{
    public async Task<IControllerGrain> Init(Guid workflowID, string plan, bool checkpointActivated)
    {
        var res = this.GrainFactory.GetGrain<IControllerGrain>(workflowID);
        RequestContext.Clear();
        RequestContext.Set("targetSilo",Constants.ClientIPAddress);
        await res.Init(res,plan,checkpointActivated);
        return res;
    }
}