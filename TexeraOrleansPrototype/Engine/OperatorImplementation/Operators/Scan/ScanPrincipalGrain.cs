using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;
using Orleans.Runtime;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanPrinicipalGrain : PrincipalGrain, IScanPrincipalGrain
    {
        public override int DefaultNumGrainsInOneLayer { get { return 3; } }

        public override async Task<IWorkerGrain> GetOperatorGrain(string extension)
        {
            RequestContext.Set("ext",extension);
            var grain=this.GrainFactory.GetGrain<IScanOperatorGrain>(this.GetPrimaryKey(), extension);
            await grain.Init(grain,predicate,self);
            var slios=RequestContext.Get("return");
            if(slios!=null)
            {
                Console.WriteLine("receives: "+slios);
            }
            else
                Console.WriteLine("cannot return value from placement");
            RequestContext.Clear();
            return grain;
        }

        protected override void PassExtraParametersByPredicate(ref PredicateBase predicate)
        {
            ((ScanPredicate)predicate).NumberOfGrains=DefaultNumGrainsInOneLayer;
        }
    }
}