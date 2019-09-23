using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Orleans.Streams;
using Engine.OperatorImplementation.SendingSemantics;
using Orleans.Runtime;
using Engine.Breakpoint.LocalBreakpoint;

namespace Engine.OperatorImplementation.Common
{
    public interface IWorkerGrain : IGrainWithGuidCompoundKey
    {

        #region used by operators that have subsequent operators
        // Task AddNextGrain(Guid nextOperatorGuid, IWorkerGrain grain);
        // Task AddNextGrainList(Guid nextOperatorGuid, List<IWorkerGrain> grains);
        #endregion

        #region Used by all operators
        Task<SiloAddress> Init(IPrincipalGrain principalGrain, ITupleProcessor processor);
        Task<SiloAddress> Init(IPrincipalGrain principalGrain, ITupleProducer producer);
        Task SetSendStrategy(string id, ISendStrategy sendStrategy);
        Task AddInputInformation(IWorkerGrain sender);
        Task OnTaskDidPaused();
        Task OnTaskFinished();
        Task AddBreakpoint(LocalBreakpointBase breakpoint);
        Task<LocalBreakpointBase> QueryBreakpoint(string id);
        Task RemoveBreakpoint(string id);
        Task Pause();
        Task Resume();
        Task Deactivate();
        /*
        Just receives the payload message, sends message to itself to process it and returns. The
        method is coded to return ASAP, thus doesn't do any processing.
         */
        Task ReceivePayloadMessage(Immutable<PayloadMessage> message);
        Task ReceivePayloadMessage(PayloadMessage message);
        //Task Process(Immutable<PayloadMessage> message);
        #endregion

        #region Used by source operators
        Task Start();
        #endregion

    }
}
