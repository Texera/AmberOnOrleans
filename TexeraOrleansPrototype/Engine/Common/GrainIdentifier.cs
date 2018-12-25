using System;

namespace Engine.Common
{
    public class GrainIdentifier
    {
        public Guid PrimaryKey {get; set;}
        public string ExtensionKey {get; set;}

        public GrainIdentifier(Guid primaryKey, string extension)
        {
            this.PrimaryKey = primaryKey;
            this.ExtensionKey = extension;
        }
    }
}