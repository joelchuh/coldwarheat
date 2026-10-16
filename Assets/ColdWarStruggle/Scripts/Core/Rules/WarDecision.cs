using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    // Authority-only continuation; sessions project a separate public DTO.
    public sealed class WarDecision
    {
        public WarEventCatalog Catalog { get; }
        public WarEventDefinition Effect { get; }
        public PlayerSide Owner { get; }
        public PlayerSide RootActor { get; }
        public string Id { get; }
        public string GameId { get; }
        public long ExpectedVersion { get; }
        public IReadOnlyList<CountryId> Candidates { get; }
        internal ActionPracticeState Root { get; }
        internal CardZones Cards { get; }
        internal WarEventState Before { get; }
        internal WarDecision(GameState state,CardZones cards,WarEventDefinition effect,WarEventState before,string id)
        {
            if(state?.Practice==null || cards==null || effect==null || before==null || string.IsNullOrWhiteSpace(id)
                || effect.Kind!=WarEventKind.IndoPakistani || !ReferenceEquals(state.Practice.Definition.Wars,before.Catalog)
                || !ReferenceEquals(before.Catalog.Cards,cards.Catalog) || cards.ResolvingCards.Count!=1 || !cards.ResolvingCards.Contains(effect.Card))throw new ArgumentException("Invalid pending war context.");
            Root=state.Practice;Cards=cards;Before=before;Catalog=before.Catalog;Effect=effect;
            Owner=RootActor=Root.Actor;Id=id;GameId=state.GameId;ExpectedVersion=checked(state.Version+1);Candidates=Array.AsReadOnly(effect.Targets.ToArray());
        }
        internal bool Matches(GameState state)
        {
            var p=state.Practice;
            return state.GameId==GameId && state.Version==ExpectedVersion && ReferenceEquals(state.Map,Catalog.Map)
                && ReferenceEquals(state.Cards,Cards) && ReferenceEquals(state.RandomState,Before.Random)
                && ReferenceEquals(p.Definition,Root.Definition) && ReferenceEquals(p.Risk,Root.Risk) && p.VpForUS==Root.VpForUS
                && ReferenceEquals(p.Space,Root.Space) && ReferenceEquals(p.OpsEffects,Root.OpsEffects) && ReferenceEquals(p.Rounds,Root.Rounds)
                && ReferenceEquals(p.OpponentOps,Root.OpponentOps) && ReferenceEquals(p.Headline,Root.Headline) && p.Actor==RootActor
                && state.Turn.Number==Root.Space.Turn.Number && state.Turn.Era==Root.Space.Turn.Era && ReferenceEquals(state.Turn.Parameters,Root.Space.Turn.Parameters)
                && state.Countries.Count==Before.Countries.Count && state.Countries.All(c=>Before.Countries.TryGetValue(c.Key,out var b) && c.Value.US==b.US && c.Value.USSR==b.USSR);
        }
    }
}
