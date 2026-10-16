using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        PublicHeadlineStep headlineFeedback;
        string headlineFeedbackGame;
        long headlineFeedbackVersion=-1;
        public PublicHeadlineStep CurrentFiveYearHeadline=>CurrentHeadlineFeedback?.FiveYearPlan!=null?CurrentHeadlineFeedback:null;
        PublicHeadlineStep CurrentHeadlineFeedback
        {
            get
            {
                if(headlineFeedback==null)return null;
                var current=ReadView();
                return current.GameId==headlineFeedbackGame && current.Version==headlineFeedbackVersion?headlineFeedback:null;
            }
        }
        void RememberHeadlineFeedback(PlayerViewSnapshot receipt,IReadOnlyList<PublicHeadlineStep> steps)
        {
            var step=steps?.LastOrDefault(s=>s.FiveYearPlan!=null || s.War!=null);if(step==null)return;
            var current=ReadView();
            // A successful retry can return an older receipt; never restore its waiting state.
            if(receipt.GameId!=current.GameId || receipt.Version!=current.Version)return;
            headlineFeedback=step;headlineFeedbackGame=receipt.GameId;headlineFeedbackVersion=receipt.Version;
        }
        void ClearHeadlineFeedback()
        {headlineFeedback=null;headlineFeedbackGame=null;headlineFeedbackVersion=-1;}
    }
}
