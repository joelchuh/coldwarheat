using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum DefconRestrictionReason { None, GeographicRestriction, NuclearWarReached }
    public enum DefconChangeKind { TurnStartImprovement, BattlegroundCoupDegradation, ImproveOne, DegradeOne, SetLevel }
    public sealed class DefconRestrictionResult
    {
        public bool Allowed => Reason==DefconRestrictionReason.None;
        public DefconRestrictionReason Reason { get; }
        public string BlockedRegion { get; }
        internal DefconRestrictionResult(DefconRestrictionReason reason,string region=null) { Reason=reason;BlockedRegion=region; }
    }
    public sealed class DefconChangePlan
    {
        public DefconState Before { get; }
        public DefconState After { get; }
        public DefconChangeKind Kind { get; }
        public bool Changed => Before.Level!=After.Level;
        public bool NuclearWar => After.Level==After.Definition.NuclearLevel;
        public PlayerSide? PhasingPlayer { get; }
        internal DefconChangePlan(DefconState before,DefconState after,DefconChangeKind kind,PlayerSide? phasingPlayer)
        { Before=before;After=after;Kind=kind;PhasingPlayer=phasingPlayer; }
    }
    public static class DefconService
    {
        // A base geographic check, not complete action authorization or an exception-policy registry.
        public static DefconRestrictionResult CheckRestriction(DefconState state,DefconActionKind action,CountryId country)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            if(!Enum.IsDefined(typeof(DefconActionKind),action))throw new ArgumentOutOfRangeException(nameof(action));
            var target=state.Definition.Map.GetCountry(country);
            if(state.Level==state.Definition.NuclearLevel)return new DefconRestrictionResult(DefconRestrictionReason.NuclearWarReached);
            if(!state.Definition.RestrictedActions.Contains(action))return new DefconRestrictionResult(DefconRestrictionReason.None);
            var region=state.Definition.BlockedRegions[state.Level].FirstOrDefault(r=>r==target.Region || target.Subregions.Contains(r));
            return region==null?new DefconRestrictionResult(DefconRestrictionReason.None)
                :new DefconRestrictionResult(DefconRestrictionReason.GeographicRestriction,region);
        }
        // Caller supplies a verified reason/root responsibility, then commits the entire surrounding transaction.
        public static DefconChangePlan PlanChange(DefconState state,DefconChangeKind kind,PlayerSide? phasingPlayer=null,int? targetLevel=null)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            if(!Enum.IsDefined(typeof(DefconChangeKind),kind))throw new ArgumentOutOfRangeException(nameof(kind));
            if(phasingPlayer.HasValue && phasingPlayer!=PlayerSide.US && phasingPlayer!=PlayerSide.USSR)
                throw new ArgumentOutOfRangeException(nameof(phasingPlayer));
            if((kind==DefconChangeKind.SetLevel)!=targetLevel.HasValue)throw new ArgumentException("Only SetLevel requires a target.");
            var definition=state.Definition;
            if(state.Level==definition.NuclearLevel)throw new InvalidOperationException("Nuclear war cannot be reversed.");
            if(targetLevel.HasValue && (targetLevel<definition.NuclearLevel || targetLevel>definition.MaximumLevel))
                throw new ArgumentOutOfRangeException(nameof(targetLevel));
            long next=kind switch
            {
                DefconChangeKind.TurnStartImprovement=>(long)state.Level+definition.TurnStartImprovement,
                DefconChangeKind.BattlegroundCoupDegradation=>(long)state.Level-definition.BattlegroundCoupDegradation,
                DefconChangeKind.ImproveOne=>(long)state.Level+1,
                DefconChangeKind.DegradeOne=>(long)state.Level-1,
                DefconChangeKind.SetLevel=>targetLevel.Value,
                _=>throw new ArgumentOutOfRangeException(nameof(kind))
            };
            if(next<state.Level && !phasingPlayer.HasValue)throw new ArgumentException("Degradation requires the phasing player.");
            int level=(int)Math.Max(definition.NuclearLevel,Math.Min(definition.MaximumLevel,next));
            return new DefconChangePlan(state,level==state.Level?state:new DefconState(definition,level),kind,phasingPlayer);
        }
    }
}
