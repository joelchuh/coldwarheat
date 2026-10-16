using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class PublicSpaceEndTurnDiscard
    {
        public PlayerSide Owner { get; }
        public SpaceEndTurnDiscardStatus Status { get; }
        public bool CanChoose { get; }
        public IReadOnlyList<CardId> Candidates { get; }
        public CardId? DiscardedCard { get; }
        public string DecisionId { get; }
        internal PublicSpaceEndTurnDiscard(GameState state,PlayerSide viewer)
        {
            var choice=state.Practice.SpaceDiscard;Owner=choice.Owner;Status=choice.Status;DecisionId=choice.DecisionId;
            CanChoose=ActionPracticeRules.CanResolveSpaceDiscard(state,viewer)==RuleError.None;
            Candidates=Array.AsReadOnly(CanChoose?choice.Candidates.ToArray():Array.Empty<CardId>());
            DiscardedCard=choice.Status==SpaceEndTurnDiscardStatus.Discarded?choice.DiscardedCard:null;
        }
    }
}
