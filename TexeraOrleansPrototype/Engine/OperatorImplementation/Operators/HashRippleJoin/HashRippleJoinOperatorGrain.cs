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
    public class HashRippleJoinOperatorGrain : WorkerGrain, IHashRippleJoinOperatorGrain
    {
        Dictionary<int,Dictionary<string,List<TexeraTuple>>> joinedTuples=new Dictionary<int, Dictionary<string, List<TexeraTuple>>>();
        int joinFieldIndex=-1;
        int TableID;
        public override Task Init(IWorkerGrain self, PredicateBase predicate, IPrincipalGrain principalGrain)
        {
            base.Init(self,predicate,principalGrain);
            joinFieldIndex=((HashRippleJoinPredicate)predicate).JoinFieldIndex;
            TableID=((HashRippleJoinPredicate)predicate).TableID;
            return Task.CompletedTask;
        }
        protected override void ProcessTuple(TexeraTuple tuple,List<TexeraTuple> output)
        {
            string field=tuple.FieldList[joinFieldIndex];
            List<string> fields=tuple.FieldList.ToList();
            fields.RemoveAt(joinFieldIndex);
            foreach(KeyValuePair<int,Dictionary<string,List<TexeraTuple>>> entry in joinedTuples)
            {
                if(entry.Key!=tuple.TableID && entry.Value.ContainsKey(field))
                {
                    foreach(TexeraTuple joinedTuple in entry.Value[field])
                    {
                        output.Add(new TexeraTuple(TableID,joinedTuple.FieldList.Concat(fields).ToArray()));
                    }
                }
            }
            if(!joinedTuples.ContainsKey(tuple.TableID))
            {
                Dictionary<string,List<TexeraTuple>> d = new Dictionary<string, List<TexeraTuple>>();
                d.Add(field,new List<TexeraTuple>{tuple});
                joinedTuples.Add(tuple.TableID,d);
            }
            else if(!joinedTuples[tuple.TableID].ContainsKey(field))
            {
                joinedTuples[tuple.TableID].Add(field,new List<TexeraTuple>{tuple});
            }
            else
            {
                joinedTuples[tuple.TableID][field].Add(tuple);
            }
        }
    }

}