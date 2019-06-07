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
    readonly object _object = new object();
    IWorkerGrain receiver;
    ulong lastSentSeqNum = 0;
    ulong lastAckSeqNum = 0;
    ulong windowSize = 20;
    HashSet<ulong> stashedSeqNum=new HashSet<ulong>();
    Queue<Immutable<PayloadMessage>> toBeSentBuffer=new Queue<Immutable<PayloadMessage>>();

    HashSet<Tuple<ulong,ulong,ulong>> ackChecked=new HashSet<Tuple<ulong, ulong,ulong>>();

    public FlowControlUnit(IWorkerGrain receiver)
    {
        this.receiver=receiver;
    }

    public void Send(Immutable<PayloadMessage> message) 
    {
        lock(_object)
        {
            //Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver));
            if (message.Value.SequenceNumber - lastAckSeqNum > windowSize) 
            {
                if(message.Value.IsEnd)
                {
                    Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver));
                    Console.WriteLine(message.Value.SequenceNumber+" "+lastAckSeqNum);
                    string temp="[";
                    foreach(var i in stashedSeqNum)
                    {
                        temp+=i.ToString()+" ";
                    }
                    Console.WriteLine(temp+"]");
                    temp="[";
                    foreach(var i in ackChecked)
                    {
                        temp+="("+i.Item1+","+i.Item2+","+i.Item3+") ";
                    }
                    Console.WriteLine(temp+"]");
                }
                toBeSentBuffer.Enqueue(message);
            }
            else 
            {
                SendInternal(message,0);
            }
        }
    }

    private void SendInternal(Immutable<PayloadMessage> message,int retryCount)
    {
        lock(_object)
        {
            if (message.Value.SequenceNumber > lastSentSeqNum) 
            {
                lastSentSeqNum = message.Value.SequenceNumber;
            }
        }

        receiver.ReceivePayloadMessage(message).ContinueWith((t) => 
        {
            if (Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
            {
                //Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" resend "+message.Value.SequenceNumber+" with retry "+retryCount);
                SendInternal(message,retryCount+1);
            } 
            else
            {
                lock(_object)
                {
                    ackChecked.Add(new Tuple<ulong,ulong,ulong>(message.Value.SequenceNumber,lastAckSeqNum,windowSize));
                    if(!t.IsCompletedSuccessfully)
                    {
                        Console.WriteLine("Exception on seqnum "+message.Value.SequenceNumber+": "+t.Exception);
                    }
                    windowSize = t.Result;
                    //Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" window size = "+windowSize);
                    // action for successful ack
                    if (message.Value.SequenceNumber < lastAckSeqNum) 
                    {
                        // ack already received, do nothing
                        Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" ??? "+message.Value.SequenceNumber);
                    }
                    else if (message.Value.SequenceNumber == lastAckSeqNum) 
                    {
                        //Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" acked "+message.Value.SequenceNumber);
                        // advance lastAckSeqNum until a gap in the list 
                        while(true)
                        {
                            lastAckSeqNum++;
                            if(stashedSeqNum.Contains(lastAckSeqNum))
                            {
                                stashedSeqNum.Remove(lastAckSeqNum);
                            }
                            else
                            {
                                break;
                            }
                        }
                        Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" advanced to "+lastAckSeqNum);
                        sendMessagesInBuffer();
                    } 
                    else 
                    {
                        Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" stashed "+message.Value.SequenceNumber);
                        stashedSeqNum.Add(message.Value.SequenceNumber);
                    }

                }
            }
        });
    }


    private void sendMessagesInBuffer() 
    {
        // window size is reduced, don't send out any
        if ((lastSentSeqNum - lastAckSeqNum)>=windowSize) 
        {
            return;
        }
        ulong numMessagesToSend = Math.Min((ulong)toBeSentBuffer.Count,windowSize - (lastSentSeqNum - lastAckSeqNum));
        //if(numMessagesToSend>0)Console.WriteLine("send "+numMessagesToSend+" messages from the buffer");
        for (ulong i=0;i<numMessagesToSend;++i) 
        { 
            SendInternal(toBeSentBuffer.Dequeue(),0);
        }
    }
}