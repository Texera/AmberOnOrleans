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
            toBeSentBuffer.Enqueue(message);
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
                if (message.Value.SequenceNumber <= lastAckSeqNum) 
                {
                    // ack already received, do nothing
                }
                else if (message.Value.SequenceNumber == lastAckSeqNum + 1) 
                {
                    lastAckSeqNum++;
                    // advance lastAckSeqNum until a gap in the list 
                    while(true)
                    {
                        if(stashedSeqNum.Contains(lastAckSeqNum))
                        {
                            stashedSeqNum.Remove(lastAckSeqNum);
                            lastAckSeqNum++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    sendMessagesInBuffer();
                } 
                else 
                {
                    stashedSeqNum.Add(message.Value.SequenceNumber);
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
        lock(toBeSentBuffer)
        {
            ulong numMessagesToSend = Math.Min((ulong)toBeSentBuffer.Count,windowSize - (lastSentSeqNum - lastAckSeqNum));
            //Console.WriteLine("send "+numMessagesToSend+" messages from the buffer");
            for (ulong i=0;i<numMessagesToSend;++i) 
            { 
                SendInternal(toBeSentBuffer.Dequeue(),0);
            }
        }
    }
}