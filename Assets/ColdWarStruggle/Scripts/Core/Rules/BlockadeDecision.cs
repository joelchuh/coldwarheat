using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum BlockadeOutcome { AwaitingChoice, Discarded, InfluenceRemoved }
    public sealed class BlockadeDecision
    {
        public BlockadeDefinition Definition { get; }
        public PlayerSide Owner => Definition.Owner;
        public string Id { get; }
        public string GameId { get; }
        public long ExpectedVersion { get; }
        // Authority-only candidates. Player projection must filter by Owner.
        public IReadOnlyList<OpsGeneralQuote> Candidates { get; }
        internal CardZones Cards { get; }
        internal TurnOpsEffectState OpsEffects { get; }
        internal GameTurnContext Turn { get; }
        internal BlockadeDecision(BlockadeDefinition definition,CardZones cards,TurnOpsEffectState effects,GameTurnContext turn,string id,string game,long version)
        {
            if(definition==null || cards==null || effects==null || turn==null)throw new ArgumentNullException();
            if(!ReferenceEquals(cards.Catalog,definition.Cards) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(game) || version<0)
                throw new ArgumentException("Invalid blockade context.");
            Candidates=HandDiscardRules.QualifyingCards(cards,definition.Owner,definition.Ops,definition.MinimumEffectiveOps,effects.GetModifiers(turn));
            Definition=definition;Cards=cards;OpsEffects=effects;Turn=turn;Id=id;GameId=game;ExpectedVersion=version;
        }
    }
    public sealed class BlockadeResolutionResult
    {
        public CardId Card { get; }
        public CountryId FallbackCountry { get; }
        public PlayerSide Owner { get; }
        public string DecisionId { get; }
        public BlockadeOutcome Outcome { get; }
        public CardId? DiscardedCard { get; }
        public IReadOnlyList<InfluenceChanged> Changes { get; }
        internal BlockadeResolutionResult(BlockadeDecision choice,BlockadeOutcome outcome,CardId? discarded=null,IReadOnlyList<InfluenceChanged> changes=null)
        {
            Card=choice.Definition.Card.Id;FallbackCountry=choice.Definition.FallbackCountry;Owner=choice.Owner;DecisionId=choice.Id;Outcome=outcome;DiscardedCard=discarded;
            Changes=Array.AsReadOnly((changes??Array.Empty<InfluenceChanged>()).ToArray());
        }
    }
}
