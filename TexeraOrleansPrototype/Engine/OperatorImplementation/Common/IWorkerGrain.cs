using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Orleans.Streams;
using Engine.OperatorImplementation.SendingSemantics;

namespace Engine.OperatorImplementation.Common
{
    public interface IWorkerGrain : IGrainWithGuidCompoundKey
    {

        #region used by operators that have subsequent operators
        Task AddNextGrain(Guid nextOperatorGuid, IWorkerGrain grain);
        Task AddNextGrainList(Guid nextOperatorGuid, List<IWorkerGrain> grains);
        #endregion

        #region Used by all operators
        Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain);
        Task SetSendStrategy(Guid operatorGuid, ISendStrategy sendStrategy);
        Task SetTargetEndFlagCount(int target);
        /*
        Receives and processes the control message completely. This is because the
        control message needs to be acted upon ASAP.
         */
        Task ProcessControlMessage(Immutable<ControlMessage> message);

        /*
        Just receives the payload message, sends message to itself to process it and returns. The
        method is coded to return ASAP, thus doesn't do any processing.
         */
        Task ReceivePayloadMessage(Immutable<PayloadMessage> message);
        Task Process(Immutable<PayloadMessage> message);
        #endregion

        #region Used by source operators
        Task Generate();
        #endregion
    }
}
