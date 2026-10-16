using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class PublicWarView
    {
        public CardId Card { get; }
        public PlayerSide Owner { get; }
        public string DecisionId { get; }
        public bool CanChoose { get; }
        public IReadOnlyList<CountryId> Candidates { get; }
        internal PublicWarView(GameState state,PlayerSide viewer)
        {
            var choice=state.Practice.War;Card=choice.Effect.Card;Owner=choice.Owner;DecisionId=choice.Id;
            CanChoose=ActionPracticeRules.CanResolveWar(state,viewer)==RuleError.None;
            Candidates=Array.AsReadOnly(choice.Candidates.ToArray()); // Country targets are public, unlike hand-discard choices.
        }
    }
    public sealed class PublicWarResult
    {
        public CardId Card { get; }
        public CountryId Target { get; }
        public PlayerSide Beneficiary { get; }
        public int Roll { get; }
        public int Penalty { get; }
        public int AdjustedRoll { get; }
        public int WinningResult { get; }
        public bool Success { get; }
        public IReadOnlyList<CountryId> ControlledNeighbors { get; }
        public bool OpponentSuperpowerAdjacent { get; }
        public bool TargetControlPenalty { get; }
        public CountryState Before { get; }
        public CountryState After { get; }
        public int Captured { get; }
        public int MilitaryCredit { get; }
        public int MilitaryBefore { get; }
        public int MilitaryAfter { get; }
        public int VpBefore { get; }
        public int VpAfter { get; }
        public bool Terminal { get; }
        internal PublicWarResult(WarEventPlan plan)
        {
            // This closed practice runtime only projects allowed, resolved wars. Prevention needs a separate future outcome.
            var c=plan?.Calculation??throw new ArgumentException("A resolved war calculation is required.");
            Card=c.Effect.Card;Target=c.Target;Beneficiary=c.Beneficiary;Roll=c.Roll;Penalty=c.Penalty;AdjustedRoll=c.AdjustedRoll;
            WinningResult=plan.Before.Catalog.Policy.WinningResult;Success=c.Success;ControlledNeighbors=Array.AsReadOnly(c.ControlledNeighbors.ToArray());
            OpponentSuperpowerAdjacent=c.OpponentSuperpowerAdjacent;TargetControlPenalty=c.TargetControlPenalty;Before=c.Before;After=c.After;Captured=c.Captured;
            MilitaryCredit=plan.Credit.Amount;MilitaryBefore=Beneficiary==PlayerSide.US?plan.Before.Military.UsEarned:plan.Before.Military.UssrEarned;
            MilitaryAfter=Beneficiary==PlayerSide.US?plan.After.Military.UsEarned:plan.After.Military.UssrEarned;VpBefore=plan.Before.VpForUS;VpAfter=plan.After.VpForUS;
            Terminal=plan.Decision.Outcome==VictoryOutcome.Finished;
        }
    }
}
