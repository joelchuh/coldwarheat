using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;

namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        readonly Dictionary<CountryId,int> allocationDraft=new Dictionary<CountryId,int>();
        public IReadOnlyList<InfluenceAllocation> AllocationDraft
        {
            get
            {
                EnsureDraftContext(ReadView());
                return Array.AsReadOnly(allocationDraft.OrderBy(p=>p.Key.Value,StringComparer.Ordinal)
                    .Select(p=>new InfluenceAllocation(p.Key,p.Value)).ToArray());
            }
        }
        bool TryAllocationEdit(EventTargetsResult query,CountryId country,int delta,out int amount)
        {
            amount=0;
            if(query.Error!=RuleError.None || query.Kind!=InfluenceDecisionKind.Allocation || delta!=1 && delta!=-1)return false;
            var capacity=query.Capacities.FirstOrDefault(c=>c.Country.Equals(country)).Amount;
            if(capacity==0)return false;
            allocationDraft.TryGetValue(country,out var before);
            long next=(long)before+delta;
            if(next<0 || next>capacity || allocationDraft.Values.Sum(v=>(long)v)+delta>query.RequiredAmount)return false;
            amount=(int)next;return true;
        }
        public bool CanChangeEventAllocation(CountryId country,int delta)=>TryAllocationEdit(EventTargets(),country,delta,out _);
        public bool ChangeEventAllocation(CountryId country,int delta)
        {
            if(!TryAllocationEdit(EventTargets(),country,delta,out var amount))return false;
            if(amount==0)allocationDraft.Remove(country);else allocationDraft[country]=amount;
            MarkInteractionChanged();return true;
        }
    }
}
