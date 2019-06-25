using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class FilterPrinicipalGrain<T> : PrincipalGrain, IFilterPrincipalGrain<T> where T:IComparable<T>
    {
        public override int DefaultNumGrainsInOneLayer { get { return 4*Constants.DefaultNumGrainsInOneLayer; } }

        public override async Task Init(Controller.IControllerGrain controllerGrain, Guid workflowID, Operator currentOperator)
        {
            await base.Init(controllerGrain,workflowID,currentOperator);
        }


        public override IWorkerGrain GetOperatorGrain(string extension)
        {
            return this.GrainFactory.GetGrain<IFilterOperatorGrain<T>>(this.GetPrimaryKey(), extension);
        }
    }
}