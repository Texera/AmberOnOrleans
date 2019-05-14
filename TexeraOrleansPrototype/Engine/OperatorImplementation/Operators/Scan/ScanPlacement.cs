using System;
using System.Linq;
using System.Threading.Tasks;
using Orleans.Placement;
using Orleans.Runtime;
using Orleans.Runtime.Placement;

namespace Engine.OperatorImplementation.Operators
{
    [Serializable]
    public class ScanPlacement : PlacementStrategy
    {
        internal static ScanPlacement Singleton { get; } = new ScanPlacement();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ScanPlacementAttribute : PlacementAttribute
    {
        public ScanPlacementAttribute() : base(ScanPlacement.Singleton)
        {
        }
    }

    public class ScanPlacementDirector : IPlacementDirector
    {
        public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
        {
            var a=RequestContext.Get("ext");
            var silos = context.GetCompatibleSilos(target).OrderBy(s => s).ToArray();
            Console.WriteLine("request context: "+a);
            RequestContext.Set("return",silos);
            return Task.FromResult(silos[0]);
        }
    }
}