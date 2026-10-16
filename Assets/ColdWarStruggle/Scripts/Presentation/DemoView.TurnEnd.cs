using System;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        void DrawTurnEndReport(PlayerViewSnapshot view)
        {
            if(view.SpaceDiscard?.Status==SpaceEndTurnDiscardStatus.Pending){DrawSpaceDiscardControls(view);return;}
            if(view.Practice.FinalScoring!=null){DrawFinalScoringControls(view);return;}
            Text(972,366,262,210,controller.TurnEndMessage,small);
            if(view.Practice.FinalScoringEnabled && view.Turn.Number==10 && !view.Practice.EndReason.HasValue)
            {
                GUI.enabled=view.Practice.FinalScoringAvailable;
                if(Button(972,584,262,32,"执行最终计分"))CompleteFinalScoring();
                GUI.enabled=true;
            }
            else if(view.Practice.NextTurnEnabled && !view.Practice.EndReason.HasValue)
            {
                GUI.enabled=view.Practice.NextTurnAvailable;
                if(Button(972,584,262,32,controller.NextTurnButtonText))BeginNextTurn();
                GUI.enabled=true;
            }
            if(controller.LastCardResult?.Accepted==false)Text(972,618,262,25,ErrorText(controller.LastCardResult.Error),small);
            GUI.enabled=controller.CanRetryCard;
            if(Button(972,646,262,27,"重发结算命令（幂等）"))RetryCard();
            GUI.enabled=true;
        }
        async void BeginNextTurn()
        { try { await controller.BeginNextTurnAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void AssessTurnEnd()
        { try { await controller.AssessTurnEndAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
    }
}
