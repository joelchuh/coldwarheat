using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        readonly Dictionary<CountryId,int> relocationSources=new Dictionary<CountryId,int>();
        readonly Dictionary<CountryId,int> relocationDestinations=new Dictionary<CountryId,int>();
        bool relocationEditingDestinations;
        public bool RelocationEditingDestinations { get {EnsureDraftContext(ReadView());return relocationEditingDestinations;} }
        static IReadOnlyList<InfluenceAllocation> RelocationSnapshot(Dictionary<CountryId,int> draft)
            =>Array.AsReadOnly(draft.OrderBy(p=>p.Key.Value,StringComparer.Ordinal).Select(p=>new InfluenceAllocation(p.Key,p.Value)).ToArray());
        public IReadOnlyList<InfluenceAllocation> RelocationSourceDraft { get {EnsureDraftContext(ReadView());return RelocationSnapshot(relocationSources);} }
        public IReadOnlyList<InfluenceAllocation> RelocationDestinationDraft { get {EnsureDraftContext(ReadView());return RelocationSnapshot(relocationDestinations);} }
        void ClearRelocationDraft()
        {relocationSources.Clear();relocationDestinations.Clear();relocationEditingDestinations=false;}
        public void SelectRelocationPhase(bool destinations)
        {
            var q=EventTargets();if(q.Error!=RuleError.None || q.Kind!=InfluenceDecisionKind.Relocation || destinations==relocationEditingDestinations)return;
            relocationEditingDestinations=destinations;MarkInteractionChanged();
        }
        bool CanConfirmRelocation(EventTargetsResult q)
        {
            var from=relocationSources.Values.Sum(v=>(long)v);var to=relocationDestinations.Values.Sum(v=>(long)v);
            return q.Error==RuleError.None && q.Kind==InfluenceDecisionKind.Relocation && from==to && from<=q.MaximumAmount;
        }
        public int RelocationCapacity(CountryId country)
        {
            var q=EventTargets();if(q.Error!=RuleError.None || q.Kind!=InfluenceDecisionKind.Relocation)return 0;
            var opposite=relocationEditingDestinations?relocationSources:relocationDestinations;
            if(opposite.ContainsKey(country))return 0; // Same-country round trips await a ruling.
            return (relocationEditingDestinations?q.DestinationCapacities:q.SourceCapacities).FirstOrDefault(a=>a.Country.Equals(country)).Amount;
        }
        bool TryRelocationEdit(CountryId country,int delta,out int amount)
        {
            amount=0;var q=EventTargets();if(q.Error!=RuleError.None || q.Kind!=InfluenceDecisionKind.Relocation || delta!=1 && delta!=-1)return false;
            var draft=relocationEditingDestinations?relocationDestinations:relocationSources;var capacity=RelocationCapacity(country);
            if(capacity==0)return false;draft.TryGetValue(country,out var before);var next=(long)before+delta;
            if(next<0 || next>capacity)return false;
            var limit=relocationEditingDestinations?relocationSources.Values.Sum(v=>(long)v):q.MaximumAmount;
            // Removing an over-allocated destination remains possible after a source edit.
            if(delta>0 && draft.Values.Sum(v=>(long)v)+delta>limit)return false;
            amount=(int)next;return true;
        }
        public bool CanChangeRelocation(CountryId country,int delta)=>TryRelocationEdit(country,delta,out _);
        public bool ChangeRelocation(CountryId country,int delta)
        {
            if(!TryRelocationEdit(country,delta,out var amount))return false;
            var draft=relocationEditingDestinations?relocationDestinations:relocationSources;
            if(amount==0)draft.Remove(country);else draft[country]=amount;MarkInteractionChanged();return true;
        }
        public IReadOnlyList<CountryId> RelocationMapTargets()
        {
            var q=EventTargets();if(q.Error!=RuleError.None || q.Kind!=InfluenceDecisionKind.Relocation)return Array.AsReadOnly(Array.Empty<CountryId>());
            return Array.AsReadOnly((relocationEditingDestinations?q.DestinationCapacities:q.SourceCapacities)
                .Where(a=>CanChangeRelocation(a.Country,1)||CanChangeRelocation(a.Country,-1)).Select(a=>a.Country).ToArray());
        }
    }
}
