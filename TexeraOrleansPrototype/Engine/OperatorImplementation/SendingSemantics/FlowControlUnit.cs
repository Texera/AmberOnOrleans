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
        static readonly TimeSpan okTime=new TimeSpan(0,0,0,5); 
        //static readonly int WindowSizeLimit=4;
        int ssthreshold = 4;
        int windowSize = 2;
        bool isPaused=false;
        Dictionary<ulong,DateTime> messagesOnTheWay=new Dictionary<ulong, DateTime>();
        Queue<PayloadMessage> toBeSentBuffer=new Queue<PayloadMessage>();

        public FlowControlUnit(IWorkerGrain receiver):base(receiver)
        {
        }

        public override void Send(PayloadMessage message) 
        {
            PayloadMessage dequeuedMessage=null;
            lock(toBeSentBuffer) lock(messagesOnTheWay)
            {
                toBeSentBuffer.Enqueue(message);
                if (!isPaused && messagesOnTheWay.Count < windowSize) 
                {
                    dequeuedMessage=toBeSentBuffer.Dequeue();
                }
            }
            if(dequeuedMessage!=null)
            {
                SendInternal(dequeuedMessage,0);
            }
        }

        public override void SetPauseFlag(bool flag)
        {
            //Console.WriteLine("flowControlUnit's pause flag = "+flag.ToString());
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
                        messagesToSend.Add(toBeSentBuffer.Dequeue());
                    else
                        break;
                }
            }
            foreach(PayloadMessage message in messagesToSend)
            {
                SendInternal(message,0);
            }
        }


        

        private void SendInternal(PayloadMessage message,int retryCount)
        {
            lock(messagesOnTheWay)
            {
                messagesOnTheWay[message.SequenceNumber]=DateTime.UtcNow;
            }
            //Console.WriteLine(Utils.GetReadableName(message.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" windowSize = "+windowSize);
            receiver.ReceivePayloadMessage(message).ContinueWith((t) => 
            {
                if (Utils.IsTaskFaultedAndStillNeedRetry(t,retryCount))
                {
                    //critical:
                    Console.WriteLine(Utils.GetReadableName(message.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+"with seqnum = "+message.SequenceNumber+" failed! \n ERROR: "+t.Exception.Message);
                    windowSize=1;
                    SendInternal(message,retryCount+1);
                } 
                else
                {
                    DateTime sentTime;
                    lock(messagesOnTheWay)
                    {
                        sentTime=messagesOnTheWay[message.SequenceNumber];
                        messagesOnTheWay.Remove(message.SequenceNumber);
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
                        // if(windowSize>WindowSizeLimit)
                        // {
                        //     windowSize=WindowSizeLimit;
                        // }
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
                            dequeuedMessage=toBeSentBuffer.Dequeue();
                        }
                    }
                    if(dequeuedMessage!=null)
                    {
                        SendInternal(dequeuedMessage,0);
                    }
                }
            });
        }


        private void sendMessagesInBuffer() 
        {
            
        }
    }
}