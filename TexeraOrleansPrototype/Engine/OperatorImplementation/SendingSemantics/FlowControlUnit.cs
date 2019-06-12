using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Engine;
using Engine.OperatorImplementation.Common;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using TexeraUtilities;

namespace Engine.OperatorImplementation.SendingSemantics
{
    public class FlowControlUnit: SendingUnit
    {
        static readonly TimeSpan okTime=new TimeSpan(0,0,0,10); 
        static readonly int WindowSizeLimit=64;
        int ssthreshold = 8;
        int windowSize = 2;
        bool isPaused=false;
        Dictionary<ulong,DateTime> messagesOnTheWay=new Dictionary<ulong, DateTime>();
        Queue<Immutable<PayloadMessage>> toBeSentBuffer=new Queue<Immutable<PayloadMessage>>();

        public FlowControlUnit(IWorkerGrain receiver):base(receiver)
        {
        }

        public override void Send(Immutable<PayloadMessage> message) 
        {
            PayloadMessage dequeuedMessage=null;
            lock(toBeSentBuffer) lock(messagesOnTheWay)
            {
                toBeSentBuffer.Enqueue(message);
                if (!isPaused && messagesOnTheWay.Count < windowSize) 
                {
                    dequeuedMessage=toBeSentBuffer.Dequeue().Value;
                }
            }
            if(dequeuedMessage!=null)
            {
                SendInternal(dequeuedMessage.AsImmutable(),0);
            }
        }

        public override void SetPauseFlag(bool flag)
        {
            isPaused=flag;
        }

        public override void ResumeSending()
        {
            List<PayloadMessage> messagesToSend=new List<PayloadMessage>();
            lock(toBeSentBuffer) lock(messagesOnTheWay)
            {
                int numToBeSent=windowSize-messagesOnTheWay.Count;
                for(int i=0;i<numToBeSent;++i)
                {
                    if(toBeSentBuffer.Count>0)
                        messagesToSend.Add(toBeSentBuffer.Dequeue().Value);
                    else
                        break;
                }
            }
            foreach(PayloadMessage message in messagesToSend)
            {
                SendInternal(message.AsImmutable(),0);
            }
        }


        

        private void SendInternal(Immutable<PayloadMessage> message,int retryCount)
        {
            lock(messagesOnTheWay)
            {
                messagesOnTheWay[message.Value.SequenceNumber]=DateTime.UtcNow;
            }
            receiver.ReceivePayloadMessage(message).ContinueWith((t) => 
            {
                if (Utils.IsTaskFaultedAndStillNeedRetry(t,retryCount))
                {
                    //critical:
                    Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" resend "+message.Value.SequenceNumber+" with retry "+retryCount+" windowsize = "+windowSize);
                    windowSize=1;
                    SendInternal(message,retryCount+1);
                } 
                else
                {
                    DateTime sentTime;
                    lock(messagesOnTheWay)
                    {
                        sentTime=messagesOnTheWay[message.Value.SequenceNumber];
                        messagesOnTheWay.Remove(message.Value.SequenceNumber);
                    }
                    if(DateTime.UtcNow.Subtract(sentTime)<okTime)
                    {
                        //ack time is good
                        if (windowSize < ssthreshold) 
                        {
                            windowSize = windowSize * 2;
                            if (windowSize > ssthreshold) 
                            {
                                windowSize = ssthreshold;
                            }
                        }
                        else 
                        {
                            windowSize = windowSize + 1;
                        }
                        if(windowSize>WindowSizeLimit)
                        {
                            windowSize=WindowSizeLimit;
                        }
                        Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" windowSize = "+windowSize);
                    }
                    else
                    {
                        //ack time is too long
                        ssthreshold/=2;
                        windowSize=ssthreshold;
                        if(windowSize<2)
                        {
                            windowSize=2;
                        }
                    }
                    PayloadMessage dequeuedMessage=null;
                    lock(toBeSentBuffer) lock(messagesOnTheWay)
                    {
                        if(!isPaused && messagesOnTheWay.Count<windowSize && toBeSentBuffer.Count>0)
                        {
                            dequeuedMessage=toBeSentBuffer.Dequeue().Value;
                        }
                    }
                    if(dequeuedMessage!=null)
                    {
                        SendInternal(dequeuedMessage.AsImmutable(),0);
                    }
                }
            });
        }


        private void sendMessagesInBuffer() 
        {
            
        }
    }
}