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
        private Dictionary<INormalGrain,Dictionary<ulong, List<TexeraTuple>>> stashed = new Dictionary<INormalGrain, Dictionary<ulong, List<TexeraTuple>>>();
        private Dictionary<INormalGrain,ulong> in_map=new Dictionary<INormalGrain, ulong>();
        private Dictionary<INormalGrain,ulong> out_map=new Dictionary<INormalGrain, ulong>();
        public List<TexeraTuple> PreProcess(Immutable<TexeraMessage> message)
        {
            INormalGrain sender=message.Value.sender;
            ulong sequenceNum=message.Value.sequenceNumber;
            if(!in_map.ContainsKey(sender))
            {
                in_map[sender]=0;
            }
            //string extensionKey = "";      
            if(sequenceNum < in_map[sender])
            {
                // de-dup messages
                //Console.WriteLine($"Grain {currentOperator.GetPrimaryKey(out extensionKey)} {extensionKey} received duplicate message with sequence number {seq_token}: expected sequence number {in_map[sender]}");
                return null;
            }
            if (sequenceNum != in_map[sender])
            {
                //Console.WriteLine($"Grain {currentOperator.GetPrimaryKey(out extensionKey)} {extensionKey} received message ahead in sequence, being put in stash: sequence number {seq_token}, expected sequence number {in_map[sender]}");                              
                if(!stashed.ContainsKey(sender))
                {
                    stashed[sender]=new Dictionary<ulong, List<TexeraTuple>>();
                }
                stashed[sender].Add(sequenceNum, message.Value.tuples);
                return null;           
            }
            else
            {
                in_map[sender]++;
                return message.Value.tuples;
            }

        }

        public ulong GetOutMessageSequenceNumber(INormalGrain nextOperator)
        {
            if(out_map.ContainsKey(nextOperator))
            {
                out_map.Add(nextOperator,0);
                return 0;
            }
            else
            {
                ulong res=out_map[nextOperator]++;
                return res;
            }
        }       

        public void CheckStashed(ref List<TexeraTuple> batchList,INormalGrain sender)
        {
            if(stashed.ContainsKey(sender))
            {
                if(!in_map.ContainsKey(sender))
                {
                    in_map[sender]=0;
                }
                while(stashed[sender].ContainsKey(in_map[sender]))
                {
                    if(batchList==null)
                    {
                        batchList=new List<TexeraTuple>();
                    }
                    List<TexeraTuple> batch = stashed[sender][in_map[sender]];
                    batchList.AddRange(batch);
                    stashed[sender].Remove(in_map[sender]);
                    in_map[sender]++;
                }
            }
        }

    }
}
