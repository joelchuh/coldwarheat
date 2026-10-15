using System;
using System.Threading;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed partial class LocalGameHost
    {
        // Called only under the shared command gate, after identity/version/decision validation.
        EventCommandResult CompleteHeadlineChoice(PlayerSide side,InfluenceEventTransition raw)
        {
            if(!raw.Accepted)return RejectEvent(raw.Error,side,raw);
            var completed=HeadlineResolutionRules.CompleteChoice(raw);
            // A successful effect draft may still fail during disposition or the following headline.
            // In that case none of its changes or sequence markers describe committed events.
            if(!completed.Accepted)return RejectEvent(completed.Error,side);
            var result=new EventCommandResult(RuleError.None,new PlayerViewSnapshot(completed.NextState,side),raw,completed.Steps);
            state=completed.NextState;
            return result;
        }
        HeadlineCommandResult RejectHeadline(RuleError error,PlayerSide side)=>new HeadlineCommandResult(error,new PlayerViewSnapshot(state,side));
        Task<HeadlineCommandResult> SubmitHeadline(PlayerSide side,HeadlineCommand command,CancellationToken token)
        {
            lock(gate)
            {
                if(token.IsCancellationRequested)return Task.FromCanceled<HeadlineCommandResult>(token);
                if(!ValidEnvelope(command) || string.IsNullOrWhiteSpace(command.Card.Value))return Task.FromResult(RejectHeadline(RuleError.InvalidCommand,side));
                var key=(side,command.CommandId);
                if(receipts.TryGetValue(key,out var receipt))return Task.FromResult(command.SameContent(receipt.Command)?receipt.Headline:RejectHeadline(RuleError.CommandIdConflict,side));
                var error=command.GameId!=state.GameId?RuleError.WrongGame:command.ExpectedVersion!=state.Version?RuleError.StaleVersion:RuleError.None;
                HeadlineCommandResult result;
                if(error!=RuleError.None)result=RejectHeadline(error,side);
                else
                {
                    var transition=HeadlineFlowRules.Submit(state,side,command.Card);
                    if(!transition.Accepted)result=RejectHeadline(transition.Error,side);
                    else
                    {
                        // Project the final transaction before publishing it; never expose authority drafts.
                        result=new HeadlineCommandResult(RuleError.None,new PlayerViewSnapshot(transition.NextState,side),transition.Steps);
                        state=transition.NextState;
                    }
                }
                receipts.Add(key,new CommandReceipt(command,result));
                return Task.FromResult(result);
            }
        }
    }
}
