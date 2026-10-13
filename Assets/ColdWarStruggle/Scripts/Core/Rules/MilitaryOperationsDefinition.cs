using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum MilitaryCreditKind { OperationsCoup, WarEvent, FreeCoup, Realignment, Influence, Space, DirectEventFixedCredit }
    public enum MilitaryCreditBasis { EffectiveMilitaryOperationsCredit, VerifiedCardParameter, None }
    public sealed class MilitaryOperationsDefinition
    {
        public int InitialEarned { get; }
        public int ResetEarned { get; }
        public int PenaltyVpPerShortfall { get; }
        public IReadOnlyDictionary<MilitaryCreditKind,MilitaryCreditBasis> CreditSources { get; }
        public MilitaryOperationsDefinition(int initial,int reset,int penalty,IReadOnlyDictionary<MilitaryCreditKind,MilitaryCreditBasis> sources)
        {
            if(initial<0 || reset<0 || penalty<1)throw new ArgumentOutOfRangeException("Invalid military parameters.");
            if(sources==null)throw new ArgumentNullException(nameof(sources));
            if(sources.Count!=Enum.GetValues(typeof(MilitaryCreditKind)).Length
                || sources.Any(p=>!Enum.IsDefined(typeof(MilitaryCreditKind),p.Key) || !Enum.IsDefined(typeof(MilitaryCreditBasis),p.Value)))
                throw new ArgumentException("Every supported military source is required.");
            InitialEarned=initial;ResetEarned=reset;PenaltyVpPerShortfall=penalty;
            CreditSources=new ReadOnlyDictionary<MilitaryCreditKind,MilitaryCreditBasis>(sources.ToDictionary(p=>p.Key,p=>p.Value));
        }
    }
    public sealed class MilitaryCredit
    {
        public string Id { get; }
        public int TurnNumber { get; }
        public PlayerSide Beneficiary { get; }
        public MilitaryCreditKind Kind { get; }
        public MilitaryCreditBasis Basis { get; }
        public int Amount { get; }
        public string SourceRuleId { get; }
        public MilitaryCredit(string id,int turn,PlayerSide beneficiary,MilitaryCreditKind kind,MilitaryCreditBasis basis,int amount,string sourceRuleId)
        {
            RequireIdentifier(id);RequireIdentifier(sourceRuleId);
            if(turn<1 || (beneficiary!=PlayerSide.US && beneficiary!=PlayerSide.USSR)
                || !Enum.IsDefined(typeof(MilitaryCreditKind),kind) || !Enum.IsDefined(typeof(MilitaryCreditBasis),basis)
                || amount<0 || (basis==MilitaryCreditBasis.None && amount!=0))
                throw new ArgumentException("Invalid military credit facts.");
            Id=id;TurnNumber=turn;Beneficiary=beneficiary;Kind=kind;Basis=basis;Amount=amount;SourceRuleId=sourceRuleId;
        }
        internal static void RequireIdentifier(string value)
        {
            if(string.IsNullOrWhiteSpace(value) || value.Length>256)throw new ArgumentException("Military identifier must contain 1–256 characters.");
        }
        internal bool SameFacts(MilitaryCredit other)=>Id==other.Id && TurnNumber==other.TurnNumber && Beneficiary==other.Beneficiary
            && Kind==other.Kind && Basis==other.Basis && Amount==other.Amount && SourceRuleId==other.SourceRuleId;
    }
    public sealed class MilitaryOperationsState
    {
        public MilitaryOperationsDefinition Definition { get; }
        public GameTurnContext Turn { get; }
        public int UsEarned { get; }
        public int UssrEarned { get; }
        public IReadOnlyDictionary<string,MilitaryCredit> AppliedCredits { get; }
        public MilitaryTurnEndAssessment Assessment { get; }
        internal MilitaryOperationsState(MilitaryOperationsDefinition definition,GameTurnContext turn,int us,int ussr,
            IReadOnlyDictionary<string,MilitaryCredit> credits,MilitaryTurnEndAssessment assessment)
        {
            Definition=definition;Turn=turn;UsEarned=us;UssrEarned=ussr;Assessment=assessment;
            AppliedCredits=new ReadOnlyDictionary<string,MilitaryCredit>(credits.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal));
        }
        public static MilitaryOperationsState Create(MilitaryOperationsDefinition definition,GameTurnContext turn)
        {
            if(definition==null || turn==null)throw new ArgumentNullException();
            return new MilitaryOperationsState(definition,turn,definition.InitialEarned,definition.InitialEarned,new Dictionary<string,MilitaryCredit>(),null);
        }
    }
    public sealed class MilitaryTurnEndAssessment
    {
        public string Id { get; }
        public int TurnNumber { get; }
        public DefconState Defcon { get; }
        public int UsEarned { get; }
        public int UssrEarned { get; }
        public int UsShortfall { get; }
        public int UssrShortfall { get; }
        public int NetVpForUS { get; }
        internal MilitaryTurnEndAssessment(string id,MilitaryOperationsState state,DefconState defcon,int usShortfall,int ussrShortfall,int net)
        {
            Id=id;TurnNumber=state.Turn.Number;Defcon=defcon;UsEarned=state.UsEarned;UssrEarned=state.UssrEarned;
            UsShortfall=usShortfall;UssrShortfall=ussrShortfall;NetVpForUS=net;
        }
    }
}
