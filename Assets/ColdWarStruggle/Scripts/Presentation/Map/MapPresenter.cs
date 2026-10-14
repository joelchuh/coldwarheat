using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Presentation
{
    public sealed class MapPresenter : IDisposable
    {
        readonly DemoController controller;
        IReadOnlyDictionary<string,string> names;
        IMapView view;
        Action<MapIntent> handler;
        MapFrame frame;
        string contextId,gameId;
        int sourceGeneration;
        PlayerSide viewer;
        long sequence,sourceInteractionRevision;
        bool disposed;
        public MapPresenter(DemoController controller,IReadOnlyDictionary<string,string> names)
        { this.controller=controller??throw new ArgumentNullException(nameof(controller));this.names=names??throw new ArgumentNullException(nameof(names)); }
        public void ReplaceNames(IReadOnlyDictionary<string,string> next)
        {
            ThrowIfDisposed();if(view==null)throw new InvalidOperationException("No map view attached.");
            if(next==null)throw new ArgumentException("Missing display names.");
            var snapshot=controller.ReadView();
            var copy=next.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);
            if(snapshot.Countries.Any(c=>!copy.TryGetValue(c.Definition.Id.Value,out var value) || string.IsNullOrWhiteSpace(value)))throw new ArgumentException("Missing country display name.");
            names=new System.Collections.ObjectModel.ReadOnlyDictionary<string,string>(copy);
            try { Initialize(snapshot);Refresh(); } catch { Detach();throw; }
        }
        public void Attach(IMapView next)
        {
            ThrowIfDisposed();if(next==null)throw new ArgumentNullException(nameof(next));if(ReferenceEquals(next,view))throw new ArgumentException("A map view is already attached.");
            Detach();view=next;
            try
            {
                Initialize(controller.ReadView());
                var source=next;handler=intent=>Handle(source,intent);view.Intent+=handler;
                Refresh();
            }
            catch { Detach();throw; }
        }
        void Initialize(PlayerViewSnapshot snapshot)
        {
            frame=null;contextId=Guid.NewGuid().ToString("N");gameId=snapshot.GameId;viewer=snapshot.Viewer;sourceGeneration=controller.ViewGeneration;
            view.Clear();
            view.Initialize(new MapViewContext(contextId,snapshot.Countries.Select(c=>new MapNode(c.Definition.Id,
                names.TryGetValue(c.Definition.Id.Value,out var name)?name:c.Definition.Id.Value,c.Definition.Region,c.Definition.Stability,c.Definition.Battleground))));
            view.FocusOn(controller.SelectedCountry);
        }
        public MapFrame Refresh()
        {
            ThrowIfDisposed();if(view==null)throw new InvalidOperationException("No map view attached.");
            frame=null; // Input is disabled until the complete replacement frame has been presented.
            try { return RefreshCurrent(); }
            catch { Detach();throw; }
        }
        MapFrame RefreshCurrent()
        {
            var snapshot=controller.ReadView();
            if(sourceGeneration!=controller.ViewGeneration || gameId!=snapshot.GameId || viewer!=snapshot.Viewer)Initialize(snapshot);
            IEnumerable<CountryId> permitted=Array.Empty<CountryId>();
            if(snapshot.Deployment!=null)
            {
                var q=controller.DeploymentTargets();
                if(q.Error==RuleError.None && q.Version==snapshot.Version && q.Viewer==snapshot.Viewer && q.ExecutionId==snapshot.Deployment.ExecutionId && q.DecisionId==snapshot.Deployment.DecisionId)permitted=q.Targets;
            }
            else if(snapshot.Event==null)
            {
                var q=controller.LegalTargets();
                if(q.Error==RuleError.None && q.Version==snapshot.Version && q.Viewer==snapshot.Viewer && q.ContextId==snapshot.ContextId)permitted=q.Targets;
            }
            else
            {
                var q=controller.EventTargets();
                if(q.Error==RuleError.None && q.Version==snapshot.Version && q.Viewer==snapshot.Viewer && q.ExecutionId==snapshot.Event.ExecutionId && q.DecisionId==snapshot.Event.DecisionId)permitted=q.Targets;
            }
            var allowed=new HashSet<CountryId>(permitted);
            var draft=new HashSet<CountryId>(snapshot.Deployment!=null?controller.DeploymentDraft.Select(a=>a.Country):controller.EventDraft);
            sourceInteractionRevision=controller.InteractionRevision;
            var next=new MapFrame(contextId,snapshot.Version,checked(++sequence),Interaction(snapshot),snapshot.Countries.Select(c=>
                new MapCountryFrame(c.Definition.Id,c.US,c.USSR,c.Controller,allowed.Contains(c.Definition.Id),draft.Contains(c.Definition.Id),c.Definition.Id.Equals(controller.SelectedCountry))));
            if(!view.Present(next)) { frame=null;view.Clear();throw new InvalidOperationException("Map view rejected its public frame."); }
            frame=next;return next;
        }
        void Handle(IMapView source,MapIntent intent)
        {
            if(disposed || !ReferenceEquals(source,view) || frame==null || intent==null || intent.ContextId!=frame.ContextId || intent.Version!=frame.Version || intent.Sequence!=frame.Sequence || intent.InteractionKey!=frame.InteractionKey)return;
            var current=controller.ReadView();
            if(sourceGeneration!=controller.ViewGeneration || sourceInteractionRevision!=controller.InteractionRevision || current.GameId!=gameId || current.Viewer!=viewer || current.Version!=frame.Version || Interaction(current)!=frame.InteractionKey || !frame.Countries.Any(c=>c.Id.Equals(intent.Country)))return;
            controller.SelectCountry(intent.Country);
            if(current.Event!=null)controller.ToggleEventTarget(intent.Country);
            Refresh();
        }
        // Length-prefixed IDs avoid ambiguity; this key is display context, never an authorization token.
        static string Token(string value)=>value==null?"-1:":value.Length+":"+value;
        static string Interaction(PlayerViewSnapshot snapshot)=>snapshot.Deployment!=null?
            "deployment"+Token(snapshot.Deployment.ExecutionId)+Token(snapshot.Deployment.StageIndex.ToString(System.Globalization.CultureInfo.InvariantCulture))+Token(snapshot.Deployment.DecisionId)
            :snapshot.Event==null?"ops"+Token(snapshot.ContextId)
            :"event"+Token(snapshot.Event.ExecutionId)+Token(snapshot.Event.Stage.ToString())+Token(snapshot.Event.DecisionId);
        void Detach()
        {
            var old=view;view=null;frame=null;
            if(old==null)return;
            if(handler!=null)old.Intent-=handler;handler=null;
            try { old.Clear(); } finally { old.Dispose(); }
        }
        public void Dispose() { if(disposed)return;disposed=true;Detach(); }
        void ThrowIfDisposed() { if(disposed)throw new ObjectDisposedException(nameof(MapPresenter)); }
    }
}
