using System;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed partial class LocalGameHost
    {
        CardActionOptions QueryCards(PlayerSide side) { lock(gate)return new CardActionOptions(state,side); }
        LegalTargetsResult QueryRealignment(PlayerSide side,string contextId,long version)
        {
            lock(gate)
            {
                var error=version!=state.Version?RuleError.StaleVersion:contextId!=state.Operations.Id?RuleError.StaleContext:ActionPracticeRules.CanRollRealignment(state,side);
                var targets=error==RuleError.None?state.Map.Countries.Where(c=>ActionPracticeRules.CheckRealignmentTarget(state,side,c.Id)==RuleError.None).Select(c=>c.Id).ToArray():Array.Empty<CountryId>();
                return new LegalTargetsResult(error,state.Version,side,state.Operations.Id,targets);
            }
        }
        LegalTargetsResult QueryCoup(PlayerSide side,CardId card,long version)
        {
            lock(gate)
            {
                var error=version!=state.Version?RuleError.StaleVersion:ActionPracticeRules.CanPlay(state,side,card,CardActionKind.Coup);
                var targets=error==RuleError.None?state.Map.Countries.Where(c=>ActionPracticeRules.CheckCoupTarget(state,side,card,c.Id)==RuleError.None).Select(c=>c.Id).ToArray():Array.Empty<CountryId>();
                return new LegalTargetsResult(error,state.Version,side,state.Operations.Id,targets);
            }
        }
        CardActionResult RejectCard(RuleError error,PlayerSide side)=>new CardActionResult(error,new PlayerViewSnapshot(state,side));
        Task<CardActionResult> SubmitCard(PlayerSide side,CardActionCommand command,CancellationToken token)
        {
            lock(gate)
            {
                if(token.IsCancellationRequested)return Task.FromCanceled<CardActionResult>(token);
                if(!ValidEnvelope(command))return Task.FromResult(RejectCard(RuleError.InvalidCommand,side));
                var key=(side,command.CommandId);
                if(receipts.TryGetValue(key,out var receipt))return Task.FromResult(command.SameContent(receipt.Command)?receipt.CardAction:RejectCard(RuleError.CommandIdConflict,side));
                var error=command.GameId!=state.GameId?RuleError.WrongGame:command.ExpectedVersion!=state.Version?RuleError.StaleVersion:RuleError.None;
                CardActionResult result;
                if(error!=RuleError.None)result=RejectCard(error,side);
                else
                {
                    var plan=ActionPracticeRules.Plan(state,side,command.Kind,command.CommandId,command.Card,command.Target);
                    if(!plan.Accepted)result=RejectCard(plan.Error,side);
                    else
                    {
                        result=new CardActionResult(RuleError.None,new PlayerViewSnapshot(plan.NextState,side),plan.SpaceReceipt,plan.CoupReceipt,plan.ScoringResult,plan.EventTransition,plan.DefconVpEvent,plan.ActivatedOpsEffect,plan.Realignment);
                        state=plan.NextState;
                    }
                }
                receipts.Add(key,new CommandReceipt(command,result));return Task.FromResult(result);
            }
        }
    }
}
