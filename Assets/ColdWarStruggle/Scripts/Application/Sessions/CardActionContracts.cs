using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class CardActionCommand : SessionCommand
    {
        public CardActionKind Kind { get; }
        public CardId? Card { get; }
        public CountryId? Target { get; }
        public CardActionCommand(string id,string game,long version,CardActionKind kind,CardId? card=null,CountryId? target=null):base(id,game,version)
        { Kind=kind;Card=card;Target=target; }
        internal override bool SameContent(SessionCommand other)=>other is CardActionCommand c && c.GameId==GameId && c.ExpectedVersion==ExpectedVersion && c.Kind==Kind && Nullable.Equals(c.Card,Card) && Nullable.Equals(c.Target,Target);
    }
    public sealed class PublicTurnOpsEffect
    {
        public CardId Card { get; }
        public string EffectId { get; }
        public PlayerSide AffectedSide { get; }
        public int Delta { get; }
        public int Turn { get; }
        public string ExpiryBoundary { get; }
        internal PublicTurnOpsEffect(TurnOpsEffectInstance instance) { Card=instance.Definition.Card;EffectId=instance.Definition.EffectId;AffectedSide=instance.AffectedSide;Delta=instance.Definition.Delta;Turn=instance.Turn;ExpiryBoundary=instance.ExpiryBoundary; }
    }
    public sealed class PublicOpponentOpsProgress
    {
        public CardId Card { get; }
        public PlayerSide Actor { get; }
        public PlayerSide EventSide { get; }
        public OpponentEventOrder Order { get; }
        public CardResolutionStep CurrentStep { get; }
        public int CompletedSteps { get; }
        public bool EventOccurred { get; }
        public OperationsMode? Mode { get; }
        internal PublicOpponentOpsProgress(OpponentOpsSequence value) { Mode=value.Mode;Card=value.Card;Actor=value.Actor;EventSide=value.EventSide;Order=value.Order;CurrentStep=value.CurrentStep;CompletedSteps=value.CompletedSteps;EventOccurred=value.EventOccurred; }
    }
    public sealed class PublicPracticeView
    {
        public ActionPracticeStage Stage { get; }
        public PlayerSide Actor { get; }
        public int Round { get; }
        public int RoundLimit { get; }
        public int CompletedActions { get; }
        public CardId? ActiveCard { get; }
        public int UsSpace { get; }
        public int UssrSpace { get; }
        public int UsAttempts { get; }
        public int UssrAttempts { get; }
        public int VpForUS { get; }
        public int? Defcon { get; }
        public int? UsMilitary { get; }
        public int? UssrMilitary { get; }
        public PlayerSide? Winner { get; }
        public VictoryCheckpoint? EndReason { get; }
        public bool HasScoring { get; }
        public bool TurnEndEnabled { get; }
        public bool NextTurnEnabled { get; }
        public bool NextTurnAvailable { get; }
        public bool TurnEndAvailable { get; }
        public PublicTurnEnd TurnEnd { get; }
        public PublicOpponentOpsProgress OpponentOps { get; }
        public IReadOnlyList<PublicTurnOpsEffect> OpsEffects { get; }
        internal PublicPracticeView(ActionPracticeState s,bool turnEndAvailable=false,bool nextTurnAvailable=false)
        { NextTurnEnabled=s.Definition.NextTurnEnabled;NextTurnAvailable=nextTurnAvailable;TurnEndEnabled=s.Definition.TurnEndEnabled;TurnEndAvailable=turnEndAvailable;TurnEnd=s.TurnEnd==null?null:new PublicTurnEnd(s.TurnEnd);OpponentOps=s.OpponentOps==null?null:new PublicOpponentOpsProgress(s.OpponentOps);OpsEffects=Array.AsReadOnly(s.OpsEffects.Instances.Select(i=>new PublicTurnOpsEffect(i)).ToArray());Stage=s.Stage;Actor=s.Actor;Round=s.ActionRound;RoundLimit=s.Space.Turn.Parameters.NormalActionsPerSide;CompletedActions=s.CompletedActions;ActiveCard=s.ActiveCard;
          UsSpace=s.Space.Track.US;UssrSpace=s.Space.Track.USSR;UsAttempts=s.Space.UsAttempts;UssrAttempts=s.Space.UssrAttempts;VpForUS=s.VpForUS;Defcon=s.Risk?.Defcon.Level;UsMilitary=s.Risk?.Military.UsEarned;UssrMilitary=s.Risk?.Military.UssrEarned;Winner=s.Risk?.Result?.Winner;EndReason=s.Risk?.Result?.Checkpoint;HasScoring=s.Definition.Scoring!=null; }
    }
    public sealed class PublicSpaceResult
    {
        public int Roll { get; }
        public bool Success { get; }
        public int From { get; }
        public int To { get; }
        public int VpForUS { get; }
        internal PublicSpaceResult(SpaceAttemptReceipt r) { Roll=r.Roll;Success=r.Success;From=r.FromPosition;To=r.ToPosition;VpForUS=r.NetVpForUS; }
    }
    public sealed class PublicCoupResult
    {
        public CountryId Target { get; }
        public int Roll { get; }
        public int Strength { get; }
        public long Margin { get; }
        public int Removed { get; }
        public int Added { get; }
        public int MilitaryCredit { get; }
        public int? DefconFrom { get; }
        public int? DefconTo { get; }
        public bool NuclearWar { get; }
        internal PublicCoupResult(CoupAttemptReceipt r)
        { Target=r.Request.Target;Roll=r.Calculation.Roll;Strength=r.Calculation.Strength;Margin=r.Calculation.Margin;Removed=r.Calculation.Removed;Added=r.Calculation.Added;
          MilitaryCredit=r.MilitaryCredit.Amount;DefconFrom=r.DefconChange?.Before.Level;DefconTo=r.DefconChange?.After.Level;NuclearWar=r.DefconChange?.NuclearWar==true; }
    }
    public sealed class PublicCardEventResult
    {
        public CardId Card { get; }
        public InfluenceEventStage Stage { get; }
        public InfluenceEventStatus? Outcome { get; }
        public IReadOnlyList<InfluenceChanged> Changes { get; }=Array.AsReadOnly(Array.Empty<InfluenceChanged>());
        internal PublicCardEventResult(InfluenceEventTransition transition)
        { var e=transition.NextState.InfluenceEvent;Card=new CardId(e.Definition.CardId);Stage=e.Stage;Outcome=e.Outcome;Changes=Array.AsReadOnly(transition.Changes.ToArray()); }
    }
    public sealed class PublicDefconVpEventResult
    {
        public CardId Card { get; }
        public int DefconFrom { get; }
        public int DefconTo { get; }
        public int VpForUS { get; }
        public PlayerSide AwardSide { get; }
        public int? Award { get; }
        public bool Terminal { get; }
        public IReadOnlyList<DefconVpStep> Steps { get; }
        internal PublicDefconVpEventResult(DefconVpEventPlan p)
        { Card=p.Effect.Card;DefconFrom=p.Before.Level;DefconTo=p.After.Level;VpForUS=checked(p.Decision.SignedVp-p.VpBefore);AwardSide=p.AwardSide;Award=p.Award;Terminal=p.Decision.Outcome==VictoryOutcome.Finished;Steps=Array.AsReadOnly(p.Steps.ToArray()); }
    }
    public sealed class PublicRealignmentResult
    {
        public CountryId Target { get; }
        public int UsRoll { get; }
        public int UssrRoll { get; }
        public RealignmentModifiers US { get; }
        public RealignmentModifiers USSR { get; }
        public long UsTotal { get; }
        public long UssrTotal { get; }
        public PlayerSide? RemovedSide { get; }
        public int Removed { get; }
        internal PublicRealignmentResult(RealignmentCalculation value) { Target=value.Target;UsRoll=value.UsRoll;UssrRoll=value.UssrRoll;US=value.US;USSR=value.USSR;UsTotal=value.UsTotal;UssrTotal=value.UssrTotal;RemovedSide=value.RemovedSide;Removed=value.Removed; }
    }
    public sealed class CardActionResult
    {
        public RuleError Error { get; }
        public bool Accepted=>Error==RuleError.None;
        public PlayerViewSnapshot View { get; }
        public PublicSpaceResult Space { get; }
        public PublicCoupResult Coup { get; }
        public PublicRegionScore Scoring { get; }
        public PublicCardEventResult Event { get; }
        public PublicDefconVpEventResult DefconVpEvent { get; }
        public PublicTurnOpsEffect ActivatedOpsEffect { get; }
        public PublicRealignmentResult Realignment { get; }
        internal CardActionResult(RuleError error,PlayerViewSnapshot view,SpaceAttemptReceipt receipt=null,CoupAttemptReceipt coup=null,RegionScoringResult scoring=null,InfluenceEventTransition eventTransition=null,DefconVpEventPlan defconVpEvent=null,TurnOpsEffectInstance activatedOpsEffect=null,RealignmentCalculation realignment=null)
        { Realignment=realignment==null?null:new PublicRealignmentResult(realignment);ActivatedOpsEffect=activatedOpsEffect==null?null:new PublicTurnOpsEffect(activatedOpsEffect);DefconVpEvent=defconVpEvent==null?null:new PublicDefconVpEventResult(defconVpEvent);Event=eventTransition==null?null:new PublicCardEventResult(eventTransition);Scoring=scoring==null?null:new PublicRegionScore(scoring);Error=error;View=view;Space=receipt==null?null:new PublicSpaceResult(receipt);Coup=coup==null?null:new PublicCoupResult(coup); }
    }
    public sealed class CardActionOption
    {
        public int? PrintedOps { get; }
        public int? RegionalMaximum { get; }
        public int? CurrentOps { get; }
        public long? FlatModifierTotal { get; }
        public CardId Card { get; }
        public RuleError Influence { get; }
        public RuleError Space { get; }
        public RuleError Coup { get; }
        public RuleError Scoring { get; }
        public RuleError Event { get; }=RuleError.UnsupportedCardAction;
        public RuleError Realignment { get; }=RuleError.UnsupportedCardAction;
        public RuleError OpponentEventFirst { get; }=RuleError.UnsupportedCardAction;
        public RuleError OpponentOpsFirst { get; }=RuleError.UnsupportedCardAction;
        public RuleError EventThenInfluence { get; }=RuleError.UnsupportedCardAction;
        public RuleError InfluenceThenEvent { get; }=RuleError.UnsupportedCardAction;
        internal CardActionOption(GameState s,PlayerSide side,CardId id)
        {
            if(s.Practice!=null)
            {
                var printed=s.Practice.Definition.Cards.GetCard(id);PrintedOps=printed.PrintedOps;
                if(printed.PrintedOps.HasValue)
                { var quote=ActionPracticeRules.QuoteCard(s,side,id,OpsValuePurpose.Influence);CurrentOps=quote.GeneralValue;FlatModifierTotal=quote.ModifierTotal;if(s.Practice.Definition.China is ChinaCardDefinition china && id.Equals(china.Card))RegionalMaximum=new OpsRegionalBudget(s.Map,quote,new[]{china.Bonus}).EffectiveBudget; }
            }
            OpponentEventFirst=ActionPracticeRules.CanPlay(s,side,id,CardActionKind.OpponentEventFirst);OpponentOpsFirst=ActionPracticeRules.CanPlay(s,side,id,CardActionKind.OpponentOpsFirst);
            EventThenInfluence=ActionPracticeRules.CanPlay(s,side,id,CardActionKind.EventThenInfluence);InfluenceThenEvent=ActionPracticeRules.CanPlay(s,side,id,CardActionKind.InfluenceThenEvent);Realignment=ActionPracticeRules.CanPlay(s,side,id,CardActionKind.Realignment);Event=ActionPracticeRules.CanPlay(s,side,id,CardActionKind.Event);Card=id;Influence=ActionPracticeRules.CanPlay(s,side,id,CardActionKind.Influence);Space=ActionPracticeRules.CanPlay(s,side,id,CardActionKind.Space);Coup=ActionPracticeRules.CanPlay(s,side,id,CardActionKind.Coup);Scoring=ActionPracticeRules.CanPlay(s,side,id,CardActionKind.Scoring); }
    }
    public sealed class PendingOperationsOptions
    {
        public CardId Card { get; }
        public int? CurrentOps { get; }
        public RuleError Influence { get; }
        public RuleError Coup { get; }
        public RuleError Realignment { get; }
        internal PendingOperationsOptions(GameState state,PlayerSide side)
        {
            Card=state.Practice.ActiveCard.Value;
            Influence=ActionPracticeRules.CanChooseOpponentOperations(state,side,OperationsMode.Influence);
            Coup=ActionPracticeRules.CanChooseOpponentOperations(state,side,OperationsMode.Coup);
            Realignment=ActionPracticeRules.CanChooseOpponentOperations(state,side,OperationsMode.Realignment);
            if(Influence==RuleError.None)CurrentOps=ActionPracticeRules.QuoteCard(state,side,Card,OpsValuePurpose.Influence).GeneralValue;
        }
    }
    public sealed class PublicCoupQuote
    {
        public RuleError Error { get; }
        public int? Strength { get; }
        public int? MilitaryCredit => Strength;
        internal PublicCoupQuote(RuleError error,int? strength=null) { Error=error;Strength=strength; }
    }
    public sealed class CardActionOptions
    {
        public string GameId { get; }
        public long Version { get; }
        public IReadOnlyList<CardActionOption> Cards { get; }
        public CardActionOption China { get; }
        public CardActionOption Find(CardId card)=>China!=null && China.Card.Equals(card)?China:Cards.FirstOrDefault(c=>c.Card.Equals(card));
        public PendingOperationsOptions PendingOperations { get; }
        internal CardActionOptions(GameState s,PlayerSide side)
        { China=s.Practice?.Definition.China==null?null:new CardActionOption(s,side,s.Cards.China.Id);PendingOperations=s.Practice?.Stage==ActionPracticeStage.ChoosingOperations?new PendingOperationsOptions(s,side):null;GameId=s.GameId;Version=s.Version;Cards=Array.AsReadOnly(s.Cards==null?Array.Empty<CardActionOption>():s.Cards.GetHand(side).Select(id=>new CardActionOption(s,side,id)).ToArray()); }
    }
}
