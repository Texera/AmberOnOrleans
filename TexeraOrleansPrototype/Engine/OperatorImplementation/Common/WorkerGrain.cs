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


    public class WorkerGrain : Grain, IWorkerGrain
    {
        protected PredicateBase predicate = null;
        protected bool isPaused = false;
        protected List<Immutable<PayloadMessage>> pausedMessages = new List<Immutable<PayloadMessage>>();
        protected IPrincipalGrain principalGrain;
        protected IWorkerGrain self = null;
        private IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();
        private Dictionary<Guid,ISendStrategy> sendStrategies = new Dictionary<Guid, ISendStrategy>();
        protected Dictionary<Guid,int> inputInfo;
        protected Queue<Action> actionQueue=new Queue<Action>();
        protected int currentIndex=0;
        protected int currentEndFlagCount=int.MaxValue;
        protected List<TexeraTuple> outputTuples=new List<TexeraTuple>();
        protected bool isFinished=false;

        public virtual Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            this.self=self;
            this.principalGrain=principalGrain;
            this.predicate=predicate;
            string ext1,opType1;
            self.GetPrimaryKey(out ext1);
            opType1=Utils.GetOperatorTypeFromGrainClass(this.GetType().Name);
            Console.WriteLine("Init: "+opType1+" "+ext1);
            return Task.CompletedTask;
        }
    

        public override Task OnDeactivateAsync()
        {
            pausedMessages=null;
            orderingEnforcer=null;
            sendStrategies=null;
            actionQueue=null;
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
                    BeforeProcessBatch(message,orleansScheduler);
                    if(batch!=null)
                    {
                        ProcessBatch(batch);
                    }
                    batch=null;
                    //GC.Collect();
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
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                strategy.Enqueue(outputTuples);
                strategy.SendBatchedMessages(self);
            }
            outputTuples=new List<TexeraTuple>();
            if(currentEndFlagCount==0 && actionQueue.Count==1)
            {
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

        public Task ProcessControlMessage(Immutable<ControlMessage> message)
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
            string ext1,opType1;
            self.GetPrimaryKey(out ext1);
            opType1=Utils.GetOperatorTypeFromGrainClass(this.GetType().Name);
            Console.WriteLine("Pause: "+opType1+" "+ext1);
            isPaused=true;
        }

        protected virtual void Resume()
        {
            string ext1,opType1;
            self.GetPrimaryKey(out ext1);
            opType1=Utils.GetOperatorTypeFromGrainClass(this.GetType().Name);
            Console.WriteLine("Resume: "+opType1+" "+ext1);
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
            throw new NotImplementedException();
        }

        public Task SetInputInformation(Dictionary<Guid,int> inputInfo)
        {
            currentEndFlagCount=inputInfo.Values.Sum();
            this.inputInfo=inputInfo;
            return Task.CompletedTask;
        }

        public async Task Generate()
        {
            if(!isPaused)
            {
                await GenerateTuples();
                if(!isFinished || outputTuples.Count>0)
                {
                    MakePayloadMessagesThenSend();
                    StartGenerate(0);
                }
                else
                {
                    foreach(ISendStrategy strategy in sendStrategies.Values)
                    {
                        strategy.SendEndMessages(self);
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
    }
}
