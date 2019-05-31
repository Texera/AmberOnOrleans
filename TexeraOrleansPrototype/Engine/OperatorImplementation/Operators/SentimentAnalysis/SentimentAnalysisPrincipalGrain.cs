using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TexeraUtilities;
using Engine.OperatorImplementation.Common;

namespace Engine.OperatorImplementation.Operators
{
    public class SentimentAnalysisPrinicipalGrain : PrincipalGrain, ISentimentAnalysisPrincipalGrain
    {
        public override IWorkerGrain GetOperatorGrain(string extension)
        {
            return this.GrainFactory.GetGrain<ISentimentAnalysisOperatorGrain>(this.GetPrimaryKey(), extension);
        }

    }
}