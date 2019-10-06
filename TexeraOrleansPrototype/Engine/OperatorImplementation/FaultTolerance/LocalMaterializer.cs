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
using Orleans.Placement;
using Orleans.Runtime;
using System.Linq;
using Serialize.Linq.Serializers;
using System.Linq.Expressions;

namespace Engine.OperatorImplementation.FaultTolerance
{
    public class LocalMaterializer : ITupleProcessor
    {
        private StreamWriter sws;
        private Guid id;

        public LocalMaterializer(Guid id)
        {
            this.id = id;
        }

        public void Accept(TexeraTuple tuple)
        {
           sws.WriteLine(String.Join("|",tuple.FieldList));
        }

        public void OnRegisterSource(Guid from)
        {
            return;
        }

        public void NoMore()
        {
            sws.Close();
            return;
        }

        public Task Initialize()
        {
            string currentDir = Environment.CurrentDirectory;
            string pathName = currentDir+"/"+id+".tmp";
            sws = new StreamWriter(pathName, false, new UTF8Encoding(false), 65536);
            return Task.CompletedTask;
        }

        public bool HasNext()
        {
            return false;
        }

        public TexeraTuple Next()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
           sws.Close();
        }

        public Task<TexeraTuple> NextAsync()
        {
            throw new NotImplementedException();
        }

        public void MarkSourceCompleted(Guid source)
        {
            
        }
    }
}