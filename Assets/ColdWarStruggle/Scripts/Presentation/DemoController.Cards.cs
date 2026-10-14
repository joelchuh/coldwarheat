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
