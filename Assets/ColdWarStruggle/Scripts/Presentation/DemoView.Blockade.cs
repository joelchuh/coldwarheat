using System;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        Vector2 blockadeScroll;
        void DrawBlockadeControls(PlayerViewSnapshot view)
        {
            var choice=view.Blockade;
            if(!choice.CanChoose)
            {
                Text(972,365,262,90,CardTitle(choice.Card)+"：等待"+Side(choice.Owner)+"回应。\n对方自行决定是否弃牌；候选仅对选择方可见。",small);
                if(Button(972,466,262,40,"切换至 "+Side(choice.Owner))){controller.SelectSide(choice.Owner);GUIUtility.ExitGUI();}
                return;
            }
            Text(972,361,262,54,"弃掉一张有效 OPS ≥ "+choice.MinimumEffectiveOps+" 的牌，可保留"+Name(choice.FallbackCountry)+"影响力。",small);
            blockadeScroll=GUI.BeginScrollView(new Rect(970,417,266,76),blockadeScroll,new Rect(0,0,244,Math.Max(74,choice.Candidates.Count*36)));
            for(int i=0;i<choice.Candidates.Count;i++)
            {
                var option=choice.Candidates[i];var selected=controller.SelectedCard.HasValue && controller.SelectedCard.Value.Equals(option.Card);
                if(Button(2,i*36,240,32,(selected?"● ":"")+CardTitle(option.Card)+" · OPS "+option.EffectiveOps))controller.SelectCard(option.Card);
            }
            GUI.EndScrollView();
            GUI.enabled=controller.CanDiscardForBlockade;
            if(Button(972,497,262,33,"确认弃掉所选牌"))RespondToBlockade(true);
            GUI.enabled=true;
            if(Button(972,536,262,35,"不弃牌 · 清除"+Name(choice.FallbackCountry)+Side(choice.Owner)))RespondToBlockade(false);
        }
        string BlockadeResultMessage(PublicBlockadeResult result)=>result.Outcome switch
        {
            BlockadeOutcome.AwaitingChoice=>"等待"+Side(result.Owner)+"回应封锁。",
            BlockadeOutcome.Discarded=>Side(result.Owner)+"已弃："+CardTitle(result.DiscardedCard.Value)+"\n"+Name(result.FallbackCountry)+"影响力保留；弃牌不触发事件。",
            _=>Name(result.FallbackCountry)+"的"+Side(result.Owner)+"影响力已清除。\n封锁事件完成。"
        };
        async void RespondToBlockade(bool discard)
        {
            try {if(discard)await controller.DiscardForBlockadeAsync(controller.SelectedCard.Value);else await controller.DeclineBlockadeAsync();}
            catch(Exception e){fatal=e.Message;Debug.LogException(e);}
        }
    }
}
