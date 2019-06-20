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
    public struct Pair<T, U> 
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
        protected volatile bool isPaused = false;
        //protected List<Immutable<PayloadMessage>> pausedMessages = new List<Immutable<PayloadMessage>>();
        protected IPrincipalGrain principalGrain;
        protected IWorkerGrain self = null;
        private IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();
        private Dictionary<Guid,ISendStrategy> sendStrategies = new Dictionary<Guid, ISendStrategy>();
        protected Dictionary<Guid,int> inputInfo=new Dictionary<Guid, int>();
        protected Queue<Action> actionQueue=new Queue<Action>();
        protected int currentIndex=0;
        protected bool messageChecked=false;
        protected int currentEndFlagCount=0;
        protected bool isFinished=false;
        protected List<TexeraTuple> savedBatch=null;
        protected volatile bool taskDidPaused=false;
        protected TimeSpan processTime=new TimeSpan(0,0,0);
        protected TimeSpan sendingTime=new TimeSpan(0,0,0);
        protected TimeSpan preprocessTime=new TimeSpan(0,0,0);
        //protected StreamSubscriptionHandle<Immutable<ControlMessage>> controlMessageStreamHandle;
        protected IWorkerGrain workerToActivate=null;
        private ILocalSiloDetails localSiloDetails => this.ServiceProvider.GetRequiredService<ILocalSiloDetails>();

#if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
        private int breakPointTarget;
        private int breakPointCurrent=0;
        private int version=-1;
        private bool breakPointEnabled=false;
