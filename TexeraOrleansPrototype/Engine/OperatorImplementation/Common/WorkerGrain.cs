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
using Orleans.Placement;
using Orleans.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Engine.Breakpoint.LocalBreakpoint;

namespace Engine.OperatorImplementation.Common
{
    public class Pair<T, U> 
    {
        public Pair(T first, U second) 
        {
            this.First = first;
            this.Second = second;
        }

        public T First { get; set; }
        public U Second { get; set; }
    };

    [WorkerGrainPlacement]
    public class WorkerGrain : Grain, IWorkerGrain
    {
        public enum ThreadStatus
        {
            Idle=0,
            Running=1,
            Pausing=2,
            Paused=4,
        }

        protected bool isActive
        {
            get
            {
                return (int)currentStatus<2;
            }
        }

        protected bool isFinished=false;
        protected ThreadStatus currentStatus=ThreadStatus.Idle;
        protected ITupleProcessor processor = null;
        protected ITupleProducer producer = null;
        protected IPrincipalGrain principalGrain;
        protected IWorkerGrain self = null;
        private IOrderingEnforcer orderingEnforcer;
        private Dictionary<string,ISendStrategy> sendStrategies = new Dictionary<string, ISendStrategy>();
        protected Dictionary<Guid, HashSet<IGrain>> unFinishedUpstream = new Dictionary<Guid,HashSet<IGrain>>();
        protected Queue<Action> actionQueue=new Queue<Action>();
        protected int currentIndex=0;
        protected bool messageChecked=false;
        protected List<TexeraTuple> savedBatch=null;

        #if (PROFILING_ENABLED)
        protected TimeSpan processTime=new TimeSpan(0,0,0);
        protected TimeSpan sendingTime=new TimeSpan(0,0,0);
        protected TimeSpan preprocessTime=new TimeSpan(0,0,0);
        #endif
        private ILocalSiloDetails localSiloDetails => this.ServiceProvider.GetRequiredService<ILocalSiloDetails>();
        protected List<LocalBreakpointBase> activeBreakpoints=new List<LocalBreakpointBase>();
        public virtual async Task<SiloAddress> Init(IPrincipalGrain principalGrain,ITupleProcessor processor)
        {
            this.self = this.GrainReference.Cast<IWorkerGrain>();
            Console.WriteLine("Init Start: "+Utils.GetReadableName(self));
            string ext;
            this.GetPrimaryKey(out ext);
            this.orderingEnforcer = new OrderingGrainWithSequenceNumber(ext);
            this.principalGrain = principalGrain;
            this.processor = processor;
            await this.processor.Initialize();
            Console.WriteLine("Init Finished: "+Utils.GetReadableName(self));
            return localSiloDetails.SiloAddress;
        }
    

        public override Task OnDeactivateAsync()
        {
            Console.WriteLine("Deactivate: "+Utils.GetReadableName(self));
            processor = null;
            producer = null;
            orderingEnforcer=null;
            sendStrategies=null;
            actionQueue=null;
            GC.Collect();
            return Task.CompletedTask;
        }

        protected async Task MakePayloadMessagesThenSend(List<TexeraTuple> outputTuples)
        {
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                strategy.Enqueue(outputTuples);
                strategy.SendBatchedMessages(self);
            }
            if(!isFinished && unFinishedUpstream.Count == 0)
            {
                Console.WriteLine("Info: "+Utils.GetReadableName(self)+" start to produce final batch");
                #if (PROFILING_ENABLED)
                Console.WriteLine(Utils.GetReadableName(self)+" Preprocess Time: "+preprocessTime+" Process Time: "+processTime+" Sending Time: "+sendingTime);
                #endif
                await MakeLastPayloadMessageThenSend();
            }
        }

        protected async Task<bool> MakeFinalOutputTuples(List<TexeraTuple> outputList)
        {
            processor.NoMore();
            while(processor.HasNext())
            {
                var output = processor.Next();
                outputList.Add(output);
                List<LocalBreakpointBase> reachedBreakpoints=null;
                foreach(LocalBreakpointBase breakpoint in activeBreakpoints)
                {
                    breakpoint.Accept(output);
                    if(breakpoint.IsTriggered)
                    {
                        if(reachedBreakpoints==null)
                            reachedBreakpoints=new List<LocalBreakpointBase>{breakpoint};
                        else
                            reachedBreakpoints.Add(breakpoint);
                    }
                }
                if(reachedBreakpoints!=null)
                {
                    await principalGrain.OnWorkerLocalBreakpointTriggered(self,reachedBreakpoints);
                    await self.Pause();
                }
                if(currentStatus==ThreadStatus.Pausing)
                {
                    return false;
                }
            }
            return true;
        }

