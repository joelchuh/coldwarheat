using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class BlockadeDiscardOption
    {
        public CardId Card { get; }
        public int EffectiveOps { get; }
        internal BlockadeDiscardOption(OpsGeneralQuote quote){Card=quote.Card.Id;EffectiveOps=quote.GeneralValue;}
    }
    public sealed class PublicBlockadeView
    {
        public PlayerSide Owner { get; }
        public CardId Card { get; }
        public CountryId FallbackCountry { get; }
        public string DecisionId { get; }
        public int MinimumEffectiveOps { get; }
        public bool CanChoose { get; }
        public IReadOnlyList<BlockadeDiscardOption> Candidates { get; }
        internal PublicBlockadeView(GameState state,PlayerSide viewer)
        {
            var choice=state.Practice.Blockade;Owner=choice.Owner;Card=choice.Definition.Card.Id;FallbackCountry=choice.Definition.FallbackCountry;
            DecisionId=choice.Id;MinimumEffectiveOps=choice.Definition.MinimumEffectiveOps;
            CanChoose=ActionPracticeRules.CanRespondToBlockade(state,viewer)==RuleError.None;
            Candidates=Array.AsReadOnly(CanChoose?choice.Candidates.Select(q=>new BlockadeDiscardOption(q)).ToArray():Array.Empty<BlockadeDiscardOption>());
        }
    }
    public sealed class PublicBlockadeResult
    {
        public CardId Card { get; }
        public PlayerSide Owner { get; }
        public CountryId FallbackCountry { get; }
        public string DecisionId { get; }
        public BlockadeOutcome Outcome { get; }
        public CardId? DiscardedCard { get; }
        public IReadOnlyList<InfluenceChanged> Changes { get; }
        internal PublicBlockadeResult(BlockadeResolutionResult result)
        {
            Card=result.Card;Owner=result.Owner;FallbackCountry=result.FallbackCountry;DecisionId=result.DecisionId;
            Outcome=result.Outcome;DiscardedCard=result.DiscardedCard;Changes=Array.AsReadOnly(result.Changes.ToArray());
        }
    }
}
