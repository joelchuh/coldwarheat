using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    // Authority-only result; never serialize the contained GameState to a player.
    public sealed class HeadlineResolutionStep
    {
        public CardId Card { get; }
        public PlayerSide PlayedBy { get; }
        public CardActionTransition Result { get; }
        internal HeadlineResolutionStep(CardId card,PlayerSide player,CardActionTransition result){Card=card;PlayedBy=player;Result=result;}
    }
    public sealed class HeadlineResolutionTransition
    {
        public RuleError Error { get; }
        public bool Accepted=>Error==RuleError.None;
        public GameState NextState { get; }
        public IReadOnlyList<HeadlineResolutionStep> Steps { get; }
        internal HeadlineResolutionTransition(RuleError error,GameState next=null,IReadOnlyList<HeadlineResolutionStep> steps=null){Error=error;NextState=next;Steps=Array.AsReadOnly((steps??Array.Empty<HeadlineResolutionStep>()).ToArray());}
    }
    public static class HeadlineResolutionRules
    {
        public static IReadOnlyList<CardId> SupportedCards(ActionPracticeDefinition definition)
        {
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            var supported=definition.Cards.Cards.Where(c=>c.Kind==CardKind.Scoring && definition.Scoring!=null
                || c.Kind==CardKind.Event && (definition.InfluenceEvents?.Effects.ContainsKey(c.Id)==true
                    || definition.DefconVpEvents?.Effects.ContainsKey(c.Id)==true || definition.TurnOpsEvents?.Effects.ContainsKey(c.Id)==true));
            return Array.AsReadOnly(supported.Select(c=>c.Id).OrderBy(id=>id.Value,StringComparer.Ordinal).ToArray());
        }
        static RuleError CheckLive(GameState state)
        {
            if(state==null)return RuleError.InvalidCommand;
            if(state.Ended || state.Practice?.Risk?.Result?.Outcome==VictoryOutcome.Finished)return RuleError.GameAlreadyEnded;
            if(state.Practice==null || state.Cards==null)return RuleError.ActionNotReady;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            return RuleError.None;
        }
        public static HeadlineResolutionTransition Begin(GameState state,HeadlineOrderParameters parameters)
        {
            var error=CheckLive(state);if(error!=RuleError.None)return new HeadlineResolutionTransition(error);
            if(parameters==null)return new HeadlineResolutionTransition(RuleError.InvalidDefinition);
            var p=state.Practice;
            if(p.Stage!=ActionPracticeStage.ChooseCard || p.CompletedActions!=0 || p.ActiveCard.HasValue || p.Headline!=null
                || p.OpponentOps!=null || p.TurnEnd!=null || state.InfluenceEvent!=null || state.Cards.HeadlineReserved.Count!=2
                || state.Cards.ResolvingCards.Count!=0 || state.Operations.Budget!=0 || state.SpentOps!=0)return new HeadlineResolutionTransition(RuleError.ActionNotReady);
            var supported=SupportedCards(p.Definition);
            if(state.Cards.HeadlineReserved.Values.Any(id=>!supported.Contains(id)))return new HeadlineResolutionTransition(RuleError.UnsupportedCardAction);
            try
            {
                var opened=ActionPracticeRules.OpenHeadlines(state,parameters);
                return Advance(opened.NextState);
            }
            catch(OverflowException){return new HeadlineResolutionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new HeadlineResolutionTransition(RuleError.InvalidDefinition);}
        }
        public static HeadlineResolutionTransition Advance(GameState state)
        {
            var error=CheckLive(state);if(error!=RuleError.None)return new HeadlineResolutionTransition(error);
            if(!state.Practice.HasPendingHeadline || state.Practice.Stage!=ActionPracticeStage.HeadlineReady || state.InfluenceEvent!=null)
                return new HeadlineResolutionTransition(RuleError.ActionNotReady);
            var steps=new List<HeadlineResolutionStep>();var current=state;
            try
            {
                while(current.Practice.HasPendingHeadline)
                {
                    var queue=current.Practice.Headline;var entry=queue.Current;
                    if(current.Practice.Stage!=ActionPracticeStage.HeadlineReady || current.Cards.ResolvingCards.Count!=0
                        || current.Cards.HeadlineReserved.Count!=queue.Order.Count-queue.CompletedCount
                        || current.Cards.GetHeadline(entry.PlayedBy)?.Equals(entry.Card)!=true)return new HeadlineResolutionTransition(RuleError.InvalidDefinition);
                    if(!SupportedCards(current.Practice.Definition).Contains(entry.Card))return new HeadlineResolutionTransition(RuleError.UnsupportedCardAction);
                    var result=ActionPracticeRules.ResolveCurrentHeadline(current);
                    if(!result.Accepted)return new HeadlineResolutionTransition(result.Error);
                    steps.Add(new HeadlineResolutionStep(entry.Card,entry.PlayedBy,result));current=result.NextState;
                    if(current.Ended || current.InfluenceEvent?.Stage==InfluenceEventStage.AwaitingChoice)
                        return new HeadlineResolutionTransition(RuleError.None,current,steps);
                    if(current.Practice.Headline==null || current.Practice.Headline.CompletedCount!=queue.CompletedCount+1)
                        return new HeadlineResolutionTransition(RuleError.InvalidDefinition);
                }
                return new HeadlineResolutionTransition(RuleError.None,current,steps);
            }
            catch(OverflowException){return new HeadlineResolutionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new HeadlineResolutionTransition(RuleError.InvalidDefinition);}
        }
    }
}
