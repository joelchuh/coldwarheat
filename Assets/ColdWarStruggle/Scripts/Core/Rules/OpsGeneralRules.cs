using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum OpsValuePurpose { Influence, Coup, Realignment, Space, OpsThreshold }
    internal static class OpsGeneralGuard
    {
        internal static void Id(string value)
        { if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value != value.Trim()) throw new ArgumentException("OPS identifier must be nonempty, trimmed and bounded."); }
        internal static void Side(PlayerSide side)
        { if (side != PlayerSide.US && side != PlayerSide.USSR) throw new ArgumentOutOfRangeException(nameof(side)); }
    }
    public sealed class OpsGeneralDefinition
    {
        public int Minimum { get; }
        public int OrdinaryMaximum { get; }
        public IReadOnlyList<OpsValuePurpose> FlatPurposes { get; }
        public OpsGeneralDefinition(int minimum,int ordinaryMaximum,IEnumerable<OpsValuePurpose> flatPurposes)
        {
            if (minimum < 1 || ordinaryMaximum < minimum) throw new ArgumentException("Invalid general OPS bounds.");
            if (flatPurposes == null) throw new ArgumentNullException(nameof(flatPurposes));
            var copy = flatPurposes.Take(6).ToArray();
            if (copy.Length == 0 || copy.Length > 5 || copy.Distinct().Count() != copy.Length || copy.Any(p => !Enum.IsDefined(typeof(OpsValuePurpose), p)))
                throw new ArgumentException("Invalid general OPS purposes.");
            Minimum=minimum;OrdinaryMaximum=ordinaryMaximum;FlatPurposes=Array.AsReadOnly(copy);
        }
    }
    // A trusted contribution; activation, expiry and same-effect repeat policy belong to the effect layer.
    public sealed class OpsFlatModifier
    {
        public string Id { get; }
        public PlayerSide AffectedSide { get; }
        public int Delta { get; }
        public OpsFlatModifier(string id,PlayerSide affectedSide,int delta)
        { OpsGeneralGuard.Id(id);OpsGeneralGuard.Side(affectedSide);Id=id;AffectedSide=affectedSide;Delta=delta; }
    }
    public sealed class OpsGeneralQuote
    {
        public OpsGeneralDefinition Definition { get; }
        public CardDefinition Card { get; }
        public string AsIfSourceRuleId { get; }
        public PlayerSide Actor { get; }
        public OpsValuePurpose Purpose { get; }
        public int BaseValue { get; }
        public long ModifierTotal { get; }
        public long RawValue { get; }
        public int GeneralValue { get; }
        public IReadOnlyList<OpsFlatModifier> AppliedModifiers { get; }
        internal OpsGeneralQuote(OpsGeneralDefinition definition,CardDefinition card,string source,PlayerSide actor,OpsValuePurpose purpose,int basis,OpsFlatModifier[] applied)
        {
            Definition=definition;Card=card;AsIfSourceRuleId=source;Actor=actor;Purpose=purpose;BaseValue=basis;
            AppliedModifiers=Array.AsReadOnly(applied);
            ModifierTotal=applied.Sum(m => (long)m.Delta);RawValue=(long)basis+ModifierTotal;
            GeneralValue=(int)Math.Max(definition.Minimum,Math.Min(definition.OrdinaryMaximum,RawValue));
        }
    }
    public static class OpsGeneralRules
    {
        // General only: do not spend this as a complete regional budget for map operations.
        public static OpsGeneralQuote QuoteCard(OpsGeneralDefinition definition,CardDefinition card,PlayerSide actor,OpsValuePurpose purpose,IEnumerable<OpsFlatModifier> modifiers)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (card.Kind == CardKind.Scoring || !card.PrintedOps.HasValue) throw new ArgumentException("Scoring cards have no OPS value.");
            return Quote(definition,card,null,card.PrintedOps.Value,actor,purpose,modifiers);
        }
        public static OpsGeneralQuote QuoteAsIfCard(OpsGeneralDefinition definition,string sourceRuleId,int verifiedBaseOps,PlayerSide actor,OpsValuePurpose purpose,IEnumerable<OpsFlatModifier> modifiers)
        {
            OpsGeneralGuard.Id(sourceRuleId);
            if (verifiedBaseOps < 1 || (purpose != OpsValuePurpose.Influence && purpose != OpsValuePurpose.Coup && purpose != OpsValuePurpose.Realignment))
                throw new ArgumentException("Verified as-if OPS require a positive value and an ordinary map-operation purpose.");
            return Quote(definition,null,sourceRuleId,verifiedBaseOps,actor,purpose,modifiers);
        }
        static OpsGeneralQuote Quote(OpsGeneralDefinition definition,CardDefinition card,string source,int basis,PlayerSide actor,OpsValuePurpose purpose,IEnumerable<OpsFlatModifier> modifiers)
        {
            if (definition == null || modifiers == null) throw new ArgumentNullException();
            OpsGeneralGuard.Side(actor);
            if (!definition.FlatPurposes.Contains(purpose)) throw new ArgumentException("Unsupported general OPS purpose.");
            var copy=modifiers.Take(129).ToArray();
            if (copy.Length > 128 || copy.Any(m=>m==null) || copy.Select(m=>m.Id).Distinct(StringComparer.Ordinal).Count()!=copy.Length)
                throw new ArgumentException("Invalid or duplicate OPS contributions.");
            var applied=copy.Where(m=>m.AffectedSide==actor).OrderBy(m=>m.Id,StringComparer.Ordinal).ToArray();
            return new OpsGeneralQuote(definition,card,source,actor,purpose,basis,applied);
        }
    }
}
