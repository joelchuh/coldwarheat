using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class PracticeRiskDefinition
    {
        public CoupDefinition Coup { get; }
        public DefconDefinition Defcon { get; }
        public MilitaryOperationsDefinition Military { get; }
        public VictoryDefinition Victory { get; }
        public PracticeRiskDefinition(CoupDefinition coup,DefconDefinition defcon,MilitaryOperationsDefinition military,VictoryDefinition victory)
        { Coup=coup??throw new ArgumentNullException(nameof(coup));Defcon=defcon??throw new ArgumentNullException(nameof(defcon));Military=military??throw new ArgumentNullException(nameof(military));Victory=victory??throw new ArgumentNullException(nameof(victory)); }
    }
    // No second copy of the world's influence or the live random cursor is kept here.
    public sealed class PracticeRiskState
    {
        public PracticeRiskDefinition Definition { get; }
        public DefconState Defcon { get; }
        public MilitaryOperationsState Military { get; }
        public IReadOnlyDictionary<string,CoupAttemptReceipt> CoupReceipts { get; }
        public VictoryDecision Result { get; }
        internal PracticeRiskState(PracticeRiskDefinition definition,DefconState defcon,MilitaryOperationsState military,IReadOnlyDictionary<string,CoupAttemptReceipt> receipts,VictoryDecision result=null)
        { Definition=definition;Defcon=defcon;Military=military;CoupReceipts=new ReadOnlyDictionary<string,CoupAttemptReceipt>(receipts.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal));Result=result; }
        public static PracticeRiskState Create(PracticeRiskDefinition definition,GameTurnContext turn)
        {
            if(definition==null || turn==null)throw new ArgumentNullException();
            return new PracticeRiskState(definition,new DefconState(definition.Defcon,definition.Defcon.InitialLevel),MilitaryOperationsState.Create(definition.Military,turn),new Dictionary<string,CoupAttemptReceipt>());
        }
        internal CoupAttemptState BindCurrent(GameState state)
        {
            if(!ReferenceEquals(state.Practice?.Risk,this) || !ReferenceEquals(state.Map,Definition.Defcon.Map) || state.Turn.Number!=Military.Turn.Number
                || CoupReceipts.Values.Any(r=>r.Request.GameId!=state.GameId))throw new ArgumentException("Risk history belongs to another authority.");
            return new CoupAttemptState(state.GameId,state.Version,Definition.Coup,Defcon,Military,state.Countries,state.RandomState,Array.Empty<string>(),CoupReceipts);
        }
        internal PracticeRiskState WithVictory(VictoryDecision decision)
        {
            if(decision==null || (decision.Outcome!=VictoryOutcome.Continue && decision.Outcome!=VictoryOutcome.Finished))throw new ArgumentException("Expected a closed ordinary victory decision.");
            return new PracticeRiskState(Definition,Defcon,Military,CoupReceipts,decision.Outcome==VictoryOutcome.Finished?decision:null);
        }
        internal PracticeRiskState After(DefconVpEventPlan plan)
        {
            if(plan==null || !ReferenceEquals(plan.Before,Defcon) || !ReferenceEquals(plan.VictoryDefinition,Definition.Victory) || Result!=null)throw new ArgumentException("Event plan belongs to a different or ended risk state.");
            return new PracticeRiskState(Definition,plan.After,Military,CoupReceipts,plan.Decision.Outcome==VictoryOutcome.Finished?plan.Decision:null);
        }
        internal PracticeRiskState After(CoupAttemptPlan plan,int vp)
        {
            var result=plan.Receipt.DefconChange?.NuclearWar==true
                ?VictoryService.Evaluate(Definition.Victory,new NuclearWarEvidence(plan.Receipt.Request.Id,plan.Receipt.Request.SourceRuleId,vp,plan.Receipt.DefconChange)):null;
            return new PracticeRiskState(Definition,plan.After.Defcon,plan.After.Military,plan.After.Receipts,result);
        }
    }
}
