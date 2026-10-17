using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        public string ReadHeadlineFiveYearPlanFeedback()
        {
            var step=controller.CurrentFiveYearHeadline;
            return step==null?null:HeadlineStepFeedback(step);
        }
        string FiveYearPlanChildDetail(CardActionResult result)
            =>FiveYearPlanChildDetail(result.DefconVpEvent,result.ActivatedOpsEffect)
                +(result.Event?.Stage==InfluenceEventStage.Resolved?"\n"+InfluenceEventMessage(result.Event):"");
        string FiveYearPlanChildDetail(PublicDefconVpEventResult track,PublicTurnOpsEffect effect)
        {
            if(track!=null)return $"\nDEFCON {track.DefconFrom}→{track.DefconTo} · VP {track.VpForUS:+0;-0;0}";
            return effect==null?"":"\n"+OpsEffectSummary(effect);
        }
        string FiveYearPlanResultMessage(PublicFiveYearPlanResult result)
        {
            if(result.Outcome==FiveYearPlanOutcome.NoCard)return "五年计划：苏联已无手牌。\n事件完成，未进行抽样。";
            var first="五年计划抽中："+CardTitle(result.SelectedCard.Value)+"\n";
            return first+(result.Outcome switch
            {
                FiveYearPlanOutcome.AwaitingUsEvent=>"等待美国选择；原行动尚未完成。",
                FiveYearPlanOutcome.Discarded=>"已弃置；不触发事件或计分。",
                FiveYearPlanOutcome.UsEventResolved=>"子事件完成；已返回原流程。",
                _=>"触发终局；后续步骤停止。"
            });
        }
    }
}
