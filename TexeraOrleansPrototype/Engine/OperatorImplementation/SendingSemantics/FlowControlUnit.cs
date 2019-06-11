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
        int windowSize = 2;
        bool isPaused=false;
        HashSet<ulong> messagesOnTheWay=new HashSet<ulong>();
        Queue<Immutable<PayloadMessage>> toBeSentBuffer=new Queue<Immutable<PayloadMessage>>();

        public FlowControlUnit(IWorkerGrain receiver):base(receiver)
        {
        }

        public override void Send(Immutable<PayloadMessage> message) 
        {
            //Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver));
            if (isPaused || messagesOnTheWay.Count > windowSize) 
            {
                // if(message.Value.IsEnd)
                // {
                //     Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver));
                //     Console.WriteLine(message.Value.SequenceNumber+" "+lastAckSeqNum+" "+lastSentSeqNum);
                //     string temp="[";
                //     foreach(var i in stashedSeqNum)
                //     {
                //         temp+=i.ToString()+" ";
                //     }
                //     Console.WriteLine(temp+"]");
                //     temp="[";
                //     foreach(var i in ackChecked)
                //     {
                //         temp+="("+i.Item1+","+i.Item2+","+i.Item3+") ";
                //     }
                //     Console.WriteLine(temp+"]");
                // }
                lock(toBeSentBuffer)
                {
                    toBeSentBuffer.Enqueue(message);
                }
            }
            else 
            {
                SendInternal(message,0);
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
                messagesOnTheWay.Add(message.Value.SequenceNumber);
            }
            receiver.ReceivePayloadMessage(message).ContinueWith((t) => 
            {
                if (Utils.IsTaskFaultedAndStillNeedRetry(t,retryCount))
                {
                    Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" resend "+message.Value.SequenceNumber+" with retry "+retryCount);
                    SendInternal(message,retryCount+1);
                } 
                else if(t.IsCompletedSuccessfully)
                {
                    lock(messagesOnTheWay)
                    {
                        messagesOnTheWay.Remove(message.Value.SequenceNumber);
                    }
                    lock(toBeSentBuffer)
                    {
                        if(!isPaused)
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