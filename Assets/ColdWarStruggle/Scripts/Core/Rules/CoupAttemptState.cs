using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class CoupAttemptRequest
    {
        public string Id { get; }
        public string GameId { get; }
        public string SourceActionId { get; }
        public string SourceRuleId { get; }
        public long ExpectedRevision { get; }
        public int TurnNumber { get; }
        public CountryId Target { get; }
        public PlayerSide Actor { get; }
        public PlayerSide PhasingPlayer { get; }
        public int Strength { get; }
        public int MilitaryCredit { get; }
        public CoupAttemptRequest(string id,string gameId,string sourceActionId,string sourceRuleId,long expectedRevision,int turnNumber,CountryId target,PlayerSide actor,PlayerSide phasingPlayer,int strength,int militaryCredit)
        {
            RequireId(id,128);RequireId(gameId);RequireId(sourceActionId);RequireId(sourceRuleId);RequireId(target.Value);
            if(expectedRevision<0 || turnNumber<1 || strength<1 || militaryCredit<1
                || (actor!=PlayerSide.US && actor!=PlayerSide.USSR) || (phasingPlayer!=PlayerSide.US && phasingPlayer!=PlayerSide.USSR))
                throw new ArgumentOutOfRangeException("Invalid coup request facts.");
            Id=id;GameId=gameId;SourceActionId=sourceActionId;SourceRuleId=sourceRuleId;ExpectedRevision=expectedRevision;TurnNumber=turnNumber;
            Target=target;Actor=actor;PhasingPlayer=phasingPlayer;Strength=strength;MilitaryCredit=militaryCredit;
        }
        internal static void RequireId(string value,int maximum=256)
        {
            if(string.IsNullOrWhiteSpace(value) || value.Length>maximum || value!=value.Trim())throw new ArgumentException("Invalid coup identifier.");
        }
        internal bool SameFacts(CoupAttemptRequest other)=>other!=null && Id==other.Id && GameId==other.GameId
            && SourceActionId==other.SourceActionId && SourceRuleId==other.SourceRuleId && ExpectedRevision==other.ExpectedRevision
            && TurnNumber==other.TurnNumber && Target.Equals(other.Target) && Actor==other.Actor && PhasingPlayer==other.PhasingPlayer
            && Strength==other.Strength && MilitaryCredit==other.MilitaryCredit;
    }
    public sealed class CoupAttemptReceipt
    {
        public CoupAttemptRequest Request { get; }
        public CoupCalculation Calculation { get; }
        public MilitaryCredit MilitaryCredit { get; }
        public DefconChangePlan DefconChange { get; }
        public RandomSnapshot RandomBefore { get; }
        public RandomSnapshot RandomAfter { get; }
        internal CoupAttemptReceipt(CoupAttemptRequest request,CoupCalculation calculation,MilitaryCredit militaryCredit,DefconChangePlan defconChange,RandomSnapshot randomBefore,RandomSnapshot randomAfter)
        { Request=request;Calculation=calculation;MilitaryCredit=militaryCredit;DefconChange=defconChange;RandomBefore=randomBefore;RandomAfter=randomAfter; }
    }
    public sealed class CoupAttemptState
    {
        public string GameId { get; }
        public long Revision { get; }
        public CoupDefinition Definition { get; }
        public DefconState Defcon { get; }
        public MilitaryOperationsState Military { get; }
        public IReadOnlyDictionary<CountryId,CountryState> Countries { get; }
        public RandomSnapshot Random { get; }
        public IReadOnlyList<string> ActiveEffectIds { get; }
        public IReadOnlyDictionary<string,CoupAttemptReceipt> Receipts { get; }
        internal CoupAttemptState(string gameId,long revision,CoupDefinition definition,DefconState defcon,MilitaryOperationsState military,IReadOnlyDictionary<CountryId,CountryState> countries,RandomSnapshot random,IReadOnlyList<string> activeEffects,IReadOnlyDictionary<string,CoupAttemptReceipt> receipts)
        {
            GameId=gameId;Revision=revision;Definition=definition;Defcon=defcon;Military=military;Random=random;
            Countries=new ReadOnlyDictionary<CountryId,CountryState>(countries.ToDictionary(p=>p.Key,p=>p.Value));
            ActiveEffectIds=Array.AsReadOnly(activeEffects.ToArray());
            Receipts=new ReadOnlyDictionary<string,CoupAttemptReceipt>(receipts.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal));
        }
        public static CoupAttemptState Create(string gameId,CoupDefinition definition,DefconState defcon,MilitaryOperationsState military,IReadOnlyDictionary<CountryId,CountryState> countries,RandomSnapshot random,IEnumerable<string> activeEffectIds)
        {
            if(definition==null || defcon==null || military==null || countries==null || random==null || activeEffectIds==null)throw new ArgumentNullException();
            CoupAttemptRequest.RequireId(gameId);
            var copied=countries.ToDictionary(p=>p.Key,p=>p.Value);
            if(copied.Count!=defcon.Definition.Map.Countries.Count || copied.Keys.Any(id=>!defcon.Definition.Map.Contains(id)))
                throw new ArgumentException("Coup country snapshot must cover the bound map exactly.");
            var effects=activeEffectIds.Take(129).ToArray();
            if(effects.Length>128 || effects.Distinct(StringComparer.Ordinal).Count()!=effects.Length)throw new ArgumentException("Invalid coup effect snapshot.");
            foreach(var id in effects)CoupAttemptRequest.RequireId(id);
            return new CoupAttemptState(gameId,0,definition,defcon,military,copied,random,effects,new Dictionary<string,CoupAttemptReceipt>());
        }
    }
}
