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
        protected virtual bool WorkAsExternalTask {get{return false;}}
        protected PredicateBase predicate = null;
        protected bool isPaused = false;
        protected List<Immutable<PayloadMessage>> pausedMessages = new List<Immutable<PayloadMessage>>();
        protected IPrincipalGrain principalGrain;
        protected IWorkerGrain self = null;
        private IOrderingEnforcer orderingEnforcer = Utils.GetOrderingEnforcerInstance();
        private Dictionary<Guid,ISendStrategy> sendStrategies = new Dictionary<Guid, ISendStrategy>();
        private int currentEndFlagCount = 0;
        private int targetEndFlagCount = int.MinValue;
        private Queue<Action> actionQueue=new Queue<Action>();
        protected int currentIndex=0;
        protected List<TexeraTuple> outputTuples=new List<TexeraTuple>();
        protected bool isFinished=false;

        public virtual Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            this.self=self;
            this.principalGrain=principalGrain;
            this.predicate=predicate;
            return Task.CompletedTask;
        }
        
        protected void PreProcess(Immutable<PayloadMessage> message, out List<TexeraTuple> batch,out bool isEnd)
        {
            isEnd=message.Value.IsEnd;
            batch=message.Value.Payload;
            orderingEnforcer.CheckStashed(ref batch,ref isEnd, message.Value.SenderIdentifer);  
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
                
                List<TexeraTuple> batch;
                bool isEnd;
                PreProcess(message,out batch,out isEnd);
                if(WorkAsExternalTask)
                {
                    var orleansScheduler=TaskScheduler.Current;
                    Action action=()=>
                    {
                        if(batch!=null)
                        {
                            ProcessBatch(batch);
                        }
                        if(isPaused)
                        {
                            return;
                        }
                        currentIndex=0;
                        Task sendTask=new Task(()=>{MakePayloadMessagesThenSend(isEnd);});
                        sendTask.Start(orleansScheduler);
                        sendTask.Wait();
                        actionQueue.Dequeue();
                        if(!isPaused && actionQueue.Count>0)
                            new Task(actionQueue.Peek()).Start();
                    };
                    actionQueue.Enqueue(action);
                    if(actionQueue.Count==1)
                        new Task(actionQueue.Peek()).Start(TaskScheduler.Default);
                }
                else
                {
                    if(batch!=null)
                    {
                        ProcessBatch(batch);
                    }
                    currentIndex=0;
                    MakePayloadMessagesThenSend(isEnd);
                }
            }
            return Task.CompletedTask;
        }

        private void MakePayloadMessagesThenSend(bool isEnd)
        {
            if(isEnd)
            {
                ++currentEndFlagCount;
            }
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                strategy.Enqueue(outputTuples);
                outputTuples.Clear();
                string identifer=ReturnGrainIndentifierString(self);
                strategy.SendBatchedMessages(identifer);
            }
            if(currentEndFlagCount==targetEndFlagCount)
            {
                isFinished=true;
                MakeLastPayloadMessageThenSend();
            }
        }

        private void MakeLastPayloadMessageThenSend()
        {
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                MakeFinalOutputTuples();
                strategy.Enqueue(outputTuples);
                outputTuples.Clear();
                string identifer=ReturnGrainIndentifierString(self);
                strategy.SendBatchedMessages(identifer);
                strategy.SendEndMessages(identifer);
            }
        }

        protected void ProcessBatch(List<TexeraTuple> batch)
        {
            for(;currentIndex<batch.Count;++currentIndex)
            {
                if(isPaused)
                {
                   return;
                }
                ProcessTuple(batch[currentIndex]);
            }
        }

        protected virtual void ProcessTuple(TexeraTuple tuple)
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
                    SendPayloadMessageToSelf(message, retryCount + 1); 
            });
        }

        protected virtual void MakeFinalOutputTuples()
        {
            
        }


        public string ReturnGrainIndentifierString(IWorkerGrain grain)
        {
            //string a="Engine.OperatorImplementation.Operators.OrleansCodeGen";
            string extension;
            //grain.GetPrimaryKey(out extension);
            return grain.GetPrimaryKey(out extension).ToString()+" "+extension;
        }

        protected virtual void Pause()
        {
            isPaused=true;
        }

        protected virtual void Resume()
        {
            isPaused=false;
            if(isFinished)
            {
                return;
            }
            if(WorkAsExternalTask)
            {
                if(actionQueue.Count>0)
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

        public Task SetTargetEndFlagCount(int target)
        {
            targetEndFlagCount=target;
            return Task.CompletedTask;
        }

        public Task Generate()
        {
            if(!isPaused)
            {
                GenerateTuples();
                if(!isFinished || outputTuples.Count>0)
                {
                    MakePayloadMessagesThenSend(false);
                    StartGenerate(0);
                }
                else
                {
                    foreach(ISendStrategy strategy in sendStrategies.Values)
                    {
                        string identifer=ReturnGrainIndentifierString(self);
                        strategy.SendEndMessages(identifer);
                    }
                }
            }
            return Task.CompletedTask;
        }

        protected virtual void GenerateTuples()
        {
            
        }

        protected void StartGenerate(int retryCount)
        {
            self.Generate().ContinueWith((t)=>
            {
                if(Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
                {
                    StartGenerate(retryCount);
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
