using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Orleans.Concurrency;

namespace TexeraOrleansPrototype.OperatorImplementation.MessagingSemantics
{
    public class OrderingGrainWithContinuousSending : IOrderingEnforcer
    {
        private Dictionary<ulong, List<Tuple>> stashed = new Dictionary<ulong, List<Tuple>>();
        private ulong current_idx = 0;
        private ulong current_seq_num = 0;

        public List<Tuple> tuplesToSendAhead = new List<Tuple>();
        private Task sendingNextTask = Task.CompletedTask;

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
        
        public List<Tuple> PreProcess(List<Tuple> batch, INormalGrain currentOperator)
        {
            var seq_token = batch[0].seq_token;           

            if(seq_token < current_idx)
            {
                // de-dup messages
                Console.WriteLine($"Grain {currentOperator.GetPrimaryKeyLong()} received duplicate message with sequence number {seq_token}: expected sequence number {current_idx}");
                return null;
            }
            if (seq_token != current_idx)
            {
                Console.WriteLine($"Grain {currentOperator.GetPrimaryKeyLong()} received message ahead in sequence, being put in stash: sequence number {seq_token}, expected sequence number {current_idx}");                              
                stashed.Add(seq_token, batch);
                return null;           
            }
            else
            {
                current_idx++;
                return batch;
            }
        }

        public async Task PostProcess(List<Tuple> batchToForward, INormalGrain currentOperator)
        {
            INormalGrain nextOperator = await currentOperator.GetNextoperator();
            if (batchToForward.Count > 0)
            {
                if (nextOperator != null)
                {
                    tuplesToSendAhead.AddRange(batchToForward);
                    // batchToForward[0].seq_token = current_seq_num;
                    // current_seq_num++;
                    // nextOperator.Process(batchToForward.AsImmutable());
                }

            }
            await ProcessStashed(currentOperator, nextOperator);

            if(tuplesToSendAhead.Count > 0 && sendingNextTask.IsCompleted)
            {
                sendingNextTask = SendNext(nextOperator);
            }
        }       

        private async Task ProcessStashed(INormalGrain currentOperator, INormalGrain nextOperator)
        {
            while(stashed.ContainsKey(current_idx))
            {
                List<Tuple> batch = stashed[current_idx];
                List<Tuple> batchToForward = new List<Tuple>();
                foreach(Tuple tuple in batch)
                {
                    Tuple ret = await currentOperator.Process_impl(tuple);
                    if(ret != null)
                    {
                        batchToForward.Add(ret);
                    }                
                }
                if (batchToForward.Count > 0)
                {
                    if(nextOperator != null)
                    {
                        tuplesToSendAhead.AddRange(batchToForward);
                        // batchToForward[0].seq_token = current_seq_num++;
                        // nextOperator.Process(batchToForward.AsImmutable());
                    }
                }
                stashed.Remove(current_idx);
                current_idx++;
            }

        }

        private async Task SendNext(INormalGrain nextOperator)
        {
            while(tuplesToSendAhead.Count > 0)
            {
                List<Tuple> batchToForward = tuplesToSendAhead.Take(Constants.batchSize).ToList();
                batchToForward[0].seq_token = current_seq_num++;
                tuplesToSendAhead = tuplesToSendAhead.Skip(Constants.batchSize).ToList();
                await nextOperator.Process(batchToForward.AsImmutable());
            }
        }

    }
}