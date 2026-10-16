using System;
namespace ColdWarStruggle.Core
{
    public sealed class StandardDieDefinition
    {
        public string Id { get; }
        public int Minimum { get; }
        public int Maximum { get; }
        public StandardDieDefinition(string id,int minimum,int maximum)
        {
            if(id!="die.standard_d6" || minimum!=1 || maximum!=6)throw new ArgumentException("Unsupported standard die profile.");
            Id=id;Minimum=minimum;Maximum=maximum;
        }
    }
}
