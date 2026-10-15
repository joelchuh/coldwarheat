using System;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        void DrawTurnEndReport(PlayerViewSnapshot view)
        {
            Text(972,366,262,210,controller.TurnEndMessage,small);
            if(view.Practice.NextTurnEnabled && !view.Practice.Winner.HasValue)
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
