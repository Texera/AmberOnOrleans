using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Orleans.Concurrency;
using TexeraUtilities;

namespace Engine.OperatorImplementation.MessagingSemantics
{
    /**
    Note that due to this class, ideally there should be background sending of batches while the operator is processing. However, what is happeining is
    something like shown in 1.txt. The SendNext() function is not able to send even one batch and the PostProcess function queues all batches in 
    tuplesToSendAhead which is sent at the last when all batches are processed by KeywordSearch. This essentially is a loss of data parallelism. We don't
    know if it is because SendNext() is waiting for the SynchronizationContext which is not being given up by PostProcess(). Only when all 100 calls of
    PostProcess() finish, the SendNext() begins execution.
     */
    public class OrderingGrainWithContinuousSending : IOrderingEnforcer
    {
        private Dictionary<ulong, List<TexeraTuple>> stashed = new Dictionary<ulong, List<TexeraTuple>>();
        private ulong current_idx = 0;
        private ulong current_seq_num = 0;

        public List<TexeraTuple> tuplesToSendAhead = new List<TexeraTuple>();
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
        
        public List<TexeraTuple> PreProcess(List<TexeraTuple> batch, INormalGrain currentOperator)
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

        public async Task PostProcess(List<TexeraTuple> batchToForward, INormalGrain currentOperator)
        {
            INormalGrain nextOperator = await currentOperator.GetNextoperator();
            if (batchToForward.Count > 0)
            {
                if (nextOperator != null)
                {
                    tuplesToSendAhead.AddRange(batchToForward);
                }

            }
            await ProcessStashed(currentOperator, nextOperator);

            if(nextOperator != null && tuplesToSendAhead.Count > 0 && sendingNextTask.IsCompleted)
            {
                sendingNextTask = SendNext(nextOperator);
            }

            // if(currentOperator.GetPrimaryKeyLong() == 3 && currentOperator.GetType() == typeof(OrderedFilterOperatorWithSqNum))
            // Console.Write($"Exiting {currentOperator.GetPrimaryKeyLong()} PostProcess, ");
        }       

        private async Task ProcessStashed(INormalGrain currentOperator, INormalGrain nextOperator)
        {
            while(stashed.ContainsKey(current_idx))
            {
                List<TexeraTuple> batch = stashed[current_idx];
                List<TexeraTuple> batchToForward = new List<TexeraTuple>();
                foreach(TexeraTuple tuple in batch)
                {
                    TexeraTuple ret = await currentOperator.Process_impl(tuple);
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
                // if(nextOperator.GetPrimaryKeyLong() == 3)
                // Console.Write($"Sending {tuplesToSendAhead.Count} next batch, ");
                
                List<TexeraTuple> batchToForward = tuplesToSendAhead.Take(Constants.batchSize).ToList();
                batchToForward[0].seq_token = current_seq_num++;
                tuplesToSendAhead = tuplesToSendAhead.Skip(Constants.batchSize).ToList();
                await nextOperator.Process(batchToForward.AsImmutable());
            }
        }

    }
}