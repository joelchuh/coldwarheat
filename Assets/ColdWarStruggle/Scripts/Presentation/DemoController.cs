using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        public PlayerSide SelectedSide { get; private set; }
        public CountryId SelectedCountry { get; private set; }
        public PlayerCommandResult LastResult { get; private set; }
        readonly Func<string,LocalGameHost> factory;
        LocalGameHost host;
        CommandEnvelope lastCommand;
        PlayerSide lastSide;
        int generation;
        public int ViewGeneration=>generation;
        long interactionRevision;
        public long InteractionRevision=>interactionRevision;
        void MarkInteractionChanged() { interactionRevision=checked(interactionRevision+1); }
        LegalTargetsResult legal;
        IGameSession Session=>SelectedSide==PlayerSide.US?host.UsSession:host.UssrSession;
        public bool CanRetry => lastCommand!=null && lastSide==SelectedSide;
        public DemoController(Func<string,LocalGameHost> factory) { this.factory=factory??throw new ArgumentNullException(nameof(factory)); }
        public void LoadScenario(string id,PlayerSide side,CountryId country)
        {
            if(side!=PlayerSide.US && side!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(side));
            var next=factory(id);
            if(!next.UsSession.ReadView().Countries.Any(c=>c.Definition.Id.Equals(country)))throw new ArgumentException("Unknown selection.");
            host=next;SelectedSide=side;SelectedCountry=country;generation++;legal=null;lastCommand=null;LastResult=null;
            ResetEventInteraction(true);
            ResetDeploymentInteraction(true);
            ResetCardInteraction(true);
        }
        public void SelectSide(PlayerSide side)
        {
            if(side!=PlayerSide.US && side!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(side));
            if(side==SelectedSide)return;
            SelectedSide=side;generation++;legal=null;LastResult=null;
            ResetEventInteraction(false);
            ResetDeploymentInteraction(false);
            ResetCardInteraction(false);
        }
        public void SelectCountry(CountryId country)
        {
            if(!ReadView().Countries.Any(c=>c.Definition.Id.Equals(country)))throw new ArgumentException("Unknown selection.");
            if(SelectedCountry.Equals(country))return;
            SelectedCountry=country;MarkInteractionChanged();
        }
        public PlayerViewSnapshot ReadView() => Session.ReadView();
        public LocalTestSnapshot ReadBoth() => host.ReadLocalTestSnapshot();
        public LegalTargetsResult LegalTargets()
        {
            var v=ReadView();var card=SelectedCard;if(legal==null || legal.Version!=v.Version || legal.Viewer!=SelectedSide || legal.ContextId!=v.ContextId)
                legal=v.Practice?.Stage==ActionPracticeStage.Realigning?Session.QueryRealignmentTargets(v.ContextId,v.Version):SelectingCoup && card.HasValue?Session.QueryCoupTargets(card.Value,v.Version):Session.QueryInfluenceTargets(v.ContextId,v.Version);
            return legal;
        }
        public async Task<PlayerCommandResult> SubmitAsync(int points)
        {
            if(points<1 || points>128)throw new ArgumentOutOfRangeException(nameof(points));
            var v=ReadView();var epoch=generation;var command=new CommandEnvelope(Guid.NewGuid().ToString("N"),v.GameId,v.Version,v.ContextId,Enumerable.Repeat(SelectedCountry,points));
            lastCommand=command;lastSide=SelectedSide;
            var result=await Session.SubmitAsync(command);
            if(epoch==generation) { LastResult=result;legal=null; }
            return result;
        }
        public async Task<PlayerCommandResult> RetryAsync()
        {
            if(!CanRetry)throw new InvalidOperationException("No command for this side.");
            var epoch=generation;var result=await Session.SubmitAsync(lastCommand);
            if(epoch==generation)LastResult=result;
            return result;
        }
    }
}
