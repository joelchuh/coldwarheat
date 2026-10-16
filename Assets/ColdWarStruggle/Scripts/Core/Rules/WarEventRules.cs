using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class WarCalculation
    {
        public WarEventDefinition Effect { get; }
        public PlayerSide Beneficiary { get; }
        public CountryId Target { get; }
        public int Roll { get; }
        public int Penalty { get; }
        public int AdjustedRoll { get; }
        public bool Success { get; }
        public CountryState Before { get; }
        public CountryState After { get; }
        public int Captured { get; }
        public IReadOnlyList<CountryId> ControlledNeighbors { get; }
        public bool OpponentSuperpowerAdjacent { get; }
        public bool TargetControlPenalty { get; }
        internal WarCalculation(WarEventDefinition effect,WarPolicy policy,PlayerSide owner,CountryId target,int roll,CountryState before,IReadOnlyList<CountryId> neighbors,bool power,bool targetPenalty)
        {
            Effect=effect;Beneficiary=owner;Target=target;Roll=roll;Before=before;ControlledNeighbors=Array.AsReadOnly(neighbors.ToArray());OpponentSuperpowerAdjacent=power;TargetControlPenalty=targetPenalty;
            Penalty=checked((neighbors.Count+(power?1:0)+(targetPenalty?1:0))*policy.NeighborPenalty);AdjustedRoll=checked(roll-Penalty);Success=AdjustedRoll>=policy.WinningResult;
            Captured=Success?before.For(owner==PlayerSide.US?PlayerSide.USSR:PlayerSide.US):0;
            After=!Success?before:owner==PlayerSide.US?new CountryState(checked(before.US+Captured),0):new CountryState(0,checked(before.USSR+Captured));
        }
    }
    public static class WarEventRules
    {
        public static IReadOnlyList<CountryId> QueryTargets(WarEventCatalog catalog,CardId card)=>Effect(catalog,card).Targets;
        internal static WarEventDefinition Effect(WarEventCatalog catalog,CardId card)
        {
            if(catalog==null)throw new ArgumentNullException(nameof(catalog));
            return catalog.Effects.TryGetValue(card,out var e)?e:throw new ArgumentException("Unregistered war card.");
        }
        internal static void RequireBoard(WarEventCatalog catalog,IReadOnlyDictionary<CountryId,CountryState> countries)
        {
            if(catalog==null || countries==null)throw new ArgumentNullException();
            if(countries.Count!=catalog.Map.Countries.Count || countries.Keys.Any(c=>!catalog.Map.Contains(c)))throw new ArgumentException("Complete matching country state required.");
        }
        public static WarCalculation Calculate(WarEventCatalog catalog,CardId card,IReadOnlyDictionary<CountryId,CountryState> countries,PlayerSide controller,CountryId target,int roll)
        {
            RequireBoard(catalog,countries);var effect=Effect(catalog,card);
            if((controller!=PlayerSide.US && controller!=PlayerSide.USSR) || !effect.Targets.Contains(target) || roll<1 || roll>catalog.Policy.DieSides)throw new ArgumentException("Invalid war controller, target or die.");
            var owner=effect.Kind==WarEventKind.IndoPakistani?controller:PlayerSide.USSR;var opponent=owner==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
            bool Controlled(CountryId c)=>ControlRules.GetController(countries[c].US,countries[c].USSR,catalog.Map.GetCountry(c).Stability)==opponent;
            var neighbors=catalog.Map.GetCountryNeighbors(target).Where(Controlled).ToArray();
            return new WarCalculation(effect,catalog.Policy,owner,target,roll,countries[target],neighbors,catalog.Map.GetSuperpowerNeighbors(opponent).Contains(target),effect.Kind==WarEventKind.ArabIsraeli && Controlled(target));
        }
    }
}
