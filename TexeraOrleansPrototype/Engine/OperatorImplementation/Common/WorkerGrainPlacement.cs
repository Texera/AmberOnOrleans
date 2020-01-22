using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Orleans.Placement;
using Orleans.Runtime;
using Orleans.Runtime.Placement;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Common
{
    [Serializable]
    public class WorkerGrainPlacement : PlacementStrategy
    {
        internal static WorkerGrainPlacement Singleton { get; } = new WorkerGrainPlacement();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class WorkerGrainPlacementAttribute : PlacementAttribute
    {
        public WorkerGrainPlacementAttribute() : base(WorkerGrainPlacement.Singleton)
        {
        }
    }

    public class WorkerGrainPlacementDirector : IPlacementDirector
    {
        public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
        {
            var silos = context.GetCompatibleSilos(target).OrderBy(x=>x).ToArray();
            if(Constants.DefaultNumGrainsInOneLayer == 0)
            {
                Constants.DefaultNumGrainsInOneLayer = 4*(silos.Length-1);
            }
            var targetSilo=RequestContext.Get("targetSilo");
            if(targetSilo!=null)
            {
                foreach(SiloAddress silo in silos)
                {
                    if(silo.Endpoint.Address.ToString().Equals(targetSilo))
                    {
                        return Task.FromResult(silo);
                    }
                }
            }
            var excludeSilo=RequestContext.Get("excludeSilo");
            if(excludeSilo!=null)
            {
                silos=silos.Where(x=>!x.Endpoint.Address.ToString().Equals(excludeSilo)).ToArray();
            }
            
            object index = RequestContext.Get("grainIndex");
            if(index==null)
            {
                index=new Random().Next(0, silos.Count());
            }
            return Task.FromResult(silos[(int)index%silos.Count()]);
        }
    }
}