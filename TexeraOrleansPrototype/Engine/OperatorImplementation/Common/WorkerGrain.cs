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

        #if (PROFILING_ENABLED)
        protected TimeSpan processTime=new TimeSpan(0,0,0);
        protected TimeSpan sendingTime=new TimeSpan(0,0,0);
        protected TimeSpan preprocessTime=new TimeSpan(0,0,0);
        #endif
        protected bool pauseBySelf=false;
        protected IWorkerGrain workerToActivate=null;
        private ILocalSiloDetails localSiloDetails => this.ServiceProvider.GetRequiredService<ILocalSiloDetails>();

#if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
        private int breakPointTarget;
        private int breakPointCurrent=0;
        private int version=-1;
        private bool breakPointEnabled=false;
        private object counterlock=new object();
#endif
        public virtual Task<SiloAddress> Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            this.self=self;
            this.principalGrain=principalGrain;
            this.predicate=predicate;
            Console.WriteLine("Init: "+Utils.GetReadableName(self));
            return Task.FromResult(localSiloDetails.SiloAddress);
        }
    

        public override Task OnDeactivateAsync()
        {
            Console.WriteLine("Deactivate: "+Utils.GetReadableName(self));
            orderingEnforcer=null;
            sendStrategies=null;
            actionQueue=null;
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
                //Console.WriteLine(Utils.GetReadableName(self)+" END!");
                #if (PROFILING_ENABLED)
                Console.WriteLine(Utils.GetReadableName(self)+" Preprocess Time: "+preprocessTime+" Process Time: "+processTime+" Sending Time: "+sendingTime);
                #endif
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

        protected virtual void ProcessTuple(TexeraTuple tuple, List<TexeraTuple> output)
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
                    if(!pauseBySelf)
                        principalGrain.OnTaskDidPaused();
                    else
                    {
                        #if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
                        int temp;
                        if(breakPointEnabled)
                        {
                            lock(counterlock)
                            {
                                temp=breakPointCurrent;
                                breakPointCurrent=0;
                            }
                            principalGrain.ReportCurrentValue(self,temp,version);
                            breakPointEnabled=false;
                        }
                        #endif
                    }
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
                        orderingEnforcer.CheckStashed(ref batch,ref isEnd, message.SenderIdentifer);
                        savedBatch=batch;
                        messageChecked=true;
                    }
                    #if (PROFILING_ENABLED)  
                    preprocessTime+=DateTime.UtcNow-start;
                    start=DateTime.UtcNow;
                    #endif
                    BeforeProcessBatch(message);
                    List<TexeraTuple> outputList=new List<TexeraTuple>();
                    if(batch!=null)
                    {
                        ProcessBatch(batch,outputList);
                        #if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
                        lock(counterlock)
                        {
                            breakPointCurrent+=outputList.Count;
                        }
                        #endif
                    }
                    if(isPaused)
                    {
                        #if (GLOBAL_CONDITIONAL_BREAKPOINTS_ENABLED)
                        if(pauseBySelf)
                        {
                            int temp;
                            if(breakPointEnabled)
                            {
                                lock(counterlock)
                                {
                                    temp=breakPointCurrent;
                                    breakPointCurrent=0;
                                }
                                principalGrain.ReportCurrentValue(self,temp,version);
                                breakPointEnabled=false;
                            }
                        }
                        #endif
                        //if we not do so, the outputlist will be lost.
                        MakePayloadMessagesThenSend(outputList);
                        if(!pauseBySelf)
                        {
                            principalGrain.OnTaskDidPaused();
                        }
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
                    }
                    AfterProcessBatch(message);
                    #if (PROFILING_ENABLED)
                    processTime+=DateTime.UtcNow-start;
                    start=DateTime.UtcNow;
                    #endif
                    MakePayloadMessagesThenSend(outputList);
                    #if (PROFILING_ENABLED)
                    sendingTime+=DateTime.UtcNow-start;
                    #endif
                }
                lock(actionQueue)
                {
                    actionQueue.Dequeue();
                    if(actionQueue.Count>0)
                    {
                        Task.Run(actionQueue.Peek());
                    }
                    else if(isPaused && !pauseBySelf)
                    {
                        principalGrain.OnTaskDidPaused();
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

        protected virtual List<TexeraTuple> MakeFinalOutputTuples()
        {
            return null;
        }

        protected virtual void Pause(bool bySelf=true)
        {
            pauseBySelf=bySelf;
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
            isPaused=true;
            lock(actionQueue)
            {
                if(actionQueue.Count==0 && !bySelf)
                    principalGrain.OnTaskDidPaused();
            }
        }

        protected virtual void Resume()
        {
            lock(actionQueue)
            {
                Console.WriteLine("Resumed: "+Utils.GetReadableName(self) +" actionQueue.Count = "+actionQueue.Count+" isFinished = "+isFinished);
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
                if(actionQueue.Count>0)
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
                #if (PROFILING_ENABLED)
                DateTime start=DateTime.UtcNow;
                #endif
                List<TexeraTuple> outputList=await GenerateTuples();
                #if (PROFILING_ENABLED)
                processTime+=DateTime.UtcNow-start;
                start=DateTime.UtcNow;
                #endif
                MakePayloadMessagesThenSend(outputList);
                #if (PROFILING_ENABLED)
                sendingTime+=DateTime.UtcNow-start;
                #endif
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

        public Task OnTaskDidPaused()
        {
            principalGrain.OnTaskDidPaused();
            return Task.CompletedTask;
        }


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
                            Pause(false);
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
            breakPointTarget=targetValue;
            Console.WriteLine(Utils.GetReadableName(self)+" set breakpoint! target value = "+targetValue+" version = "+version);
            return Task.CompletedTask;
        }

        public Task AskToReportCurrentValue()
        {
            if(breakPointEnabled==false)return Task.CompletedTask;
            Console.WriteLine(Utils.GetReadableName(self)+" received AskToReport");
            if(!isPaused)
            {
                Pause();
            }
        }
#endif
    }
}
