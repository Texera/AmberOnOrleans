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
        private Queue<Task> taskQueue=new Queue<Task>();

        public virtual Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            this.self=self;
            this.principalGrain=principalGrain;
            this.predicate=predicate;
            return Task.CompletedTask;
        }

        public Task AddNextGrain(Guid nextOperatorGuid,IWorkerGrain grain)
        {
            if(sendStrategies.ContainsKey(nextOperatorGuid))
            {
                sendStrategies[nextOperatorGuid].AddReceiver(grain);
            }
            else
            {
                throw new Exception("unknown next operator guid");
            }
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
                List<TexeraTuple> output=new List<TexeraTuple>();
                if(WorkAsExternalTask)
                {
                    var orleansScheduler=TaskScheduler.Current;
                    Task externalTask=new Task(()=>
                    {
                        if(batch!=null)
                        {
                            ProcessBatch(batch,ref output);
                        }
                        Task sendTask=new Task(()=>{MakePayloadMessagesThenSend(output,isEnd);});
                        sendTask.Start(orleansScheduler);
                        sendTask.Wait();
                        taskQueue.Dequeue();
                        if(!isPaused && taskQueue.Count>0 && taskQueue.Peek().Status==TaskStatus.Created)
                            taskQueue.Peek().Start();
                    });
                    taskQueue.Enqueue(externalTask);
                    if(taskQueue.Peek().Status==TaskStatus.Created)
                        taskQueue.Peek().Start(TaskScheduler.Default);
                }
                else
                {
                    if(batch!=null)
                    {
                        ProcessBatch(batch,ref output);
                    }
                    MakePayloadMessagesThenSend(output,isEnd);
                }
            }
            return Task.CompletedTask;
        }

        private void MakePayloadMessagesThenSend(List<TexeraTuple> output, bool isEnd)
        {
            if(isEnd)
            {
                ++currentEndFlagCount;
            }
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                strategy.Enqueue(output);
                string identifer=MakeIdentifier(self);
                strategy.SendBatchedMessages(identifer);
            }
            if(currentEndFlagCount==targetEndFlagCount)
            {
                MakeLastPayloadMessageThenSend();
            }
        }

        private void MakeLastPayloadMessageThenSend()
        {
            foreach(ISendStrategy strategy in sendStrategies.Values)
            {
                List<TexeraTuple> result=MakeFinalOutputTuples();
                if(result!=null)
                {
                    strategy.Enqueue(result);
                }
                string identifer=MakeIdentifier(self);
                strategy.SendBatchedMessages(identifer);
                strategy.SendEndMessages(identifer);
            }
        }

        protected virtual void ProcessBatch(List<TexeraTuple> batch, ref List<TexeraTuple> output)
        {
            foreach(TexeraTuple tuple in batch)
            {
                List<TexeraTuple> results=ProcessTuple(tuple);
                if(results!=null)
                    output.AddRange(results);
            }
        }

        protected virtual List<TexeraTuple> ProcessTuple(TexeraTuple tuple)
        {
            return null;
        }

        public Task AddNextGrainList(Guid nextOperatorGuid,List<IWorkerGrain> grains)
        {
            if(sendStrategies.ContainsKey(nextOperatorGuid))
            {
                sendStrategies[nextOperatorGuid].AddReceivers(grains);
            }
            else
            {
                throw new Exception("unknown next operator guid");
            }
            return Task.CompletedTask;
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

        protected virtual List<TexeraTuple> MakeFinalOutputTuples()
        {
            return null;
        }


        public string MakeIdentifier(IWorkerGrain grain)
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
            /*
            if(WorkAsExternalTask)
            {
                if(taskQueue.Count>0 && taskQueue.Peek().Status!=TaskStatus.Running)
                    taskQueue.Peek().Start(TaskScheduler.Default);
            }
            */
            foreach(Immutable<PayloadMessage> message in pausedMessages)
            {
                SendPayloadMessageToSelf(message,0);
            }
            pausedMessages.Clear();
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
                List<TexeraTuple> output=GenerateTuples();
                if(output!=null)
                {
                    MakePayloadMessagesThenSend(output,false);
                    StartGenerate(0);
                }
                else
                {
                    foreach(ISendStrategy strategy in sendStrategies.Values)
                    {
                        string identifer=MakeIdentifier(self);
                        strategy.SendEndMessages(identifer);
                    }
                }
            }
            return Task.CompletedTask;
        }

        protected virtual List<TexeraTuple> GenerateTuples()
        {
            return null;
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
