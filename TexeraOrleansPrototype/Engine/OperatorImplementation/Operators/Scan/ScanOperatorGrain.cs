using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;
using Engine.OperatorImplementation.Common;
using TexeraUtilities;

namespace Engine.OperatorImplementation.Operators
{
    public class ScanOperatorGrain : NormalGrain, IScanOperatorGrain
    {
        public List<TexeraTuple> Rows = new List<TexeraTuple>();
        System.IO.StreamReader file;

        public override Task OnActivateAsync()
        {
            
            // nextGrain = this.GrainFactory.GetGrain<IFilterOperatorGrain>(this.GetPrimaryKey(), Constants.OperatorAssemblyPathPrefix);

            string p2;
            string extensionKey = "";
            Guid key = this.GetPrimaryKey(out extensionKey);
            if (Constants.num_scan == 1)
                p2 = Constants.dir + Constants.dataset + "_input.csv";
            else
                p2 = Constants.dir + Constants.dataset + "_input" + "_" + (Int32.Parse(extensionKey) + 1) + ".csv";
            file = new System.IO.StreamReader(p2);
            return base.OnActivateAsync();
        }

        public override async Task PauseGrain()
        {
            await nextGrain.PauseGrain();
        }

        public override async Task ResumeGrain()
        {
            await nextGrain.ResumeGrain();
            // ((IProcessorGrain)nextGrain).StartProcessAfterPause();
        }

        public async Task SubmitTuples() 
        {
            try
            {
                List<TexeraTuple> batch = new List<TexeraTuple>();
                ulong seq = 0;

                for (int i = 1; i <= Rows.Count; ++i)
                {
                    batch.Add(Rows[i-1]);
                    if(i%Constants.batchSize == 0)
                    {
                        batch[0].seq_token = seq++;
                        // TODO: We can't call batch.Clear() after this because it somehow ends
                        // up clearing the memory and the next grain gets list with no tuples.
                        await (nextGrain).ReceiveTuples(batch.AsImmutable());
                        // batch.Clear();
                        batch = new List<TexeraTuple>();
                    }
                }

                // Console.WriteLine(seq);
                if(batch.Count > 0)
                {
                    batch[0].seq_token = seq++;
                    await (nextGrain).ReceiveTuples(batch.AsImmutable());
                    // batch.Clear();
                    batch = new List<TexeraTuple>();
                }

                // Console.WriteLine("Seq num for last tuple " + seq);
                batch.Add(new TexeraTuple(seq ,- 1, null));
                await (nextGrain).ReceiveTuples(batch.AsImmutable());

                string extensionKey = "";
                this.GetPrimaryKey(out extensionKey);
                Console.WriteLine("Scan " + (this.GetPrimaryKey(out extensionKey)).ToString() +" "+extensionKey + " sending done");
             // return Task.CompletedTask;
            }
            catch(Exception ex)
            {
                Console.WriteLine("EXCEPTION in Sending Tuples - "+ ex.ToString());
            }
        }


        public Task LoadTuples()
        {
            string line;
            ulong count = 0;
            while ((line = file.ReadLine()) != null)
            {
                // The sequence token filled here will be replaced later in SubmitTuples().
                Rows.Add(new TexeraTuple(count, (int)count, line.Split(",")));
                count++;
            }
            string extensionKey = "";
            this.GetPrimaryKey(out extensionKey);
            Console.WriteLine("Scan " + (this.GetPrimaryKey(out extensionKey)).ToString() +" "+extensionKey + " loading done");
            return Task.CompletedTask;
        }
       
    }
}