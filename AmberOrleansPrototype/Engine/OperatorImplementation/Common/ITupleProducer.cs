using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Orleans.Streams;
using System.Diagnostics;
using Engine.OperatorImplementation.MessagingSemantics;
using Engine.OperatorImplementation.SendingSemantics;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
namespace Engine.OperatorImplementation.Common
{
    public interface ITupleProducer
    {
        Task Initialize();

        bool HasNext();

        TexeraTuple Next();

        Task<TexeraTuple> NextAsync();

        void Dispose();
    }
}