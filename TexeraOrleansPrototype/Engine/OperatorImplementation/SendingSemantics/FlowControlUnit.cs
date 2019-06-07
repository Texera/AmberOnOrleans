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
    ulong lastSentSeqNum = 0;
    ulong lastAckSeqNum = 0;
    ulong windowSize = 20;
    HashSet<ulong> stashedSeqNum=new HashSet<ulong>();
    Queue<Immutable<PayloadMessage>> toBeSentBuffer=new Queue<Immutable<PayloadMessage>>();

    public FlowControlUnit(IWorkerGrain receiver)
    {
        this.receiver=receiver;
    }

    public void Send(Immutable<PayloadMessage> message) 
    {
        //Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver));
        if (message.Value.SequenceNumber - lastAckSeqNum > windowSize) 
        {
            if(message.Value.IsEnd)
            {
                Console.WriteLine(message.Value.SequenceNumber+" "+lastAckSeqNum+" "+toBeSentBuffer.Count);
                Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" END -> "+Utils.GetReadableName(receiver)+" stashed??? current window size = "+windowSize);
            }
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
        if(message.Value.IsEnd)
        {
            Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" END -> "+Utils.GetReadableName(receiver));
        }
        
        if (message.Value.SequenceNumber > lastSentSeqNum) 
        {
             lastSentSeqNum = message.Value.SequenceNumber;
        }

        receiver.ReceivePayloadMessage(message).ContinueWith((t) => 
        {
            if (Utils.IsTaskTimedOutAndStillNeedRetry(t,retryCount))
            {
                //Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" resend "+message.Value.SequenceNumber);
                SendInternal(message,retryCount+1);
            } 
            else
            {
                windowSize = t.Result;
                //Console.WriteLine(Utils.GetReadableName(message.Value.SenderIdentifer)+" -> "+Utils.GetReadableName(receiver)+" window size = "+windowSize);
                // action for successful ack
                if (message.Value.SequenceNumber < lastAckSeqNum) 
                {
                    // ack already received, do nothing
                    Console.WriteLine("ERROR??????: "+message.Value.SequenceNumber+" "+lastAckSeqNum);
                }
                else if (message.Value.SequenceNumber == lastAckSeqNum) 
                {
                    // advance lastAckSeqNum until a gap in the list 
                    lock(stashedSeqNum)
                    {
                        while(true)
                        {
                            if(stashedSeqNum.Contains(lastAckSeqNum+1))
                            {
                                lastAckSeqNum++;
                                stashedSeqNum.Remove(lastAckSeqNum);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    sendMessagesInBuffer();
                } 
                else 
                {
                    lock(stashedSeqNum)
                    {
                        stashedSeqNum.Add(message.Value.SequenceNumber);
                    }
                }
            }
        });
    }


    private void sendMessagesInBuffer() 
    {
        lock(toBeSentBuffer)
        {
            // window size is reduced, don't send out any
            if ((lastSentSeqNum - lastAckSeqNum)>=windowSize) 
            {
                return;
            }
            ulong numMessagesToSend = Math.Min((ulong)toBeSentBuffer.Count,windowSize - (lastSentSeqNum - lastAckSeqNum));
            if(numMessagesToSend>0)Console.WriteLine("send "+numMessagesToSend+" messages from the buffer");
            for (ulong i=0;i<numMessagesToSend;++i) 
            { 
                SendInternal(toBeSentBuffer.Dequeue(),0);
            }
        }
    }
}