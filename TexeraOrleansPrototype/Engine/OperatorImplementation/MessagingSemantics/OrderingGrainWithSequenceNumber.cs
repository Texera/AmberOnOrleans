using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;


namespace Engine.OperatorImplementation.MessagingSemantics
{
    public class OrderingGrainWithSequenceNumber : IOrderingEnforcer
    {
        private Dictionary<string,Dictionary<ulong, Pair<bool,List<TexeraTuple>>>> stashedPayloadMessages = new Dictionary<string, Dictionary<ulong, Pair<bool, List<TexeraTuple>>>>();
        private Dictionary<string,Dictionary<ulong, ControlMessage.ControlMessageType>> stashedControlMessages = new Dictionary<string, Dictionary<ulong, ControlMessage.ControlMessageType>>();
        private Dictionary<string,ulong> inSequenceNumberMap=new Dictionary<string, ulong>();
        private enum MessageStatus
        {
            Vaild,
            Duplicated,
            Ahead,
        }

        private MessageStatus CheckMessage(string sender, ulong sequenceNum)
        {
            if(!inSequenceNumberMap.ContainsKey(sender))
            {
                inSequenceNumberMap[sender]=0;
            }  
            ulong currentSequenceNumber=inSequenceNumberMap[sender];
            if(sequenceNum < currentSequenceNumber)
            {
                // de-dup messages
                return MessageStatus.Duplicated;
            }
            if (sequenceNum != currentSequenceNumber)
            {
                return MessageStatus.Ahead;           
            }
            return MessageStatus.Vaild;
        }

        public bool PreProcess(Immutable<PayloadMessage> message)
        {
            string sender=message.Value.SenderIdentifer;
            ulong sequenceNum=message.Value.SequenceNumber;
            switch(CheckMessage(sender,sequenceNum))
            {
                case MessageStatus.Vaild:
                    inSequenceNumberMap[sender]++;
                    return true;
                case MessageStatus.Ahead:
                    //Console.WriteLine("expected "+inSequenceNumberMap[sender]+" but get "+sequenceNum);
                    if(!stashedPayloadMessages.ContainsKey(sender))
                    {
                        stashedPayloadMessages[sender]=new Dictionary<ulong, Pair<bool, List<TexeraTuple>>>();
                    }
                    stashedPayloadMessages[sender].Add(sequenceNum, new Pair<bool, List<TexeraTuple>>(message.Value.IsEnd,message.Value.Payload));
                    break;
                case MessageStatus.Duplicated:
                    //Console.WriteLine("expected "+inSequenceNumberMap[sender]+" but get "+sequenceNum+" duplicated");
                    break;
            }
            return false;
        }

        public void CheckStashed(ref List<TexeraTuple> batchList, ref bool isEnd, string sender)
        {
            if(stashedPayloadMessages.ContainsKey(sender))
            {
                if(!inSequenceNumberMap.ContainsKey(sender))
                {
                    inSequenceNumberMap[sender]=0;
                }
                ulong currentSequenceNumber=inSequenceNumberMap[sender];
                //Console.WriteLine("check seqnum "+currentSequenceNumber+" from stashed");
                Dictionary<ulong, Pair<bool,List<TexeraTuple>>> currentMap=stashedPayloadMessages[sender];
                while(currentMap.ContainsKey(currentSequenceNumber))
                {
                    if(batchList==null)
                    {
                        batchList=new List<TexeraTuple>();
                    }
                    Pair<bool, List<TexeraTuple>> pair = currentMap[currentSequenceNumber];
                    isEnd |= pair.First;
                    if(pair.Second!=null)
                    {
                        batchList.AddRange(pair.Second);
                    }
                    currentMap.Remove(currentSequenceNumber);
                    //Console.WriteLine("extract seqnum "+currentSequenceNumber+" from stashed");
                    currentSequenceNumber++;
                    inSequenceNumberMap[sender]++;
                }
            }
        }

        public List<ControlMessage.ControlMessageType> PreProcess(Immutable<ControlMessage> message)
        {
            string sender=message.Value.SenderIdentifer;
            ulong sequenceNum=message.Value.SequenceNumber;
            switch(CheckMessage(sender,sequenceNum))
            {
                case MessageStatus.Vaild:
                    inSequenceNumberMap[sender]++;
                    return new List<ControlMessage.ControlMessageType>{message.Value.Type};
                case MessageStatus.Ahead:
                    if(!stashedControlMessages.ContainsKey(sender))
                    {
                        stashedControlMessages[sender]=new Dictionary<ulong, ControlMessage.ControlMessageType>();
                    }
                    stashedControlMessages[sender].Add(sequenceNum, message.Value.Type);
                    break;
                case MessageStatus.Duplicated:
                    break;
            }
            return null;
        }

        public void CheckStashed(ref List<ControlMessage.ControlMessageType> controlMessages, string sender)
        {
            if(stashedControlMessages.ContainsKey(sender))
            {
                if(!inSequenceNumberMap.ContainsKey(sender))
                {
                    inSequenceNumberMap[sender]=0;
                }
                ulong currentSequenceNumber=inSequenceNumberMap[sender];
                Dictionary<ulong, ControlMessage.ControlMessageType> currentMap=stashedControlMessages[sender];
                while(currentMap.ContainsKey(currentSequenceNumber))
                {
                    controlMessages.Add(currentMap[currentSequenceNumber]);
                    currentMap.Remove(currentSequenceNumber);
                    inSequenceNumberMap[sender]++;
                }
            }
        }
    }
}
