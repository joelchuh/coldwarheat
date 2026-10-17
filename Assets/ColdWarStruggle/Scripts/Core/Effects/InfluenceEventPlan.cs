using System;
using System.Collections.Generic;
using System.Linq;

namespace ColdWarStruggle.Core
{
    public enum InfluenceEventPolicy { Unknown, NoActiveInterference, Forbidden }
    public enum InfluenceEventStatus { Ready, AwaitingChoice, ResolvedChange, ResolvedNoChange, Rejected, RequiresRuling, Blocked }
    public enum InfluenceEventError { None, InvalidDefinition, InvalidSelection, WrongDecisionOwner, StaleSelection, NumericOverflow, GameAlreadyEnded, InterferenceUnknown, CandidateShortage, Forbidden, MissingTurnContext, SetupInProgress, RelocationOverlap }

    public sealed class InfluenceEventSelection
    {
        public const int MaxTargets = 128;
        public PlayerSide Owner { get; }
        public long SourceVersion { get; }
        public IReadOnlyList<CountryId> Targets { get; }
        public InfluenceDecisionKind Kind { get; }
        public IReadOnlyList<InfluenceAllocation> Allocations { get; }
        public IReadOnlyList<InfluenceAllocation> Sources { get; }
        public IReadOnlyList<InfluenceAllocation> Destinations { get; }
        public static InfluenceEventSelection Relocate(PlayerSide owner,long sourceVersion,IEnumerable<InfluenceAllocation> sources,IEnumerable<InfluenceAllocation> destinations)
            =>new InfluenceEventSelection(owner,sourceVersion,sources,destinations);
        private InfluenceEventSelection(PlayerSide owner,long sourceVersion,IEnumerable<InfluenceAllocation> sources,IEnumerable<InfluenceAllocation> destinations)
        {
            Owner=owner;SourceVersion=sourceVersion;Kind=InfluenceDecisionKind.Relocation;
            Sources=sources==null?null:Array.AsReadOnly(sources.Take(MaxTargets+1).ToArray());
            Destinations=destinations==null?null:Array.AsReadOnly(destinations.Take(MaxTargets+1).ToArray());
        }
        public InfluenceEventSelection(PlayerSide owner, long sourceVersion, IEnumerable<CountryId> targets)
        {
            Owner = owner; SourceVersion = sourceVersion;
            Kind = InfluenceDecisionKind.CountrySet;
            Targets = targets == null ? null : Array.AsReadOnly(targets.Take(MaxTargets + 1).ToArray());
        }
        private InfluenceEventSelection(PlayerSide owner, long sourceVersion, IEnumerable<InfluenceAllocation> allocations)
        {
            Owner = owner; SourceVersion = sourceVersion; Kind = InfluenceDecisionKind.Allocation;
            Allocations = allocations == null ? null : Array.AsReadOnly(allocations.Take(MaxTargets + 1).ToArray());
        }
        public static InfluenceEventSelection Allocate(PlayerSide owner, long sourceVersion, IEnumerable<InfluenceAllocation> allocations)
            => new InfluenceEventSelection(owner, sourceVersion, allocations);
    }

    public sealed class InfluenceEventQuery
    {
        public InfluenceEventStatus Status { get; }
        public InfluenceEventError Error { get; }
        public PlayerSide Owner { get; }
        public long SourceVersion { get; }
        public int RequiredTargets { get; }
        public IReadOnlyList<CountryId> LegalCountries { get; }
        public InfluenceDecisionKind Kind { get; }
        public int RequiredAmount { get; }
        public IReadOnlyList<InfluenceAllocation> Capacities { get; }
        public int MaximumAmount { get; }
        public IReadOnlyList<InfluenceAllocation> SourceCapacities { get; }=Array.AsReadOnly(Array.Empty<InfluenceAllocation>());
        public IReadOnlyList<InfluenceAllocation> DestinationCapacities { get; }=Array.AsReadOnly(Array.Empty<InfluenceAllocation>());
        internal InfluenceEventQuery(PlayerSide owner,long version,int maximum,IEnumerable<InfluenceAllocation> sources,IEnumerable<InfluenceAllocation> destinations)
        {
            Owner=owner;SourceVersion=version;MaximumAmount=maximum;Kind=InfluenceDecisionKind.Relocation;
            SourceCapacities=Array.AsReadOnly(sources.ToArray());DestinationCapacities=Array.AsReadOnly(destinations.ToArray());
            Status=SourceCapacities.Count==0?InfluenceEventStatus.Ready:InfluenceEventStatus.AwaitingChoice;
            LegalCountries=Array.AsReadOnly(SourceCapacities.Concat(DestinationCapacities).Select(a=>a.Country).Distinct().OrderBy(c=>c.Value,StringComparer.Ordinal).ToArray());
            Capacities=Array.AsReadOnly(Array.Empty<InfluenceAllocation>());
        }
        internal InfluenceEventQuery(InfluenceEventStatus status, InfluenceEventError error, PlayerSide owner, long version, int required, IEnumerable<CountryId> countries)
        {
            Status = status; Error = error; Owner = owner; SourceVersion = version; RequiredTargets = required;
            LegalCountries = Array.AsReadOnly(countries.ToArray());
            Kind = required > 0 ? InfluenceDecisionKind.CountrySet : InfluenceDecisionKind.None;
            Capacities = Array.AsReadOnly(Array.Empty<InfluenceAllocation>());
        }
        internal InfluenceEventQuery(PlayerSide owner, long version, int requiredAmount, IEnumerable<InfluenceAllocation> capacities)
        {
            Owner = owner; SourceVersion = version; RequiredAmount = requiredAmount;
            Status = requiredAmount > 0 ? InfluenceEventStatus.AwaitingChoice : InfluenceEventStatus.Ready;
            Kind = requiredAmount > 0 ? InfluenceDecisionKind.Allocation : InfluenceDecisionKind.None;
            Capacities = Array.AsReadOnly(capacities.ToArray());
            LegalCountries = Array.AsReadOnly(Capacities.Select(c => c.Country).ToArray());
        }
    }

    public sealed class InfluenceChanged
    {
        public CountryId Country { get; }
        public PlayerSide Side { get; }
        public int Before { get; }
        public int After { get; }
        public long Sequence { get; }
        internal InfluenceChanged(CountryId country, PlayerSide side, int before, int after, long sequence)
        { Country = country; Side = side; Before = before; After = after; Sequence = sequence; }
    }

    public sealed class InfluenceEventPlan
    {
        public InfluenceEventStatus Status { get; }
        public InfluenceEventError Error { get; }
        public string CardId { get; }
        public string EffectId { get; }
        public long SourceVersion { get; }
        public GameState NextState { get; }
        public IReadOnlyList<InfluenceChanged> Changes { get; }
        public bool Resolved => Status == InfluenceEventStatus.ResolvedChange || Status == InfluenceEventStatus.ResolvedNoChange;
        public long? ResolutionSequence => Resolved ? NextState.EventSequence : (long?)null;
        internal InfluenceEventPlan(InfluenceEventStatus status, InfluenceEventError error, InfluenceEffectDefinition definition, GameState source, GameState next = null, IEnumerable<InfluenceChanged> changes = null)
        {
            Status = status; Error = error; CardId = definition?.CardId; EffectId = definition?.EffectId; SourceVersion = source.Version;
            NextState = next; Changes = Array.AsReadOnly((changes ?? Enumerable.Empty<InfluenceChanged>()).ToArray());
        }
    }
}
