using Orleans;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using Engine.OperatorImplementation.Operators;
using Engine.OperatorImplementation.Common;
using Engine.WorkflowImplementation;

namespace Engine.Controller
{
    public class ControllerGrain : Grain, IControllerGrain
    {
        public async Task Init(HashSet<Operator> graph)
        {
            foreach(Operator o in graph)
            {
                o.SetUpPrincipalGrain(this.GrainFactory);
                await o.PrincipalGrain.SetPredicate(o.Predicate);
                await o.PrincipalGrain.Init(o.Predicate);
            }
            foreach(Operator o in graph)
            {
                await o.LinkToDownstreamPrincipleGrains();
            }
            foreach(Operator o in graph)
            {
                await o.Link();
            }
        }
    }
}