using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        CardId? selectedCard;
        string selectedCardGame;
        long selectedCardVersion;
        PlayerSide selectedCardSide,lastCardSide;
        CardActionCommand lastCardCommand;
        public CardId? SelectedCard
        {
            get
            {
                var v=ReadView();
                if(v.GameId!=selectedCardGame || v.Version!=selectedCardVersion || selectedCardSide!=SelectedSide){selectedCard=null;SelectingCoup=false;legal=null;}
                return selectedCard;
            }
        }
        public CardActionResult LastCardResult { get; private set; }
        string eventCoupGame,eventCoupExecution,eventCoupMessage;
        // Store only public display text across identity changes, never another player's snapshot.
        public string EventPanelCoupMessage
        {
            get
            {
                var view=ReadView();return view.GameId==eventCoupGame && view.Event?.ExecutionId==eventCoupExecution
                    && view.Event?.Stage==InfluenceEventStage.AwaitingChoice?eventCoupMessage:null;
            }
        }
        void RememberEventCoup(CardActionResult result)
        {
            if(!result.Accepted)return;
            eventCoupGame=null;eventCoupExecution=null;eventCoupMessage=null;
            if(result.Coup==null || result.Event?.Stage!=InfluenceEventStage.AwaitingChoice)return;
            var c=result.Coup;eventCoupGame=result.View.GameId;eventCoupExecution=result.View.Event.ExecutionId;
            eventCoupMessage=$"政变骰 {c.Roll} / OPS {c.Strength} / 差额 {c.Margin}\n影响力 −{c.Removed}/+{c.Added} · 军事 +{c.MilitaryCredit}\n随后事件：等待受益方选择。";
        }
        public bool CanRetryCard=>lastCardCommand!=null && lastCardSide==SelectedSide;
        public CardActionOptions CardOptions()=>Session.QueryCardActions();
        public void SelectCard(CardId card)
        {
            var v=ReadView();var china=v.Cards?.China;
            if(!v.OwnHand.Contains(card.Value) && !(china!=null && china.Id.Equals(card) && china.Owner==SelectedSide && china.Face==CardFace.FaceUp && CardOptions().China!=null))throw new ArgumentException("Only this identity's hand or available China card can be selected.");
            SelectingCoup=false;legal=null;selectedCard=card;selectedCardGame=v.GameId;selectedCardVersion=v.Version;selectedCardSide=SelectedSide;MarkInteractionChanged();
        }
        public PublicCoupQuote SelectedCoupQuote
        {
            get
            {
                var v=ReadView();var card=SelectedCard;
                if(v.Practice?.Stage==ActionPracticeStage.ChoosingOperations)card=v.Practice.ActiveCard;
                return SelectingCoup && card.HasValue?Session.QueryCoupQuote(card.Value,SelectedCountry,v.Version):null;
            }
        }
        public bool SelectingCoup { get; private set; }
        public void SelectCoupUse()
        {
            var v=ReadView();
            if(v.Practice?.Stage==ActionPracticeStage.ChoosingOperations)
            {
                if(CardOptions().PendingOperations?.Coup!=RuleError.None)throw new InvalidOperationException("Pending OPS cannot be used for a coup.");
                selectedCard=null;selectedCardGame=v.GameId;selectedCardVersion=v.Version;selectedCardSide=SelectedSide;
            }
            else
            {
                var card=SelectedCard??throw new InvalidOperationException("Select a card first.");
                var option=CardOptions().Find(card);
                if(option.Coup!=RuleError.None)throw new InvalidOperationException("This card cannot be used for a coup.");
            }
            SelectingCoup=true;legal=null;MarkInteractionChanged();
        }
        public Task<CardActionResult> PlayCardAsync(CardActionKind kind)
        {
            if(kind!=CardActionKind.Influence && kind!=CardActionKind.Space && kind!=CardActionKind.Coup && kind!=CardActionKind.Scoring && kind!=CardActionKind.Event && kind!=CardActionKind.Realignment && kind!=CardActionKind.EventThenInfluence && kind!=CardActionKind.InfluenceThenEvent && kind!=CardActionKind.OpponentEventFirst && kind!=CardActionKind.OpponentOpsFirst)throw new ArgumentException("A card use is required.");
            var card=SelectedCard??throw new InvalidOperationException("Select a card first.");return SendCard(kind,card,kind==CardActionKind.Coup?SelectedCountry:(CountryId?)null);
        }
        public Task<CardActionResult> ChooseOpponentOperationsAsync(OperationsMode mode)
        {
            if(!Enum.IsDefined(typeof(OperationsMode),mode))throw new ArgumentOutOfRangeException(nameof(mode));
            var kind=mode==OperationsMode.Coup?CardActionKind.UseOpponentCoup:mode==OperationsMode.Realignment?CardActionKind.UseOpponentRealignment:CardActionKind.UseOpponentInfluence;
            return SendCard(kind,target:mode==OperationsMode.Coup?SelectedCountry:(CountryId?)null);
        }
        public Task<CardActionResult> RollRealignmentAsync()=>SendCard(CardActionKind.RollRealignment,target:SelectedCountry);
        public Task<CardActionResult> FinishRealignmentAsync()=>SendCard(CardActionKind.FinishRealignment);
        public string TurnEndPrompt
        {
            get
            {
                var p=ReadView().Practice;
                if(p==null || p.Stage!=ActionPracticeStage.Complete || p.TurnEnd!=null || p.Winner.HasValue)return null;
                if(!p.TurnEndEnabled)return "本回合行动试玩已结束。\n此旧练习未启用回合末审核。";
                if(p.TurnEndAvailable)return "双方普通行动已结束。\n下一步：军事净罚分与扣留计分牌审核。";
                if(p.TurnEndError==RuleError.UnsupportedCardAction)return "已到试玩边界：太空额外行动尚未接入。\n暂不能结算本回合。";
                return "当前不能结算本回合，请先完成待处理行动。";
            }
        }
        public string TurnEndMessage
        {
            get
            {
                var p=ReadView().Practice;var r=p?.TurnEnd;if(r==null)return null;
                if(p.FinalScoring!=null)return $"回合 {r.Turn} 审核已完成。\n军事净罚分 {r.VpDelta:+0;-0;0}：{r.BeforeVp} → {r.AfterVp}。\n最终计分已完成，详见六地区明细。";
                var choice=ReadView().SpaceDiscard;
                var continuation=choice?.Status==SpaceEndTurnDiscardStatus.Pending?"等待"+(choice.Owner==PlayerSide.US?"美国":"苏联")+"选择太空回合末弃牌或保留。":p.FinalScoringEnabled && ReadView().Turn.Number==10?FinalScoringPrompt:!p.NextTurnEnabled?"试玩停在下一回合开始前。":p.NextTurnAvailable?"可进入"+NextTurnButtonText+"。":
                    p.NextTurnError==RuleError.ActionSliceComplete?"已到试玩边界：最终计分尚未接入。":
                    p.NextTurnError==RuleError.UnsupportedCardAction?"已到试玩边界：太空额外行动或其他未支持奖励尚未接入。":
                    "当前不能进入下一回合，请先完成待处理步骤。";
                var spaceResult=choice==null || choice.Status==SpaceEndTurnDiscardStatus.Pending?"":choice.Status==SpaceEndTurnDiscardStatus.Discarded?"太空奖励：已弃一张，不触发事件。\n":choice.Status==SpaceEndTurnDiscardStatus.Declined?"太空奖励：保留全部手牌。\n":"太空奖励：无普通手牌可弃。\n";
                string SideName(PlayerSide? side)=>side==PlayerSide.US?"美国":side==PlayerSide.USSR?"苏联":"无";
                return $"回合 {r.Turn} 结算 · 军事要求 {r.RequiredMilitary}\n美国累计 {r.UsEarned}，缺额 {r.UsShortfall}\n苏联累计 {r.UssrEarned}，缺额 {r.UssrShortfall}\n净 VP（美+）{r.VpDelta:+0;-0;0}：{r.BeforeVp} → {r.AfterVp}\n军事累计已归零。\n扣留计分牌：美{(r.UsHoldsScoring?"是":"否")} / 苏{(r.UssrHoldsScoring?"是":"否")}\n军事候选：{SideName(r.MilitaryCandidate)}\n"
                    +(r.Winner.HasValue?$"最终：{SideName(r.Winner)}获胜。\n终局停止翻面与效果清理。":$"尚未终局。中国牌{(r.ChinaFlipped?"已翻正":"保持正面")}。\n本回合 OPS 效果到期 {r.ExpiredOpsEffects} 项。\n{spaceResult}{continuation}");
            }
        }
        public Task<CardActionResult> BeginNextTurnAsync()=>SendCard(CardActionKind.BeginNextTurn);
        public Task<CardActionResult> AssessTurnEndAsync()=>SendCard(CardActionKind.AssessTurnEnd);
        public Task<CardActionResult> StartPracticeAsync()=>SendCard(CardActionKind.StartPractice);
        public Task<CardActionResult> FinishInfluenceAsync()=>SendCard(CardActionKind.FinishInfluence);
        async Task<CardActionResult> SendCard(CardActionKind kind,CardId? card=null,CountryId? target=null)
        {
            var v=ReadView();var epoch=generation;var session=Session;
            var command=new CardActionCommand(Guid.NewGuid().ToString("N"),v.GameId,v.Version,kind,card,target);
            lastCardCommand=command;lastCardSide=SelectedSide;
            var result=await session.SubmitCardActionAsync(command);
            if(epoch==generation) { LastCardResult=result;RememberEventCoup(result);LastResult=null;legal=null;if(result.Accepted){selectedCard=null;SelectingCoup=false;ResetEventInteraction(false);}MarkInteractionChanged(); }
            return result;
        }
        public async Task<CardActionResult> RetryCardAsync()
        {
            if(!CanRetryCard)throw new InvalidOperationException("No card command for this identity.");
            var epoch=generation;var result=await Session.SubmitCardActionAsync(lastCardCommand);
            if(epoch==generation)LastCardResult=result;return result;
        }
        void ResetCardInteraction(bool clearReceipt)
        { SelectingCoup=false;legal=null;selectedCard=null;selectedCardGame=null;LastCardResult=null;if(clearReceipt){lastCardCommand=null;eventCoupGame=null;eventCoupExecution=null;eventCoupMessage=null;}MarkInteractionChanged(); }
    }
}
