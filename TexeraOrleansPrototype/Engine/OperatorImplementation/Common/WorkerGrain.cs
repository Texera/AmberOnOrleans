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
        protected ulong windowLimit = 100;
        protected PredicateBase predicate = null;
        protected volatile bool isPaused = false;
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
        protected volatile bool taskDidPaused=false;
        //protected StreamSubscriptionHandle<Immutable<ControlMessage>> controlMessageStreamHandle;
        private ILocalSiloDetails localSiloDetails => this.ServiceProvider.GetRequiredService<ILocalSiloDetails>();

#if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
        private int breakPointTarget;
        private int breakPointCurrent;
        private int version=-1;
        private bool breakPointEnabled=false;
#endif
        public virtual async Task<SiloAddress> Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            this.self=self;
            this.principalGrain=principalGrain;
            this.predicate=predicate;
            Console.WriteLine("Init: "+Utils.GetReadableName(self));
            //var streamProvider = GetStreamProvider("SMSProvider");
            //var stream=streamProvider.GetStream<Immutable<ControlMessage>>(principalGrain.GetPrimaryKey(), "Ctrl");
            //controlMessageStreamHandle=await stream.SubscribeAsync(this);
            return localSiloDetails.SiloAddress;
            
        }
    

        public override Task OnDeactivateAsync()
        {
            Console.WriteLine("Deactivate: "+Utils.GetReadableName(self));
            pausedMessages=null;
            orderingEnforcer=null;
            sendStrategies=null;
            actionQueue=null;
            //controlMessageStreamHandle.UnsubscribeAsync();
            GC.Collect();
            return Task.CompletedTask;
        }

        protected void MakePayloadMessagesThenSend()
        {
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                strategy.Enqueue(outputTuples);
                strategy.SendBatchedMessages(self);
            }
            outputTuples=new List<TexeraTuple>();
            if(!isFinished && currentEndFlagCount==0)
            {
                Console.WriteLine(Utils.GetReadableName(self)+" END!!!!!!!!!");
                isFinished=true;
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
                #if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
                if(breakPointEnabled && localList.Count+breakPointCurrent>=breakPointTarget)
                {
                    breakPointCurrent+=localList.Count;
                    Pause();
                }
                #endif
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

        
        public Task<ulong> ReceivePayloadMessage(Immutable<PayloadMessage> message)
        {
            if(isPaused)
            {
                pausedMessages.Add(message);
                if(windowLimit>(ulong)actionQueue.Count)
                    return Task.FromResult(windowLimit-(ulong)actionQueue.Count);
                else
                    return Task.FromResult((ulong)10);
            }
            if(orderingEnforcer.PreProcess(message))
            {
                bool isEnd=message.Value.IsEnd;
                List<TexeraTuple> batch=message.Value.Payload;
                orderingEnforcer.CheckStashed(ref batch,ref isEnd, message.Value.SenderIdentifer);  
                var orleansScheduler=TaskScheduler.Current;
                Action action=()=>
                {
                    BeforeProcessBatch(message,orleansScheduler);
                    if(batch!=null)
                    {
                        ProcessBatch(batch);
                    }
                    if(isPaused)
                    {
                        #if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
                        if(breakPointEnabled && breakPointCurrent>=breakPointTarget)
                        {
                            await principalGrain.ReportCurrentValue(self,breakPointCurrent,version);
                        }
                        #endif
                        taskDidPaused=true;
                        return;
                    }
                    batch=null;
                    currentIndex=0;
                    if(isEnd)
                    {
                        string ext;
                        inputInfo[message.Value.SenderIdentifer.GetPrimaryKey(out ext)]--;
                        currentEndFlagCount--;
                        Console.WriteLine(Utils.GetReadableName(self)+" <- "+Utils.GetReadableName(message.Value.SenderIdentifer)+" END: "+message.Value.SequenceNumber);
                    }
                    AfterProcessBatch(message,orleansScheduler);
                    MakePayloadMessagesThenSend();
                    lock(actionQueue)
                    {
                        actionQueue.Dequeue();
                        if(actionQueue.Count>0)
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
            if(windowLimit>(ulong)actionQueue.Count)
                return Task.FromResult(windowLimit-(ulong)actionQueue.Count);
            else
                return Task.FromResult((ulong)10);
        }


        // private void SendPayloadMessageToSelf(Immutable<PayloadMessage> message, int retryCount)
        // {
        //     self.Process(message).ContinueWith((t)=>
        //     {  
        //         if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
        //         {
        //             Console.WriteLine(this.GetType().Name+"("+self+")"+" re-receive message with retry count "+retryCount);
        //             SendPayloadMessageToSelf(message, retryCount + 1); 
        //         }
        //     });
        // }

        protected virtual List<TexeraTuple> MakeFinalOutputTuples()
        {
            return null;
        }

        protected virtual void Pause()
        {
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            Console.WriteLine(Utils.GetReadableName(self)+" receives the pause message at "+ (int)t.TotalSeconds);
            lock(actionQueue)
            {
                Console.WriteLine("Paused: "+Utils.GetReadableName(self)+" actionQueue.Count = "+actionQueue.Count);
            }
            taskDidPaused=false;
            isPaused=true;
        }

        protected virtual void Resume()
        {
            lock(actionQueue)
            {
                 Console.WriteLine("Resumed: "+Utils.GetReadableName(self) +" taskDidPaused = "+taskDidPaused +" actionQueue.Count = "+actionQueue.Count);
            }
            isPaused=false;
            if(isFinished)
            {
                return;
            }
            lock(actionQueue)
            {
                if(actionQueue.Count>0 && taskDidPaused)
                {
                    new Task(actionQueue.Peek()).Start(TaskScheduler.Default);
                }
            }
            foreach(Immutable<PayloadMessage> message in pausedMessages)
            {
                ReceivePayloadMessage(message);
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

        public Task Generate()
        {
            var orleansScheduler=TaskScheduler.Current;
            Action action=async ()=>
            {
                if(isFinished)
                {
                    lock(actionQueue)
                    {
                        actionQueue.Clear();
                    }
                    return;
                }
                if(isPaused)
                {
                    Console.WriteLine(Utils.GetReadableName(self)+" Paused before generating tuples");
                    taskDidPaused=true;
                    return;
                }
                await GenerateTuples();
                if(isPaused)
                {
                    Console.WriteLine(Utils.GetReadableName(self)+" Paused after generating tuples");
                    taskDidPaused=true;
                    return;
                }
                MakePayloadMessagesThenSend();
                if(currentEndFlagCount!=0)
                {
                    StartGenerate(0);
                }
                lock(actionQueue)
                {
                    actionQueue.Dequeue();
                    if(actionQueue.Count>0)
                    {
                        Task.Run(actionQueue.Peek());
                    }
                }
            };
            lock(actionQueue)
            {
                if(actionQueue.Count<2)
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
        public Task ReceiveControlMessage(Immutable<ControlMessage> message)
        {
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            Console.WriteLine(Utils.GetReadableName(self)+" received control message at "+(int)t.TotalSeconds);
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
        
#if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
        public Task SetTargetValue(int targetValue)
        {
            breakPointEnabled=true;
            version++;
            breakPointCurrent=0;
            breakPointTarget=targetValue;
            Console.WriteLine(Utils.GetReadableName(self)+" set breakpoint! target value = "+targetValue+" version = "+version);
            return Task.CompletedTask;
        }

        public Task AskToReportCurrentValue()
        {
            if(!isPaused)
            {
                Pause();
            }
            principalGrain.ReportCurrentValue(self,breakPointCurrent,version);
            return Task.CompletedTask;
        }
#endif
    }
}
