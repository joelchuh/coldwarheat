using System;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        void DrawSpaceDiscardControls(PlayerViewSnapshot view)
        {
            var choice=view.SpaceDiscard;var audit=view.Practice.TurnEnd;
            Text(972,366,262,27,"太空奖励 · "+Side(choice.Owner),label);
            Text(972,400,262,55,$"军事／扣留审核已通过。\n军事净 VP {audit.VpDelta:+0;-0;0}。\n可弃一张手牌，或保留全部。",small);
            var selected=controller.SelectedCard;
            Text(972,460,262,40,choice.CanChoose?(selected.HasValue?"所选："+CardTitle(selected.Value):"请先打开手牌选择一张。")
                :"等待 "+Side(choice.Owner)+" 决定。\n对方候选牌保持隐藏。",small);
            if(choice.CanChoose)
            {
                if(Button(972,506,262,32,"打开手牌选择弃牌"))OpenCards();
                GUI.enabled=controller.CanDiscardSelectedHeldCard;
                if(Button(972,545,262,32,"确认弃掉所选牌"))DiscardHeldCard();
                GUI.enabled=true;
                if(Button(972,584,262,32,"保留全部手牌"))KeepHeldCards();
            }
            else if(Button(972,506,262,32,"切换至 "+Side(choice.Owner)))
            {controller.SelectSide(choice.Owner);GUIUtility.ExitGUI();}
            if(controller.LastCardResult?.Accepted==false)Text(972,621,262,20,ErrorText(controller.LastCardResult.Error),small);
            GUI.enabled=controller.CanRetryCard;
            if(Button(972,646,262,27,"重发上次命令（幂等）"))RetryCard();
            GUI.enabled=true;
        }
        async void DiscardHeldCard()
        {try {await controller.DiscardSelectedHeldCardAsync();}catch(Exception e){fatal=e.Message;Debug.LogException(e);}}
        async void KeepHeldCards()
        {try {await controller.KeepHeldCardsAsync();}catch(Exception e){fatal=e.Message;Debug.LogException(e);}}
    }
}
