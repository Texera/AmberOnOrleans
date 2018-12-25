using System;
using Engine.Common;

namespace Engine.Controller
{
    public class ExecutionController
    {
        public Guid ControllerGuid {get;}
        public GrainIdentifier GrainID {get;}

        public ExecutionController(Guid guid)
        {
            this.ControllerGuid = guid;
            GrainID = new GrainIdentifier(guid, "1");
        }
    }
}