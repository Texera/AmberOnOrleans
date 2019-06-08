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

public class FlowControlUnit
{
    IWorkerGrain receiver;
    int windowSize = 50;
    HashSet<ulong> messagesOnTheWay=new HashSet<ulong>();
    Queue<Immutable<PayloadMessage>> toBeSentBuffer=new Queue<Immutable<PayloadMessage>>();

    public FlowControlUnit(IWorkerGrain receiver)
    {
        this.receiver=receiver;
    }

    public void Send(Immutable<PayloadMessage> message) 
    {
        //Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver));
        if (messagesOnTheWay.Count > windowSize) 
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
                    SendInternal(toBeSentBuffer.Dequeue(),0);
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