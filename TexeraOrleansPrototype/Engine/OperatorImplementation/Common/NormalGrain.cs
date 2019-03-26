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

namespace Engine.OperatorImplementation.Common
{
    public class NormalGrain : Grain, INormalGrain
    {
        protected PredicateBase predicate = null;
        protected bool isPaused = false;
        protected List<Immutable<TexeraMessage>> pausedRows = new List<Immutable<TexeraMessage>>();
        protected IAsyncStream<Immutable<TexeraMessage>> stream=null;
        protected List<INormalGrain> nextGrains=new List<INormalGrain>();
        protected IPrincipalGrain root;//for reporting status
        protected INormalGrain self = null;
        IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();
        public virtual Task Init(PredicateBase predicate)
        {
            this.root=this.GrainFactory.GetGrain<IPrincipalGrain>(this.GetPrimaryKey(),"Principal");
            this.predicate=predicate;
            return Task.CompletedTask;
        }

        public virtual Task InitSelf()
        {
            string extension;
            this.self=this.GrainFactory.GetGrain<INormalGrain>(this.GetPrimaryKey(out extension),extension);
            return Task.CompletedTask;
        }

        public Task AddNextGrain(INormalGrain grain)
        {
            nextGrains.Add(grain);
            return Task.CompletedTask;
        }

        public virtual async Task Pause()
        {
            isPaused = true;
        }

        public virtual async Task Resume()
        {
            isPaused = false;
        }

        public Task AddNextGrain(List<INormalGrain> grains)
        {
            nextGrains.AddRange(grains);
            return Task.CompletedTask;
        }

        public virtual Task<bool> NeedCustomSending()
        {
            return Task.FromResult(false);
        }

        public Task AddNextStream(IAsyncStream<Immutable<TexeraMessage>> stream)
        {
            Trace.Assert(stream==null, "Overwritting output stream!");
            this.stream=stream;
            return Task.CompletedTask;
        }

        public virtual Task Start()
        {
            throw new NotImplementedException();
        }

        public virtual Task Receive(Immutable<TexeraMessage> message)
        {
            Trace.Assert(self!=null,"Worker Grain should have its own address to send message to itself!");
            List<TexeraTuple> tuples=orderingEnforcer.PreProcess(message);
            orderingEnforcer.CheckStashed(ref tuples,message.Value.sender);
            //assume message send to itself will never fail
            if(tuples!=null)
                self.Process(tuples.AsImmutable()).ContinueWith((t)=>
                {
                    if(t.IsCompleted)
                    {
                        //make an new texera message then send it to next worker with retry
                        INormalGrain receiver=GetNextOperatorGrain();
                        TexeraMessage messageToNext=new TexeraMessage();
                        messageToNext.sender=self;
                        messageToNext.sequenceNumber=orderingEnforcer.GetOutMessageSequenceNumber(receiver);
                        messageToNext.tuples=t.Result;
                        receiver.Receive(messageToNext.AsImmutable()).ContinueWith((t)=>
                        {
                            //retry logic 
                        });
                    }
                    else
                        throw new NotImplementedException();
                });
            
            return Task.CompletedTask;
        }

        public virtual List<TexeraTuple> Process(Immutable<List<TexeraTuple>> message)
        {
            List<TexeraTuple> output = new List<TexeraTuple>();
            foreach(TexeraTuple tuple in message.Value)
            {
                List<TexeraTuple> results=Process_impl(tuple);
                if(results!=null)
                    output.AddRange(results);
            }
            return output;
        }

        public virtual List<TexeraTuple> Process_impl(TexeraTuple tuple)
        {
            return null;
        }


        public virtual INormalGrain GetNextOperatorGrain()
        {
            return nextGrains[0];
        }
    }
}