        private async Task MakeLastPayloadMessageThenSend()
        {
            List<TexeraTuple> output=new List<TexeraTuple>();
            bool res = true;
            if(processor != null)
            {
                res = await MakeFinalOutputTuples(output);
                if(output.Count > 0)
                {
                    foreach(ISendStrategy strategy in sendStrategies.Values)
                    {
                        strategy.Enqueue(output);
                        strategy.SendBatchedMessages(self);
                    }
                }
            }
            if(res)
            {
                foreach(ISendStrategy strategy in sendStrategies.Values)
                {
                    strategy.SendEndMessages(self);
                }
                if(processor != null)
                {
                    processor.Dispose();
                }
                if(producer != null)
                {
                    producer.Dispose();
                }
                await self.OnTaskFinished();
            }
        }
        protected async Task ProcessBatch(List<TexeraTuple> batch,List<TexeraTuple> outputList)
        {
            while(processor.HasNext())
            {
                var output = processor.Next();
                outputList.Add(output);
                List<LocalBreakpointBase> reachedBreakpoints=null;
                foreach(LocalBreakpointBase breakpoint in activeBreakpoints)
                {
                    breakpoint.Accept(output);
                    if(breakpoint.IsTriggered)
                    {
                        if(reachedBreakpoints==null)
                            reachedBreakpoints=new List<LocalBreakpointBase>{breakpoint};
                        else
                            reachedBreakpoints.Add(breakpoint);
                    }
                }
                if(reachedBreakpoints!=null)
                {
                    await principalGrain.OnWorkerLocalBreakpointTriggered(self,reachedBreakpoints);
                    await self.Pause();
                }
                if(currentStatus==ThreadStatus.Pausing)
                {
                    return;
                }
            }
            int limit=batch.Count;
            for(;currentIndex<limit;)
            {
                processor.Accept(batch[currentIndex++]);
                while(processor.HasNext())
                {
                    var output = processor.Next();
                    outputList.Add(output);
                    List<LocalBreakpointBase> reachedBreakpoints=null;
                    foreach(LocalBreakpointBase breakpoint in activeBreakpoints)
                    {
                        breakpoint.Accept(output);
                        if(breakpoint.IsTriggered)
                        {
                            if(reachedBreakpoints==null)
                                reachedBreakpoints=new List<LocalBreakpointBase>{breakpoint};
                            else
                                reachedBreakpoints.Add(breakpoint);
                        }
                    }
                    if(reachedBreakpoints!=null)
                    {
                        await principalGrain.OnWorkerLocalBreakpointTriggered(self,reachedBreakpoints);
                        await self.Pause();
                    }
                    if(currentStatus==ThreadStatus.Pausing)
                    {
                        return;
                    }
                }
            }
        }
 
        public Task ReceivePayloadMessage(Immutable<PayloadMessage> message)
        {
            Process(message.Value);
            return Task.CompletedTask;
        }

        public Task ReceivePayloadMessage(PayloadMessage message)
        {
            Process(message);
            return Task.CompletedTask;
        }

