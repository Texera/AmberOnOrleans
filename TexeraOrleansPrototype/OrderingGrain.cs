using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;

namespace TexeraOrleansPrototype
{
    public class OrderingGrain: NormalGrain
    {
        private Dictionary<ulong, List<Immutable<Tuple>>> stashed = new Dictionary<ulong, List<Immutable<Tuple>>>();
        private ulong current_idx = 0;
        private ulong current_seq_num = 0;
        
        public override Task Process(List<Immutable<Tuple>> batch)
        {
            var seq_token = batch[0].Value.seq_token;

            if(seq_token < current_idx)
            {
                // de-dup messages
                return Task.CompletedTask;
            }
            if (seq_token != current_idx)
            {
                Console.WriteLine("stashed " + stashed.Count);                              
                stashed.Add(seq_token, batch);                
            }
            else
            {
                Process_impl(ref batch);
                if (batch != null)
                {
                    if(next_op != null)
                    {
                        if (next_op is OrderingGrain)
                        {
                            // List<Immutable<Tuple>> newBatch = new List<Immutable<Tuple>>();
                            // Immutable<Tuple> firstTuple = new Immutable<Tuple>();
                            
                            // if(batch[0].Value.id != -1)
                            // {

                            // }
                            // newBatch.Add(new Immutable<Tuple>(current_seq_num++, batch[0].Value.id, ));
                            // for(int i=0; i< batch.Count; i++)
                            // {
                            //     newBatch.Add(new Tuple(count, (int)count, line.Split(",")))
                            // }
                            // BUG HERE: the below token doesn't get incremented
                            batch[0].Value.seq_token = current_seq_num++;
                        }

                        
                        if (next_op != null)
                            next_op.Process(new List<Immutable<Tuple>>(batch));
                    }
                    
                }
                current_idx++;
                ProcessStashed();
            }
            return Task.CompletedTask;
        }

        private void ProcessStashed()
        {
            while (true)
            {
                if (stashed.ContainsKey(current_idx))
                {
                    List<Immutable<Tuple>> batch = stashed[current_idx];
                    Process_impl(ref batch);
                    if (batch != null)
                    {
                        if (next_op is OrderingGrain)
                            batch[0].Value.seq_token = current_seq_num++;
                        if (next_op != null)
                            next_op.Process(new List<Immutable<Tuple>>(batch));
                    }
                    stashed.Remove(current_idx);
                    current_idx++;
                }
                else
                    break;
            }
        }

    }
}
