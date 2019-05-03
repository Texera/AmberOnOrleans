using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Orleans.Concurrency;
using Engine.OperatorImplementation.MessagingSemantics;
using Engine.OperatorImplementation.Common;
using TexeraUtilities;
using System.Linq;

namespace Engine.OperatorImplementation.Operators
{
    public class CrossRippleJoinOperatorGrain : WorkerGrain, ICrossRippleJoinOperatorGrain
    {
        Dictionary<int,List<TexeraTuple>> CrossRippleJoinedTuples=new Dictionary<int, List<TexeraTuple>>();
        int TableID;
        int counter=0;
        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            TableID=((CrossRippleJoinPredicate)predicate).TableID;
            return Task.CompletedTask;
        }
        protected override List<TexeraTuple> ProcessTuple(TexeraTuple tuple)
        {
            //Console.WriteLine(++counter+" tuple processed");
            List<TexeraTuple> output=new List<TexeraTuple>();
            foreach(KeyValuePair<int,List<TexeraTuple>> entry in CrossRippleJoinedTuples)
            {
                if(entry.Key!=tuple.TableID)
                {
                    foreach(TexeraTuple t in entry.Value)
                    {
                        output.Add(new TexeraTuple(TableID,tuple.FieldList.Concat(t.FieldList).ToArray()));
                    }
                }
            }
            if(CrossRippleJoinedTuples.ContainsKey(tuple.TableID))
            {
                CrossRippleJoinedTuples[tuple.TableID].Add(tuple);
            }
            else
            {
                CrossRippleJoinedTuples.Add(tuple.TableID,new List<TexeraTuple>{tuple});
            }
            return output;
        }
    }

}