#endif
        public virtual Task<SiloAddress> Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            this.self=self;
            this.principalGrain=principalGrain;
            this.predicate=predicate;
            Console.WriteLine("Init: "+Utils.GetReadableName(self));
            //var streamProvider = GetStreamProvider("SMSProvider");
            //var stream=streamProvider.GetStream<Immutable<ControlMessage>>(principalGrain.GetPrimaryKey(), "Ctrl");
            //controlMessageStreamHandle=await stream.SubscribeAsync(this);
            return Task.FromResult(localSiloDetails.SiloAddress);
            
        }
    

        public override Task OnDeactivateAsync()
        {
            Console.WriteLine("Deactivate: "+Utils.GetReadableName(self));
            //pausedMessages=null;
            orderingEnforcer=null;
            sendStrategies=null;
            actionQueue=null;
            //controlMessageStreamHandle.UnsubscribeAsync();
            GC.Collect();
            return Task.CompletedTask;
        }

        protected void MakePayloadMessagesThenSend(List<TexeraTuple> outputTuples)
        {
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                strategy.Enqueue(outputTuples);
                strategy.SendBatchedMessages(self);
            }
            if(!isFinished && currentEndFlagCount==0)
            {
                Console.WriteLine(Utils.GetReadableName(self)+" END!");
                Console.WriteLine(Utils.GetReadableName(self)+" Preprocess Time: "+preprocessTime+" Process Time: "+processTime+" Sending Time: "+sendingTime);
                isFinished=true;
                MakeLastPayloadMessageThenSend();
            }
        }

        private void MakeLastPayloadMessageThenSend()
        {
            List<TexeraTuple> output=MakeFinalOutputTuples();
            if(output!=null)
            {
                foreach(ISendStrategy strategy in sendStrategies.Values)
                {
                    strategy.Enqueue(output);
                    strategy.SendBatchedMessages(self);
                }
            }
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                strategy.SendEndMessages(self);
            }
            if(workerToActivate!=null)
            {
                workerToActivate.ReceiveControlMessage(new Immutable<ControlMessage>(new ControlMessage(self,0,ControlMessage.ControlMessageType.Start)));
            }
        }


        protected virtual void BeforeProcessBatch(PayloadMessage message)
        {

        }

        protected virtual void AfterProcessBatch(PayloadMessage message)
        {

        }
        protected void ProcessBatch(List<TexeraTuple> batch,List<TexeraTuple> outputList)
        {
            int limit=batch.Count;
            for(;currentIndex<limit;++currentIndex)
            {
                #if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
                if(breakPointEnabled && outputList.Count+breakPointCurrent>=breakPointTarget)
                {
                    Pause();
                }
                #endif
                if(isPaused)
                {
                    return;
                }
                ProcessTuple(batch[currentIndex],outputList);
            }
        }

        protected virtual void ProcessTuple(in TexeraTuple tuple, List<TexeraTuple> output)
        {

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



        public void Process(PayloadMessage message)
        {
            Action action=()=>
            {
                if(isPaused)
                {
                    taskDidPaused=true;
                    return;
                }
                DateTime start=DateTime.UtcNow;
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
                        orderingEnforcer.CheckStashed(ref batch,ref isEnd, message.SenderIdentifer);
                        savedBatch=batch;
                        messageChecked=true;
                    }  
                    preprocessTime+=DateTime.UtcNow-start;
                    start=DateTime.UtcNow;
                    BeforeProcessBatch(message);
                    List<TexeraTuple> outputList=new List<TexeraTuple>();
                    if(batch!=null)
                    {
                        ProcessBatch(batch,outputList);
                        #if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
                        breakPointCurrent+=outputList.Count;
                        #endif
                    }
                    if(isPaused)
                    {
                        #if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
                        if(breakPointEnabled && breakPointCurrent>=breakPointTarget)
                        {
                            await principalGrain.ReportCurrentValue(self,breakPointCurrent,version);
                        }
                        #endif
                        //if we not do so, the outputlist will be lost.
                        MakePayloadMessagesThenSend(outputList);
                        taskDidPaused=true;
                        return;
                    }
                    batch=null;
                    savedBatch=null;
                    currentIndex=0;
                    messageChecked=false;
                    if(isEnd)
                    {
                        string ext;
                        inputInfo[message.SenderIdentifer.GetPrimaryKey(out ext)]--;
                        currentEndFlagCount--;
                        Console.WriteLine(Utils.GetReadableName(self)+" <- "+Utils.GetReadableName(message.SenderIdentifer)+" END: "+message.SequenceNumber);
                    }
                    AfterProcessBatch(message);
                    processTime+=DateTime.UtcNow-start;
                    start=DateTime.UtcNow;
                    MakePayloadMessagesThenSend(outputList);
                    sendingTime+=DateTime.UtcNow-start;
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
                actionQueue.Enqueue(action);
                if(actionQueue.Count==1)
                {
                    Task.Run(action);
                }
            }
            //return Task.CompletedTask;
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
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                strategy.SetPauseFlag(true);
            }
            taskDidPaused=false;
            isPaused=true;
        }

        protected virtual void Resume()
        {
            lock(actionQueue)
            {
                Console.WriteLine("Resumed: "+Utils.GetReadableName(self) +" taskDidPaused = "+taskDidPaused +" actionQueue.Count = "+actionQueue.Count+" isFinished = "+isFinished);
            }
            isPaused=false;
            if(isFinished)
            {
                return;
            }
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
                if(actionQueue.Count>0 && taskDidPaused)
                {
                    Task.Run(actionQueue.Peek());
                }
            }
        }

       
        protected virtual void Start()
        {
            currentEndFlagCount=-1;
            Task.Run(()=>Generate());
        }

        public Task AddInputInformation(Pair<Guid,int> inputInfo)
        {
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

        public async void Generate()
        {
            while(true)
            {
                DateTime start=DateTime.UtcNow;
                List<TexeraTuple> outputList=await GenerateTuples();
                processTime+=DateTime.UtcNow-start;
                start=DateTime.UtcNow;
                MakePayloadMessagesThenSend(outputList);
                sendingTime+=DateTime.UtcNow-start;
                if(isPaused || isFinished)
                {
                    break;
                }
            }
        }

        protected virtual Task<List<TexeraTuple>> GenerateTuples()
        {
            return Task.FromResult(new List<TexeraTuple>());
        }

        // protected void StartGenerate(int retryCount)
        // {
        //     self.Generate().ContinueWith((t)=>
        //     {
        //         if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
        //         {
        //             Console.WriteLine(this.GetType().Name+"("+self+")"+" re-receive message with retry count "+retryCount);
        //             StartGenerate(retryCount+1);
        //         }
        //     });
        // }

        public Task SetSendStrategy(Guid operatorGuid,ISendStrategy sendStrategy)
        {
            Console.WriteLine("Linking: "+Utils.GetReadableName(self)+" "+sendStrategy);
            sendStrategies[operatorGuid]=sendStrategy;
            return Task.CompletedTask;
        }
        public Task ReceiveControlMessage(Immutable<ControlMessage> message)
        {
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            Console.WriteLine(Utils.GetReadableName(self)+" received control message at "+(int)t.TotalSeconds);
            List<Pair<ControlMessage.ControlMessageType,object>> executeSequence = orderingEnforcer.PreProcess(message);
            if(executeSequence!=null)
            {
                orderingEnforcer.CheckStashed(ref executeSequence,message.Value.SenderIdentifer);
                foreach(Pair<ControlMessage.ControlMessageType,object> pair in executeSequence)
                {
                    switch(pair.First)
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
                        case ControlMessage.ControlMessageType.addCallbackWorker:
                            workerToActivate=(IWorkerGrain)pair.Second;
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
            while(!taskDidPaused);
            principalGrain.ReportCurrentValue(self,breakPointCurrent,version);
            return Task.CompletedTask;
        }
#endif
    }
}
