using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;

namespace TexeraOrleansPrototype.OperatorImplementation.MessagingSemantics
{
    public class OrderingGrainWithSequenceNumber : IOrderingEnforcer
    {
        private Dictionary<ulong, List<Tuple>> stashed = new Dictionary<ulong, List<Tuple>>();
        private ulong current_idx = 0;
        private ulong current_seq_num = 0;


        public ulong GetOutgoingSequenceNumber()
        {
            return current_seq_num;
        }

        public ulong GetExpectedSequenceNumber()
        {
            return current_idx;
        }

        public void IncrementOutgoingSequenceNumber()
        {
            current_seq_num++;
        }

        public void IncrementExpectedSequenceNumber()
        {
            current_idx++;
        }
        
        public List<Tuple> PreProcess(List<Tuple> batch, INormalGrain operatorGrain)
        {
            var seq_token = batch[0].seq_token;           

            if(seq_token < current_idx)
            {
                // de-dup messages
                Console.WriteLine($"Grain {operatorGrain.GetPrimaryKeyLong()} received duplicate message with sequence number {seq_token}: expected sequence number {current_idx}");
                return null;
            }
            if (seq_token != current_idx)
            {
                Console.WriteLine($"Grain {operatorGrain.GetPrimaryKeyLong()} received message ahead in sequence, being put in stash: sequence number {seq_token}, expected sequence number {current_idx}");                              
                stashed.Add(seq_token, batch);
                return null;           
            }
            else
            {
                current_idx++;
                return batch;
            }
        }

        public async Task PostProcess(INormalGrain operatorGrain)
        {
            await ProcessStashed(operatorGrain);
        }       

        private async Task ProcessStashed(INormalGrain operatorGrain)
        {
            while(stashed.ContainsKey(current_idx))
            {
                List<Tuple> batch = stashed[current_idx];
                List<Tuple> batchToForward = new List<Tuple>();
                foreach(Tuple tuple in batch)
                {
                    Tuple ret = await operatorGrain.Process_impl(tuple);
                    if(ret != null)
                    {
                        batchToForward.Add(ret);
                    }                
                }
                if (batchToForward.Count > 0)
                {
                    INormalGrain nextOperator = await operatorGrain.GetNextoperator();
                    if(nextOperator != null)
                    {
                        batchToForward[0].seq_token = current_seq_num++;
                        nextOperator.Process(batchToForward.AsImmutable());
                    }
                }
                stashed.Remove(current_idx);
                current_idx++;
            }

        }

    }
}
