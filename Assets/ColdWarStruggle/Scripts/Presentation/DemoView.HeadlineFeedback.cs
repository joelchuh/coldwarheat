using System.Linq;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        // A receipt may resolve both headlines. Preserve their public summaries in execution order.
        public string ReadHeadlineFeedback()
        {
            var steps=controller.CurrentHeadlineFeedback;
            return steps.Count==0?null:string.Join("\n\n",steps.Select(HeadlineStepFeedback));
        }
        string HeadlineStepFeedback(PublicHeadlineStep step)
        {
            if(step.SpaceEvent!=null)return "头条 · "+SpaceEventResultMessage(step.SpaceEvent);
            if(step.War!=null)return "头条 · "+WarResultMessage(step.War);
            if(step.FiveYearPlan!=null)
                return "头条 · "+FiveYearPlanResultMessage(step.FiveYearPlan)+FiveYearPlanChildDetail(step.DefconVpEvent,step.ActivatedOpsEffect)
                    +(step.Event?.Stage==ColdWarStruggle.Core.InfluenceEventStage.Resolved?"\n"+InfluenceEventMessage(step.Event):"");
            return "头条 · "+CardTitle(step.Card)+"\n"+InfluenceEventMessage(step.Event);
        }
    }
}
