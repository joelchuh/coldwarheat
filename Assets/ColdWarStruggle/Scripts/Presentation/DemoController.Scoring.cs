using System;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        ScoringPreviewResult scoringPreview;
        string scoringRegion;
        public ScoringPreviewResult ScoringPreview(string region)
        {
            var view=ReadView();
            if(scoringPreview==null || scoringPreview.GameId!=view.GameId || scoringPreview.Version!=view.Version || scoringRegion!=region)
            {scoringPreview=Session.QueryScoringPreview(region,view.Version);scoringRegion=region;}
            return scoringPreview;
        }
    }
}
