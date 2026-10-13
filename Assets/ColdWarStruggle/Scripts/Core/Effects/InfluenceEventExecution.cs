using System;

namespace ColdWarStruggle.Core
{
    public enum InfluenceEventStage { Ready, AwaitingChoice, Resolved }

    // Issued only by trusted setup/flow code, never supplied by a player's command.
    public sealed class InfluenceEventExecution
    {
        public string Id { get; }
        public InfluenceEffectDefinition Definition { get; }
        public PlayerSide PhasingPlayer { get; }
        public PlayerSide Owner => Definition.Beneficiary;
        public InfluenceEventPolicy Policy { get; }
        public InfluenceEventStage Stage { get; }
        public string DecisionId { get; }
        public long? DecisionSourceVersion { get; }
        public InfluenceEventStatus? Outcome { get; }
        public InfluenceEventExecution(string id,InfluenceEffectDefinition definition,PlayerSide phasingPlayer,InfluenceEventPolicy policy=InfluenceEventPolicy.Unknown)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>128 || definition==null)throw new ArgumentException("Invalid event execution.");
            if(phasingPlayer!=PlayerSide.US && phasingPlayer!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(phasingPlayer));
            Id=id;Definition=definition;PhasingPlayer=phasingPlayer;Policy=policy;
        }
        InfluenceEventExecution(InfluenceEventExecution source,InfluenceEventStage stage,string decisionId,long? decisionVersion,InfluenceEventStatus? outcome)
        {
            Id=source.Id;Definition=source.Definition;PhasingPlayer=source.PhasingPlayer;Policy=source.Policy;
            Stage=stage;DecisionId=decisionId;DecisionSourceVersion=decisionVersion;Outcome=outcome;
        }
        internal InfluenceEventExecution Wait(long version)=>new InfluenceEventExecution(this,InfluenceEventStage.AwaitingChoice,Id+":choice",version,null);
        internal InfluenceEventExecution Complete(InfluenceEventStatus outcome)=>new InfluenceEventExecution(this,InfluenceEventStage.Resolved,DecisionId,DecisionSourceVersion,outcome);
    }
}
