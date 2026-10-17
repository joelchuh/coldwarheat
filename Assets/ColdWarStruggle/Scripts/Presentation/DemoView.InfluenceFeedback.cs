using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        // Read the public receipt in execution order. The current board may already be newer.
        string WithInfluenceChanges(string summary,IReadOnlyList<InfluenceChanged> changes)
        {
            if(changes.Count==0)return summary+"\n影响力无变化。";
            return summary+"\n"+string.Join("\n",changes.Select(change=>
                $"{Name(change.Country)} · {Side(change.Side)} {change.Before} → {change.After} ({((long)change.After-change.Before).ToString("+0;-0;0",CultureInfo.InvariantCulture)})"));
        }
        string InfluenceEventMessage(PublicCardEventResult result)
            =>result.Stage==InfluenceEventStage.Resolved?WithInfluenceChanges("事件完成",result.Changes):
                result.Stage==InfluenceEventStage.AwaitingChoice?"等待事件受益方完成选择。":"事件已建立，尚未结算。";
        string InfluenceChoiceMessage(EventCommandResult result)
        {
            if(!result.Accepted)return "未执行："+ErrorText(result.Error);
            var summary=result.FiveYearPlan!=null?FiveYearPlanResultMessage(result.FiveYearPlan):
                result.ResolutionSequence.HasValue?"事件选择完成 · 收据序号 "+result.ResolutionSequence.Value:"选择请求已建立。请按左侧提示操作。";
            return result.ResolutionSequence.HasValue?WithInfluenceChanges(summary,result.Changes):summary;
        }
    }
}
