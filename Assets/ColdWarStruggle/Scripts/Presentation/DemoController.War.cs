using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        public bool CanConfirmWar
        {
            get {var choice=ReadView().War;return choice?.CanChoose==true && choice.Candidates.Contains(SelectedCountry);}
        }
        public Task<CardActionResult> ConfirmWarAsync()
        {
            if(!CanConfirmWar)throw new InvalidOperationException("Only the live owner may confirm a current war target.");
            return SendCard(CardActionKind.ResolveWar,target:SelectedCountry);
        }
        public PublicHeadlineStep CurrentWarHeadline=>CurrentHeadlineFeedback?.War!=null?CurrentHeadlineFeedback:null;
    }
}
