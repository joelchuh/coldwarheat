using System.Linq;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        void DrawRelocationControls(EventTargetsResult query)
        {
            var destinations=controller.RelocationEditingDestinations;
            if(Button(972,366,126,29,(destinations?"":"● ")+"编辑移出"))controller.SelectRelocationPhase(false);
            if(Button(1108,366,126,29,(destinations?"● ":"")+"编辑移入"))controller.SelectRelocationPhase(true);
            destinations=controller.RelocationEditingDestinations;
            var from=controller.RelocationSourceDraft;var to=controller.RelocationDestinationDraft;
            var amount=(destinations?to:from).FirstOrDefault(a=>a.Country.Equals(controller.SelectedCountry)).Amount;
            Text(972,401,262,35,$"移出 {from.Sum(a=>a.Amount)} · 移入 {to.Sum(a=>a.Amount)} · 上限 {query.MaximumAmount}\n本国{(destinations?"移入":"移出")}：{amount} / {controller.RelocationCapacity(controller.SelectedCountry)}",small);
            GUI.enabled=controller.CanChangeRelocation(controller.SelectedCountry,-1);
            if(Button(972,440,126,29,"− 1 点"))controller.ChangeRelocation(controller.SelectedCountry,-1);
            GUI.enabled=controller.CanChangeRelocation(controller.SelectedCountry,1);
            if(Button(1108,440,126,29,"+ 1 点"))controller.ChangeRelocation(controller.SelectedCountry,1);
            GUI.enabled=controller.CanConfirmEvent;
            if(Button(972,475,262,35,from.Count==0 && to.Count==0?"不迁移并完成":"确认完整迁移"))ConfirmEvent();
            GUI.enabled=from.Count>0 || to.Count>0;
            if(Button(972,515,126,30,"清空草稿"))controller.ClearEventDraft();
            GUI.enabled=controller.CanRetryEvent;
            if(Button(1108,515,126,30,"重发命令"))RetryEvent();
            GUI.enabled=true;
            Text(972,550,262,32,"先移出、再分配相同点数。\n同国往返仍待裁定。",small);
            DrawFeedback(587,83,ReadVisibleFeedback());
        }
    }
}
