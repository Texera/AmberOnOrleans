// using Orleans;
// using Orleans.Streams;
// using System;
// using System.Collections.Generic;
// using System.Text;
// using System.Threading.Tasks;
// using System.Linq;
// using Orleans.Concurrency;
// using TexeraUtilities;
// using Engine.OperatorImplementation.Common;

// namespace Engine.OperatorImplementation.MessagingSemantics
// {
//     /**
//     Note that due to this class, ideally there should be background sending of batches while the operator is processing. However, what is happeining is
//     something like shown in 1.txt. The SendNext() function is not able to send even one batch and the PostProcess function queues all batches in 
//     tuplesToSendAhead which is sent at the last when all batches are processed by KeywordSearch. This essentially is a loss of data parallelism. We don't
//     know if it is because SendNext() is waiting for the SynchronizationContext which is not being given up by PostProcess(). Only when all 100 calls of
//     PostProcess() finish, the SendNext() begins execution.
//      */
//     public class OrderingGrainWithContinuousSending
//     {
//         private Dictionary<ulong, List<TexeraTuple>> stashed = new Dictionary<ulong, List<TexeraTuple>>();
//         private ulong current_idx = 0;
//         private ulong current_seq_num = 0;

//         public List<TexeraTuple> tuplesToSendAhead = new List<TexeraTuple>();
//         private Task sendingNextTask = Task.CompletedTask;

//         public ulong GetOutgoingSequenceNumber()
//         {
//             return current_seq_num;
//         }

//         public ulong GetExpectedSequenceNumber()
//         {
//             return current_idx;
//         }

//         public void IncrementOutgoingSequenceNumber()
//         {
//             current_seq_num++;
//         }

//         public void IncrementExpectedSequenceNumber()
//         {
//             current_idx++;
//         }
        
//         public List<TexeraTuple> PreProcess(List<TexeraTuple> batch, IProcessorGrain currentOperator)
//         {
//             var seq_token = batch[0].seq_token;
//             string extensionKey = "";

//             if(seq_token < current_idx)
//             {
//                 // de-dup messages
//                 Console.WriteLine($"Grain {currentOperator.GetPrimaryKey(out extensionKey)} received duplicate message with sequence number {seq_token}: expected sequence number {current_idx}");
//                 return null;
//             }
//             if (seq_token != current_idx)
//             {
//                 Console.WriteLine($"Grain {currentOperator.GetPrimaryKey(out extensionKey)} received message ahead in sequence, being put in stash: sequence number {seq_token}, expected sequence number {current_idx}");                              
//                 stashed.Add(seq_token, batch);
//                 return null;           
//             }
//             else
//             {
//                 current_idx++;
//                 return batch;
//             }
//         }

//         // TODO: The third argument should ideally not be here
//         public async Task PostProcess(List<TexeraTuple> batchToForward, IProcessorGrain currentOperatorGrain, IAsyncStream<Immutable<List<TexeraTuple>>> stream)
//         { 
//             INormalGrain nextGrain = await currentOperatorGrain.GetNextGrain();
//             bool isLastGrain = await currentOperatorGrain.GetIsLastOperatorGrain();
//             bool shouldBeForwarded = (nextGrain != null) | isLastGrain;
//             if (batchToForward.Count > 0)
//             {
//                 if (shouldBeForwarded)
//                 {
//                     tuplesToSendAhead.AddRange(batchToForward);
//                 }

//             }
//             await ProcessStashed(currentOperatorGrain);

//             if(shouldBeForwarded && tuplesToSendAhead.Count > 0 && sendingNextTask.IsCompleted)
//             {
//                 sendingNextTask = SendNext(currentOperatorGrain, stream);
//             }

//             // if(currentOperator.GetPrimaryKey() == 3 && currentOperator.GetType() == typeof(FilterOperatorGrain))
//             // Console.Write($"Exiting {currentOperator.GetPrimaryKey()} PostProcess, ");
//         }       

//         private async Task ProcessStashed(IProcessorGrain currentOperatorGrain)
//         {
//             INormalGrain nextGrain = await currentOperatorGrain.GetNextGrain();
//             bool isLastGrain = await currentOperatorGrain.GetIsLastOperatorGrain();
//             bool shouldBeForwarded = (nextGrain != null) | isLastGrain;

//             while(stashed.ContainsKey(current_idx))
//             {
//                 List<TexeraTuple> batch = stashed[current_idx];
//                 List<TexeraTuple> batchToForward = new List<TexeraTuple>();
//                 foreach(TexeraTuple tuple in batch)
//                 {
//                     TexeraTuple ret = await currentOperatorGrain.Process_impl(tuple);
//                     if(ret != null)
//                     {
//                         batchToForward.Add(ret);
//                     }                
//                 }
//                 if (batchToForward.Count > 0)
//                 {
//                     if(shouldBeForwarded)
//                     {
//                         tuplesToSendAhead.AddRange(batchToForward);
//                     }
//                 }
//                 stashed.Remove(current_idx);
//                 current_idx++;
//             }

//         }

//         private async Task SendNext(IProcessorGrain currentOperatorGrain, IAsyncStream<Immutable<List<TexeraTuple>>> stream)
//         {
//             while(tuplesToSendAhead.Count > 0)
//             {
//                 // if(nextGrain.GetPrimaryKey() == 3)
//                 // Console.Write($"Sending {tuplesToSendAhead.Count} next batch, ");
//                 IProcessorGrain nextGrain = await currentOperatorGrain.GetNextGrain();
//                 bool isLastGrain = await currentOperatorGrain.GetIsLastOperatorGrain();
                
//                 List<TexeraTuple> batchToForward = tuplesToSendAhead.Take(Constants.batchSize).ToList();
//                 batchToForward[0].seq_token = current_seq_num++;
//                 tuplesToSendAhead = tuplesToSendAhead.Skip(Constants.batchSize).ToList();

//                 if(nextGrain != null)
//                 {
//                     await (nextGrain).Process(batchToForward.AsImmutable());
//                 }
//                 else if(isLastGrain)
//                 {
//                     await stream.OnNextAsync(batchToForward.AsImmutable());
//                 }
//             }
//         }

//     }
// }