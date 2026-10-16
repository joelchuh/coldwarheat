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
            =>"头条 · "+(step.SpaceEvent!=null?SpaceEventResultMessage(step.SpaceEvent):step.War!=null?WarResultMessage(step.War):
                FiveYearPlanResultMessage(step.FiveYearPlan)+FiveYearPlanChildDetail(step.DefconVpEvent,step.ActivatedOpsEffect));
    }
}
