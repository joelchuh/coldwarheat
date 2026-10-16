using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    internal sealed class DemoTurnEndSetup
    {
        internal int Turn { get; }
        internal int InitialVp { get; }
        internal CardZones Cards { get; }
        internal RandomSnapshot Random { get; }
        internal SpaceTrackState InitialTrack { get; }
        internal IReadOnlyList<CardId> HeldCards { get; }
        internal int? OrdinaryActionsToPrepare { get; }
        internal DemoTurnEndSetup(int turn,int vp,CardZones cards,RandomSnapshot random,SpaceTrackState initialTrack=null,IEnumerable<CardId> heldCards=null,int? ordinaryActionsToPrepare=null)
        { OrdinaryActionsToPrepare=ordinaryActionsToPrepare;Turn=turn;InitialVp=vp;Cards=cards;Random=random;InitialTrack=initialTrack;HeldCards=System.Array.AsReadOnly(heldCards?.ToArray()??System.Array.Empty<CardId>()); }
    }
    public sealed partial class DemoCatalog
    {
        GameState CreateTurnEndFixture(DemoScenario scene)
        {
            var setup=scene.TurnEndSetup;var p=ActionPracticeState.CreateAtTurn(scene.PracticeDefinition,setup.Turn,setup.InitialVp,setup.InitialTrack);
            var prepared=setup.OrdinaryActionsToPrepare??p.Space.Turn.Parameters.NormalActionsPerSide*2;
            if(prepared<0 || prepared>p.Space.Turn.Parameters.NormalActionsPerSide*2)throw new ArgumentException("Fixture preparation exceeds ordinary slots.");
            var gameId="demo-turn-end-"+Guid.NewGuid().ToString("N");var ops=OperationsContext.FromRoundStart(gameId+".setup",PlayerSide.USSR,0,Map,scene.Countries,costs);
            var state=new GameState(Map,scene.Countries,ops,gameId,turn:p.Space.Turn,cards:setup.Cards,randomState:setup.Random,practice:p);
            GameState Apply(CardActionKind kind,CardId? card=null)
            {
                var plan=ActionPracticeRules.Plan(state,state.Practice.Actor,kind,"fixture-"+state.Version,card);
                if(!plan.Accepted)throw new ArgumentException("Invalid turn-end fixture action: "+plan.Error);
                return plan.NextState;
            }
            state=Apply(CardActionKind.StartPractice);
            while(state.Practice.CompletedActions<prepared)
            {
                state=Apply(CardActionKind.Influence,state.Cards.GetHand(state.Practice.Actor).First(card=>!setup.HeldCards.Contains(card)));
                state=Apply(CardActionKind.FinishInfluence);
            }
            return state;
        }
    }
}
