using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;

namespace TexeraOrleansPrototype
{
    public class ScanOperator : Grain, IScanOperator
    {
        public List<Immutable<Tuple>> Rows = new List<Immutable<Tuple>>();
        public INormalGrain nextOperator;
        System.IO.StreamReader file;

        public override Task OnActivateAsync()
        {
            if(Program.ordered_on)
                nextOperator = this.GrainFactory.GetGrain<IFilterOperator>(this.GetPrimaryKeyLong());
            // else
                // nextOperator = base.GrainFactory.GetGrain<IFilterOperator>(this.GetPrimaryKeyLong());
            string p2;
            if (Program.num_scan == 1)
                p2 = Program.dir + Program.dataset + "_input.csv";
            else
                p2 = Program.dir + Program.dataset + "_input" + "_" + (this.GetPrimaryKeyLong() - 1) + ".csv";
            file = new System.IO.StreamReader(p2);
            return base.OnActivateAsync();
        }
        public override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }

        public Task SubmitTuples() 
        {
            List<Immutable<Tuple>> batch = new List<Immutable<Tuple>>();
            ulong seq = 0;
            for (int i = 0; i < Rows.Count; ++i)
            {
                batch.Add(Rows[i]);
                if(i%1000 == 0)
                {
                    batch[0].Value.seq_token = seq++;
                    nextOperator.Process(batch);
                    batch.Clear();
                    batch = new List<Immutable<Tuple>>();
                }
                // Console.WriteLine("Scan " + (this.GetPrimaryKeyLong() - 1).ToString() + " sending "+i.ToString());
                // nextOperator.Process(Rows[i]);
	        }

            batch[0].Value.seq_token = seq++;
            nextOperator.Process(batch);
            batch.Clear();
            batch = new List<Immutable<Tuple>>();

            batch.Add(new Immutable<Tuple>(new Tuple(seq ,- 1, null)));
            nextOperator.Process(batch);
            Console.WriteLine("Scan " + (this.GetPrimaryKeyLong() - 1).ToString() + " sending done");
            return Task.CompletedTask;
        }


        public Task LoadTuples()
        {
            string line;
            ulong count = 0;
            while ((line = file.ReadLine()) != null)
            {
                // The sequence token filled here will be replaced later in SubmitTuples().
                Rows.Add(new Immutable<Tuple>(new Tuple(count, (int)count, line.Split(","))));
                count++;
            }
            Console.WriteLine("Scan " + (this.GetPrimaryKeyLong() - 1).ToString() + " loading done");
            return Task.CompletedTask;
        }
       
    }
}