        public virtual void Process(PayloadMessage message)
        {
            Action action=async ()=>
            {
                if(currentStatus==ThreadStatus.Pausing)
                {
                    await self.OnTaskDidPaused();
                    return;
                }
                #if (PROFILING_ENABLED)
                DateTime start=DateTime.UtcNow;
                #endif
                if(messageChecked || orderingEnforcer.PreProcess(message))
                {
                    bool isEnd=message.IsEnd;
                    List<TexeraTuple> batch;
                    if(savedBatch!=null)
                    {
                        batch=savedBatch;
                    }
                    else
                    {
                        batch=message.Payload;
                    }
                    if(!messageChecked)
                    {
                        string sender;
                        message.SenderIdentifer.GetPrimaryKey(out sender);
                        orderingEnforcer.CheckStashed(ref batch,ref isEnd, sender);
                        savedBatch=batch;
                        messageChecked=true;
                    }
                    #if (PROFILING_ENABLED)  
                    preprocessTime+=DateTime.UtcNow-start;
                    start=DateTime.UtcNow;
                    #endif
                    List<TexeraTuple> outputList=new List<TexeraTuple>();
                    string ext;
                    Guid from = message.SenderIdentifer.GetPrimaryKey(out ext);
                    processor.OnRegisterSource(from);
                    if(batch!=null)
                    {
                        await ProcessBatch(batch,outputList);
                    }
                    if(currentStatus==ThreadStatus.Pausing)
                    {
                        //if we do not do so, the outputlist will be lost.
                        Console.WriteLine("Info: "+Utils.GetReadableName(self)+" paused while processing");
                        await MakePayloadMessagesThenSend(outputList);
                        await self.OnTaskDidPaused();
                        return;
                    }
                    batch=null;
                    savedBatch=null;
                    currentIndex=0;
                    messageChecked=false;
                    if(isEnd)
                    {
                        unFinishedUpstream[from].Remove(message.SenderIdentifer);
                        if(unFinishedUpstream[from].Count == 0)
                        {
                            unFinishedUpstream.Remove(from);
                            processor.MarkSourceCompleted(from);
                        }
                    }
                    #if (PROFILING_ENABLED)
                    processTime+=DateTime.UtcNow-start;
                    start=DateTime.UtcNow;
                    #endif
                    await MakePayloadMessagesThenSend(outputList);
                    #if (PROFILING_ENABLED)
                    sendingTime+=DateTime.UtcNow-start;
                    #endif
                }
                bool noMoreAction=false;
                lock(actionQueue)
                {
                    actionQueue.Dequeue();
                    noMoreAction=actionQueue.Count==0;
                    if(!noMoreAction)
                    {
                        Task.Run(actionQueue.Peek());
                    }
                }
                if(noMoreAction && currentStatus==ThreadStatus.Pausing)
                {
                    await self.OnTaskDidPaused();
                }
            };
            lock(actionQueue)
            {
                actionQueue.Enqueue(action);
                if(isActive && actionQueue.Count==1)
                {
                    currentStatus=ThreadStatus.Running;
                    Task.Run(action);
                }
            }
            //return Task.CompletedTask;
        }


