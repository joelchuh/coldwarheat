using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        IReadOnlyList<PublicHeadlineStep> headlineFeedback=Array.Empty<PublicHeadlineStep>();
        string headlineFeedbackGame;
        long headlineFeedbackVersion=-1;
        public PublicHeadlineStep CurrentFiveYearHeadline=>CurrentHeadlineFeedback.LastOrDefault(s=>s.FiveYearPlan!=null);
        public IReadOnlyList<PublicHeadlineStep> CurrentHeadlineFeedback
        {
            get
            {
                if(headlineFeedback.Count==0)return headlineFeedback;
                var current=ReadView();
                return current.GameId==headlineFeedbackGame && current.Version==headlineFeedbackVersion?headlineFeedback:Array.Empty<PublicHeadlineStep>();
            }
        }
        void RememberHeadlineFeedback(PlayerViewSnapshot receipt,IReadOnlyList<PublicHeadlineStep> steps)
        {
            var summaries=steps?.Where(s=>s.FiveYearPlan!=null || s.War!=null || s.SpaceEvent!=null || s.Event!=null || s.ActivatedOpsEffect!=null).ToArray();if(summaries==null || summaries.Length==0)return;
            var current=ReadView();
            // A successful retry can return an older receipt; never restore its waiting state.
            if(receipt.GameId!=current.GameId || receipt.Version!=current.Version)return;
            headlineFeedback=Array.AsReadOnly(summaries);headlineFeedbackGame=receipt.GameId;headlineFeedbackVersion=receipt.Version;
        }
        void ClearHeadlineFeedback()
        {headlineFeedback=Array.Empty<PublicHeadlineStep>();headlineFeedbackGame=null;headlineFeedbackVersion=-1;}
    }
}
