using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
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
                if(result.Accepted){selectedCard=null;SelectingCoup=false;ResetEventInteraction(false);}
                MarkInteractionChanged();
            }
            return result;
        }
        public async Task<HeadlineCommandResult> RetryHeadlineAsync()
        {
            if(!CanRetryHeadline)throw new InvalidOperationException("No headline command for this identity.");
            var epoch=generation;var result=await Session.SubmitHeadlineAsync(lastHeadlineCommand);
            if(epoch==generation){LastHeadlineResult=result;MarkInteractionChanged();}return result;
        }
        void ResetHeadlineInteraction(bool clearReceipt)
        {LastHeadlineResult=null;if(clearReceipt)lastHeadlineCommand=null;}
    }
}