        public virtual Task Pause()
        {
            #if (PROFILING_ENABLED)
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            Console.WriteLine(Utils.GetReadableName(self)+" receives the pause message at "+ (int)t.TotalSeconds);
            #endif
            if(currentStatus==ThreadStatus.Pausing)
            {
                return Task.CompletedTask;
            }
            else if(currentStatus==ThreadStatus.Paused || isFinished)
            {
                return OnTaskDidPaused();
            }
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                strategy.SetPauseFlag(true);
            }
            currentStatus=ThreadStatus.Pausing;
            lock(actionQueue)
            {
                if(actionQueue.Count==0)
                    OnTaskDidPaused();
            }
            return Task.CompletedTask;
        }

        public virtual Task Resume()
        {
            if(isFinished)
            {
                Console.WriteLine("Info: " + Utils.GetReadableName(self)+" already finished! so Resume won't have any effect.");
                return Task.CompletedTask;
            }
            if(currentStatus!=ThreadStatus.Paused)
            {
                Console.WriteLine("ERROR: "+Utils.GetReadableName(self)+" invaild state when resume! state = "+currentStatus.ToString());
                return Task.CompletedTask;
            }
            currentStatus=ThreadStatus.Idle;
            Task.Delay(100).ContinueWith((t)=>
            {
                foreach(ISendStrategy strategy in sendStrategies.Values)
                {
                    strategy.SetPauseFlag(false);
                }
            });
            lock(actionQueue)
            {
                if(actionQueue.Count==0)
                {
                    Task.Delay(100).ContinueWith((t)=>
                    {
                        Task.Run(()=>
                        {
                            foreach(ISendStrategy strategy in sendStrategies.Values)
                            {
                                strategy.ResumeSending();
                            }
                        });
                    });
                }
            }
            lock(actionQueue)
            {
                if(actionQueue.Count>0)
                {
                    currentStatus=ThreadStatus.Running;
                    Task.Run(actionQueue.Peek());
                }
                else if(!isFinished && unFinishedUpstream.Count == 0)
                {
                    Task.Run(async ()=>{await MakeLastPayloadMessageThenSend();});
                }
            }
            if(producer != null)
            {
                if(currentStatus==ThreadStatus.Idle && !isFinished)
                {
                    Console.WriteLine("Resume: "+Common.Utils.GetReadableName(self)+" restart scanning");
                    Task.Run(()=>Generate());
                }
            }
            Console.WriteLine("Resumed: "+Utils.GetReadableName(self));
            return Task.CompletedTask;
        }

       
        public async Task Start()
        {
            string ext;
            var t = self.GetPrimaryKey(out ext);
            await this.producer.Initialize();
            unFinishedUpstream[t]=new HashSet<IGrain>{self};
            Console.WriteLine("Start: "+Utils.GetReadableName(self));
            Task.Run(()=>{Generate();});
        }

        public Task AddInputInformation(IWorkerGrain sender)
        {
            string ext;
            Guid id = sender.GetPrimaryKey(out ext);
            if(unFinishedUpstream.ContainsKey(id))
            {
                unFinishedUpstream[id].Add(sender);
            }
            else
            {
                unFinishedUpstream[id] = new HashSet<IGrain>{sender};
            }
            return Task.CompletedTask;
        }

        public async void Generate()
        {
            currentStatus=ThreadStatus.Running;
            while(true)
            {
                if(!isActive || isFinished)
                {
                    if(currentStatus==ThreadStatus.Pausing)
                    {
                        await self.OnTaskDidPaused();
                    }
                    break;
                }
                #if (PROFILING_ENABLED)
                DateTime start=DateTime.UtcNow;
                #endif
                List<TexeraTuple> outputList=new List<TexeraTuple>();
                await GenerateTuples(outputList);
                #if (PROFILING_ENABLED)
                processTime+=DateTime.UtcNow-start;
                start=DateTime.UtcNow;
                #endif
                await MakePayloadMessagesThenSend(outputList);
                #if (PROFILING_ENABLED)
                sendingTime+=DateTime.UtcNow-start;
                #endif
            }
        }

        async Task GenerateTuples(List<TexeraTuple> outputList)
        {
            int i=0;
            while(i<1000 && producer.HasNext())
            {
                var output = await producer.NextAsync();
                if(output.FieldList != null)
                {
                    outputList.Add(output);
                    i++;
                    List<LocalBreakpointBase> reachedBreakpoints=null;
                    foreach(LocalBreakpointBase breakpoint in activeBreakpoints)
                    {
                        breakpoint.Accept(output);
                        if(breakpoint.IsTriggered)
                        {
                            if(reachedBreakpoints==null)
                                reachedBreakpoints=new List<LocalBreakpointBase>{breakpoint};
                            else
                                reachedBreakpoints.Add(breakpoint);
                        }
                    }
                    if(reachedBreakpoints!=null)
                    {
                        await principalGrain.OnWorkerLocalBreakpointTriggered(self,reachedBreakpoints);
                        await self.Pause();
                    }
                    if(currentStatus==ThreadStatus.Pausing)
                    {
                        return;
                    }
                }
            }
            if(!producer.HasNext())
            {
                unFinishedUpstream.Clear();
            }
        }

        public async Task SetSendStrategy(string id,ISendStrategy sendStrategy)
        {
            Console.WriteLine("Link: "+Utils.GetReadableName(self)+" "+sendStrategy);
            foreach(var receiver in sendStrategy.GetReceivers())
            {
                await receiver.AddInputInformation(self);
            }
            sendStrategies[id]=sendStrategy;
        }

        public Task OnTaskDidPaused()
        {
            //Console.WriteLine("Info: "+Utils.GetReadableName(self)+" currentEndFlagCount: "+currentEndFlagCount);
            currentStatus=ThreadStatus.Paused;
            Console.WriteLine("Paused: "+Utils.GetReadableName(self));
            principalGrain.OnWorkerDidPaused(self);
            return Task.CompletedTask;
        }

        public Task Deactivate()
        {
            DeactivateOnIdle();
            return Task.CompletedTask;
        }

        public Task OnTaskFinished()
        {
            isFinished=true;
            Console.WriteLine("Finish: "+Utils.GetReadableName(self));
            principalGrain.OnWorkerFinished(self);
            return Task.CompletedTask;
        }

        public Task<SiloAddress> Init(IPrincipalGrain principalGrain, ITupleProducer producer)
        {
            this.self = this.GrainReference.Cast<IWorkerGrain>();
            Console.WriteLine("Init Start: "+Utils.GetReadableName(self));
            this.principalGrain = principalGrain;
            this.producer = producer;
            Console.WriteLine("Init Finished: "+Utils.GetReadableName(self));
            return Task.FromResult(localSiloDetails.SiloAddress);
        }

        public Task AddBreakpoint(LocalBreakpointBase breakpoint)
        {
            activeBreakpoints.Add(breakpoint);
            return Task.CompletedTask;
        }

        public Task<LocalBreakpointBase> QueryBreakpoint(string id)
        {
            foreach(LocalBreakpointBase breakpoint in activeBreakpoints)
            {
                if(breakpoint.id.Equals(id))
                {
                    return Task.FromResult(breakpoint);
                }
            }
            return Task.FromException<LocalBreakpointBase>(new KeyNotFoundException());
        }

        public Task RemoveBreakpoint(string id)
        {
            for(int i=activeBreakpoints.Count-1;i>=0;--i)
            {
                if(activeBreakpoints[i].id.Equals(id))
                {
                    activeBreakpoints.RemoveAt(i);
                }
            }
            return Task.CompletedTask;
        }
    }
}
