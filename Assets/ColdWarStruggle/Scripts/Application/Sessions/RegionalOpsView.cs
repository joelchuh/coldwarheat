using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class PublicRegionalOps
    {
        public int Total { get; } public int Spent { get; } public int OrdinaryTotal { get; }
        public bool HasTargets { get; } public IReadOnlyList<string> EligibleRegions { get; }
        internal PublicRegionalOps(OpsRegionalBudget budget)
        {Total=budget.EffectiveBudget;Spent=budget.Spent;OrdinaryTotal=budget.Quote.GeneralValue;HasTargets=budget.HasTargets;EligibleRegions=Array.AsReadOnly(budget.EligibleBonuses.Select(b=>b.Region).Distinct().ToArray());}
    }
}
