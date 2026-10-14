using System;
using System.Collections.Generic;
namespace ColdWarStruggle.Core
{
    public sealed class CoupDefinition
    {
        public int DieMinimum { get; }
        public int DieMaximum { get; }
        public int StabilityMultiplier { get; }
        public CoupDefinition(int dieMinimum,int dieMaximum,int stabilityMultiplier)
        {
            if(dieMinimum<1 || dieMaximum<dieMinimum || stabilityMultiplier<1)throw new ArgumentOutOfRangeException();
            DieMinimum=dieMinimum;DieMaximum=dieMaximum;StabilityMultiplier=stabilityMultiplier;
        }
    }
    public enum CoupTargetReason { Allowed, NoOpponentInfluence, GeographicRestriction, NuclearWarReached }
    public sealed class CoupCalculation
    {
        public CoupDefinition Definition { get; }
        public CountryDefinition Target { get; }
        public PlayerSide Actor { get; }
        public CountryState Before { get; }
        public CountryState After { get; }
        public int Roll { get; }
        public int Strength { get; }
        public long Margin { get; }
        public int Removed { get; }
        public int Added { get; }
        public bool RequiresBattlegroundDegradation => Target.Battleground;
        internal CoupCalculation(CoupDefinition definition,CountryDefinition target,PlayerSide actor,CountryState before,CountryState after,int roll,int strength,long margin,int removed,int added)
        { Definition=definition;Target=target;Actor=actor;Before=before;After=after;Roll=roll;Strength=strength;Margin=margin;Removed=removed;Added=added; }
    }
    // Base trusted arithmetic only; the host owns RNG, military credit, DEFCON and the atomic command.
    public static class CoupRules
    {
        public static CoupTargetReason CheckTarget(DefconState defcon,CountryId target,CountryState before,PlayerSide actor,IReadOnlyCollection<string> activeEffectIds)
        {
            if(defcon==null || activeEffectIds==null)throw new ArgumentNullException();
            if(actor!=PlayerSide.US && actor!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(actor));
            // This is an explicit trusted snapshot of all active effects, never a client-provided exemption.
            if(activeEffectIds.Count!=0)throw new NotSupportedException("Coup effect interventions are not implemented.");
            var restriction=DefconService.CheckRestriction(defcon,DefconActionKind.Coup,target);
            if(restriction.Reason==DefconRestrictionReason.NuclearWarReached)return CoupTargetReason.NuclearWarReached;
            if(restriction.Reason==DefconRestrictionReason.GeographicRestriction)return CoupTargetReason.GeographicRestriction;
            if(!restriction.Allowed)throw new NotSupportedException("Unknown DEFCON restriction.");
            return before.For(actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US)>0
                ?CoupTargetReason.Allowed:CoupTargetReason.NoOpponentInfluence;
        }
        public static CoupCalculation Calculate(CoupDefinition definition,DefconState defcon,CountryId target,CountryState before,PlayerSide actor,int verifiedStrength,int verifiedRoll,IReadOnlyCollection<string> activeEffectIds)
        {
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            if(verifiedStrength<1)throw new ArgumentOutOfRangeException(nameof(verifiedStrength));
            if(verifiedRoll<definition.DieMinimum || verifiedRoll>definition.DieMaximum)throw new ArgumentOutOfRangeException(nameof(verifiedRoll));
            var reason=CheckTarget(defcon,target,before,actor,activeEffectIds);
            if(reason!=CoupTargetReason.Allowed)throw new InvalidOperationException("Coup target rejected: "+reason);
            var country=defcon.Definition.Map.GetCountry(target);
            var opponent=actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
            long margin=(long)verifiedRoll+verifiedStrength-(long)definition.StabilityMultiplier*country.Stability;
            var removed=(int)Math.Min(Math.Max(0L,margin),before.For(opponent));
            var added=checked((int)(Math.Max(0L,margin)-removed));
            var own=checked(before.For(actor)+added);var enemy=before.For(opponent)-removed;
            var after=actor==PlayerSide.US?new CountryState(own,enemy):new CountryState(enemy,own);
            return new CoupCalculation(definition,country,actor,before,after,verifiedRoll,verifiedStrength,margin,removed,added);
        }
    }
}
