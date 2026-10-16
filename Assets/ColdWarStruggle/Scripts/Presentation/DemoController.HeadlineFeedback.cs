using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        PublicHeadlineStep fiveYearHeadline;
        string fiveYearHeadlineGame;
        long fiveYearHeadlineVersion=-1;
        public PublicHeadlineStep CurrentFiveYearHeadline
        {
            get
            {
                if(fiveYearHeadline==null)return null;
                var current=ReadView();
                return current.GameId==fiveYearHeadlineGame && current.Version==fiveYearHeadlineVersion?fiveYearHeadline:null;
            }
        }
        void RememberHeadlineFeedback(PlayerViewSnapshot receipt,IReadOnlyList<PublicHeadlineStep> steps)
        {
            var step=steps?.LastOrDefault(s=>s.FiveYearPlan!=null);if(step==null)return;
            var current=ReadView();
            // A successful retry can return an older receipt; never restore its waiting state.
            if(receipt.GameId!=current.GameId || receipt.Version!=current.Version)return;
            fiveYearHeadline=step;fiveYearHeadlineGame=receipt.GameId;fiveYearHeadlineVersion=receipt.Version;
        }
        void ClearHeadlineFeedback()
        {fiveYearHeadline=null;fiveYearHeadlineGame=null;fiveYearHeadlineVersion=-1;}
    }
}
