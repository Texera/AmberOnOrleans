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
        static readonly TimeSpan okTime=new TimeSpan(0,0,0,800); 
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
            lock(toBeSentBuffer)
            {
                toBeSentBuffer.Enqueue(message);
                if (!isPaused && messagesOnTheWay.Count < windowSize) 
                {
                    SendInternal(toBeSentBuffer.Dequeue(),0);
                }
            }
        }

        public override void Pause()
        {
            isPaused=true;
        }

        public override void Resume()
        {
            isPaused=false;
            lock(toBeSentBuffer)
            {
                int numToBeSent=windowSize-toBeSentBuffer.Count;
                for(int i=0;i<numToBeSent;++i)
                {
                    if(toBeSentBuffer.Count>0)
                        SendInternal(toBeSentBuffer.Dequeue(),0);
                    else
                        break;
                }
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
                else if(t.IsCompletedSuccessfully)
                {
                    lock(messagesOnTheWay)
                    {
                        if(DateTime.UtcNow.Subtract(messagesOnTheWay[message.Value.SequenceNumber])<okTime)
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
                        }
                        else
                        {
                            //ack time is too long
                            ssthreshold/=2;
                            windowSize=ssthreshold;
                            if(windowSize<2)
                            {
                                windowSize=1;
                            }
                        }
                        messagesOnTheWay.Remove(message.Value.SequenceNumber);
                    }
                    lock(toBeSentBuffer)
                    {
                        if(!isPaused && toBeSentBuffer.Count<windowSize)
                        {
                            SendInternal(toBeSentBuffer.Dequeue(),0);
                        }
                    }
                }
                else
                {
                    //Console.WriteLine("??????????????????????????????????");
                }
            });
        }


        private void sendMessagesInBuffer() 
        {
            
        }
    }
}