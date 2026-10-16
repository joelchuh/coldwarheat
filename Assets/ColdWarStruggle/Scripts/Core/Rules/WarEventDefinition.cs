using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum WarEventKind { Korean, ArabIsraeli, IndoPakistani }
    public sealed class WarPolicy
    {
        public int DieSides { get; }
        public int WinningResult { get; }
        public int NeighborPenalty { get; }
        public int MilitaryCredit { get; }
        public int VictoryVp { get; }
        public WarPolicy(int dieSides,int winningResult,int neighborPenalty,int militaryCredit,int victoryVp)
        {
            if(dieSides<2 || winningResult<1 || winningResult>dieSides || neighborPenalty<1 || militaryCredit<1 || victoryVp<0)throw new ArgumentOutOfRangeException("Invalid war policy.");
            DieSides=dieSides;WinningResult=winningResult;NeighborPenalty=neighborPenalty;MilitaryCredit=militaryCredit;VictoryVp=victoryVp;
        }
    }
    public sealed class WarEventDefinition
    {
        public CardId Card { get; }
        public string EffectId { get; }
        public WarEventKind Kind { get; }
        public IReadOnlyList<CountryId> Targets { get; }
        public WarEventDefinition(CardId card,string effectId,WarEventKind kind,IEnumerable<CountryId> targets)
        {
            if(targets==null)throw new ArgumentNullException(nameof(targets));
            var name=Name(kind);var copy=targets.ToArray();
            var expected=kind==WarEventKind.Korean?new[]{"country.south_korea"}:kind==WarEventKind.ArabIsraeli?new[]{"country.israel"}:new[]{"country.india","country.pakistan"};
            if(card.Value!="card."+name || effectId!="effect."+name || copy.Length!=expected.Length || !new HashSet<string>(copy.Select(t=>t.Value),StringComparer.Ordinal).SetEquals(expected))throw new ArgumentException("Unsupported war identity or targets.");
            Card=card;EffectId=effectId;Kind=kind;Targets=Array.AsReadOnly(copy);
        }
        internal static string Name(WarEventKind kind)=>kind switch {WarEventKind.Korean=>"korean_war",WarEventKind.ArabIsraeli=>"arab_israeli_war",WarEventKind.IndoPakistani=>"indo_pakistani_war",_=>throw new ArgumentException("Unknown war.")};
    }
    public sealed class WarEventCatalog
    {
        public MapDefinition Map { get; }
        public CardCatalog Cards { get; }
        public WarPolicy Policy { get; }
        public IReadOnlyDictionary<CardId,WarEventDefinition> Effects { get; }
        public WarEventCatalog(MapDefinition map,CardCatalog cards,WarPolicy policy,IEnumerable<WarEventDefinition> effects)
        {
            if(map==null || cards==null || policy==null || effects==null)throw new ArgumentNullException();
            var copy=effects.ToArray();if(copy.Length==0 || copy.Any(e=>e==null) || copy.Select(e=>e.Card).Distinct().Count()!=copy.Length)throw new ArgumentException("Missing or duplicate war definitions.");
            foreach(var e in copy)
            {
                if(!cards.Contains(e.Card) || e.Targets.Any(t=>!map.Contains(t)))throw new ArgumentException("Unknown war card or target.");
                var c=cards.GetCard(e.Card);var name=WarEventDefinition.Name(e.Kind);
                if(c.Kind!=CardKind.Event || c.Affiliation!=(e.Kind==WarEventKind.IndoPakistani?CardAffiliation.Neutral:CardAffiliation.USSR)
                    || c.RemoveAfterResolvedEvent!=(e.Kind==WarEventKind.Korean) || c.Effect.EffectId!=e.EffectId || c.Effect.ContractVersion!="0.1.0"
                    || c.Effect.Kind!=CardParameterBindingKind.Bound || c.Effect.Parameters.Count!=1 || c.Effect.Parameters[0].DatasetId!="effects.deluxe2015.war_events_batch1"
                    || c.Effect.Parameters[0].JsonPointer!="/parameters/"+name)throw new ArgumentException("Printed card does not bind this war.");
            }
            Map=map;Cards=cards;Policy=policy;Effects=new ReadOnlyDictionary<CardId,WarEventDefinition>(copy.ToDictionary(e=>e.Card));
        }
    }
}
