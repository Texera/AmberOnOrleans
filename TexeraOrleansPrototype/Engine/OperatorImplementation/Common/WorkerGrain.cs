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
        protected PredicateBase predicate = null;
        protected bool isPaused = false;
        protected List<Immutable<PayloadMessage>> pausedMessages = new List<Immutable<PayloadMessage>>();
        protected IPrincipalGrain principalGrain;
        protected IWorkerGrain self = null;
        private IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();
        private Dictionary<Guid,ISendStrategy> sendStrategies = new Dictionary<Guid, ISendStrategy>();
        protected Dictionary<Guid,int> inputInfo=new Dictionary<Guid, int>();
        protected Queue<Action> actionQueue=new Queue<Action>();
        protected int currentIndex=0;
        protected int currentEndFlagCount=0;
        protected List<TexeraTuple> outputTuples=new List<TexeraTuple>();
        protected bool isFinished=false;
        protected StreamSubscriptionHandle<Immutable<ControlMessage>> controlMessageStreamHandle;
        private ILocalSiloDetails localSiloDetails => this.ServiceProvider.GetRequiredService<ILocalSiloDetails>();

        public virtual async Task<SiloAddress> Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            this.self=self;
            this.principalGrain=principalGrain;
            this.predicate=predicate;
            Console.WriteLine("Init: "+Utils.GetReadableName(self));
            var streamProvider = GetStreamProvider("SMSProvider");
            var stream=streamProvider.GetStream<Immutable<ControlMessage>>(principalGrain.GetPrimaryKey(), "Ctrl");
            controlMessageStreamHandle=await stream.SubscribeAsync(this);
            return localSiloDetails.SiloAddress;
            
        }
    

        public override Task OnDeactivateAsync()
        {
            Console.WriteLine("Deactivate: "+Utils.GetReadableName(self));
            pausedMessages=null;
            orderingEnforcer=null;
            sendStrategies=null;
            actionQueue=null;
            controlMessageStreamHandle.UnsubscribeAsync();
            GC.Collect();
            return Task.CompletedTask;
        }

        public Task Process(Immutable<PayloadMessage> message)
        {
            if(isPaused)
            {
                pausedMessages.Add(message);
                return Task.CompletedTask;
            }
            if(orderingEnforcer.PreProcess(message))
            {
                bool isEnd=message.Value.IsEnd;
                List<TexeraTuple> batch=message.Value.Payload;
                orderingEnforcer.CheckStashed(ref batch,ref isEnd, message.Value.SenderIdentifer);  
                var orleansScheduler=TaskScheduler.Current;
                Action action=async ()=>
                {
                    Console.WriteLine(Utils.GetReadableName(self)+" invokes process with seq num: "+message.Value.SequenceNumber+" from "+Utils.GetReadableName(message.Value.SenderIdentifer)+" is end: "+message.Value.IsEnd);
                    BeforeProcessBatch(message,orleansScheduler);
                    if(batch!=null)
                    {
                        ProcessBatch(batch);
                    }
                    batch=null;
                    if(isPaused)
                    {
                        return;
                    }
                    currentIndex=0;
                    if(isEnd)
                    {
                        string ext;
                        inputInfo[message.Value.SenderIdentifer.GetPrimaryKey(out ext)]--;
                        currentEndFlagCount--;
                        Console.WriteLine(Utils.GetReadableName(self)+" receives end flag from "+Utils.GetReadableName(message.Value.SenderIdentifer)+" current: "+currentEndFlagCount);
                    }
                    AfterProcessBatch(message,orleansScheduler);
                    await Task.Factory.StartNew(()=>{MakePayloadMessagesThenSend();},CancellationToken.None,TaskCreationOptions.None,orleansScheduler);
                    lock(actionQueue)
                    {
                        actionQueue.Dequeue();
                        if(!isPaused && actionQueue.Count>0)
                        {
                            Task.Run(actionQueue.Peek());
                        }
                    }
                };
                lock(actionQueue)
                {
                    actionQueue.Enqueue(action);
                    if(actionQueue.Count==1)
                    {
                        Task.Run(action);
                    }
                }
            }
            return Task.CompletedTask;
        }

        protected void MakePayloadMessagesThenSend()
        {
            // if(isFinished)
            // {
            //     Console.WriteLine("error on "+Utils.GetReadableName(this)+": ready to send payload "+(outputTuples!=null?outputTuples.Count.ToString():"null"));
            // }
            if(sendStrategies==null)
            {
                Console.WriteLine("ERROR: detect a payload with size "+outputTuples.Count);
                Console.WriteLine("ERROR: "+Utils.GetReadableName(this)+" tries to send message but sendStrategies is null");
            }
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                strategy.Enqueue(outputTuples);
                strategy.SendBatchedMessages(self);
            }
            outputTuples=new List<TexeraTuple>();
            if(!isFinished && currentEndFlagCount==0 && actionQueue.Count==1)
            {
                isFinished=true;
                Console.WriteLine("Finished: "+Utils.GetReadableName(self));
                MakeLastPayloadMessageThenSend();
            }
        }

        private void MakeLastPayloadMessageThenSend()
        {
            List<TexeraTuple> output=MakeFinalOutputTuples();
            if(output!=null)
            {
                outputTuples.AddRange(output);
            }
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                strategy.Enqueue(outputTuples);
                strategy.SendBatchedMessages(self);
                strategy.SendEndMessages(self);
            }
            outputTuples= new List<TexeraTuple>();
        }


        protected virtual void BeforeProcessBatch(Immutable<PayloadMessage> message, TaskScheduler orleansScheduler)
        {

        }

        protected virtual void AfterProcessBatch(Immutable<PayloadMessage> message, TaskScheduler orleansScheduler)
        {

        }
        protected void ProcessBatch(List<TexeraTuple> batch)
        {
            List<TexeraTuple> localList=new List<TexeraTuple>();
            for(;currentIndex<batch.Count;++currentIndex)
            {
                if(isPaused)
                {
                    lock(outputTuples)
                    {
                        outputTuples.AddRange(localList);
                        localList=null;
                    }
                    return;
                }
                ProcessTuple(batch[currentIndex],localList);
            }
            lock(outputTuples)
            {
                outputTuples.AddRange(localList);
                localList=null;
            }
        }

        protected virtual void ProcessTuple(TexeraTuple tuple, List<TexeraTuple> output)
        {

        }

        
        public Task ReceivePayloadMessage(Immutable<PayloadMessage> message)
        {
            //Console.WriteLine(MakeIdentifier(self) + " received message from "+message.Value.SenderIdentifer+"with seqnum "+message.Value.SequenceNumber);
            SendPayloadMessageToSelf(message,0);
            return Task.CompletedTask;
        }


        private void SendPayloadMessageToSelf(Immutable<PayloadMessage> message, int retryCount)
        {
            self.Process(message).ContinueWith((t)=>
            {  
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                {
                    Console.WriteLine(this.GetType().Name+"("+self+")"+" re-receive message with retry count "+retryCount);
                    SendPayloadMessageToSelf(message, retryCount + 1); 
                }
            });
        }

        protected virtual List<TexeraTuple> MakeFinalOutputTuples()
        {
            return null;
        }


        // public string ReturnGrainIndentifierString(IWorkerGrain grain)
        // {
        //     //string a="Engine.OperatorImplementation.Operators.OrleansCodeGen";
        //     string extension;
        //     //grain.GetPrimaryKey(out extension);
        //     return grain.GetPrimaryKey(out extension).ToString()+" "+extension;
        // }

        protected virtual void Pause()
        {
            Console.WriteLine("Paused: "+Utils.GetReadableName(self));
            isPaused=true;
        }

        protected virtual void Resume()
        {
            Console.WriteLine("Resumed: "+Utils.GetReadableName(self));
            isPaused=false;
            if(isFinished)
            {
                return;
            }
            if(actionQueue.Count>0)
            {
                new Task(actionQueue.Peek()).Start(TaskScheduler.Default);
            }
            foreach(Immutable<PayloadMessage> message in pausedMessages)
            {
                SendPayloadMessageToSelf(message,0);
            }
            pausedMessages=new List<Immutable<PayloadMessage>>();
        }

       
        protected virtual void Start()
        {
            currentEndFlagCount=-1;
        }

        public Task AddInputInformation(Pair<Guid,int> inputInfo)
        {
            Console.WriteLine("Linking: "+Utils.GetReadableName(self)+" will receive "+inputInfo.Second+" end flags from "+inputInfo.First.ToString().Substring(0,8));
            currentEndFlagCount+=inputInfo.Second;
            if(this.inputInfo.ContainsKey(inputInfo.First))
            {
                this.inputInfo[inputInfo.First]+=inputInfo.Second;
            }
            else
            {
                this.inputInfo.Add(inputInfo.First,inputInfo.Second);
            }
            return Task.CompletedTask;
        }

        public async Task Generate()
        {
            if(!isPaused)
            {
                var orleansScheduler=TaskScheduler.Current;
                Action action=async ()=>
                {
                    if(isPaused)
                    {
                        return;
                    }
                    await GenerateTuples();
                    if(isPaused)
                    {
                        return;
                    }
                    if(!isFinished || outputTuples.Count>0)
                    {
                        await Task.Factory.StartNew(()=>
                        {
                            MakePayloadMessagesThenSend();
                            StartGenerate(0);
                        },CancellationToken.None,TaskCreationOptions.None,orleansScheduler);
                        lock(actionQueue)
                        {
                            actionQueue.Dequeue();
                            if(!isPaused && actionQueue.Count>0)
                            {
                                Task.Run(actionQueue.Peek());
                            }
                        }
                    }
                    else
                    {
                        await Task.Factory.StartNew(()=>
                        {
                            foreach(ISendStrategy strategy in sendStrategies.Values)
                            {
                                strategy.SendEndMessages(self);
                            }
                            Console.WriteLine("Finished: "+Utils.GetReadableName(self));
                        },CancellationToken.None,TaskCreationOptions.None,orleansScheduler);
                        lock(actionQueue)
                        {
                            actionQueue.Clear();
                        }
                    }
                };
                lock(actionQueue)
                {
                    actionQueue.Enqueue(action);
                    if(actionQueue.Count==1)
                    {
                        Task.Run(action);
                    }
                }
            }
        }

        protected async virtual Task GenerateTuples()
        {
            
        }

        protected void StartGenerate(int retryCount)
        {
            self.Generate().ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                {
                    Console.WriteLine(this.GetType().Name+"("+self+")"+" re-receive message with retry count "+retryCount);
                    StartGenerate(retryCount+1);
                }
            });
        }

        public Task SetSendStrategy(Guid operatorGuid,ISendStrategy sendStrategy)
        {
            sendStrategies[operatorGuid]=sendStrategy;
            return Task.CompletedTask;
        }

        public Task OnNextAsync(Immutable<ControlMessage> message, StreamSequenceToken token = null)
        {
            List<ControlMessage.ControlMessageType> executeSequence = orderingEnforcer.PreProcess(message);
            if(executeSequence!=null)
            {
                orderingEnforcer.CheckStashed(ref executeSequence,message.Value.SenderIdentifer);
                foreach(ControlMessage.ControlMessageType type in executeSequence)
                {
                    switch(type)
                    {
                        case ControlMessage.ControlMessageType.Pause:
                            Pause();
                            break;
                        case ControlMessage.ControlMessageType.Resume:
                            Resume();
                            break;
                        case ControlMessage.ControlMessageType.Start:
                            Start();
                            break;
                        case ControlMessage.ControlMessageType.Deactivate:
                            DeactivateOnIdle();
                            break;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task OnCompletedAsync()
        {
            throw new NotImplementedException();
        }

        public Task OnErrorAsync(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
