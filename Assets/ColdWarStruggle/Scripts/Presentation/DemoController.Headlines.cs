using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        public bool CanSelectHandCard(CardId card)
        {
            var v=ReadView();if(v.Practice==null || v.Practice.EndReason.HasValue || !v.OwnHand.Contains(card.Value))return false;
            if(v.SpaceDiscard!=null)return v.SpaceDiscard.CanChoose && v.SpaceDiscard.Candidates.Contains(card);
            var selection=v.Headline?.Selection;
            return selection==null || (selection.CanChoose && selection.Candidates.Contains(card));
        }
        public string NextTurnButtonText=>ReadView().Practice?.HeadlineEnabled==true?"下一回合（进入头条）":"下一回合（跳过头条）";
        public string PracticePhaseText
        {
            get
            {
                var v=ReadView();var p=v.Practice;if(p==null)return "独立练习";
                if(p.FinalScoring!=null)return FinalScoringPanelFrame.Create(p.FinalScoring).Outcome;
                if(v.SpaceDiscard?.Status==SpaceEndTurnDiscardStatus.Pending)return "太空弃牌 · "+(v.SpaceDiscard.Owner==PlayerSide.US?"美国":"苏联")+"选择";
                if(p.Winner.HasValue)return v.Headline?.Status==PublicHeadlineStatus.Terminal?"头条终局":"对局终局";
                if(v.FiveYearPlan?.Outcome==FiveYearPlanOutcome.AwaitingUsEvent)return "五年计划 · 等待美国选择";
                if(v.Blockade!=null)return "封锁 · 等待"+(v.Blockade.Owner==PlayerSide.US?"美国":"苏联")+"回应";
                if(v.Headline?.Status==PublicHeadlineStatus.Collecting)return "头条选择 · 双方分别锁定";
                if(v.Headline!=null && v.Headline.Status!=PublicHeadlineStatus.Completed)return "头条结算";
                if(p.Stage==ActionPracticeStage.Complete)return p.TurnEnd!=null?"回合末审核完成":"本回合行动结束";
                return (p.Actor==PlayerSide.US?"美国":"苏联")+(p.IsExtraAction?" 太空额外":" 行动")+$" {p.Round}/{p.RoundLimit}";
            }
        }
        HeadlineCommand lastHeadlineCommand;
        PlayerSide lastHeadlineSide;
        public HeadlineCommandResult LastHeadlineResult { get; private set; }
        public bool CanRetryHeadline=>lastHeadlineCommand!=null && lastHeadlineSide==SelectedSide;
        public async Task<HeadlineCommandResult> SubmitSelectedHeadlineAsync()
        {
            var card=SelectedCard;var v=ReadView();var selection=v.Headline?.Selection;
            if(!card.HasValue || selection?.CanChoose!=true || !selection.Candidates.Contains(card.Value))
                throw new InvalidOperationException("Select a currently available headline for this identity.");
            var epoch=generation;var session=Session;var command=new HeadlineCommand(Guid.NewGuid().ToString("N"),v.GameId,v.Version,card.Value);
            lastHeadlineCommand=command;lastHeadlineSide=SelectedSide;
            var result=await session.SubmitHeadlineAsync(command);
            if(epoch==generation)
            {
                LastHeadlineResult=result;LastResult=null;LastCardResult=null;legal=null;
                if(result.Accepted){RememberHeadlineFeedback(result.View,result.Steps);selectedCard=null;SelectingCoup=false;ResetEventInteraction(false);}
                MarkInteractionChanged();
            }
            return result;
        }
        public async Task<HeadlineCommandResult> RetryHeadlineAsync()
        {
            if(!CanRetryHeadline)throw new InvalidOperationException("No headline command for this identity.");
            var epoch=generation;var result=await Session.SubmitHeadlineAsync(lastHeadlineCommand);
            if(epoch==generation){LastHeadlineResult=result;if(result.Accepted)RememberHeadlineFeedback(result.View,result.Steps);MarkInteractionChanged();}return result;
        }
        void ResetHeadlineInteraction(bool clearReceipt)
        {LastHeadlineResult=null;if(clearReceipt)lastHeadlineCommand=null;}
    }
}
