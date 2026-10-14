using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class RealignmentDefinition
    {
        public int DieMinimum { get; }
        public int DieMaximum { get; }
        public int OpsCostPerRoll { get; }
        public int AdjacentControlledBonus { get; }
        public int MoreInfluenceBonus { get; }
        public int SuperpowerBonus { get; }
        public RealignmentDefinition(int minimum,int maximum,int cost,int adjacent,int influence,int superpower)
        {
            if(minimum<1 || maximum<minimum || cost<1 || adjacent<0 || influence<0 || superpower<0)throw new ArgumentOutOfRangeException();
            DieMinimum=minimum;DieMaximum=maximum;OpsCostPerRoll=cost;AdjacentControlledBonus=adjacent;MoreInfluenceBonus=influence;SuperpowerBonus=superpower;
        }
    }
    public sealed class RealignmentModifiers
    {
        public PlayerSide Side { get; }
        public IReadOnlyList<CountryId> ControlledNeighbors { get; }
        public bool HasMoreInfluence { get; }
        public bool HasSuperpowerNeighbor { get; }
        public long AdjacentControlled { get; }
        public long MoreInfluence { get; }
        public long Superpower { get; }
        public long Total { get; }
        internal RealignmentModifiers(RealignmentDefinition d,PlayerSide side,IEnumerable<CountryId> neighbors,bool more,bool power)
        {
            Side=side;ControlledNeighbors=Array.AsReadOnly(neighbors.ToArray());HasMoreInfluence=more;HasSuperpowerNeighbor=power;
            AdjacentControlled=(long)ControlledNeighbors.Count*d.AdjacentControlledBonus;MoreInfluence=more?d.MoreInfluenceBonus:0;Superpower=power?d.SuperpowerBonus:0;Total=checked(AdjacentControlled+MoreInfluence+Superpower);
        }
    }
    public enum RealignmentTargetReason { Allowed, NoOpponentInfluence, GeographicRestriction, NuclearWarReached }
    public sealed class RealignmentCalculation
    {
        public CountryId Target { get; }
        public PlayerSide Actor { get; }
        public CountryState Before { get; }
        public CountryState After { get; }
        public int UsRoll { get; }
        public int UssrRoll { get; }
        public RealignmentModifiers US { get; }
        public RealignmentModifiers USSR { get; }
        public long UsTotal { get; }
        public long UssrTotal { get; }
        public long DifferenceForUS { get; }
        public PlayerSide? RemovedSide { get; }
        public int Removed { get; }
        internal RealignmentCalculation(CountryId target,PlayerSide actor,CountryState before,int usRoll,int ussrRoll,RealignmentModifiers us,RealignmentModifiers ussr)
        {
            Target=target;Actor=actor;Before=before;UsRoll=usRoll;UssrRoll=ussrRoll;US=us;USSR=ussr;
            UsTotal=checked(usRoll+us.Total);UssrTotal=checked(ussrRoll+ussr.Total);DifferenceForUS=checked(UsTotal-UssrTotal);
            var removedUS=DifferenceForUS<0?(int)Math.Min(-(long)DifferenceForUS,before.US):0;
            var removedUSSR=DifferenceForUS>0?(int)Math.Min(DifferenceForUS,before.USSR):0;
            Removed=removedUS+removedUSSR;RemovedSide=removedUS>0?PlayerSide.US:removedUSSR>0?PlayerSide.USSR:(PlayerSide?)null;
            After=new CountryState(before.US-removedUS,before.USSR-removedUSSR);
        }
    }
    // Trusted base arithmetic. Flat OPS changes available rolls, never these die modifiers.
    public static class RealignmentRules
    {
        public static RealignmentTargetReason CheckTarget(DefconState defcon,CountryId target,CountryState before,PlayerSide actor,IReadOnlyCollection<string> interventions)
        {
            if(defcon==null || interventions==null)throw new ArgumentNullException();
            OpsGeneralGuard.Side(actor);
            if(interventions.Count!=0)throw new NotSupportedException("Realignment interventions are not implemented.");
            var restriction=DefconService.CheckRestriction(defcon,DefconActionKind.Realignment,target);
            if(restriction.Reason==DefconRestrictionReason.NuclearWarReached)return RealignmentTargetReason.NuclearWarReached;
            if(restriction.Reason==DefconRestrictionReason.GeographicRestriction)return RealignmentTargetReason.GeographicRestriction;
            if(!restriction.Allowed)throw new NotSupportedException("Unknown realignment restriction.");
            return before.For(actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US)>0?RealignmentTargetReason.Allowed:RealignmentTargetReason.NoOpponentInfluence;
        }
        static void ValidateSnapshot(RealignmentDefinition definition,MapDefinition map,IReadOnlyDictionary<CountryId,CountryState> countries,CountryId target,PlayerSide side)
        {
            if(definition==null || map==null || countries==null)throw new ArgumentNullException();
            OpsGeneralGuard.Side(side);
            if(countries.Count!=map.Countries.Count || countries.Keys.Any(id=>!map.Contains(id)) || !countries.ContainsKey(target))throw new ArgumentException("Complete current realignment snapshot required.");
        }
        public static RealignmentModifiers Modifiers(RealignmentDefinition definition,MapDefinition map,IReadOnlyDictionary<CountryId,CountryState> countries,CountryId target,PlayerSide side)
        {
            ValidateSnapshot(definition,map,countries,target,side);
            var neighbors=map.GetCountryNeighbors(target).Where(id=>ControlRules.GetController(countries[id].US,countries[id].USSR,map.GetCountry(id).Stability)==side);
            var country=countries[target];var opponent=side==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
            return new RealignmentModifiers(definition,side,neighbors,country.For(side)>country.For(opponent),map.GetSuperpowerNeighbors(side).Contains(target));
        }
        public static RealignmentCalculation Calculate(RealignmentDefinition definition,DefconState defcon,IReadOnlyDictionary<CountryId,CountryState> countries,CountryId target,PlayerSide actor,int usRoll,int ussrRoll,IReadOnlyCollection<string> interventions)
        {
            if(defcon==null)throw new ArgumentNullException(nameof(defcon));
            var map=defcon.Definition.Map;ValidateSnapshot(definition,map,countries,target,actor);
            if(usRoll<definition.DieMinimum || usRoll>definition.DieMaximum || ussrRoll<definition.DieMinimum || ussrRoll>definition.DieMaximum)throw new ArgumentOutOfRangeException("Realignment rolls outside die bounds.");
            var reason=CheckTarget(defcon,target,countries[target],actor,interventions);
            if(reason!=RealignmentTargetReason.Allowed)throw new InvalidOperationException("Realignment target rejected: "+reason);
            return new RealignmentCalculation(target,actor,countries[target],usRoll,ussrRoll,Modifiers(definition,map,countries,target,PlayerSide.US),Modifiers(definition,map,countries,target,PlayerSide.USSR));
        }
    }
}
